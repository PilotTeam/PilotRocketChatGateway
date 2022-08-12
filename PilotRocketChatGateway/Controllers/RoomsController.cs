using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private IContextService _contextService;

        public RoomsController(IContextService contextService)
        {
            _contextService = contextService;
        }

        [Authorize]
        [HttpGet("api/v1/rooms.get")]
        public string Get()
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor());
            var rooms = context.ChatService.LoadRooms();

            var result = new Rooms() { success = true, update = rooms, remove = new List<Room>() };
            return JsonConvert.SerializeObject(result);
        }
    }
}
