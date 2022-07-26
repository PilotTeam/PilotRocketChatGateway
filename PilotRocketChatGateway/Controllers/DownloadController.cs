﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using System.Web;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class DownloadController : ControllerBase
    {
        private readonly IContextService _contextService;
        private readonly IAuthHelper _authHelper;
        private readonly AuthSettings _authSettings;

        public DownloadController(IContextService contextService, IAuthHelper authHelper, IOptions<AuthSettings> authSettings)
        {
            _contextService = contextService;
            _authHelper = authHelper;
            _authSettings = authSettings.Value;
        }

        [Route("/[controller]/{objId}")]
        public IActionResult Get(Guid objId)
        {
            string rc_token;
            rc_token = GetParam(nameof(rc_token));

            if (_authHelper.ValidateToken(rc_token, _authSettings) == false)
                throw new UnauthorizedAccessException();

            var context = _contextService.GetContext(_authHelper.GetTokenActor(rc_token));
            var attach = context.RemoteService.FileManager.LoadFileInfo(objId);
            return File(attach.Data, attach.FileType);
        }

        private string GetParam(string query)
        {
            return HttpUtility.ParseQueryString(HttpContext.Request.QueryString.ToString()).Get(query) ?? string.Empty;
        }
    }
}
