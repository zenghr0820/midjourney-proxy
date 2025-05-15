using Microsoft.Extensions.Caching.Memory;
using Midjourney.Infrastructure.LoadBalancer;
using Midjourney.Infrastructure.Wss.Handle;
using Serilog;
using System.Net;

namespace Midjourney.Infrastructure.Wss
{
    /// <summary>
    /// WebSocket启动器工厂实现
    /// </summary>
    public class WebSocketStarterFactory : IWebSocketStarterFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly WebSocketConfig _config;

        private readonly DiscordHelper _discordHelper;
        private readonly IMemoryCache _memoryCache;

        private readonly ProxyProperties _properties = GlobalConfiguration.Setting;

        private readonly WebProxy _webProxy;
        private IEnumerable<MessageHandler> _messageHandlers;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="discordHelper"></param>
        /// <param name="messageHandlers"></param>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="memoryCache"></param>
        /// <param name="config">WebSocket配置</param>
        public WebSocketStarterFactory(
            DiscordHelper discordHelper,
            IEnumerable<MessageHandler> messageHandlers,
            IServiceProvider serviceProvider,
            IMemoryCache memoryCache,
            WebSocketConfig config)
        {
            _serviceProvider = serviceProvider;
            _config = config;
            _logger = Log.Logger;
            _discordHelper = discordHelper;
            _memoryCache = memoryCache;
            _messageHandlers = messageHandlers;

            // 创建WebProxy
            if (!string.IsNullOrEmpty(_properties.Proxy?.Host))
            {
                _webProxy = new WebProxy(_properties.Proxy.Host, _properties.Proxy.Port ?? 80);
            }
        }

        /// <summary>
        /// 根据Discord账号创建WebSocket启动器
        /// </summary>
        /// <param name="instance">Discord实例</param>
        /// <returns>WebSocket启动器</returns>
        /// <exception cref="ArgumentNullException">当Discord实例为空时抛出</exception>
        public IWebSocketStarter CreateDiscordSocketWithInstance(DiscordInstance instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance), "Discord 实例不能为空");
            }

            // 创建WebSocket启动器
            var socketStarter = new DiscordSockerStarter(_discordHelper, _webProxy, instance, _messageHandlers, _memoryCache, _config);
            
            // 设置事件订阅
            socketStarter.ChannelSubscribeEvent += instance.OnChannelSubscribe;
            
            // 设置WebSocketStarter到Discord实例
            instance.WebSocketStarter = socketStarter;
            
            return socketStarter;
        }
    }
}