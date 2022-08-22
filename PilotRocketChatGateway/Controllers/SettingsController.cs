using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Reflection;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private const string ROCKET_CHAT_SETTNGS_FILE = "rocketchatsettings.json";
        private static Dictionary<string, Setting> _rocketChatSettings { get; } = LoadRocketChatSettings();

        [HttpGet("api/v1/settings.oauth")]
        public OauthSettings Outh()
        {
            return new OauthSettings { success = false, services = new string[] { } };
        }

        [HttpGet("api/v1/settings.public")]
        public string Public(string query)
        {
            var settings = JsonConvert.DeserializeObject<SettingsRequest>(query);

            var serverSettings = GetServerSetting(settings);
            return JsonConvert.SerializeObject(serverSettings);
        }

        private ServerSettings GetServerSetting(SettingsRequest settingsRequest)
        {
            var result = new ServerSettings
            {
                success = true,
                settings = new List<Setting>(),
                total = settingsRequest.settings.settings.Count
            };

            foreach (var setting in settingsRequest.settings.settings)
            {
                if (_rocketChatSettings.TryGetValue(setting, out var val))
                    result.settings.Add(val);
            }

            return result;
        }

        private static Dictionary<string, Setting> LoadRocketChatSettings()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ROCKET_CHAT_SETTNGS_FILE);
            string json = System.IO.File.ReadAllText(path);

            var settings = JsonConvert.DeserializeObject<ServerSettings>(json);
            var result = new Dictionary<string, Setting>();
            foreach (var setting in settings.settings)
                result[setting.id] = setting;
            return result;
        }
    }
}
