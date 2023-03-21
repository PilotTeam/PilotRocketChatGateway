using System.Text.RegularExpressions;

namespace PilotRocketChatGateway.Utils
{
    public class MarkdownHelper
    {
        public static readonly Regex MarkdownRegex = new Regex(@"(\[([^\]\(\)\r\n]+\])\(([^\[\]\)\n\r]+\)))", RegexOptions.Compiled);
        public static readonly Regex UriRegex = new Regex(@"((http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)", RegexOptions.Compiled);
        public static (Uri, string) CutHyperLink(string str)
        {
            var (flags, text) = GetFlags(str, MarkdownRegex);
            if (flags.Any() == false)
                return (null, str);

            var (uriFlags, _) = GetFlags(flags[0], UriRegex);
            if (uriFlags.Any() == false)
                return (null, str);

            return (new Uri(uriFlags[0]), text.TrimStart());
        }
        private static (IList<string>, string) GetFlags(string input, Regex regex)
        {
            var flags = new List<string>();
            var result = input;

            foreach (Match match in regex.Matches(result))
            {
                flags.Add(match.Value);
                result = result.Replace(match.Value, " ");
            }

            return (flags, result);
        }
    }
}
