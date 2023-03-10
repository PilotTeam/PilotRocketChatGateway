using Ascon.Pilot.DataClasses;
using Newtonsoft.Json.Linq;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using System;

namespace PilotRocketChatGateway.Pushes
{
    public interface IPushService : IService
    {
        PushToken PushToken { get; set; }
        Task SendPushAsync(DMessage message);
    }
    public enum PushTokenTypes
    {
        apn,
        gcm
    }
    public class PushToken
    {
        public PushTokenTypes Type { get; init; }
        public string Value { get; init; }
    }
    public class PushService : IPushService
    {
        private readonly IPushGatewayConnector _connector;
        private readonly IContext _context;
        private object _locker = new object();
        private PushToken _pushToken;

        public PushService(IPushGatewayConnector connector, IContext context)
        {
            _connector = connector;
            _context = context;
        }
        public PushToken PushToken
        {
            get
            {
                lock (_locker)
                {
                    return _pushToken;
                }
            }
            set
            {
                lock (_locker)
                {
                    _pushToken = value;
                }
            }
        }

        public async Task SendPushAsync(DMessage message)
        {
            if (CanPushToUser(message) == false)
                return;

            var chat = _context.RemoteService.ServerApi.GetChat(message.ChatId);
            var options = chat.Chat.Type == ChatKind.Personal ? GetPersonalChatOption(message) : GetGroupChatOption(message, chat.Chat);
            await _connector.SendPushAsync(PushToken, options);
        }

        private bool CanPushToUser(DMessage message)
        {
            if (_pushToken == null)
                return false;

            var currentPersonId = _context.RemoteService.ServerApi.CurrentPerson.Id;
            if (message.CreatorId == currentPersonId)
                return false;

            if (message.Type != MessageType.TextMessage && message.Type != MessageType.MessageAnswer)
                return false;

            if (_context.WebSocketsNotifyer.Services.Any(x => x.Value.RCPresenceStatus == WebSockets.UserStatuses.online))
            {
                return false;
            }

            if (_context.ChatService.DataLoader.IsChatNotifiable(message.ChatId) == false)
                return false;

            return true;
        }

        private PushOptions GetGroupChatOption(DMessage message, DChat chat)
        {
            var rcMesssage = _context.ChatService.DataLoader.RCDataConverter.ConvertToMessage(message);
            return new PushOptions
            {
                createdAt = rcMesssage.creationDate,
                title = $"#{chat.Name}",
                name = chat.Name,
                msgId = rcMesssage.id,
                text = $"{rcMesssage.u.name}: {rcMesssage.msg}",
                roomId = rcMesssage.roomId,
                sender = rcMesssage.u,
                userId = _context.RemoteService.ServerApi.CurrentPerson.Id.ToString(),
                type = ChatType.GROUP_CHAT_TYPE,
                appName = GetAppName()
            };
        }

        private PushOptions GetPersonalChatOption(DMessage message)
        {
            var rcMesssage = _context.ChatService.DataLoader.RCDataConverter.ConvertToMessage(message);
            return new PushOptions
            {
                createdAt = rcMesssage.creationDate,
                title = rcMesssage.u.name,
                msgId = rcMesssage.id,
                text = rcMesssage.msg,
                roomId = rcMesssage.roomId,
                sender = rcMesssage.u,
                userId = _context.RemoteService.ServerApi.CurrentPerson.Id.ToString(),
                type = ChatType.PERSONAL_CHAT_TYPE,
                appName = GetAppName()
            };
        }
        
        private string GetAppName()
        {
            return PushToken.Type == PushTokenTypes.apn ? "chat.rocket.ios" : "chat.rocket.android";
        }

        public void Dispose()
        {
            PushToken = null;
        }
    }
}
