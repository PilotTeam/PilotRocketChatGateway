using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.WebSockets;
using SixLabors.ImageSharp;

namespace PilotRocketChatGateway.UserContext
{
    public interface IChatService : IService
    {
        Room LoadRoom(Guid id);
        IList<Room> LoadRooms();
        Subscription LoadRoomsSubscription(string roomId);
        IList<Subscription> LoadRoomsSubscriptions();
        Room LoadPersonalRoom(string username);
        IList<Message> LoadMessages(string roomId, int count, string latest);
        IList<Message> LoadUnreadMessages(string roomId);
        User LoadUser(int usderId);
        string GetRoomId(DChat chat);
        IList<User> LoadUsers(int count);
        IList<User> LoadMembers(string roomId);
        void SendTextMessageToServer(string roomId, string msgId, string text);
        void SendEditMessageToServer(string roomId, string msgId, string text);
        void SendAttachmentMessageToServer(string roomId, string fileName, byte[] data, string text);
        void SendReadAllMessageToServer(string roomId);
        Room CreateChat(string name, IList<string> members, ChatKind kind);
        Message ConvertToMessage(DMessage msg);
        (IList<FileAttachment>, int) LoadFiles(string roomId, int offset);
    }
    public class ChatService : IChatService
    {
        private const string DOWNLOAD_URL = "/download";
        IContext _context;
        public ChatService(IContext context)
        {
            _context = context;
        }
        public IList<Room> LoadRooms()
        {
            var chats = _context.RemoteService.ServerApi.GetChats();
            return chats.Select(x => ConvertToRoom(GetRoomId(x.Chat), x.Chat, x.Relations,x.LastMessage)).ToList();
        }

        public IList<Subscription> LoadRoomsSubscriptions()
        {
            var chats = _context.RemoteService.ServerApi.GetChats();
            return chats.Select(x => ConvertToSubscription(x)).ToList();
        }

        public IList<Message> LoadMessages(string roomId, int count, string latest)
        {
            var id = GetRoomId(roomId);
            var dateTo = ConvertFromJSDate(latest);
            return LoadMessages(id, dateTo.AddMilliseconds(-1), count);
        }


        public IList<Message> LoadUnreadMessages(string roomId)
        {
            var id = GetRoomId(roomId);
            var chat = _context.RemoteService.ServerApi.GetChat(id);
            if (chat.UnreadMessagesNumber == 0)
                return new List<Message>();
            return LoadMessages(id, DateTime.MaxValue, chat.UnreadMessagesNumber);
        }

        public Room LoadRoom(Guid id)
        {
            var roomId = GetRoomId(id.ToString());
            var chat = _context.RemoteService.ServerApi.GetChat(roomId);
            return ConvertToRoom(GetRoomId(chat.Chat), chat.Chat, chat.Relations, chat.LastMessage);
        }
        public Subscription LoadRoomsSubscription(string roomId)
        {
            var id = GetRoomId(roomId);
            var chat = _context.RemoteService.ServerApi.GetChat(id);
            return ConvertToSubscription(chat);
        }
        public IList<User> LoadUsers(int count)
        {
            var users = _context.RemoteService.ServerApi.GetPeople().Values;
            return users.Select(x => GetUser(x)).ToList();
        }
        public User LoadUser(int userId)
        {
            INPerson person;
            if (_context.RemoteService.ServerApi.CurrentPerson.Id == userId)
                person = _context.RemoteService.ServerApi.CurrentPerson;
            else
                person = _context.RemoteService.ServerApi.GetPerson(userId); 

            return GetUser(person);
        }

        public Room LoadPersonalRoom(string username)
        {
            var person = _context.RemoteService.ServerApi.GetPerson(username);
            var chat = _context.RemoteService.ServerApi.GetPersonalChat(person.Id);
            return chat.Chat.Id == Guid.Empty ? null : ConvertToRoom(GetRoomId(chat.Chat), chat.Chat, chat.Relations, chat.LastMessage);
        }
        public IList<User> LoadMembers(string roomId)
        {
            var id = GetRoomId(roomId);
            var members = _context.RemoteService.ServerApi.GetChatMembers(id);
            return members.Select(x =>
            {
                var person = _context.RemoteService.ServerApi.GetPerson(x.PersonId);
                return GetUser(person);
            }).ToList();
        }
        public string GetRoomId(DChat chat)
        {
            return chat.Type == ChatKind.Personal ? GetPersonalChatTarget(chat).id : chat.Id.ToString();
        }
        public Room CreateChat(string name, IList<string> members, ChatKind kind)
        {
            var chat = new DChat
            {
                Id = Guid.NewGuid(),
                Name = name,
                Type = kind,
                CreatorId = _context.RemoteService.ServerApi.CurrentPerson.Id,
                CreationDateUtc = DateTime.UtcNow
            };

            var msg = CreateMessage(chat.Id, MessageType.ChatCreation);
            chat.LastMessageId = msg.Id;

            SetMessageData(msg, chat);
            _context.RemoteService.ServerApi.SendMessage(msg);
            foreach (var member in members)
                SendChatsMemberMessageToServer(chat.Id, member);

            _context.WebSocketsSession.NotifyMessageCreatedAsync(msg, NotifyClientKind.Chat);

            string roomId = kind == ChatKind.Personal ? _context.RemoteService.ServerApi.GetPerson(members.First()).Id.ToString() : chat.Id.ToString();
            return ConvertToRoom(roomId, chat, new List<DChatRelation>(), msg);
        }
        public void SendReadAllMessageToServer(string roomId)
        {
            var id = GetRoomId(roomId);
            var chat = _context.RemoteService.ServerApi.GetChat(id);
            if (chat.UnreadMessagesNumber == 0)
                return;
            var unreads = _context.RemoteService.ServerApi.GetMessages(id, DateTime.MaxValue, chat.UnreadMessagesNumber);
            foreach (var unread in unreads)
            {
                var msg = CreateMessage(id, MessageType.MessageRead, unread.Id);
                _context.RemoteService.ServerApi.SendMessage(msg);
            }
        }
        public Message ConvertToMessage(DMessage msg)
        {
            var chat = _context.RemoteService.ServerApi.GetChat(msg.ChatId);
            var attachId = GetMsgAttachmentId(chat.Relations, msg.Id);
            return ConvertToMessage(msg, chat.Chat, attachId);
        }
        public void SendTextMessageToServer(string roomId, string rcMsgId, string text)
        {
            var chatId = GetRoomId(roomId);
            var dMessage = CreateMessage(chatId, MessageType.TextMessage);
            var data = new DTextMessageData { Text = text, ThirdPartyInfo = rcMsgId };

            SetMessageData(dMessage, data);
            _context.RemoteService.ServerApi.SendMessage(dMessage);
            _context.WebSocketsSession.NotifyMessageCreatedAsync(dMessage, NotifyClientKind.Chat);
        }

        public void SendAttachmentMessageToServer(string roomId, string fileName, byte[] data, string text)
        {
            var objId = _context.RemoteService.ServerApi.CreateAttachmentObject(fileName, data);
            var chatId = GetRoomId(roomId);
            var dMessage = CreateMessage(chatId, MessageType.TextMessage);
            var msgData = GetAttachmentsMessageData(objId, dMessage.Id, text);

            SetMessageData(dMessage, msgData);
            _context.RemoteService.ServerApi.SendMessage(dMessage);
            _context.WebSocketsSession.NotifyMessageCreatedAsync(dMessage, NotifyClientKind.FullChat);
        }

        public void SendEditMessageToServer(string roomId, string strMsgId, string text)
        {
            var relatedMsgId = GetGuidMsgId(strMsgId);
            if (relatedMsgId == Guid.Empty)
                return;

            var chatId = GetRoomId(roomId);

            var edit = CreateMessage(chatId, MessageType.EditTextMessage, relatedMsgId);
            var data = new DTextMessageData { Text = text };

            var origin = LoadMessage(chatId, relatedMsgId);
            origin.RelatedMessages.Add(edit);

            SetMessageData(edit, data);
            _context.RemoteService.ServerApi.SendMessage(edit);
            _context.WebSocketsSession.NotifyMessageCreatedAsync(origin, NotifyClientKind.FullChat);
        }

        private DMessage LoadMessage(Guid chatId, Guid relatedMsgId)
        {
            var msgs = _context.RemoteService.ServerApi.GetMessages(chatId, DateTime.Now, int.MaxValue);
            return msgs.FirstOrDefault(x => x.Id == relatedMsgId);
        }

        private Guid GetGuidMsgId(string msgId)
        {
            if (IsRocketChatId(msgId))
            {
                var msg = _context.RemoteService.ServerApi.GetMessage(msgId);
                return msg == null ? Guid.Empty : msg.Id;
            }

            return Guid.Parse(msgId);
        }

        private bool IsRocketChatId(string msgId)
        {
            return msgId.Length == 17;
        }

        public IFileInfo LoadFileInfo(Guid objId)
        {
            var obj = _context.RemoteService.ServerApi.GetObject(objId);
            var fileLoader = _context.RemoteService.FileManager.FileLoader;

            var file = obj.ActualFileSnapshot.Files.First();
            return fileLoader.Download(file);
        }

        public (IList<FileAttachment>, int) LoadFiles(string roomId, int offset)
        {
            var id = GetRoomId(roomId);
            var chat = _context.RemoteService.ServerApi.GetChat(id);
            var attachs = GetAttachmentsIds(chat.Relations).Skip(offset);
            var user = GetUser(_context.RemoteService.ServerApi.CurrentPerson);
            return (attachs.Select(x => LoadFileAttachment(x.Value, roomId)).ToList(), attachs.Count());
        }
        public IList<Attachment> LoadAttachments(Guid objId)
        {
            var attach = LoadAttachment(objId);
            return attach == null ? new List<Attachment> { } : new List<Attachment> { attach };
        }

        private DTextMessageData GetAttachmentsMessageData(Guid objId, Guid messageId, string text)
        {
            var relation = new DChatRelation
            {
                Type = ChatRelationType.Attach,
                ObjectId = objId,
                MessageId = messageId,
                IsDeleted = false
            };

            var data = new DTextMessageData { Text = text };
            data.Attachments = new List<DChatRelation>() { relation };
            return data;
        }
        private Attachment LoadAttachment(Guid objId)
        {
            if (objId == Guid.Empty)
                return null;

            var attach = LoadFileInfo(objId);

            if (string.IsNullOrEmpty(attach.FileType) || string.IsNullOrEmpty(attach.Format))
                return null;

            var downloadUrl = MakeDownloadLink(new List<(string, string)> { ("objId", objId.ToString()) });
            var image = Image.Load(attach.Data);
            return new Attachment()
            {
                title = attach.File.Name,
                title_link = downloadUrl,
                image_dimensions = new Dimension { width = image.Width, height = image.Height },
                image_type = attach.FileType,
                image_size = attach.File.Size,
                image_url = downloadUrl,
                type = "file",
            };
        }
        private FileAttachment LoadFileAttachment(Guid objId, string roomId)
        {
            if (objId == Guid.Empty)
                return null;

            var attach = LoadFileInfo(objId);
            var creator = LoadUser(attach.File.CreatorId);
            using (var ms = new MemoryStream(attach.Data))
            {
                var image = Image.Load(attach.Data);
                var downloadUrl = MakeDownloadLink(new List<(string, string)> { ("objId", objId.ToString()) });

                return new FileAttachment
                {
                    name = attach.File.Name,
                    type = attach.FileType,
                    id = attach.File.Id.ToString(),
                    size = attach.File.Size,
                    roomId = roomId,
                    userId = creator.id,
                    identify = new FileIdentity
                    {
                        format = attach.Format,
                        size = new Dimension { width = image.Width, height = image.Height },
                    },
                    url = downloadUrl,
                    typeGroup = "image",
                    user = creator,
                    uploadedAt = ConvertToJSDate(attach.File.Created)
                };
            }
        }
        private void SendChatsMemberMessageToServer(Guid roomId, string username)
        {
            var msg = CreateMessage(roomId, MessageType.ChatMembers);
            var person = _context.RemoteService.ServerApi.GetPerson(username);

            var change = new DMemberChange
            {
                PersonId = person.Id,
                IsNotifiable = true,
                IsAdded = true,
                IsViewerOnly = false
            };

            var memberData = new DChatMembersData();
            memberData.Changes.Add(change);

            SetMessageData(msg, memberData);
            _context.RemoteService.ServerApi.SendMessage(msg);
        }
        private IList<Message> LoadMessages(Guid roomId, DateTime dateTo, int count)
        {
            var msgs = _context.RemoteService.ServerApi.GetMessages(roomId, dateTo, count);

            if (msgs == null)
                return new List<Message>();

            var chat = _context.RemoteService.ServerApi.GetChat(roomId);
            var attachs = GetAttachmentsIds(chat.Relations);

            var result = new List<Message>();
            foreach (var msg in msgs)
            {
                if (msg.Type != MessageType.TextMessage)
                    continue;

                    attachs.TryGetValue(msg.Id, out var objId);
                result.Add(ConvertToMessage(msg, chat.Chat, objId));
            }

            return result;
        }

        private static Dictionary<Guid, Guid> GetAttachmentsIds(IList<DChatRelation> chatRelations)
        {
            var attachs = new Dictionary<Guid, Guid>();
            foreach (var rel in chatRelations.Where(x => x.Type == ChatRelationType.Attach && x.MessageId.HasValue))
                attachs[rel.MessageId.Value] = rel.ObjectId;
            return attachs;
        }
        private static Guid GetMsgAttachmentId(IList<DChatRelation> chatRelations, Guid msgId)
        {
            var attachs = GetAttachmentsIds(chatRelations);
            return attachs.Where(x => x.Key == msgId).FirstOrDefault().Value;
        }

        private Message ConvertToMessage(DMessage msg, DChat chat, Guid objId)
        {
            var user = LoadUser(msg.CreatorId);
            var roomId = GetRoomId(chat);
            var editedAt = GetEditedAt(msg);
            return new Message()
            {
                id = GetMessageId(msg),
                roomId = roomId,
                updatedAt = ConvertToJSDate(msg.LocalDate),
                creationDate = ConvertToJSDate(msg.LocalDate),
                msg = GetMessageText(msg),
                u = user,
                attachments = LoadAttachments(objId),
                editedAt = editedAt,
                editedBy = GetEditor(msg)
            };
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

        private string GetEditedAt(DMessage msg)
        {
            var edit = GetEditMessage(msg);
            if (edit == null)
                return string.Empty;

            return ConvertToJSDate(edit.LocalDate);
        }
        private User GetEditor(DMessage msg)
        {
            var edit = GetEditMessage(msg);
            if (edit == null)
                return null;

            return LoadUser(edit.CreatorId);
        }

        private DMessage GetEditMessage(DMessage msg)
        {
            return msg.RelatedMessages.Where(x => x.Type == MessageType.EditTextMessage).LastOrDefault();
        }

        private string GetMessageId(DMessage msg)
        {
            var msgData = GetMessageData<DTextMessageData>(msg);
            return string.IsNullOrEmpty(msgData.ThirdPartyInfo) ? msg.Id.ToString() : msgData.ThirdPartyInfo;
        }

        private static string MakeDownloadLink(IList<(string, string)> @params)
        {
            var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            foreach (var p in @params)
                queryString.Add(p.Item1, p.Item2);

            return $"{DOWNLOAD_URL}/{@params.First().Item2}";
        }

        private DMessage CreateMessage(Guid chatId, MessageType type, Guid? relatedMessageId = null)
        {
            return new DMessage()
            {
                Id = Guid.NewGuid(),
                CreatorId = _context.RemoteService.ServerApi.CurrentPerson.Id,
                ChatId = chatId,
                LocalDate = DateTime.Now.ToUniversalTime(),
                Type = type,
                RelatedMessageId = relatedMessageId
            };
        }

        private User GetUser(INPerson person)
        {
            return new User()
            {
                id = person.Id.ToString(),
                username = person.Login,
                name = person.DisplayName,
                status = GetUserStatus(person.Id),
                roles = new string[] { "user" }
            };
        }

        private string GetUserStatus(int person)
        {
            if (_context.RemoteService.ServerApi.IsOnline(person))
                return nameof(UserStatuses.online);
            return nameof(UserStatuses.offline);
        }

        private static void SetMessageData<T>(DMessage message, T data)
        {
            using (var stream = new MemoryStream())
            {
                ProtoSerializer.Serialize(stream, data);
                stream.Position = 0;
                message.Data = stream.ToArray();
            }
        }
        public static T GetMessageData<T>(DMessage msg)
        {
            using (var stream = new MemoryStream(msg.Data))
            {
                return ProtoSerializer.Deserialize<T>(stream);
            }
        }

        private Subscription ConvertToSubscription(DChatInfo chat)
        {
            return new Subscription()
            {
                updatedAt = ConvertToJSDate(chat.LastMessage.LocalDate),
                lastSeen = LoadLastSeenChatsDate(chat),
                unread = chat.UnreadMessagesNumber,
                open = true,
                name = chat.Chat.Type == ChatKind.Personal ? GetPersonalChatTarget(chat.Chat).username : chat.Chat.Name,
                displayName = chat.Chat.Type == ChatKind.Personal ? GetPersonalChatTarget(chat.Chat).name : chat.Chat.Name,
                alert = chat.UnreadMessagesNumber > 0,
                id = GetRoomId(chat.Chat),
                roomId = GetRoomId(chat.Chat),
                channelType = GetChannelType(chat.Chat)
            };
        }

        private User GetPersonalChatTarget(DChat chat)
        {
            var members = _context.RemoteService.ServerApi.GetChatMembers(chat.Id);
            var currentPersonId = _context.RemoteService.ServerApi.CurrentPerson.Id;
            var target = members.First(x => x.PersonId != currentPersonId);
            var person = _context.RemoteService.ServerApi.GetPerson(target.PersonId);
            return GetUser(person);
        }

        private string GetChannelType(DChat chat)
        {
            return chat.Type == ChatKind.Personal ? "d" : "p";
        }
        private string LoadLastSeenChatsDate(DChatInfo chat)
        {
            if (chat.UnreadMessagesNumber == 0)
                return ConvertToJSDate(chat.LastMessage.LocalDate);

            var unread = _context.RemoteService.ServerApi.GetMessages(chat.Chat.Id, DateTime.MaxValue, chat.UnreadMessagesNumber);
            var earliestUnreadMessage = unread.LastOrDefault(x => x.Type == MessageType.TextMessage);

            if (earliestUnreadMessage == null)
                return ConvertToJSDate(chat.LastMessage.LocalDate);

            return ConvertToJSDate(earliestUnreadMessage.LocalDate);
        }
        private Room ConvertToRoom(string roomId, DChat chat, IList<DChatRelation> chatRelations, DMessage lastMessage)
        {
            var attachId = GetMsgAttachmentId(chatRelations, lastMessage.Id);
            return new Room()
            {
                updatedAt = ConvertToJSDate(lastMessage.LocalDate),
                name = chat.Type == ChatKind.Personal ? string.Empty : chat.Name,
                id = roomId,
                channelType = GetChannelType(chat),
                creationDate = ConvertToJSDate(chat.CreationDateUtc),
                lastMessage = lastMessage.Type == MessageType.TextMessage ? ConvertToMessage(lastMessage, chat, attachId) : null,
                usernames = GetUserNames(chat)
            };
        }

        private string[] GetUserNames(DChat chat)
        {
            var members = _context.RemoteService.ServerApi.GetChatMembers(chat.Id);
            return members.Select(x => _context.RemoteService.ServerApi.GetPerson(x.PersonId).Login).ToArray();
        }

        private Guid GetRoomId(string strId)
        {
            if (Guid.TryParse(strId, out var id))
                return id;

            var personId = int.Parse(strId);
            var chat = _context.RemoteService.ServerApi.GetPersonalChat(personId);
            return chat.Chat.Id;
        }

        private static string ConvertToJSDate(DateTime date)
        {
            return date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
        private DateTime ConvertFromJSDate(string date)
        {
            return string.IsNullOrEmpty(date) ? DateTime.MaxValue : DateTime.Parse(date).ToUniversalTime();
        }

        public void Dispose()
        {
        }
    }
}
