using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.Controllers;
using PilotRocketChatGateway.UserContext;
using System.Net.WebSockets;

namespace PilotRocketChatGateway.WebSockets
{
    public interface IWebSocketsServiceFactory
    {
        IWebSocksetsService CreateWebSocketsService(WebSocket webSocket, ILogger<WebSocketController> logger, AuthSettings authSettings, IWebSocketSessionFactory webSocketSessionFactory, IAuthHelper authHelper, IWebSocketAgentFactory agentFactory);
    }
    public class WebSocketsServiceFactory : IWebSocketsServiceFactory
    {
        public IWebSocksetsService CreateWebSocketsService(WebSocket webSocket, ILogger<WebSocketController> logger, AuthSettings authSettings, IWebSocketSessionFactory webSocketSessionFactory, IAuthHelper authHelper, IWebSocketAgentFactory agentFactory)
        {
            return new WebSocketsService(webSocket, logger, authSettings, webSocketSessionFactory, authHelper, agentFactory);
        }
    }
}
