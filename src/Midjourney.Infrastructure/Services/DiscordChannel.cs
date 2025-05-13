using System.Collections.Concurrent;
using Midjourney.Infrastructure.Util;

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