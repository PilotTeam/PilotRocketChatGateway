using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api.Contracts;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.PilotServer
{
    public class MessagesCallback : IMessageCallback
    {
        private readonly IContext _context;

        public MessagesCallback(IContext context)
        {
            _context = context;
        }
        public void CreateNotification(DNotification notification)
        {
        }

        public void NotifyMessageCreated(NotifiableDMessage message)
        {
            _context.WebSocketsSession.SendMessageToClientAsync(message.Message);
        }

        public void NotifyTypingMessage(Guid chatId, int personId)
        {
        }
    }
}
