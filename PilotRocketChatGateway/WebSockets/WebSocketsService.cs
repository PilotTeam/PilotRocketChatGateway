using Ascon.Pilot.DataClasses;
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
        WebSocketState State { get; }
        UserStatuses RCPresenceStatus { get; }
    }

    public interface IWebSocketAgent
    {
        INPerson CurrentPerson { get; }
        IChatService ChatService { get; }
        void AddWebSocketService(WebSocketsService service);
        void RemoveWebSocketService(WebSocketsService service);
    }

    public interface IWebSocketAgentFactory
    {
        IWebSocketAgent Create(string authToken);
    }

    public class WebSocketsService : IWebSocksetsService
    {
        private readonly ILogger<WebSocketController> _logger;
        private readonly WebSocket _webSocket;
        private readonly AuthSettings _authSettings;
        private readonly IWebSocketSessionFactory _webSocketSessionFactory;
        private readonly IAuthHelper _authHelper;
        private readonly IWebSocketAgentFactory _agentFactory;
        private IWebSocketAgent _agent;

        public WebSocketsService(WebSocket webSocket, ILogger<WebSocketController> logger, AuthSettings authSettings, IWebSocketSessionFactory webSocketSessionFactory, IAuthHelper authHelper, IWebSocketAgentFactory agentFactory)
        {
            _logger = logger;
            _webSocket = webSocket;
            _authSettings = authSettings;
            _webSocketSessionFactory = webSocketSessionFactory;
            _authHelper = authHelper;
            _agentFactory = agentFactory;
        }

        public IWebSocketSession Session { get; private set; }

        public UserStatuses RCPresenceStatus { get; private set; }
        public bool IsActive { get; private set; }

        public WebSocketState State => _webSocket.State;


        public async Task ProcessAsync()
        {
            var buffer = new byte[1024 * 4];
            while (true)
            {
                WebSocketReceiveResult result = null;
                try
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                catch(WebSocketException) 
                {
                    await CloseWebSocketAsync();
                    return;
                }
                if (result.CloseStatus.HasValue)
                {
                    await CloseWebSocketAsync(result.CloseStatus.Value);
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
                    HandleSubRequest(request);
                    break;
                case "unsub":
                    HandleUnsubRequest(request);
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
                case Streams.STREAM_NOTIFY_ROOM:
                    SendTypingMessageToServer(request);
                    break;
                case "setUserStatus":
                    SetStatus(request);
                    break;
                case $"UserPresence:{nameof(UserStatuses.away)}":
                    RCPresenceStatus = UserStatuses.away;
                    break;
                case $"UserPresence:{nameof(UserStatuses.online)}":
                    RCPresenceStatus = UserStatuses.online;
                    break;
            }
        }

        private void SetStatus(dynamic request)
        {
            var result = new
            {
                request.id,
                msg = "result"
            };
            _webSocket.SendResultAsync(result);
        }
        private void HandleSubRequest(dynamic request)
        {
            if (Session == null || !IsActive)
                throw new UnauthorizedAccessException();

            Session.Subscribe(request);
        }

        private void HandleUnsubRequest(dynamic request)
        {
            if (Session == null || !IsActive)
                throw new UnauthorizedAccessException();

            Session.Unsubscribe(request);
        }

        private Task LoginAsync(dynamic request)
        {
            if (Session != null)
                return Task.CompletedTask;

            _agent = _agentFactory.Create(request.@params[0].resume);
            _agent.AddWebSocketService(this);
            RCPresenceStatus = UserStatuses.online;
            Session = _webSocketSessionFactory.CreateWebSocketSession(request, _authSettings, _agent.ChatService, _agent.CurrentPerson, _authHelper, _webSocket);
            var result = new
            {
                request.id,
                msg = "result"
            };
            return _webSocket.SendResultAsync(result);
        }


        private void SendTypingMessageToServer(dynamic request)
        {
            var eventParam = request.@params[0].Split('/');
            if (eventParam.Length != 2 || eventParam[1] != "typing")
                return;

            var isTyping = request.@params[2];
            if (isTyping == false)
                return;

            _agent.ChatService.DataSender.SendTypingMessageToServer(eventParam[0]);
        }
        private Task CloseWebSocketAsync(WebSocketCloseStatus? status = null)
        {
            if (IsActive == false)
                return Task.CompletedTask;

            IsActive = false;

            _agent?.RemoveWebSocketService(this);
            Session?.Dispose();
            if (_agent != null)
                _logger.Log(LogLevel.Information, $"Closed websocket. Username: {_agent.CurrentPerson.Login}.");

            if (status != null)
                return _webSocket.CloseAsync(status.Value, string.Empty, CancellationToken.None);

            _webSocket.Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        { }
    }
}
