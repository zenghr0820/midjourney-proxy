using System.Runtime;
using System.Text;
using Consul;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Midjourney.YarpProxy.Middleware;
using Midjourney.YarpProxy.Models;
using Midjourney.YarpProxy.Services;
using Serilog;
using Yarp.ReverseProxy.Configuration;

// ���� Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("���� YARP ����...");

    var builder = WebApplication.CreateBuilder(args);

    // ����ת��ͷ
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                  ForwardedHeaders.XForwardedProto |
                                  ForwardedHeaders.XForwardedHost;

        // �����֪������������� IP��������ӵ���֪�����б�
        // options.KnownProxies.Add(IPAddress.Parse("192.168.1.1"));

        // ����� Docker �� Kubernetes �����У�������Ҫ�����֪����
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // ��� Consul ����
    builder.Services.Configure<ConsulOptions>(builder.Configuration.GetSection(nameof(ConsulOptions)));

    // ��� YARP ����
    builder.Services.AddSingleton<IProxyConfigProvider>(new InMemoryConfigProvider([], []));
    builder.Services.AddReverseProxy().LoadFromMemory([], []);

    // ��� Consul ������
    builder.Services.AddSingleton<IConsulClient>(sp =>
    {
        var consulOptions = sp.GetRequiredService<IOptions<ConsulOptions>>().Value;
        return new ConsulClient(config =>
        {
            config.Address = new Uri(consulOptions.ConsulUrl);
        });
    });

    builder.Services.AddHealthChecks()
        .AddCheck("YARP Gateway Health Check", () => HealthCheckResult.Healthy("YARP ������������"));

    // ��� Consul �����ֺ�̨����
    builder.Services.AddHostedService<ConsulServiceDiscoveryHostedService>();

    var app = builder.Build();

    // ʹ��ת��ͷ�м����Ӧ���������м��֮ǰ��
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.All
    });

    // �����м���ܵ�
    app.UseMiddleware<RequestLoggingMiddleware>();

    // ���ά��ģʽ�м��
    app.UseMiddleware<MaintenanceModeMiddleware>();

    app.UseRouting();

    // �������˵�
    app.MapHealthChecks("/health");

    // ���һ������·��
    app.MapGet("/debug/routes", (IProxyConfigProvider provider) =>
    {
        var config = provider.GetConfig();
        return Results.Ok(new
        {
            Routes = config.Routes.Select(r => new
            {
                r.RouteId,
                Path = r.Match.Path,
                ClusterId = r.ClusterId
            }),
            Clusters = config.Clusters.Select(c => new
            {
                c.ClusterId,
                c.Destinations.Count,
                Destinations = c.Destinations.Select(d => new
                {
                    d.Key,
                    Address = d.Value.Address
                })
            })
        });
    });

    app.MapGet("/debug/headers", (HttpContext context) =>
    {
        var headers = context.Request.Headers
            .ToDictionary(h => h.Key, h => h.Value.ToArray());

        var request = context.Request;

        var ipInfo = new
        {
            RemoteIp = context.Connection.RemoteIpAddress?.ToString(),
            XForwardedFor = request.Headers["X-Forwarded-For"],
            XRealIp = request.Headers["X-Real-IP"],
            XOriginalFor = request.Headers["X-Original-For"],
            CFConnectingIp = request.Headers["CF-Connecting-IP"], // Cloudflare
            XClientIp = request.Headers["X-Client-IP"],
            UserAgent = request.Headers["User-Agent"]
        };

        return Results.Ok(new
        {
            request.Method,
            request.Path,
            IpInfo = ipInfo,
            RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
            LocalIpAddress = context.Connection.LocalIpAddress?.ToString(),
            Headers = headers,
            UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault()
        });
    });

    // YARP �������
    app.MapReverseProxy();

    //// YARP ����������ȴ���
    //app.MapReverseProxy(proxyPipeline => {
    //    // ������������Ӷ����ǰ�û���ô���
    //    proxyPipeline.Use((context, next) => {
    //        // ��¼ת��ǰ����Ϣ
    //        return next();
    //    });
    //});

    // Ĭ��·�ɣ�����δƥ�������
    app.MapFallback(async context =>
    {
        context.Response.StatusCode = 404;
        //context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync($$"""
        {
            "error": "Not Found",
            "message": "�����·��δ�ҵ�ƥ��ķ���",
            "path": "{{context.Request.Path}}",
            "timestamp": "{{DateTime.UtcNow:O}}"
        }
        """, Encoding.UTF8);
    });

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "YARP ��������ʧ��");
}
finally
{
    Log.CloseAndFlush();
}