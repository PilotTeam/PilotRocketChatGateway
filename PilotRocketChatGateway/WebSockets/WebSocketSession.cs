using Ascon.Pilot.DataClasses;
using Microsoft.IdentityModel.Tokens;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;

namespace PilotRocketChatGateway.WebSockets
{
    public enum NotifyClientKind
    {
        Room = 0,
        Subscription = 1,
        Message = 2,
        Chat = Room | Subscription,
        FullChat = Room | Subscription | Message
    }
    public enum UserStatuses
    {
        offline,
        online,
        away,
        busy
    }
    public class Streams
    {
        public const string STREAM_NOTIFY_USER = "stream-notify-user";
        public const string STREAM_NOTIFY_ROOM = "stream-notify-room";
        public const string STREAM_ROOM_MESSAGES = "stream-room-messages";
        public const string STREAM_USER_PRESENCE = "stream-user-presence";
    }
    public class Events
    {
        public const string EVENT_ROOMS_CHANGED = "rooms-changed";
        public const string EVENT_SUBSCRIPTIONS_CHANGED = "subscriptions-changed";
    }
    public interface IWebSocketSession : IDisposable
    {
        Task SendMessageToClientAsync(DMessage dMessage);
        Task SendUserStatusChangeAsync(int person, UserStatuses status);
        void SendTypingMessageToClient(DChat chat, int personId);
        Task NotifyMessageCreatedAsync(DMessage dMessage, NotifyClientKind notify);
        void Subscribe(dynamic request);
        void Unsubscribe(dynamic request);
    }

    public class WebSocketSession : IWebSocketSession
    {
        private readonly string _sessionId;
        private readonly IChatService _chatService;
        private readonly IServerApiService _serverApi;
        private readonly WebSocket _webSocket;
        private readonly TypingTimer _typingTimer;
        private Dictionary<string, string> _subscriptions = new Dictionary<string, string>();

        public WebSocketSession(dynamic request, AuthSettings authSettings, IChatService chatService, IServerApiService serverApi, IAuthHelper authHelper, WebSocket webSocket)
        {
            var authToken = request.@params[0].resume;
            if (authHelper.ValidateToken(authToken, authSettings) == false)
                throw new UnauthorizedAccessException();

            _sessionId = request.id;
            _chatService = chatService;
            _serverApi = serverApi;
            _webSocket = webSocket;
            _typingTimer = new TypingTimer
            (
                (chat, personId) => SendTypingMessageToClientAsync(chat, personId, true),
                (chat, personId) => SendTypingMessageToClientAsync(chat, personId, false)
            );
        }

        public void Subscribe(dynamic request)
        {
            _subscriptions[GetSubsName(request)] = request.id;
            var result = new
            {
                msg = "ready",
                subs = new string[] { request.id.ToString() }
            };
            _webSocket.SendResultAsync(result);
        }

        private string GetSubsName(dynamic request)
        {
            return string.IsNullOrEmpty(request.@params[0]) ? request.name : request.@params[0];
        }

        public void Unsubscribe(dynamic request)
        {
            var sub = _subscriptions.FirstOrDefault(x => x.Value == request.id).Key;
            _subscriptions.Remove(sub);
        }

        public async Task SendMessageToClientAsync(DMessage dMessage)
        {
            switch (dMessage.Type)
            {
                case MessageType.TextMessage:
                case MessageType.EditTextMessage:
                    await NotifyMessageCreatedAsync(dMessage, NotifyClientKind.FullChat);
                    return;

                case MessageType.ChatCreation:
                case MessageType.ChatMembers:
                    await NotifyMessageCreatedAsync(dMessage, NotifyClientKind.Chat);
                    return;

                default:
                    return;
            }
        }

        public async Task NotifyMessageCreatedAsync(DMessage dMessage, NotifyClientKind notify)
        {
            if (notify.HasFlag(NotifyClientKind.Subscription))
                await UpdateRoomsSubscription(dMessage.ChatId);

            if (notify.HasFlag(NotifyClientKind.Room))
                await UpdateRoom(dMessage.ChatId);

            if (notify.HasFlag(NotifyClientKind.Message))
                await SendMessageUpdate(dMessage);
        }
        public void SendTypingMessageToClient(DChat chat, int personId)
        {
            var roomId = _chatService.DataLoader.RCDataConverter.ConvertToRoomId(chat);
            _typingTimer.Start(roomId, personId);
        }
        public Task SendUserStatusChangeAsync(int personId, UserStatuses status)
        {
            if (!_subscriptions.TryGetValue(Streams.STREAM_USER_PRESENCE, out var id))
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

        private Task SendChatCreated(DMessage dMessage)
        {
            var eventName = $"{_sessionId}/{Events.EVENT_ROOMS_CHANGED}";
            var room = _chatService.DataLoader.LoadRoom(dMessage.ChatId);
            if (!_subscriptions.TryGetValue(eventName, out var id))
                return Task.CompletedTask;

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

        private Task SendTypingMessageToClientAsync(string roomId, int personId, bool isTyping)
        {
            var person = _serverApi.GetPerson(personId);
            var eventName = $"{roomId}/typing";
            if (!_subscriptions.TryGetValue(eventName, out var id))
                return Task.CompletedTask;

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
            return _webSocket.SendResultAsync(result);
        }

        private async Task SendMessageUpdate(DMessage message)
        {
            var rocketChatMessage = _chatService.DataLoader.RCDataConverter.ConvertToMessage(message);
            var eventName = $"{rocketChatMessage.roomId}";
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

            await SendTypingMessageToClientAsync(rocketChatMessage.roomId, message.CreatorId, false);
            await _webSocket.SendResultAsync(result);
        }

        private Task UpdateRoomsSubscription(Guid chatId)
        {
            var eventName = $"{_sessionId}/{Events.EVENT_SUBSCRIPTIONS_CHANGED}";
            var sub = _chatService.DataLoader.LoadRoomsSubscription(chatId.ToString());
            if (!_subscriptions.TryGetValue(eventName, out var id))
                return Task.CompletedTask;

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

        private Task UpdateRoom(Guid chatId)
        {
            var eventName = $"{_sessionId}/{Events.EVENT_ROOMS_CHANGED}";
            var room = _chatService.DataLoader.LoadRoom(chatId);
            if (!_subscriptions.TryGetValue(eventName, out var id))
                return Task.CompletedTask;

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
        public void Dispose()
        {
            _typingTimer.Dispose();
            _subscriptions.Clear();
        }
    }
}
