using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.WebSockets.EventStreams;
using System.Net.WebSockets;

namespace PilotRocketChatGateway.WebSockets.Subscriptions
{
    public class StreamRoomMessages : EventStream
    {
        private readonly IChatService _chatService;
        private readonly WebSocket _webSocket;

        public StreamRoomMessages(WebSocket webSocket, IChatService chatService)
        {
            _webSocket = webSocket;
            _chatService = chatService;
        }

        public void SendMessageUpdate(DMessage message)
        {
            var rocketChatMessage = _chatService.DataLoader.RCDataConverter.ConvertToMessage(message);

            var eventName = rocketChatMessage.roomId;
            _events.TryGetValue(eventName, out var id);
            if (string.IsNullOrEmpty(id))
                return;

            var result = new
            {
                msg = "created",
                collection = Streams.STREAM_ROOM_MESSAGES,
                id = id,
                fields = new
                {
                    eventName = eventName,
                    args = new object[] { rocketChatMessage }
                }
            };

            _webSocket.SendResultAsync(result);
        }
    }
}
