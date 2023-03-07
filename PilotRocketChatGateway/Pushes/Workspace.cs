using Newtonsoft.Json;

namespace PilotRocketChatGateway.Pushes
{
    public interface IWorkspace
    {
        WorkspaceData Data { get; }
        void SaveData(string json);
    }
    public class Workspace : IWorkspace
    {
        const string WORKSPACE_FILE_NAME = "workspace.json";
        private static string _settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);

        private ILogger<Workspace> _logger;

        public Workspace(ILogger<Workspace> logger)
        {
            _logger = logger;
            LoadData();
        }
        public WorkspaceData Data { get; private set; }
        public void SaveData(string json)
        {
            var fullName = GetFilePath();
            try
            {

                var dir = Path.GetDirectoryName(fullName);
                if (Directory.Exists(dir) == false)
                    Directory.CreateDirectory(dir);
                File.WriteAllText(fullName, json);
                Data = JsonConvert.DeserializeObject<WorkspaceData>(json);

                _logger.Log(LogLevel.Information, $"Workspace was initiated, saved data in {fullName}");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        private void LoadData()
        {
            try
            {
                var path = GetFilePath();
                var json = File.ReadAllText(path);
                Data = JsonConvert.DeserializeObject<WorkspaceData>(json);

                _logger.Log(LogLevel.Information, "Workspace was initiated");
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e.Message);
            }
        }

        private string GetFilePath()
        {
            return Path.Combine(_settingsFolder, WORKSPACE_FILE_NAME); 
        }
    }
}
