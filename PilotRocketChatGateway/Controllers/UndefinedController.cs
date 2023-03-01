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
        private IContextsBank _contextsBank;
        private IAuthHelper _authHelper;

        public UndefinedController(IContextsBank contextsBank, IAuthHelper authHelper)
        {
            _contextsBank = contextsBank;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpGet("api/v1/undefined.members")]
        public string Members(string roomId)
        {
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            var users = context.ChatService.DataLoader.LoadMembers(roomId);
            var result = new { success = true, members = users };
            return JsonConvert.SerializeObject(result);
        }
    }
}
