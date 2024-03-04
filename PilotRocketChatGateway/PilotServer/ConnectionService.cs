using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api;
using Microsoft.Extensions.Options;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IConnectionService
    {
        Task<HttpPilotClient> ConnectAsync(UserData credentials);
    }
    public class ConnectionService : IConnectionService
    {
        const string SELF_IDENTITY = "Pilot-Rocket.Chat";
        private readonly PilotSettings _config;
        public ConnectionService(IOptions<PilotSettings> config)
        {
            _config = config.Value;
        }

        public async Task<HttpPilotClient> ConnectAsync(UserData credentials)
        {
            var client = new HttpPilotClient(_config.Url);
            // Do not check versions of the Server and Client
            client.Connect(false);

            var authApi = client.GetAuthenticationAsyncApi();
            await authApi.LoginWithIdentityAsync(_config.Database, credentials.Username, credentials.ProtectedPassword, _config.LicenseCode, new SelfIdentity() { Id = SELF_IDENTITY });
            return client;
        }
    }
}
