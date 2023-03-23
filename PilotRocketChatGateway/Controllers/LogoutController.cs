using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class LogoutController : ControllerBase
    {
        private IContextsBank _contextBank;
        private ILogger<LogoutController> _logger;
        private IAuthHelper _authHelper;

        public LogoutController(IContextsBank contextBank, ILogger<LogoutController> logger, IAuthHelper authHelper)
        {
            _contextBank = contextBank;
            _logger = logger;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpPost]
        public object Post()
        {
            var actor = HttpContext.GetTokenActor(_authHelper);
            var success = _contextBank.RemoveContext(actor);

            if (success)
                _logger.Log(LogLevel.Information, $"Logged out. Username: {actor}.");

            return new { success = true };
        }
    }
}
