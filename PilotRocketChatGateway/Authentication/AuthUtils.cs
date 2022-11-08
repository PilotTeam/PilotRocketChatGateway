using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace PilotRocketChatGateway.Authentication
{
    public interface IAuthHelper
    {
        TokenValidationParameters GetTokenValidationParameters(AuthSettings authSettings);
        string GetTokenActor(string token);
        bool ValidateToken(string token, AuthSettings authSettings);
    }
    public class AuthHelper : IAuthHelper
    {
        public static string AUTH_HEADER_NAME = "x-auth-token";
        public TokenValidationParameters GetTokenValidationParameters(AuthSettings authSettings)
        {
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = authSettings.Issuer,
                ValidateAudience = false,
                ValidAudience = authSettings.GetAudience(),
                ValidateLifetime = false,
                IssuerSigningKey = authSettings.GetSymmetricSecurityKey(),
                ValidateIssuerSigningKey = true,
                ClockSkew = authSettings.GetClockCrew()
            };
        }

        public string GetTokenActor(string token)
        {
            var jwtToken = new JwtSecurityToken(token);
            return jwtToken.Actor;

        }
        public bool ValidateToken(string token, AuthSettings authSettings)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ValidateToken(token, GetTokenValidationParameters(authSettings), out SecurityToken validatedToken);
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
