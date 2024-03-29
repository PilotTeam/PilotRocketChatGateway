﻿using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api.Contracts;
using PilotRocketChatGateway.Utils;
using System;
using System.ServiceModel.Channels;

namespace PilotRocketChatGateway.UserContext
{
    public interface IDataLoader
    {
        IRCDataConverter RCDataConverter { get; }
        Room LoadRoom(DChat chat, DMessage lastMessage);
        IList<Room> LoadRooms(string updatedSince);
        Subscription LoadRoomsSubscription(DChatInfo chat);
        IList<Subscription> LoadRoomsSubscriptions(string updatedSince);
        Room LoadPersonalRoom(string username);
        IList<Message> LoadMessages(string roomId, int count, string upperBound);
        IList<Message> LoadMessages(string roomId, string lowerBound);
        Message LoadMessage(string msgId);
        IList<Message> LoadSurroundingMessages(string rcMsgId, string roomId, int count);
        User LoadUser(int usderId);
        IList<User> LoadUsers(int count);
        IList<User> LoadMembers(string roomId);
        DChatInfo LoadChat(Guid chatId);
        INPerson LoadPerson(int userId);
    }
    public class DataLoader : IDataLoader
    {
        private readonly ICommonDataConverter _commonConverter;
        private readonly IContext _context;
        private readonly IBatchMessageLoader _loader;
        private readonly ILogger _logger;

        public DataLoader(IRCDataConverter rcConverter, ICommonDataConverter commonConverter, IContext context, IBatchMessageLoader msgLoader, ILogger logger)
        {
            RCDataConverter = rcConverter;
            _commonConverter = commonConverter;
            _context = context;
            _loader = msgLoader;
            _logger = logger;
        }
        public IRCDataConverter RCDataConverter { get; }
        public Room LoadRoom(DChat chat, DMessage lastMessage)
        {
            return RCDataConverter.ConvertToRoom(chat, lastMessage);
        }
        public DChatInfo LoadChat(Guid chatId)
        {
            return _context.RemoteService.ServerApi.GetChat(chatId);
        }

        public IList<Room> LoadRooms(string updatedSince)
        {
            DateTime lastUpdated = GetChatListLastUpdated(updatedSince);
            var chats = _context.RemoteService.ServerApi.GetChats(lastUpdated);
            var result = new List<Room>();

            foreach (var chat in chats)
            {
                try
                {
                    var rcChat = RCDataConverter.ConvertToRoom(chat.Chat, chat.LastMessage);
                    result.Add(rcChat);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
            }
            return result;
        }

        private DateTime GetChatListLastUpdated(string updatedSince)
        {
            var date = string.IsNullOrEmpty(updatedSince) ? DateTime.MinValue : _commonConverter.ConvertFromJSDate(updatedSince);

            if (date == DateTime.MinValue)
                return date;

            return date.AddDays(-1);
        }

        public Subscription LoadRoomsSubscription(DChatInfo chat)
        {
            return RCDataConverter.ConvertToSubscription(chat);
        }
        public IList<Subscription> LoadRoomsSubscriptions(string updatedSince)
        {
            var lastUpdated = GetChatListLastUpdated(updatedSince);
            var chats = _context.RemoteService.ServerApi.GetChats(lastUpdated);
            var result = new List<Subscription>();
            foreach (var chat in chats)
            {
                try
                {
                    var rcChat = RCDataConverter.ConvertToSubscription(chat);
                    result.Add(rcChat);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
            }
            return result;
        }
        public Room LoadPersonalRoom(string username)
        {
            var person = _context.RemoteService.ServerApi.GetPerson(username);
            var chat = _context.RemoteService.ServerApi.GetPersonalChat(person.Id);
            return chat.Chat.Id == Guid.Empty ? null : RCDataConverter.ConvertToRoom(chat.Chat, chat.LastMessage);
        }
        public IList<Message> LoadMessages(string roomId, int count, string upperBound)
        {
            var id = _commonConverter.ConvertToChatId(roomId);
            var dateTo = _commonConverter.ConvertFromJSDate(upperBound);
            return LoadMessages(id, DateTime.MinValue.ToUniversalTime(), dateTo.AddMilliseconds(-1), count);
        }
        public IList<Message> LoadMessages(string roomId, string lowerBound)
        {
            var id = _commonConverter.ConvertToChatId(roomId);
            var dateFrom = _commonConverter.ConvertFromJSDate(lowerBound);
            return LoadMessages(id, dateFrom, DateTime.MaxValue.ToUniversalTime(), int.MaxValue);
        }

        public Message LoadMessage(string msgId)
        {
            DMessage? msg = _commonConverter.IsRocketChatId(msgId) ?
                _context.RemoteService.ServerApi.GetMessage(msgId) :
                _context.RemoteService.ServerApi.GetMessage(Guid.Parse(msgId));

            var chat = _context.RemoteService.ServerApi.GetChat(msg.ChatId);
            return RCDataConverter.ConvertToMessage(msg, chat.Chat);
        }

        public IList<Message> LoadSurroundingMessages(string rcMsgId, string roomId, int count)
        {
            Guid msgId = _commonConverter.ConvertToMsgId(rcMsgId);
            Guid chatId = _commonConverter.ConvertToChatId(roomId);
            var chat = _context.RemoteService.ServerApi.GetChat(chatId);
            var messages = _loader.FindMessage(msgId, chatId, count);
            return messages.Select(x => RCDataConverter.ConvertToMessage(x, chat.Chat)).ToList();
        }

        public User LoadUser(int userId)
        {
            var person = _context.RemoteService.ServerApi.GetPerson(userId);
            return _commonConverter.ConvertToUser(person);
        }
        public INPerson LoadPerson(int userId)
        {
            return _context.RemoteService.ServerApi.GetPerson(userId);
        }
        public IList<User> LoadUsers(int count)
        {
            var users = _context.RemoteService.ServerApi.GetPeople().Values;
            var result = new List<User>();
            foreach(var user in users.Where(x => !x.IsDeleted && x.Login != _context.UserData.Username))
            {
                try
                {
                    var rcUser = _commonConverter.ConvertToUser(user);
                    result.Add(rcUser);
                } catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
            }
            return result;
        }
        public IList<User> LoadMembers(string roomId)
        {
            var id = _commonConverter.ConvertToChatId(roomId);
            var members = _context.RemoteService.ServerApi.GetChatMembers(id);
            var result = new List<User>();
            foreach (var member in members)
            {
                try
                {
                    var person = _context.RemoteService.ServerApi.GetPerson(member.PersonId);
                    var rcUser = _commonConverter.ConvertToUser(person);
                    result.Add(rcUser);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
            }
            return result;
        }

        public bool IsChatNotifiable(Guid chatId)
        {
            var member = _context.RemoteService.ServerApi.GetChatMembers(chatId).First(x => x.PersonId == _context.RemoteService.ServerApi.CurrentPerson.Id);
            return member.IsNotifiable;
        }

        private IList<Message> LoadMessages(Guid roomId, DateTime dateFrom, DateTime dateTo, int count)
        {
            var msgs = _context.RemoteService.ServerApi.GetMessages(roomId, dateFrom, dateTo, count);

            if (msgs == null)
                return new List<Message>();

            var chat = _context.RemoteService.ServerApi.GetChat(roomId);
            var result = new List<Message>();
            foreach (var msg in msgs)
            {
                try
                {
                    var rcMsg = RCDataConverter.ConvertToMessage(msg, chat.Chat);
                    result.Add(rcMsg);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
               
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
