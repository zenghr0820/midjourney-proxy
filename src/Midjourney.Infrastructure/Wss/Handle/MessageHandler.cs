using System.Collections.Concurrent;
using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.LoadBalancer;
using Serilog;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用消息处理程序，支持处理 Discord 机器人消息和用户消息
    /// </summary>
    public abstract class MessageHandler
    {
        protected readonly TaskHandler taskHandler;

        protected ILogger Log { get; }

        private static readonly ConcurrentDictionary<string, ILogger> _loggers = new();

        protected MessageHandler( ILogger logger)
        {
            taskHandler = new TaskHandler();
            Log = _loggers.GetOrAdd(MessageHandleType, name => new PrefixLogger(logger, name));
        }

        /// <summary>
        /// 消息处理器类型
        /// </summary>
        public abstract string MessageHandleType { get; }

        /// <summary>
        /// 消息处理顺序
        /// </summary>
        public virtual int Order() => 100;

        public void Handle(DiscordInstance instance, MessageType messageType, EventData message)
        {
            if (MessageParser.IsWaitingToStart(message.Content))
            {
                Log.Debug("跳过 Waiting to start 消息 {@0}", message.Id);
                return;
            }

            Log.Information("开始处理Discord账号[{0}] 的 {1} 消息 - messageId: {@2}", instance.Account.Id, messageType, message.Id);

            var wrapper = new MessageWrapper(message);

            HandleMessage(instance, messageType, wrapper);
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