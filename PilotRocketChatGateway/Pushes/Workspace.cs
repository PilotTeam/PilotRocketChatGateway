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
        private ILogger<Workspace> _logger;

        public Workspace(ILogger<Workspace> logger)
        {
            _logger = logger;
            LoadData();
        }
        public WorkspaceData Data { get; private set; }
        public void SaveData(string json)
        {
            var path = GetFilePath();
            File.WriteAllText(path, json);
            Data = JsonConvert.DeserializeObject<WorkspaceData>(json);

            _logger.Log(LogLevel.Information, $"Workspace was initiated, saved data in {path}");
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
            return Path.Combine(Directory.GetCurrentDirectory(), WORKSPACE_FILE_NAME); 
        }
    }
}
