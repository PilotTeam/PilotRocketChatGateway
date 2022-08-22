using Ascon.Pilot.DataClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IContextService _contextService;
        public ChatController(IContextService contextService)
        {
            _contextService = contextService;
        }

        [Authorize]
        [HttpGet("api/v1/chat.syncMessages")]
        public string SyncMessages(string roomId, string lastUpdate) //TODO to use lastUpdate
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor());
            var msgs = context.ChatService.LoadUnreadMessages(Guid.Parse(roomId));
            var res = new MessagesUpdated()
            {
                updated = msgs,
                deleted = new List<Message>(),
                success = true
            };
            return JsonConvert.SerializeObject(res);
        }

        [Authorize]
        [HttpPost("api/v1/chat.sendMessage")]
        public string SendMessage(object request)
        {
            var message = JsonConvert.DeserializeObject<MessageRequest>(request.ToString()).message;
            var context = _contextService.GetContext(HttpContext.GetTokenActor());
            var msg = context.ChatService.SendMessageToServer(MessageType.TextMessage, Guid.Parse(message.roomId), message.msg);
            var result = new MessageRequest()
            {
                message = msg,
                success = true
            };
            return JsonConvert.SerializeObject(result);
        }
    }
}
