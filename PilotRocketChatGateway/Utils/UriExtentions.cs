using System.Web;
using System;

namespace PilotRocketChatGateway.Utils
{
    public static class UriExtentions
    {
        public static string GetParameter(this Uri uri, string name)
        {
            return HttpUtility.ParseQueryString(uri.Query).Get(name);
        }
    }
}
