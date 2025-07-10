﻿
using Swashbuckle.AspNetCore.Annotations;

namespace Midjourney.Base.Dto
{
    /// <summary>
    /// Imagine提交参数。
    /// </summary>
    [SwaggerSchema("Imagine提交参数")]
    public class SubmitImagineDTO : BaseSubmitDTO
    {
        /// <summary>
        /// bot 类型，mj(默认)或niji
        /// MID_JOURNEY | 枚举值: NIJI_JOURNEY
        /// </summary>
        public string BotType { get; set; }

        /// <summary>
        /// 提示词。
        /// </summary>
        [SwaggerSchema("提示词", Description = "Cat")]
        public string Prompt { get; set; }

        /// <summary>
        /// 垫图base64数组。
        /// </summary>
        [SwaggerSchema("垫图base64数组")]
        public List<string> Base64Array { get; set; }

        /// <summary>
        /// 账号过滤
        /// </summary>
        public AccountFilter AccountFilter { get; set; }
    }
}