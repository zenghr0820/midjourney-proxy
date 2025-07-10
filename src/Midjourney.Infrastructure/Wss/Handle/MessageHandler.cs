using Midjourney.Infrastructure.Services;
using Serilog;
using Serilog.Context;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用消息处理程序，支持处理 Discord 机器人消息和用户消息
    /// </summary>
    public abstract class MessageHandler
    {
        protected readonly TaskHandler taskHandler;

        protected MessageHandler()
        {
            taskHandler = new TaskHandler();

        }

        /// <summary>
        /// 消息处理器类型
        /// </summary>
        public abstract string MessageHandleType { get; }

        /// <summary>
        /// 消息处理顺序
        /// </summary>
        public virtual int Order() => 100;

        public void Handle(DiscordInstance instance, MessageType messageType, MessageWrapper message)
        {
            using (LogContext.PushProperty("LogPrefix", MessageHandleType))
            {
                Log.Debug("Start handle message guildId: {0} channelId: {1} messageId: {2} messageType: {3}", instance.Account.GuildId, message.ChannelId, message.Id, messageType);

                // var wrapper = new MessageWrapper(message);

                HandleMessage(instance, messageType, message);
            }
        }

        // 子类必须实现的扩展点（可改为 virtual 空方法，若允许部分子类不实现）
        protected abstract void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message);


        /// <summary>
        /// 替换CDN URL
        /// </summary>
        /// <param name="imageUrl">图片URL</param>
        /// <returns>替换后的图片URL</returns>
        protected string ReplaceCdnUrl(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return imageUrl;

            string cdn = DiscordHelper.GetCdn();
            if (imageUrl.StartsWith(cdn)) return imageUrl;

            return imageUrl.Replace(DiscordHelper.DISCORD_CDN_URL, cdn);
        }
    }
}