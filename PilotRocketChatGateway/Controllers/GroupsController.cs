using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{

    [ApiController]
    public class GroupsController : ControllerBase
    {
        private readonly IContextService _contextService;
        public GroupsController(IContextService contextService)
        {
            _contextService = contextService;
        }

        [Authorize]
        [HttpGet("api/v1/groups.history")]
        public string History(string roomId, int count)
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor());
            var msgs = context.ChatService.LoadMessages(Guid.Parse(roomId), count);

            var result = new Messages() { success = true, messages = msgs };
            return JsonConvert.SerializeObject(result);
        }
    }
}
