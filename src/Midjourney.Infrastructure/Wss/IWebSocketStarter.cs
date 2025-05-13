
namespace Midjourney.Infrastructure.Wss
{
    /// <summary>
    /// WebSocket启动器接口
    /// </summary>
    public interface IWebSocketStarter
    {
        /// <summary>
        /// 异步启动WebSocket连接
        /// </summary>
        /// <param name="reconnect">是否重新连接</param>
        /// <returns>连接是否成功</returns>
        Task<bool> StartAsync(bool reconnect = false);

        /// <summary>
        /// 获取连接是否运行中
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 尝试重新连接
        /// </summary>
        void TryReconnect();

        /// <summary>
        /// 尝试新的连接
        /// </summary>
        void TryNewConnect();

        /// <summary>
        /// 关闭Socket连接
        /// </summary>
        /// <param name="reconnect">是否为重连关闭</param>
        void CloseSocket(bool reconnect = false);
    }
} 