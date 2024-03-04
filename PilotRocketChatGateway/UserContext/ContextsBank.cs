using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.Pushes;
using PilotRocketChatGateway.Utils;
using PilotRocketChatGateway.WebSockets;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;

namespace PilotRocketChatGateway.UserContext
{
    public interface IContextsBank
    {
        IContext GetContext(string actor);
        Task CreateContextAsync(UserData credentials);
        bool RemoveContext(string actor);
    }

    public class ContextsBank : IContextsBank
    {
        private readonly ConcurrentDictionary<string, IContext> _contexts = new ConcurrentDictionary<string, IContext>();
        private readonly IConnectionService _connectionService;
        private readonly IContextFactory _contextFactory;
        private readonly ILogger<ContextsBank> _logger;
        private readonly IPushGatewayConnector _pushConnector;

        public ContextsBank(IConnectionService connectionService, IContextFactory contextFactory, ILogger<ContextsBank> logger, IPushGatewayConnector pushConnector)
        {
            _connectionService = connectionService;
            _contextFactory = contextFactory;
            _logger = logger;
            _pushConnector = pushConnector;
        }

        public async Task CreateContextAsync(UserData credentials)
        {
            lock (_contexts)
            {
                if (_contexts.TryGetValue(credentials.Username, out var old))
                    old?.Dispose();
            }
        
            var context = await _contextFactory.CreateContextAsync(credentials, _connectionService, _logger, _pushConnector);
            lock (_contexts)
            { 
                _contexts[credentials.Username] = context;
            }
        }

        public bool RemoveContext(string actor)
        {
            lock (_contexts)
            {
                if (_contexts.Remove(actor, out var context))
                {
                    context.Dispose();
                    return true;
                }
                return false;
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
