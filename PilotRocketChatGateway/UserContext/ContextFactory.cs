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
            var fileLoader = new FileLoader(client.GetFileArchiveApi());
            var context = new Context();
            var remoteSerive = new RemoteService(client, context, fileLoader, logger);

            var commonConverter = new CommonDataConverter(context);
            var attachLoader = new AttachmentLoader(commonConverter, context);
            var rcConverter = new RCDataConverter(context, attachLoader, commonConverter);
            var loader = new DataLoader(rcConverter, commonConverter, context);
            var sender = new DataSender(rcConverter, commonConverter, context);

            var chatService = new ChatService(sender, loader);

            context.SetService(remoteSerive);
            context.SetService(chatService);
            return context;
        }
    }
}
