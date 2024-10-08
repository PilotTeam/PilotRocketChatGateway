﻿using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api.Contracts;
using Serilog;
using System.Collections.Concurrent;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IServerApiService
    {
        INPerson CurrentPerson { get; }
        INPerson GetPerson(int id);
        INPerson GetPerson(string login);
        bool IsOnline(int person);
        List<DChatInfo> GetChats(DateTime lastUpdated);
        DChatInfo GetChat(Guid id);
        DObject GetObject(Guid id);
        DChatInfo GetPersonalChat(int personId);
        DMessage GetLastUnreadMessage(Guid chatId);
        List<DMessage> GetMessages(Guid chatId, DateTime dateFrom, DateTime dateTo, int count);
        DMessage GetMessage(string thirdPartyInfo);
        DMessage GetMessage(Guid id);
        List<DChatMember> GetChatMembers(Guid chatId);
        DateTime SendMessage(DMessage message);
        void SendTypingMessage(Guid chatId);
        DDatabaseInfo GetDatabaseInfo();
        IReadOnlyDictionary<int, INPerson> GetPeople();
        Task<DObject> CreateAttachmentObjectAsync(string fileName, byte[] attach);
        INType GetNType(int typeId);
    }
    public interface IPersonChangeListener
    {
        void Notify(IEnumerable<DPerson> personChangeset);
    }
    public class ServerApiService : IServerApiService, IPersonChangeListener
    {
        private readonly IServerApi _serverApi;
        private readonly IMessagesApi _messagesApi;
        private readonly IAttachmentHelper _attachmentHelper;
        private readonly DDatabaseInfo _dbInfo;
        private readonly IChangesetSender _changeSender;
        private readonly DPerson _currentPerson;
        private readonly ConcurrentDictionary<int, INPerson> _people = new ConcurrentDictionary<int, INPerson>();

        public ServerApiService(IServerApi serverApi, IMessagesApi messagesApi, IAttachmentHelper attachmentHelper, DDatabaseInfo dbInfo, IChangesetSender changeSender)
        {
            _serverApi = serverApi;
            _messagesApi = messagesApi;
            _dbInfo = dbInfo;
            _changeSender = changeSender;
            _currentPerson = dbInfo.Person;
            _attachmentHelper = attachmentHelper;
            var people = _serverApi.LoadPeople().ToDictionary(k => k.Id, v => (INPerson)v);
            _people = new ConcurrentDictionary<int, INPerson>(people);
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

        public bool IsOnline(int person)
        {
            return _messagesApi.CheckIsOnline(person);
        }

        public DChatInfo GetPersonalChat(int personId)
        {
            return _messagesApi.GetPersonalChat(personId);
        }

        public List<DChatInfo> GetChats(DateTime lastUpdated)
        {
            return _messagesApi.GetChats(_currentPerson.Id, lastUpdated, DateTime.MaxValue, 50, false).ToList();
        }

        public DDatabaseInfo GetDatabaseInfo()
        {
            return _dbInfo;
        }

        public List<DMessage> GetMessages(Guid chatId, DateTime dateFrom, DateTime dateTo, int count)
        {
            return _messagesApi.GetMessages(chatId, dateFrom, dateTo, count).Item1;
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
            return _people.Values.FirstOrDefault(x => x.Login == login);
        }

        public DateTime SendMessage(DMessage message)
        {
            return _messagesApi.SendMessage(message);
        }

        public List<DChatMember> GetChatMembers(Guid chatId)
        {
            return _messagesApi.GetChatMembers(chatId, DateTime.MinValue);
        }

        public DObject GetObject(Guid id)
        {
            return _serverApi.GetObjects(new Guid[] { id }).FirstOrDefault();
        }

        public async Task<DObject> CreateAttachmentObjectAsync(string fileName, byte[] data)
        {
            var change = _attachmentHelper.CreateChangeWithAttachmentObject(fileName, data);
            var changeset = new DChangesetData(Guid.NewGuid(), DateTime.UtcNow, CurrentPerson.Id, string.Empty, new List<DChange> { change });
            await _changeSender.ChangeAsync(changeset);
            return change.New;
        }

        public DMessage GetMessage(string thirdPartyInfo)
        {
            return _messagesApi.GetThirdPartyMessage(thirdPartyInfo);
        }

        public DMessage GetMessage(Guid id)
        {
            return _messagesApi.GetMessage(id);
        }

        public void SendTypingMessage(Guid chatId)
        {
            _messagesApi.TypingMessage(chatId);
        }

        public INType GetNType(int typeId)
        {
            var type = _serverApi.GetMetadata(0).Types.FirstOrDefault(x => x.Id == typeId);
            return type;
        }

        public void Notify(IEnumerable<DPerson> personChangeset)
        {
            foreach (var person in personChangeset)
            {
                _people[person.Id] = person;
            }
        }
    }

}
