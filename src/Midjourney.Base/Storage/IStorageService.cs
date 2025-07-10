

namespace Midjourney.Base.Storage
{
    /// <summary>
    /// 存储服务
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// 上传
        /// </summary>
        /// <param name="mediaBinaryStream"></param>
        /// <param name="key"></param>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        UploadResult SaveAsync(Stream mediaBinaryStream, string key, string mimeType);

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="isDeleteMedia">是否标识删除记录</param>
        /// <param name="keys"></param>
        /// <returns></returns>
        Task DeleteAsync(bool isDeleteMedia = false, params string[] keys);

        /// <summary>
        /// 获取文件流数据
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Stream GetObject(string key);

        ///// <summary>
        ///// 获取文件流数据,返回文件类型
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="contentType"></param>
        ///// <returns></returns>
        //Stream GetObject(string key, out string contentType);

        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="key"></param>
        /// <param name="newKey"></param>
        /// <param name="isCopy"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        Task MoveAsync(string key, string newKey, bool isCopy = false);

        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// 覆盖保存文件
        /// </summary>
        /// <param name="mediaBinaryStream"></param>
        /// <param name="key"></param>
        /// <param name="mimeType"></param>
        void Overwrite(Stream mediaBinaryStream, string key, string mimeType);

        /// <summary>
        /// 获取自定义加速域名
        /// </summary>
        /// <returns></returns>
        string GetCustomCdn();

        /// <summary>
        /// 生成带签名的URL，设置过期时间为 1 小时
        /// </summary>
        /// <param name="key"></param>
        /// <param name="minutes"></param>
        /// <returns></returns>
        Uri GetSignKey(string key, int minutes = 60);
    }
}