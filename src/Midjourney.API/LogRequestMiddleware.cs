using Microsoft.AspNetCore.Mvc.Controllers;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Text;

namespace Midjourney.API
{
    public class LogRequestMiddleware
    {
        private readonly RequestDelegate _next;

        public LogRequestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // 仅处理 Controller 请求
                var endpoint = context.GetEndpoint();
                var controllerDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
                if (controllerDescriptor == null)
                {
                    if (!context.Response.HasStarted)
                    {
                        await _next(context);
                        return;
                    }
                }

                // 检查是否跳过日志
                var skipLog = endpoint.Metadata.Any(m => m is SkipLogRequestAttribute);
                if (skipLog)
                {
                    await _next(context);
                    return;
                }

                // 获取用户信息
                var workContext = context.RequestServices.GetService<WorkContext>();
                var user = workContext?.GetUser();
                var userId = user?.Id ?? "Guest";
                var userName = user?.Name ?? "Guest";

                // 记录基础信息
                var url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}";
                var method = context.Request.Method;
                // var headers = string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}"));
                var queryString = context.Request.QueryString.Value;

                // 读取请求体
                string requestBody = "";
                if (context.Request.ContentLength > 0)
                {
                    context.Request.EnableBuffering();
                    using var reader = new StreamReader(
                        context.Request.Body,
                        Encoding.UTF8,
                        detectEncodingFromByteOrderMarks: false,
                        leaveOpen: true
                    );
                    requestBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                }

                // 敏感信息脱敏
                var sanitizedBody = SanitizeRequestBody(requestBody);
                var truncatedBody = sanitizedBody.Length > 1024
                    ? sanitizedBody.Substring(0, 1024) + "...[TRUNCATED]"
                    : sanitizedBody;

                // 记录日志
                Log.Information(
                    "请求记录 | 方法={Method} | 用户={UserId} | Url={Url} | Query={Query} | Body={Body}",
                    method, userId, url, queryString, truncatedBody ?? "[空]"
                );

            }
            catch (Exception ex)
            {
                Log.Error(ex, "记录请求日志失败");
            }

            
            await _next(context);
            return;
        }

        private string SanitizeRequestBody(string rawBody)
        {
            try
            {
                var json = JObject.Parse(rawBody);
                if (json.ContainsKey("password"))
                    json["password"] = "***REDACTED***";
                return json.ToString();
            }
            catch
            {
                return rawBody;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SkipLogRequestAttribute : Attribute { }
}