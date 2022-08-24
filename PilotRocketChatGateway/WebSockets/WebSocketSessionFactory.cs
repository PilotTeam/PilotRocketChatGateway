using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.UserContext;
using System.Net.WebSockets;

namespace PilotRocketChatGateway.WebSockets
{
    public interface IWebSocketSessionFactory
    {
        IWebSocketSession CreateWebSocketSession(dynamic request, AuthSettings authSettings, IChatService chatService, WebSocket webSocket);
    }
    public class WebSocketSessionFactory : IWebSocketSessionFactory
    {
        public IWebSocketSession CreateWebSocketSession(dynamic request, AuthSettings authSettings, IChatService chatService, WebSocket webSocket)
        {
           return new WebSocketSession(request, authSettings, chatService, webSocket);
        }
    }
}
