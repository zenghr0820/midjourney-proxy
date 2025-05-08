using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.Util;

namespace Midjourney.Infrastructure.Services
{
    /// <summary>
    /// 系统设置服务接口，定义了与设置相关的操作方法。
    /// </summary>
    public interface ISystemSettingkService
    {

        /// <summary>
        ///     重启系统
        /// </summary>
        bool restartSystem();

    }
}