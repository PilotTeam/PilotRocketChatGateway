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
        private readonly IChatService _chatService;
        private readonly IServerApiService _serverApi;

        public PushService(IPushGatewayConnector connector, IChatService chatService, IServerApiService serverApi)
        {
            _connector = connector;
            _chatService = chatService;
            _serverApi = serverApi;
        }

        public void SetPushToken(PushToken token)
        {
            _pushToken = token;
        }

        public async Task SendPushAsync(DMessage message)
        {
            if (_pushToken == null)
                return;

            if (message.Type == MessageType.TextMessage || message.Type == MessageType.MessageAnswer)
            {
                var chat = _serverApi.GetChat(message.ChatId);
                var options = chat.Chat.Type == ChatKind.Personal ? GetPersonalChatOption(message) : GetGroupChatOption(message, chat.Chat);
                await _connector.SendPushAsync(_pushToken, options);
            }
        }


        private PushOptions GetGroupChatOption(DMessage message, DChat chat)
        {
            var rcMesssage = _chatService.DataLoader.RCDataConverter.ConvertToMessage(message);
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
                userId = _serverApi.CurrentPerson.Id.ToString(),
                type = ChatType.GROUP_CHAT_TYPE
            };
        }

        private PushOptions GetPersonalChatOption(DMessage message)
        {
            var rcMesssage = _chatService.DataLoader.RCDataConverter.ConvertToMessage(message);
            return new PushOptions
            {
                createdAt = rcMesssage.creationDate,
                title = rcMesssage.u.name,
                msgId = rcMesssage.id,
                text = rcMesssage.msg,
                roomId = rcMesssage.roomId,
                msg = rcMesssage,
                sender = rcMesssage.u,
                userId = _serverApi.CurrentPerson.Id.ToString(),
                type = ChatType.PERSONAL_CHAT_TYPE
            };
        }

        public void Dispose()
        {
            _pushToken = null;
        }
    }
}
