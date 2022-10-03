using Microsoft.AspNetCore.Mvc;

namespace PilotRocketChatGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InfoController : ControllerBase
    {
        public const string SERVER_VERSION = "5.0"; 
        [HttpGet]
        public Info Get()
        {
            throw new Exception("Error");
            return new Info { success = true, version = SERVER_VERSION };
        }
    }
}
