using Ascon.Pilot.Server.Api.Contracts;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.WebSockets.EventStreams;
using System.Net.WebSockets;

namespace PilotRocketChatGateway.WebSockets.Subscriptions
{
    public class StreamUserPresence : EventStream
    {
        private readonly IServerApiService _serverApi;
        private readonly WebSocket _webSocket;

        public StreamUserPresence(WebSocket webSocket, IServerApiService serverApi)
        {
            _webSocket = webSocket;
            _serverApi = serverApi;
        }
        public string StreamName => Streams.STREAM_USER_PRESENCE;

        public Task SendUserStatusChangeAsync(int personId, UserStatuses status)
        {
            var (_, id) = _events.FirstOrDefault();

            if (string.IsNullOrEmpty(id))
                return Task.CompletedTask;

            var person = _serverApi.GetPerson(personId);
            var result = new
            {
                msg = "updated",
                collection = Streams.STREAM_USER_PRESENCE,
                id,
                fields = new
                {
                    args = new object[]
                    {
                        new object[]
                        {
                            person.Login,
                            (int) status,
                            ""
                        }
                    },
                    uid = personId
                }
            };
            return _webSocket.SendResultAsync(result);
        }
    }
}
