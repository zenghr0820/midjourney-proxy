using Discord.WebSocket;
using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.LoadBalancer;
using Serilog;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用消息处理程序，支持处理 Discord 机器人消息和用户消息
    /// </summary>
    public abstract class MessageHandler(DiscordLoadBalancer discordLoadBalancer, DiscordHelper discordHelper)
    {
        protected readonly DiscordLoadBalancer discordLoadBalancer = discordLoadBalancer;
        protected readonly DiscordHelper discordHelper = discordHelper;
        protected readonly TaskHandler taskHandler = new TaskHandler(discordHelper);

        /// <summary>
        /// 消息处理器类型
        /// </summary>
        public abstract string MessageHandleType { get; }

        /// <summary>
        /// 消息处理顺序
        /// </summary>
         public virtual int Order() => 100;

        /// <summary>
        /// 处理机器人消息
        /// </summary>
        public virtual void Handle(DiscordInstance instance, MessageType messageType, SocketMessage message)
        {
            var wrapper = new MessageWrapper(message, discordHelper);
            HandleMessage(instance, messageType, wrapper);
        }

        /// <summary>
        /// 处理用户消息
        /// </summary>
        public virtual void Handle(DiscordInstance instance, MessageType messageType, EventData message)
        {
            var wrapper = new MessageWrapper(message, discordHelper);
            HandleMessage(instance, messageType, wrapper);
        }

        /// <summary>
        /// 处理通用消息
        /// </summary>
        protected virtual void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
        {
            // 跳过 Waiting to start 消息
            if (MessageParser.IsWaitingToStart(message.Content))
            {
                Log.Debug("跳过 Waiting to start 消息 {@0}", message.Id);
                return;
            }
            // 子类实现通用消息处理逻辑
        }

        /// <summary>
        /// 替换CDN URL
        /// </summary>
        /// <param name="imageUrl">图片URL</param>
        /// <returns>替换后的图片URL</returns>
        protected string ReplaceCdnUrl(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return imageUrl;

            string cdn = discordHelper.GetCdn();
            if (imageUrl.StartsWith(cdn)) return imageUrl;

            return imageUrl.Replace(DiscordHelper.DISCORD_CDN_URL, cdn);
        }
    }
} 