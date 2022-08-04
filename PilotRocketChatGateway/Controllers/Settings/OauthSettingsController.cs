using Microsoft.AspNetCore.Mvc;

namespace PilotRocketChatGateway.Controllers.Settings
{
    [ApiController]
    public class OauthSettingsController : ControllerBase
    {
        [HttpGet("api/v1/settings.oauth")]
        public OauthSettings Get()
        {
            return new OauthSettings { success = false, services = new string[]{ }};
        }
    }
}
