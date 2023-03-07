using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api.Contracts;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.WebSockets.EventStreams;
using System.Net.WebSockets;

namespace PilotRocketChatGateway.WebSockets.Subscriptions
{
    public class StreamUserPresence : EventStream
    {
        private readonly IChatService _chatService;
        private readonly WebSocket _webSocket;

        public StreamUserPresence(WebSocket webSocket, IChatService chatService)
        {
            _chatService = chatService;
            _webSocket = webSocket;
        }
        public void SendUserStatusChange(int personId, UserStatuses status)
        {
            var (_, id) = _events.FirstOrDefault();

            if (string.IsNullOrEmpty(id))
                return;

            var person = _chatService.DataLoader.LoadPerson(personId);
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
            _webSocket.SendResultAsync(result);
        }
    }
}
