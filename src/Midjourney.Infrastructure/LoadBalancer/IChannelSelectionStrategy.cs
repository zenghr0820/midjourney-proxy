using System.Collections.Concurrent;

namespace Midjourney.Infrastructure.LoadBalancer
{
    /// <summary>
    /// 频道选择策略接口
    /// </summary>
    public interface IChannelSelectionStrategy
    {
        /// <summary>
        /// 从频道池中选择一个频道
        /// </summary>
        /// <param name="channelPool">频道池</param>
        /// <param name="preferredChannelId">优先使用的频道ID，如果为空则自动选择</param>
        /// <returns>选择的频道ID</returns>
        DiscordChannel SelectChannel(
            ConcurrentDictionary<string, DiscordChannel> channelPool, 
            string preferredChannelId = null);
    }
} 