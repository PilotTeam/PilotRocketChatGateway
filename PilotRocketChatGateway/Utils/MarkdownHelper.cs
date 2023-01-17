using System.Text.RegularExpressions;

namespace PilotRocketChatGateway.Utils
{
    public class MarkdownHelper
    {
        public static (Uri, string) CutHyperLink(string str)
        {
            var regexes = new[]
            {
                new UriMarkdown(new Regex(@"((http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)", RegexOptions.Compiled), (s) => s),
                new UriMarkdown(new Regex(@"(\[([^\]\(\)\r\n]+\])\(([^\[\]\)\n\r]+\)))", RegexOptions.Compiled), (s => s)),
            };

            var (flags, result) = GetFlags(str, regexes);
            if (flags.Count < 2) // chek 3
                return (null, result);

            return (new Uri(flags[0]), result.Replace(flags[1], string.Empty));
        }
        private static (IList<string>, string) GetFlags(string input, UriMarkdown[] regexes)
        {
            var flags = new List<string>();
            var result = input;
            for (var regexIndex = 0; regexIndex < regexes.Length; regexIndex++)
            {
                var regex = regexes[regexIndex];

                foreach (Match match in regex.Regex.Matches(result))
                {
                    flags.Add(match.Value);
                    result = result.Replace(match.Value, " ");
                }
            }

            return (flags, result);
        }
        private class UriMarkdown
        {
            public UriMarkdown(Regex regex, Func<string, string> wrapInMarkdownFunc)
            {
                WrapInMarkdown = wrapInMarkdownFunc;
                Regex = regex;
            }

            public Func<string, string> WrapInMarkdown { get; }
            public Regex Regex { get; }
        }
    }
}
