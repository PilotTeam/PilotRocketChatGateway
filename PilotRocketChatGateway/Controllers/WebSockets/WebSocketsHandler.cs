using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Text;

namespace PilotRocketChatGateway.Controllers.WebSockets
{
    public class WebSocketsProcessor
    {
        private ILogger<WebSocketsController> _logger;
        private WebSocket _webSocket;
        private AuthSettings _authSettings;
        private bool _authorized;

        public WebSocketsProcessor(WebSocket webSocket, ILogger<WebSocketsController> logger, AuthSettings authSettings)
        {
            _logger = logger;
            _webSocket = webSocket;
            _authSettings = authSettings;
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
                    var result = Login(request);
                    await SendResult(result);
                    break;
            }
        }

        private WebSocketResult Login(WebSocketRequest request)
        {
            if (ValidateCurrentToken(request.@params[0].authToken) == false)
                throw new UnauthorizedAccessException();

            _authorized = true;
            return new WebSocketResult()
            {
                id = request.id,
                msg = "result",
            };
        }

        private bool ValidateCurrentToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ValidateToken(token, AuthUtils.GetTokenValidationParameters(_authSettings), out SecurityToken validatedToken);
            }
            catch
            {
                return false;
            }
            return true;
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
