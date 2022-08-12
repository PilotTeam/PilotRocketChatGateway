using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace PilotRocketChatGateway.Controllers.WebSockets
{
    public class WebSocketsProcessor
    {
        private ILogger<WebSocketsController> _logger;
        private WebSocket _webSocket;

        public WebSocketsProcessor(WebSocket webSocket, ILogger<WebSocketsController> logger)
        {
            _logger = logger;
            _webSocket = webSocket;
        }

        public async Task ProcessAsync()
        {
            var buffer = new byte[1024 * 4];
            while (true)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.CloseStatus.HasValue)
                {
                    await _webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    _logger.Log(LogLevel.Information, "WebSocket connection closed");
                    return;
                }
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                _logger.Log(LogLevel.Information, json);
                try
                {
                    var request = JsonConvert.DeserializeObject<WebSocketRequest>(json);
                    await HandleRequestAsync(request);
                }
                catch(Exception e)
                {
                 //   _logger.Log(LogLevel.Information, $"WebSocket request is failed. Username: {user.user}.");
                   // _logger.LogError(0, e, e.Message);
                }
            }
        }

        private async Task HandleRequestAsync(WebSocketRequest request)
        {
            switch (request.msg)
            {
                case "connect":
                    var result = new WebSocketResult() { id = request.id, msg = "connected" };
                    await SendResult(result);
                    break;
                case "ping":
                    result = new WebSocketResult() { id = request.id, msg = "pong" };
                    await SendResult(result);
                    break;
                case "method":
                    await HandleMethodRequestAsync(request);
                    break;
            }
        }

        private async Task HandleMethodRequestAsync(WebSocketRequest request)
        {
            switch (request.method)
            {
                case "login":
                    var result = new WebSocketResult()
                    {
                        id = request.id,
                        msg = "result",
                    };

                    await SendResult(result);
                    break;
            }
        }

        private async Task SendResult(WebSocketResult result)
        {
            var json = JsonConvert.SerializeObject(result);
            var send = Encoding.UTF8.GetBytes(json);
            ArraySegment<byte> toSend = new ArraySegment<byte>(send, 0, send.Length);
            await _webSocket.SendAsync(toSend, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
