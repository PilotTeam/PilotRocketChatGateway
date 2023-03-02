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
        private readonly CloudsAthorizeQueue _authorizeQueue;
        private object _locker = new object();
        private string _accessToken;

        public PushGatewayConnector(IWorkspace workspace, ILogger<PushGatewayConnector> logger)
        {
            _workspace = workspace;
            _logger = logger;
            _authorizeQueue = new CloudsAthorizeQueue(workspace, logger);
        }
        string AccessToken
        {
            get
            {
                lock (_locker)
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


            var thread = new Action<string>(async (t) =>
            {
                if (AccessToken == null)
                    AccessToken = t;

                var (result, code) = await PushAsync(userToken, options);
                if (code == HttpStatusCode.OK)
                    _logger.Log(LogLevel.Information, $"successfully pushed to {options.userId}");
            });


            if (string.IsNullOrEmpty(AccessToken))
            {
                _authorizeQueue.Authorize(thread);
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
                AccessToken = null;
                _logger.Log(LogLevel.Error, $"trying to refresh cloud's autorize token");
               _authorizeQueue.Authorize(thread); 
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
                    topic = options.appName,
                    badge = options.badge,
                    payload = new
                    {
                        host = "",
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

    }
}
