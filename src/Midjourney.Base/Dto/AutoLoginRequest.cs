

using System.ComponentModel.DataAnnotations;

namespace Midjourney.Base.Dto
{
    /// <summary>
    /// 自动登录请求
    /// </summary>
    public class AutoLoginRequest
    {
        /// <summary>
        /// 账号（用于自动登录）
        /// </summary>
        [Required]
        public string LoginAccount { get; set; }

        /// <summary>
        /// 密码（用于自动登录）
        /// </summary>
        [Required]
        public string LoginPassword { get; set; }

        /// <summary>
        /// 2FA 密钥（用于自动登录）
        /// </summary>
        [Required]
        public string Login2fa { get; set; }

        /// <summary>
        /// 登陆前的启用状态
        /// </summary>
        public bool LoginBeforeEnabled { get; set; }

        /// <summary>
        /// 自定义参数 = ChannelId
        /// </summary>
        [MaxLength(4000)]
        public string State { get; set; }

        /// <summary>
        /// 登录成功后的 Token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// 通知回调的密钥，防止篡改
        /// </summary>
        [MaxLength(4000)]
        public string Secret { get; set; }

        /// <summary>
        /// 回调地址, 为空时使用全局notifyHook。
        /// </summary>
        [MaxLength(4000)]
        public string NotifyHook { get; set; }

        /// <summary>
        /// 是否验证成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        [MaxLength(4000)]
        public string Message { get; set; }
    }
}
