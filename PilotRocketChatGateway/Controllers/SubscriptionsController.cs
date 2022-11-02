using Ascon.Pilot.DataClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private IContextService _contextService;
        private IAuthHelper _authHelper;

        public SubscriptionsController(IContextService contextService, IAuthHelper authHelper)
        {
            _contextService = contextService;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpGet("api/v1/subscriptions.get")]
        public string Get()
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor(_authHelper));
            var subs = context.ChatService.DataLoader.LoadRoomsSubscriptions();

            var result = new Subscriptions() { success = true, update = subs, remove = new List<Subscription>() };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpPost("/api/v1/subscriptions.read")]
        public string Read(object request)
        {
            var room = JsonConvert.DeserializeObject<RoomRequest>(request.ToString());
            var context = _contextService.GetContext(HttpContext.GetTokenActor(_authHelper));
            context.ChatService.DataSender.SendReadAllMessageToServer(room.roomId);

            var result = new HttpResult()
            {
                success = true
            };
            return JsonConvert.SerializeObject(result);
        }
    }
}
