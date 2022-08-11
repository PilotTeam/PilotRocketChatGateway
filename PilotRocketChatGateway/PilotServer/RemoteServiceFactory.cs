using Ascon.Pilot.Server.Api;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IRemoteServiceFactory
    {
        IRemoteService CreateRemoteService(HttpPilotClient client);
    }

    public class RemoteServiceFactory : IRemoteServiceFactory
    {
        public IRemoteService CreateRemoteService(HttpPilotClient client)
        {
            return new RemoteService(client);
        }
    }
}
