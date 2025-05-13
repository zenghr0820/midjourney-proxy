namespace Midjourney.Infrastructure.Util
{
    /// <summary>
    /// 定义一个基于信号量的锁管理类，支持动态调整最大并行度
    /// </summary>
    public class AsyncParallelLock : IDisposable
    {
        private readonly object _syncLock = new object();
        private SemaphoreSlim _semaphore;
        private int _maxCount; // 存储最大数量
        private int _currentlyHeld; // 跟踪当前已获取的资源数量

        /// <summary>
        /// 构造并发锁，允许设置最大并发数量。
        /// </summary>
        /// <param name="maxParallelism">最大并行数量。</param>
        public AsyncParallelLock(int maxParallelism)
        {
            if (maxParallelism <= 0)
                throw new ArgumentException("并行数必须大于0", nameof(maxParallelism));

            _maxCount = maxParallelism;
            _currentlyHeld = 0;
            _semaphore = new SemaphoreSlim(maxParallelism, maxParallelism);
        }

        /// <summary>
        /// 最大并发数
        /// </summary>
        public int MaxParallelism
        {
            get
            {
                lock (_syncLock)
                {
                    return _maxCount;
                }
            }
        }

        /// <summary>
        /// 当前已获取的资源数量
        /// </summary>
        public int CurrentlyHeldCount
        {
            get
            {
                lock (_syncLock)
                {
                    return _currentlyHeld;
                }
            }
        }

        /// <summary>
        /// 当前可用的资源数量
        /// </summary>
        public int AvailableCount
        {
            get
            {
                lock (_syncLock)
                {
                    return _semaphore.CurrentCount;
                }
            }
        }

        /// <summary>
        /// 设置新的最大并行度（必须所有锁可用时才允许修改）
        /// </summary>
        /// <param name="newMaxParallelism">新的最大并行数量</param>
        /// <returns>设置是否成功</returns>
        public bool SetMaxParallelism(int newMaxParallelism)
        {
            if (newMaxParallelism <= 0)
                throw new ArgumentException("并行数必须大于0", nameof(newMaxParallelism));

            lock (_syncLock)
            {
                // 如果新值与当前值一致，无需调整
                if (newMaxParallelism == _maxCount)
                    return true;

                // 检查是否所有锁都可用（没有锁被持有）
                if (_currentlyHeld > 0 || _semaphore.CurrentCount < _maxCount)
                {
                    // 如果有锁被持有，不允许调整
                    return false;
                }

                // 创建新的信号量实例
                var oldSemaphore = _semaphore;
                _semaphore = new SemaphoreSlim(newMaxParallelism, newMaxParallelism);
                _maxCount = newMaxParallelism;

                // 释放旧的信号量
                oldSemaphore.Dispose();

                return true;
            }
        }

        /// <summary>
        /// 异步等待获取锁。
        /// </summary>
        public async Task LockAsync(CancellationToken cancellationToken = default)
        {
            SemaphoreSlim semaphore;

            lock (_syncLock)
            {
                semaphore = _semaphore;
            }

            // 在锁外等待，避免死锁
            await semaphore.WaitAsync(cancellationToken);

            lock (_syncLock)
            {
                // 增加持有计数
                _currentlyHeld++;
            }
        }

        /// <summary>
        /// 同步等待获取锁。
        /// </summary>
        public void Lock(CancellationToken cancellationToken = default)
        {
            SemaphoreSlim semaphore;

            lock (_syncLock)
            {
                semaphore = _semaphore;
            }

            // 在锁外等待，避免死锁
            semaphore.Wait(cancellationToken);

            lock (_syncLock)
            {
                // 增加持有计数
                _currentlyHeld++;
            }
        }

        /// <summary>
        /// 尝试获取锁，如果无法立即获取则返回失败。
        /// </summary>
        /// <returns>如果成功获取锁返回true，否则返回false</returns>
        public bool TryLock()
        {
            SemaphoreSlim semaphore;

            lock (_syncLock)
            {
                semaphore = _semaphore;
            }

            // 尝试立即获取锁
            bool acquired = semaphore.Wait(0);
            if (acquired)
            {
                lock (_syncLock)
                {
                    _currentlyHeld++;
                }
            }

            return acquired;
        }

        /// <summary>
        /// 尝试异步获取锁，有超时时间。
        /// </summary>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>锁结果对象，包含是否成功获取锁</returns>
        public async Task<AsyncLockResult> TryLockAsync(int timeoutMs)
        {
            SemaphoreSlim semaphore;

            lock (_syncLock)
            {
                semaphore = _semaphore;
            }

            // 尝试在指定时间内获取锁
            bool acquired = await semaphore.WaitAsync(timeoutMs);
            if (acquired)
            {
                lock (_syncLock)
                {
                    _currentlyHeld++;
                }
                return new AsyncLockResult(true, this);
            }

            return new AsyncLockResult(false, null);
        }

        /// <summary>
        /// 释放锁。
        /// </summary>
        public void Unlock()
        {
            SemaphoreSlim semaphore;

            lock (_syncLock)
            {
                if (_currentlyHeld <= 0)
                    throw new InvalidOperationException("尝试释放未获取的锁");

                _currentlyHeld--;
                semaphore = _semaphore;
            }

            semaphore.Release();
        }

        /// <summary>
        /// 判断当前是否有可用锁。
        /// </summary>
        /// <returns>如果有可用锁则返回 true，否则返回 false。</returns>
        public bool IsLockAvailable()
        {
            lock (_syncLock)
            {
                return _semaphore.CurrentCount > 0;
            }
        }

        /// <summary>
        /// 判断是否所有锁都可用（没有锁被持有）
        /// </summary>
        /// <returns>如果所有锁都可用返回true，否则返回false</returns>
        public bool AreAllLocksAvailable()
        {
            lock (_syncLock)
            {
                return _currentlyHeld == 0 && _semaphore.CurrentCount == _maxCount;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            lock (_syncLock)
            {
                _semaphore?.Dispose();
                _semaphore = null;
            }
        }
    }
}