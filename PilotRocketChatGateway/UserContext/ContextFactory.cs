using Ascon.Pilot.Server.Api;
using PilotRocketChatGateway.PilotServer;

namespace PilotRocketChatGateway.UserContext
{
    public interface IContextFactory
    {
        IContext CreateContext(HttpPilotClient client);
    }

    public class ContextFactory : IContextFactory
    {
        public IContext CreateContext(HttpPilotClient client)
        {
            var context = new Context();
            var remoteSerive = new RemoteService(client, context);
            var chatService = new ChatService(remoteSerive.ServerApi);

            context.SetService(remoteSerive);
            context.SetService(chatService);
            return context;
        }
    }
}
