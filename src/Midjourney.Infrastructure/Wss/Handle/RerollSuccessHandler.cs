using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.LoadBalancer;
using Midjourney.Infrastructure.Util;
using Serilog;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用的重绘图像成功处理程序
    /// </summary>
    public class RerollSuccessHandler : MessageHandler
    {
        public RerollSuccessHandler()
        {
        }

        public override string MessageHandleType => "RerollSuccessHandler";

        /// <summary>
        /// 处理重绘图像成功消息
        /// </summary>
        protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
        {

            // 判断消息是否处理过了
            CacheHelper<string, bool>.TryAdd(message.Id, false);
            if (CacheHelper<string, bool>.Get(message.Id))
            {
                Log.Debug("消息已经处理过了 {@0}", message.Id);
                return;
            }

            if (message.Author == null || message.Author.Bot != true)
            {
                return;
            }

            string content = message.Content;
            if (message.Author.Id.ToString() == Constants.MJ_APPLICATION_ID)
            {
                // MJ
                var parseData = GetParseData(content);
                if (messageType == MessageType.CREATE && message.HasImage && parseData != null)
                {
                    taskHandler.FindAndFinishTask(instance, TaskAction.REROLL, parseData.Prompt, message);
                }
            }
            else if (message.Author.Id.ToString() == Constants.NIJI_APPLICATION_ID
                && message.Type == (int)Discord.MessageType.Reply)
            {
                // 特殊处理 -> U -> PAN -> R
                // NIJI
                var parseData = ConvertUtils.ParseContent(content, MessageParser.RegexPatterns.REROLL_CONTENT_REGEX_0);
                if (messageType == MessageType.CREATE && message.HasImage && parseData != null)
                {
                    taskHandler.FindAndFinishTask(instance, TaskAction.REROLL, parseData.Prompt, message);
                }
            }
        }

        private ContentParseData GetParseData(string content)
        {
            var parseData = ConvertUtils.ParseContent(content, MessageParser.RegexPatterns.REROLL_CONTENT_REGEX_1)
                ?? ConvertUtils.ParseContent(content, MessageParser.RegexPatterns.REROLL_CONTENT_REGEX_2)
                ?? ConvertUtils.ParseContent(content, MessageParser.RegexPatterns.REROLL_CONTENT_REGEX_3);

            return parseData;
        }
    }
}