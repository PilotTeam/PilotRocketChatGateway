namespace PilotRocketChatGateway.Pushes
{
    [Serializable]
    public class RocketChatCloudSettings
    {
        public string RegistrationToken { get; set; }
        public string WorkspaceName { get; set; }
        public string WorkspaceEmail { get; set; }
        public string WorkspaceUri { get; set; }
    }
}
