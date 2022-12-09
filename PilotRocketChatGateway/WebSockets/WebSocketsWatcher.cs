using System.Collections.Concurrent;

namespace PilotRocketChatGateway.WebSockets
{
    public interface IWebSocketsWatcher
    {
        void Watch(ConcurrentDictionary<string, ConcurrentDictionary<int, IWebSocksetsService>> services);
        void Stop();
    }
    public class WebSocketsWatcher : IWebSocketsWatcher
    {
        private readonly int _timeout = 10 * 6 * 1000; //10 min
        private readonly System.Timers.Timer _timer;
        public WebSocketsWatcher()
        {
            _timer = new System.Timers.Timer(_timeout)
            {
                Enabled = false
            };
        }
        public void Watch(ConcurrentDictionary<string, ConcurrentDictionary<int, IWebSocksetsService>> services)
        {
            _timer.Elapsed += (o, e) =>
            {
                foreach (var pair1 in services)
                    foreach (var pair2 in pair1.Value.ToArray())
                    {
                        var websockets = pair1.Value; 
                        var service = pair2.Value;

                        if (service.State != System.Net.WebSockets.WebSocketState.Open)
                        {
                            websockets.Remove(service.GetHashCode(), out _);
                            service.Dispose();
                        }
                    }
            };
            _timer.Start();
        }
        public void Stop()
        {
            _timer.Stop();
        }
    }
}
