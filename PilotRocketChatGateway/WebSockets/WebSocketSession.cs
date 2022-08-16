using Ascon.Pilot.DataClasses;
using Microsoft.IdentityModel.Tokens;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.UserContext;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;

namespace PilotRocketChatGateway.WebSockets
{
    public class Streams
    {
        public const string STREAM_NOTIFY_USER = "stream-notify-user";
    }
    public interface IWebSocketSession : IDisposable
    {
        Task SendMessageToClientAsync(DMessage dMessage);
        void SubscribeEvent(dynamic request);
    }
    public class WebSocketSession : IWebSocketSession
    {
        private readonly string _sessionId;
        private readonly IChatService _chatService;
        private readonly WebSocket _webSocket;
        private Dictionary<string, string> _subscriptions = new Dictionary<string, string>();

        public WebSocketSession(dynamic request, AuthSettings authSettings, IChatService chatService, WebSocket webSocket)
        {
            var authToken = request.@params[0].resume;
            if (ValidateCurrentToken(authToken, authSettings) == false)
                throw new UnauthorizedAccessException();

            _sessionId = request.id;
            _chatService = chatService;
            _webSocket = webSocket;
        }

        public void SubscribeEvent(dynamic request)
        {
            _subscriptions[request.@params[0]] = request.id;
        }

        public async Task SendMessageToClientAsync(DMessage dMessage)
        {
            await UpdateSubscription(dMessage);
           await UpdateRoom(dMessage);
        }

        private async Task UpdateSubscription(DMessage dMessage)
        {
            var eventName = $"{_sessionId}/subscriptions-changed";
            var sub = _chatService.LoadRoomsSubscription(dMessage.ChatId);
            var id = _subscriptions[eventName];

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
            await _webSocket.SendResultAsync(result);
        }

        private async Task UpdateRoom(DMessage dMessage)
        {
            var eventName = $"{_sessionId}/rooms-changed";
            var room = _chatService.LoadRoom(dMessage.ChatId);
            var id = _subscriptions[eventName];

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
            await _webSocket.SendResultAsync(result);
        }

        private bool ValidateCurrentToken(string token, AuthSettings authSettings)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ValidateToken(token, AuthUtils.GetTokenValidationParameters(authSettings), out SecurityToken validatedToken);
            }
            catch
            {
                return false;
            }
            return true;
        }
        public void Dispose()
        {
            _subscriptions.Clear();
        }
    }
}
