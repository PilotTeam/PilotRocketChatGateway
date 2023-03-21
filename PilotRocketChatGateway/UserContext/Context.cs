using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.Pushes;
using PilotRocketChatGateway.WebSockets;

namespace PilotRocketChatGateway.UserContext
{
    public interface IContext : IDisposable
    {
        IRemoteService RemoteService { get; }
        IChatService ChatService { get; }
        IPushService PushService { get; }
        IWebSocketsNotifyer WebSocketsNotifyer { get; }
        void SetService(IService service);
        UserData UserData { get; }

        bool IsDisposed { get; }
    }
    public class Context : IContext
    {
        private IList<IDisposable> _disposables = new List<IDisposable>();
        private IRemoteService _remoteService;
        private IChatService _chatService;
        private IPushService _pushService;
        private IWebSocketsNotifyer _webSocketsNotifyer;

        public Context(UserData credentials)
        {
            UserData = credentials;
        }
        public UserData UserData { get; }
        
        public IRemoteService RemoteService
        {

            get
            {
                return _remoteService;
            }
        }
        public IChatService ChatService
        {

            get
            {
                return _chatService;
            }
        }
        public IPushService PushService => _pushService;
        public IWebSocketsNotifyer WebSocketsNotifyer => _webSocketsNotifyer;

        public bool IsDisposed { get; private set; }
        public void SetService(IService service)
        {
            switch (service)
            {
                case IRemoteService remoteService:
                    _remoteService = remoteService;
                    break;
                case IChatService chatService:
                    _chatService = chatService;
                    break;
                case IWebSocketsNotifyer webSocksetsService:
                    _webSocketsNotifyer = webSocksetsService;
                    break;
                case IPushService pushService:
                    _pushService = pushService;
                    break;
                default: 
                    throw new Exception($"unknown service: {service.GetType()}");
            }

            _disposables.Add(service);
        }
        public void Dispose()
        {
            IsDisposed = true;
            foreach (var disposable in _disposables)
                disposable.Dispose();

        }
    }
}
