using Newtonsoft.Json;

namespace PilotRocketChatGateway
{
    public record HttpResult
    {
        public bool success { get; init; }
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
        public SettingsListRequest settings{ get; init; }
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

    public record WebSocketRequest : HttpResult
    {
        public string msg { get; init; }
        public string version { get; init; }
        public string[] support { get; init; }
    }
}
