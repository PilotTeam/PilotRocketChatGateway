using Ascon.Pilot.Server.Api;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.Pushes;
using PilotRocketChatGateway.Utils;
using PilotRocketChatGateway.WebSockets;

namespace PilotRocketChatGateway.UserContext
{
    public interface IContextFactory
    {
        Task<IContext> CreateContextAsync(UserData credentials, IConnectionService connector, ILogger logger, IPushGatewayConnector pushConnector);
    }

    public class ContextFactory : IContextFactory
    {
        public async Task<IContext> CreateContextAsync(UserData credentials, IConnectionService connector, ILogger logger, IPushGatewayConnector pushConnector)
        {
            var context = new Context(credentials);
            var remoteSerive = new RemoteService(context, connector, logger);
            await remoteSerive.ConnectAsync();

            var commonConverter = new CommonDataConverter(context);
            var attachLoader = new MediaAttachmentLoader(commonConverter, context);
            var rcConverter = new RCDataConverter(context, attachLoader, commonConverter, logger);
            var msgLoader = new BatchMessageLoader(context);
            var loader = new DataLoader(rcConverter, commonConverter, context, msgLoader, logger);
            var sender = new DataSender(rcConverter, commonConverter, context);
            var notifyer = new WebSocketsNotifyer(); 

            var chatService = new ChatService(sender, loader);
            var pushService = new PushService(pushConnector, context);

            context.SetService(remoteSerive);
            context.SetService(chatService);
            context.SetService(notifyer);
            context.SetService(pushService);
            return context;
        }
    }
}
