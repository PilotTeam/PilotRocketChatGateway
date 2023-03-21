using System.Net;
using System.Text;

namespace PilotRocketChatGateway.Utils
{
    public interface IHttpRequestHelper
    {
        Task<(string, HttpStatusCode)> PostJsonAsync(string requestUri, string json, string accessToken);
        Task<(string, HttpStatusCode)> PostEncodedContentAsync(string requestUri, IEnumerable<KeyValuePair<string, string>> payload);
    }

    public class HttpRequestHelper : IHttpRequestHelper
    {
        public async Task<(string, HttpStatusCode)> PostJsonAsync(string requestUri, string json, string accessToken)
        {
            var client = CreateHttpClient(accessToken);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                HttpResponseMessage response = await client.PostAsync(requestUri, content);
                return (await response.Content.ReadAsStringAsync(), response.StatusCode);
            }
        }

        public async Task<(string, HttpStatusCode)> PostEncodedContentAsync(string requestUri, IEnumerable<KeyValuePair<string, string>> payload)
        {
            var client = CreateHttpClient();

            using (var content = new FormUrlEncodedContent(payload))
            {
                content.Headers.Clear();
                content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                HttpResponseMessage response = await client.PostAsync(requestUri, content);
                return (await response.Content.ReadAsStringAsync(), response.StatusCode);
            }
        }

        private static HttpClient CreateHttpClient(string accessToken = null)
        {

            var proxy = new WebProxy
            {
                Address = new Uri("http://10.1.5.248:3128"),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("olkhovskii_av", "JCe9LuRm")
            };

            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
            };

            HttpClient httpClient = new HttpClient(httpClientHandler);

            if (accessToken != null)
                httpClient.DefaultRequestHeaders.Add("Authorization", accessToken);

            return httpClient;
        }


    }
}
