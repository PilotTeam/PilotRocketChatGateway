using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IService : IDisposable { }
    public interface IRemoteService : IService
    {
        IServerApiService ServerApi { get; }
        IFileFileManager FileManager { get; }
        bool IsConnected { get; }
    }
    public class RemoteService : IRemoteService, IConnectionLostListener
    {
        const int RECONNECT_TIME_OUT = 5000;
        private ILogger _logger;
        private IContext _context;
        private IConnectionService _connector;
        private HttpPilotClient _client;
        private ServerApiService _serverApi;
        private IFileFileManager _fileManager;
        private bool _disposed = false;

        public RemoteService(IContext context, IConnectionService connector, ILogger logger)
        {
            _logger = logger;
            _context = context;
            _connector = connector;

            Connect();
        }

        public bool IsConnected { get; private set; }

        public IServerApiService ServerApi => _serverApi;

        public IFileFileManager FileManager => _fileManager;

        public void ConnectionLost(Exception ex = null)
        {
            if (_disposed)
                return;

            _client?.Dispose();
            _logger.Log(LogLevel.Information, $"Lost connection to pilot-server. person: {_context.RemoteService.ServerApi.CurrentPerson.Login}");
            _logger.LogError(0, ex, ex.Message);

            IsConnected = false;
            TryConnect();
        }

        private void TryConnect()
        {
            bool firstTry = true;
            while (!IsConnected)
            {
                try
                {
                    if (_disposed)
                        return;

                    Connect();
                }
                catch (Exception)
                {
                    if (firstTry)
                    {
                        _logger.Log(LogLevel.Information, $"failed to connect to the server. person: {ServerApi.CurrentPerson.Login}");
                        firstTry = false;
                    }
                    Thread.Sleep(RECONNECT_TIME_OUT);
                }
            }
        }

        private void Connect()
        {
            _client = _connector.Connect(_context.UserData);

            var fileLoader = new FileLoader(_client.GetFileArchiveApi());
            _client.SetConnectionLostListener(this);
            var serverApi = _client.GetServerApi(new NullableServerCallback());
            var messageApi = _client.GetMessagesApi(new MessagesCallback(_context, _logger));
            var dbInfo = serverApi.OpenDatabase();

            var archiveApi = _client.GetFileArchiveApi();
            _fileManager = new FileManager(archiveApi, serverApi, fileLoader);
            var attachmentHelper = new AttachmentHelper(serverApi, _fileManager, dbInfo.Person);

            _serverApi = new ServerApiService(serverApi, messageApi, attachmentHelper, dbInfo);

            IsConnected = true;
            _logger.Log(LogLevel.Information, $"connected to pilot-server. person: {ServerApi.CurrentPerson.Login}");
        }

        public void Dispose()
        {
            _disposed = true;
            _client.Disconnect();
            _client?.Dispose();
        }
    }
}
