using Midjourney.Infrastructure.Services;
using Serilog;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用的描述图像成功处理程序
    /// </summary>
    public class DescribeSuccessHandler : MessageHandler
    {

        public DescribeSuccessHandler()
        {
        }

        public override int Order() => 88888;

        public override string MessageHandleType => "DescribeSuccessHandler";

        /// <summary>
        /// 处理图生文成功消息
        /// </summary>
        protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
        {
            if (MessageParser.IsWaitingToStart(message.Content))
            {
                return;
            }

            // 判断消息是否处理过了
            CacheHelper<string, bool>.TryAdd(message.Id, false);
            if (CacheHelper<string, bool>.Get(message.Id))
            {
                Log.Debug("消息已经处理过了 {@0}", message.Id);
                return;
            }

            if (messageType == MessageType.CREATE
                && message.Author.Bot == true
                && message.Author.Username.Contains("journey Bot", StringComparison.OrdinalIgnoreCase))
            {
                // 图生文完成
                if (message.Embeds.Count > 0 && !string.IsNullOrWhiteSpace(message.Embeds.FirstOrDefault()?.Image?.Url))
                {

                    // 查询并更新图生文任务
                    taskHandler.FindAndFinishDescribeTask(instance, message, null);
                }
            }
        }

    }
}