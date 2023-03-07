using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api.Contracts;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.WebSockets.EventStreams;
using System.Net.WebSockets;

namespace PilotRocketChatGateway.WebSockets.Subscriptions
{
    public class StreamNotifyRoom : EventStream
    {
        private readonly WebSocket _webSocket;
        private readonly IChatService _chatService;

        public StreamNotifyRoom(WebSocket webSocket, IChatService chatService)
        {
            _webSocket = webSocket;
            _chatService = chatService;
        }
        public void SendTypingMessageToClient(string roomId, int personId, bool isTyping)
        {
            var eventName = $"{roomId}/typing";
            _events.TryGetValue(eventName, out var id);
            if (string.IsNullOrEmpty(id))
                return;

            var person = _chatService.DataLoader.LoadPerson(personId);
            var result = new
            {
                msg = "",
                collection = Streams.STREAM_NOTIFY_ROOM,
                id = id,
                fields = new
                {
                    eventName = eventName,
                    args = new object[]
                    {
                        person.DisplayName,
                        isTyping
                    }
                }
            };
            _webSocket.SendResultAsync(result);
        }
    }
}
