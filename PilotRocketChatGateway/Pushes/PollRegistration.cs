using Newtonsoft.Json;
using PilotRocketChatGateway.Utils;

namespace PilotRocketChatGateway.Pushes
{
    public interface IPollRegistration
    {
        Task<WorkspaceData> PollAsync(IntentData intent, Serilog.ILogger log);
    }
    public class PollRegistration : IPollRegistration
    {
        private readonly IHttpRequestHelper _requestHelper;

        public PollRegistration(IHttpRequestHelper requestHelper)
        {
            _requestHelper = requestHelper;
        }

        public Task<WorkspaceData> PollAsync(IntentData intent, Serilog.ILogger log)
        {
            using (var cancelTokenSource = new CancellationTokenSource())
            {
                var task = Task.Run(() => Polling(intent.device_code, cancelTokenSource.Token, log));

                if (task.Wait(TimeSpan.FromMinutes(15)))
                    return task;
            
                cancelTokenSource.Cancel();
                log.Error("Failed to register in in cloud.rocket.chat: polling time is out, restart the app to start over");
                return Task.FromResult<WorkspaceData>(null);
            }    
        }

        private async Task<WorkspaceData> Polling(string token, CancellationToken cancelToken, Serilog.ILogger log)
        {
            WorkspacePollResult data = null;

            while (data?.payload == null)
            {
                var (responce, code) = await _requestHelper.GetAsync($"{Const.CLOUD_URI}/api/v2/register/workspace/poll", new Dictionary<string, object>() { { "token", token } });

                data = JsonConvert.DeserializeObject<WorkspacePollResult>(responce);
                if (data?.payload == null)
                {
                    var pollResponse = JsonConvert.DeserializeObject<PollStatus>(responce);
                    if (pollResponse == null)
                        log.Information($"polling responce: {responce}. code: {code}");
                    else 
                        log.Information($"polling responce: {pollResponse.status}");

                    cancelToken.WaitHandle.WaitOne(5000);
                }

                if (cancelToken.IsCancellationRequested)
                    cancelToken.ThrowIfCancellationRequested(); 
            }

            log.Information($"successfully registered in cloud.rocket.chat");
            return data.payload;
        }
    }
}
