using Ascon.Pilot.DataClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.Utils;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IContextService _contextService;
        private readonly IAuthHelper _authHelper;

        public ChatController(IContextService contextService, IAuthHelper authHelper)
        {
            _contextService = contextService;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpGet("api/v1/chat.syncMessages")]
        public string SyncMessages(string roomId, string lastUpdate) //TODO to use lastUpdate
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor(_authHelper));
            var msgs = context.ChatService.DataLoader.LoadUnreadMessages(roomId);
            var res = new
            {
                result = new
                {
                    updated = msgs,
                    deleted = new List<Message>(),
                },
                success = true
            };
            return JsonConvert.SerializeObject(res);
        }

        [Authorize]
        [HttpPost("api/v1/chat.sendMessage")]
        public string SendMessage(object request)
        {
            var message = JsonConvert.DeserializeObject<MessageRequest>(request.ToString()).message;
            var context = _contextService.GetContext(HttpContext.GetTokenActor(_authHelper));
            var (link, text) = MarkdownHelper.CutHyperLink(message.msg);
            context.ChatService.DataSender.SendTextMessageToServer(message.roomId, message.id, text, link);
            var result = new MessageRequest()
            {
                success = true
            };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpPost("api/v1/chat.update")]
        public string Update(object request)
        {
            var message = JsonConvert.DeserializeObject<MessageEdit>(request.ToString());
            var context = _contextService.GetContext(HttpContext.GetTokenActor(_authHelper));
            context.ChatService.DataSender.SendEditMessageToServer(message.roomId, message.msgId, message.text);
            var result = new MessageRequest()
            {
                success = true
            };
            return JsonConvert.SerializeObject(result);
        }
    }
}
