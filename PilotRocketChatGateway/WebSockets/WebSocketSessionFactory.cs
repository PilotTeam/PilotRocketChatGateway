using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using System.Net.WebSockets;

namespace PilotRocketChatGateway.WebSockets
{
    public interface IWebSocketSessionFactory
    {
        IWebSocketSession CreateWebSocketSession(dynamic request, AuthSettings authSettings, IChatService chatService, IServerApiService serverApi, IAuthHelper authHelper, WebSocket webSocket);
    }
    public class WebSocketSessionFactory : IWebSocketSessionFactory
    {
        public IWebSocketSession CreateWebSocketSession(dynamic request, AuthSettings authSettings, IChatService chatService, IServerApiService serverApi, IAuthHelper authHelper, WebSocket webSocket)
        {
           return new WebSocketSession(request, authSettings, chatService, serverApi, authHelper, webSocket);
        }
    }
}
