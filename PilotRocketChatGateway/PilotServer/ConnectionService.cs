using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api;
using Ascon.Pilot.Server.Api.Contracts;
using Microsoft.Extensions.Options;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IConnectionService
    {
        Task<HttpPilotClient> ConnectAsync(UserData credentials);
        Task ConnectAsync(IAuthenticationAsyncApi authApi, UserData credentials);
    }
    public class ConnectionService : IConnectionService
    {
        private const string SELF_IDENTITY = "Pilot-Rocket.Chat";

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
            await ConnectAsync(authApi, credentials);
            return client;
        }

        public Task ConnectAsync(IAuthenticationAsyncApi authApi, UserData credentials)
        {
            return authApi.LoginWithIdentityAsync(_config.Database, credentials.Username, credentials.ProtectedPassword, new SelfIdentity() { Id = SELF_IDENTITY });
        }
    }
}
