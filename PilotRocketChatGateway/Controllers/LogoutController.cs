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
        private IContextsBank _context;
        private ILogger<LogoutController> _logger;
        private IAuthHelper _authHelper;

        public LogoutController(IContextsBank context, ILogger<LogoutController> logger, IAuthHelper authHelper)
        {
            _context = context;
            _logger = logger;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpPost]
        public object Post()
        {
            return new { success = true };
        }
    }
}
