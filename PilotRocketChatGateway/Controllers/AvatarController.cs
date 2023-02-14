using Microsoft.AspNetCore.Mvc;
using NIdenticon;
using NIdenticon.BrushGenerators;
using PilotRocketChatGateway.Authentication;
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
            int size;
            size = int.Parse(GetParam(nameof(size)));

            var generator = GetGenerator(size, roomId).WithBackgroundColor(Color.Black);

            using (var stream = new MemoryStream())
            {
                generator.Create(roomId).Save(stream, ImageFormat.Png);
                return File(stream.ToArray(), "image/png");
            }
        }


        [Route("/[controller]/{username}")]
        public IActionResult Get(string username)
        {
            int size;
            size = Convert.ToInt32(double.Parse(GetParam(nameof(size)), CultureInfo.InvariantCulture));

            var generator = GetGenerator(size, username); 

            using (var stream = new MemoryStream())
            {
                generator.Create(username).Save(stream, ImageFormat.Png);
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
