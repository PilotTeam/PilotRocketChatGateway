using Newtonsoft.Json;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace PilotRocketChatGateway.Pushes
{
    public class CloudWorkspace
    {
        private const string CLOUD_URI = "https://cloud.rocket.chat";
        const string WORKSPACE_FILE_NAME = "workspace.json";
        public static async Task RegisterAsync(RocketChatCloudSettings settings, Serilog.ILogger logger)
        {
            logger.Information("tring to register in cloud.rocket.chat");

            var request = new
            {
                client_name = settings.WorkspaceName,
                email = settings.WorkspaceEmail,
                redirect_uris = new[] { settings.WorkspaceUri }
            };

            var (result, code) = await PostJson($"{CLOUD_URI}/api/oauth/clients", JsonConvert.SerializeObject(request), $"Bearer {settings.RegistrationToken}");

            if (code == HttpStatusCode.Created)
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), WORKSPACE_FILE_NAME);
                File.WriteAllText(path, result);

                logger.Information($"successfully registered in cloud.rocket.chat: saved result in {path}");
                return;
            }

            logger.Information($"Result: {result}, code: {code}");
        }
        private static async Task<(string, HttpStatusCode)> PostJson(string requestUri, string json, string accessToken)
        {
            var client = CreateHttpClient(accessToken);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(requestUri, content);
            return (await response.Content.ReadAsStringAsync(), response.StatusCode);
        }

        private static HttpClient CreateHttpClient(string accessToken)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", accessToken);
            return httpClient;
        }
    }
}
