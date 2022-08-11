using System.IdentityModel.Tokens.Jwt;

namespace PilotRocketChatGateway.PilotServer
{
    public static class HttpContextExtentions
    {
        public static string GetTokenActor(this HttpContext httpContext)
        {
            httpContext.Request.Headers.TryGetValue("x-auth-token", out var tokenSource);
            if (string.IsNullOrEmpty(tokenSource))
                return null;

            var jwtToken = new JwtSecurityToken(tokenSource.ToString());
            return jwtToken.Actor;

        }
    }
}
