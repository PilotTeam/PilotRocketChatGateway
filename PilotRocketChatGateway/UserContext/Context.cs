using PilotRocketChatGateway.PilotServer;

namespace PilotRocketChatGateway.UserContext
{
    public interface IContext : IDisposable
    {
        IRemoteService RemoteService { get; init; }

        IChatService ChatService { get; init; }
        IWebSocksetsService WebSocketsService { get; set; }
    }
    public class Context : IContext
    {
        private IRemoteService _remoteService;
        private IChatService _chatService;
        public IRemoteService RemoteService
        {

            get
            {
                if (_remoteService.IsActive == false)
                    throw new UnauthorizedAccessException();
                return _remoteService;
            }
            init
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
            init
            {
                _chatService = value;
            }
        }

        public IWebSocksetsService WebSocketsService { get; set; }

        public void Dispose()
        {
            _remoteService.Dispose();
            WebSocketsService.Dispose();
        }
    }
}
