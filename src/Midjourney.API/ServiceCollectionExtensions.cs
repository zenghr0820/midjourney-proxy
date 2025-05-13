using Midjourney.Infrastructure.Handle;
using Midjourney.Infrastructure.Wss.Handle;
using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.LoadBalancer;
using Midjourney.Infrastructure.Services;
using Midjourney.Infrastructure.Storage;
using Midjourney.Infrastructure.Wss;

namespace Midjourney.API
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMidjourneyServices(this IServiceCollection services, ProxyProperties config)
        {

            // 注册所有的处理程序

            // 机器人消息处理程序
            services.AddTransient<BotMessageHandler, BotErrorMessageHandler>();
            services.AddTransient<BotMessageHandler, BotImagineSuccessHandler>();
            services.AddTransient<BotMessageHandler, BotRerollSuccessHandler>();
            services.AddTransient<BotMessageHandler, BotStartAndProgressHandler>();
            services.AddTransient<BotMessageHandler, BotUpscaleSuccessHandler>();
            services.AddTransient<BotMessageHandler, BotVariationSuccessHandler>();
            services.AddTransient<BotMessageHandler, BotDescribeSuccessHandler>();
            services.AddTransient<BotMessageHandler, BotActionSuccessHandler>();
            services.AddTransient<BotMessageHandler, BotBlendSuccessHandler>();
            services.AddTransient<BotMessageHandler, BotShowSuccessHandler>();

            // 用户消息处理程序
            services.AddTransient<UserMessageHandler, UserErrorMessageHandler>();
            services.AddTransient<UserMessageHandler, UserImagineSuccessHandler>();
            services.AddTransient<UserMessageHandler, UserActionSuccessHandler>();
            services.AddTransient<UserMessageHandler, UserUpscaleSuccessHandler>();
            services.AddTransient<UserMessageHandler, UserBlendSuccessHandler>();
            services.AddTransient<UserMessageHandler, UserDescribeSuccessHandler>();
            services.AddTransient<UserMessageHandler, UserShowSuccessHandler>();
            services.AddTransient<UserMessageHandler, UserVariationSuccessHandler>();
            services.AddTransient<UserMessageHandler, UserStartAndProgressHandler>();
            services.AddTransient<UserMessageHandler, UserRerollSuccessHandler>();

            // wss消息处理程序
            services.AddTransient<MessageHandler, ErrorMessageHandler>();
            services.AddTransient<MessageHandler, ImagineSuccessHandler>();
            services.AddTransient<MessageHandler, ActionSuccessHandler>();
            services.AddTransient<MessageHandler, UpscaleSuccessHandler>();
            services.AddTransient<MessageHandler, BlendSuccessHandler>();
            services.AddTransient<MessageHandler, DescribeSuccessHandler>();
            services.AddTransient<MessageHandler, ShowSuccessHandler>();
            services.AddTransient<MessageHandler, VariationSuccessHandler>();
            services.AddTransient<MessageHandler, StartAndProgressHandler>();
            services.AddTransient<MessageHandler, RerollSuccessHandler>();
            services.AddTransient<MessageHandler, ShortenSuccessHandler>();


            // 换脸服务
            services.AddSingleton<FaceSwapInstance>();
            services.AddSingleton<VideoFaceSwapInstance>();

            // 通知服务
            services.AddSingleton<INotifyService, NotifyServiceImpl>();

            // 翻译服务
            if (config.TranslateWay == TranslateWay.GPT)
            {
                services.AddSingleton<ITranslateService, GPTTranslateService>();
            }
            else
            {
                services.AddSingleton<ITranslateService, BaiduTranslateService>();
            }

            // 存储服务
            StorageHelper.Configure();

            // 存储服务
            // 内存
            //services.AddSingleton<ITaskStoreService, InMemoryTaskStoreServiceImpl>();
            // LiteDB
            services.AddSingleton<ITaskStoreService>(new TaskRepository());

            // 账号负载均衡服务
            switch (config.AccountChooseRule)
            {
                case AccountChooseRule.BestWaitIdle:
                    services.AddSingleton<IRule, BestWaitIdleRule>();
                    break;
                case AccountChooseRule.Random:
                    services.AddSingleton<IRule, RandomRule>();
                    break;
                case AccountChooseRule.Weight:
                    services.AddSingleton<IRule, WeightRule>();
                    break;
                case AccountChooseRule.Polling:
                    services.AddSingleton<IRule, RoundRobinRule>();
                    break;
                default:
                    services.AddSingleton<IRule, BestWaitIdleRule>();
                    break;
            }

            // Discord 负载均衡器
            services.AddSingleton<DiscordLoadBalancer>();

            // WebSocket配置
            services.AddSingleton<WebSocketConfig>();

            // WebSocketStarterFactory 注册
            services.AddSingleton<IWebSocketStarterFactory, WebSocketStarterFactory>();

            // Discord 账号助手
            services.AddSingleton<DiscordAccountHelper>();

            // Discord 助手
            services.AddSingleton<DiscordHelper>();

            // 任务服务
            services.AddSingleton<ITaskService, TaskService>();
            
            // 系统配置服务
            services.AddSingleton<ISystemSettingkService, SystemSettingService>();
        }
    }
}