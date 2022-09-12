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
        DChatInfo GetPersonalChat(int personId);
        DMessage GetLastUnreadMessage(Guid chatId);
        List<DMessage> GetMessages(Guid chatId, int count);
        void SendMessage(DMessage message);
        DDatabaseInfo GetDatabaseInfo();
        IReadOnlyDictionary<int, INPerson> GetPeople();
    }
    public class ServerApiService : IServerApiService
    {
        private readonly IServerApi _serverApi;
        private readonly IMessagesApi _messagesApi;
        private readonly DDatabaseInfo _dbInfo;
        private readonly DPerson _currentPerson;
        private Dictionary<int, INPerson> _people;

        public ServerApiService(IServerApi serverApi, IMessagesApi messagesApi, DDatabaseInfo dbInfo)
        {
            _serverApi = serverApi;
            _messagesApi = messagesApi;
            _dbInfo = dbInfo;
            _currentPerson = dbInfo.Person;

            LoadPeople();
        }

        public INPerson CurrentPerson => _currentPerson;

        public DMessage GetLastUnreadMessage(Guid chatId)
        {
            return _messagesApi.GetLastUnreadMessage(chatId);
        }

        public DChatInfo GetChat(Guid id)
        {
            return _messagesApi.GetChat(id).DChatInfo;
        }

        public DChatInfo GetPersonalChat(int personId)
        {
            return _messagesApi.GetPersonalChat(personId).DChatInfo;
        }

        public List<DChatInfo> GetChats()
        {
            return _messagesApi.GetChats(_currentPerson.Id, DateTime.MinValue, DateTime.MaxValue, int.MaxValue).Select(x => x.DChatInfo).ToList();
        }

        public DDatabaseInfo GetDatabaseInfo()
        {
            return _dbInfo;
        }

        public List<DMessage> GetMessages(Guid chatId, int count)
        {
            return _messagesApi.GetMessages(chatId, DateTime.MinValue, DateTime.MaxValue, count).Item1;
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
    }
}
