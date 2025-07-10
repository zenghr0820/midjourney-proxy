using Midjourney.Infrastructure.Services;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 动作执行成功处理程序
    /// </summary>
    public class ActionSuccessHandler : MessageHandler
    {

        public ActionSuccessHandler()
        {
        }

        public override string MessageHandleType => "ActionSuccessHandler";

        public override int Order() => 99999;

        /// <summary>
        /// 处理动作执行成功消息
        /// </summary>
        protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
        {
 
            string content = message.Content;
            var parseData = ConvertUtils.ParseContent(content, MessageParser.RegexPatterns.CONTENT_REGEX);
            var parseActionData = GetActionContent(content);
            if (messageType == MessageType.CREATE && message.HasImage
                && parseData != null && parseActionData != null
                && message.Author.Bot == true && message.Author.Username.Contains("journey Bot", StringComparison.OrdinalIgnoreCase))
            {
                taskHandler.FindAndFinishTask(
                    instance,
                    parseActionData.Action,
                    parseData.Prompt,
                    message);

            }
        }

        private ContentActionData GetActionContent(string content)
        {
            return ConvertUtils.ParseActionContent(content);
        }
    }
} 