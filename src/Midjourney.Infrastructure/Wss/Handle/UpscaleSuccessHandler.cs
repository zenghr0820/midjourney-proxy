using Midjourney.Infrastructure.LoadBalancer;
using Midjourney.Infrastructure.Util;
using Serilog;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用的放大图像成功处理程序
    /// </summary>
    public class UpscaleSuccessHandler : MessageHandler
    {
        public UpscaleSuccessHandler(DiscordLoadBalancer discordLoadBalancer, DiscordHelper discordHelper)
            : base(discordLoadBalancer, discordHelper)
        {
        }

        public override string MessageHandleType => "Upscale-Success-Handler";

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
            var parseData = MessageParser.ParseUpscaleContent(content);

            if (messageType == MessageType.CREATE && parseData != null && message.HasImage)
            {
                taskHandler.FindAndFinishTask(
                        instance,
                        parseData.Index > 0 ? TaskAction.MIX_UPSCALE : TaskAction.UPSCALE,
                        parseData.Prompt,
                        message);

                // if (parseData.Index > 0)
                // {
                //     taskHandler.FindAndFinishUTask(
                //         instance,
                //         message,
                //         parseData.Prompt,
                //         parseData.Index);
                // }
                // else
                // {
                //    taskHandler.FindAndFinishTask(
                //         instance,
                //         message,
                //         parseData.Prompt,
                //         TaskAction.UPSCALE);
                // }
                
                // 标记为已处理
                // CacheHelper<string, bool>.AddOrUpdate(message.Id, true);
            }
        }
    }
} 