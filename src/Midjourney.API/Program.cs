using Serilog;
using Serilog.Debugging;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;

namespace Midjourney.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = CreateHostBuilder(args).Build();
            var env = builder.Services.GetService<IWebHostEnvironment>();

            // 配置 Serilog
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Services.GetService<IConfiguration>())

                // 读取 error.txt
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(evt => evt.Level == Serilog.Events.LogEventLevel.Error)
                    .WriteTo.File("logs/error.txt", rollingInterval: RollingInterval.Day));

            if (env.IsDevelopment())
            {
#if DEBUG
                logger.MinimumLevel.Debug()
                          .Enrich.FromLogContext();
#endif
                SelfLog.Enable(Console.Error);
            }

            Log.Logger = logger.CreateLogger();

            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();

            try
            {
                Log.Information($"Current: {Directory.GetCurrentDirectory()}");

                // 注册 ApplicationStarted 事件
                var appLifetime = builder.Services.GetRequiredService<IHostApplicationLifetime>();
                appLifetime.ApplicationStarted.Register(() => OnApplicationStarted(builder));

                // 启动 Host
                await builder.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application run failure.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog();


        private static void OnApplicationStarted(IHost host)
        {
            using var scope = host.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var server = serviceProvider.GetRequiredService<IServer>();

            if (server is null)
            {
                Log.Warning("检索服务器实例失败.");
                return;
            }

            var addressesFeature = server.Features.Get<IServerAddressesFeature>();
            if (addressesFeature == null || !addressesFeature.Addresses.Any())
            {
                Log.Warning("没有可用的服务器地址.");
                return;
            }

            var urls = addressesFeature.Addresses.ToList();

            // 日志输出监听的地址
            Log.Information("Listening URLs:");
            urls.ForEach(url => Log.Information($" - {url}"));

            var env = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            if (env.IsDevelopment())
            {
                var firstUrl = urls.FirstOrDefault();
                if (firstUrl != null)
                {
                    // 替换IPv6地址[::]为IPv4地址127.0.0.1
                    var urlToOpen = firstUrl.Replace("[::]", "127.0.0.1");

                    if (OperatingSystem.IsWindows())
                    {
                        try
                        {
                            Log.Information($"API服务监听于: {urlToOpen}");
                            Log.Information($"前端应用访问地址: {urlToOpen}");
                            Log.Information("=========================================================");
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = urlToOpen,
                                UseShellExecute = true,
                                Verb = "open"
                            });
                            Log.Information($"尝试使用默认浏览器打开地址: {urlToOpen}");
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, $"无法打开默认浏览器: {urlToOpen}");
                        }
                    }
                    else
                    {
                        Log.Information("仅在Windows上支持自动启动浏览器.");
                    }
                }
            }
        }

    }

}