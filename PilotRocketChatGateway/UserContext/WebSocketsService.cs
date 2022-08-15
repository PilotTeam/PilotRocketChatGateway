using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.Controllers;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Text;

namespace PilotRocketChatGateway.UserContext
{
    public interface IWebSocksetsService : IDisposable
    {
        Task ProcessAsync();
        bool IsAuthorized { get; }
        bool IsActive { get; }
    }
    public class WebSocketsService : IWebSocksetsService
    {
        private readonly ILogger<WebSocketsController> _logger;
        private readonly WebSocket _webSocket;
        private readonly AuthSettings _authSettings;
        private readonly IContextService _contextService;
        private IContext _context;

        public WebSocketsService(WebSocket webSocket, ILogger<WebSocketsController> logger, AuthSettings authSettings, IContextService contextService)
        {
            _logger = logger;
            _webSocket = webSocket;
            _authSettings = authSettings;
            _contextService = contextService;
        }

        public async Task ProcessAsync()
        {
            var buffer = new byte[1024 * 4];
            while (true)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.CloseStatus.HasValue)
                {
                    await CloseAsync(result.CloseStatus.Value);
                    return;
                }
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                _logger.Log(LogLevel.Information, json);

                try
                {
                    var request = JsonConvert.DeserializeObject<WebSocketRequest>(json);
                    await HandleRequestAsync(request);
                }
                catch (Exception e)
                {
                    //   _logger.Log(LogLevel.Information, $"WebSocket request is failed. Username: {user.user}.");
                    // _logger.LogError(0, e, e.Message);
                }
            }
        }

        public bool IsAuthorized { get; private set; }

        public bool IsActive { get; private set; }

        private async Task CloseAsync(WebSocketCloseStatus status)
        {
            if (IsActive == false)
                return;

            IsActive = false;
            IsAuthorized = false;
            await _webSocket.CloseAsync(status, string.Empty, CancellationToken.None);
            _logger.Log(LogLevel.Information, "WebSocket connection closed");
        }

        private async Task HandleRequestAsync(WebSocketRequest request)
        {
            switch (request.msg)
            {
                case "connect":
                    var result = new WebSocketResult() { id = request.id, msg = "connected" };
                    await SendResult(result);
                    IsActive = true;
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
            var authToken = request.@params[0].authToken;
            if (ValidateCurrentToken(authToken) == false)
                throw new UnauthorizedAccessException();

            RegisterService(authToken);
            IsAuthorized = true;
            return new WebSocketResult()
            {
                id = request.id,
                msg = "result",
            };
        }

        private void RegisterService(string authToken)
        {
            var jwtToken = new JwtSecurityToken(authToken);
            _context = _contextService.GetContext(jwtToken.Actor);
            _context.WebSocketsService = this;

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

        public void Dispose()
        {
            CloseAsync(WebSocketCloseStatus.NormalClosure);
        }
    }
}
