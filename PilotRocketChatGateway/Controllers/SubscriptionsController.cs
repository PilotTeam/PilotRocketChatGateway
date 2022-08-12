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
            return JsonConvert.SerializeObject(subs);
        }

    }
}
