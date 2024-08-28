using Ascon.Pilot.Common;
using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.WebSockets;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using static System.Net.WebRequestMethods;

namespace PilotRocketChatGateway.UserContext
{
    public interface IRCDataConverter
    {
        ICommonDataConverter CommonDataConverter { get; }
        IMediaAttachmentLoader AttachmentLoader { get; }
        Room ConvertToRoom(DChat chat, DMessage lastMessage);
        Subscription ConvertToSubscription(DChatInfo chat);
        Message ConvertToMessage(DMessage msg, DChat chat);
        string ConvertToRoomId(DChat chat);
    }
    public class RCDataConverter : IRCDataConverter
    {
        private readonly ILogger _logger;
        private readonly IContext _context;
        public RCDataConverter(IContext context, IMediaAttachmentLoader attachLoader, ICommonDataConverter commonDataConverter, ILogger logger)
        {
            _logger = logger;
            _context = context;
            AttachmentLoader = attachLoader;
            CommonDataConverter = commonDataConverter;
        }

        public ICommonDataConverter CommonDataConverter { get; }
        public IMediaAttachmentLoader AttachmentLoader { get; }

        public static T GetMessageData<T>(DMessage msg)
        {
            using (var stream = new MemoryStream(msg.Data))
            {
                return ProtoSerializer.Deserialize<T>(stream);
            }
        }
        public Room ConvertToRoom(DChat chat, DMessage lastMessage)
        {
            var roomId = ConvertToRoomId(chat);
            return new Room()
            {
                updatedAt = CommonDataConverter.ConvertToJSDate(lastMessage.ServerDate.Value),
                name = chat.Type == ChatKind.Personal ? string.Empty : chat.Name,
                id = roomId,
                channelType = GetChannelType(chat),
                creationDate = CommonDataConverter.ConvertToJSDate(chat.CreationDateUtc),
                lastMessage = lastMessage.Type == MessageType.ChatCreation ? null : ConvertToMessage(lastMessage, chat),
            };
        }
        public Subscription ConvertToSubscription(DChatInfo chat)
        {
            var roomId = ConvertToRoomId(chat.Chat);
            return new Subscription()
            {
                updatedAt = CommonDataConverter.ConvertToJSDate(chat.LastMessage.ServerDate.Value),
                lastSeen = LoadLastSeenChatsDate(chat),
                unread = chat.UnreadMessagesNumber,
                open = true,
                name = chat.Chat.Type == ChatKind.Personal ? GetPersonalChatTarget(chat.Chat).username : chat.Chat.Name,
                displayName = chat.Chat.Type == ChatKind.Personal ? GetPersonalChatTarget(chat.Chat).name : chat.Chat.Name,
                alert = chat.UnreadMessagesNumber > 0,
                id = roomId,
                roomId = roomId,
                channelType = GetChannelType(chat.Chat),
                disableNotifications = false
            };
        }

        public Message ConvertToMessage(DMessage msg, DChat chat)
        {
            var origin = GetOriginMessage(msg);
            var user = CommonDataConverter.ConvertToUser(_context.RemoteService.ServerApi.GetPerson(origin.CreatorId));
            var roomId = ConvertToRoomId(chat);
            var editedAt = GetEditedAt(origin);
            return new Message()
            {
                id = GetMessageId(origin),
                roomId = roomId,
                updatedAt = CommonDataConverter.ConvertToJSDate(origin.ServerDate.Value),
                creationDate = CommonDataConverter.ConvertToJSDate(origin.ServerDate.Value),
                msg = GetMessageText(origin),
                u = user,
                attachments = LoadAttachments(roomId, origin),
                editedAt = editedAt,
                editedBy = GetEditor(origin),
                type = GetMsgType(origin),
                role = GetRole(origin)
            };
        }

        public string ConvertToRoomId(DChat chat)
        {
            return chat.Type == ChatKind.Personal ? GetPersonalChatTarget(chat).id : chat.Id.ToString();
        }

        private string GetRole(DMessage msg)
        {
            if (msg.Type != MessageType.ChatMembers)
                return null;

            var data = msg.GetMessageData<DChatMembersData>();
            var change = data.Changes.First();
            if (change.IsAdmin.HasValue)
                return "moderator";
            
            return null;
        }
        private string GetMsgType(DMessage msg)
        {
            switch (msg.Type)
            {
                case MessageType.ChatMembers:
                    return GetChatMemberMsgType(msg);
                case MessageType.ChatChanged:
                    var cData = msg.GetMessageData<DChatChange>();
                    return cData.IsRenamed ? "r" : "room_changed_description";
                default:
                    return null;
            }
        }

        private static string GetChatMemberMsgType(DMessage msg)
        {
            var data = msg.GetMessageData<DChatMembersData>();
            var change = data.Changes.First();

            if (change.IsAdded.HasValue)
                return "au";

            if (change.IsDeleted.HasValue)
                return "ru";

            if (change.IsAdmin.HasValue)
            {
                if (change.IsAdmin.Value)
                    return "subscription-role-added";
                else
                    return "subscription-role-removed";
            }

            return string.Empty;
        }

        private string[] GetUserNames(DChat chat)
        {
            var members = _context.RemoteService.ServerApi.GetChatMembers(chat.Id);
            var result = new List<string>();
            foreach (var member in members)
            {
                try
                {
                    var login = _context.RemoteService.ServerApi.GetPerson(member.PersonId).Login;
                    result.Add(login);
                }
                catch(Exception e)
                {
                    _logger.LogError(e.Message);
                }
            }
            return result.ToArray();
        }
        private DMessage GetOriginMessage(DMessage msg)
        {
            if (msg.Type == MessageType.EditTextMessage)
                return _context.RemoteService.ServerApi.GetMessage(msg.RelatedMessageId.Value);
            return msg;
        }
        private string LoadLastSeenChatsDate(DChatInfo chat)
        {
            if (chat.UnreadMessagesNumber == 0)
                return CommonDataConverter.ConvertToJSDate(chat.LastMessage.ServerDate.Value);

            var unread = _context.RemoteService.ServerApi.GetMessages(chat.Chat.Id, DateTime.MinValue, DateTime.MaxValue, chat.UnreadMessagesNumber);
            var earliestUnreadMessage = unread.LastOrDefault();

            if (earliestUnreadMessage == null)
                return CommonDataConverter.ConvertToJSDate(chat.LastMessage.ServerDate.Value);

            return CommonDataConverter.ConvertToJSDate(earliestUnreadMessage.ServerDate.Value);
        }


        private IList<Guid> GetAttachmentId(byte[] msgData)
        {
            using (var stream = new MemoryStream(msgData))
            {
                var data = ProtoBuf.Serializer.Deserialize<DTextMessageData>(stream);
                if (data.Attachments.Any() == false)
                    return new List<Guid>();

                return data.Attachments.Where(x => x.Type == ChatRelationType.Attach).Select(x => x.ObjectId).ToList();
            }
        }

        private Attachment LoadReplyAttachments(string roomId, DMessage msg)
        {
            var related = _context.RemoteService.ServerApi.GetMessage(msg.RelatedMessageId.Value);
            var edited = GetEditMessage(related);
            var replyAttachId = edited == null ? GetAttachmentId(related.Data) : GetAttachmentId(edited.Data);
            var creator = _context.RemoteService.ServerApi.GetPerson(related.CreatorId);

            return new Attachment()
            {
                text = GetMessageText(related),
                author_name = CommonDataConverter.GetUserDisplayName(creator),
                creationDate = CommonDataConverter.ConvertToJSDate(msg.ServerDate.Value),
                message_link = $"{roomId}?msg={GetMessageId(related)}",
                attachments = LoadAttachments(replyAttachId)
            };
        }

        private IList<Attachment> LoadAttachments(string roomId, DMessage msg)
        {
            List<Attachment> attachments = new List<Attachment>();
            try
            {
                if (msg.Type != MessageType.MessageAnswer && msg.Type != MessageType.TextMessage)
                return attachments;

                if (msg.Type == MessageType.MessageAnswer)
                {
                    var replyAttach = LoadReplyAttachments(roomId, msg);
                    attachments.Add(replyAttach);
                }
          
                var edited = GetEditMessage(msg);
                var attachId = edited == null ? GetAttachmentId(msg.Data) : GetAttachmentId(edited.Data);
                return attachments.Concat(LoadAttachments(attachId)).ToList();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return attachments;
            }
        }
        private IList<Attachment> LoadAttachments(IList<Guid> objIds)
        {
            var result = new List<Attachment>();
            foreach (var id in objIds)
            {
                var attach = AttachmentLoader.LoadAttachment(id);
                if (attach != null)
                    result.Add(attach);
            }
            return result;
        }

        private string GetMessageText(DMessage msg)
        {
            switch (msg.Type)
            {
                case MessageType.ChatMembers:
                    return GetChatMembersText(msg);
                case MessageType.ChatChanged:
                    return GetChatChangedText(msg);
                default:
                    return GetDisplayMessageText(msg);
            }
        }

        private string GetChatChangedText(DMessage msg)
        {
            var data = msg.GetMessageData<DChatChange>();
            return data.IsRenamed ? data.Chat.Name : data.Chat.Description;
        }
        private string GetChatMembersText(DMessage msg)
        {
            var data = msg.GetMessageData<DChatMembersData>();
            var personId = data.Changes.First().PersonId;
            var person = _context.RemoteService.ServerApi.GetPerson(personId);
            return person.Login;
        }
        private string GetDisplayMessageText(DMessage msg)
        {
            var edit = GetEditMessage(msg);
            if (edit != null)
            {
                using (var editStream = new MemoryStream(edit.Data))
                    return ProtoBuf.Serializer.Deserialize<string>(editStream);
            }

            using (var stream = new MemoryStream(msg.Data))
                return ProtoBuf.Serializer.Deserialize<string>(stream);
        }
        private string GetMessageId(DMessage msg)
        {
            switch (msg.Type)
            {
                case MessageType.ChatMembers:
                case MessageType.ChatChanged:
                    return msg.Id.ToString();
                default:
                    var msgData = GetMessageData<DTextMessageData>(msg);
                    return string.IsNullOrEmpty(msgData.ThirdPartyInfo) ? msg.Id.ToString() : msgData.ThirdPartyInfo;
            }
        }

        private User GetPersonalChatTarget(DChat chat)
        {
            var currentPersonId = _context.RemoteService.ServerApi.CurrentPerson.Id;
            if (chat.CreatorId != currentPersonId)
            {
                var person = _context.RemoteService.ServerApi.GetPerson(chat.CreatorId);
                return CommonDataConverter.ConvertToUser(person);
            }

            if (string.IsNullOrEmpty(chat.Name))
            {
                var members = _context.RemoteService.ServerApi.GetChatMembers(chat.Id);
                var target = members.First(x => x.PersonId != currentPersonId);
                var person = _context.RemoteService.ServerApi.GetPerson(target.PersonId);
                return CommonDataConverter.ConvertToUser(person);
            }

            var p = _context.RemoteService.ServerApi.GetPerson(int.Parse(chat.Name));
            return CommonDataConverter.ConvertToUser(p);
        }

        private string GetEditedAt(DMessage msg)
        {
            var edit = GetEditMessage(msg);
            if (edit == null)
                return string.Empty;

            return CommonDataConverter.ConvertToJSDate(edit.ServerDate.Value);
        }
        private User GetEditor(DMessage msg)
        {
            var edit = GetEditMessage(msg);
            if (edit == null)
                return null;

            return CommonDataConverter.ConvertToUser(_context.RemoteService.ServerApi.GetPerson(msg.CreatorId));
        }
        private DMessage GetEditMessage(DMessage msg)
        {
            return msg.RelatedMessages.Where(x => x.Type == MessageType.EditTextMessage).LastOrDefault();
        }
        private string GetChannelType(DChat chat)
        {
            return chat.Type == ChatKind.Personal ? ChatType.PERSONAL_CHAT_TYPE : ChatType.GROUP_CHAT_TYPE;
        }
    }
}
