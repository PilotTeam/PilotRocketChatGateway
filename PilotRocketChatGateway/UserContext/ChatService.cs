using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.PilotServer;

namespace PilotRocketChatGateway.UserContext
{
    public interface IChatService : IService
    {
        Room LoadRoom(Guid id);
        IList<Room> LoadRooms();
        Subscription LoadRoomsSubscription(Guid id);
        IList<Subscription> LoadRoomsSubscriptions();
        Room LoadPersonalRoom(string username);
        IList<Message> LoadMessages(Guid roomId, int count);
        IList<Message> LoadUnreadMessages(Guid roomId);
        User LoadUser(int usderId);
        IList<User> LoadUsers(int count);
        Message SendTextMessageToServer(Guid chatId, string text);
        void SendReadAllMessageToServer(Guid chatId);
        public void SendChatsMemberMessageToServer(Guid chatId, string username);
        Room CreateChat(IList<string> members, ChatKind kind);
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

        public IList<Message> LoadMessages(Guid roomId, int count)
        {
            var msgs = _context.RemoteService.ServerApi.GetMessages(roomId, count);
            return msgs.Where(x => x.Type == MessageType.TextMessage).Select(x => ConvertToMessage(x)).ToList();
        }
        public IList<Message> LoadUnreadMessages(Guid roomId)
        {
            var chat = _context.RemoteService.ServerApi.GetChat(roomId);
            if (chat.UnreadMessagesNumber == 0)
                return new List<Message>();
            return LoadMessages(roomId, chat.UnreadMessagesNumber);
        }

        public Room LoadRoom(Guid id)
        {
            var chat = _context.RemoteService.ServerApi.GetChat(id);
            return ConvertToRoom(chat.Chat, chat.LastMessage);
        }
        public Subscription LoadRoomsSubscription(Guid id)
        {
            var chat = _context.RemoteService.ServerApi.GetChat(id);
            return ConvertToSubscription(chat);
        }

        public Message ConvertToMessage(DMessage msg)
        {
            var user = LoadUser(msg.CreatorId);

            using (var stream = new MemoryStream(msg.Data))
            {
                return new Message()
                {
                    id = msg.Id.ToString(),
                    roomId = msg.ChatId.ToString(),
                    updatedAt = ConvertToJSDate(msg.LocalDate),
                    creationDate = ConvertToJSDate(msg.LocalDate),
                    msg = ProtoBuf.Serializer.Deserialize<string>(stream),
                    u = user
                };
            }
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

        public Room CreateChat(IList<string> members, ChatKind kind)
        {
            var chat = new DChat
            {
                Id = Guid.NewGuid(),
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
        public void SendReadAllMessageToServer(Guid chatId)
        {
            var chat = _context.RemoteService.ServerApi.GetChat(chatId);
            if (chat.UnreadMessagesNumber == 0)
                return;
            var unreads = _context.RemoteService.ServerApi.GetMessages(chatId, chat.UnreadMessagesNumber);
            foreach (var unread in unreads)
            {
                var msg = CreateMessage(chatId, MessageType.MessageRead, unread.Id);
                _context.RemoteService.ServerApi.SendMessage(msg);
            }
        }

        public Message SendTextMessageToServer(Guid chatId, string text)
        {
            var date = DateTime.Now.ToUniversalTime();
            var dMessage = CreateMessage(chatId, MessageType.TextMessage);
            SetMessageData(dMessage, text);
            _context.RemoteService.ServerApi.SendMessage(dMessage);
            NotifyMessageCreated(dMessage);
            return ConvertToMessage(dMessage);
        }

        public void SendChatsMemberMessageToServer(Guid chatId, string username)
        {
            var msg = CreateMessage(chatId, MessageType.ChatMembers);
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
                name = chat.Chat.Type == ChatKind.Personal ? GetPersonalChatTarget(chat.Chat).name : chat.Chat.Name,
                alert = chat.UnreadMessagesNumber > 0,
                id = chat.Chat.Id.ToString(),
                roomId = chat.Chat.Id.ToString(),
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

            var unread = _context.RemoteService.ServerApi.GetMessages(chat.Chat.Id, chat.UnreadMessagesNumber);
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
                id = chat.Id.ToString(),
                channelType = GetChannelType(chat),
                creationDate = ConvertToJSDate(chat.CreationDateUtc),
                lastMessage = lastMessage.Type == MessageType.TextMessage ? ConvertToMessage(lastMessage) : null,
            };
        }


        private static string ConvertToJSDate(DateTime date)
        {
            return date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        public void Dispose()
        {
        }
    }
}
