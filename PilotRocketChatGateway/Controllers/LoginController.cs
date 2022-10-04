using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PilotRocketChatGateway.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IContextService _contextService;
        private readonly ILogger<LoginController> _logger;
        private readonly AuthSettings _authSettings;
        private readonly IAuthHelper _authHelper;

        public LoginController(IContextService contextService, IOptions<AuthSettings> authSettings, ILogger<LoginController> logger, IAuthHelper authHelper)
        {
            _contextService = contextService;
            _logger = logger;
            _authSettings = authSettings.Value;
            _authHelper = authHelper;
        }

        [HttpPost]
        public string Post(object request)
        {
            try
            {
                var user = JsonConvert.DeserializeObject<LoginRequest>(request.ToString());
                var response = Login(user);
                return JsonConvert.SerializeObject(response);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Information, $"Signed in failed");
                _logger.LogError(0, e, e.Message);
                var error = new Error() { status = "error", error = "Unauthorized", message = e.Message };
                return JsonConvert.SerializeObject(error);
            }
        }

        private HttpLoginResponse Login(LoginRequest? user)
        {
            if (user.token == null)
                return CreateNewSession(user);

            return ContinueSession(user);
        }

        private HttpLoginResponse ContinueSession(LoginRequest? user)
        {
            var context = _contextService.GetContext(_authHelper.GetTokenActor(user.token));
            _logger.Log(LogLevel.Information, $"Resume signed in successfully. Username: {context.RemoteService.ServerApi.CurrentPerson.Login}.");
            return GetLoginResponse(context.RemoteService.ServerApi, context.ChatService, user.token);
        }

        private IContext CreateContext(Credentials credentials)
        {
            _contextService.CreateContext(credentials);
            return  _contextService.GetContext(credentials.Username);
        }

        private HttpLoginResponse CreateNewSession(LoginRequest? user)
        {
            var credentials = Credentials.GetConnectionCredentials(user.user, user.password);
            var context = CreateContext(credentials);
            var tokenString = CreateToken(user);

            _logger.Log(LogLevel.Information, $"Signed in successfully. Username: {user.user}.");
            return GetLoginResponse(context.RemoteService.ServerApi, context.ChatService, tokenString);
        }

        private static HttpLoginResponse GetLoginResponse(IServerApiService serverApi, IChatService chatService, string tokenString)
        {
            return new HttpLoginResponse()
            {
                status = "success",
                data = new LoginData()
                {
                    authToken = tokenString,
                    userId = serverApi.CurrentPerson.Id.ToString(),
                    me = chatService.LoadUser(serverApi.CurrentPerson.Id)
                }
            };
        }

        private string CreateToken(LoginRequest user)
        {
            var secretKey = _authSettings.GetSymmetricSecurityKey();
            var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("actort", user.user),
            };

            var tokeOptions = new JwtSecurityToken(
                claims: claims,
                expires: _authSettings.GetTokenLifetime(_authSettings.TokenLifeTimeDays),
                signingCredentials: signinCredentials,
                audience: _authSettings.GetAudience(),
                issuer: _authSettings.Issuer
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokeOptions);
            return tokenString;
        }
    }
}
