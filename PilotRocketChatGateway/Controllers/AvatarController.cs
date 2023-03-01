using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NIdenticon;
using NIdenticon.BrushGenerators;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Web;

namespace PilotRocketChatGateway.Controllers
{
    [ApiController]
    public class AvatarController : ControllerBase
    {
        private const int BLOCK_SIZE = 5; 

        private readonly IContextService _contextService;
        private readonly IAuthHelper _authHelper;

        public AvatarController(IContextService contextService, IAuthHelper authHelper)
        {
            _contextService = contextService;
            _authHelper = authHelper;
        }

        [Route("/[controller]/Room/{roomId}")]
        public IActionResult Room(string roomId)
        {
            string rc_token;
            rc_token = GetParam(nameof(rc_token));

            var user = _authHelper.GetTokenActor(rc_token);
            if (string.IsNullOrEmpty(user))
                throw new UnauthorizedAccessException();

            var context = _contextService.GetContext(user);
            var chatId = context.ChatService.DataLoader.RCDataConverter.CommonDataConverter.ConvertToChatId(roomId);
            var room = context.RemoteService.ServerApi.GetChat(chatId);

            int size;
            size = int.Parse(GetParam(nameof(size)));

            var generator = GetGenerator(size, room.Chat.Name);

            using (var stream = new MemoryStream())
            {
                generator.Create(room.Chat.Name).Save(stream, ImageFormat.Png);
                return File(stream.ToArray(), "image/png");
            }
        }

        [Route("/[controller]/{name}")]
        public IActionResult Get(string name)
        {
            string rc_token;
            rc_token = GetParam(nameof(rc_token));

            var user = _authHelper.GetTokenActor(rc_token);
            if (string.IsNullOrEmpty(user))
                throw new UnauthorizedAccessException();

            int size;
            size = Convert.ToInt32(double.Parse(GetParam(nameof(size)), CultureInfo.InvariantCulture));

            var generator = GetGenerator(size, name); 

            using (var stream = new MemoryStream())
            {
                generator.Create(name).Save(stream, ImageFormat.Png);
                return File(stream.ToArray(), "image/png");
            }
        }

        private IdenticonGenerator GetGenerator(int size, string text)
        {
            return new IdenticonGenerator()
                        .WithSize(new Size(size, size))
                        .WithBlocks(BLOCK_SIZE, BLOCK_SIZE)
                        .WithBlockGenerators(IdenticonGenerator.ExtendedBlockGeneratorsConfig)
                        .WithBrushGenerator(new StaticColorBrushGenerator(StaticColorBrushGenerator.ColorFromText(text)))
                        ;
        }
        private string GetParam(string query)
        {
            return HttpUtility.ParseQueryString(HttpContext.Request.QueryString.ToString()).Get(query) ?? string.Empty;
        }
    }
}
