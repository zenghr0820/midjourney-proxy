using Midjourney.Infrastructure.Services;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用的变体图像成功处理程序
    /// </summary>
    public class VariationSuccessHandler : MessageHandler
    {
        public VariationSuccessHandler()
        {
        }

        public override string MessageHandleType => "VariationSuccessHandler";

        /// <summary>
        /// 处理变体图像成功消息
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