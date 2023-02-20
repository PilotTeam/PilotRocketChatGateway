using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.Utils;
using PilotRocketChatGateway.WebSockets;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;

namespace PilotRocketChatGateway.UserContext
{
    public interface IContextService
    {
        IContext GetContext(string actor);
        void CreateContext(UserData credentials);
        void RemoveContext(string actor);
    }

    public class ContextService : IContextService
    {
        private readonly ConcurrentDictionary<string, IContext> _contexts = new ConcurrentDictionary<string, IContext>();
        private readonly IConnectionService _connectionService;
        private readonly IContextFactory _contextFactory;
        private readonly IWebSocketBank _bank;
        private readonly ILogger<ContextService> _logger;
        private readonly IBatchMessageLoaderFactory _batchMessageLoaderFactory;

        public ContextService(IConnectionService connectionService, IContextFactory contextFactory, IWebSocketBank bank, ILogger<ContextService> logger, IBatchMessageLoaderFactory batchMessageLoaderFactory)
        {
            _connectionService = connectionService;
            _batchMessageLoaderFactory = batchMessageLoaderFactory;
            _contextFactory = contextFactory;
            _bank = bank;
            _logger = logger;
        }

        public void CreateContext(UserData credentials)
        {
            lock (_contexts)
            {
                if (_contexts.TryGetValue(credentials.Username, out var old))
                    old?.Dispose();                

                var context = _contextFactory.CreateContext(credentials, _connectionService, _bank, _logger, _batchMessageLoaderFactory);
                _contexts[credentials.Username] = context;
            }
        }

        public void RemoveContext(string actor)
        {
            lock (_contexts)
            {
                if (_contexts.Remove(actor, out var context))
                {
                    context.Dispose();
                }
            }
        }

        public IContext GetContext(string actor)
        {
            lock (_contexts)
            {
                _contexts.TryGetValue(actor, out var context);

                if (context == null)
                    throw new UnauthorizedAccessException();

                return context;
            }
        }
    }
}
