
using Serilog;
using Serilog.Debugging;

namespace Midjourney.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = CreateHostBuilder(args).Build();
            var env = builder.Services.GetService<IWebHostEnvironment>();

            // ���� Serilog
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Services.GetService<IConfiguration>())

                // ������ȷ�� error.txt ����
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

            // ȷ����Ӧ�ó������ʱ�رղ�ˢ����־
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();

            try
            {
                Log.Information($"Current: {Directory.GetCurrentDirectory()}");

                //// ʹ�� Serilog
                //builder.Host.UseSerilog();

                var app = builder;

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Ӧ������ʧ��");
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
    }
}