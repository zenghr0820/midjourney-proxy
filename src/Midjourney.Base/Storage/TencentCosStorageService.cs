using COSXML;
using COSXML.Auth;
using COSXML.Model.Object;
using Serilog;

namespace Midjourney.Base.Storage
{
    /// <summary>
    /// 腾讯云存储服务
    /// </summary>
    public class TencentCosStorageService : IStorageService
    {
        private readonly TencentCosOptions _cosOptions;
        private readonly ILogger _logger;

        public TencentCosStorageService()
        {
            _cosOptions = GlobalConfiguration.Setting.TencentCos;

            _logger = Log.Logger;
        }

        public CosXml GetClient()
        {
            // 配置腾讯云 COS 客户端
            var config = new CosXmlConfig.Builder()
                .IsHttps(true)
                .SetAppid(_cosOptions.AppId)
                .SetRegion(_cosOptions.Region)
                .Build();

            var qCloudCredentialProvider = new DefaultQCloudCredentialProvider(
                _cosOptions.SecretId, _cosOptions.SecretKey, 600);

            return new CosXmlServer(config, qCloudCredentialProvider);
        }

        public UploadResult SaveAsync(Stream mediaBinaryStream, string key, string mimeType)
        {
            if (mediaBinaryStream == null || mediaBinaryStream?.Length <= 0)
            {
                throw new ArgumentNullException(nameof(mediaBinaryStream));
            }

            PutObjectRequest request = new PutObjectRequest(_cosOptions.Bucket, key, mediaBinaryStream);
            request.SetRequestHeader("Content-Type", mimeType);

            try
            {
                var client = GetClient();
                PutObjectResult result = client.PutObject(request);
                if (result.httpCode == 200)
                {
                    _logger.Information("上传成功 {@0}", key);

                    return new UploadResult
                    {
                        FileName = Path.GetFileName(key),
                        Key = key,
                        Size = mediaBinaryStream.Length,
                        Md5 = result.GetResultInfo(),  // 获取 ETag 或者 MD5 值
                        ContentType = mimeType,
                        Url = GetSignKey(key, _cosOptions.ExpiredMinutes).ToString()
                    };
                }
                else
                {
                    throw new Exception("上传失败");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "上传文件异常 {@key}", key);
                throw;
            }
        }

        public async Task DeleteAsync(bool isDeleteMedia = false, params string[] keys)
        {
            foreach (var key in keys)
            {
                try
                {
                    _logger.Information("删除文件: {@key}", key);
                    DeleteObjectRequest request = new DeleteObjectRequest(_cosOptions.Bucket, key);

                    // 使用Task.Run将同步操作包装成异步操作
                    await Task.Run(() =>
                    {
                        var client = GetClient();
                        DeleteObjectResult result = client.DeleteObject(request);
                        if (result.httpCode != 204)
                        {
                            _logger.Warning("删除文件失败, {@deleteObjectResult}", result);
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "删除文件异常, {key}", key);
                }
            }
        }

        public Stream GetObject(string key)
        {
            try
            {
                var client = GetClient();
                GetObjectBytesRequest request = new GetObjectBytesRequest(_cosOptions.Bucket, key);
                GetObjectBytesResult result = client.GetObject(request);

                MemoryStream memoryStream = new MemoryStream(result.content);
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "下载文件异常 {@key}", key);
                throw;
            }
        }

        public async Task MoveAsync(string key, string newKey, bool isCopy = false)
        {
            try
            {
                CopyObjectRequest request = new CopyObjectRequest(_cosOptions.Bucket, newKey);

                request.SetCopySource(new COSXML.Model.Tag.CopySourceStruct(_cosOptions.AppId,
                    _cosOptions.Bucket, _cosOptions.Region, key));

                var client = GetClient();
                CopyObjectResult result = client.CopyObject(request);

                if (result.httpCode == 200 && !isCopy)
                {
                    await DeleteAsync(false, key);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "移动文件异常 {@key}, {@newKey}", key, newKey);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                // 使用Task.Run将同步操作包装成异步操作
                return await Task.Run(() =>
                {
                    var client = GetClient();
                    HeadObjectRequest request = new HeadObjectRequest(_cosOptions.Bucket, key);
                    HeadObjectResult result = client.HeadObject(request);
                    return result.httpCode == 200;
                });
            }
            catch
            {
                return false;
            }
        }

        public void Overwrite(Stream mediaBinaryStream, string key, string mimeType)
        {
            SaveAsync(mediaBinaryStream, key, mimeType);
        }

        /// <summary>
        /// 生成带签名的 URL，设置过期时间（单位：分钟）
        /// </summary>
        /// <param name="key">文件的对象 Key</param>
        /// <param name="minutes">签名的过期时间，默认 60 分钟</param>
        /// <returns>带签名的 URL</returns>
        public Uri GetSignKey(string key, int minutes = 60)
        {
            try
            {
                if (minutes <= 0)
                {
                    return new Uri($"{_cosOptions.CustomCdn}/{key}");
                }

                var client = GetClient();

                // 创建签名 URL 的请求
                var preSignatureStruct = new COSXML.Model.Tag.PreSignatureStruct();

                // APPID 获取参考 https://console.cloud.tencent.com/developer
                preSignatureStruct.appid = _cosOptions.AppId;
                // 存储桶所在地域, COS 地域的简称请参照 https://cloud.tencent.com/document/product/436/6224
                preSignatureStruct.region = _cosOptions.Region;
                // 存储桶名称，此处填入格式必须为 bucketname-APPID, 其中 APPID 获取参考 https://console.cloud.tencent.com/developer
                preSignatureStruct.bucket = _cosOptions.Bucket;
                preSignatureStruct.key = key; //对象键
                preSignatureStruct.httpMethod = "GET"; //HTTP 请求方法
                preSignatureStruct.isHttps = true; //生成 HTTPS 请求 URL
                preSignatureStruct.signDurationSecond = minutes * 60; //请求签名时间为600s

                // 生成带签名的 URL
                string signedUrl = client.GenerateSignURL(preSignatureStruct);

                return new Uri(signedUrl);
            }
            catch (Exception ex)
            {
                throw new Exception("生成签名 URL 异常", ex);
            }
        }

        public string GetCustomCdn()
        {
            return _cosOptions.CustomCdn;
        }
    }
}