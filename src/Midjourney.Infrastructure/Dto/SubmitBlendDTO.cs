﻿
using Swashbuckle.AspNetCore.Annotations;

namespace Midjourney.Infrastructure.Dto
{
    /// <summary>
    /// Blend提交参数。
    /// </summary>
    [SwaggerSchema("Blend提交参数")]
    public class SubmitBlendDTO : BaseSubmitDTO
    {
        /// <summary>
        /// bot 类型，mj(默认)或niji
        /// MID_JOURNEY | 枚举值: NIJI_JOURNEY
        /// </summary>
        public string BotType { get; set; } 

        /// <summary>
        /// 图片base64数组。
        /// </summary>
        [SwaggerSchema("图片base64数组", Description = "[\"data:image/png;base64,xxx1\", \"data:image/png;base64,xxx2\"]")]
        public List<string> Base64Array { get; set; }

        /// <summary>
        /// 比例: PORTRAIT(2:3); SQUARE(1:1); LANDSCAPE(3:2)。
        /// </summary>
        [SwaggerSchema("比例: PORTRAIT(2:3); SQUARE(1:1); LANDSCAPE(3:2)", Description = "SQUARE")]
        public BlendDimensions? Dimensions { get; set; } = BlendDimensions.SQUARE;

        /// <summary>
        /// 账号过滤支持
        /// </summary>
        public AccountFilter AccountFilter { get; set; }
    }
}