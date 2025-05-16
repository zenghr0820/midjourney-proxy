using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.Util;
using Midjourney.Infrastructure.Wss.Gateway;
using System.Collections.Concurrent;

namespace Midjourney.Infrastructure.Wss
{
    /// <summary>
    /// 事件订阅
    /// </summary>
    partial class DiscordSockerStarter
    {
        #region General

        public event Func<DiscordReadyEvent, Task> ReadyEvent
        {
            add => _readyEvent.Add(value);
            remove => _readyEvent.Remove(value);
        }
        private readonly AsyncEvent<Func<DiscordReadyEvent, Task>> _readyEvent = new();
        // public event Func<Task> ResumedEvent
        // {
        //     add => _resumedEvent.Add(value);
        //     remove => _resumedEvent.Remove(value);
        // }
        // private readonly AsyncEvent<Func<Task>> _resumedEvent = new();
        public event Func<ConcurrentDictionary<string, DiscordExtendedGuild>, Task> GuildSubscribeEvent
        {
            add => _guildSubscribeEvent.Add(value);
            remove => _guildSubscribeEvent.Remove(value);
        }
        private readonly AsyncEvent<Func<ConcurrentDictionary<string, DiscordExtendedGuild>, Task>> _guildSubscribeEvent = new();

        #endregion

        /// <summary>
        /// channel 频道事件
        /// </summary>
        #region Channel
        public event Func<ConcurrentDictionary<string, DiscordExtendedGuild>, Task> ChannelSubscribeEvent
        {
            add => _channelSubscribeEvent.Add(value);
            remove => _channelSubscribeEvent.Remove(value);
        }
        private readonly AsyncEvent<Func<ConcurrentDictionary<string, DiscordExtendedGuild>, Task>> _channelSubscribeEvent = new();

        /// <summary>
        /// 私信频道事件
        /// </summary>
        public event Func<ConcurrentDictionary<string, DiscordChannelDto>, Task> DmChannelEvent
        {
            add => _dmChannelEvent.Add(value);
            remove => _dmChannelEvent.Remove(value);
        }
        private readonly AsyncEvent<Func<ConcurrentDictionary<string, DiscordChannelDto>, Task>> _dmChannelEvent = new();

        #endregion
    }
}