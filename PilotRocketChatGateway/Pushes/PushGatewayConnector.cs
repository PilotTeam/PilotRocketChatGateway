using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.Utils;
using System.Net;

namespace PilotRocketChatGateway.Pushes
{
    public interface IPushGatewayConnector
    {
        Task SendPushAsync(PushToken userToken, PushOptions options);
    }
    public class PushGatewayConnector : IPushGatewayConnector
    {
        private const string PUSH_GATEWAY_URL = "https://gateway.rocket.chat";
        private readonly IWorkspace _workspace;
        private readonly ILogger<PushGatewayConnector> _logger;
        private string _accessToken;
        private object _locker = new object();
        public PushGatewayConnector(IWorkspace workspace, ILogger<PushGatewayConnector> logger)
        {
            _workspace = workspace;
            _logger = logger;
        }

        string AccessToken
        {
            get
            {
                lock(_locker)
                {
                    return _accessToken;
                }
            }
            set
            {
                lock (_locker)
                {
                    _accessToken = value;
                }
            } 
        }

        public async Task SendPushAsync(PushToken userToken, PushOptions options)
        {
            if (_workspace.Data == null)
                return;

            if (string.IsNullOrEmpty(AccessToken))
            {
                var success = await AuthorizeAsync();
                if (!success)
                    return;
            }


            var (result, code) = await PushAsync(userToken, options);

            if (code == HttpStatusCode.OK)
            {
                _logger.Log(LogLevel.Information, $"successfully pushed to {options.userId}");
                return;
            }

            if (code == HttpStatusCode.Unauthorized)
            {
                _logger.Log(LogLevel.Error, $"trying to refresh cloud's autorize token");
                var success = await AuthorizeAsync();
                if (!success)
                    return;


                (result, code) = await PushAsync(userToken, options);
                if (code == HttpStatusCode.OK)
                {
                    _logger.Log(LogLevel.Information, $"successfully pushed to {options.userId}");
                    return;
                }

            }

            _logger.Log(LogLevel.Error, $"failed to push to {options.userId}: {result}, code: {code}");
        }

        private Task<(string, HttpStatusCode)> PushAsync(PushToken userToken, PushOptions options)
        {
            var data = new
            {
                token = userToken.Value,
                options = new
                {
                    createdAt = options.createdAt,
                    createdBy = "<SERVER>",
                    sent = false,
                    sending = 0,
                    from = "push",
                    title = options.title,
                    text = options.text,
                    userId = options.userId,
                    sound = "default",
                    topic = "chat.rocket.ios",
                    badge = options.badge,
                    payload = new
                    {
                        messageId = options.msgId,
                        notificationType = "message",
                        msg = options.msg,
                        rid = options.roomId,
                        sender = options.sender,
                        senderName = options.sender.username,
                        type = options.type,
                        name = options.name
                    },

                }
            };

            return HttpRequestHelper.PostJsonAsync($"{PUSH_GATEWAY_URL}/push/{userToken.Type}/send", JsonConvert.SerializeObject(data), $"Bearer {AccessToken}");
        }

        private async Task<bool> AuthorizeAsync()
        {
            var cloudToken = await CloudConnector.AutorizeAsync(_workspace, _logger);
            if (string.IsNullOrEmpty(cloudToken))
                return false;

            AccessToken = cloudToken;
            return true;
        }
    }
}
