using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class E2EController : ControllerBase
    {
        private readonly IContextsBank _contextsBank;
        private readonly IAuthHelper _authHelper;
        public E2EController(IContextsBank contextsBank, IAuthHelper authHelper)
        {
            _contextsBank = contextsBank;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpPost("api/v1/e2e.setRoomKeyID")]
        public string SetRoomKeyId(object request)
        {
            var result = new Messages() { success = true };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpGet("api/v1/e2e.getUsersOfRoomWithoutKey")]
        public string GetUsersOfRoomWithoutKey(string rid)
        {
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            var users = context.ChatService.DataLoader.LoadMembers(rid);
            var result = new { success = true, users = users };
            return JsonConvert.SerializeObject(result);
        }
    }
}
