using log4net.Filter;
using Newtonsoft.Json;
using PilotRocketChatGateway.Controllers;
using PilotRocketChatGateway.Utils;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace PilotRocketChatGateway.Pushes
{
    public interface ICloudConnector
    {
        Task<WorkspaceData> RegisterAsync(RocketChatCloudSettings settings, Serilog.ILogger logger);
        Task<string> AutorizeAsync(IWorkspace workspace, ILogger logger);
    }

    public class CloudConnector : ICloudConnector
    {
        private IHttpRequestHelper _requestHelper;
        private IPollRegistration _poll;

        public CloudConnector(IHttpRequestHelper requestHelper, IPollRegistration poll)
        {
            _requestHelper = requestHelper;
            _poll = poll;
        }
        public async Task<WorkspaceData> RegisterAsync(RocketChatCloudSettings settings, Serilog.ILogger logger)
        {
            logger.Information("trying to register in cloud.rocket.chat");

            var intent = await RegisterIntent(settings, logger);
            return await _poll.PollAsync(intent, logger);
        }

        public async Task<string> AutorizeAsync(IWorkspace workspace, ILogger logger)
        {
            logger.Log(LogLevel.Information, "trying to authorize in cloud.rocket.chat");

            var payload = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string> (nameof(workspace.Data.client_id) , workspace.Data.client_id),
                new KeyValuePair<string, string> (nameof(workspace.Data.client_secret) , workspace.Data.client_secret),
                new KeyValuePair<string, string> ("scope" , "workspace:push:send"),
                new KeyValuePair<string, string> ("grant_type" , "client_credentials"),
                new KeyValuePair<string, string> ("redirect_uri" , "")
            };

            var (result, code) = await _requestHelper.PostEncodedContentAsync($"{Const.CLOUD_URI}/api/oauth/token", payload);
            if (code == HttpStatusCode.Created) 
            {
                var data = JsonConvert.DeserializeObject<PushGatewayAccessData>(result);
                logger.Log(LogLevel.Information, $"successfully authorized in cloud.rocket.chat");
                return data.access_token;
            }

            logger.Log(LogLevel.Information, $"Result: {result}, code: {code}");
            return null;
        }

        private async Task<IntentData> RegisterIntent(RocketChatCloudSettings settings, Serilog.ILogger logger)
        {
            logger.Information("calling intent registration");

            var payload = new
            {
                contactEmail = settings.WorkspaceEmail,
                siteName = settings.WorkspaceName,
                address = settings.WorkspaceUri,
                version = Const.SERVER_VERSION,
            };

            var (result, code) = await _requestHelper.PostJsonAsync($"{Const.CLOUD_URI}/api/v2/register/workspace/intent", JsonConvert.SerializeObject(payload));

            if (code != HttpStatusCode.Created)
                throw new Exception($"Failed to process intent: {result}, code: {code}");

            logger.Information($"intent part of registration is passed");
            return JsonConvert.DeserializeObject<IntentData>(result);
        }

    }
}
