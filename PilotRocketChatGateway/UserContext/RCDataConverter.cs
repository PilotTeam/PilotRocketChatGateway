using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.WebSockets;
using System.Collections.Generic;
using System.Net.Mail;
using static System.Net.WebRequestMethods;

namespace PilotRocketChatGateway.UserContext
{
    public interface IRCDataConverter
    {
        ICommonDataConverter CommonDataConverter { get; }
        IMediaAttachmentLoader AttachmentLoader { get; }
        IList<MessageType> ShowedMessageType { get; }
        Room ConvertToRoom(DChat chat, IList<DChatRelation> chatRelations, DMessage lastMessage);
        Subscription ConvertToSubscription(DChatInfo chat);
        Message ConvertToMessage(DMessage msg);
        Message ConvertToMessage(DMessage msg, DChat chat, Dictionary<Guid, Guid> attachs);
        string ConvertToRoomId(DChat chat);
    }
    public class RCDataConverter : IRCDataConverter
    {
        private readonly IList<MessageType> _showedMessageType = new List<MessageType>() { MessageType.TextMessage, MessageType.MessageAnswer };
        private readonly IContext _context;
        public RCDataConverter(IContext context, IMediaAttachmentLoader attachLoader, ICommonDataConverter commonDataConverter)
        {
            _context = context;
            AttachmentLoader = attachLoader;
            CommonDataConverter = commonDataConverter;
        }

        public IList<MessageType> ShowedMessageType => _showedMessageType;
        public ICommonDataConverter CommonDataConverter { get; }
        public IMediaAttachmentLoader AttachmentLoader { get; }

        public static T GetMessageData<T>(DMessage msg)
        {
            using (var stream = new MemoryStream(msg.Data))
            {
                return ProtoSerializer.Deserialize<T>(stream);
            }
        }
        public Room ConvertToRoom(DChat chat, IList<DChatRelation> chatRelations, DMessage lastMessage)
        {
            var roomId = ConvertToRoomId(chat);
            var attachs = AttachmentLoader.GetAttachmentsIds(chatRelations);
            return new Room()
            {
                updatedAt = CommonDataConverter.ConvertToJSDate(lastMessage.LocalDate),
                name = chat.Type == ChatKind.Personal ? string.Empty : chat.Name,
                id = roomId,
                channelType = GetChannelType(chat),
                creationDate = CommonDataConverter.ConvertToJSDate(chat.CreationDateUtc),
                lastMessage = ShowedMessageType.Contains(lastMessage.Type) ? ConvertToMessage(lastMessage, chat, attachs) : null,
                usernames = GetUserNames(chat)
            };
        }
        public Subscription ConvertToSubscription(DChatInfo chat)
        {
            return new Subscription()
            {
                updatedAt = CommonDataConverter.ConvertToJSDate(chat.LastMessage.LocalDate),
                lastSeen = LoadLastSeenChatsDate(chat),
                unread = chat.UnreadMessagesNumber,
                open = true,
                name = chat.Chat.Type == ChatKind.Personal ? GetPersonalChatTarget(chat.Chat).username : chat.Chat.Name,
                displayName = chat.Chat.Type == ChatKind.Personal ? GetPersonalChatTarget(chat.Chat).name : chat.Chat.Name,
                alert = chat.UnreadMessagesNumber > 0,
                id = ConvertToRoomId(chat.Chat),
                roomId = ConvertToRoomId(chat.Chat),
                channelType = GetChannelType(chat.Chat)
            };
        }
        public Message ConvertToMessage(DMessage msg)
        {
            var origin = GetOriginMessage(msg);
            var chat = _context.RemoteService.ServerApi.GetChat(origin.ChatId);
            var attachs = AttachmentLoader.GetAttachmentsIds(chat.Relations);
            return ConvertToMessage(origin, chat.Chat, attachs);
        }
        public Message ConvertToMessage(DMessage msg, DChat chat, Dictionary<Guid, Guid>  attachs)
        {
            var user = CommonDataConverter.ConvertToUser(_context.RemoteService.ServerApi.GetPerson(msg.CreatorId));
            var roomId = ConvertToRoomId(chat);
            var editedAt = GetEditedAt(msg);
            return new Message()
            {
                id = GetMessageId(msg),
                roomId = roomId,
                updatedAt = CommonDataConverter.ConvertToJSDate(msg.LocalDate),
                creationDate = CommonDataConverter.ConvertToJSDate(msg.LocalDate),
                msg = GetMessageText(msg),
                u = user,
                attachments = LoadAttachments(roomId, msg),
                editedAt = editedAt,
                editedBy = GetEditor(msg),
            };
        }
        public string ConvertToRoomId(DChat chat)
        {
            return chat.Type == ChatKind.Personal ? GetPersonalChatTarget(chat).id : chat.Id.ToString();
        }
        private string[] GetUserNames(DChat chat)
        {
            var members = _context.RemoteService.ServerApi.GetChatMembers(chat.Id);
            return members.Select(x => _context.RemoteService.ServerApi.GetPerson(x.PersonId).Login).ToArray();
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
                return CommonDataConverter.ConvertToJSDate(chat.LastMessage.LocalDate);

            var unread = _context.RemoteService.ServerApi.GetMessages(chat.Chat.Id, DateTime.MinValue, DateTime.MaxValue, chat.UnreadMessagesNumber);
            var earliestUnreadMessage = unread.LastOrDefault(x => ShowedMessageType.Contains(x.Type));

            if (earliestUnreadMessage == null)
                return CommonDataConverter.ConvertToJSDate(chat.LastMessage.LocalDate);

            return CommonDataConverter.ConvertToJSDate(earliestUnreadMessage.LocalDate);
        }


        private Guid? GetAttachmentId(byte[] msgData)
        {
            using (var stream = new MemoryStream(msgData))
            {
                var data = ProtoBuf.Serializer.Deserialize<DTextMessageData>(stream);
                if (data.Attachments.Any() == false)
                    return null;

                return data.Attachments.Where(x => x.Type == ChatRelationType.Attach).FirstOrDefault()?.ObjectId;
            }
        }

        private Attachment LoadReplyAttachments(string roomId, DMessage msg)
        {
            var related = _context.RemoteService.ServerApi.GetMessage(msg.RelatedMessageId.Value);
            var edited = GetEditMessage(related);
            var replyAttachId = edited == null ? GetAttachmentId(related.Data) : GetAttachmentId(edited.Data);

            return new Attachment()
            {
                text = GetMessageText(related),
                author_name = CommonDataConverter.ConvertToUser(_context.RemoteService.ServerApi.GetPerson(msg.CreatorId)).username,
                creationDate = CommonDataConverter.ConvertToJSDate(msg.LocalDate),
                message_link = $"{roomId}?msg={GetMessageId(related)}",
                attachments = LoadImageAttachments(replyAttachId)
            };
        }

        private IList<Attachment> LoadAttachments(string roomId, DMessage msg)
        {
            List<Attachment> attachments = new List<Attachment>();
            if (msg.Type == MessageType.MessageAnswer)
            {
                var replyAttach = LoadReplyAttachments(roomId, msg); 
                attachments.Add(replyAttach); 
            }

            var edited = GetEditMessage(msg);
            var attachId = edited == null ? GetAttachmentId(msg.Data) : GetAttachmentId(edited.Data);
            return attachments.Concat(LoadImageAttachments(attachId)).ToList();
        }
        private IList<Attachment> LoadImageAttachments(Guid? objId)
        {
            var attach = AttachmentLoader.LoadAttachment(objId);
            return attach == null ? new List<Attachment> { } : new List<Attachment> { attach };
        }

        private string GetMessageText(DMessage msg)
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
            var msgData = GetMessageData<DTextMessageData>(msg);
            return string.IsNullOrEmpty(msgData.ThirdPartyInfo) ? msg.Id.ToString() : msgData.ThirdPartyInfo;
        }
        private User GetPersonalChatTarget(DChat chat)
        {
            var members = _context.RemoteService.ServerApi.GetChatMembers(chat.Id);
            var currentPersonId = _context.RemoteService.ServerApi.CurrentPerson.Id;
            var target = members.First(x => x.PersonId != currentPersonId);
            var person = _context.RemoteService.ServerApi.GetPerson(target.PersonId);
            return CommonDataConverter.ConvertToUser(person);
        }
        private string GetEditedAt(DMessage msg)
        {
            var edit = GetEditMessage(msg);
            if (edit == null)
                return string.Empty;

            return CommonDataConverter.ConvertToJSDate(edit.LocalDate);
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
            return chat.Type == ChatKind.Personal ? "d" : "p";
        }
    }
}
