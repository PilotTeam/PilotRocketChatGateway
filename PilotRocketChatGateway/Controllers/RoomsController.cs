using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private IContextService _contextService;
        private IAuthHelper _authHelper;

        public RoomsController(IContextService contextService, IAuthHelper authHelper)
        {
            _contextService = contextService;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpGet("api/v1/rooms.get")]
        public string Get()
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor(_authHelper));
            var rooms = context.ChatService.LoadRooms();

            var result = new Rooms() { success = true, update = rooms, remove = new List<Room>() };
            return JsonConvert.SerializeObject(result);
        }
        [Authorize]
        [HttpPost("api/v1/rooms.upload/{roomId}")]
        public string Upload(string roomId)
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor(_authHelper));
            var file = HttpContext.Request.Form.Files[0];
            var text = HttpContext.Request.Form["description"];
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                context.ChatService.SendAttachmentMessageToServer(roomId, file.FileName, ms.ToArray(), text);

                var result = new MessageRequest()
                {
                    success = true
                };
                return JsonConvert.SerializeObject(result);
            }
        }
    }
}
