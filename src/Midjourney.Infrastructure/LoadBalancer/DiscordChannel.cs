using System.Collections.Concurrent;

namespace Midjourney.Infrastructure.LoadBalancer
{
    /// <summary>
    /// Discord频道类
    /// 管理单个频道的任务和队列
    /// </summary>
    public class DiscordChannel
    {
        /// <summary>
        /// 频道ID
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// 服务器ID
        /// </summary>
        public string GuildId { get; set; }

        /// <summary>
        /// 频道名称
        /// </summary>
        public string ChannelName { get; set; }

        /// <summary>
        /// 频道描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// 频道队列大小限制
        /// </summary>
        public int QueueSize { get; set; } = 10;

        /// <summary>
        /// 频道最大并发数
        /// </summary>
        public int MaxConcurrent { get; set; } = 3;

        /// <summary>
        /// 正在运行的任务列表 key：任务ID，value：任务信息
        /// </summary>
        public ConcurrentDictionary<string, TaskInfo> RunningTasks { get; } = new();

        /// <summary>
        /// 任务Future映射 key：任务ID，value：作业 Action
        /// </summary>
        public ConcurrentDictionary<string, Task> TaskFutureMap { get; } = new();

        /// <summary>
        /// 任务取消令牌映射 key：任务ID，value：取消令牌
        /// </summary>
        public ConcurrentDictionary<string, CancellationTokenSource> TaskCancellationTokens { get; } = new();

        /// <summary>
        /// 任务持有锁状态映射 key：任务ID，value：是否持有锁
        /// </summary>
        public ConcurrentDictionary<string, bool> TaskHoldingLock { get; } = new();

        /// <summary>
        /// 当前队列任务
        /// </summary>
        public ConcurrentQueue<(TaskInfo, Func<Task<Message>>)> QueueTasks { get; set; } = new();

        /// <summary>
        /// 信号量锁
        /// </summary>
        public AsyncParallelLock SemaphoreSlimLock { get; set; }

        /// <summary>
        /// 信号控制
        /// </summary>
        public ManualResetEvent SignalControl { get; set; }

        public DiscordChannel(string channelId, string channelName = null, int queueSize = 10, int maxConcurrent = 3)
        {
            ChannelId = channelId;
            ChannelName = channelName ?? channelId;
            QueueSize = queueSize;
            MaxConcurrent = maxConcurrent;

            // 初始化信号量锁
            SemaphoreSlimLock = new AsyncParallelLock(Math.Max(1, Math.Min(maxConcurrent, 12)));

            // 初始化信号控制
            SignalControl = new ManualResetEvent(false);
        }

        /// <summary>
        /// 是否存在空闲队列，即：队列是否已满，是否可加入新的任务
        /// </summary>
        public bool IsIdleQueue => QueueSize <= 0 || QueueTasks.Count < QueueSize;

        /// <summary>
        /// 获取正在运行的任务列表
        /// </summary>
        public List<TaskInfo> GetRunningTasks() => RunningTasks.Values.ToList();

        /// <summary>
        /// 获取队列中的任务列表
        /// </summary>
        public List<TaskInfo> GetQueueTasks() => QueueTasks.Select(c => c.Item1).ToList();

        /// <summary>
        /// 获取所有任务列表（包括正在运行和队列中的任务）
        /// </summary>
        public List<TaskInfo> GetAllTasks()
        {
            var result = new List<TaskInfo>();
            result.AddRange(GetRunningTasks());
            result.AddRange(GetQueueTasks());
            return result;
        }

        /// <summary>
        /// 移除指定任务（包括队列和运行中的任务）
        /// </summary>
        /// <param name="taskId">要移除的任务ID</param>
        /// <param name="reason">失败原因，如果不为空则将任务标记为失败</param>
        /// <returns>是否成功移除任务</returns>
        public bool RemoveTask(string taskId, string reason = null)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                return false;
            }

            bool taskRemoved = false;

            // 获取任务的CancellationTokenSource（如果存在）
            if (TaskCancellationTokens.TryRemove(taskId, out var cts))
            {
                // 请求取消任务
                try
                {
                    cts.Cancel();
                    taskRemoved = true;
                }
                catch (Exception)
                {
                    // 忽略令牌取消异常
                }
                finally
                {
                    // 释放资源
                    cts.Dispose();
                }
            }

            // 从任务Future映射中移除
            if (TaskFutureMap.TryRemove(taskId, out _))
            {
                taskRemoved = true;
            }

            // 从运行中任务列表移除
            if (RunningTasks.TryRemove(taskId, out var runningTask))
            {
                runningTask.Fail(reason ?? "任务已取消");
                taskRemoved = true;
            }

            // 从队列任务中移除
            if (QueueTasks.Any(c => c.Item1.Id == taskId))
            {
                var tempQueue = new ConcurrentQueue<(TaskInfo, Func<Task<Message>>)>();

                // 将不需要移除的元素加入到临时队列中
                while (QueueTasks.TryDequeue(out var item))
                {
                    if (item.Item1.Id != taskId)
                    {
                        tempQueue.Enqueue(item);
                    }
                    else
                    {
                        // 找到匹配的任务，将其标记为失败
                        if (item.Item1.Status != TaskStatus.FAILURE)
                        {
                            // 设置状态为失败
                            item.Item1.Status = TaskStatus.FAILURE;
                            item.Item1.Fail(reason ?? "任务已取消");
                        }
                        taskRemoved = true;
                    }
                }

                // 交换队列引用
                QueueTasks = tempQueue;
            }

            // 任务如果持有锁，释放它
            if (TaskHoldingLock.TryRemove(taskId, out var hasLock) && hasLock)
            {
                try
                {
                    SemaphoreSlimLock.Unlock();
                }
                catch
                {
                    // 忽略解锁异常
                }
            }
            

            return taskRemoved;
        }

        /// <summary>
        /// 清理所有等待队列中的任务（不影响运行中的任务）
        /// </summary>
        /// <param name="reason">取消原因</param>
        /// <returns>被清理的任务数量</returns>
        public int ClearQueueTasks(string reason = "任务队列已清空")
        {
            int clearedCount = 0;
            try
            {
                // 获取队列中所有任务的副本
                var queueTasksInfo = GetQueueTasks();
                clearedCount = queueTasksInfo.Count;

                // 清空队列
                var tempQueue = new ConcurrentQueue<(TaskInfo, Func<Task<Message>>)>();
                while (QueueTasks.TryDequeue(out var item))
                {
                    if (!string.IsNullOrWhiteSpace(reason))
                    {
                        item.Item1.Fail(reason);
                    }
                }

                // 替换队列
                QueueTasks = tempQueue;

                // 唤醒可能在等待的线程
                try
                {
                    SignalControl.Set();
                }
                catch (ObjectDisposedException)
                {
                    // 忽略已释放的对象异常
                }
            }
            catch (Exception)
            {
                // 忽略异常，确保清理过程不中断
            }

            return clearedCount;
        }

        /// <summary>
        /// 清理所有运行中的任务（不影响等待队列中的任务）
        /// </summary>
        /// <param name="reason">取消原因</param>
        /// <returns>被清理的任务数量</returns>
        public int ClearRunningTasks(string reason = "运行中的任务已取消")
        {
            int clearedCount = 0;
            try
            {
                // 获取所有运行中任务的ID列表
                var runningTaskIds = RunningTasks.Keys.ToList();
                clearedCount = runningTaskIds.Count;

                // 取消所有运行中任务的令牌
                foreach (var taskId in runningTaskIds)
                {
                    try
                    {
                        if (TaskCancellationTokens.TryRemove(taskId, out var cts))
                        {
                            cts.Cancel();
                            cts.Dispose();
                        }

                        // 从运行中任务列表移除并标记失败
                        if (RunningTasks.TryRemove(taskId, out var task) && !string.IsNullOrWhiteSpace(reason))
                        {
                            task.Fail(reason);
                        }

                        // 从任务Future映射中移除
                        TaskFutureMap.TryRemove(taskId, out _);

                        // 任务如果持有锁，释放它
                        if (TaskHoldingLock.TryRemove(taskId, out var hasLock) && hasLock)
                        {
                            try
                            {
                                SemaphoreSlimLock.Unlock();
                            }
                            catch
                            {
                                // 忽略解锁异常
                            }
                        }
                    }
                    catch
                    {
                        // 忽略单个任务取消过程中的异常，继续处理其他任务
                    }
                }
            }
            catch (Exception)
            {
                // 忽略异常，确保清理过程不中断
            }

            return clearedCount;
        }

        /// <summary>
        /// 根据条件筛选并清理任务
        /// </summary>
        /// <param name="predicate">任务筛选条件</param>
        /// <param name="reason">取消原因</param>
        /// <param name="includeQueue">是否包含队列中的任务</param>
        /// <param name="includeRunning">是否包含运行中的任务</param>
        /// <returns>被清理的任务列表</returns>
        public List<TaskInfo> ClearTasksByCondition(Func<TaskInfo, bool> predicate, string reason = "任务已按条件取消",
            bool includeQueue = true, bool includeRunning = true)
        {
            var clearedTasks = new List<TaskInfo>();
            try
            {
                // 处理运行中的任务
                if (includeRunning)
                {
                    var runningTaskIds = RunningTasks.Where(kv => predicate(kv.Value))
                                                      .Select(kv => kv.Key)
                                                      .ToList();

                    foreach (var taskId in runningTaskIds)
                    {
                        if (RemoveTask(taskId, reason) && RunningTasks.TryGetValue(taskId, out var task))
                        {
                            clearedTasks.Add(task);
                        }
                    }
                }

                // 处理队列中的任务
                if (includeQueue)
                {
                    var tempQueue = new ConcurrentQueue<(TaskInfo, Func<Task<Message>>)>();
                    while (QueueTasks.TryDequeue(out var item))
                    {
                        if (predicate(item.Item1))
                        {
                            item.Item1.Fail(reason);
                            clearedTasks.Add(item.Item1);
                        }
                        else
                        {
                            tempQueue.Enqueue(item);
                        }
                    }

                    // 交换队列引用
                    QueueTasks = tempQueue;
                }
            }
            catch (Exception)
            {
                // 忽略异常，确保清理过程不中断
            }

            return clearedTasks;
        }

        /// <summary>
        /// 取消并清理所有任务
        /// </summary>
        /// <param name="reason">取消原因</param>
        public void CancelAndClearAllTasks(string reason)
        {
            try
            {
                // 通知信号控制退出等待
                try
                {
                    SignalControl.Set();
                }
                catch (ObjectDisposedException)
                {
                    // 忽略已释放的对象异常
                }

                // 取消所有任务的令牌
                foreach (var tokenEntry in TaskCancellationTokens)
                {
                    try
                    {
                        tokenEntry.Value.Cancel();
                        tokenEntry.Value.Dispose();
                    }
                    catch
                    {
                        // 忽略令牌取消异常
                    }
                }

                // 标记所有运行中的任务为失败
                foreach (var runningTask in RunningTasks)
                {
                    runningTask.Value.Fail(reason);
                }

                // 清理任务队列
                while (QueueTasks.TryDequeue(out var taskInfo))
                {
                    taskInfo.Item1.Fail(reason);
                }

                // 清理任务相关集合
                TaskHoldingLock.Clear();
                TaskFutureMap.Clear();
                TaskCancellationTokens.Clear();
                RunningTasks.Clear();
            }
            catch
            {
                // 忽略异常，确保清理过程不中断
            }
        }

        /// <summary>
        /// 处理资源释放
        /// </summary>
        public void Dispose()
        {
            try
            {
                // 释放信号量
                SemaphoreSlimLock?.Dispose();

                // 释放信号
                SignalControl?.Dispose();
            }
            catch
            {
                // 忽略释放异常
            }
        }
    }
}