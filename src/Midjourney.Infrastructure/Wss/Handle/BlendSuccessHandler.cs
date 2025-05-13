using System.Text.RegularExpressions;
using Midjourney.Infrastructure.LoadBalancer;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用的混合图像成功处理程序
    /// </summary>
    public class BlendSuccessHandler : MessageHandler
    {
        private const int MIN_URLS = 2;
        private const int MAX_URLS = 5;

        public BlendSuccessHandler(DiscordLoadBalancer discordLoadBalancer, DiscordHelper discordHelper)
            : base(discordLoadBalancer, discordHelper)
        {
        }

        public override int Order() => 99998;

        public override string MessageHandleType => "Blend-Success-Handler";

        /// <summary>
        /// 处理通用消息
        /// </summary>
        protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
        {
            var content = message.Content;
            var parseData = MessageParser.ParseContent(content, MessageParser.RegexPatterns.CONTENT_REGEX);

            var urls = ExtractUrls(content);
            var prompt = parseData?.Prompt.FormatPrompt();

            if (messageType == MessageType.CREATE
                && string.IsNullOrWhiteSpace(prompt)
                && message.HasImage
                && parseData != null
                && urls.Count >= MIN_URLS && urls.Count <= MAX_URLS
                && message.Author.Bot == true && message.Author.Username.Contains("journey Bot", StringComparison.OrdinalIgnoreCase))
            {
                taskHandler.FindAndFinishTask(
                    instance,
                    TaskAction.BLEND,
                    parseData.Prompt,
                    message);
            }
        }

        private List<string> ExtractUrls(string content)
        {
            var urls = new List<string>();
            var regex = new Regex(@"https?://[^\s>]+");
            var matches = regex.Matches(content);

            foreach (Match match in matches)
            {
                urls.Add(match.Value);
            }

            return urls;
        }
    }


}