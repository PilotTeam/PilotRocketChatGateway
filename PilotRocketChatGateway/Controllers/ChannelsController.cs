using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class ChannelsController : ControllerBase
    {
        private readonly IContextService _contextService;
        public ChannelsController(IContextService contextService)
        {
            _contextService = contextService;
        }

        [Authorize]
        [HttpGet("api/v1/channels.history")]
        public string History(string roomId, int count)
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor());
            var msgs = context.ChatService.LoadMessages(roomId, count, string.Empty);

            var result = new Messages() { success = true, messages = msgs };
            return JsonConvert.SerializeObject(result);
        }
    }
}
