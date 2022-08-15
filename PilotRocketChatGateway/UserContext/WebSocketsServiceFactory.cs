using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.Controllers;
using System.Net.WebSockets;

namespace PilotRocketChatGateway.UserContext
{
    public interface IWebSocketsServiceFactory
    {
        IWebSocksetsService CreateWebSocketsService(WebSocket webSocket, ILogger<WebSocketsController> logger, AuthSettings authSettings, IContextService contextService);
    }
    public class WebSocketsServiceFactory : IWebSocketsServiceFactory
    {
        public IWebSocksetsService CreateWebSocketsService(WebSocket webSocket, ILogger<WebSocketsController> logger, AuthSettings authSettings, IContextService contextService)
        {
            return new WebSocketsService(webSocket, logger, authSettings, contextService);
        }
    }
}
