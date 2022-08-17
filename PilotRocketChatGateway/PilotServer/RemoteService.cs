using Ascon.Pilot.Server.Api;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IService : IDisposable { }
    public interface IRemoteService : IService
    {
        IServerApiService ServerApi { get; }
        bool IsActive { get; }
    }
    public class RemoteService : IRemoteService, IConnectionLostListener
    {
        private HttpPilotClient _client;
        private ServerApiService _serverApi;

        public RemoteService(HttpPilotClient client, IContext context, ILogger logger)
        {
            _client = client;
            _client.SetConnectionLostListener(this);

            var serverApi = _client.GetServerApi(new NullableServerCallback());
            var messageApi = _client.GetMessagesApi(new MessagesCallback(context, logger));
            var dbInfo = serverApi.OpenDatabase();

            _serverApi = new ServerApiService(serverApi, messageApi, dbInfo);
            IsActive = true;
        }

        public bool IsActive { get; private set; }

        public IServerApiService ServerApi => _serverApi;

        public void ConnectionLost(Exception ex = null)
        {
            IsActive = false;
        }

        public void Dispose()
        {
            _client.Disconnect();
            _client?.Dispose();
        }
    }
}
