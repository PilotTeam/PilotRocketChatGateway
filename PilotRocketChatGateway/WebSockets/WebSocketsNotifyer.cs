using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace PilotRocketChatGateway.WebSockets
{
    public interface IWebSocketsNotifyer : IService
    {
        Dictionary<int, IWebSocksetsService> Services { get; }
        void RegisterWebSocketService(IWebSocksetsService service);
        void RemoveWebSocketService(IWebSocksetsService service);
        void SendMessage(DMessage dMessage, DChatInfo chat, bool isChatNotifiable);
        void SendUserStatusChange(int person, UserStatuses status);
        void SendTypingMessage(DChat chat, int personId);
        void NotifyMessageCreated(DMessage dMessage, DChatInfo chat, NotifyClientKind notify);
    }
    public class WebSocketsNotifyer : IWebSocketsNotifyer
    {
        public WebSocketsNotifyer()
        {
        }
        public Dictionary<int, IWebSocksetsService> Services { get; } = new Dictionary<int, IWebSocksetsService>();
        public void RegisterWebSocketService(IWebSocksetsService service)
        {
            Services[service.GetHashCode()] = service;
        }
        public void RemoveWebSocketService(IWebSocksetsService service)
        {
            Services.Remove(service.GetHashCode(), out _);
        }

        public void SendMessage(DMessage dMessage, DChatInfo chat, bool isChatNotifiable)
        {
            foreach (var service in Services)
                service.Value.Session.SendMessageToClient(dMessage, chat, isChatNotifiable);
        }

        public void SendUserStatusChange(int person, UserStatuses status)
        {
            foreach (var service in Services)
                service.Value.Session.SendUserStatusChange(person, status);
        }

        public void SendTypingMessage(DChat chat, int personId)
        {
            foreach (var service in Services)
                service.Value.Session.SendTypingMessageToClient(chat, personId);
        }

        public void NotifyMessageCreated(DMessage dMessage, DChatInfo chat, NotifyClientKind notify)
        {
            foreach (var service in Services)
                service.Value.Session.NotifyMessageCreated(dMessage, chat, notify, false);
        }
        public void Dispose()
        {
            foreach (var service in Services)
            {
                service.Value.Dispose();
            }
        }
    }
}
