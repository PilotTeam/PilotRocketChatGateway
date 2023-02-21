using Ascon.Pilot.DataClasses;
using Newtonsoft.Json.Linq;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

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
        private IPushGatewayConnector _connector;
        private readonly IChatService _chatService;

        public PushService(IPushGatewayConnector connector, IChatService chatService)
        {
            _connector = connector;
            _chatService = chatService;
        }

        public void SetPushToken(PushToken token)
        {
            _pushToken = token;
        }

        public async Task SendPushAsync(DMessage message)
        {
            if (_pushToken == null)
                return;

            if (message.Type == MessageType.TextMessage)
            {
                var rcMesssage = _chatService.DataLoader.RCDataConverter.ConvertToMessage(message);
                var options = new PushOptions 
                {
                    createdAt = rcMesssage.creationDate,
                    createdBy = rcMesssage.u.name,
                    title = "title",
                    text = rcMesssage.msg,
                    userId = rcMesssage.u.id
                };
                await _connector.SendPushAsync(_pushToken, options);
            }
        }

        public void Dispose()
        {
            _pushToken = null;
        }
    }
}
