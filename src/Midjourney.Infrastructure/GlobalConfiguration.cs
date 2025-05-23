﻿using Microsoft.Extensions.Caching.Memory;
using System.Runtime.InteropServices;

namespace Midjourney.Infrastructure
{
    /// <summary>
    /// 全局配置
    /// </summary>
    public class GlobalConfiguration
    {
        /// <summary>
        /// 网站配置为演示模式
        /// </summary>
        public static bool? IsDemoMode { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public static string Version { get; set; } = "v7.0.1";

        /// <summary>
        /// 全局配置项
        /// </summary>
        public static Setting Setting { get; set; }

        /// <summary>
        /// 全局缓存项
        /// </summary>
        public static IMemoryCache MemoryCache { get; set; }

        /// <summary>
        /// 站点根目录 wwwroot
        /// </summary>
        public static string WebRootPath { get; set; }

        /// <summary>
        /// 静态文件根目录
        /// </summary>
        public static string ContentRootPath { get; set; }

        /// <summary>
        /// 判断是否是 Windows 系统
        /// </summary>
        /// <returns></returns>
        public static bool IsWindows()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        /// <summary>
        /// 判断是否是 Linux 系统
        /// </summary>
        /// <returns></returns>
        public static bool IsLinux()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }

        /// <summary>
        /// 判断是否是 macOS 系统
        /// </summary>
        /// <returns></returns>
        public static bool IsMacOS()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                || Environment.OSVersion.Platform == PlatformID.MacOSX;
        }
    }
}