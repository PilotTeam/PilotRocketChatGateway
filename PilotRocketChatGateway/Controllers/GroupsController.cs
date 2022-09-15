using Ascon.Pilot.DataClasses;
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
            var msgs = context.ChatService.LoadMessages(roomId, count);

            var result = new Messages() { success = true, messages = msgs };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpPost("api/v1/groups.create")]
        public string Create(object request)
        {
            var group = JsonConvert.DeserializeObject<GroupRequest>(request.ToString());
            var context = _contextService.GetContext(HttpContext.GetTokenActor());

            var created = context.ChatService.CreateChat(group.name, group.members, ChatKind.Group);
            var result = new { group = created, success = true };
            return JsonConvert.SerializeObject(result);
        }
    }
}
