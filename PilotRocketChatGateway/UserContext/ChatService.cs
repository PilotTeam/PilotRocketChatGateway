using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.WebSockets;
using SixLabors.ImageSharp;

namespace PilotRocketChatGateway.UserContext
{
    public interface IChatService : IService
    {
        IDataSender DataSender { get; }
        IDataLoader DataLoader { get; }
    }
    public class ChatService : IChatService
    {
        public ChatService(IDataSender dataSender, IDataLoader dataLoader)
        {
            DataSender = dataSender;
            DataLoader = dataLoader;
        }

        public IDataSender DataSender { get; }
        public IDataLoader DataLoader { get; }

        public void Dispose()
        {
        }
    }
}
