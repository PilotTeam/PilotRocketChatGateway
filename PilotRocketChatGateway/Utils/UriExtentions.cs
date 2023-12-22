using System.Web;
using System;

namespace PilotRocketChatGateway.Utils
{
    public static class UriExtentions
    {
        public static string GetParameter(this Uri uri, string name)
        {
            var quary = string.IsNullOrEmpty(uri.Query) ? GetQuaryFromFragment(uri.Fragment) : uri.Query;
            return HttpUtility.ParseQueryString(quary).Get(name);
        }

        private static string GetQuaryFromFragment(string fragment)
        {
            int startIndex = fragment.IndexOf('?');
            if (startIndex == -1)
                return string.Empty;

            return fragment.Substring(startIndex);
        }
    }
}
