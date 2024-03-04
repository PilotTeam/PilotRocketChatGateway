using Ascon.Pilot.Server.Api;
using Ascon.Pilot.Transport;
using Microsoft.AspNetCore.StaticFiles;
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
        private object _lock = new object();


        public RemoteService(IContext context, IConnectionService connector, ILogger logger)
        {
            _logger = logger;
            _context = context;
            _connector = connector;
        }

        public bool IsConnected { get; private set; }

        public IServerApiService ServerApi => _serverApi;

        public IFileFileManager FileManager => _fileManager;

        public void ConnectionLost(Exception ex = null)
        {
            _logger.Log(LogLevel.Information, $"Notification on ConnectionLost. person: {_context.RemoteService.ServerApi.CurrentPerson.Login}. Erorr: {ex?.Message}");

            if (_disposed || !IsConnected)
                return;

            lock (_lock)
            {
                if (_disposed || !IsConnected)
                    return;

                IsConnected = false;
            }

            _logger.Log(LogLevel.Information, $"Lost connection to pilot-server. person: {_context.RemoteService.ServerApi.CurrentPerson.Login}");
            _logger.LogError(0, ex, ex?.Message);

            _ = TryConnectAsync();
        }

        
        public async Task ConnectAsync()
        {
            _client = await _connector.ConnectAsync(_context.UserData);

            var fileLoader = new FileLoader(_client.GetFileArchiveApi(), new FileExtensionContentTypeProvider());
            _client.SetConnectionLostListener(this);
            var serverCallback = new ServerCallback();
            var serverApi = _client.GetServerApi(serverCallback);
            var messageApi = _client.GetMessagesApi(new MessagesCallback(_context, _logger));
            messageApi.Open(30, DateTime.UtcNow);
            var dbInfo = serverApi.OpenDatabase();

            var archiveApi = _client.GetFileArchiveApi();
            _fileManager = new FileManager(archiveApi, serverApi, fileLoader);
            var attachmentHelper = new AttachmentHelper(serverApi, _fileManager, dbInfo.Person);

            var changeSender = new ChangesetSender(serverApi, serverCallback);
            _serverApi = new ServerApiService(serverApi, messageApi, attachmentHelper, dbInfo, changeSender);
            serverCallback.Subscribe(_serverApi);

            lock (_lock)
            {
                IsConnected = true;
            }
            _logger.Log(LogLevel.Information, $"connected to pilot-server. person: {ServerApi.CurrentPerson.Login}");
        }
        private async Task TryConnectAsync()
        {
            while (!IsConnected)
            {
                try
                {
                    if (_disposed)
                        return;

                    _client?.Dispose();
                    await ConnectAsync();
                }
                catch (Exception e)
                {
                    _logger.Log(LogLevel.Information, $"failed to connect to the server. person: {ServerApi.CurrentPerson.Login}");
                    _logger.LogError(e.Message);
                    Thread.Sleep(RECONNECT_TIME_OUT);
                }
            }
        }


        public void Dispose()
        {
            lock (_lock)
            {
                _disposed = true;
                _client.Disconnect();
                _client?.Dispose();
            }
        }
    }
}
