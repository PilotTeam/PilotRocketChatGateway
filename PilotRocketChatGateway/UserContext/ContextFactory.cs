using Ascon.Pilot.Server.Api;
using PilotRocketChatGateway.PilotServer;

namespace PilotRocketChatGateway.UserContext
{
    public interface IContextFactory
    {
        IContext CreateContext(HttpPilotClient client, ILogger logger);
    }

    public class ContextFactory : IContextFactory
    {
        public IContext CreateContext(HttpPilotClient client, ILogger logger)
        {
            var context = new Context();
            var remoteSerive = new RemoteService(client, context, logger);
            var chatService = new ChatService(remoteSerive.ServerApi);

            context.SetService(remoteSerive);
            context.SetService(chatService);
            return context;
        }
    }
}
