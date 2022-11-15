using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace PilotRocketChatGateway.WebSockets
{
    public static class WebSocketExtentions
    {
        public static Task SendResultAsync(this WebSocket webSocket, dynamic result)
        {
            var json = JsonConvert.SerializeObject(result);
            var send = Encoding.UTF8.GetBytes(json);
            ArraySegment<byte> toSend = new ArraySegment<byte>(send, 0, send.Length);
            return webSocket.SendAsync(toSend, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
