using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace PilotRocketChatGateway.Utils
{
    public interface IHttpRequestHelper
    {
        Task<(string, HttpStatusCode)> GetAsync(string requestUri, IDictionary<string, object> quaryParams, string accessToken = null);
        Task<(string, HttpStatusCode)> PostJsonAsync(string requestUri, string json, string accessToken = null);
        Task<(string, HttpStatusCode)> PostEncodedContentAsync(string requestUri, IEnumerable<KeyValuePair<string, string>> payload);
    }

    public class HttpRequestHelper : IHttpRequestHelper
    {
        private const string PROXY_FILE_NAME = "proxy.json";

        private ProxySettings _proxySettings;
        public HttpRequestHelper(IWebHostEnvironment env)
        {
            SetUpProxy(env);
        }

        public async Task<(string, HttpStatusCode)> GetAsync(string requestUri, IDictionary<string, object> quaryParams, string accessToken = null)
        {
            if (quaryParams.Any())
                requestUri = $"{requestUri}?{StringifyParams(quaryParams)}";

            var client = InitializeHttpClient(accessToken);
            HttpResponseMessage response = await client.GetAsync(requestUri);
            return (await response.Content.ReadAsStringAsync(), response.StatusCode);
        }

        public async Task<(string, HttpStatusCode)> PostJsonAsync(string requestUri, string json, string accessToken)
        {
            var client = InitializeHttpClient(accessToken);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                HttpResponseMessage response = await client.PostAsync(requestUri, content);
                return (await response.Content.ReadAsStringAsync(), response.StatusCode);
            }
        }

        public async Task<(string, HttpStatusCode)> PostEncodedContentAsync(string requestUri, IEnumerable<KeyValuePair<string, string>> payload)
        {
            var client = InitializeHttpClient();

            using (var content = new FormUrlEncodedContent(payload))
            {
                content.Headers.Clear();
                content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                HttpResponseMessage response = await client.PostAsync(requestUri, content);
                return (await response.Content.ReadAsStringAsync(), response.StatusCode);
            }
        }

        private HttpClient InitializeHttpClient(string accessToken = null)
        {
            var httpClient = CreateHttpClient();

            if (accessToken != null)
                httpClient.DefaultRequestHeaders.Add("Authorization", accessToken);

            return httpClient;
        }
        private static string StringifyParams(IDictionary<string, object> quaryParams)
        {
            var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            foreach (var param in quaryParams)
                queryString.Add(param.Key, param.Value.ToString());

            return queryString.ToString();
        }

        private HttpClient CreateHttpClient()
        {
            if (_proxySettings == null)
                return new HttpClient();

            var proxy = new WebProxy
            {
                Address = new Uri(_proxySettings.address),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_proxySettings.login, _proxySettings.password)
            };
            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
            };

            return new HttpClient(httpClientHandler);
            
        }

        private void SetUpProxy(IHostEnvironment env)
        {
            var proxyfile = Path.Combine(env.ContentRootPath, PROXY_FILE_NAME);
            if (File.Exists(proxyfile) == false)
                return;

            var json = File.ReadAllText(proxyfile);
            _proxySettings = JsonConvert.DeserializeObject<ProxySettings>(json);
        }

    }
}
