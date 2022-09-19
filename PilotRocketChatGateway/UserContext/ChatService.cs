using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.PilotServer;

namespace PilotRocketChatGateway.UserContext
{
    public interface IChatService : IService
    {
        Room LoadRoom(string id);
        IList<Room> LoadRooms();
        Subscription LoadRoomsSubscription(string roomId);
        IList<Subscription> LoadRoomsSubscriptions();
        Room LoadPersonalRoom(string username);
        IList<Message> LoadMessages(string roomId, int count, string latest);
        IList<Message> LoadUnreadMessages(string roomId);
        User LoadUser(int usderId);
        IList<User> LoadUsers(int count);
        IList<User> LoadMembers(string roomId);
        Message SendTextMessageToServer(string roomId, string text);
        void SendReadAllMessageToServer(string roomId);
        Room CreateChat(string name, IList<string> members, ChatKind kind);
        Message ConvertToMessage(DMessage msg);
    }
    public class ChatService : IChatService
    {
        IContext _context;
        public ChatService(IContext context)
        {
            _context = context;
        }
        public IList<Room> LoadRooms()
        {
            var chats = _context.RemoteService.ServerApi.GetChats();
            return chats.Select(x => ConvertToRoom(x.Chat, x.LastMessage)).ToList();
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

        public Room LoadRoom(string id)
        {
            var roomId = GetRoomId(id);
            var chat = _context.RemoteService.ServerApi.GetChat(roomId);
            return ConvertToRoom(chat.Chat, chat.LastMessage);
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
            return chat.Chat.Id == Guid.Empty ? null : ConvertToRoom(chat.Chat, chat.LastMessage);
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

            NotifyMessageCreated(msg);
            return ConvertToRoom(chat, msg);
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
            return ConvertToMessage(msg, chat.Chat);
        }

        public Message SendTextMessageToServer(string roomId, string text)
        {
            var id = GetRoomId(roomId);
            var dMessage = CreateMessage(id, MessageType.TextMessage);
            SetMessageData(dMessage, text);
            _context.RemoteService.ServerApi.SendMessage(dMessage);
            NotifyMessageCreated(dMessage);
            return ConvertToMessage(dMessage, _context.RemoteService.ServerApi.GetChat(id).Chat);
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
            var chat = _context.RemoteService.ServerApi.GetChat(roomId);
            return msgs.Where(x => x.Type == MessageType.TextMessage).Select(x => ConvertToMessage(x, chat.Chat)).ToList();
        }
        private Message ConvertToMessage(DMessage msg, DChat chat)
        {
            var user = LoadUser(msg.CreatorId);

            using (var stream = new MemoryStream(msg.Data))
            {
                return new Message()
                {
                    id = msg.Id.ToString(),
                    roomId = GetRoomId(chat),
                    updatedAt = ConvertToJSDate(msg.LocalDate),
                    creationDate = ConvertToJSDate(msg.LocalDate),
                    msg = ProtoBuf.Serializer.Deserialize<string>(stream),
                    u = user
                };
            }
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
                status = "online",
                roles = new string[] { "user" }
            };
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

        private void NotifyMessageCreated(DMessage dMessage)
        {
            _context.WebSocketsSession.NotifyMessageCreatedAsync(dMessage);
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
            return chat.Type == ChatKind.Personal ? "d" : "g";
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
        private Room ConvertToRoom(DChat chat, DMessage lastMessage)
        {
            return new Room()
            {
                updatedAt = ConvertToJSDate(lastMessage.LocalDate),
                name = chat.Type == ChatKind.Personal ? string.Empty : chat.Name,
                id = GetRoomId(chat),
                channelType = GetChannelType(chat),
                creationDate = ConvertToJSDate(chat.CreationDateUtc),
                lastMessage = lastMessage.Type == MessageType.TextMessage ? ConvertToMessage(lastMessage, chat) : null,
                usernames = GetUserNames(chat)
            };
        }

        private string[] GetUserNames(DChat chat)
        {
            var members = _context.RemoteService.ServerApi.GetChatMembers(chat.Id);
            return members.Select(x => _context.RemoteService.ServerApi.GetPerson(x.PersonId).Login).ToArray();
        }

        private string GetRoomId(DChat chat)
        {
            return chat.Type == ChatKind.Personal ? GetPersonalChatTarget(chat).id : chat.Id.ToString();
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
