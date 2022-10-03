using Ascon.Pilot.DataClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using System.Web;

namespace PilotRocketChatGateway.Controllers
{

    [ApiController]
    public class GroupsController : ControllerBase
    {
        private readonly IContextService _contextService;
        private readonly IAuthHelper _authHelper;

        public GroupsController(IContextService contextService, IAuthHelper authHelper)
        {
            _contextService = contextService;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpGet("api/v1/groups.history")]
        public string History(string roomId, int count)
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor(_authHelper));
            var msgs = context.ChatService.LoadMessages(roomId, count, string.Empty);
            var result = new Messages() { success = true, messages = msgs };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpGet("api/v1/groups.members")]
        public string Members(string roomId)
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor(_authHelper));
            var users = context.ChatService.LoadMembers(roomId);
            var result = new { success = true, members = users };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpPost("api/v1/groups.create")]
        public string Create(object request)
        {
            var group = JsonConvert.DeserializeObject<GroupRequest>(request.ToString());
            var context = _contextService.GetContext(HttpContext.GetTokenActor(_authHelper));

            var created = context.ChatService.CreateChat(group.name, group.members, ChatKind.Group);
            var result = new { group = created, success = true };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpGet("api/v1/groups.files")]
        public string Files()
        {
            string roomId;
            int offset;

            roomId = GetParam(nameof(roomId));
            offset = int.Parse(GetParam(nameof(offset)));


            var context = _contextService.GetContext(HttpContext.GetTokenActor(_authHelper));
            var (files, total) = context.ChatService.LoadFiles(roomId, offset);
            var result = new { files = files, success = true, count = files.Count, offset = offset, total = total };
            return JsonConvert.SerializeObject(result);
        }
        private string GetParam(string query)
        {
            return HttpUtility.ParseQueryString(HttpContext.Request.QueryString.ToString()).Get(query) ?? string.Empty;
        }
    }
}
