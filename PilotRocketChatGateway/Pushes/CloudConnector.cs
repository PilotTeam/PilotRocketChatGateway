using Newtonsoft.Json;
using PilotRocketChatGateway.Utils;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace PilotRocketChatGateway.Pushes
{
    public interface ICloudConnector
    {
        Task<string> RegisterAsync(RocketChatCloudSettings settings, Serilog.ILogger logger);
        Task<string> AutorizeAsync(IWorkspace workspace, ILogger logger);
    }

    public class CloudConnector : ICloudConnector
    {
        private const string CLOUD_URI = "https://cloud.rocket.chat";
        private IHttpRequestHelper _requestHelper;

        public CloudConnector(IHttpRequestHelper requestHelper)
        {
            _requestHelper = requestHelper;
        }
        public async Task<string> RegisterAsync(RocketChatCloudSettings settings, Serilog.ILogger logger)
        {
            logger.Information("trying to register in cloud.rocket.chat");

            var payload = new
            {
                client_name = settings.WorkspaceName,
                email = settings.WorkspaceEmail,
                redirect_uris = new[] { settings.WorkspaceUri }
            };

            var (result, code) = await _requestHelper.PostJsonAsync($"{CLOUD_URI}/api/oauth/clients", JsonConvert.SerializeObject(payload), $"Bearer {settings.RegistrationToken}");

            if (code == HttpStatusCode.Created)
            {
                logger.Information($"successfully registered in cloud.rocket.chat");
                return result;
            }

            logger.Information($"Result: {result}, code: {code}");
            return null;
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

            var (result, code) = await _requestHelper.PostEncodedContentAsync($"{CLOUD_URI}/api/oauth/token", payload);
            if (code == HttpStatusCode.Created) 
            {
                var data = JsonConvert.DeserializeObject<PushGatewayAccessData>(result);
                logger.Log(LogLevel.Information, $"successfully authorizes in cloud.rocket.chat");
                return data.access_token;
            }

            logger.Log(LogLevel.Information, $"Result: {result}, code: {code}");
            return string.Empty;
        }
    }
}
