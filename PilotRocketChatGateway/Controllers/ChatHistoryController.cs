using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{

    [ApiController]
    public class ChatHistoryController : ControllerBase
    {
        private readonly IContextService _contextService;
        public ChatHistoryController(IContextService contextService)
        {
            _contextService = contextService;
        }

        [Authorize]
        [HttpGet("api/v1/channels.history")]
        public string GetChannelsHistory(string roomId, int count)
        {
            var result = LoadChatHistory(roomId, count);
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpGet("api/v1/groups.history")]
        public string GetGroupsHistory(string roomId, int count)
        {
            var result = LoadChatHistory(roomId, count);
            return JsonConvert.SerializeObject(result);
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

        private Messages LoadChatHistory(string roomId, int count)
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor());
            var msgs = context.ChatService.LoadMessages(Guid.Parse(roomId), count);

            var result = new Messages() { success = true, messages = msgs };
            return result;
        }
    }
}
