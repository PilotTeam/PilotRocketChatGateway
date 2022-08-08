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

        public async Task Process()
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
                var request = JsonConvert.DeserializeObject<WebSocketRequest>(json);
                await HandleRequest(request);
            }
        }

        private async Task HandleRequest(WebSocketRequest request)
        {
            switch (request.msg)
            {
                case "connect":
                    var result = new WebSocketRequest() { msg = "connected" };
                    await SendResult(result);
                    break;
                case "ping":
                    result = new WebSocketRequest() { msg = "pong" };
                    await SendResult(result);
               break;
            }
        }

        private async Task SendResult(WebSocketRequest result)
        {
            var json = JsonConvert.SerializeObject(result);
            var send = Encoding.UTF8.GetBytes(json);
            ArraySegment<byte> toSend = new ArraySegment<byte>(send, 0, send.Length);
            await _webSocket.SendAsync(toSend, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
