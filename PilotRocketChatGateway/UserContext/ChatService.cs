using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.PilotServer;

namespace PilotRocketChatGateway.UserContext
{
    public interface IChatService
    {
        Rooms LoadRooms();
        Subscriptions LoadRoomsSubscriptions();
    }
    public class ChatService : IChatService
    {
        IServerApiService _serverApi;
        public ChatService(IServerApiService serverApi)
        {
            _serverApi = serverApi;
        }
        public Rooms LoadRooms()
        {
            var chats = _serverApi.GetChats();
            var subs = new Rooms() { success = true, update = new List<Room>(), remove = new List<Room>() };
            foreach (var chat in chats)
            {
                subs.update.Add(ConvertToRoom(chat));
            }
            return subs;
        }

        public Subscriptions LoadRoomsSubscriptions()
        {
            var chats = _serverApi.GetChats();
            var subs = new Subscriptions() { success = true, update = new List<Subscription>(), remove = new List<Subscription>() };
            foreach (var chat in chats)
            {
                subs.update.Add(ConvertToSubscription(chat));
            }
            return subs;
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
                creationDate = ConvertToJSDate(chat.Chat.CreationDateUtc),
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

        //public static string ConvertToJSDate2(DateTime date)
        //{
        //    return date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        //}
        private static JSDate ConvertToJSDate(DateTime date)
        {
            var jsDate = new JSDate()
            {
                date = ToJavaScriptMilliseconds(date)
            };
            return jsDate;
        }
        private static readonly long DatetimeMinTimeTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        public static long ToJavaScriptMilliseconds(DateTime dt)
        {
            return (dt.ToUniversalTime().Ticks - DatetimeMinTimeTicks) / 10000;
        }



        public Messages LoadMessages(Guid roomId, int count)
        {
            var msgs = _serverApi.GetMessages(roomId, count);

            var result = new Messages()
            {
                success = true,
                messages = new List<Message>()
            };

            foreach (var msg in msgs.Where(x => x.Type == MessageType.TextMessage))
            {
                result.messages.Add(GetMessage(msg));

            }
            return result;
        }
    }
}
