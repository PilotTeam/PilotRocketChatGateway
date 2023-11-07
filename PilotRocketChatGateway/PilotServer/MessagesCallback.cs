using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api.Contracts;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.WebSockets;

namespace PilotRocketChatGateway.PilotServer
{
    public class MessagesCallback : IMessageCallback
    {
        private readonly ILogger _logger;
        private readonly IContext _context;
        private Guid _lastProcessedMsgId;

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
            if (_lastProcessedMsgId == message.Message.Id)
            {
                _logger.Log(LogLevel.Information, $"Duplicate notification user: {_context.RemoteService.ServerApi.CurrentPerson.Login} msgId: {message.Message.Id}  chatId: {message.Message.ChatId} messageType: {message.Message.Type}");
                return;
            }

            _lastProcessedMsgId = message.Message.Id;
            _logger.Log(LogLevel.Information, $"Call on {nameof(NotifyMessageCreated)} in {_context.RemoteService.ServerApi.CurrentPerson.Login} context. creatorId: {message.Message.CreatorId} chatId: {message.Message.ChatId} messageType: {message.Message.Type}");
            try
            {
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
            _logger.Log(LogLevel.Information, $"Call on {nameof(NotifyOffline)}. personId: {personId}");
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
            _logger.Log(LogLevel.Information, $"Call on {nameof(NotifyOnline)}. personId: {personId}");
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
            _logger.Log(LogLevel.Information, $"Call on {nameof(NotifyTypingMessage)}. chatId: {chatId}, personId: {personId}");
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
