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
        public int error { get; init; }
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

    public record ProxySettings
    {
        public string address { get; init; }
        public string login { get; init; }
        public string password { get; init; }
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
        public bool success;
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
    public class ChatType
    {
        public const string PERSONAL_CHAT_TYPE = "d";
        public const string GROUP_CHAT_TYPE = "p";
    }
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
    public record MessageEdit 
    {
        public string roomId { get; init; }
        public string msgId { get; init; }
        public string text { get; init; }
    }
    public record MessageUpload : HttpResult
    {
        public FileAttachment file { get; init; }
    }
    public record SaveNotification
    {
        public string roomId { get; init; }
        public Notifications notifications { get; init; }
    }
    public record ConfirmUpload
    {
        public string msg { get; init; }
        public string tmid { get; init; }
        public string description { get; init; }
    }
    public record Notifications
    {
        public string disableNotifications { get; init; }
    }
    public record MessagesUpdated 
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
        public bool disableNotifications { get; init; }
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
        public string updatedAt { get; init; }
        [JsonProperty("rid")]
        public string roomId { get; init; }
        public string msg { get; init; }
        [JsonProperty("ts")]
        public string creationDate { get; init; }
        public User u { get; init; }
        public IList<Attachment> attachments { get; init; }
        public string editedAt { get; init; }
        public User editedBy { get; init; }
        [JsonProperty("t")]
        public string type { get; init; }
        public string role { get; init; }
    }
    public record Attachment
    {
        public string title { get; init; }
        public string title_link { get; init; }
        public Dimension image_dimensions { get; init; }
        public string image_preview { get; init; }
        public string image_type { get; init; }
        public long image_size { get; init; }
        public string image_url { get; init; }
        public string type { get; init; }
        public string text { get; init; }
        public string author_name { get; init; }
        [JsonProperty("ts")]
        public object creationDate { get; init; }
        public string message_link { get; init; }
        public string author_icon { get; init; }
        public IList<Attachment> attachments { get; init; }
    }
    public record FileAttachment
    {
        [JsonProperty("_id")]
        public string id { get; init; }
        public string name { get; init; }
        public string type { get; init; }
        public long size { get; init; }
        [JsonProperty("rid")]
        public string roomId { get; init; }
        public string userId { get; init; }
        public FileIdentity identify { get; init; }
        public string uploadedAt { get; init; }
        public string url { get; init; }
        public string _updatedAt { get; init; }
        public string typeGroup { get; init; }
        public User user { get; init; }
    }

    public record FileIdentity
    {
        public string format { get; init; }
        public Dimension size { get; init; }
    }

    public record Dimension
    {
        public int width { get; init; }
        public int height { get; init; }
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
    #region pushes
    public record PushTokenRequest
    {
        public string value { get; init; }
        public string type { get; init; }
        public string appName { get; init; }
    }
    public record WorkspacePollResult
    {
        public bool successful { get; init; }
        public WorkspaceData payload { get; init; }
    }

    public record WorkspaceData
    {
        public string workspaceId { get; init; }
        public string client_name { get; init; }
        public string client_id { get; init; }
        public string client_secret { get; init; }
        public long client_secret_expires_at { get; init; }
        public string publicKey { get; init; }
        public string registration_client_uri { get; init; }
    }

    public record IntentData
    {
        public string device_code { get; init; }
        public string user_code { get; init; }
        public string interval { get; init; }
        public long expires_in { get; init; }
    }
    public class RocketChatCloudSettings
    {
        public string WorkspaceName { get; set; }
        public string WorkspaceEmail { get; set; }
        public string WorkspaceUri { get; set; }
        public bool HidePushInfo { get; set; }
    }

    public class PushGatewayAccessData
    {
        public string access_token { get; set; }
        public long expires_in { get; set; }
        public string scope { get; set; }
        public string token_type { get; set; }
    }
    public record PushOptions
    {
        public string createdAt { get; init; }
        public string userId { get; init; }
        public string msgId { get; init; }
        public string title { get; init; }
        public int badge { get; init; }
        public string name { get; init; }
        public Message msg { get; init; }
        public string text { get; init; }
        [JsonProperty("rid")]
        public string roomId { get; init; }
        public User sender { get; init; }
        public string type { get; init; }
        public string appName { get; init; }
    }

    public record PollStatus
    {
        public string status { get; set; }
    }
    #endregion
}
