﻿using Ascon.Pilot.DataClasses;
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
    class ERROR_CONSTS
    {
        public const string NEED_TO_LOG_OUT = "You've been logged out by the server. Please log in again";
        public const string UNAUTHORIZED = "Unauthorized";
    }

    [Route("api/v1/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IContextsBank _contextsBank;
        private readonly ILogger<LoginController> _logger;
        private readonly AuthSettings _authSettings;
        private readonly IAuthHelper _authHelper;

        public LoginController(IContextsBank contextsBank, IOptions<AuthSettings> authSettings, ILogger<LoginController> logger, IAuthHelper authHelper)
        {
            _contextsBank = contextsBank;
            _logger = logger;
            _authSettings = authSettings.Value;
            _authHelper = authHelper;
        }

        [HttpPost]
        public async Task<string> Post(object request)
        {
            try
            {
                var user = JsonConvert.DeserializeObject<LoginRequest>(request.ToString());
                var response = await LoginAsync(user);
                return JsonConvert.SerializeObject(response);
            }
            catch(PilotSecurityAccessDeniedException e) //if login or password is incorrect
            {
                var error = MekeLoginError(e);
                return JsonConvert.SerializeObject(error);
            }
            catch (UnauthorizedAccessException e) //if need to relogin
            {
                var error = MekeLogOutError(e);
                return JsonConvert.SerializeObject(error);
            }
            catch (Exception e) //if pilot-server is unavailable
            {
                var error = MekeLogOutError(e);
                return JsonConvert.SerializeObject(error);
            }
        }

        private Error MekeLogOutError(Exception e)
        {
            _logger.Log(LogLevel.Information, $"Log in with token failed");
            _logger.LogError(0, e, e.Message);
            var code = 403;
            Response.StatusCode = code;
            var error = new Error() { status = "error", error = code, message = ERROR_CONSTS.NEED_TO_LOG_OUT };
            return error;
        }

        private Error MekeLoginError(Exception e)
        {
            _logger.Log(LogLevel.Information, $"Log in failed");
            _logger.LogError(0, e, e.Message);
            var code = 401;
            Response.StatusCode = code;
            var error = new Error() { status = "error", error = code, message = ERROR_CONSTS.UNAUTHORIZED };
            return error;
        }

        private async Task<HttpLoginResponse> LoginAsync(LoginRequest? user)
        {
            if (user.token == null)
                return await CreateNewSessionAsync(user);

            return ContinueSession(user);
        }

        private HttpLoginResponse ContinueSession(LoginRequest? user)
        {
            var context = _contextsBank.GetContext(_authHelper.GetTokenActor(user.token));
            _logger.Log(LogLevel.Information, $"Resume signed in successfully. Username: {context.RemoteService.ServerApi.CurrentPerson.Login}.");
            return GetLoginResponse(context.RemoteService.ServerApi, context.ChatService, user.token);
        }

        private async Task<IContext> CreateContextAsync(UserData credentials)
        {
            await _contextsBank.CreateContextAsync(credentials);
            return  _contextsBank.GetContext(credentials.Username);
        }

        private async Task<HttpLoginResponse> CreateNewSessionAsync(LoginRequest? user)
        {
            var credentials = UserData.GetConnectionCredentials(user.user, user.password);
            var context = await CreateContextAsync(credentials);
            var tokenString = CreateToken(user);

            _logger.Log(LogLevel.Information, $"Logged in successfully successfully. Username: {user.user}.");
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
                    me = chatService.DataLoader.LoadUser(serverApi.CurrentPerson.Id)
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
                signingCredentials: signinCredentials,
                audience: _authSettings.GetAudience(),
                issuer: _authSettings.Issuer
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokeOptions);
            return tokenString;
        }
    }
}
