namespace PilotRocketChatGateway.PilotServer
{
    public record PilotSettings
    {
        public string Url { get; init; }
        public string Database { get; init; }
        public int LicenseCode { get; init; }
    }
}
