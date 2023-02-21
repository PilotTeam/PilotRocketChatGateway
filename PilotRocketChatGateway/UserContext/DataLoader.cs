using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.Utils;
using System;

namespace PilotRocketChatGateway.UserContext
{
    public interface IDataLoader
    {
        IRCDataConverter RCDataConverter { get; }
        Room LoadRoom(Guid id);
        IList<Room> LoadRooms();
        Subscription LoadRoomsSubscription(string roomId);
        IList<Subscription> LoadRoomsSubscriptions();
        Room LoadPersonalRoom(string username);
        IList<Message> LoadMessages(string roomId, int count, string upperBound);
        IList<Message> LoadMessages(string roomId, string lowerBound);
        Message LoadMessage(string msgId);
        IList<Message> LoadSurroundingMessages(string rcMsgId, string roomId, int count);
        User LoadUser(int usderId);
        IList<User> LoadUsers(int count);
        IList<User> LoadMembers(string roomId);
    }
    public class DataLoader : IDataLoader
    {
        private readonly ICommonDataConverter _commonConverter;
        private readonly IContext _context;
        private readonly IBatchMessageLoader _loader;

        public DataLoader(IRCDataConverter rcConverter, ICommonDataConverter commonConverter, IContext context, IBatchMessageLoader msgLoader)
        {
            RCDataConverter = rcConverter;
            _commonConverter = commonConverter;
            _context = context;
            _loader = msgLoader;
        }
        public IRCDataConverter RCDataConverter { get; }
        public Room LoadRoom(Guid id)
        {
            var roomId = _commonConverter.ConvertToChatId(id.ToString());
            var chat = _context.RemoteService.ServerApi.GetChat(roomId);
            var lastMsg = _context.RemoteService.ServerApi.GetMessage(chat.LastMessage.Id);
            return RCDataConverter.ConvertToRoom(chat.Chat, chat.Relations, lastMsg);
        }

        public IList<Room> LoadRooms()
        {
            var chats = _context.RemoteService.ServerApi.GetChats();
            return chats.Select(x => RCDataConverter.ConvertToRoom(x.Chat, x.Relations, x.LastMessage)).ToList();
        }
        public Subscription LoadRoomsSubscription(string roomId)
        {
            var id = _commonConverter.ConvertToChatId(roomId);
            var chat = _context.RemoteService.ServerApi.GetChat(id);
            return RCDataConverter.ConvertToSubscription(chat);
        }
        public IList<Subscription> LoadRoomsSubscriptions()
        {
            var chats = _context.RemoteService.ServerApi.GetChats();
            return chats.Select(x => RCDataConverter.ConvertToSubscription(x)).ToList();
        }
        public Room LoadPersonalRoom(string username)
        {
            var person = _context.RemoteService.ServerApi.GetPerson((x) => x.Login == username);
            var chat = _context.RemoteService.ServerApi.GetPersonalChat(person.Id);
            return chat.Chat.Id == Guid.Empty ? null : RCDataConverter.ConvertToRoom(chat.Chat, chat.Relations, chat.LastMessage);
        }
        public IList<Message> LoadMessages(string roomId, int count, string upperBound)
        {
            var id = _commonConverter.ConvertToChatId(roomId);
            var dateTo = _commonConverter.ConvertFromJSDate(upperBound);
            return LoadMessages(id, DateTime.MinValue, dateTo.AddMilliseconds(-1), count);
        }
        public IList<Message> LoadMessages(string roomId, string lowerBound)
        {
            var id = _commonConverter.ConvertToChatId(roomId);
            var dateFrom = _commonConverter.ConvertFromJSDate(lowerBound);
            return LoadMessages(id, dateFrom, DateTime.MaxValue, int.MaxValue);
        }

        public Message LoadMessage(string msgId)
        {
            DMessage? msg = _commonConverter.IsRocketChatId(msgId) ?
                _context.RemoteService.ServerApi.GetMessage(msgId) :
                _context.RemoteService.ServerApi.GetMessage(Guid.Parse(msgId));

            return RCDataConverter.ConvertToMessage(msg);
        }

        public IList<Message> LoadSurroundingMessages(string rcMsgId, string roomId, int count)
        {
            Guid msgId = _commonConverter.ConvertToMsgId(rcMsgId);
            Guid chatId = _commonConverter.ConvertToChatId(roomId);
            var messages = _loader.FindMessage(msgId, chatId, count);
            return messages.Select(x => RCDataConverter.ConvertToMessage(x)).ToList();
        }

        public User LoadUser(int userId)
        {
            var person = _context.RemoteService.ServerApi.GetPerson(userId);
            return _commonConverter.ConvertToUser(person, fullName: true);
        }
        public IList<User> LoadUsers(int count)
        {
            var users = _context.RemoteService.ServerApi.GetPeople().Values;
            return users.Select(x => _commonConverter.ConvertToUser(x, fullName: true)).ToList();
        }
        public IList<User> LoadMembers(string roomId)
        {
            var id = _commonConverter.ConvertToChatId(roomId);
            var members = _context.RemoteService.ServerApi.GetChatMembers(id);
            return members.Select(x =>
            {
                var person = _context.RemoteService.ServerApi.GetPerson(x.PersonId);
                return _commonConverter.ConvertToUser(person);
            }).ToList();
        }
      
        private IList<Message> LoadMessages(Guid roomId, DateTime dateFrom, DateTime dateTo, int count)
        {
            var msgs = _context.RemoteService.ServerApi.GetMessages(roomId, dateFrom, dateTo, count);

            if (msgs == null)
                return new List<Message>();

            var chat = _context.RemoteService.ServerApi.GetChat(roomId);
            var attachs = GetAttachmentsIds(chat.Relations);

            var result = new List<Message>();
            foreach (var msg in msgs)
            {
                result.Add(RCDataConverter.ConvertToMessage(msg, chat.Chat, attachs));
            }

            return result;
        }
        private static Dictionary<Guid, Guid> GetAttachmentsIds(IList<DChatRelation> chatRelations)
        {
            var attachs = new Dictionary<Guid, Guid>();
            foreach (var rel in chatRelations.Where(x => x.Type == ChatRelationType.Attach && x.MessageId.HasValue))
                attachs[rel.MessageId.Value] = rel.ObjectId;
            return attachs;
        }
    }
}
