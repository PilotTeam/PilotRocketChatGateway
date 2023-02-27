using Ascon.Pilot.DataClasses;
using Newtonsoft.Json.Linq;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using System;

namespace PilotRocketChatGateway.Pushes
{
    public interface IPushService : IService
    {
        void SetPushToken(PushToken token);
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
        private PushToken _pushToken;
        private readonly IPushGatewayConnector _connector;
        private readonly IContext _context;

        public PushService(IPushGatewayConnector connector, IContext context)
        {
            _connector = connector;
            _context = context;
        }

        public void SetPushToken(PushToken token)
        {
            _pushToken = token;
        }

        public async Task SendPushAsync(DMessage message)
        {
            if (CanPushToUser(message) == false)
                return;

            var chat = _context.RemoteService.ServerApi.GetChat(message.ChatId);
            var options = chat.Chat.Type == ChatKind.Personal ? GetPersonalChatOption(message) : GetGroupChatOption(message, chat.Chat);
            await _connector.SendPushAsync(_pushToken, options);
        }

        private bool CanPushToUser(DMessage message)
        {
            if (_pushToken == null)
                return false;

            if (message.Type != MessageType.TextMessage && message.Type != MessageType.MessageAnswer)
                return false;

            if (_context.WebSocketsNotifyer.Services.FirstOrDefault(x => x.Value.RCPresenceStatus == WebSockets.UserStatuses.online).Value != null)
            {
                return false;
            }

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
                msg = rcMesssage,
                sender = rcMesssage.u,
                userId = _context.RemoteService.ServerApi.CurrentPerson.Id.ToString(),
                type = ChatType.GROUP_CHAT_TYPE
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
                msg = rcMesssage,
                sender = rcMesssage.u,
                userId = _context.RemoteService.ServerApi.CurrentPerson.Id.ToString(),
                type = ChatType.PERSONAL_CHAT_TYPE
            };
        }

        public void Dispose()
        {
            _pushToken = null;
        }
    }
}
