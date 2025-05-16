using System.Collections.Concurrent;

namespace Midjourney.Infrastructure.LoadBalancer
{
    /// <summary>
    /// 默认频道选择策略：选择最空闲的频道
    /// </summary>
    public class DefaultChannelSelectionStrategy : IChannelSelectionStrategy
    {
        /// <summary>
        /// 从频道池中选择一个频道
        /// </summary>
        /// <param name="channelPool">频道池</param>
        /// <param name="preferredChannelId">优先使用的频道ID，如果为空则自动选择</param>
        /// <returns>选择的频道ID</returns>
        public DiscordChannel SelectChannel(
            ConcurrentDictionary<string, DiscordChannel> channelPool, 
            string preferredChannelId = null)
        {
            // 1. 检查优先频道是否可用
            if (!string.IsNullOrWhiteSpace(preferredChannelId) 
                && channelPool.TryGetValue(preferredChannelId, out var preferredChannel) 
                && preferredChannel is { Enable: true, IsIdleQueue: true })
            {
                return preferredChannel;
            }

            // 2. 选择最空闲的频道
            var selectedChannel = channelPool.Values
                .Where(c => c.Enable && c.IsIdleQueue)
                .OrderBy(c => c.QueueTasks.Count)
                .ThenBy(c => c.RunningTasks.Count)
                .FirstOrDefault();

            // 3. 返回选择的频道ID或备选项
            return selectedChannel;
        }

        /// <summary>
        /// 默认选择策略 - 选择任务队列最空闲的频道
        /// </summary>
        private DiscordChannel SelectByDefault(ConcurrentDictionary<string, DiscordChannel> channelPool)
        {
            // 只选择启用且队列有空间的频道
            var availableChannels = channelPool.Values
                .Where(c => c.Enable && c.IsIdleQueue)
                .ToList();
                
            if (!availableChannels.Any())
                return null;
                
            // 优先选择没有任何运行中任务的频道
            var idleChannel = availableChannels
                .Where(c => c.RunningTasks.IsEmpty)
                .OrderBy(c => c.QueueTasks.Count)
                .FirstOrDefault();
                
            if (idleChannel != null)
                return idleChannel;
                
            // 如果所有频道都有运行中的任务，选择队列最短的频道
            return availableChannels
                .OrderBy(c => c.QueueTasks.Count)
                .ThenBy(c => c.RunningTasks.Count)
                .FirstOrDefault();
        }

        /// <summary>
        /// 最小负载选择策略 - 选择当前负载最低的频道
        /// </summary>
        private DiscordChannel SelectByLeastLoaded(ConcurrentDictionary<string, DiscordChannel> channelPool)
        {
            // 只选择启用且队列有空间的频道
            var availableChannels = channelPool.Values
                .Where(c => c.Enable && c.IsIdleQueue)
                .ToList();
                
            if (!availableChannels.Any())
                return null;
                
            // 计算每个频道的负载指数 = 运行中任务数 * 5 + 队列中任务数
            return availableChannels
                .OrderBy(c => c.RunningTasks.Count * 5 + c.QueueTasks.Count)
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// 轮询选择策略：按顺序循环选择频道
    /// </summary>
    public class RoundRobinChannelSelectionStrategy : IChannelSelectionStrategy
    {
        private int _currentIndex = 0;
        private readonly object _lockObj = new object();

        /// <summary>
        /// 从频道池中选择一个频道
        /// </summary>
        /// <param name="channelPool">频道池</param>
        /// <param name="preferredChannelId">优先使用的频道ID，如果为空则自动选择</param>
        /// <returns>选择的频道ID</returns>
        public DiscordChannel SelectChannel(
            ConcurrentDictionary<string, DiscordChannel> channelPool, 
            string preferredChannelId = null)
        {
            // 1. 检查优先频道是否可用
            if (!string.IsNullOrWhiteSpace(preferredChannelId) 
                && channelPool.TryGetValue(preferredChannelId, out var preferredChannel) 
                && preferredChannel is { Enable: true, IsIdleQueue: true })
            {
                return preferredChannel;
            }

            // 获取可用频道列表
            var availableChannels = channelPool.Values
                .Where(c => c.Enable && c.IsIdleQueue)
                .OrderBy(c => c.ChannelId)
                .ToList();

            if (availableChannels.Count == 0)
            {
                return null;
            }

            // 使用轮询方式选择频道
            lock (_lockObj)
            {
                _currentIndex = (_currentIndex + 1) % availableChannels.Count;
                return availableChannels[_currentIndex];
            }
        }
    }

    /// <summary>
    /// 最少负载选择策略：选择负载最小的频道
    /// </summary>
    public class LeastLoadChannelSelectionStrategy : IChannelSelectionStrategy
    {
        /// <summary>
        /// 从频道池中选择一个频道
        /// </summary>
        /// <param name="channelPool">频道池</param>
        /// <param name="preferredChannelId">优先使用的频道ID，如果为空则自动选择</param>
        /// <returns>选择的频道ID</returns>
        public DiscordChannel SelectChannel(
            ConcurrentDictionary<string, DiscordChannel> channelPool, 
            string preferredChannelId = null)
        {
            // 1. 检查优先频道是否可用
            if (!string.IsNullOrWhiteSpace(preferredChannelId) 
                && channelPool.TryGetValue(preferredChannelId, out var preferredChannel) 
                && preferredChannel is { Enable: true, IsIdleQueue: true })
            {
                return preferredChannel;
            }

            // 2. 选择负载最小的频道 (根据运行中的任务数量和队列中的任务数量计算负载)
            var selectedChannel = channelPool.Values
                .Where(c => c.Enable && c.IsIdleQueue)
                .OrderBy(c => c.RunningTasks.Count + (c.QueueTasks.Count * 0.5)) // 运行中的任务权重更高
                .FirstOrDefault();

            // 3. 返回选择的频道ID或备选项
            return selectedChannel;
        }
    }
} 