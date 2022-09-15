using Ascon.Pilot.DataClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class ImController : ControllerBase
    {
        private IContextService _contextService;

        public ImController(IContextService contextService)
        {
            _contextService = contextService;
        }

        [Authorize]
        [HttpPost("api/v1/im.create")]
        public string Create(object request)
        {
            var user = JsonConvert.DeserializeObject<User>(request.ToString());
            var context = _contextService.GetContext(HttpContext.GetTokenActor());


            var room = context.ChatService.LoadPersonalRoom(user.username);
            if (room != null)
            {
                var result = new { room = room, success = true };
                return JsonConvert.SerializeObject(result);
            }    

            room = context.ChatService.CreateChat(new List<string>() { user.username }, ChatKind.Personal);
            var result1 = new { room = room, success = true };
            return JsonConvert.SerializeObject(result1);
        }

        [Authorize]
        [HttpGet("api/v1/im.history")]
        public string History(string roomId, int count)
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor());
            var msgs = context.ChatService.LoadMessages(roomId, count);

            var result = new Messages() { success = true, messages = msgs };
            return JsonConvert.SerializeObject(result);
        }
    }
}
