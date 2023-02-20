using Ascon.Pilot.Server.Api;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.Utils;
using PilotRocketChatGateway.WebSockets;

namespace PilotRocketChatGateway.UserContext
{
    public interface IContextFactory
    {
        IContext CreateContext(UserData credentials, IConnectionService connector, IWebSocketBank bank, ILogger logger, IBatchMessageLoaderFactory batchMessageLoaderFactory);
    }

    public class ContextFactory : IContextFactory
    {
        public IContext CreateContext(UserData credentials, IConnectionService connector, IWebSocketBank bank, ILogger logger, IBatchMessageLoaderFactory batchMessageLoaderFactory)
        {
            var context = new Context(credentials);
            var remoteSerive = new RemoteService(context, connector, logger);

            var commonConverter = new CommonDataConverter(context);
            var attachLoader = new MediaAttachmentLoader(commonConverter, context);
            var rcConverter = new RCDataConverter(context, attachLoader, commonConverter);
            var loader = new DataLoader(rcConverter, commonConverter, context, batchMessageLoaderFactory);
            var sender = new DataSender(rcConverter, commonConverter, context);
            var notifyer = new WebSocketsNotifyer(bank, context); 

            var chatService = new ChatService(sender, loader);

            context.SetService(remoteSerive);
            context.SetService(chatService);
            context.SetService(notifyer);
            return context;
        }
    }
}
