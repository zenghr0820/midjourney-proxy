
using RestSharp;
using Serilog;
using Serilog.Debugging;
using System.Net;
using System.Net.Security;

namespace Midjourney.Captcha.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;

            var builder = CreateHostBuilder(args).Build();
            var env = builder.Services.GetService<IWebHostEnvironment>();

            // ���� Serilog
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Services.GetService<IConfiguration>());

            if (env.IsDevelopment())
            {
                logger.MinimumLevel.Debug()
                      .Enrich.FromLogContext();

                // ʹ�� Serilog.Debugging.SelfLog.Enable(Console.Error) ������ Serilog ��������ϣ��⽫��������������⡣
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