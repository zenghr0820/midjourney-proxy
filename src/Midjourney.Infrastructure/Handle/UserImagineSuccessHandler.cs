
using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.LoadBalancer;
using Midjourney.Infrastructure.Models;
using Midjourney.Infrastructure.Util;

namespace Midjourney.Infrastructure.Handle
{
    public class UserImagineSuccessHandler : UserMessageHandler
    {
        private const string CONTENT_REGEX = "\\*\\*(.*)\\*\\* - <@\\d+> \\((.*?)\\)";

        public UserImagineSuccessHandler(DiscordLoadBalancer discordLoadBalancer, DiscordHelper discordHelper)
            : base(discordLoadBalancer, discordHelper)
        {
        }

        public override int Order() => 101;

        public override void Handle(DiscordInstance instance, MessageType messageType, EventData message)
        {
            var content = GetMessageContent(message);
            var parseData = ConvertUtils.ParseContent(content, CONTENT_REGEX);
            if (messageType == MessageType.CREATE && parseData != null && HasImage(message))
            {
                FindAndFinishImageTask(instance, TaskAction.IMAGINE, parseData.Prompt, message);
                
                // 检查是否启用了自动删除imagine消息功能
                var setting = GlobalConfiguration.Setting;
                if (setting?.EnableAutoDeleteImagineMessage == true && message?.Id != null)
                {
                    // 异步删除消息，不等待结果
                    _ = instance.DeleteMessageAsync(message.Id);
                }
            }
        }
    }
}