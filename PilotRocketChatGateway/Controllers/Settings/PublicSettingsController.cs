using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Reflection;

namespace PilotRocketChatGateway.Controllers.Settings
{
    public class PublicSettingsController : Controller
    {
        public const string SERVER_SETTNGS_FILE = "serversettings.json";
        private static Dictionary<string, Setting> _serverSettings  = LoadServerSettings();

        [HttpGet("api/v1/settings.public")]
        public string Get(string query)
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

            foreach(var setting in settingsRequest.settings.settings)
            {
                if (_serverSettings.TryGetValue(setting, out var val))
                    result.settings.Add(val);
            }

            return result;
        }

        private static Dictionary<string, Setting> LoadServerSettings()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), SERVER_SETTNGS_FILE);
            string json = System.IO.File.ReadAllText(path);

            var settings = JsonConvert.DeserializeObject<ServerSettings>(json);
            var result = new Dictionary<string, Setting>();
            foreach (var setting in settings.settings)
                result[setting.id] = setting;
            return result;
        }
    }
}
