using Ascon.Pilot.DataClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private IContextService _contextService;

        public SubscriptionsController(IContextService contextService)
        {
            _contextService = contextService;
        }

        [Authorize]
        [HttpGet("api/v1/subscriptions.get")]
        public string Get()
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor());
            var subs = context.ChatService.LoadRoomsSubscriptions();

            var result = new Subscriptions() { success = true, update = subs, remove = new List<Subscription>() };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpPost("/api/v1/subscriptions.read")]
        public string Read(object request)
        {
            var room = JsonConvert.DeserializeObject<RoomRequest>(request.ToString());
            var context = _contextService.GetContext(HttpContext.GetTokenActor());
            context.ChatService.SendMessageToServer(MessageType.MessageRead, Guid.Parse(room.roomId), string.Empty);
            var result = new HttpResult()
            {
                success = true
            };
            return JsonConvert.SerializeObject(result);
        }
    }
}
