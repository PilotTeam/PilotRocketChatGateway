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
    public record JSDate
    {
        [JsonProperty("$date")]
        public long date { get; init; }
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

    public record Permissions : HttpResult
    {
        public IList<Permission> update { get; init; }
        public IList<Permission> remove { get; init; }
    }
    public record Permission
    {
        [JsonProperty("_id")]
        public string id { get; init; }
        [JsonProperty("_updatedAt")]
        public object updatedAt { get; init; }
        public string group { get; init; }
        public string groupPermissionId { get; init; }
        public string level { get; init; }
        public string[] roles { get; init; }
        public string section { get; init; }
        public string sectionPermissionId { get; init; }
        public string settingId { get; init; }
        public int sorter { get; init; }
    }
    #endregion settings
    #region websocket
    public class User
    {
        [JsonProperty("_id")]
        public string id;
        public string name;
        public string username;
        public string status;
        public string[] roles;
    }
    #endregion websocket
    #region login

    public class LoginRequest
    {
        public string user { get; init; }
        public string password { get; init; }
        [JsonProperty("resume")]
        public string token { get; init; }
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
    #region rooms

    public record Rooms : HttpResult
    {
        public IList<Room> update { get; init; }
        public IList<Room> remove { get; init; }
    }
    public record Subscriptions : HttpResult
    {
        public IList<Subscription> update { get; init; }
        public IList<Subscription> remove { get; init; }
    }
    public record MessageRequest : HttpResult
    {
        public Message message { get; init; }
    }
    public record MessagesUpdated : HttpResult
    {
        public IList<Message> updated { get; init; }
        public IList<Message> deleted { get; init; }
    }
    public record RoomRequest
    {
        [JsonProperty("rid")]
        public string roomId { get; init; }
    }

    public record GroupRequest
    {
        public string name { get; init; }
        public string[] members { get; init; }
    }
    public record Subscription
    {
        [JsonProperty("_updatedAt")]
        public object updatedAt { get; init; }
        [JsonProperty("ls")]
        public object lastSeen { get; init; }
        [JsonProperty("_id")]
        public string id { get; init; }
        [JsonProperty("rid")]
        public string roomId { get; init; }
        public int unread { get; init; }
        public bool alert { get; init; }
        public string name { get; init; }
        [JsonProperty("fname")]
        public string displayName { get; init; }
        public bool open { get; init; }
        [JsonProperty("t")]
        public string channelType { get; init; }
    }
    public record Room
    {
        [JsonProperty("_updatedAt")]
        public object updatedAt { get; init; }
        [JsonProperty("_id")]
        public string id { get; init; }
        public string name { get; init; }
        [JsonProperty("t")]
        public string channelType { get; init; }
        public Message lastMessage { get; init; }
        [JsonProperty("ts")]
        public object creationDate { get; init; }
        public string[] usernames { get; init; }
    }

    public record Messages : HttpResult
    {
        public IList<Message> messages { get; init; }
    }

    public record Message
    {
        [JsonProperty("_id")]
        public string id { get; init; }
        [JsonProperty("_updatedAt")]
        public object updatedAt { get; init; }
        [JsonProperty("rid")]
        public string roomId { get; init; }
        public string msg { get; init; }
        [JsonProperty("ts")]
        public object creationDate { get; init; }
        public User u { get; init; }
    }
    #endregion rooms
    #region directory
    public record DirectoryRequest
    {
        public string type { get; init; }
        public string workspace { get; init; }
        public int count { get; init; }
        public int offset { get; init; }
        public string sort { get; init; }
    }
    #endregion
}
