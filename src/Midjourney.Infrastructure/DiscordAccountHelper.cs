using Microsoft.Extensions.Caching.Memory;
using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.Handle;
using Midjourney.Infrastructure.LoadBalancer;
using Midjourney.Infrastructure.Services;
using Midjourney.Infrastructure.Wss;
using RestSharp;
using Serilog;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace Midjourney.Infrastructure
{
    /// <summary>
    /// Discord账号辅助类，用于创建和管理Discord实例。
    /// </summary>
    public class DiscordAccountHelper
    {
        private readonly DiscordHelper _discordHelper;
        private readonly ProxyProperties _properties;

        private readonly ITaskStoreService _taskStoreService;
        private readonly INotifyService _notifyService;

        private readonly Dictionary<string, string> _paramsMap;
        private readonly IMemoryCache _memoryCache;
        private readonly ITaskService _taskService;
        private readonly IWebSocketStarterFactory _webSocketStarterFactory;
        public DiscordAccountHelper(
            DiscordHelper discordHelper,
            ITaskStoreService taskStoreService,
            IEnumerable<BotMessageHandler> messageHandlers,
            INotifyService notifyService,
            IEnumerable<UserMessageHandler> userMessageHandlers,
            IMemoryCache memoryCache,
            IWebSocketStarterFactory webSocketStarterFactory,
            ITaskService taskService)
        {
            _properties = GlobalConfiguration.Setting;
            _discordHelper = discordHelper;
            _taskStoreService = taskStoreService;
            _notifyService = notifyService;
            _memoryCache = memoryCache;
            _webSocketStarterFactory = webSocketStarterFactory;
            var paramsMap = new Dictionary<string, string>();
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName().Name;
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.EndsWith(".json") && name.Contains("Resources.ApiParams"))
                .ToList();

            foreach (var resourceName in resourceNames)
            {
                var fileName = Path.GetFileNameWithoutExtension(resourceName);
                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(stream);
                var paramsContent = reader.ReadToEnd();

                var fileKey = fileName.TrimPrefix(assemblyName + ".Resources.ApiParams.").TrimSuffix(".json");

                paramsMap[fileKey] = paramsContent;
            }

            _paramsMap = paramsMap;
            _taskService = taskService;
        }

        /// <summary>
        /// 创建Discord实例。
        /// </summary>
        /// <param name="account">Discord账号信息。</param>
        /// <returns>Discord实例。</returns>
        /// <exception cref="ArgumentException">当guildId, channelId或userToken为空时抛出。</exception>
        public async Task<DiscordInstance> CreateDiscordInstance(DiscordAccount account)
        {
            if (string.IsNullOrWhiteSpace(account.GuildId) || string.IsNullOrWhiteSpace(account.ChannelId) || string.IsNullOrWhiteSpace(account.UserToken))
            {
                throw new ArgumentException("guildId, channelId, userToken must not be blank");
            }

            if (string.IsNullOrWhiteSpace(account.UserAgent))
            {
                account.UserAgent = Constants.DEFAULT_DISCORD_USER_AGENT;
            }

            // 创建WebProxy
            WebProxy webProxy = null;
            if (!string.IsNullOrEmpty(_properties.Proxy?.Host))
            {
                webProxy = new WebProxy(_properties.Proxy.Host, _properties.Proxy.Port ?? 80);
            }

            var discordInstance = new DiscordInstance(
                _memoryCache,
                account,
                _taskStoreService,
                _notifyService,
                _discordHelper,
                _paramsMap,
                webProxy,
                _taskService);

            if (account.Enable == true)
            {
                // 使用工厂创建WebSocketStarter，工厂内部会设置WebSocketStarter到DiscordInstance
                var webSocketStarter = _webSocketStarterFactory.CreateDiscordSocketWithInstance(discordInstance);
                await webSocketStarter.StartAsync();
                
                // 不需要再次设置WebSocketStarter，因为工厂已经设置了
                // discordInstance.WebSocketStarter = webSocketStarter;
            }
            else
            {
                Log.Information("Discord账号未启用, 跳过连接 - 账号: {0}", account.GetDisplay());
            }

            return discordInstance;
        }

        /// <summary>
        /// 验证账号是否可用
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> ValidateAccount(DiscordAccount account)
        {
            if (string.IsNullOrWhiteSpace(account.UserAgent))
            {
                account.UserAgent = Constants.DEFAULT_DISCORD_USER_AGENT;
            }

            WebProxy webProxy = null;
            if (!string.IsNullOrEmpty(_properties.Proxy?.Host))
            {
                webProxy = new WebProxy(_properties.Proxy.Host, _properties.Proxy.Port ?? 80);
            }

            var hch = new HttpClientHandler
            {
                UseProxy = webProxy != null,
                Proxy = webProxy
            };

            var client = new HttpClient(hch)
            {
                Timeout = TimeSpan.FromMinutes(10),
            };

            var request = new HttpRequestMessage(HttpMethod.Get, DiscordHelper.DISCORD_VAL_URL);
            request.Headers.Add("Authorization", account.UserToken);
            request.Headers.Add("User-Agent", account.UserAgent);

            var response = await client.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            //{
            //    "message": "执行此操作需要先验证您的账号。",
            //    "code": 40002
            //}

            var data = JsonDocument.Parse(json).RootElement;
            if (data.TryGetProperty("message", out var message))
            {
                throw new Exception(message.GetString() ?? "账号验证异常");
            }

            return false;
        }

        /// <summary>
        /// 获取私信 ID
        /// </summary>
        /// <param name="account"></param>
        /// <param name="botType"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> GetBotPrivateId(DiscordAccount account, EBotType botType)
        {
            if (string.IsNullOrWhiteSpace(account.UserAgent))
            {
                account.UserAgent = Constants.DEFAULT_DISCORD_USER_AGENT;
            }

            WebProxy webProxy = null;
            if (!string.IsNullOrEmpty(_properties.Proxy?.Host))
            {
                webProxy = new WebProxy(_properties.Proxy.Host, _properties.Proxy.Port ?? 80);
            }

            var hch = new HttpClientHandler
            {
                UseProxy = webProxy != null,
                Proxy = webProxy
            };

            var client = new HttpClient(hch)
            {
                Timeout = TimeSpan.FromMinutes(10),
            };

            var request = new HttpRequestMessage(HttpMethod.Post, DiscordHelper.ME_CHANNELS_URL);
            request.Headers.Add("Authorization", account.UserToken);
            request.Headers.Add("User-Agent", account.UserAgent);

            var obj = new
            {
                recipients = new string[] { botType == EBotType.MID_JOURNEY ? Constants.MJ_APPLICATION_ID : Constants.NIJI_APPLICATION_ID }
            };
            var objStr = JsonSerializer.Serialize(obj);
            var content = new StringContent(objStr, null, "application/json");
            request.Content = content;

            var response = await client.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var data = JsonDocument.Parse(json).RootElement;
                if (data.TryGetProperty("id", out var id))
                {
                    return id.GetString();
                }
            }

            throw new Exception($"获取私信 ID 失败 {response?.StatusCode}, {response?.Content}");
        }

        /// <summary>
        /// 自动登录
        /// </summary>
        /// <param name="model"></param>
        /// <param name="beforeEnable">登陆前账号是否启用</param>
        /// <returns></returns>
        public static bool AutoLogin(DiscordAccount model, bool beforeEnable = false)
        {
            if (string.IsNullOrWhiteSpace(model.LoginAccount)
                || string.IsNullOrWhiteSpace(model.LoginPassword)
                || string.IsNullOrWhiteSpace(model.Login2fa))
            {
                return false;
            }

            var setting = GlobalConfiguration.Setting;
            var notifyUrl = $"{setting.CaptchaServer.Trim().TrimEnd('/')}/login/auto";
            var client = new RestClient();
            var request = new RestRequest(notifyUrl, Method.Post);
            request.AddHeader("Content-Type", "application/json");
            var body = new AutoLoginRequest
            {
                Login2fa = model.Login2fa,
                LoginAccount = model.LoginAccount,
                LoginPassword = model.LoginPassword,
                LoginBeforeEnabled = beforeEnable,
                State = model.ChannelId,
                NotifyHook = setting.CaptchaNotifyHook,
                Secret = setting.CaptchaNotifySecret,
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(body);
            request.AddJsonBody(json);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                model.IsAutoLogining = true;
                model.LoginStart = DateTime.Now;

                DbHelper.Instance.AccountStore.Update("LoginStart,IsAutoLogining", model);

                return true;
            }

            Log.Error($"自动登录失败 failed: {response.Content}");

            return false;
        }
    }
}