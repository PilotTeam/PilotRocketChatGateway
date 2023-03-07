using Ascon.Pilot.DataClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class UsersController : ControllerBase
    {
        private IContextsBank _contextsBank;
        private IAuthHelper _authHelper;

        public UsersController(IContextsBank contextsBank, IAuthHelper authHelper)
        {
            _contextsBank = contextsBank;
            _authHelper = authHelper; 
        }
        [Authorize]
        [HttpGet("api/v1/users.presence")]
        public string Presence(string ids)
        {
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));

            var users = new List<User>();
            foreach (var id in ids.Split(',').Select(x => int.Parse(x)))
            {
                var user = context.ChatService.DataLoader.LoadUser(id);
                users.Add(user);
            }

            var result = new { success = true, full = false, users = users };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpGet("api/v1/users.info")]
        public string Info(int userId)
        {
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            var user = context.ChatService.DataLoader.LoadUser(userId);

            var result = new { success = true, user = user };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpPost("api/v1/users.setStatus")]
        public string SetStatus(object request)
        {
            var result = new { success = true };
            return JsonConvert.SerializeObject(result);
        }
    }
}
