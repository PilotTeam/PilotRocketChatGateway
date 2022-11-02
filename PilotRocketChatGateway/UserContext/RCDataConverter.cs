using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.WebSockets;

namespace PilotRocketChatGateway.UserContext
{
    public interface IRCDataConverter
    {
        ICommonDataConverter CommonDataConverter { get; }
        IAttachmentLoader AttachmentLoader { get; }
        Room ConvertToRoom(DChat chat, IList<DChatRelation> chatRelations, DMessage lastMessage);
        Subscription ConvertToSubscription(DChatInfo chat);
        Message ConvertToMessage(DMessage msg);
        Message ConvertToMessage(DMessage msg, DChat chat, Guid objId);
        string ConvertToRoomId(DChat chat);
    }
    public class RCDataConverter : IRCDataConverter
    {
        private readonly IContext _context;

        public RCDataConverter(IContext context, IAttachmentLoader attachLoader, ICommonDataConverter commonDataConverter)
        {
            _context = context;
            AttachmentLoader = attachLoader;
            CommonDataConverter = commonDataConverter;
        }

        public ICommonDataConverter CommonDataConverter { get; }
        public IAttachmentLoader AttachmentLoader { get; }

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
            var attachId = GetMsgAttachmentId(chatRelations, lastMessage.Id);
            return new Room()
            {
                updatedAt = CommonDataConverter.ConvertToJSDate(lastMessage.LocalDate),
                name = chat.Type == ChatKind.Personal ? string.Empty : chat.Name,
                id = roomId,
                channelType = GetChannelType(chat),
                creationDate = CommonDataConverter.ConvertToJSDate(chat.CreationDateUtc),
                lastMessage = lastMessage.Type == MessageType.TextMessage ? ConvertToMessage(lastMessage, chat, attachId) : null,
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
            var attachId = GetMsgAttachmentId(chat.Relations, origin.Id);
            return ConvertToMessage(origin, chat.Chat, attachId);
        }
        public Message ConvertToMessage(DMessage msg, DChat chat, Guid objId)
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
                attachments = LoadAttachments(objId),
                editedAt = editedAt,
                editedBy = GetEditor(msg)
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
        private Guid GetMsgAttachmentId(IList<DChatRelation> chatRelations, Guid msgId)
        {
            var attachs = AttachmentLoader.GetAttachmentsIds(chatRelations);
            return attachs.Where(x => x.Key == msgId).FirstOrDefault().Value;
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

            var unread = _context.RemoteService.ServerApi.GetMessages(chat.Chat.Id, DateTime.MaxValue, chat.UnreadMessagesNumber);
            var earliestUnreadMessage = unread.LastOrDefault(x => x.Type == MessageType.TextMessage);

            if (earliestUnreadMessage == null)
                return CommonDataConverter.ConvertToJSDate(chat.LastMessage.LocalDate);

            return CommonDataConverter.ConvertToJSDate(earliestUnreadMessage.LocalDate);
        }
        private IList<Attachment> LoadAttachments(Guid objId)
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
