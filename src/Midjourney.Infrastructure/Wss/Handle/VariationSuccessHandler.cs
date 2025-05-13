
using Midjourney.Infrastructure.LoadBalancer;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用的变体图像成功处理程序
    /// </summary>
    public class VariationSuccessHandler : MessageHandler
    {
        public VariationSuccessHandler(DiscordLoadBalancer discordLoadBalancer, DiscordHelper discordHelper)
            : base(discordLoadBalancer, discordHelper)
        {
        }

        public override string MessageHandleType => "Variation-Success-Handler";

        /// <summary>
        /// 处理通用消息
        /// </summary>
        protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
        {
            var content = message.Content;
            var parseData = MessageParser.ParseContent(content, MessageParser.RegexPatterns.REROLL_CONTENT_REGEX_2)
                ?? MessageParser.ParseContent(content, MessageParser.RegexPatterns.REROLL_CONTENT_REGEX_3);
            if (messageType == MessageType.CREATE && parseData != null && message.HasImage)
            {
                taskHandler.FindAndFinishTask( instance, TaskAction.VARIATION, parseData.Prompt, message);
            }
        }

    }
} 