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
        public string StreamName => Streams.STREAM_NOTIFY_USER;

        public Task UpdateRoomsSubscriptionAsync(Guid chatId)
        {
            var(eventName, id) = _events.Where(x => x.Key.Contains(Events.EVENT_SUBSCRIPTIONS_CHANGED)).FirstOrDefault();
            if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(id))
                return Task.CompletedTask;

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
            return _webSocket.SendResultAsync(result);
        }
        public Task UpdateRoomAsync(Guid chatId)
        {
            var (eventName, id) = _events.Where(x => x.Key.Contains(Events.EVENT_ROOMS_CHANGED)).FirstOrDefault();
            if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(id))
                return Task.CompletedTask;

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
            return _webSocket.SendResultAsync(result);
        }

    }
}
