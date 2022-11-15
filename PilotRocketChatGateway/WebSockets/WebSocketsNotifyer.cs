using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.PilotServer;

namespace PilotRocketChatGateway.WebSockets
{
    public interface IWebSocketsNotifyer : IService
    {
        void RegisterWebSocketServise(IWebSocksetsService service);
        Task SendMessageAsync(DMessage dMessage);
        Task SendUserStatusChangeAsync(int person, UserStatuses status);
        void SendTypingMessage(DChat chat, int personId);
        Task NotifyMessageCreatedAsync(DMessage dMessage, NotifyClientKind notify);
    }
    public class WebSocketsNotifyer : IWebSocketsNotifyer
    {
        private IList<IWebSocksetsService> _servises;
        public WebSocketsNotifyer()
        {
            _servises = new List<IWebSocksetsService>();
        }
        public void RegisterWebSocketServise(IWebSocksetsService service)
        {
            _servises.Add(service);
        }

        public async Task SendMessageAsync(DMessage dMessage)
        {
            foreach (var service in _servises)
                await service.Session.SendMessageToClientAsync(dMessage);
        }

        public async Task SendUserStatusChangeAsync(int person, UserStatuses status)
        {
            foreach (var service in _servises)
                await service.Session.SendUserStatusChangeAsync(person, status);
        }

        public void SendTypingMessage(DChat chat, int personId)
        {
            foreach (var service in _servises)
                service.Session.SendTypingMessageToClient(chat, personId);
        }

        public async Task NotifyMessageCreatedAsync(DMessage dMessage, NotifyClientKind notify)
        {
            foreach (var service in _servises)
                await service.Session.NotifyMessageCreatedAsync(dMessage, notify);
        }
        public void Dispose()
        {
            foreach (var service in _servises)
                service.Dispose();
        }
    }
}
