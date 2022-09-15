using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class UsersController : ControllerBase
    {
        private IContextService _contextService;

        public UsersController(IContextService contextService)
        {
            _contextService = contextService;
        }
        [Authorize]
        [HttpGet("api/v1/users.presence")]
        public string Presence(string ids)
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor());

            var users = new List<User>();
            foreach (var id in ids.Split(',').Select(x => int.Parse(x)))
            {
                var user = context.ChatService.LoadUser(id);
                users.Add(user);
            }

            var result = new { success = true, full = false, users = users };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpGet("api/v1/users.info")]
        public string Info(int userId)
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor());
            var user = context.ChatService.LoadUser(userId);

            var result = new { success = true, user = user };
            return JsonConvert.SerializeObject(result);
        }
    }
}
