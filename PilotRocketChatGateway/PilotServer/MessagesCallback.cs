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

        public MessagesCallback(IContext context, ILogger logger)
        {
            _logger = logger;
            _context = context;
        }
        public void CreateNotification(DNotification notification)
        {
        }

        public void NotifyMessageCreated(NotifiableDMessage message)
        {
            _logger.Log(LogLevel.Information, $"Call on {nameof(NotifyMessageCreated)}. CreatorId: {message.Message.CreatorId} ChatId: {message.Message.ChatId} MessageType: {message.Message.Type}");
            try
            {
                _context.WebSocketsSession.SendMessageToClientAsync(message.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public void NotifyOffline(int personId)
        {
            _logger.Log(LogLevel.Information, $"Call on {nameof(NotifyOffline)}. PersonId: {personId}");
            try
            {
                _context.WebSocketsSession.SendUserStatusChangeAsync(personId, UserStatuses.offline);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public void NotifyOnline(int personId)
        {
            _logger.Log(LogLevel.Information, $"Call on {nameof(NotifyOnline)}. PersonId: {personId}");
            try
            {
                _context.WebSocketsSession.SendUserStatusChangeAsync(personId, UserStatuses.online);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public void NotifyTypingMessage(Guid chatId, int personId)
        {
        }
    }
}
