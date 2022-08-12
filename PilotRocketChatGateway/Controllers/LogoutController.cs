using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public LogoutController(IContextService context, ILogger<LogoutController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Authorize]
        [HttpPost]
        public object Post()
        {
            var actor = HttpContext.GetTokenActor();
            _context.RemoveContext(actor);
            _logger.Log(LogLevel.Information, $"Signed out successfully. Username: {actor}.");
            return new { success = true };
        }
    }
}
