using System.Diagnostics;
using System.Runtime.InteropServices;
using Serilog;

namespace Midjourney.Infrastructure.Services
{
    /// <summary>
    /// 系统设置接口实现，定义了与设置相关的操作方法。
    /// </summary>
    public class SystemSettingService : ISystemSettingkService
    {
        /// <summary>
        /// 重启系统
        /// </summary>
        public bool restartSystem()
        {
            StartNewInstance();
            return true;
        }

        /// <summary>
        /// 启动新实例
        /// </summary>
        private void StartNewInstance()
        {
            string applicationPath = Process.GetCurrentProcess().MainModule.FileName;
            string workingDirectory = Path.GetDirectoryName(applicationPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = applicationPath,
                WorkingDirectory = workingDirectory,
                Arguments = "--restarted", // 可以添加重启标记
                UseShellExecute = true,
                CreateNoWindow = false
            };

            // 对于非Windows系统可能需要调整
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                startInfo.FileName = "dotnet";
                startInfo.Arguments = applicationPath + " --restarted";
            }

            Process.Start(startInfo);
        }
    }
}