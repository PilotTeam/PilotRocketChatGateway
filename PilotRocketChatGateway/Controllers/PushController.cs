using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class PushController : ControllerBase
    {
        private readonly ILogger<PushController> _logger;

        public PushController(ILogger<PushController> logger)
        {
            _logger = logger;
        }
        [Authorize]
        [HttpPost("api/v1/push.token")]
        public string Token(object request)
        {
            _logger.Log(LogLevel.Information, $"push token: {request.ToString()}");
            return string.Empty;
        }
    }
}
