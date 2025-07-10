﻿

using System.Net;
using Midjourney.Base.Util;
using Serilog;

namespace Midjourney.Base.Storage
{
    /// <summary>
    /// 全局单例存储服务
    /// </summary>
    public class StorageHelper
    {
        private static IStorageService _instance;

        public static IStorageService Instance => _instance;

        /// <summary>
        /// 配置 IStorageService 并初始化
        /// </summary>
        public static void Configure()
        {
            var config = GlobalConfiguration.Setting;

            if (config.ImageStorageType == ImageStorageType.LOCAL)
            {
                _instance = new LocalStorageService();
            }
            else if (config.ImageStorageType == ImageStorageType.OSS)
            {
                _instance = new AliyunOssStorageService();
            }
            else if (config.ImageStorageType == ImageStorageType.COS)
            {
                _instance = new TencentCosStorageService();
            }
            else if (config.ImageStorageType == ImageStorageType.R2)
            {
                _instance = new CloudflareR2StorageService();
            }
            else
            {
                _instance = null;
            }
        }

        /// <summary>
        /// 下载并保存图片
        /// </summary>
        /// <param name="taskInfo"></param>
        public static void DownloadFile(TaskInfo taskInfo)
        {
            // 是否启用保存到文件存储
            if (!GlobalConfiguration.Setting.EnableSaveGeneratedImage)
            {
                return;
            }

            var imageUrl = taskInfo.ImageUrl;
            var isReplicate = taskInfo.IsReplicate;
            var thumbnailUrl = taskInfo.ThumbnailUrl;
            var action = taskInfo.Action;

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            var lockKey = $"download:{imageUrl}";
            var setting = GlobalConfiguration.Setting;

            WebProxy webProxy = null;
            var proxy = setting.Proxy;
            if (!string.IsNullOrEmpty(proxy?.Host))
            {
                webProxy = new WebProxy(proxy.Host, proxy.Port ?? 80);
            }
            var hch = new HttpClientHandler
            {
                UseProxy = webProxy != null,
                Proxy = webProxy,
            };

            // 创建保存路径
            var uri = new Uri(imageUrl);
            var localPath = uri.AbsolutePath.TrimStart('/');

            // 换脸放到私有附件中
            if (isReplicate)
            {
                localPath = $"pri/{localPath}";
            }

            // 判断 localPath 层级
            if (localPath.Split('/').Length <= 1)
            {
                localPath = $"attachments/{localPath}";
            }

            // 阿里云 OSS
            if (setting.ImageStorageType == ImageStorageType.OSS)
            {
                var opt = setting.AliyunOss;
                var cdn = opt.CustomCdn;

                if (string.IsNullOrWhiteSpace(cdn) || imageUrl.StartsWith(cdn))
                {
                    return;
                }

                // 本地锁
                LocalLock.TryLock(lockKey, TimeSpan.FromSeconds(10), () =>
                {
                    var oss = new AliyunOssStorageService();

                    // 替换 url
                    var url = $"{cdn?.Trim()?.Trim('/')}/{localPath}{uri?.Query}";

                    // 下载图片并保存
                    using (HttpClient client = new HttpClient(hch))
                    {
                        client.Timeout = TimeSpan.FromMinutes(15);

                        var response = client.GetAsync(imageUrl).Result;
                        response.EnsureSuccessStatusCode();
                        var stream = response.Content.ReadAsStreamAsync().Result;

                        var mm = MimeKit.MimeTypes.GetMimeType(Path.GetFileName(localPath));
                        if (string.IsNullOrWhiteSpace(mm))
                        {
                            mm = "image/png";
                        }

                        oss.SaveAsync(stream, localPath, mm);

                        // 如果配置了链接有效期，则生成带签名的链接
                        if (opt.ExpiredMinutes > 0)
                        {
                            var priUri = oss.GetSignKey(localPath, opt.ExpiredMinutes);
                            url = $"{cdn?.Trim()?.Trim('/')}/{priUri.PathAndQuery.TrimStart('/')}";
                        }
                    }

                    if (action == TaskAction.SWAP_VIDEO_FACE)
                    {
                        imageUrl = url;
                        thumbnailUrl = url.ToStyle(opt.VideoSnapshotStyle);
                    }
                    else if (action == TaskAction.SWAP_FACE)
                    {
                        // 换脸不格式化 url
                        imageUrl = url;
                        thumbnailUrl = url;
                    }
                    else
                    {
                        imageUrl = url.ToStyle(opt.ImageStyle);
                        thumbnailUrl = url.ToStyle(opt.ThumbnailImageStyle);
                    }
                });
            }
            // 腾讯云 COS
            else if (setting.ImageStorageType == ImageStorageType.COS)
            {
                var opt = setting.TencentCos;
                var cdn = opt.CustomCdn;

                if (string.IsNullOrWhiteSpace(cdn) || imageUrl.StartsWith(cdn))
                {
                    return;
                }

                // 本地锁
                LocalLock.TryLock(lockKey, TimeSpan.FromSeconds(10), () =>
                {
                    var cos = new TencentCosStorageService();

                    // 替换 url
                    var url = $"{cdn?.Trim()?.Trim('/')}/{localPath}{uri?.Query}";

                    // 下载图片并保存
                    using (HttpClient client = new HttpClient(hch))
                    {
                        client.Timeout = TimeSpan.FromMinutes(15);

                        var response = client.GetAsync(imageUrl).Result;
                        response.EnsureSuccessStatusCode();
                        var stream = response.Content.ReadAsStreamAsync().Result;

                        var mm = MimeKit.MimeTypes.GetMimeType(Path.GetFileName(localPath));
                        if (string.IsNullOrWhiteSpace(mm))
                        {
                            mm = "image/png";
                        }

                        cos.SaveAsync(stream, localPath, mm);

                        // 如果配置了链接有效期，则生成带签名的链接
                        if (opt.ExpiredMinutes > 0)
                        {
                            var priUri = cos.GetSignKey(localPath, opt.ExpiredMinutes);
                            url = $"{cdn?.Trim()?.Trim('/')}/{priUri.PathAndQuery.TrimStart('/')}";
                        }
                    }

                    if (action == TaskAction.SWAP_VIDEO_FACE)
                    {
                        imageUrl = url;
                        thumbnailUrl = url.ToStyle(opt.VideoSnapshotStyle);
                    }
                    else if (action == TaskAction.SWAP_FACE)
                    {
                        // 换脸不格式化 url
                        imageUrl = url;
                        thumbnailUrl = url;
                    }
                    else
                    {
                        imageUrl = url.ToStyle(opt.ImageStyle);
                        thumbnailUrl = url.ToStyle(opt.ThumbnailImageStyle);
                    }
                });
            }
            else if (setting.ImageStorageType == ImageStorageType.R2)
            {
                var opt = setting.CloudflareR2;
                var cdn = opt.CustomCdn;

                if (string.IsNullOrWhiteSpace(cdn) || imageUrl.StartsWith(cdn))
                {
                    return;
                }

                // 本地锁
                LocalLock.TryLock(lockKey, TimeSpan.FromSeconds(10), () =>
                {
                    var r2 = new CloudflareR2StorageService();

                    // 替换 url
                    var url = $"{cdn?.Trim()?.Trim('/')}/{localPath}{uri?.Query}";

                    // 下载图片并保存
                    using (HttpClient client = new HttpClient(hch))
                    {
                        client.Timeout = TimeSpan.FromMinutes(15);

                        var response = client.GetAsync(imageUrl).Result;
                        response.EnsureSuccessStatusCode();
                        var stream = response.Content.ReadAsStreamAsync().Result;

                        var mm = MimeKit.MimeTypes.GetMimeType(Path.GetFileName(localPath));
                        if (string.IsNullOrWhiteSpace(mm))
                        {
                            mm = "image/png";
                        }

                        r2.SaveAsync(stream, localPath, mm);

                        // 如果配置了链接有效期，则生成带签名的链接
                        if (opt.ExpiredMinutes > 0)
                        {
                            var priUri = r2.GetSignKey(localPath, opt.ExpiredMinutes);
                            url = $"{cdn?.Trim()?.Trim('/')}/{priUri.PathAndQuery.TrimStart('/')}";
                        }
                    }

                    if (action == TaskAction.SWAP_VIDEO_FACE)
                    {
                        imageUrl = url;
                        thumbnailUrl = url.ToStyle(opt.VideoSnapshotStyle);
                    }
                    else if (action == TaskAction.SWAP_FACE)
                    {
                        // 换脸不格式化 url
                        imageUrl = url;
                        thumbnailUrl = url;
                    }
                    else
                    {
                        imageUrl = url.ToStyle(opt.ImageStyle);
                        thumbnailUrl = url.ToStyle(opt.ThumbnailImageStyle);
                    }
                });
            }
            // https://cdn.discordapp.com/attachments/1265095688782614602/1266300100989161584/03ytbus_LOGO_design_A_warrior_frog_Muscles_like_Popeye_Freehand_06857373-4fd9-403d-a5df-c2f27f9be269.png?ex=66a4a55e&is=66a353de&hm=c597e9d6d128c493df27a4d0ae41204655ab73f7e885878fc1876a8057a7999f&
            // 将图片保存到本地，并替换 url，并且保持原 url和参数
            // 默认保存根目录为 /wwwroot
            // 保存图片
            // 如果处理过了，则不再处理
            else if (setting.ImageStorageType == ImageStorageType.LOCAL)
            {
                var opt = setting.LocalStorage;
                var cdn = opt.CustomCdn;

                if (string.IsNullOrWhiteSpace(cdn) || imageUrl.StartsWith(cdn))
                {
                    return;
                }

                // 本地锁
                LocalLock.TryLock(lockKey, TimeSpan.FromSeconds(10), () =>
                {
                    // 如果路径是 ephemeral-attachments 或 attachments 才处理

                    // 如果是本地文件，则依然放到 attachments
                    // 换脸放到附件中
                    if (isReplicate)
                    {
                        localPath = $"attachments/{localPath}";
                    }

                    var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", localPath);
                    var directoryPath = Path.GetDirectoryName(savePath);

                    if (!string.IsNullOrWhiteSpace(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);

                        // 下载图片并保存
                        using (HttpClient client = new HttpClient(hch))
                        {
                            var response = client.GetAsync(imageUrl).Result;
                            response.EnsureSuccessStatusCode();
                            var imageBytes = response.Content.ReadAsByteArrayAsync().Result;
                            File.WriteAllBytes(savePath, imageBytes);
                        }

                        // 替换 url
                        imageUrl = $"{cdn?.Trim()?.Trim('/')}/{localPath}{uri?.Query}";
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                taskInfo.ImageUrl = imageUrl;
            }

            if (!string.IsNullOrWhiteSpace(thumbnailUrl))
            {
                taskInfo.ThumbnailUrl = thumbnailUrl;
            }
        }


        /// <summary>
        /// 保存内存中的文件到存储服务并返回访问URL
        /// </summary>
        /// <param name="stream">文件内容流</param>
        /// <param name="filename">文件名</param>
        /// <param name="contentType">内容类型</param>
        /// <returns>访问URL</returns>
        public static string SaveFileAsync(Stream stream, string filename, string contentType)
        {
            // 是否启用保存到文件存储
            if (!GlobalConfiguration.Setting.EnableSaveGeneratedImage || _instance == null)
            {
                return null;
            }

            var setting = GlobalConfiguration.Setting;
            string resultUrl = null;

            try
            {
                // 构建存储路径 - 使用一个固定的目录结构
                string path = $"attachments/merges/{DateTime.UtcNow:yyyy/MM/dd}/{filename}";

                // 阿里云 OSS
                if (setting.ImageStorageType == ImageStorageType.OSS)
                {
                    var opt = setting.AliyunOss;
                    var cdn = opt.CustomCdn;

                    if (string.IsNullOrWhiteSpace(cdn))
                    {
                        return null;
                    }

                    _instance.SaveAsync(stream, path, contentType);

                    // 构建访问URL
                    resultUrl = $"{cdn.Trim().TrimEnd('/')}/{path}";

                    // 如果配置了链接有效期，则生成带签名的链接
                    if (opt.ExpiredMinutes > 0)
                    {
                        var priUri = _instance.GetSignKey(path, opt.ExpiredMinutes);
                        resultUrl = $"{cdn.Trim().TrimEnd('/')}/{priUri.PathAndQuery.TrimStart('/')}";
                    }

                    // 根据内容类型应用不同的样式
                    if (contentType == "image/webp" || contentType.StartsWith("image/"))
                    {
                        resultUrl = resultUrl.ToStyle(opt.ImageStyle);
                    }
                }
                // 腾讯云 COS
                else if (setting.ImageStorageType == ImageStorageType.COS)
                {
                    var opt = setting.TencentCos;
                    var cdn = opt.CustomCdn;

                    if (string.IsNullOrWhiteSpace(cdn))
                    {
                        return null;
                    }

                    _instance.SaveAsync(stream, path, contentType);

                    // 构建访问URL
                    resultUrl = $"{cdn.Trim().TrimEnd('/')}/{path}";

                    // 如果配置了链接有效期，则生成带签名的链接
                    if (opt.ExpiredMinutes > 0)
                    {
                        var priUri = _instance.GetSignKey(path, opt.ExpiredMinutes);
                        resultUrl = $"{cdn.Trim().TrimEnd('/')}/{priUri.PathAndQuery.TrimStart('/')}";
                    }

                    // 根据内容类型应用不同的样式
                    if (contentType == "image/webp" || contentType.StartsWith("image/"))
                    {
                        resultUrl = resultUrl.ToStyle(opt.ImageStyle);
                    }
                }
                // Cloudflare R2
                else if (setting.ImageStorageType == ImageStorageType.R2)
                {
                    var opt = setting.CloudflareR2;
                    var cdn = opt.CustomCdn;

                    if (string.IsNullOrWhiteSpace(cdn))
                    {
                        return null;
                    }

                    _instance.SaveAsync(stream, path, contentType);

                    // 构建访问URL
                    resultUrl = $"{cdn.Trim().TrimEnd('/')}/{path}";

                    // 如果配置了链接有效期，则生成带签名的链接
                    if (opt.ExpiredMinutes > 0)
                    {
                        var priUri = _instance.GetSignKey(path, opt.ExpiredMinutes);
                        resultUrl = $"{cdn.Trim().TrimEnd('/')}/{priUri.PathAndQuery.TrimStart('/')}";
                    }

                    // 根据内容类型应用不同的样式
                    if (contentType == "image/webp" || contentType.StartsWith("image/"))
                    {
                        resultUrl = resultUrl.ToStyle(opt.ImageStyle);
                    }
                }
                // 本地存储
                else if (setting.ImageStorageType == ImageStorageType.LOCAL)
                {
                    var opt = setting.LocalStorage;
                    var cdn = opt.CustomCdn;

                    if (string.IsNullOrWhiteSpace(cdn))
                    {
                        return null;
                    }

                    var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", path.Trim('/'));

                    _instance.SaveAsync(stream, localPath, contentType);

                    // 构建访问URL
                    resultUrl = $"{cdn.Trim().TrimEnd('/')}/{path.Trim('/')}";
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出
                Log.Error(ex, "保存文件到存储服务失败: {Filename}, {ContentType}", filename, contentType);

                resultUrl = null;
            }

            return resultUrl;
        }

    }
}