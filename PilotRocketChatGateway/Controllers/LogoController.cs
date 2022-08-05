using Microsoft.AspNetCore.Mvc;
using System.Drawing;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class LogoSettingsController : ControllerBase
    {
        [HttpGet("/images/logo/android-chrome-512x512.png")]
        public IActionResult Get()
        {
            return File(Properties.Resources.logo, "image/png");
        }
    }
}
