using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Reflection;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class PermissionsController : ControllerBase
    {
        private const string ROCKET_CHAT_PERMISSIONS_FILE = "rocketchatpermissions.json";
        private static IList<Permission> _rocketChatPermissions { get; } = LoadRocketChatSettings();

        [Authorize]
        [HttpGet("api/v1/permissions.listAll")]
        public string ListAll()
        {
            var permissions = new Permissions
            {
                success = true,
                update = _rocketChatPermissions,
                remove = new List<Permission>()
            };
            return JsonConvert.SerializeObject(permissions);
        }


        private static IList<Permission> LoadRocketChatSettings()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ROCKET_CHAT_PERMISSIONS_FILE);
            string json = System.IO.File.ReadAllText(path);

            var permissions = JsonConvert.DeserializeObject<Permissions>(json);
            return permissions.update;
        }
    }
}
