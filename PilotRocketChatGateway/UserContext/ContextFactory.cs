using Ascon.Pilot.Server.Api;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.Pushes;
using PilotRocketChatGateway.Utils;
using PilotRocketChatGateway.WebSockets;

namespace PilotRocketChatGateway.UserContext
{
    public interface IContextFactory
    {
        IContext CreateContext(UserData credentials, IConnectionService connector, IWebSocketBank bank, ILogger logger, IPushGatewayConnector pushConnector);
    }

    public class ContextFactory : IContextFactory
    {
        public IContext CreateContext(UserData credentials, IConnectionService connector,IWebSocketBank bank, ILogger logger, IPushGatewayConnector pushConnector)
        {
            var context = new Context(credentials);
            var remoteSerive = new RemoteService(context, connector, logger);

            var commonConverter = new CommonDataConverter(context);
            var attachLoader = new MediaAttachmentLoader(commonConverter, context);
            var rcConverter = new RCDataConverter(context, attachLoader, commonConverter);
            var msgLoader = new BatchMessageLoader(context);
            var loader = new DataLoader(rcConverter, commonConverter, context, msgLoader);
            var sender = new DataSender(rcConverter, commonConverter, context);
            var notifyer = new WebSocketsNotifyer(bank, context); 

            var chatService = new ChatService(sender, loader);
            var pushService = new PushService(pushConnector, chatService, remoteSerive.ServerApi);

            context.SetService(remoteSerive);
            context.SetService(chatService);
            context.SetService(notifyer);
            context.SetService(pushService);
            return context;
        }
    }
}
