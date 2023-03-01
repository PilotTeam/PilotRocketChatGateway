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
        void CreateContext(UserData credentials);
        void RemoveContext(string actor);
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

        public void CreateContext(UserData credentials)
        {
            lock (_contexts)
            {
                if (_contexts.TryGetValue(credentials.Username, out var old))
                    old?.Dispose();                

                var context = _contextFactory.CreateContext(credentials, _connectionService, _logger, _pushConnector);
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
