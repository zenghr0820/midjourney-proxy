using Microsoft.AspNetCore.Mvc;
using Midjourney.Base.Dto;

namespace Midjourney.Captcha.API.Controllers
{
    /// <summary>
    /// 自动登录控制器。
    /// </summary>
    [Route("login")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        /// <summary>
        /// 自动登录
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("auto")]
        public ActionResult AutoLogin([FromBody] AutoLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.State))
            {
                return BadRequest("State 不能为空");
            }

            if (string.IsNullOrWhiteSpace(request.LoginAccount) || string.IsNullOrWhiteSpace(request.LoginPassword))
            {
                return BadRequest("账号或密码不能为空");
            }

            if (string.IsNullOrWhiteSpace(request.Login2fa))
            {
                return BadRequest("2FA 密钥不能为空");
            }

            SeleniumLoginQueueHostedService.EnqueueRequest(request);

            return Ok();
        }
    }
}