using Midjourney.OcelotProxy;
using Midjourney.OcelotProxy.Middleware;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;
using Serilog;

// ���� Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .Build())
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ��� Ocelot �����ļ�
    builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

    // ��� Ocelot ����
    builder.Services.AddOcelot(builder.Configuration)
        // ��� Consul ������
        // ʹ���Զ���� ConsulServiceBuilder
        .AddConsul<MyConsulServiceBuilder>()
        .AddPolly();     // ��� Polly �۶Ͻ���

    var app = builder.Build();

    // ����־�м����������㣬�����޸���Ӧ
    app.UseMiddleware<RequestLoggingMiddleware>();

    // ʹ�� Ocelot �м��
    await app.UseOcelot();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "��������ʧ��");
}
finally
{
    Log.CloseAndFlush();
}