using Newtonsoft.Json;

namespace PilotRocketChatGateway
{
    public record HttpResult
    {
        public bool success { get; init; }
    }
    public record Error
    {
        public string status { get; init; }
        public string error { get; init; }
        public string message { get; init; }
    }
    public record Info : HttpResult
    {
        public string version { get; init; }
    }

    #region settings
    public record OauthSettings : HttpResult
    {
        public string[] services { get; init; }
    }

    public record SettingsRequest
    {
        [JsonProperty("_id")]
        public SettingsListRequest settings { get; init; }
    }

    public record SettingsListRequest
    {
        [JsonProperty("$in")]
        public List<string> settings { get; init; }
    }

    public record ServerSettings : HttpResult
    {
        public List<Setting> settings { get; init; }
        public int count => settings.Count;
        public int offset { get; init; }
        public int total { get; init; }
    }

    public record Setting
    {
        [JsonProperty("_id")]
        public string id { get; init; }
        public object value { get; init; }
        public bool enterprise { get; init; }
        public bool invalidValue { get; init; }
        public string[] modules { get; init; }
    }
    #endregion settings
    public class User
    {
        public string name;
        public string username;
    }
    public record WebSocketRequest
    {
        public string id { get; init; }

        public string msg { get; init; }

        public string method { get; init; }

        [JsonProperty("params")]
        public WebSocketParams[] @params { get; init; }
    }
    public record WebSocketParams
    {
        public User user { get; init; }
        public string password { get; init; }
    }

    public record WebSocketResult
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string id { get; init; }

        public string msg { get; init; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public WebSocketLogin result { get; init; }

    }

    public class WebSocketLogin
    {
        public string token { get; init; }
        public string id { get; init; }
        public Date tokenExpires { get; init; }
    }

    public class Date
    {
        [JsonProperty("$date")]
        public long date { get; init; }
    }

    #region login

    public class LoginRequest
    {
        public string user { get; init; }
        public string password { get; init; }
    }

    public class HttpLoginResponse
    {
        public string status;
        public LoginData data;

    }

    public class LoginData
    {
        public string authToken;
        public string userId;
        public User me;
    }

    #endregion
}
