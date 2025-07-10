using Discord;

namespace Midjourney.Base.Dto
{
    public class CurrentUserDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 用户名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 头像
        /// </summary>
        public string Avatar { get; set; }
        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// 角色
        /// </summary>
        public string Role { get; set; }
        /// <summary>
        /// 角色，适配旧系统
        /// </summary>
        public string Access => Role;
        /// <summary>
        /// 账号状态
        /// </summary>
        public bool Active { get; set; }
        /// <summary>
        /// 令牌
        /// </summary>
        public string ApiSecret { get; set; }
        /// <summary>
        /// 令牌
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// 版本
        /// </summary>
        public string Version { get; set; }
         /// <summary>
        /// 每日限额绘图次数
        /// </summary>
        public int DayDrawLimit { get; set; } = -1;
        /// <summary>
        /// 今日绘图次数
        /// </summary>
        public int DayDrawCount { get; set; } = 0;
        /// <summary>
        /// 总绘图次数
        /// </summary>
        public int TotalDrawCount { get; set; } = 0;
        /// <summary>
        /// 用户并发绘图数量限制，默认 -1 不限制
        /// </summary>
        public int CoreSize { get; set; } = -1;
        /// <summary>
        /// 用户队列绘图数量限制，默认 -1 不限制
        /// </summary>
        public int QueueSize { get; set; } = -1;
        /// <summary>
        /// 最后登录时间
        /// </summary>
        public DateTime? LastLoginTime { get; set; }
        /// <summary>
        /// 最后登录时间格式化
        /// </summary>
        public string LastLoginTimeFormat => LastLoginTime?.ToString("yyyy-MM-dd HH:mm");
         /// <summary>
        /// 账号有效开始时间
        /// </summary>
        public DateTime? ValidStartTime { get; set; }
        /// <summary>
        /// 账号有效开始时间格式化
        /// </summary>
        public string ValidStartTimeFormat => ValidStartTime?.ToString("yyyy-MM-dd HH:mm:ss");
        /// <summary>
        /// 账号有效结束时间
        /// </summary>
        public DateTime? ValidEndTime { get; set; }

        /// <summary>
        /// 账号有效结束时间格式化
        /// </summary>
        public string ValidEndTimeFormat => ValidEndTime?.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
