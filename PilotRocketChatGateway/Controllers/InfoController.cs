using Microsoft.AspNetCore.Mvc;

namespace PilotRocketChatGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InfoController : ControllerBase
    {
        [HttpGet]
        public Info Get()
        {
            return new Info { success = true, version = Const.SERVER_VERSION };
        }
    }
}
