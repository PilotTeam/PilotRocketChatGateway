using Ascon.Pilot.Server.Api.Contracts;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.WebSockets.EventStreams;
using System.Net.WebSockets;

namespace PilotRocketChatGateway.WebSockets.Subscriptions
{
    public class StreamNotifyRoom : EventStream
    {
        private readonly IServerApiService _serverApi;
        private readonly WebSocket _webSocket;

        public StreamNotifyRoom(WebSocket webSocket, IServerApiService serverApi)
        {
            _webSocket = webSocket;
            _serverApi = serverApi;
        }
        public void SendTypingMessageToClient(string roomId, int personId, bool isTyping)
        {
            var eventName = $"{roomId}/typing";
            _events.TryGetValue(eventName, out var id);
            if (string.IsNullOrEmpty(id))
                return;

            var person = _serverApi.GetPerson(personId);
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
