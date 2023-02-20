using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Utils
{
    public interface IBatchMessageLoaderFactory
    {
        IBatchMessageLoader Create(IContext context);
    }
    public class BatchMessageLoaderFactory : IBatchMessageLoaderFactory
    {
        public IBatchMessageLoader Create(IContext context)
        {
            return new BatchMessageLoader(context);
        }
    }
}
