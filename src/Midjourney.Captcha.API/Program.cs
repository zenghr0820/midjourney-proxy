
using RestSharp;
using Serilog;
using Serilog.Debugging;
using System.Net;
using System.Net.Security;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;

namespace Midjourney.Captcha.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;

            try
            {
                // ��������������
                var host = CreateHostBuilder(args).Build();

                // ȷ����Ӧ�ó������ʱ�رղ�ˢ����־
                AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();

                // ��¼��ǰĿ¼
                Log.Information($"Current directory: {Directory.GetCurrentDirectory()}");

                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Ӧ�ó�������ʧ��");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                // ���ö�ȡ��ɺ󣬿����������������ǰ��������
                var configuration = config.Build();

                // ���������������־����ʹ��������Ϣ
                ConfigureInitialLogger(configuration, hostingContext.HostingEnvironment.IsDevelopment());
            })
            .ConfigureLogging((hostContext, loggingBuilder) =>
            {
                // ����Ĭ����־�ṩ������ȫ���� Serilog
                loggingBuilder.ClearProviders();
            })
            .UseSerilog()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });

        /// <summary>
        /// ��ȡ���ò����³�ʼ��־��
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="isDevelopment"></param>
        private static void ConfigureInitialLogger(IConfiguration configuration, bool isDevelopment)
        {
            // ������־����
            var loggerConfiguration = new LoggerConfiguration()
                  .ReadFrom.Configuration(configuration)
                  .Enrich.FromLogContext();

            // ���������ض�����
            if (isDevelopment)
            {
                loggerConfiguration.MinimumLevel.Debug();

                // ���������û�����ÿ���̨��־�������
                // ���򣬲�Ҫ�ڴ�������ӣ������ظ�
                bool hasConsoleInConfig = configuration
                    .GetSection("Serilog:WriteTo")
                    .GetChildren()
                    .Any(section => section["Name"]?.Equals("Console", StringComparison.OrdinalIgnoreCase) == true);

                if (!hasConsoleInConfig)
                {
                    loggerConfiguration.WriteTo.Console();
                }

                // ���� Serilog �������
                SelfLog.Enable(Console.Error);
            }
            else
            {
                // ��������ʹ�� appsettings.json �е���С��־��������
                loggerConfiguration.WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information);
            }

            // ���л�������¼���󵽵����ļ�
            loggerConfiguration.WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(evt => evt.Level >= LogEventLevel.Error)
                .WriteTo.File("logs/error.txt", rollingInterval: RollingInterval.Day));

            Log.Logger = loggerConfiguration.CreateLogger();
        }
    }
}