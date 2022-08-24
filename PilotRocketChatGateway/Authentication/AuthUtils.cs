using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace PilotRocketChatGateway.Authentication
{
    public class AuthUtils
    {
        public static string AUTH_HEADER_NAME = "x-auth-token";
        public static TokenValidationParameters GetTokenValidationParameters(AuthSettings authSettings)
        {
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = authSettings.Issuer,
                ValidateAudience = false,
                ValidAudience = authSettings.GetAudience(),
                ValidateLifetime = true,
                IssuerSigningKey = authSettings.GetSymmetricSecurityKey(),
                ValidateIssuerSigningKey = true,
                ClockSkew = authSettings.GetClockCrew()
            };
        }

        public static string GetTokenActor(string token)
        {
            var jwtToken = new JwtSecurityToken(token);
            return jwtToken.Actor;

        }
    }
}
