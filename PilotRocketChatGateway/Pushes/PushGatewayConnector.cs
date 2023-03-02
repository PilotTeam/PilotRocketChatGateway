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
        private readonly ICloudsAuthorizeQueue _authorizeQueue;
        private readonly IHttpRequestHelper _requestHelper;
        private object _locker = new object();
        private string _accessToken;

        public PushGatewayConnector(IWorkspace workspace, ICloudsAuthorizeQueue authorizeQueue, IHttpRequestHelper requestHelper, ILogger<PushGatewayConnector> logger)
        {
            _workspace = workspace;
            _logger = logger;
            _authorizeQueue = authorizeQueue;
            _requestHelper = requestHelper;
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


            var action = new Action<string>(async (t) =>
            {
                if (AccessToken == null)
                    AccessToken = t;

                var (result, code) = await PushAsync(userToken, options);
                if (code == HttpStatusCode.OK)
                    _logger.Log(LogLevel.Information, $"successfully pushed to {options.userId}");
            });


            if (string.IsNullOrEmpty(AccessToken))
            {
                _authorizeQueue.Authorize(action);
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
               _authorizeQueue.Authorize(action); 
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

            return _requestHelper.PostJsonAsync($"{PUSH_GATEWAY_URL}/push/{userToken.Type}/send", JsonConvert.SerializeObject(data), $"Bearer {AccessToken}");
        }

    }
}
