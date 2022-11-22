﻿using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.PilotServer;

namespace PilotRocketChatGateway.WebSockets
{
    public interface IWebSocketsNotifyer : IService
    {
        bool IsEmpty { get; }
        void RegisterWebSocketServise(IWebSocksetsService service);
        void RemoveWebSocketServise(IWebSocksetsService service);
        void SendMessage(DMessage dMessage);
        void SendUserStatusChange(int person, UserStatuses status);
        void SendTypingMessage(DChat chat, int personId);
        void NotifyMessageCreated(DMessage dMessage, NotifyClientKind notify);
    }
    public class WebSocketsNotifyer : IWebSocketsNotifyer
    {
        private IList<IWebSocksetsService> _servises;

        public bool IsEmpty => _servises.Any() == false;

        public WebSocketsNotifyer()
        {
            _servises = new List<IWebSocksetsService>();
        }
        public void RegisterWebSocketServise(IWebSocksetsService service)
        {
            _servises.Add(service);
        }
        public void RemoveWebSocketServise(IWebSocksetsService service)
        {
            _servises.Remove(service);
        }

        public void SendMessage(DMessage dMessage)
        {
            CheckServises();
            foreach (var service in _servises)
                service.Session.SendMessageToClientAsync(dMessage);
        }

        public void SendUserStatusChange(int person, UserStatuses status)
        {
            CheckServises();
            foreach (var service in _servises)
                service.Session.SendUserStatusChangeAsync(person, status);
        }

        public void SendTypingMessage(DChat chat, int personId)
        {
            CheckServises();
            foreach (var service in _servises)
                service.Session.SendTypingMessageToClient(chat, personId);
        }

        public void NotifyMessageCreated(DMessage dMessage, NotifyClientKind notify)
        {
            CheckServises();
            foreach (var service in _servises)
                service.Session.NotifyMessageCreatedAsync(dMessage, notify);
        }
        private void CheckServises()
        {
            foreach (var service in _servises.ToArray())
            {
                if (service.State != System.Net.WebSockets.WebSocketState.Open)
                {
                    _servises.Remove(service);
                    service.Dispose();
                }
            }
        }
        public void Dispose()
        {
            foreach (var service in _servises)
                service.Dispose();
        }
    }
}
