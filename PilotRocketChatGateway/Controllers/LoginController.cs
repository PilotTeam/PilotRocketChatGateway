using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using PilotRocketChatGateway.Controllers.WebSockets;
using PilotRocketChatGateway.PilotServer;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PilotRocketChatGateway.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IContextService _contextService;
        private readonly ILogger<WebSocketsController> _logger;
        private readonly AuthSettings _authSettings;

        public LoginController(IContextService contextService, IOptions<AuthSettings> authSettings, ILogger<WebSocketsController> logger)
        {
            _contextService = contextService;
            _logger = logger;
            _authSettings = authSettings.Value;
        }

        [HttpPost]
        public string Post(LoginRequest user)
        {
            try
            {
                var credentials = Credentials.GetConnectionCredentials(user.user, user.password);
                _contextService.CreateContext(credentials);
                var serverApi = _contextService.GetServerApi(credentials.Username);
                var tokenString = CreateToken(credentials);
                _logger.Log(LogLevel.Information, $"Signed in successfully. Username: {user.user}.");

                var response = new HttpLoginResponse()
                {
                    status = "success",
                    data = new LoginData()
                    {
                        authToken = tokenString,
                        userId = serverApi.CurrentPerson.Id.ToString(),
                        me = new User()
                        {
                            name = serverApi.CurrentPerson.DisplayName,
                            username = serverApi.CurrentPerson.Login,
                        },
                    }
                };
                return JsonConvert.SerializeObject(response); 
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Information, $"Signed in failed. Username: {user.user}.");
                _logger.LogError(0, e, e.Message);
                var error = new Error() { status = "error", error = "Unauthorized", message = e.Message };
                return JsonConvert.SerializeObject(error);
            }
        }

        private string CreateToken(Credentials credentials)
        {
            var secretKey = _authSettings.GetSymmetricSecurityKey();
            var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("actort", credentials.Username)
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
