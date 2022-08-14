using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using System.Net.WebSockets;
using System.Text;

namespace PilotRocketChatGateway.Controllers.WebSockets
{
    [ApiController]
    [Route("[controller]")]
    public class WebSocketsController : ControllerBase
    {
        private readonly ILogger<WebSocketsController> _logger;
        private readonly AuthSettings _authSettings;

        public WebSocketsController(ILogger<WebSocketsController> logger, IOptions<AuthSettings> authSettings)
        {
            _logger = logger;
            _authSettings = authSettings.Value;
        }

        [HttpGet("/websocket")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                _logger.Log(LogLevel.Information, "WebSocket connection established");
                await new WebSocketsProcessor(webSocket, _logger, _authSettings).ProcessAsync();
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }
    }
}
