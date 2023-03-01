using Ascon.Pilot.DataClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.Pushes;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class PushController : ControllerBase
    {

        private readonly IContextsBank _contextsBank;
        private readonly IAuthHelper _authHelper;
        private readonly ILogger<PushController> _logger;

        public PushController(IContextsBank contextsBank, IAuthHelper authHelper, ILogger<PushController> logger)
        {
            _contextsBank = contextsBank;
            _authHelper = authHelper;
            _logger = logger;
        }

        [Authorize]
        [HttpPost("api/v1/push.token")]
        public string Token(object request)
        {
            var token = JsonConvert.DeserializeObject<PushTokenRequest>(request.ToString());
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));

            PushToken pushToken = null;
            switch (token.type)
            {
                case nameof(PushTokenTypes.apn):
                    pushToken = new PushToken { Value = token.value, Type = PushTokenTypes.apn };
                    break;
                case nameof(PushTokenTypes.gcm):
                    pushToken = new PushToken { Value = token.value, Type = PushTokenTypes.gcm };
                    break;
                default:
                    _logger.Log(LogLevel.Error, $"unknow token: {token.type}");
                    break;
            }

            if (pushToken != null)
                context.PushService.PushToken = pushToken;

            return JsonConvert.SerializeObject(new { success = true });
        }
    }
}
