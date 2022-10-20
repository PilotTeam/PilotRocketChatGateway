using Ascon.Pilot.Server.Api;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IService : IDisposable { }
    public interface IRemoteService : IService
    {
        IServerApiService ServerApi { get; }
        IFileFileManager FileManager { get; }
        bool IsActive { get; }
    }
    public class RemoteService : IRemoteService, IConnectionLostListener
    {
        private HttpPilotClient _client;
        private ServerApiService _serverApi;
        private IFileFileManager _fileManager;

        public RemoteService(HttpPilotClient client, IContext context, IFileLoader fileLoader, ILogger logger)
        {
            _client = client;
            _client.SetConnectionLostListener(this);

            var serverApi = _client.GetServerApi(new NullableServerCallback());
            var messageApi = _client.GetMessagesApi(new MessagesCallback(context, logger));
            var dbInfo = serverApi.OpenDatabase();

            var archiveApi = _client.GetFileArchiveApi();
            _fileManager = new FileManager(archiveApi, serverApi, fileLoader);
            var attachmentHelper = new AttachmentHelper(serverApi, _fileManager, dbInfo.Person);

            _serverApi = new ServerApiService(serverApi, messageApi, attachmentHelper, dbInfo);
            IsActive = true;
        }

        public bool IsActive { get; private set; }

        public IServerApiService ServerApi => _serverApi;

        public IFileFileManager FileManager => _fileManager;

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
