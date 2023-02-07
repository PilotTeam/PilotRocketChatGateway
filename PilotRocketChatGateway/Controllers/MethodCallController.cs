using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.WebSockets;
using System.Dynamic;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class MethodCallController : ControllerBase
    {
        private IContextService _contextService;
        private IAuthHelper _authHelper;

        public MethodCallController(IContextService contextService, IAuthHelper authHelper)
        {
            _contextService = contextService;
            _authHelper = authHelper;
    }

        [Authorize]
        [HttpPost("api/v1/method.call/loadSurroundingMessages")]
        public string LoadSurroundingMessages(object request)
        {
            var context = _contextService.GetContext(HttpContext.GetTokenActor(_authHelper));


            dynamic dRequest = JsonConvert.DeserializeObject<ExpandoObject>(request.ToString(), new ExpandoObjectConverter());
            dynamic dRequest2 = JsonConvert.DeserializeObject<ExpandoObject>(dRequest.message, new ExpandoObjectConverter());

            string msgId = dRequest2.@params[0]._id;
            string rid = dRequest2.@params[0].rid;
            int count = unchecked((int)dRequest2.@params[1]);

            var msgs = context.ChatService.DataLoader.LoadSurroundingMessages(msgId, rid, count);
            var result = new
            {
                message = JsonConvert.SerializeObject(new
                {
                    error = false,
                    result = new
                    {
                        messages = msgs
                    }
                })
            };
            return JsonConvert.SerializeObject(result);
        }
    }

}
