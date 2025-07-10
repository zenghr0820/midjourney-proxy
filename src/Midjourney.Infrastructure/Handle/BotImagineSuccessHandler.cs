﻿
using Discord.WebSocket;
using Midjourney.Infrastructure.LoadBalancer;
using Midjourney.Infrastructure.Services;

namespace Midjourney.Infrastructure.Handle
{
    public class BotImagineSuccessHandler : BotMessageHandler
    {
        private const string CONTENT_REGEX = "\\*\\*(.*)\\*\\* - <@\\d+> \\((.*?)\\)";

        public BotImagineSuccessHandler(DiscordLoadBalancer discordLoadBalancer, DiscordHelper discordHelper)
            : base(discordLoadBalancer, discordHelper)
        {
        }

        public override int Order() => 101;

        public override void Handle(DiscordInstance instance, MessageType messageType, SocketMessage message)
        {
            var content = GetMessageContent(message);
            var parseData = ConvertUtils.ParseContent(content, CONTENT_REGEX);
            if (messageType == MessageType.CREATE && parseData != null && HasImage(message))
            {
                FindAndFinishImageTask(instance, TaskAction.IMAGINE, parseData.Prompt, message);
            }
        }
    }
}