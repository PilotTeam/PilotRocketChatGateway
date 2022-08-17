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
        void SendMessageToServer(MessageType type, Guid chatId, string text);
        Message ConvertToMessage(DMessage msg);
    }
    public class ChatService : IChatService
    {
        IServerApiService _serverApi;
        public ChatService(IServerApiService serverApi)
        {
            _serverApi = serverApi;
        }
        public IList<Room> LoadRooms()
        {
            var chats = _serverApi.GetChats();
            return chats.Select(x => ConvertToRoom(x)).ToList();
        }

        public IList<Subscription> LoadRoomsSubscriptions()
        {
            var chats = _serverApi.GetChats();
            return chats.Select(x => ConvertToSubscription(x)).ToList();
        }

        public IList<Message> LoadMessages(Guid roomId, int count)
        {
            var msgs = _serverApi.GetMessages(roomId, count);
            return msgs.Where(x => x.Type == MessageType.TextMessage).Select(x => ConvertToMessage(x)).ToList();
        }

        public Room LoadRoom(Guid id)
        {
            var chat = _serverApi.GetChat(id);
            return ConvertToRoom(chat);
        }
        public Subscription LoadRoomsSubscription(Guid id)
        {
            var chat = _serverApi.GetChat(id);
            return ConvertToSubscription(chat);
        }

        public Message ConvertToMessage(DMessage msg)
        {
            var user = _serverApi.GetPerson(msg.CreatorId);
            using (var stream = new MemoryStream(msg.Data))
            {
                return new Message()
                {
                    id = msg.Id.ToString(),
                    roomId = msg.ChatId.ToString(),
                    updatedAt = ConvertToJSDate(msg.LocalDate),
                    creationDate = ConvertToJSDate(msg.LocalDate),
                    msg = ProtoBuf.Serializer.Deserialize<string>(stream),
                    u = new User()
                    {
                        id = user.Id.ToString(),
                        username = user.Login,
                        name = user.DisplayName
                    }
                };
            }
        }


        public void SendMessageToServer(MessageType type, Guid chatId, string text)
        {
            switch (type)
            {
                case MessageType.TextMessage:
                    SendTextMessageToServer(chatId, text);
                    break;
                case MessageType.MessageRead:
                    SendReadAllMessageToServer(chatId, text);
                    break;

            }
        }

        private void SendReadAllMessageToServer(Guid chatId, string text)
        {
            var chat = _serverApi.GetChat(chatId);
            var unreads = _serverApi.GetMessages(chatId, chat.UnreadMessagesNumber);
            foreach (var unread in unreads)
                _serverApi.SendMessage(new DMessage()
                {
                    Id = Guid.NewGuid(),
                    CreatorId = _serverApi.CurrentPerson.Id,
                    ChatId = chatId,
                    LocalDate = DateTime.Now.ToUniversalTime(),
                    Type = MessageType.MessageRead,
                    RelatedMessageId = unread.Id
                });
        }

        private void SendTextMessageToServer(Guid chatId, string text)
        {
            var dMessage = new DMessage()
            {
                Id = Guid.NewGuid(),
                CreatorId = _serverApi.CurrentPerson.Id,
                ChatId = chatId,
                LocalDate = DateTime.Now.ToUniversalTime(),
                Type = MessageType.TextMessage,
                Data = GetMessageData(text)
            };
            _serverApi.SendMessage(dMessage);
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

            var lastUnreadMessage = _serverApi.GetLastUnreadMessage(chat.Chat.Id);
            return ConvertToJSDate(lastUnreadMessage.LocalDate);
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
                lastMessage = ConvertToMessage(chat.LastMessage)
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
