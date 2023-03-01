using Ascon.Pilot.DataClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.Utils;
using System.Web;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IContextsBank _contextsBank;
        private readonly IAuthHelper _authHelper;

        public ChatController(IContextsBank contextsBank, IAuthHelper authHelper)
        {
            _contextsBank = contextsBank;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpGet("api/v1/chat.syncMessages")]
        public string SyncMessages(string roomId, string lastUpdate) 
        {
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            var msgs = context.ChatService.DataLoader.LoadMessages(roomId, lastUpdate);
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
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
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
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            context.ChatService.DataSender.SendEditMessageToServer(message.roomId, message.msgId, message.text);
            var result = new MessageRequest()
            {
                success = true
            };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpGet("api/v1/chat.getMessage")]
        public string GetMessage(string msgId)
        {
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            var msg = context.ChatService.DataLoader.LoadMessage(msgId);
            var res = new
            {
                message = msg,
                success = true
            };
            return JsonConvert.SerializeObject(res);

        }
    }
}
