using Microsoft.AspNetCore.Mvc;

namespace PilotRocketChatGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InfoController : ControllerBase
    {
        public const string SERVER_VERSION = "6.5"; 
        [HttpGet]
        public Info Get()
        {
            return new Info { success = true, version = SERVER_VERSION };
        }
    }
}
