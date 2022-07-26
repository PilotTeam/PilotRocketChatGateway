﻿using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.WebSockets;

namespace PilotRocketChatGateway.UserContext
{
    public interface IDataSender
    {
        void SendTextMessageToServer(string roomId, string msgId, string text);
        void SendEditMessageToServer(string roomId, string msgId, string text);
        void SendAttachmentMessageToServer(string roomId, string fileName, byte[] data, string text);
        void SendReadAllMessageToServer(string roomId);
        void SendTypingMessageToServer(string roomId);
        Room SendChatCreationMessageToServer(string name, IList<string> members, ChatKind kind);
    }
    public class DataSender : IDataSender
    {
        private readonly IRCDataConverter _rcConverter;
        private readonly ICommonDataConverter _commonConverter;
        private readonly IContext _context;

        public DataSender(IRCDataConverter rcConverter, ICommonDataConverter commonConverter, IContext context)
        {
            _rcConverter = rcConverter;
            _commonConverter = commonConverter;
            _context = context;
        }
        public void SendTextMessageToServer(string roomId, string rcMsgId, string text)
        {
            var chatId = _commonConverter.ConvertToChatId(roomId);
            var dMessage = CreateMessage(chatId, MessageType.TextMessage);
            var data = new DTextMessageData { Text = text, ThirdPartyInfo = rcMsgId };

            SetMessageData(dMessage, data);
            _context.RemoteService.ServerApi.SendMessage(dMessage);
            _context.WebSocketsNotifyer.NotifyMessageCreated(dMessage, NotifyClientKind.Chat);
        }
        public void SendEditMessageToServer(string roomId, string strMsgId, string text)
        {
            var relatedMsgId = _commonConverter.ConvertToMsgId(strMsgId);
            if (relatedMsgId == Guid.Empty)
                return;

            var chatId = _commonConverter.ConvertToChatId(roomId);

            var edit = CreateMessage(chatId, MessageType.EditTextMessage, relatedMsgId);
            var data = new DTextMessageData { Text = text };

            var origin = _context.RemoteService.ServerApi.GetMessage(relatedMsgId);
            origin.RelatedMessages.Add(edit);

            SetMessageData(edit, data);
            _context.RemoteService.ServerApi.SendMessage(edit);
            _context.WebSocketsNotifyer.NotifyMessageCreated(origin, NotifyClientKind.FullChat);
        }
        public void SendAttachmentMessageToServer(string roomId, string fileName, byte[] data, string text)
        {
            var objId = _context.RemoteService.ServerApi.CreateAttachmentObject(fileName, data);
            var chatId = _commonConverter.ConvertToChatId(roomId);
            var dMessage = CreateMessage(chatId, MessageType.TextMessage);
            var msgData = GetAttachmentsMessageData(objId, dMessage.Id, text);

            SetMessageData(dMessage, msgData);
            _context.RemoteService.ServerApi.SendMessage(dMessage);
            _context.WebSocketsNotifyer.NotifyMessageCreated(dMessage, NotifyClientKind.FullChat);
        }
        public void SendReadAllMessageToServer(string roomId)
        {
            var id = _commonConverter.ConvertToChatId(roomId);
            var chat = _context.RemoteService.ServerApi.GetChat(id);
            if (chat.UnreadMessagesNumber == 0)
                return;
            var unreads = _context.RemoteService.ServerApi.GetMessages(id, DateTime.MaxValue, chat.UnreadMessagesNumber);
            foreach (var unread in unreads)
            {
                var msg = CreateMessage(id, MessageType.MessageRead, unread.Id);
                _context.RemoteService.ServerApi.SendMessage(msg);
            }
        }
        public void SendTypingMessageToServer(string roomId)
        {
            var chatId = _commonConverter.ConvertToChatId(roomId);
            _context.RemoteService.ServerApi.SendTypingMessage(chatId);
        }
        public Room SendChatCreationMessageToServer(string name, IList<string> members, ChatKind kind)
        {
            var chat = new DChat
            {
                Id = Guid.NewGuid(),
                Name = name,
                Type = kind,
                CreatorId = _context.RemoteService.ServerApi.CurrentPerson.Id,
                CreationDateUtc = DateTime.UtcNow
            };

            var msg = CreateMessage(chat.Id, MessageType.ChatCreation);
            chat.LastMessageId = msg.Id;

            SetMessageData(msg, chat);
            _context.RemoteService.ServerApi.SendMessage(msg);
            foreach (var member in members)
                SendChatsMemberMessageToServer(chat.Id, member);

            _context.WebSocketsNotifyer.NotifyMessageCreated(msg, NotifyClientKind.Chat);

            return _rcConverter.ConvertToRoom(chat, new List<DChatRelation>(), msg);
        }
        private DTextMessageData GetAttachmentsMessageData(Guid objId, Guid messageId, string text)
        {
            var relation = new DChatRelation
            {
                Type = ChatRelationType.Attach,
                ObjectId = objId,
                MessageId = messageId,
                IsDeleted = false
            };

            var data = new DTextMessageData { Text = text };
            data.Attachments = new List<DChatRelation>() { relation };
            return data;
        }

        private DMessage CreateMessage(Guid chatId, MessageType type, Guid? relatedMessageId = null)
        {
            return new DMessage()
            {
                Id = Guid.NewGuid(),
                CreatorId = _context.RemoteService.ServerApi.CurrentPerson.Id,
                ChatId = chatId,
                LocalDate = DateTime.Now.ToUniversalTime(),
                Type = type,
                RelatedMessageId = relatedMessageId
            };
        }
        private static void SetMessageData<T>(DMessage message, T data)
        {
            using (var stream = new MemoryStream())
            {
                ProtoSerializer.Serialize(stream, data);
                stream.Position = 0;
                message.Data = stream.ToArray();
            }
        }
        private void SendChatsMemberMessageToServer(Guid roomId, string username)
        {
            var msg = CreateMessage(roomId, MessageType.ChatMembers);
            var person = _context.RemoteService.ServerApi.GetPerson((x) => x.Login == username);

            var change = new DMemberChange
            {
                PersonId = person.Id,
                IsNotifiable = true,
                IsAdded = true,
                IsViewerOnly = false
            };

            var memberData = new DChatMembersData();
            memberData.Changes.Add(change);

            SetMessageData(msg, memberData);
            _context.RemoteService.ServerApi.SendMessage(msg);
        }
    }
}
