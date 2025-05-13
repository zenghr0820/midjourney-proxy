using Midjourney.Infrastructure.LoadBalancer;
using Midjourney.Infrastructure.Util;
using Serilog;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用的显示图像成功处理程序
    /// </summary>
    public class ShowSuccessHandler : MessageHandler
    {
        public ShowSuccessHandler(DiscordLoadBalancer discordLoadBalancer, DiscordHelper discordHelper)
            : base(discordLoadBalancer, discordHelper)
        {
        }

        public override int Order() => 77777;

        public override string MessageHandleType => "Show-Success-Handler";

        /// <summary>
        /// 处理通用消息
        /// </summary>
        protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
        {
            // 判断消息是否处理过了
            CacheHelper<string, bool>.TryAdd(message.Id, false);
            if (CacheHelper<string, bool>.Get(message.Id))
            {
                Log.Debug("{0} 消息已经处理过了 {@1}", message.MessageHandler, message.Id);
                return;
            }

            string content = message.Content;

            var imagineParseData = ConvertUtils.ParseContent(content, MessageParser.RegexPatterns.SHORTEN_IMAGINE_CONTENT_REGEX);
            var actionParseData = ConvertUtils.ParseContent(content, MessageParser.RegexPatterns.SHORTEN_ACTION_CONTENT_REGEX);

            var actionParseData2 = ConvertUtils.ParseActionContent(content);
            var actionParseData3 = ConvertUtils.ParseContent(content, MessageParser.RegexPatterns.SHORTEN_CONTENT_REGEX);

            if (messageType == MessageType.CREATE && message.HasImage
                && message.Author.Bot == true && message.Author.Username.Contains("journey Bot", StringComparison.OrdinalIgnoreCase)
                && (imagineParseData != null || actionParseData != null || actionParseData2 != null || actionParseData3 != null))
            {
                taskHandler.FindAndFinishTask(instance, TaskAction.SHOW, 
                imagineParseData?.Prompt ?? actionParseData?.Prompt ?? actionParseData2?.Prompt ?? actionParseData3?.Prompt, message);

            }
        }
    }
} 