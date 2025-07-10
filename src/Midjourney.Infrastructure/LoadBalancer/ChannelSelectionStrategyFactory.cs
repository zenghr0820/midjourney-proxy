namespace Midjourney.Infrastructure.LoadBalancer
{
    /// <summary>
    /// 频道选择策略工厂
    /// </summary>
    public class ChannelSelectionStrategyFactory
    {
        /// <summary>
        /// 根据策略类型创建频道选择策略
        /// </summary>
        /// <param name="type">策略类型</param>
        /// <returns>频道选择策略</returns>
        public static IChannelSelectionStrategy CreateStrategy(ChannelSelectionStrategyType type)
        {
            return type switch
            {
                ChannelSelectionStrategyType.RoundRobin => new RoundRobinChannelSelectionStrategy(),
                ChannelSelectionStrategyType.LeastLoad => new LeastLoadChannelSelectionStrategy(),
                _ => new DefaultChannelSelectionStrategy()
            };
        }
    }
} 