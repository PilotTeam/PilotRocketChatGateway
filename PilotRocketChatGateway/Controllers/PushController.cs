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
    public class PushController : ControllerBase
    {

        private readonly IContextService _contextService;
        private readonly IAuthHelper _authHelper;

        public PushController(IContextService contextService, IAuthHelper authHelper)
        {
            _contextService = contextService;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpPost("api/v1/push.token")]
        public string Token(object request)
        {
            var token = JsonConvert.DeserializeObject<PushTokenRequest>(request.ToString());
            var context = _contextService.GetContext(HttpContext.GetTokenActor(_authHelper));
            context.UserData.PushToken = new PushToken { apn = token.value };
            return JsonConvert.SerializeObject(new { success = true });
        }
    }
}
