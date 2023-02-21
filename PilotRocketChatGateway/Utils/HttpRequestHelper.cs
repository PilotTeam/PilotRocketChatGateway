using System.Net;
using System.Text;

namespace PilotRocketChatGateway.Utils
{
    public class HttpRequestHelper
    {
        public static async Task<(string, HttpStatusCode)> PostJsonAsync(string requestUri, string json, string accessToken)
        {
            var client = CreateHttpClient(accessToken);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                HttpResponseMessage response = await client.PostAsync(requestUri, content);
                return (await response.Content.ReadAsStringAsync(), response.StatusCode);
            }
        }

        public static async Task<(string, HttpStatusCode)> PostEncodedContentAsync(string requestUri, IEnumerable<KeyValuePair<string, string>> payload)
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
            HttpClient httpClient = new HttpClient();

            if (accessToken != null)
                httpClient.DefaultRequestHeaders.Add("Authorization", accessToken);

            return httpClient;
        }


    }
}
