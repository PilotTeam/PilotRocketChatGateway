using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.UserContext;
using System.IdentityModel.Tokens.Jwt;

namespace PilotRocketChatGateway.WebSockets
{
    public class WebSocketAgentFactory : IWebSocketAgentFactory
    {
        IContextsBank _contextsBank;
        public WebSocketAgentFactory(IContextsBank contextsBank) 
        {
            _contextsBank = contextsBank;
        }
        public IWebSocketAgent Create(string authToken)
        {
            var context = GetContext(authToken);
            return new WebSocketAgent(context);
        }
        private IContext GetContext(string authToken)
        {
            var jwtToken = new JwtSecurityToken(authToken);
            var context = _contextsBank.GetContext(jwtToken.Actor);
            return context;
        }
    }
    public class WebSocketAgent : IWebSocketAgent
    {
        private IContext _context;

        public WebSocketAgent(IContext context)
        {
            _context = context;
        }

        public INPerson CurrentPerson => _context.RemoteService.ServerApi.CurrentPerson;

        public IChatService ChatService => _context.ChatService;

        public void AddWebSocketService(WebSocketsService service)
        {
            _context.WebSocketsNotifyer.RegisterWebSocketService(service);
        }

        public void RemoveWebSocketService(WebSocketsService service)
        {
            if (_context.IsDisposed != false)
                _context.WebSocketsNotifyer.RemoveWebSocketService(service);
        }
    }
}
