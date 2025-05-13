using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.LoadBalancer;

namespace Midjourney.Infrastructure.Wss
{
    /// <summary>
    /// Discord实例提供者接口，用于解决DiscordInstance和WebSocketStarter之间的循环依赖
    /// </summary>
    public interface IDiscordInstanceProvider
    {
        /// <summary>
        /// 获取Discord账号信息
        /// </summary>
        /// <returns>Discord账号</returns>
        DiscordAccount GetAccount();

        /// <summary>
        /// 获取带前缀的Token
        /// </summary>
        /// <returns>带前缀的Token</returns>
        string GetPrefixedToken();

        /// <summary>
        /// 设置WebSocketStarter
        /// </summary>
        /// <param name="webSocketStarter">WebSocket启动器</param>
        void SetWebSocketStarter(IWebSocketStarter webSocketStarter);
    }
}