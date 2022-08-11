using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IContextService
    {
        IServerApiService GetServerApi(string actor);
        void CreateContext(Credentials credentials);
        void RemoveContext(string actor);
    }
    public class ContextService : IContextService
    {
        private readonly ConcurrentDictionary<string, IRemoteService> _services = new ConcurrentDictionary<string, IRemoteService>();
        private IConnectionService _connectionService;
        private IRemoteServiceFactory _remoteServiceFactory;

        public ContextService(IConnectionService connectionService, IRemoteServiceFactory remoteServiceFactory)
        {
            _connectionService = connectionService;
            _remoteServiceFactory = remoteServiceFactory;
        }

        public void CreateContext(Credentials credentials)
        {
            lock (_services)
            {
                if (string.IsNullOrEmpty(credentials.Username))
                    throw new UnauthorizedAccessException("Access denied. The user name or password is incorrect.");

                if (_services.ContainsKey(credentials.Username))
                    return;

                var httpClient = _connectionService.Connect(credentials);
                var apiService = _remoteServiceFactory.CreateRemoteService(httpClient);
                _services[credentials.Username] = apiService;
            }
        }

        public void RemoveContext(string actor)
        {
            lock (_services)
            {
                if (_services.Remove(actor, out var remoteService))
                    remoteService.Dispose();
            }
        }
        public IServerApiService GetServerApi(string actor)
        {
            var apiService = GetRemoteService(actor);
            return apiService.ServerApi;
        }

        private IRemoteService GetRemoteService(string actor)
        {
            lock (_services)
            {
                _services.TryGetValue(actor, out var apiService);

                if (apiService == null)
                    throw new UnauthorizedAccessException();

                if (!apiService.IsActive)
                {
                    throw new UnauthorizedAccessException();
                }

                return apiService;
            }
        }
    }
}
