using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.Utils;
using System.Net;

namespace PilotRocketChatGateway.Pushes
{
    public interface IPushGatewayConnector
    {
        Task SendPushAsync(PushToken userToken, PushOptions options, string userName);
    }
    public class PushGatewayConnector : IPushGatewayConnector
    {
        private const string PUSH_GATEWAY_URL = "https://gateway.rocket.chat";
        private readonly IWorkspace _workspace;
        private readonly ILogger<PushGatewayConnector> _logger;
        private readonly ICloudsAuthorizeQueue _authorizeQueue;
        private readonly IHttpRequestHelper _requestHelper;
        private readonly RocketChatCloudSettings _cloudSettings;
        private object _locker = new object();
        private string _accessToken;

        public PushGatewayConnector(IWorkspace workspace, ICloudsAuthorizeQueue authorizeQueue, IHttpRequestHelper requestHelper, ILogger<PushGatewayConnector> logger, IOptions<RocketChatCloudSettings> settings)
        {
            _cloudSettings = settings.Value;
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

        public async Task SendPushAsync(PushToken userToken, PushOptions options, string userName)
        {
            if (_workspace.Data == null)
                return;


            var action = new Action<string>((t) =>
            {
                if (string.IsNullOrEmpty(AccessToken))
                    AccessToken = t;

                PushAsync(userToken, options, userName);
            });


            if (string.IsNullOrEmpty(AccessToken))
            {
                _authorizeQueue.Authorize(action);
                return;
            }

            var (result, code) = await PushAsync(userToken, options, userName);
            if (code == HttpStatusCode.OK)
                return;

            if (code == HttpStatusCode.Unauthorized)
            {
                AccessToken = null;
                _logger.Log(LogLevel.Error, $"trying to refresh cloud's autorize token");
               _authorizeQueue.Authorize(action); 
            }
        }

        private async Task<(string, HttpStatusCode)> PushAsync(PushToken userToken, PushOptions options, string userName)
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
                    title = _cloudSettings.HidePushInfo ? "new message" : options.title,
                    text = _cloudSettings.HidePushInfo ? "new message in rocket chat." : options.text,
                    userId = _cloudSettings.HidePushInfo ? "" : options.userId,
                    sound = "default",
                    topic = options.appName,
                    badge = options.badge,
                    payload = new
                    {
                        host = "",
                        messageId = _cloudSettings.HidePushInfo ? "" : options.msgId,
                        notificationType = "message",
                        rid = _cloudSettings.HidePushInfo ? "" : options.roomId,
                        sender = _cloudSettings.HidePushInfo ? null : options.sender,
                        senderName = _cloudSettings.HidePushInfo ? "" : options.sender.name,
                        type = _cloudSettings.HidePushInfo ? "" : options.type,
                        name = _cloudSettings.HidePushInfo ? "" : options.name
                    },

                }
            };

            var (result, code) = await _requestHelper.PostJsonAsync($"{PUSH_GATEWAY_URL}/push/{userToken.Type}/send", JsonConvert.SerializeObject(data), $"Bearer {AccessToken}");

            if (code == HttpStatusCode.OK)
            {
                _logger.Log(LogLevel.Information, $"successfully pushed to {userName}. msg id: {options.msgId}. creation date: {options.createdAt}");
                return (result, code);
            }

            _logger.Log(LogLevel.Error, $"failed to push to {options.userId}: {result}, code: {code}");
            return (result, code);
        } 
    }
}
  