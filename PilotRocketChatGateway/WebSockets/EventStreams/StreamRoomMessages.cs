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
        private readonly string _id;

        public StreamRoomMessages(WebSocket webSocket, IChatService chatService)
        {
            _webSocket = webSocket;
            _chatService = chatService;
        }
        public string StreamName => Streams.STREAM_ROOM_MESSAGES;

        public async Task SendMessageUpdate(DMessage message)
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

            await _webSocket.SendResultAsync(result);
        }
    }
}
