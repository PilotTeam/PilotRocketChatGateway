﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using System.Web;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private IContextsBank _contextsBank;
        private IAuthHelper _authHelper;

        public RoomsController(IContextsBank contextsBank, IAuthHelper authHelper)
        {
            _contextsBank = contextsBank;
            _authHelper = authHelper;
        }

        [Authorize]
        [HttpGet("api/v1/rooms.get")]
        public string Get()
        {
            string updatedSince;

            updatedSince = GetParam(nameof(updatedSince));

            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            var rooms = context.ChatService.DataLoader.LoadRooms(updatedSince);

            var result = new Rooms() { success = true, update = rooms, remove = new List<Room>() };
            return JsonConvert.SerializeObject(result);
        }

        [Authorize]
        [HttpPost("api/v1/rooms.upload/{roomId}")]
        [Obsolete]
        public async Task<string> Upload(string roomId)
        {
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            var file = HttpContext.Request.Form.Files[0];

            var text = HttpContext.Request.Form["description"];
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                var newFile = await context.ChatService.DataSender.SendAttachmentMessageToServerAsync(roomId, file.FileName, ms.ToArray(), text);
                var result = new MessageUpload()
                {
                    file = newFile,
                    success = true
                };
                return JsonConvert.SerializeObject(result);
            }
        }

        [Authorize]
        [HttpPost("api/v1/rooms.media/{roomId}")]
        public async Task<string> Upload2(string roomId)
        {
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            var file = HttpContext.Request.Form.Files[0];

            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                var newFile = await context.ChatService.DataSender.CreateAttachmentObject(roomId, file.FileName, ms.ToArray());
                var result = new MessageUpload()
                {
                    file = newFile,
                    success = true
                };
                return JsonConvert.SerializeObject(result);
            }
        }

        [Authorize]
        [HttpPost("api/v1/rooms.mediaConfirm/{roomId}/{objId}")]
        public void Confirm(string roomId, string objId)
        {
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));
            using (var reader = new StreamReader(Request.Body))
            {
                var data = reader.ReadToEnd();
                var setting = JsonConvert.DeserializeObject<ConfirmUpload>(data);
                context.ChatService.DataSender.SendAttachmentMessageToServer(roomId, objId, setting.description);
            }
        }

        [Authorize]
        [HttpPost("api/v1/rooms.saveNotification")]
        public string SaveNotification(object request)
        {
            var context = _contextsBank.GetContext(HttpContext.GetTokenActor(_authHelper));

            var setting = JsonConvert.DeserializeObject<SaveNotification>(request.ToString());
            bool isOn = setting.notifications.disableNotifications == "0";

            context.ChatService.DataSender.SendChageNotificationMessageToServer(setting.roomId, isOn);

            var result = new { success = true };
            return JsonConvert.SerializeObject(result);

        }

        private string GetParam(string query)
        {
            return HttpUtility.ParseQueryString(HttpContext.Request.QueryString.ToString()).Get(query) ?? string.Empty;
        }
    }
}
