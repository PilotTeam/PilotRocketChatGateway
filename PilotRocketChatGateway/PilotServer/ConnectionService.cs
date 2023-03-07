﻿using Ascon.Pilot.Server.Api;
using Microsoft.Extensions.Options;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IConnectionService
    {
        HttpPilotClient Connect(UserData credentials);
    }
    public class ConnectionService : IConnectionService
    {
        private readonly PilotSettings _config;
        public ConnectionService(IOptions<PilotSettings> config)
        {
            _config = config.Value;
        }

        public HttpPilotClient Connect(UserData credentials)
        {
            var client = new HttpPilotClient(_config.Url);
            // Do not check versions of the Server and Client
            client.Connect(false);

            var authApi = client.GetAuthenticationApi();
            authApi.Login(_config.Database, credentials.Username, credentials.ProtectedPassword, false, _config.LicenseCode);
            return client;
        }
    }
}
