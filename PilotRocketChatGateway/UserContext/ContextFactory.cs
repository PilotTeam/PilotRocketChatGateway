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
            var remoteSerive = new RemoteService(client);
            var chatService = new ChatService(remoteSerive.ServerApi);
            return new Context()
            {
                RemoteService = remoteSerive,
                ChatService = chatService
            };
        }
    }
}
