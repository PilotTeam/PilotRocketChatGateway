using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.WebSockets;

namespace PilotRocketChatGateway.UserContext
{
    public interface IContext : IDisposable
    {
        IRemoteService RemoteService { get; }

        IChatService ChatService { get; }
        IWebSocketSession WebSocketsSession { get; }
        void SetService(IService service);
    }
    public class Context : IContext
    {
        private IList<IDisposable> _disposables = new List<IDisposable>();
        private IRemoteService _remoteService;
        private IChatService _chatService;
        private IWebSocksetsService _webSocksetsService;

        public IRemoteService RemoteService
        {

            get
            {
                if (_remoteService.IsActive == false)
                {
                    Dispose();
                    throw new UnauthorizedAccessException();
                }
                return _remoteService;
            }
        }
        public IChatService ChatService
        {

            get
            {
                if (_remoteService.IsActive == false)
                {
                    Dispose();
                    throw new UnauthorizedAccessException();
                }
                return _chatService;
            }
        }

        public IWebSocketSession WebSocketsSession => _webSocksetsService.Session;
        
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
                case IWebSocksetsService webSocksetsService: 
                    _webSocksetsService = webSocksetsService;
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
