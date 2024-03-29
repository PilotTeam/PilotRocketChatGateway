﻿using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api.Contracts;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.WebSockets;

namespace PilotRocketChatGateway.PilotServer
{
    public class MessagesCallback : IMessageCallback
    {
        private readonly ILogger _logger;
        private readonly IContext _context;
        private Guid _instanseId = Guid.NewGuid();

        public MessagesCallback(IContext context, ILogger logger)
        {
            _logger = logger;
            _context = context;
        }
        public void CreateNotification(DNotification notification)
        {
        }

        public async void NotifyMessageCreated(NotifiableDMessage message)
        {
            _logger.Log(LogLevel.Information, $"Call on {nameof(NotifyMessageCreated)} in {_context.RemoteService.ServerApi.CurrentPerson.Login} context. Instanse id {_instanseId}. creatorId: {message.Message.CreatorId} chatId: {message.Message.ChatId} messageType: {message.Message.Type}. message id: {message.Message.Id}");

            if (message.Message.Id == _context.LastSentMsg)
            {
                _logger.Log(LogLevel.Information, $"Duplicate call in {_context.RemoteService.ServerApi.CurrentPerson.Login} context. Instanse id {_instanseId}. creatorId: {message.Message.CreatorId} chatId: {message.Message.ChatId} messageType: {message.Message.Type}");
                return;
            }

            try
            {
                _context.LastSentMsg = message.Message.Id;
                var chat = _context.RemoteService.ServerApi.GetChat(message.Message.ChatId);
                _context.WebSocketsNotifyer.SendMessage(message.Message, chat, message.IsNotifiable);
                await _context.PushService.SendPushAsync(message, chat.Chat);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public void NotifyOffline(int personId)
        {
            _logger.Log(LogLevel.Information, $"Call on {nameof(NotifyOffline)} in {_context.RemoteService.ServerApi.CurrentPerson.Login} context. Instanse id {_instanseId}. personId: {personId}");
            try
            {
                _context.WebSocketsNotifyer.SendUserStatusChange(personId, UserStatuses.offline);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public void NotifyOnline(int personId)
        {
            _logger.Log(LogLevel.Information, $"Call on {nameof(NotifyOnline)} in {_context.RemoteService.ServerApi.CurrentPerson.Login} context. Instanse id {_instanseId}. personId: {personId}");
            try
            {
                _context.WebSocketsNotifyer.SendUserStatusChange(personId, UserStatuses.online);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public void NotifyTypingMessage(Guid chatId, int personId)
        {
            _logger.Log(LogLevel.Information, $"Call on {nameof(NotifyTypingMessage)} in {_context.RemoteService.ServerApi.CurrentPerson.Login} context. Instanse id {_instanseId}. chatId: {chatId}, personId: {personId}");
            try
            {
                var chat = _context.RemoteService.ServerApi.GetChat(chatId);
                _context.WebSocketsNotifyer.SendTypingMessage(chat.Chat, personId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public void UpdateLastMessageDate(DateTime maxDate)
        {
        }
    }
}
