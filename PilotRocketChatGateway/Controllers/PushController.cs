using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.PilotServer;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class PushController : ControllerBase
    {

        [HttpPost("push/apn/send")]
        public string Send(object request)
        {
            var a = this.HttpContext;
            var req = request.ToString();
            return "Hi";
        }


        [Authorize]
        [HttpPost("api/v1/push.token")]
        public string Token(object request)
        {
            var a = request.ToString();
            return string.Empty;
        }
    }
}
