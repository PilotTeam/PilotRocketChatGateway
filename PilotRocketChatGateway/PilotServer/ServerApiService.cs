using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api.Contracts;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IServerApiService
    {
        INPerson CurrentPerson { get; }
        INPerson GetPerson(int id);
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

        public DDatabaseInfo GetDatabaseInfo()
        {
            return _dbInfo;
        }

        public IReadOnlyDictionary<int, INPerson> GetPeople()
        {
            return _people;
        }

        public INPerson GetPerson(int id)
        {
            return _people[id];
        }

        private void LoadPeople()
        {
            _people = _serverApi.LoadPeople().ToDictionary(k => k.Id, v => (INPerson)v);
        }
    }
}
