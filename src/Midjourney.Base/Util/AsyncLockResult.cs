namespace Midjourney.Base.Util
{
    /// <summary>
    /// 异步锁结果
    /// </summary>
    public class AsyncLockResult : IDisposable
    {
        private readonly AsyncParallelLock _lock;

        /// <summary>
        /// 获取锁是否成功
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="success">是否成功获取锁</param>
        /// <param name="lock">锁对象</param>
        public AsyncLockResult(bool success, AsyncParallelLock @lock)
        {
            Success = success;
            _lock = @lock;
        }

        /// <summary>
        /// 释放锁
        /// </summary>
        public void Dispose()
        {
            if (Success && _lock != null)
            {
                _lock.Unlock();
            }
        }
    }
} 