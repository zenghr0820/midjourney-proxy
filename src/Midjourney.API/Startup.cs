﻿

global using Midjourney.Infrastructure;
global using Midjourney.Infrastructure.Models;

using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.Options;
using Midjourney.Infrastructure.Services;
using Serilog;

namespace Midjourney.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // 启动时，优先初始化全局配置项
            var configSec = Configuration.GetSection("mj");
            var configOpt = configSec.Get<ProxyProperties>();
            services.Configure<ProxyProperties>(configSec);

            var ipSec = Configuration.GetSection("IpRateLimiting");
            var ipRateOpt = ipSec.Get<IpRateLimitingOptions>();
            services.Configure<IpRateLimitingOptions>(ipSec);

            var ipBlackSec = Configuration.GetSection("IpBlackRateLimiting");
            var ipBlackOpt = ipBlackSec.Get<IpBlackRateLimitingOptions>();
            services.Configure<IpBlackRateLimitingOptions>(ipBlackSec);

            var setting = LiteDBHelper.SettingStore.Get(Constants.DEFAULT_SETTING_ID);
            if (setting == null)
            {
                setting = new Setting
                {
                    Id = Constants.DEFAULT_SETTING_ID,
                    IpRateLimiting = ipRateOpt,
                    IpBlackRateLimiting = ipBlackOpt,

                    EnableRegister = true,
                    EnableGuest = true,

                    RegisterUserDefaultDayLimit = -1,
                    RegisterUserDefaultCoreSize = -1,
                    RegisterUserDefaultQueueSize = -1,
                    RegisterUserDefaultTotalLimit = -1,

                    GuestDefaultDayLimit = -1,
                    GuestDefaultCoreSize = -1,
                    GuestDefaultQueueSize = -1,

                    AccountChooseRule = configOpt.AccountChooseRule,
                    BaiduTranslate = configOpt.BaiduTranslate,
                    CaptchaNotifyHook = configOpt.CaptchaNotifyHook,
                    CaptchaNotifySecret = configOpt.CaptchaNotifySecret,
                    CaptchaServer = configOpt.CaptchaServer,
                    NgDiscord = configOpt.NgDiscord,
                    NotifyHook = configOpt.NotifyHook,
                    NotifyPoolSize = configOpt.NotifyPoolSize,
                    Openai = configOpt.Openai,
                    Proxy = configOpt.Proxy,
                    TranslateWay = configOpt.TranslateWay,
                    Smtp = configOpt.Smtp
                };
                LiteDBHelper.SettingStore.Save(setting);
            }

            GlobalConfiguration.Setting = setting;

            // 原始 Mongo 配置，旧版数据库配置
            if (setting.DatabaseType == DatabaseType.NONE)
            {
                if (MongoHelper.OldVerify())
                {
                    // 将原始 Mongo 配置转换为新配置
                    setting.DatabaseConnectionString = setting.MongoDefaultConnectionString;
                    setting.DatabaseName = setting.MongoDefaultDatabase;
                    setting.DatabaseType = DatabaseType.MongoDB;
                }
            }

            // 如果未配置则为 None
            if (setting.DatabaseType == DatabaseType.NONE)
            {
                setting.DatabaseType = DatabaseType.LiteDB;
            }

            // 验证数据库是否可连接
            if (!DbHelper.Verify())
            {
                // 切换为本地数据库
                setting.DatabaseType = DatabaseType.LiteDB;

                // 日志
                Log.Error("数据库连接失败，自动切换为 LiteDB 数据库");
            }

            // 更新数据库
            LiteDBHelper.SettingStore.Save(setting);

            GlobalConfiguration.Setting = setting;

            // 缓存
            services.AddMemoryCache();

            // 是否为演示模式
            var isDemoMode = Configuration.GetSection("Demo").Get<bool?>();
            if (isDemoMode != true)
            {
                if (bool.TryParse(Environment.GetEnvironmentVariable("DEMO"), out var demo) && demo)
                {
                    isDemoMode = demo;
                }
            }
            GlobalConfiguration.IsDemoMode = isDemoMode;

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<WorkContext>();

            // API 异常过滤器
            // API 方法/模型过滤器
            services.AddControllers(options =>
            {
                options.Filters.Add<CustomLogicExceptionFilterAttribute>();
                options.Filters.Add<CustomActionFilterAttribute>();
            }).AddJsonOptions(options =>
            {
                // 配置枚举序列化为字符串
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            // 添加授权服务
            services.AddAuthorization();

            // 自定义配置 API 行为选项
            // 配置 api 视图模型验证 400 错误处理，需要在 AddControllers 之后配置
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = (context) =>
                {
                    var error = context.ModelState.Values.FirstOrDefault()?.Errors?.FirstOrDefault()?.ErrorMessage ?? "参数异常";
                    Log.Logger.Warning("参数异常 {@0} - {@1}", context.HttpContext?.Request?.GetUrl() ?? "", error);
                    return new JsonResult(Result.Fail(error));
                };
            });

            // 注册 HttpClient
            services.AddHttpClient();

            // 注册 Midjourney 服务
            services.AddMidjourneyServices(setting);

            // 注册 Discord 账号初始化器
            services.AddSingleton<DiscordAccountInitializer>();
            services.AddHostedService(provider => provider.GetRequiredService<DiscordAccountInitializer>());

            // 注册任务清理服务
            services.AddHostedService<TaskCleanupService>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Midjourney API", Version = "v1" });

                c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Description = "在下框中输入请求头中需要添加的授权 Authorization: {Token}",
                    Name = "Authorization", // 或者 "Mj-Api-Secret" 视具体需求而定
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "ApiKeyScheme"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            }
                        },
                        new string[] { }
                    }
                });

                var xmls = new string[] { "Midjourney.Infrastructure.xml" };
                foreach (var xmlModel in xmls)
                {
                    var baseDirectory = AppContext.BaseDirectory;
                    if (!File.Exists(Path.Combine(baseDirectory, xmlModel)))
                    {
                        baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
                    }

                    var xmlSubPath = Path.Combine(baseDirectory, xmlModel);
                    if (File.Exists(xmlSubPath))
                    {
                        c.IncludeXmlComments(xmlSubPath, true);
                    }
                }

                // 当前程序集名称
                var assemblyMame = Assembly.GetExecutingAssembly().GetName().Name;
                var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyMame}.xml");
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath, true);
                }
            });
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (env.IsDevelopment() || GlobalConfiguration.IsDemoMode == true || GlobalConfiguration.Setting?.EnableSwagger == true)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.EnablePersistAuthorization();
                    c.DisplayRequestDuration();
                });
            }

            app.UseDefaultFiles(); // 启用默认文件（index.html）
            // app.UseStaticFiles(); // 配置提供静态文件
            // 读取配置（开发环境优先读取 appsettings.Development.json）
            string staticFilesPath = Configuration.GetValue<string>("StaticFiles:Path") ?? "wwwroot";
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, staticFilesPath))
            });

            app.UseCors(builder =>
            {
                builder.AllowAnyMethod().AllowAnyHeader().SetIsOriginAllowed(origin => true).AllowCredentials();
            });

            app.UseRouting();

            app.UseAuthorization();

            // ⭐ 注册日志中间件（在认证之后，其他中间件之前）
            app.UseMiddleware<LogRequestMiddleware>();

            // 简单的授权中间件
            app.UseMiddleware<SimpleAuthMiddleware>();

            // 限流
            app.UseMiddleware<RateLimitingMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}