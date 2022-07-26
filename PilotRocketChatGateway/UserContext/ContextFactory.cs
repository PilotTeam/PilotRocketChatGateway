﻿using Ascon.Pilot.Server.Api;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.WebSockets;

namespace PilotRocketChatGateway.UserContext
{
    public interface IContextFactory
    {
        IContext CreateContext(Credentials credentials, IConnectionService connector, ILogger logger);
    }

    public class ContextFactory : IContextFactory
    {
        public IContext CreateContext(Credentials credentials, IConnectionService connector, ILogger logger)
        {
            var context = new Context(credentials);
            var remoteSerive = new RemoteService(context, connector, logger);

            var commonConverter = new CommonDataConverter(context);
            var attachLoader = new AttachmentLoader(commonConverter, context);
            var rcConverter = new RCDataConverter(context, attachLoader, commonConverter);
            var loader = new DataLoader(rcConverter, commonConverter, context);
            var sender = new DataSender(rcConverter, commonConverter, context);
            var notifyer = new WebSocketsNotifyer(); 

            var chatService = new ChatService(sender, loader);

            context.SetService(remoteSerive);
            context.SetService(chatService);
            context.SetService(notifyer);
            return context;
        }
    }
}
