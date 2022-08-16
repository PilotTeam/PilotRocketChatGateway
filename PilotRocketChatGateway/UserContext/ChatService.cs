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

        private static Subscription ConvertToSubscription(DChatInfo chat)
        {
            return new Subscription()
            {
                updatedAt = ConvertToJSDate(chat.LastMessage.LocalDate),
                unread = chat.UnreadMessagesNumber,
                open = true,
                name = chat.Chat.Name,
                id = chat.Chat.Id.ToString(),
                roomId = chat.Chat.Id.ToString(),
                channelType = "p",
            };
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

        private Message ConvertToMessage(DMessage msg)
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

        public static string ConvertToJSDate(DateTime date)
        {
            return date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        public void Dispose()
        {
        }
    }
}
