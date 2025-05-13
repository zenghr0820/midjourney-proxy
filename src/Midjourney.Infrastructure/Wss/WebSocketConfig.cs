using System;

namespace Midjourney.Infrastructure.Wss
{
    /// <summary>
    /// WebSocket配置类
    /// </summary>
    public class WebSocketConfig
    {
        /// <summary>
        /// 连接重试限制
        /// </summary>
        public int ConnectRetryLimit { get; set; } = 5;

        /// <summary>
        /// 心跳因子(0.9-1.0之间)
        /// </summary>
        public double HeartbeatFactor { get; set; } = 0.9;

        /// <summary>
        /// 重连延迟(毫秒)
        /// </summary>
        public int ReconnectDelay { get; set; } = 1000;

        /// <summary>
        /// 默认心跳间隔(毫秒)
        /// </summary>
        public int DefaultHeartbeatInterval { get; set; } = 41250;

        /// <summary>
        /// 消息处理延迟(毫秒)
        /// </summary>
        public int MessageProcessDelay { get; set; } = 10;
    }
} 