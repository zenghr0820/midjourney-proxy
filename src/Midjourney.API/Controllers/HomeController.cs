

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Midjourney.API.Controllers
{
    /// <summary>
    /// 用于获取首页等信息的控制器
    /// </summary>
    [ApiController]
    [Route("mj/home")]
    public class HomeController : ControllerBase
    {
        private readonly IMemoryCache _memoryCache;

        public HomeController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// 首页
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        public Result<HomeDto> Info()
        {
            var now = DateTime.Now.ToString("yyyyMMdd");
            var homeKey = $"{now}_home";

            var data = _memoryCache.GetOrCreate(homeKey, c =>
            {
                c.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

                var dto = new HomeDto
                {
                    IsRegister = GlobalConfiguration.Setting.EnableRegister == true
                    && !string.IsNullOrWhiteSpace(GlobalConfiguration.Setting?.Smtp?.FromPassword),
                    IsGuest = GlobalConfiguration.Setting.EnableGuest == true,
                    IsDemoMode = GlobalConfiguration.IsDemoMode == true,
                    Version = GlobalConfiguration.Version,
                    Notify = GlobalConfiguration.Setting.Notify
                };

                var now = new DateTimeOffset(DateTime.Now.Date).ToUnixTimeMilliseconds();
                var yesterday = new DateTimeOffset(DateTime.Now.Date.AddDays(-1)).ToUnixTimeMilliseconds();

                dto.TodayDraw = (int)DbHelper.Instance.TaskStore.Count(x => x.SubmitTime >= now);
                dto.YesterdayDraw = (int)DbHelper.Instance.TaskStore.Count(x => x.SubmitTime >= yesterday && x.SubmitTime < now);
                dto.TotalDraw = (int)DbHelper.Instance.TaskStore.Count(x => true);

                // 今日绘图客户端 top 10
                var setting = GlobalConfiguration.Setting;


                var top = GlobalConfiguration.Setting.HomeTopCount;
                if (top <= 0)
                {
                    top = 10; // 默认取前10
                }
                if (top > 100)
                {
                    top = 100; // 最多取前100
                }

                var todayList = DbHelper.Instance.TaskStore.Where(x => x.SubmitTime >= now).ToList();
                var tops = todayList
                .GroupBy(c =>
                {
                    if (setting.HomeDisplayRealIP)
                    {
                        return c.ClientIp ?? "null";
                    }

                    // 如果不显示真实IP，则只显示前两段IP地址
                    // 只显示前两段IP地址
                    return string.Join(".", c.ClientIp?.Split('.')?.Take(2) ?? []) + ".x.x";
                })
                .Select(c =>
                {
                    // 如果显示 ip 对应的身份
                    if (setting.HomeDisplayUserIPState)
                    {
                        var item = todayList.FirstOrDefault(u => u.ClientIp == c.Key && !string.IsNullOrWhiteSpace(u.State));

                        return new
                        {
                            ip = (c.Key ?? "null") + " - " + item?.State,
                            count = c.Count(),
                        };
                    }

                    return new
                    {
                        ip = c.Key ?? "null",
                        count = c.Count()
                    };
                })
                .OrderByDescending(c => c.count)
                .Take(top)
                .ToDictionary(c => c.ip, c => c.count);

                dto.Tops = tops;

                return dto;
            });
            data.SystemInfo = SystemInfo.GetCurrentSystemInfo();

            return Result.Ok(data);
        }
    }
}