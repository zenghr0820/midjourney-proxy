namespace Midjourney.Infrastructure.LoadBalancer
{
    /// <summary>
    /// 频道选择策略工厂
    /// </summary>
    public class ChannelSelectionStrategyFactory
    {
        /// <summary>
        /// 频道选择策略类型
        /// </summary>
        public enum StrategyType
        {
            /// <summary>
            /// 默认策略（最小队列优先）
            /// </summary>
            Default,
            
            /// <summary>
            /// 轮询策略
            /// </summary>
            RoundRobin,
            
            /// <summary>
            /// 最少负载策略
            /// </summary>
            LeastLoad
        }
        
        /// <summary>
        /// 根据策略类型创建频道选择策略
        /// </summary>
        /// <param name="type">策略类型</param>
        /// <returns>频道选择策略</returns>
        public static IChannelSelectionStrategy CreateStrategy(StrategyType type)
        {
            return type switch
            {
                StrategyType.RoundRobin => new RoundRobinChannelSelectionStrategy(),
                StrategyType.LeastLoad => new LeastLoadChannelSelectionStrategy(),
                _ => new DefaultChannelSelectionStrategy()
            };
        }
    }
} 