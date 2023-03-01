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
        private readonly IContextsBank _contextsBank;
        private readonly IAuthHelper _authHelper;

        public GroupsController(IContextsBank contextsBank, IAuthHelper authHelper)
        {
            _contextsBank = contextsBank;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpGet("api/v1/groups.history")]
        public string History()
        {
            string roomId;
            int count;
            string latest;

            roomId = GetParam(nameof(roomId));
            count = int.Parse(GetParam(nameof(count)));
            latest = GetParam(nameof(latest));

            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            var msgs = context.ChatService.DataLoader.LoadMessages(roomId, count, latest);
            var result = new Messages() { success = true, messages = msgs };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpGet("api/v1/groups.members")]
        public string Members(string roomId)
        {
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            var users = context.ChatService.DataLoader.LoadMembers(roomId);
            var result = new { success = true, members = users };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpPost("api/v1/groups.create")]
        public string Create(object request)
        {
            var group = JsonConvert.DeserializeObject<GroupRequest>(request.ToString());
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));

            var created = context.ChatService.DataSender.SendChatCreationMessageToServer(group.name, group.members, ChatKind.Group);
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


            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            var (files, total) = context.ChatService.DataLoader.RCDataConverter.AttachmentLoader.LoadFiles(roomId, offset);
            var result = new { files = files, success = true, count = files.Count, offset = offset, total = total };
            return JsonConvert.SerializeObject(result);
        }
        private string GetParam(string query)
        {
            return HttpUtility.ParseQueryString(HttpContext.Request.QueryString.ToString()).Get(query) ?? string.Empty;
        }
    }
}
