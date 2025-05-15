using Midjourney.Infrastructure.LoadBalancer;
using Serilog;
namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用的生成图像成功处理程序
    /// </summary>
    public class ImagineSuccessHandler : MessageHandler
    {
        public ImagineSuccessHandler(ILogger logger)
            : base(logger)
        {
        }

        public override int Order() => 101;

        public override string MessageHandleType => "Imagine-Success-Handler";

        /// <summary>
        /// 处理绘图消息
        /// </summary>
        protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
        {
            var content = message.Content;
            var parseData = MessageParser.ParseContent(content, MessageParser.RegexPatterns.IMAGINE);
            if (messageType == MessageType.CREATE && parseData != null && message.HasImage)
            {
                taskHandler.FindAndFinishTask(
                    instance,
                    TaskAction.IMAGINE,
                    parseData.Prompt,
                    message);
            }
        }
    }
} 