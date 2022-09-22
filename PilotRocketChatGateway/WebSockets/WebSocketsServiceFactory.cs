using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.Controllers;
using PilotRocketChatGateway.UserContext;
using System.Net.WebSockets;

namespace PilotRocketChatGateway.WebSockets
{
    public interface IWebSocketsServiceFactory
    {
        IWebSocksetsService CreateWebSocketsService(WebSocket webSocket, ILogger<WebSocketController> logger, AuthSettings authSettings, IContextService contextService, IWebSocketSessionFactory webSocketSessionFactory, IAuthHelper authHelper);
    }
    public class WebSocketsServiceFactory : IWebSocketsServiceFactory
    {
        public IWebSocksetsService CreateWebSocketsService(WebSocket webSocket, ILogger<WebSocketController> logger, AuthSettings authSettings, IContextService contextService, IWebSocketSessionFactory webSocketSessionFactory, IAuthHelper authHelper)
        {
            return new WebSocketsService(webSocket, logger, authSettings, contextService, webSocketSessionFactory, authHelper);
        }
    }
}
