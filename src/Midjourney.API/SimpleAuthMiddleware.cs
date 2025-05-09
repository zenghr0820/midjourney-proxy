using Microsoft.AspNetCore.Authorization;
using Serilog;

namespace Midjourney.API
{
    /// <summary>
    /// 简单的授权中间件
    /// </summary>
    public class SimpleAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public SimpleAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, WorkContext workContext)
        {
            var path = context.Request.Path.Value;
            if (path.StartsWith("/mj-turbo"))
            {
                context.Items["Mode"] = "turbo";
            }
            else if (path.StartsWith("/mj-relax"))
            {
                context.Items["Mode"] = "relax";
            }
            else if (path.StartsWith("/mj-fast"))
            {
                context.Items["Mode"] = "fast";
            }
            else
            {
                context.Items["Mode"] = "";
            }

            // 演示模式下不需要验证
            if (GlobalConfiguration.IsDemoMode == true)
            {
                await _next(context);
                return;
            }

            // 检查是否有 AllowAnonymous 特性
            var endpoint = context.GetEndpoint();
            var allowAnonymous = endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null;

            if (allowAnonymous)
            {
                await _next(context);
                return;
            }


            var user = workContext.GetUser();

            // 如果用户被禁用
            if (user?.Status == EUserStatus.DISABLED)
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("账号已禁用");
                }
                return;
            }

            // 如果账号不可用
            if (user != null && !user.IsAvailable)
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("账号不可用");
                }
                return;
            }

            // 管理员接口权限检查
            if (context.Request.Path.StartsWithSegments("/mj/admin"))
            {
                if (user?.Role != EUserRole.ADMIN)
                {
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("账号无权限");
                    }
                    return;
                }
            }
            else
            {
                // 非管理员接口：未登录且未开启访客模式
                if (user == null && !GlobalConfiguration.Setting.EnableGuest)
                {
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Authorization header missing.");
                    }
                    return;
                }
            }

            await _next(context);
        }
    }
}