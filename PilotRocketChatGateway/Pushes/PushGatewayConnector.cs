using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.Utils;
using System.Net;
using System.ServiceModel.Channels;

namespace PilotRocketChatGateway.Pushes
{
    public interface IPushGatewayConnector
    {
        Task SendPushAsync(PushToken userToken, PushOptions options);
    }

    public record PushOptions
    {
        public string createdAt { get; init; }
        public string createdBy { get; init; }
        public string title { get; init; }
        public string text { get; init; }
        public string userId { get; init; }
    }
    public class PushGatewayConnector : IPushGatewayConnector
    {
        const string PUSH_GATEWAY_URL = "https://gateway.rocket.chat";
        private readonly IWorkspace _workspace;
        private readonly ILogger<PushGatewayConnector> _logger;
        private string _accessToken;
        public PushGatewayConnector(IWorkspace workspace, ILogger<PushGatewayConnector> logger)
        {
            _workspace = workspace;
            _logger = logger;
        }

        public async Task SendPushAsync(PushToken userToken, PushOptions options)
        {
            if (_workspace.Data == null)
                return;

            if (string.IsNullOrEmpty(_accessToken))
            {
                var success = await AuthorizeAsync();
                if (!success)
                    return;
            }


            var (result, code) = await PushAsync(userToken, options);

            if (code == System.Net.HttpStatusCode.OK)
            {
                _logger.Log(LogLevel.Information, $"successfully pushed to {options.createdBy}");
                return;
            }

            if (code == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.Log(LogLevel.Error, $"trying to refresh cloud's autorize token");
                var success = await AuthorizeAsync();
                if (!success)
                    return;


                (result, code) = await PushAsync(userToken, options);
                if (code == System.Net.HttpStatusCode.OK)
                {
                    _logger.Log(LogLevel.Information, $"successfully pushed to {options.createdBy}");
                    return;
                }

            }



            _logger.Log(LogLevel.Error, $"failed to push to {options.createdBy}: {result}, code: {code}");
        }

        private Task<(string, HttpStatusCode)> PushAsync(PushToken userToken, PushOptions options)
        {
            var payload = new
            {
                token = userToken.apn,
                options = new
                {
                    createdAt = options.createdAt,
                    createdBy = options.createdBy,
                    sent = false,
                    sending = 0,
                    from = "push",
                    title = options.title,
                    text = options.text,
                    userId = options.userId,
                    sound = "default",
                    topic = "chat.rocket.ios",
                }
            };

            return HttpRequestHelper.PostJsonAsync($"{PUSH_GATEWAY_URL}/push/apn/send", JsonConvert.SerializeObject(payload), $"Bearer {_accessToken}");
        }

        private async Task<bool> AuthorizeAsync()
        {
            var cloudToken = await CloudConnector.AutorizeAsync(_workspace, _logger);
            if (string.IsNullOrEmpty(cloudToken))
                return false;

            _accessToken = cloudToken;
            return true;
        }
    }
}
