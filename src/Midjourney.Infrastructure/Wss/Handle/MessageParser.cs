using System.Text.RegularExpressions;

namespace Midjourney.Infrastructure.Wss.Handle
{
    public static class MessageParser
    {
        public static class RegexPatterns
        {
            public const string CONTENT_REGEX = "\\*\\*(.*)\\*\\* - (.*?)<@\\d+> \\((.*?)\\)";
            public const string IMAGINE = "\\*\\*(.*)\\*\\* - <@\\d+> \\((.*?)\\)";
            public const string UPSCALE_1 = "\\*\\*(.*)\\*\\* - Upscaled \\(.*?\\) by <@\\d+> \\((.*?)\\)";
            public const string UPSCALE_2 = "\\*\\*(.*)\\*\\* - Upscaled by <@\\d+> \\((.*?)\\)";
            public const string UPSCALE_U = "\\*\\*(.*)\\*\\* - Image #(\\d) <@\\d+>";
            public const string REROLL_CONTENT_REGEX_0 = "\\*\\*(.*)\\*\\* - (.*?)<@\\d+> \\((.*?)\\)";
            public const string REROLL_CONTENT_REGEX_1 = "\\*\\*(.*)\\*\\* - <@\\d+> \\((.*?)\\)";
            public const string REROLL_CONTENT_REGEX_2 = "\\*\\*(.*)\\*\\* - Variations by <@\\d+> \\((.*?)\\)";
            public const string REROLL_CONTENT_REGEX_3 = "\\*\\*(.*)\\*\\* - Variations \\(.*?\\) by <@\\d+> \\((.*?)\\)";
            public const string SHORTEN_ACTION_CONTENT_REGEX = "\\*\\*(.*)\\*\\* - (.*?)<@\\d+>";
            public const string SHORTEN_IMAGINE_CONTENT_REGEX = "\\*\\*(.*)\\*\\* - <@\\d+>";
            public const string SHORTEN_CONTENT_REGEX = "\\*\\*(.*)\\*\\* - <@\\d+>";
        }

        public static ContentParseData ParseContent(string content, string pattern)
        {
            return ConvertUtils.ParseContent(content, pattern);
        }

        public static UContentParseData ParseUpscaleContent(string content)
        {
            // 首先尝试判断是否是U类型的放大
            var matcher = Regex.Match(content, RegexPatterns.UPSCALE_U);
            if (matcher.Success)
            {
                return new UContentParseData
                {
                    Prompt = matcher.Groups[1].Value,
                    Index = int.Parse(matcher.Groups[2].Value),
                    Status = "done"
                };
            }

            // 然后判断是否是普通放大
            var parseData = ParseContent(content, RegexPatterns.UPSCALE_1)
                ?? ParseContent(content, RegexPatterns.UPSCALE_2);

            if (parseData != null)
            {
                return new UContentParseData
                {
                    Prompt = parseData.Prompt,
                    Status = parseData.Status,
                    Index = 0  // 非U类型放大设置Index=0
                };
            }

            return null;
        }

        public static bool IsWaitingToStart(string content)
        {
            return !string.IsNullOrWhiteSpace(content) && content.Contains("(Waiting to start)");
        }
    }

    public class UContentParseData : ContentParseData
    {
        public int Index { get; set; }
    }
}