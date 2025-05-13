using Midjourney.Infrastructure.LoadBalancer;
using Midjourney.Infrastructure.Util;
using Serilog;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用的动作成功处理程序
    /// </summary>
    public class ActionSuccessHandler : MessageHandler
    {

        public ActionSuccessHandler(DiscordLoadBalancer discordLoadBalancer, DiscordHelper discordHelper)
            : base(discordLoadBalancer, discordHelper)
        {
        }

        public override string MessageHandleType => "Action-Success-Handler";

        public override int Order() => 99999;

        /// <summary>
        /// 处理通用消息
        /// </summary>
        protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
        {
            // 判断消息是否处理过了
            CacheHelper<string, bool>.TryAdd(message.Id, false);
            if (CacheHelper<string, bool>.Get(message.Id))
            {
                Log.Debug("{0} 消息已经处理过了 {@1}", message.IsSocketMessage ? "BOT" : "USER", message.Id);
                return;
            }

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