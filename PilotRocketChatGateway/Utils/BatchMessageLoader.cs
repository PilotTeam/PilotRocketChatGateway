using Ascon.Pilot.Common;
using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;

namespace PilotRocketChatGateway.Utils
{

    public interface IBatchMessageLoader
    {
        List<DMessage> FindMessage(Guid msgId, Guid chatId, int count);
    }
    public class BatchMessageLoader : IBatchMessageLoader
    {
        IContext _context;
        public BatchMessageLoader(IContext context)
        {
            _context = context;
        }   

        public List<DMessage> FindMessage(Guid msgId, Guid chatId, int count)
        {
            DateTime from = DateTime.MinValue;
            DateTime to = DateTime.MaxValue;
            while(true)
            {
                var messages = _context.RemoteService.ServerApi.GetMessages(chatId, from, to, count);
                if (messages.Where(x => x.Id == msgId).Any())
                    return messages;

                var changed = SetUpperBound(messages.Last(), ref to);
                if (changed == false)
                    return new List<DMessage>();
            }

        }

        private bool SetUpperBound(INMessage msg, ref DateTime upperBound)
        {
            if (msg.ServerDate < upperBound)
            {
                upperBound = msg.ServerDate.Value;
                return true;
            }
            return false;
        }
    }
}
