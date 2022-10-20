using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api.Contracts;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IServerApiService
    {
        INPerson CurrentPerson { get; }
        INPerson GetPerson(int id);
        INPerson GetPerson(string login);
        List<DChatInfo> GetChats();
        DChatInfo GetChat(Guid id);
        DObject GetObject(Guid id);
        DChatInfo GetPersonalChat(int personId);
        DMessage GetLastUnreadMessage(Guid chatId);
        List<DMessage> GetMessages(Guid chatId, DateTime dateTo, int count);
        List<DChatMember> GetChatMembers(Guid chatId);
        void SendMessage(DMessage message);
        DDatabaseInfo GetDatabaseInfo();
        IReadOnlyDictionary<int, INPerson> GetPeople();
        Guid CreateAttachmentObject(string fileName, byte[] attach);
    }
    public class ServerApiService : IServerApiService
    {
        private readonly IServerApi _serverApi;
        private readonly IMessagesApi _messagesApi;
        private readonly IAttachmentHelper _attachmentHelper;
        private readonly DDatabaseInfo _dbInfo;
        private readonly DPerson _currentPerson;
        private Dictionary<int, INPerson> _people;

        public ServerApiService(IServerApi serverApi, IMessagesApi messagesApi, IAttachmentHelper attachmentHelper, DDatabaseInfo dbInfo)
        {
            _serverApi = serverApi;
            _messagesApi = messagesApi;
            _dbInfo = dbInfo;
            _currentPerson = dbInfo.Person;
            _attachmentHelper = attachmentHelper;

            LoadPeople();
        }

        public INPerson CurrentPerson => _currentPerson;

        public DMessage GetLastUnreadMessage(Guid chatId)
        {
            return _messagesApi.GetLastUnreadMessage(chatId);
        }

        public DChatInfo GetChat(Guid id)
        {
            return _messagesApi.GetChat(id);
        }

        public DChatInfo GetPersonalChat(int personId)
        {
            return _messagesApi.GetPersonalChat(personId);
        }

        public List<DChatInfo> GetChats()
        {
            return _messagesApi.GetChats(_currentPerson.Id, DateTime.MinValue, DateTime.MaxValue, int.MaxValue).ToList();
        }

        public DDatabaseInfo GetDatabaseInfo()
        {
            return _dbInfo;
        }

        public List<DMessage> GetMessages(Guid chatId, DateTime dateTo, int count)
        {
            return _messagesApi.GetMessages(chatId, DateTime.MinValue, dateTo, count).Item1;
        }

        public IReadOnlyDictionary<int, INPerson> GetPeople()
        {
            return _people;
        }

        public INPerson GetPerson(int id)
        {
            _people.TryGetValue(id, out var person);
            return person;
        }
        public INPerson GetPerson(string login)
        {
            return _people.Values.First(x => x.Login == login);
        }

        public void SendMessage(DMessage message)
        {
            _messagesApi.SendMessage(message);
        }

        private void LoadPeople()
        {
            _people = _serverApi.LoadPeople().ToDictionary(k => k.Id, v => (INPerson)v);
        }

        public List<DChatMember> GetChatMembers(Guid chatId)
        {
            return _messagesApi.GetChatMembers(chatId, DateTime.MinValue);
        }

        public DObject GetObject(Guid id)
        {
            return _serverApi.GetObjects(new Guid[] { id }).First();
        }

        public Guid CreateAttachmentObject(string fileName, byte[] data)
        {
            var change = _attachmentHelper.CreateChangeWithAttachmentObject(fileName, data);
            var changeset = new DChangesetData(Guid.NewGuid(), DateTime.UtcNow, CurrentPerson.Id, string.Empty, new List<DChange> { change }, new List<Guid>() { });
            _serverApi.Change(changeset);
            return change.New.Id;
        }
    }

}
