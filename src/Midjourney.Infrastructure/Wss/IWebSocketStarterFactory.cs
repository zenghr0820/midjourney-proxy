using Midjourney.Infrastructure.Services;

namespace Midjourney.Infrastructure.Wss
{
    /// <summary>
    /// WebSocket启动器工厂接口
    /// </summary>
    public interface IWebSocketStarterFactory
    {
        /// <summary>
        /// 根据Discord账号创建WebSocket启动器
        /// </summary>
        /// <param name="instance">Discord实例</param>
        /// <returns>WebSocket启动器</returns>
        IWebSocketStarter CreateDiscordSocketWithInstance(DiscordInstance instance);

    }
} 