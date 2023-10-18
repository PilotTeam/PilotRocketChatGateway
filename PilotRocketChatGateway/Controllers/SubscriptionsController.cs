using Ascon.Pilot.DataClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using System.Web;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private IContextsBank _contextsBank;
        private IAuthHelper _authHelper;

        public SubscriptionsController(IContextsBank contextsBank, IAuthHelper authHelper)
        {
            _contextsBank = contextsBank;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpGet("api/v1/subscriptions.get")]
        public string Get()
        {
            string updatedSince;

            updatedSince = GetParam(nameof(updatedSince));

            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            var subs = context.ChatService.DataLoader.LoadRoomsSubscriptions(updatedSince);

            var result = new Subscriptions() { success = true, update = subs, remove = new List<Subscription>() };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpPost("/api/v1/subscriptions.read")]
        public string Read(object request)
        {
            var room = JsonConvert.DeserializeObject<RoomRequest>(request.ToString());
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            context.ChatService.DataSender.SendReadAllMessageToServer(room.roomId);

            var result = new HttpResult()
            {
                success = true
            };
            return JsonConvert.SerializeObject(result);
        }
        private string GetParam(string query)
        {
            return HttpUtility.ParseQueryString(HttpContext.Request.QueryString.ToString()).Get(query) ?? string.Empty;
        }
    }
}
