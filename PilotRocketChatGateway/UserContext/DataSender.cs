﻿using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.Utils;
using PilotRocketChatGateway.WebSockets;
using System;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace PilotRocketChatGateway.UserContext
{
    public interface IDataSender
    {
        void SendTextMessageToServer(string roomId, string msgId, string text, Uri replyLink);
        void SendEditMessageToServer(string roomId, string msgId, string text);
        void SendReadAllMessageToServer(string roomId);
        void SendTypingMessageToServer(string roomId);
        void SendChageNotificationMessageToServer(string roomId, bool on);
        Room SendChatCreationMessageToServer(string name, IList<string> members, ChatKind kind);
        Task<FileAttachment> CreateAttachmentObject(string roomId, string fileName, byte[] data);
        void SendAttachmentMessageToServer(string roomId, string objId, string text);
        [Obsolete]
        Task<FileAttachment> SendAttachmentMessageToServerAsync(string roomId, string fileName, byte[] data, string text);
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
        public void SendTextMessageToServer(string roomId, string rcMsgId, string text, Uri replyLink)
        {
            var chatId = _commonConverter.ConvertToChatId(roomId);
            DMessage dMessage = null;
            if (replyLink == null)
            {
                dMessage = CreateMessage(Guid.NewGuid(), chatId, MessageType.TextMessage);
            }
            else
            {
                var id = replyLink.GetParameter("msg");
                var reply = _commonConverter.IsRocketChatId(id) ?
                            _context.RemoteService.ServerApi.GetMessage(id) :
                            _context.RemoteService.ServerApi.GetMessage(Guid.Parse(id));
                dMessage = CreateMessage(Guid.NewGuid(), chatId, MessageType.MessageAnswer, reply.Id);
                dMessage.RelatedMessages.Add(reply); 
            }
            var data = new DTextMessageData { Text = text, ThirdPartyInfo = rcMsgId };

            SetMessageData(dMessage, data);
            dMessage.ServerDate = _context.RemoteService.ServerApi.SendMessage(dMessage);

            var chatid = _commonConverter.ConvertToChatId(roomId);
            var chat = _context.RemoteService.ServerApi.GetChat(chatid);
            _context.WebSocketsNotifyer.NotifyMessageCreated(dMessage, chat, NotifyClientKind.FullChat);
        }
        public void SendEditMessageToServer(string roomId, string strMsgId, string text)
        {
            var relatedMsgId = _commonConverter.ConvertToMsgId(strMsgId);
            if (relatedMsgId == Guid.Empty)
                return;

            var chatId = _commonConverter.ConvertToChatId(roomId);

            var edit = CreateMessage(Guid.NewGuid(),chatId, MessageType.EditTextMessage, relatedMsgId);
            var data = new DTextMessageData { Text = text };

            var origin = _context.RemoteService.ServerApi.GetMessage(relatedMsgId);
            origin.RelatedMessages.Add(edit);

            SetMessageData(edit, data);
            edit.ServerDate = _context.RemoteService.ServerApi.SendMessage(edit);

            var id = _commonConverter.ConvertToChatId(roomId);
            var chat = _context.RemoteService.ServerApi.GetChat(id);
            _context.WebSocketsNotifyer.NotifyMessageCreated(origin, chat, NotifyClientKind.FullChat);
        }

        public async Task<FileAttachment> CreateAttachmentObject(string roomId, string fileName, byte[] data)
        {
            var obj = await _context.RemoteService.ServerApi.CreateAttachmentObjectAsync(fileName, data);
            return _rcConverter.AttachmentLoader.LoadFileAttachment(obj, roomId, obj.Id.ToString());
        }

        public void SendAttachmentMessageToServer(string roomId, string objId, string text)
        {
            var chatId = _commonConverter.ConvertToChatId(roomId);
            var dMessage = CreateMessage(Guid.Parse(objId), chatId, MessageType.TextMessage);
            var msgData = GetAttachmentsMessageData(Guid.Parse(objId), dMessage.Id, text);

            SetMessageData(dMessage, msgData);
            dMessage.ServerDate = _context.RemoteService.ServerApi.SendMessage(dMessage);

            var chat = _context.RemoteService.ServerApi.GetChat(chatId);
            _context.WebSocketsNotifyer.NotifyMessageCreated(dMessage, chat, NotifyClientKind.FullChat);
        }


        [Obsolete]
        public async Task<FileAttachment> SendAttachmentMessageToServerAsync(string roomId, string fileName, byte[] data, string text)
        {
            var obj = await _context.RemoteService.ServerApi.CreateAttachmentObjectAsync(fileName, data);
            var chatId = _commonConverter.ConvertToChatId(roomId);
            var dMessage = CreateMessage(Guid.NewGuid(), chatId, MessageType.TextMessage);
            var msgData = GetAttachmentsMessageData(obj.Id, dMessage.Id, text);

            SetMessageData(dMessage, msgData);
            dMessage.ServerDate = _context.RemoteService.ServerApi.SendMessage(dMessage);

            var chat = _context.RemoteService.ServerApi.GetChat(chatId);
            _context.WebSocketsNotifyer.NotifyMessageCreated(dMessage, chat, NotifyClientKind.FullChat);

            return _rcConverter.AttachmentLoader.LoadFileAttachment(obj, roomId, dMessage.Id.ToString());
        }
        public void SendChageNotificationMessageToServer(string roomId, bool on)
        {
            var chatId = _commonConverter.ConvertToChatId(roomId);
            var dMessage = CreateMessage(Guid.NewGuid(), chatId, MessageType.ChatMembers);

            var data = new DChatMembersData();
            data.Changes.Add(new DMemberChange()
            {
                PersonId = _context.RemoteService.ServerApi.CurrentPerson.Id,
                IsNotifiable = on
            });
            SetMessageData(dMessage, data);

            dMessage.ServerDate = _context.RemoteService.ServerApi.SendMessage(dMessage);

            var id = _commonConverter.ConvertToChatId(roomId);
            var chat = _context.RemoteService.ServerApi.GetChat(id);
            _context.WebSocketsNotifyer.NotifyMessageCreated(dMessage, chat, NotifyClientKind.FullChat);
        }

        public void SendReadAllMessageToServer(string roomId)
        {
            var id = _commonConverter.ConvertToChatId(roomId);
            var chat = _context.RemoteService.ServerApi.GetChat(id);
            if (chat.UnreadMessagesNumber == 0)
                return;

            var msg = CreateMessage(Guid.NewGuid(), id, MessageType.MessageDropUnreadCounter, chat.LastMessage?.Id);
            _context.RemoteService.ServerApi.SendMessage(msg);
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

            var msg = CreateMessage(Guid.NewGuid(), chat.Id, MessageType.ChatCreation);
            chat.LastMessageId = msg.Id;

            SetMessageData(msg, chat);
            msg.ServerDate = _context.RemoteService.ServerApi.SendMessage(msg);
            foreach (var member in members)
                SendChatsMemberMessageToServer(chat.Id, member);

            var dchat = _context.RemoteService.ServerApi.GetChat(msg.ChatId);
            _context.WebSocketsNotifyer.NotifyMessageCreated(msg, dchat, NotifyClientKind.Chat);

            return _rcConverter.ConvertToRoom(chat, msg);
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

        private DMessage CreateMessage(Guid msgId, Guid chatId, MessageType type, Guid? relatedMessageId = null)
        {
            return new DMessage()
            {
                Id = msgId,
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
            var msg = CreateMessage(Guid.NewGuid(), roomId, MessageType.ChatMembers);
            var person = _context.RemoteService.ServerApi.GetPerson(username);

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
