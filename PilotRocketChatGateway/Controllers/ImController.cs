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
    public class ImController : ControllerBase
    {
        private IContextsBank _contextsBank;
        private IAuthHelper _authHelper;

        public ImController(IContextsBank contextsBank, IAuthHelper authHelper)
        {
            _contextsBank = contextsBank;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpPost("api/v1/im.create")]
        public string Create(object request)
        {
            var user = JsonConvert.DeserializeObject<User>(request.ToString());
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));


            var room = context.ChatService.DataLoader.LoadPersonalRoom(user.username);
            if (room != null)
            {
                var result = new { room = room, success = true };
                return JsonConvert.SerializeObject(result);
            }

            room = context.ChatService.DataSender.SendChatCreationMessageToServer(string.Empty, new List<string>() { user.username }, ChatKind.Personal);
            var result1 = new { room = room, success = true };
            return JsonConvert.SerializeObject(result1);
        }

        [Authorize]
        [HttpGet("api/v1/im.history")]
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
        [HttpGet("api/v1/im.files")]
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
