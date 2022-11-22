using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.WebSockets;

namespace PilotRocketChatGateway.UserContext
{
    public interface IContext : IDisposable
    {
        IRemoteService RemoteService { get; }

        IChatService ChatService { get; }
        IWebSocketsNotifyer WebSocketsNotifyer { get; }
        void SetService(IService service);
        Credentials Credentials { get; }
    }
    public class Context : IContext
    {
        private IList<IDisposable> _disposables = new List<IDisposable>();
        private IRemoteService _remoteService;
        private IChatService _chatService;
        private IWebSocketsNotifyer _webSocketsNotifyer;

        public Context(Credentials credentials)
        {
            Credentials = credentials;
        }
        public Credentials Credentials { get; }
        
        public IRemoteService RemoteService
        {

            get
            {
                if (_remoteService.IsConnected == false)
                    return null;

                return _remoteService;
            }
        }
        public IChatService ChatService
        {

            get
            {
                if (_remoteService.IsConnected == false)
                    return null;

                return _chatService;
            }
        }

        public IWebSocketsNotifyer WebSocketsNotifyer => _webSocketsNotifyer;

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
                default: 
                    throw new Exception($"unknown service: {service.GetType()}");
            }

            _disposables.Add(service);
        }
        public void Dispose()
        {
            foreach (var disposable in _disposables)
                disposable.Dispose();
        }
    }
}
