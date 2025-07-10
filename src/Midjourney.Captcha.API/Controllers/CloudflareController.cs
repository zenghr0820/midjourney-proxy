using Microsoft.AspNetCore.Mvc;
using Midjourney.Base.Dto;

namespace Midjourney.Captcha.API.Controllers
{
    /// <summary>
    /// Cloudflare 自动验证控制器。
    /// </summary>
    [Route("cf")]
    [ApiController]
    public class CloudflareController : ControllerBase
    {
        /// <summary>
        /// 校验 Cloudflare URL
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("verify")]
        public ActionResult Validate([FromBody] CaptchaVerfyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.State))
            {
                return BadRequest("State 不能为空");
            }

            CloudflareQueueHostedService.EnqueueRequest(request);

            return Ok();
        }
    }
}