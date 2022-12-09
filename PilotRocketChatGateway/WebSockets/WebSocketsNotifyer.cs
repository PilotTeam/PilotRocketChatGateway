using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using System.Collections.Concurrent;

namespace PilotRocketChatGateway.WebSockets
{
    public interface IWebSocketsNotifyer : IService
    {
        bool IsEmpty { get; }
        void RegisterWebSocketService(IWebSocksetsService service);
        void RemoveWebSocketService(IWebSocksetsService service);
        void SendMessage(DMessage dMessage);
        void SendUserStatusChange(int person, UserStatuses status);
        void SendTypingMessage(DChat chat, int personId);
        void NotifyMessageCreated(DMessage dMessage, NotifyClientKind notify);
    }
    public class WebSocketsNotifyer : IWebSocketsNotifyer
    {
        private IWebSocketBank _bank;
        private IContext _context;
        private ConcurrentDictionary<int, IWebSocksetsService> _servises => _bank.GetServises(_context.Credentials.Username);

        public bool IsEmpty => _servises.Any() == false;

        public WebSocketsNotifyer(IWebSocketBank bank, IContext context)
        {
            _context = context;
            _bank = bank;
        }
        public void RegisterWebSocketService(IWebSocksetsService service)
        {
            _bank.RegisterWebSocketService(_context.Credentials.Username, service);
        }
        public void RemoveWebSocketService(IWebSocksetsService service)
        {
            _bank.RemoveWebSocketService(_context.Credentials.Username, service);
        }

        public void SendMessage(DMessage dMessage)
        {
            foreach (var service in _servises)
                service.Value.Session.SendMessageToClientAsync(dMessage);
        }

        public void SendUserStatusChange(int person, UserStatuses status)
        {
            foreach (var service in _servises)
                service.Value.Session.SendUserStatusChangeAsync(person, status);
        }

        public void SendTypingMessage(DChat chat, int personId)
        {
            foreach (var service in _servises)
                service.Value.Session.SendTypingMessageToClient(chat, personId);
        }

        public void NotifyMessageCreated(DMessage dMessage, NotifyClientKind notify)
        {
            foreach (var service in _servises)
                service.Value.Session.NotifyMessageCreatedAsync(dMessage, notify);
        }
        public void Dispose()
        {
            foreach (var service in _servises)
            {
                _bank.RemoveWebSocketService(_context.Credentials.Username, service.Value);
                service.Value.Dispose();
            }
        }
    }
}
