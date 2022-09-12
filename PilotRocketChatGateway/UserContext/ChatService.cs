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
        IList<Message> LoadMessages(Guid roomId, int count);
        IList<Message> LoadUnreadMessages(Guid roomId);
        User LoadUser(int usderId);
        IList<User> LoadUsers(int count);
        Message SendMessageToServer(MessageType type, Guid chatId, string text);
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
            return chats.Select(x => ConvertToRoom(x)).ToList();
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
            return ConvertToRoom(chat);
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

        public Message SendMessageToServer(MessageType type, Guid chatId, string text)
        {
            switch (type)
            {
                case MessageType.TextMessage:
                    return SendTextMessageToServer(chatId, text);
                case MessageType.MessageRead:
                    return SendReadAllMessageToServer(chatId, text);
                default:
                    throw new Exception($"unknow message type: {type}");
            }
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

        private Message SendReadAllMessageToServer(Guid chatId, string text)
        {
            var chat = _context.RemoteService.ServerApi.GetChat(chatId);
            if (chat.UnreadMessagesNumber == 0)
                return null;
            var unreads = _context.RemoteService.ServerApi.GetMessages(chatId, chat.UnreadMessagesNumber);
            foreach (var unread in unreads)
                _context.RemoteService.ServerApi.SendMessage(new DMessage()
                {
                    Id = Guid.NewGuid(),
                    CreatorId = _context.RemoteService.ServerApi.CurrentPerson.Id,
                    ChatId = chatId,
                    LocalDate = DateTime.Now.ToUniversalTime(),
                    Type = MessageType.MessageRead,
                    RelatedMessageId = unread.Id
                });
            return null;
        }

        private Message SendTextMessageToServer(Guid chatId, string text)
        {
            var id = Guid.NewGuid();
            var date = DateTime.Now.ToUniversalTime();
            var dMessage = new DMessage()
            {
                Id = id,
                CreatorId = _context.RemoteService.ServerApi.CurrentPerson.Id,
                ChatId = chatId,
                LocalDate = date,
                Type = MessageType.TextMessage,
                Data = GetMessageData(text)
            };

            _context.RemoteService.ServerApi.SendMessage(dMessage);
            NotifyMessageCreated(dMessage);
            return ConvertToMessage(dMessage);
        }

        private void NotifyMessageCreated(DMessage dMessage)
        {
            _context.WebSocketsSession.NotifyMessageCreatedAsync(dMessage);
        }

        private byte[] GetMessageData(string text)
        {
            using (var stream = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(stream, text);
                return stream.ToArray();
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
                name = chat.Chat.Name,
                alert = chat.UnreadMessagesNumber > 0,
                id = chat.Chat.Id.ToString(),
                roomId = chat.Chat.Id.ToString(),
                channelType = "p",
            };
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
        private Room ConvertToRoom(DChatInfo chat)
        {
            return new Room()
            {
                updatedAt = ConvertToJSDate(chat.LastMessage.LocalDate),
                name = chat.Chat.Name,
                id = chat.Chat.Id.ToString(),
                channelType = "p",
                creationDate = ConvertToJSDate(chat.Chat.CreationDateUtc),
                lastMessage = chat.LastMessage.Type == MessageType.TextMessage ? ConvertToMessage(chat.LastMessage) : null
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
