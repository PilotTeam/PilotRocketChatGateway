using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class UndefinedController : ControllerBase
    {
        private IContextService _contextService;
        private IAuthHelper _authHelper;

        public UndefinedController(IContextService contextService, IAuthHelper authHelper)
        {
            _contextService = contextService;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpGet("api/v1/undefined.members")]
        public string Members(string roomId)
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor(_authHelper));
            var users = context.ChatService.LoadMembers(roomId);
            var result = new { success = true, members = users };
            return JsonConvert.SerializeObject(result);
        }
    }
}
