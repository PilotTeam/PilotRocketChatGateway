﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DirectoryController : ControllerBase
    {
        private IContextsBank _contextsBank;
        private readonly IAuthHelper _authHelper;

        public DirectoryController(IContextsBank contextsBank, IAuthHelper authHelper)
        {
            _contextsBank = contextsBank;
            _authHelper = authHelper;
        }

        [Authorize]
        public string Get(string query)
        {
            var requset = JsonConvert.DeserializeObject<DirectoryRequest>(query);
            if (requset.type != "users")
                return string.Empty;

            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            var users = context.ChatService.DataLoader.LoadUsers(requset.count);

            var result = new { success = true, result = users, total = context.RemoteService.ServerApi.GetPeople().Where(x => !x.Value.IsDeleted && x.Value.Login != context.UserData.Username).Count() };
            return JsonConvert.SerializeObject(result);
        }
    }
}
