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
        Task SendPushAsync(NotifiableDMessage message);
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
        private const string ATTACHMENT_TEXT = "Attachments";
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

        public async Task SendPushAsync(NotifiableDMessage message)
        {
            if (CanPushToUser(message) == false)
                return;

            var chat = _context.RemoteService.ServerApi.GetChat(message.Message.ChatId);
            var options = chat.Chat.Type == ChatKind.Personal ? GetPersonalChatOption(message.Message, chat.Chat) : GetGroupChatOption(message.Message, chat.Chat);
            await _connector.SendPushAsync(PushToken, options, _context.UserData.Username);
        }

        private bool CanPushToUser(NotifiableDMessage message)
        {
            if (_pushToken == null)
                return false;

            var currentPersonId = _context.RemoteService.ServerApi.CurrentPerson.Id;
            if (message.Message.CreatorId == currentPersonId)
                return false;

            if (message.Message.Type != MessageType.TextMessage && message.Message.Type != MessageType.MessageAnswer)
                return false;

            if (_context.WebSocketsNotifyer.Services.Any(x => x.Value.RCPresenceStatus == WebSockets.UserStatuses.online))
            {
                return false;
            }

            if (!message.IsNotifiable)
                return false;

            return true;
        }

        private PushOptions GetGroupChatOption(DMessage message, DChat chat)
        {
            var rcMesssage = _context.ChatService.DataLoader.RCDataConverter.ConvertToSimpleMessage(message, chat);
            return new PushOptions
            {
                createdAt = rcMesssage.creationDate,
                title = $"#{chat.Name}",
                name = chat.Name,
                msgId = rcMesssage.id,
                text = $"{rcMesssage.u.name}: {GetMsgText(rcMesssage)}",
                roomId = rcMesssage.roomId,
                sender = rcMesssage.u,
                userId = _context.RemoteService.ServerApi.CurrentPerson.Id.ToString(),
                type = ChatType.GROUP_CHAT_TYPE,
                appName = GetAppName()
            };
        }

        private PushOptions GetPersonalChatOption(DMessage message, DChat chat)
        {
            var rcMesssage = _context.ChatService.DataLoader.RCDataConverter.ConvertToSimpleMessage(message, chat);
            return new PushOptions
            {
                createdAt = rcMesssage.creationDate,
                title = rcMesssage.u.name,
                msgId = rcMesssage.id,
                text = GetMsgText(rcMesssage),
                roomId = rcMesssage.roomId,
                sender = rcMesssage.u,
                userId = _context.RemoteService.ServerApi.CurrentPerson.Id.ToString(),
                type = ChatType.PERSONAL_CHAT_TYPE,
                appName = GetAppName()
            };
        }
        private string GetMsgText(Message rcMessage)
        {
            return string.IsNullOrEmpty(rcMessage.msg) && rcMessage.attachments.Any() ? ATTACHMENT_TEXT : rcMessage.msg;
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
