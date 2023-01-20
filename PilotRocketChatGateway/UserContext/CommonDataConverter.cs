using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.WebSockets;

namespace PilotRocketChatGateway.UserContext
{
    public interface ICommonDataConverter
    {
        User ConvertToUser(INPerson person);
        string ConvertToJSDate(DateTime date);
        Guid ConvertToChatId(string roomId);
        Guid ConvertToMsgId(string rcMsgId);
        DateTime ConvertFromJSDate(string date);
        bool IsRocketChatId(string msgId);
    }
    public class CommonDataConverter : ICommonDataConverter
    {
        private readonly IContext _context;

        public CommonDataConverter(IContext context)
        {
            _context = context;
        }
        public User ConvertToUser(INPerson person)
        {
            return new User()
            {
                id = person.Id.ToString(),
                username = person.Login,
                name = person.DisplayName,
                status = GetUserStatus(person.Id),
                roles = new string[] { "user" }
            };
        }
        public string ConvertToJSDate(DateTime date)
        {
            return date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
        public Guid ConvertToChatId(string roomId)
        {
            if (Guid.TryParse(roomId, out var id))
                return id;

            var personId = int.Parse(roomId);
            var chat = _context.RemoteService.ServerApi.GetPersonalChat(personId);
            return chat.Chat.Id;
        }
        public Guid ConvertToMsgId(string rcMsgId)
        {
            if (IsRocketChatId(rcMsgId))
            {
                var msg = _context.RemoteService.ServerApi.GetMessage(rcMsgId);
                return msg == null ? Guid.Empty : msg.Id;
            }

            return Guid.Parse(rcMsgId);
        }
        public DateTime ConvertFromJSDate(string date)
        {
            return string.IsNullOrEmpty(date) ? DateTime.MaxValue : DateTime.Parse(date).ToUniversalTime();
        }
        public bool IsRocketChatId(string msgId)
        {
            return msgId.Length == 17;
        }
        private string GetUserStatus(int person)
        {
            if (_context.RemoteService.ServerApi.IsOnline(person))
                return nameof(UserStatuses.online);
            return nameof(UserStatuses.offline);
        }
    }
}
