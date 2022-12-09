using System.Collections.Concurrent;

namespace PilotRocketChatGateway.WebSockets
{
    public interface IWebSocketBank
    {
        void RegisterWebSocketService(string user, IWebSocksetsService service);
        void RemoveWebSocketService(string user, IWebSocksetsService service);
        ConcurrentDictionary<int, IWebSocksetsService> GetServises(string user);
    }
    public class WebSocketBank : IWebSocketBank
    {
        private IWebSocketsWatcher _webSocketsWatcher;
        private ConcurrentDictionary<string, ConcurrentDictionary<int, IWebSocksetsService>> _services;
        public WebSocketBank(IWebSocketsWatcher webSocketsWatcher)
        {
            _webSocketsWatcher = webSocketsWatcher;
            _services = new ConcurrentDictionary<string, ConcurrentDictionary<int, IWebSocksetsService>>();
            _webSocketsWatcher.Watch(_services);
        }

        public ConcurrentDictionary<int, IWebSocksetsService> GetServises(string user)
        {
            _services.TryGetValue(user, out var websockets);
            return websockets;
        }

        public void RegisterWebSocketService(string user, IWebSocksetsService service)
        {
            if (_services.TryGetValue(user, out var websockets) == false)
            {
                websockets = new ConcurrentDictionary<int, IWebSocksetsService>();
                _services[user] = websockets;
            }

            websockets[service.GetHashCode()] = service;
        }
        public void RemoveWebSocketService(string user, IWebSocksetsService service)
        {
            if (_services.TryGetValue(user, out var websockets) == false)
                return;

            websockets.Remove(service.GetHashCode(), out _);
        }
    }
}
