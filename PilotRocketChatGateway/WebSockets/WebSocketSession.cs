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
        public const string STREAM_ROOM_MESSAGES = "stream-room-messages";
    }
    public interface IWebSocketSession : IDisposable
    {
        Task SendMessageToClientAsync(DMessage dMessage);
        Task NotifyMessageCreatedAsync(DMessage dMessage);
        void Subscribe(dynamic request);
        void Unsubscribe(dynamic request);
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
        public class R
        {
            public string msg { get; init; }
            public string[] subs { get; init; }
        }

        public void Subscribe(dynamic request)
        {
            _subscriptions[request.@params[0]] = request.id;
            var result = new 
            {
                msg = "ready",
                subs = new string[] { request.id.ToString() }
            };
            _webSocket.SendResultAsync(result);
        }
        public void Unsubscribe(dynamic request)
        {
            var sub = _subscriptions.FirstOrDefault(x => x.Value == request.id).Key;
            _subscriptions.Remove(sub);
        }

        public async Task SendMessageToClientAsync(DMessage dMessage)
        {
            if (dMessage.Type != MessageType.TextMessage)
                return;
            await NotifyMessageCreatedAsync(dMessage);
            await SendMessageUpdate(dMessage);
        }

        public async Task NotifyMessageCreatedAsync(DMessage dMessage)
        {
            await UpdateRoomsSubscription(dMessage);
            await UpdateRoom(dMessage);
        }

        private async Task SendMessageUpdate(DMessage message)
        {
            var eventName = $"{message.ChatId}";
            var rocketChatMessage = _chatService.ConvertToMessage(message);
            if (!_subscriptions.TryGetValue(eventName, out var id))
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

        private async Task UpdateRoomsSubscription(DMessage dMessage)
        {
            var eventName = $"{_sessionId}/subscriptions-changed";
            var sub = _chatService.LoadRoomsSubscription(dMessage.ChatId);
            if (!_subscriptions.TryGetValue(eventName, out var id))
                return;

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
            if (!_subscriptions.TryGetValue(eventName, out var id))
                return;

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
