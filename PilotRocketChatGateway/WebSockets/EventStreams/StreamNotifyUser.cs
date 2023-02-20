using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.WebSockets;
using PilotRocketChatGateway.WebSockets.EventStreams;
using System.Net.WebSockets;

namespace PilotRocketChatGateway.WebSockets.Subscriptions
{
    public class StreamNotifyUser : EventStream
    {
        private readonly IChatService _chatService;
        private readonly WebSocket _webSocket;

        public StreamNotifyUser(WebSocket webSocket, IChatService chatService)
        {
            _webSocket = webSocket;
            _chatService = chatService;
        }
        public void UpdateRoomsSubscription(Guid chatId)
        {
            var(eventName, id) = _events.Where(x => x.Key.Contains(Events.EVENT_SUBSCRIPTIONS_CHANGED)).FirstOrDefault();
            if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(id))
                return;

            var sub = _chatService.DataLoader.LoadRoomsSubscription(chatId.ToString());
            var result = new
            {
                msg = "updated",
                collection = Streams.STREAM_NOTIFY_USER,
                id,
                fields = new
                {
                    eventName,
                    args = new object[] { "updated", sub }
                }
            };
             _webSocket.SendResultAsync(result);
        }
        public void UpdateRoom(Guid chatId)
        {
            var (eventName, id) = _events.Where(x => x.Key.Contains(Events.EVENT_ROOMS_CHANGED)).FirstOrDefault();
            if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(id))
                return;

            var room = _chatService.DataLoader.LoadRoom(chatId);
            var result = new
            {
                msg = "updated",
                collection = Streams.STREAM_NOTIFY_USER,
                id,
                fields = new
                {
                    eventName,
                    args = new object[] { "updated", room }
                }
            };
            _webSocket.SendResultAsync(result);
        }

    }
}
