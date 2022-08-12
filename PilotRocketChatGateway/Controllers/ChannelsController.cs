using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.PilotServer;

namespace PilotRocketChatGateway.Controllers
{

    [ApiController]
    public class ChannelsController : ControllerBase
    {

        public ChannelsController()
        {
            //_logger = logger;
        }

        [Authorize]
        [HttpGet("api/v1/channels.history")]
        public string Get()
        {
            //var context = _context.GetContext(HttpContext.GetTokenActor());
            //var rooms = context.ChatService.LoadRooms();

            //return JsonConvert.SerializeObject(subs);
            return null;
        }
    }
}
