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
        private IRemoteService _remoteService;
        private IChatService _chatService;
        private IWebSocksetsService _webSocksetsService;

        public IRemoteService RemoteService
        {

            get
            {
                if (_remoteService.IsActive == false)
                    throw new UnauthorizedAccessException();
                return _remoteService;
            }
            set
            {
                _remoteService = value;
            }
        }
        public IChatService ChatService
        {

            get
            {
                if (_remoteService.IsActive == false)
                    throw new UnauthorizedAccessException();
                return _chatService;
            }
            set
            {
                _chatService = value;
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
        }
        public void Dispose()
        {
            _remoteService.Dispose();
            _webSocksetsService?.Dispose();
        }
    }
}
