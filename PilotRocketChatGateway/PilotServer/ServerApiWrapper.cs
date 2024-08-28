using Ascon.Pilot.DataClasses;
using Ascon.Pilot.DataModifier;
using Ascon.Pilot.Server.Api;
using Ascon.Pilot.Server.Api.Contracts;
using Microsoft.Extensions.Options;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.UserContext;
using System.Net;
using System.ServiceModel.Channels;

namespace PilotRocketChatGateway.PilotServer
{
    public class ServerApiWrapper : IServerApi, IMessagesApi, IFileArchiveApi
    {
        private readonly object _locker = new object();
        private readonly HttpPilotClient _client;
        private readonly PilotSettings _config;
        private readonly UserData _credentials;
        private readonly IConnectionService _connector;
        private readonly IAuthenticationAsyncApi _authenticationApi;
        private readonly IServerApi _serverApi;
        private readonly IMessagesApi _messagesApi;
        private readonly IFileArchiveApi _fileArchiveApi;

        private bool _isConnected;


        public ServerApiWrapper(HttpPilotClient client, UserData credentials, IConnectionService connector, IServerApi serverApi, IMessagesApi messagesApi, IFileArchiveApi fileArchiveApi, bool isConnected = false)
        {
            _isConnected = isConnected;
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _credentials = credentials;
            _connector = connector;

            _authenticationApi = client.GetAuthenticationAsyncApi();
            _serverApi = serverApi;
            _fileArchiveApi = fileArchiveApi;
            _messagesApi = messagesApi;
        }

        public bool IsConnected => _isConnected;

        #region Implementation of IServerApi

        public void SendCustomNotification(string name, byte[] data)
        {
            Call(() => _serverApi.SendCustomNotification(name, data));
        }

        DDatabaseInfo IServerApi.OpenDatabase()
        {
            return Call(() => _serverApi.OpenDatabase());
        }

        DDatabaseInfo IServerApi.GetDatabase(string database)
        {
            return Call(() => _serverApi.GetDatabase(database));
        }

        DDatabaseInfo IServerApi.GetDatabaseInfo()
        {
            return Call(() => _serverApi.GetDatabaseInfo());
        }

        DMetadata IServerApi.GetMetadata(long localVersion)
        {
            return Call(() => _serverApi.GetMetadata(localVersion));
        }

        DSettings IServerApi.GetPersonalSettings(string key = null)
        {
            return Call(() => _serverApi.GetPersonalSettings(key));
        }

        DSettings IServerApi.GetCommonSettings()
        {
            return Call(() => _serverApi.GetCommonSettings());
        }

        void IServerApi.ChangeSettings(DSettingsChange change)
        {
            Call(() => _serverApi.ChangeSettings(change));
        }

        List<DObject> IServerApi.GetObjects(Guid[] ids)
        {
            return Call(() => _serverApi.GetObjects(ids));
        }

        public List<(DObject obj, AccessLevel level, List<int> subtypes)> GetObjectsWithRights(Guid[] ids)
        {
            return Call(() => _serverApi.GetObjectsWithRights(ids));
        }

        List<DChangeset> IServerApi.GetChangesets(long first, long last)
        {
            return Call(() => _serverApi.GetChangesets(first, last));
        }

        DChangeset IServerApi.Change(DChangesetData changesetData)
        {
            return Call(() => _serverApi.Change(changesetData));
        }

        void IServerApi.ChangeAsync(DChangesetData changesetData)
        {
            Call(() => _serverApi.ChangeAsync(changesetData));
        }

        List<DPerson> IServerApi.LoadPeople()
        {
            return Call(() => _serverApi.LoadPeople());
        }

        List<DPerson> IServerApi.LoadPeopleByIds(int[] ids)
        {
            return Call(() => _serverApi.LoadPeopleByIds(ids));
        }

        List<DOrganisationUnit> IServerApi.LoadOrganisationUnits()
        {
            return Call(() => _serverApi.LoadOrganisationUnits());
        }

        List<DOrganisationUnit> IServerApi.LoadOrganisationUnitsByIds(int[] ids)
        {
            return Call(() => _serverApi.LoadOrganisationUnitsByIds(ids));
        }

        void IServerApi.AddSearch(DSearchDefinition searchDefinition)
        {
            Call(() => _serverApi.AddSearch(searchDefinition));
        }

        void IServerApi.RemoveSearch(Guid searchDefinitionId)
        {
            Call(() => _serverApi.RemoveSearch(searchDefinitionId));
        }

        void IServerApi.UpdatePerson(DPersonUpdateInfo updateInfo)
        {
            Call(() => _serverApi.UpdatePerson(updateInfo));
        }

        void IServerApi.UpdateOrganisationUnit(DOrganisationUnitUpdateInfo updateInfo)
        {
            Call(() => _serverApi.UpdateOrganisationUnit(updateInfo));
        }

        IEnumerable<DHistoryItem> IServerApi.GetHistoryItems(Guid[] ids)
        {
            return Call(() => _serverApi.GetHistoryItems(ids));
        }

        void IServerApi.InvokeCommand(string commandName, Guid requestId, byte[] data)
        {
            Call(() => _serverApi.InvokeCommand(commandName, requestId, data));
        }

        public void SubscribeCustomNotification(string name)
        {
            Call(() => _serverApi.SubscribeCustomNotification(name));
        }

        public void UnsubscribeCustomNotification(string name)
        {
            Call(() => _serverApi.UnsubscribeCustomNotification(name));
        }

        public IEnumerable<AccessRecord> LoadAccessRecords(Guid objectId)
        {
            return Call(() => _serverApi.LoadAccessRecords(objectId));
        }

        public List<IEnumerable<AccessRecord>> LoadAccessRecordsBatch(Guid[] objectIds)
        {
            return Call(() => _serverApi.LoadAccessRecordsBatch(objectIds));
        }

        public AccessLevel CalcAccessByPerson(Guid objectId, int personid, AccessFlags flags = AccessFlags.None)
        {
            return Call(() => _serverApi.CalcAccessByPerson(objectId, personid, flags));
        }

        public AccessLevel CalcAccessByUnit(Guid objectId, int unitid, AccessFlags flags = AccessFlags.None)
        {
            return Call(() => _serverApi.CalcAccessByUnit(objectId, unitid, flags));
        }

        #endregion

        #region Implementation of IMessagesApi
        public void Open(int maxNotificationCount, DateTime lastMessageDate)
        {
            Call(() => _messagesApi.Open(maxNotificationCount, lastMessageDate));
        }
        DateTime IMessagesApi.SendMessage(DMessage message)
        {
            return Call(() => _messagesApi.SendMessage(message));
        }

        List<NotifiableDChatInfo> IMessagesApi.GetNotifiableChats(int personId, DateTime fromDateTimeServer, DateTime toDateTimeServer, int topN, bool skipObjectRelated)
        {
            return Call(() => _messagesApi.GetNotifiableChats(personId, fromDateTimeServer, toDateTimeServer, topN, skipObjectRelated));
        }

        public List<DChatInfo> GetChats(int personId, DateTime fromDateTimeServer, DateTime toDateTimeServer, int topN,
            bool skipObjectRelated = true)
        {
            return Call(() => _messagesApi.GetChats(personId, fromDateTimeServer, toDateTimeServer, topN, skipObjectRelated));
        }

        NotifiableDChatInfo IMessagesApi.GetNotifiableChat(Guid chatId)
        {
            return Call(() => _messagesApi.GetNotifiableChat(chatId));
        }

        public DChatInfo GetChat(Guid chatId)
        {
            return Call(() => _messagesApi.GetChat(chatId));
        }

        public DChatInfo GetPersonalChat(int personId)
        {
            return Call(() => _messagesApi.GetPersonalChat(personId));
        }

        NotifiableDChatInfo IMessagesApi.GetNotifiablePersonalChat(int personId)
        {
            return Call(() => _messagesApi.GetNotifiablePersonalChat(personId));
        }

        DMessage IMessagesApi.GetChatCreationMessage(Guid chatId)
        {
            return Call(() => _messagesApi.GetChatCreationMessage(chatId));
        }

        List<DChatMember> IMessagesApi.GetChatMembers(Guid chatId, DateTime fromDateUtc)
        {
            return Call(() => _messagesApi.GetChatMembers(chatId, fromDateUtc));
        }

        Tuple<List<DMessage>, int> IMessagesApi.GetMessages(Guid chatId, DateTime dateFromUtc, DateTime dateToUtc, int maxNumber)
        {
            return Call(() => _messagesApi.GetMessages(chatId, dateFromUtc, dateToUtc, maxNumber));
        }
        public DMessage GetLastUnreadMessage(Guid chatId)
        {
            return Call(() => _messagesApi.GetLastUnreadMessage(chatId));
        }

        public void TypingMessage(Guid chatId)
        {
            Call(() => _messagesApi.TypingMessage(chatId));
        }

        public List<DMessage> GetMessagesWithAttachments(Guid chatId, DateTime fromServerDateUtc, DateTime toServerDateUtc, int pageSize)
        {
            return Call(() => _messagesApi.GetMessagesWithAttachments(chatId, fromServerDateUtc, toServerDateUtc, pageSize));
        }

        public List<NotifiableDChatInfo> GetRelatedChats(int personId, Guid objectId, ChatRelationType type)
        {
            return Call(() => _messagesApi.GetRelatedChats(personId, objectId, type));
        }

        public bool CheckIsOnline(int personId)
        {
            return Call(() => _messagesApi.CheckIsOnline(personId));
        }

        public DMessage GetThirdPartyMessage(string thirdPartyInfo)
        {
            return Call(() => _messagesApi.GetThirdPartyMessage(thirdPartyInfo));
        }

        public DMessage GetMessage(Guid messageId)
        {
            return Call(() => _messagesApi.GetMessage(messageId));
        }
        #endregion

        #region Implementation of IFileArchiveApi
        void IFileArchiveApi.PutFileChunk(Guid id, byte[] buffer, long pos)
        {
            Call(() => _fileArchiveApi.PutFileChunk(id, buffer, pos));
        }

        long IFileArchiveApi.GetFilePosition(Guid id)
        {
            return Call(() => _fileArchiveApi.GetFilePosition(id));
        }

        void IFileArchiveApi.PutFileInArchive(DFileBody fileBody)
        {
            Call(() => _fileArchiveApi.PutFileInArchive(fileBody));
        }

        public (Guid ListId, int FileCount) PrepareFileList()
        {
            return Call(() => _fileArchiveApi.PrepareFileList());
        }

        public IEnumerable<(Guid fileId, Guid objectId)> EnumerateFileList(Guid listId, int offset, int count)
        {
            return Call(() => _fileArchiveApi.EnumerateFileList(listId, offset, count));
        }

        public void CloseFileList(Guid listId)
        {
            Call(() => _fileArchiveApi.CloseFileList(listId));
        }

        byte[] IFileArchiveApi.GetFileChunk(Guid id, long pos, int count)
        {
            return Call(() => _fileArchiveApi.GetFileChunk(id, pos, count));
        }
        #endregion

        private T Call<T>(Func<T> func, bool tryReconnect = true)
        {
            try
            {
                CheckConnected();
                return func();
            }
            catch (Exception ex)
            {
                if (ex.ReconnectCouldHelp())
                {
                    if (tryReconnect)
                    {
                        _isConnected = false;
                        return Call(func, false);
                    }
                    _isConnected = false;
                }
                throw;
            }
        }
        private void Call(Action action)
        {
            Call(() =>
            {
                action();
                return true;
            }, true);
        }

        private void CheckConnected()
        {
            if (!_isConnected)
            {
                lock (_locker)
                {
                    if (!_isConnected)
                    {
                        LoginAsync().GetAwaiter().GetResult();
                    }
                }
            }
        }

        public async Task LoginAsync()
        {
            _client.Connect(false);
            await _connector.ConnectAsync(_authenticationApi, _credentials);
            _serverApi.OpenDatabase();
            _messagesApi.Open(30, DateTime.UtcNow);
            _isConnected = true;
        }
        
        public void LostConnection()
        {
            _isConnected = false;
        }

    }
}
