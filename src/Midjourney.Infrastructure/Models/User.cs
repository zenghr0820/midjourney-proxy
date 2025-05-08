

using FreeSql.DataAnnotations;
using Midjourney.Infrastructure.Data;
using MongoDB.Bson.Serialization.Attributes;

namespace Midjourney.Infrastructure.Models
{
    /// <summary>
    /// 用户
    /// </summary>
    [BsonCollection("user")]
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    [Serializable]
    public class User : DomainObject
    {
        public User()
        {
        }

        /// <summary>
        /// 用户昵称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// 头像
        /// </summary>
        [Column(StringLength = 2000)]
        public string Avatar { get; set; }

        /// <summary>
        /// 角色 ADMIN | USER
        /// </summary>
        public EUserRole? Role { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public EUserStatus? Status { get; set; }

        /// <summary>
        /// 用户令牌
        /// </summary>
        [Column(StringLength = 2000)]
        public string Token { get; set; }

        /// <summary>
        /// 最后登录 ip
        /// </summary>
        public string LastLoginIp { get; set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? LastLoginTime { get; set; }

        /// <summary>
        /// 最后登录时间格式化
        /// </summary>
        public string LastLoginTimeFormat => LastLoginTime?.ToString("yyyy-MM-dd HH:mm");

        /// <summary>
        /// 注册 ip
        /// </summary>
        public string RegisterIp { get; set; }

        /// <summary>
        /// 注册时间
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime RegisterTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 注册时间格式化
        /// </summary>
        public string RegisterTimeFormat => RegisterTime.ToString("yyyy-MM-dd HH:mm");

        /// <summary>
        /// 日绘图最大次数限制，默认 0 不限制
        /// </summary>
        public int DayDrawLimit { get; set; } = -1;

        /// <summary>
        /// 用户最大绘图数量限制，默认 0 不限制
        /// </summary>
        public int TotalDrawLimit { get; set; } = -1;

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
        /// 白名单用户（加入白名单不受限流控制）
        /// </summary>
        public bool IsWhite { get; set; } = false;

        /// <summary>
        /// 账号有效开始时间
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? ValidStartTime { get; set; }

        /// <summary>
        /// 账号有效开始时间
        /// </summary>
        public string ValidStartTimeFormat => ValidStartTime?.ToString("yyyy-MM-dd");

        /// <summary>
        /// 账号有效结束时间
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? ValidEndTime { get; set; }

        /// <summary>
        /// 账号有效结束时间
        /// </summary>
        public string ValidEndTimeFormat => ValidEndTime?.ToString("yyyy-MM-dd");

        /// <summary>
        /// 账号是否可用（在有效期内）
        /// </summary>
        public bool IsAvailable => (ValidStartTime == null || ValidStartTime <= DateTime.Now)
            && (ValidEndTime == null || ValidEndTime >= DateTime.Now);

        /// <summary>
        /// 创建时间
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdateTime { get; set; } = DateTime.Now;
    }
}