﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.WebSockets;
using System.Net.WebSockets;
using System.Text;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WebSocketController : ControllerBase
    {
        private readonly ILogger<WebSocketController> _logger;
        private readonly AuthSettings _authSettings;
        private IContextsBank _contextsBank;
        private readonly IWebSocketsServiceFactory _webSocketsServiceFactory;
        private readonly IWebSocketSessionFactory _webSocketSessionFactory;
        private readonly IAuthHelper _authHelper;

        public WebSocketController(ILogger<WebSocketController> logger, IOptions<AuthSettings> authSettings, IContextsBank contextsBank, IWebSocketsServiceFactory webSocketsServiceFactory, IWebSocketSessionFactory webSocketSessionFactory, IAuthHelper authHelper)
        {
            _logger = logger;
            _authSettings = authSettings.Value;
            _contextsBank = contextsBank;
            _webSocketsServiceFactory = webSocketsServiceFactory;
            _webSocketSessionFactory = webSocketSessionFactory;
            _authHelper = authHelper;
        }

        [HttpGet]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                _logger.Log(LogLevel.Information, "WebSocket connection established");

                var agentFactory = new WebSocketAgentFactory(_contextsBank);
                var webSocketService = _webSocketsServiceFactory.CreateWebSocketsService(webSocket, _logger, _authSettings, _webSocketSessionFactory, _authHelper, agentFactory);
                await webSocketService.ProcessAsync();
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }
    }
}
