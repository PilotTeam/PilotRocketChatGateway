using Ascon.Pilot.DataClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class SendMessageController : ControllerBase
    {
        private readonly IContextService _contextService;
        public SendMessageController(IContextService contextService)
        {
            _contextService = contextService;
        }

        [Authorize]
        [HttpPost("api/v1/chat.sendMessage")]
        public void Post(object request)
        {
            var message = JsonConvert.DeserializeObject<MessageRequest>(request.ToString()).message;
            var context = _contextService.GetContext(HttpContext.GetTokenActor());
            context.ChatService.SendMessageToServer(MessageType.TextMessage, Guid.Parse(message.roomId), message.msg);
        }

        [Authorize]
        [HttpPost("/api/v1/subscriptions.read")]
        public void PostRead(object request)
        {
            var room = JsonConvert.DeserializeObject<RoomRequest>(request.ToString());
            var context = _contextService.GetContext(HttpContext.GetTokenActor());
            context.ChatService.SendMessageToServer(MessageType.MessageRead, Guid.Parse(room.roomId), string.Empty);
        }
    }
}
