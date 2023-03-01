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
        private IContextsBank _contextsBank;
        private IAuthHelper _authHelper;

        public RoomsController(IContextsBank contextsBank, IAuthHelper authHelper)
        {
            _contextsBank = contextsBank;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpGet("api/v1/rooms.get")]
        public string Get()
        {
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            var rooms = context.ChatService.DataLoader.LoadRooms();

            var result = new Rooms() { success = true, update = rooms, remove = new List<Room>() };
            return JsonConvert.SerializeObject(result);
        }
        [Authorize]
        [HttpPost("api/v1/rooms.upload/{roomId}")]
        public string Upload(string roomId)
        {
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            var file = HttpContext.Request.Form.Files[0];
            var text = HttpContext.Request.Form["description"];
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                context.ChatService.DataSender.SendAttachmentMessageToServer(roomId, file.FileName, ms.ToArray(), text);

                var result = new MessageRequest()
                {
                    success = true
                };
                return JsonConvert.SerializeObject(result);
            }
        }
        [Authorize]
        [HttpPost("api/v1/rooms.saveNotification")]
        public string SaveNotification(object request)
        {
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));

            var setting = JsonConvert.DeserializeObject<SaveNotification>(request.ToString());
            bool isOn = setting.notifications.disableNotifications == "0";

            context.ChatService.DataSender.SendChageNotificationMessageToServer(setting.roomId, isOn);

            var result = new { success = true };
            return JsonConvert.SerializeObject(result);

        }
    }
}
