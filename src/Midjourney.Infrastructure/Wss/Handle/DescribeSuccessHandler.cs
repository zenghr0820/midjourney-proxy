using Midjourney.Infrastructure.LoadBalancer;
using Midjourney.Infrastructure.Util;
using Serilog;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用的描述图像成功处理程序
    /// </summary>
    public class DescribeSuccessHandler : MessageHandler
    {

        public DescribeSuccessHandler(ILogger logger)
            : base(logger)
        {
        }

        public override int Order() => 88888;

        public override string MessageHandleType => "Describe-Success-Handler";

        /// <summary>
        /// 处理通用消息
        /// </summary>
        protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
        {
            // 判断消息是否处理过了
            CacheHelper<string, bool>.TryAdd(message.Id, false);
            if (CacheHelper<string, bool>.Get(message.Id))
            {
                Log.Debug("[{@0}] - 消息已经处理过了 {@1}", MessageHandleType, message.Id);
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

                    // 标记为已处理
                    CacheHelper<string, bool>.AddOrUpdate(message.Id, true);
                }
            }
        }

    }
}