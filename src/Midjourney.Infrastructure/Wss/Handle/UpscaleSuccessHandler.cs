using Midjourney.Infrastructure.Services;
using Serilog;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用的放大图像成功处理程序
    /// </summary>
    public class UpscaleSuccessHandler : MessageHandler
    {
        public UpscaleSuccessHandler()
        {
        }

        public override string MessageHandleType => "UpscaleSuccessHandler";

        /// <summary>
        /// 处理放大图像成功消息
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

            string content = message.Content;
            var parseData = MessageParser.ParseUpscaleContent(content);

            if (messageType == MessageType.CREATE && parseData != null && message.HasImage)
            {
                taskHandler.FindAndFinishTask(
                        instance,
                        parseData.Index > 0 ? TaskAction.MIX_UPSCALE : TaskAction.UPSCALE,
                        parseData.Prompt,
                        message);
            
            }
        }
    }
} 