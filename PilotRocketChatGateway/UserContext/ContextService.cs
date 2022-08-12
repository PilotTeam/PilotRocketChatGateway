using PilotRocketChatGateway.PilotServer;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;

namespace PilotRocketChatGateway.UserContext
{
    public interface IContextService
    {
        IContext GetContext(string actor);
        void CreateContext(Credentials credentials);
        void RemoveContext(string actor);
    }

    public class ContextService : IContextService
    {
        private readonly ConcurrentDictionary<string, IContext> _services = new ConcurrentDictionary<string, IContext>();
        private readonly IConnectionService _connectionService;
        private readonly IContextFactory _contextFactory;

        public ContextService(IConnectionService connectionService, IContextFactory contextFactory)
        {
            _connectionService = connectionService;
            _contextFactory = contextFactory;
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
                var context = _contextFactory.CreateContext(httpClient);

                _services[credentials.Username] = context;
            }
        }

        public void RemoveContext(string actor)
        {
            lock (_services)
            {
                if (_services.Remove(actor, out var context))
                {
                    context.Dispose();
                }
            }
        }

        public IContext GetContext(string actor)
        {
            lock (_services)
            {
                _services.TryGetValue(actor, out var context);

                if (context == null)
                    throw new UnauthorizedAccessException();

                return context;
            }
        }
    }
}
