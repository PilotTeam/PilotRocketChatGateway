using PilotRocketChatGateway.Authentication;
using System.IdentityModel.Tokens.Jwt;

namespace PilotRocketChatGateway.PilotServer
{
    public static class HttpContextExtentions
    {
        public static string GetTokenActor(this HttpContext httpContext, IAuthHelper authHelper)
        {
            httpContext.Request.Headers.TryGetValue(AuthHelper.AUTH_HEADER_NAME, out var tokenSource);
            if (string.IsNullOrEmpty(tokenSource))
                return null;

            return authHelper.GetTokenActor(tokenSource);
        }
    }
}
