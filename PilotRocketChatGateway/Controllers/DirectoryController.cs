using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DirectoryController : ControllerBase
    {
        private IContextService _contextService;

        public DirectoryController(IContextService contextService)
        {
            _contextService = contextService;
        }

        [Authorize]
        public string Get(string query)
        {
            var requset = JsonConvert.DeserializeObject<DirectoryRequest>(query);
            if (requset.type != "users")
                return string.Empty;

            var context = _contextService.GetContext(HttpContext.GetTokenActor());
            var users = context.ChatService.LoadUsers(requset.count);

            var result = new { success = true, result = users, total = context.RemoteService.ServerApi.GetPeople().Count };
            return JsonConvert.SerializeObject(result);
        }
    }
}
