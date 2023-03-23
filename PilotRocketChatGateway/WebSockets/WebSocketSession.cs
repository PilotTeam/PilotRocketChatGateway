using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api.Contracts;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.WebSockets.EventStreams;
using PilotRocketChatGateway.WebSockets.Subscriptions;
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
        void SendMessageToClient(DMessage dMessage);
        void NotifyMessageCreated(DMessage dMessage, NotifyClientKind notify);
        void SendUserStatusChange(int person, UserStatuses status);
        void SendTypingMessageToClient(DChat chat, int personId);
        void Subscribe(dynamic request);
        void Unsubscribe(dynamic request);
    }

    public class WebSocketSession : IWebSocketSession
    {
        private readonly IChatService _chatService;
        private readonly INPerson _currentPerson;
        private readonly WebSocket _webSocket;
        private readonly TypingTimer _typingTimer;

        private List<IEventStream> _streams = new List<IEventStream>();
        private StreamNotifyUser _streamNotifyUser;
        private StreamNotifyRoom _streamNotifyRoom;
        private StreamRoomMessages _streamRoomMessages;
        private StreamUserPresence _streamUserPresence;

        public WebSocketSession(dynamic request, AuthSettings authSettings, IChatService chatService, INPerson currentPerson, IAuthHelper authHelper, WebSocket webSocket)
        {
            var authToken = request.@params[0].resume;
            if (authHelper.ValidateToken(authToken, authSettings) == false)
                throw new UnauthorizedAccessException();

            _chatService = chatService;
            _currentPerson = currentPerson;
            _webSocket = webSocket;

            _streamNotifyUser = new StreamNotifyUser(_webSocket, _chatService);
            _streamUserPresence = new StreamUserPresence(_webSocket, _chatService);
            _streamNotifyRoom = new StreamNotifyRoom(_webSocket, chatService);
            _streamRoomMessages = new StreamRoomMessages(_webSocket, _chatService);
            _streams.Add(_streamNotifyUser);
            _streams.Add(_streamUserPresence);
            _streams.Add(_streamNotifyRoom);
            _streams.Add(_streamRoomMessages);

            _typingTimer = new TypingTimer
            (
                (chat, personId) => _streamNotifyRoom.SendTypingMessageToClient(chat, personId, true),
                (chat, personId) => _streamNotifyRoom.SendTypingMessageToClient(chat, personId, false)
            );
        }

        public void Subscribe(dynamic request)
        {
            switch (request.name as string)
            {
                case Streams.STREAM_NOTIFY_USER:
                    _streamNotifyUser.RegisterEvent(request);
                    break;
                case Streams.STREAM_USER_PRESENCE:
                    _streamUserPresence.RegisterEvent(request);
                    break;
                case Streams.STREAM_NOTIFY_ROOM:
                    _streamNotifyRoom.RegisterEvent(request);
                    break;
                case Streams.STREAM_ROOM_MESSAGES:
                    _streamRoomMessages.RegisterEvent(request);
                    break;
            }

            var result = new
            {
                msg = "ready",
                subs = new string[] { request.id.ToString() }
            };
            _webSocket.SendResultAsync(result);
        }

        public void Unsubscribe(dynamic request)
        {
            foreach(var stream in _streams)
            {
                if (stream.DeleteEvent(request))
                    return;
            }
        }

        public void SendMessageToClient(DMessage dMessage)
        {
            switch (dMessage.Type)
            {
                case MessageType.TextMessage:
                case MessageType.EditTextMessage:
                case MessageType.MessageAnswer:
                case MessageType.ChatMembers:
                case MessageType.ChatChanged:
                    NotifyMessageCreated(dMessage, NotifyClientKind.FullChat);
                    return;

                case MessageType.ChatCreation:
                    NotifyMessageCreated(dMessage, NotifyClientKind.Chat);
                    return;

                default:
                    return;
            }
        }
        public void NotifyMessageCreated(DMessage dMessage, NotifyClientKind notify)
        {

            if (notify.HasFlag(NotifyClientKind.Subscription))
                _streamNotifyUser.UpdateRoomsSubscription(dMessage.ChatId);

            if (notify.HasFlag(NotifyClientKind.Room))
                _streamNotifyUser.UpdateRoom(dMessage.ChatId);

            if (notify.HasFlag(NotifyClientKind.Message))
            {
                var rocketChatMessage = _chatService.DataLoader.RCDataConverter.ConvertToMessage(dMessage);
                _streamNotifyRoom.SendTypingMessageToClient(rocketChatMessage.roomId, dMessage.CreatorId, false);
                _streamRoomMessages.SendMessageUpdate(rocketChatMessage);

                if (dMessage.CreatorId != _currentPerson.Id && _chatService.DataLoader.IsChatNotifiable(dMessage.ChatId))
                {
                    var chat = _chatService.DataLoader.LoadChat(dMessage.ChatId);
                    _streamNotifyUser.NotifyUser(rocketChatMessage, chat.Chat.Type);
                }
            }
        }
        public void SendTypingMessageToClient(DChat chat, int personId)
        {
            var roomId = _chatService.DataLoader.RCDataConverter.ConvertToRoomId(chat);
            _typingTimer.Start(roomId, personId);
        }
        public void SendUserStatusChange(int personId, UserStatuses status)
        {
            _streamUserPresence.SendUserStatusChange(personId, status);
        }
        public void Dispose()
        {
            _typingTimer.Dispose();
        }
    }
}
