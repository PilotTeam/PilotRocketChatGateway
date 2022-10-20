using Ascon.Pilot.DataClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.Controllers;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Text;

namespace PilotRocketChatGateway.WebSockets
{
    public interface IWebSocksetsService : IService
    {
        Task ProcessAsync();
        IWebSocketSession Session { get; }
        bool IsActive { get; }
    }
    public class WebSocketsService : IWebSocksetsService
    {
        private readonly ILogger<WebSocketController> _logger;
        private readonly WebSocket _webSocket;
        private readonly AuthSettings _authSettings;
        private readonly IContextService _contextService;
        private readonly IWebSocketSessionFactory _webSocketSessionFactory;
        private readonly IAuthHelper _authHelper;

        public WebSocketsService(WebSocket webSocket, ILogger<WebSocketController> logger, AuthSettings authSettings, IContextService contextService, IWebSocketSessionFactory webSocketSessionFactory, IAuthHelper authHelper)
        {
            _logger = logger;
            _webSocket = webSocket;
            _authSettings = authSettings;
            _contextService = contextService;
            _webSocketSessionFactory = webSocketSessionFactory;
            _authHelper = authHelper;
        }

        public IWebSocketSession Session { get; private set; }

        public bool IsActive { get; private set; }

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
                    dynamic request = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
                    await HandleRequestAsync(request);
                }
                catch (Exception e)
                {
                    //   _logger.Log(LogLevel.Information, $"WebSocket request is failed. Username: {user.user}.");
                    _logger.LogError(0, e, e.Message);
                }
            }
        }

        private async Task HandleRequestAsync(dynamic request)
        {
            switch (request.msg)
            {
                case "connect":
                    var result = new { msg = "connected" };
                    await _webSocket.SendResultAsync(result);
                    IsActive = true;
                    break;
                case "ping":
                    result = new { msg = "pong" };
                    await _webSocket.SendResultAsync(result);
                    break;
                case "method":
                    await HandleMethodRequestAsync(request);
                    break;
                case "sub":
                    await HandleSubRequestAsync(request);
                    break;
                case "unsub":
                    await HandleUnsubRequestAsync(request);
                    break;
            }
        }
        private async Task HandleMethodRequestAsync(dynamic request)
        {
            switch (request.method)
            {
                case "login":
                    await LoginAsync(request);
                    break;
            }
        }
        private async Task HandleSubRequestAsync(dynamic request)
        {
            if (Session == null || !IsActive)
                throw new UnauthorizedAccessException();

            Session.Subscribe(request);
        }

        private async Task HandleUnsubRequestAsync(dynamic request)
        {
            if (Session == null || !IsActive)
                throw new UnauthorizedAccessException();

            Session.Unsubscribe(request);
        }

        private async Task LoginAsync(dynamic request)
        {
            if (Session != null)
                return;

            var context = RegisterService(request.@params[0].resume);
            Session = _webSocketSessionFactory.CreateWebSocketSession(request, _authSettings, context.ChatService, context.RemoteService.ServerApi, _authHelper, _webSocket);
            var result = new
            {
                request.id,
                msg = "result"
            };
            await _webSocket.SendResultAsync(result);
        }

        private IContext RegisterService(string authToken)
        {
            var jwtToken = new JwtSecurityToken(authToken);
            var context = _contextService.GetContext(jwtToken.Actor);
            context.SetService(this);
            return context;
        }
        private async Task CloseAsync(WebSocketCloseStatus status)
        {
            if (IsActive == false)
                return;

            IsActive = false;
            await _webSocket.CloseAsync(status, string.Empty, CancellationToken.None);
            _logger.Log(LogLevel.Information, "WebSocket connection closed");
        }

        public async void Dispose()
        {
            try
            {
                await CloseAsync(WebSocketCloseStatus.NormalClosure);
            }
            catch(Exception e)
            {
                _logger.Log(LogLevel.Information, "Failed to close websocket");
                _logger.LogError(0, e, e.Message);
            }
            Session?.Dispose();
            _webSocket.Dispose();
        }
    }
}
