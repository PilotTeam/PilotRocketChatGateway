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
        private IContextService _context;
        private ILogger<LogoutController> _logger;
        private IAuthHelper _authHelper;

        public LogoutController(IContextService context, ILogger<LogoutController> logger, IAuthHelper authHelper)
        {
            _context = context;
            _logger = logger;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpPost]
        public object Post()
        {
            var actor = HttpContext.GetTokenActor(_authHelper);
            _context.RemoveContext(actor);
            _logger.Log(LogLevel.Information, $"Signed out successfully. Username: {actor}.");
            return new { success = true };
        }
    }
}
