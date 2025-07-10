using System.Collections.Concurrent;
using System.Diagnostics;
using Midjourney.Infrastructure.Services;
using Serilog;

namespace Midjourney.Infrastructure.LoadBalancer
{
    /// <summary>
    /// 频道池管理器：负责管理Discord频道池的创建、更新和选择逻辑
    /// </summary>
    public class ChannelPoolManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, DiscordChannel> _channelPool = new();
        private readonly DiscordInstance _instance;
        private readonly IChannelSelectionStrategy _selectionStrategy;
        private readonly CancellationTokenSource _longToken;
        private readonly DiscordAccount _account;

        /// <summary>
        /// taskChangeEvent 任务变化事件
        /// </summary>
        #region TaskChangeEvent
        public event Func<TaskInfo, Task> TaskChangeEvent
        {
            add => _taskChangeEvent.Add(value);
            remove => _taskChangeEvent.Remove(value);
        }
        private readonly AsyncEvent<Func<TaskInfo, Task>> _taskChangeEvent = new();
        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="selectionStrategy">频道选择策略</param>
        public ChannelPoolManager(DiscordInstance instance, IChannelSelectionStrategy selectionStrategy = null)
        {
            _logger = Log.Logger.ForContext("LogPrefix", "ChannelPoolManager");
            _instance = instance;
            _account = instance.Account;
            _selectionStrategy = selectionStrategy ?? new DefaultChannelSelectionStrategy();

            // 初始化默认频道（主频道）
            InitChannel(_account.ChannelId);

            if (_account.EnableAutoFetchChannels)
            {
                // 初始化频道池中的其他频道
                if (_account.ChannelIds != null)
                {
                    foreach (var channel in _account.ChannelIds)
                    {
                        InitChannel(channel);
                    }
                }
            }

            // 后台任务取消 token
            _longToken = new CancellationTokenSource();

            // 为每个频道启动任务处理线程
            foreach (var channel in _channelPool)
            {
                StartChannelTaskProcessing(channel.Value);
            }

            // 启动频道监控线程
            // StartChannelMonitoring();
        }

        /// <summary>
        /// 获取所有频道ID
        /// </summary>
        public List<string> AllChannelIds => _channelPool.Keys.ToList();
        
        /// <summary>
        /// 悠船账号任务Service
        /// </summary>
        public IYmTaskService YmTaskService { get; set; }

        /// <summary>
        /// 获取所有频道
        /// </summary>
        public List<DiscordChannel> Channels => [.. _channelPool.Values];

        /// <summary>
        /// 获取频道池中的频道数量
        /// </summary>
        public int Count => _channelPool.Count;

        /// <summary>
        /// 初始化频道
        /// </summary>
        /// <param name="channelId">频道ID</param>
        /// <param name="channelName">频道名称</param>
        private void InitChannel(string channelId, string channelName = null)
        {
            if (string.IsNullOrWhiteSpace(channelId))
                return;

            if (!_channelPool.ContainsKey(channelId))
            {
                // 如果开启了自动获取频道且有多个频道，则每个频道的并发数设为1
                // 否则使用账号配置的并发数
                int maxConcurrency = _account.EnableAutoFetchChannels && (_account.ChannelIds?.Count > 0 || (_channelPool.Count > 0 && channelId != _account.ChannelId))
                    ? 1  // 多频道模式，每个频道并发数为1
                    : Math.Max(1, Math.Min(_account.CoreSize, 12)); // 单频道模式，使用账号配置的并发数

                var channel = new DiscordChannel(
                    channelId,
                    channelName ?? channelId,
                    _account.QueueSize,
                    maxConcurrency
                );
                channel.GuildId = _account.GuildId;
                _channelPool.TryAdd(channelId, channel);
            }
        }

        /// <summary>
        /// 启动频道任务处理线程
        /// </summary>
        private void StartChannelTaskProcessing(DiscordChannel channel)
        {
            // 创建任务处理线程
            Task.Factory.StartNew(() =>
            {
                _logger.Information("频道 {0} 任务处理线程启动", channel.ChannelId);

                // 设置线程名称
                Thread.CurrentThread.Name = $"Task-{channel.ChannelId}";

                // 重置信号
                try 
                {
                    channel.SignalControl.Reset();
                }
                catch (ObjectDisposedException)
                {
                    _logger.Warning("频道 {0} 的SignalControl已释放，任务处理线程退出", channel.ChannelId);
                    return;
                }

                while (true)
                {
                    try
                    {
                        // 如果处于取消状态，则退出
                        if (_longToken.IsCancellationRequested)
                        {
                            break;
                        }

                        // 如果队列为空且没有运行中的任务，等待信号
                        if (channel.QueueTasks.IsEmpty && channel.RunningTasks.IsEmpty)
                        {
                            try
                            {
                                // 安全访问SignalControl，处理可能的ObjectDisposedException
                                if (!channel.SignalControl.WaitOne(TimeSpan.FromSeconds(15)))
                                {
                                    // 超时，继续循环
                                    continue;
                                }
                            }
                            catch (ObjectDisposedException)
                            {
                                _logger.Warning("频道 {0} 的SignalControl已释放，任务处理线程退出", channel.ChannelId);
                                return; // 直接退出线程
                            }
                            continue;
                        }

                        // 判断是否还有资源可用
                        try
                        {
                            if (!channel.SemaphoreSlimLock.IsLockAvailable())
                            {
                                // 如果没有可用资源，等待
                                Thread.Sleep(100);
                                continue;
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            _logger.Warning("频道 {0} 的SemaphoreSlimLock已释放，任务处理线程退出", channel.ChannelId);
                            return; // 直接退出线程
                        }

                        // 根据频道配置设置信号量最大值
                        int targetMaxParallelism = _account.EnableAutoFetchChannels && (_account.ChannelIds?.Count > 0 || _channelPool.Count > 1)
                            ? 1  // 多频道模式，每个频道并发数为1
                            : Math.Max(1, Math.Min(_account.CoreSize, 12)); // 单频道模式，使用账号配置的并发数

                        // 如果并发数修改，判断信号最大值是否需要更新
                        try
                        {
                            if (channel.SemaphoreSlimLock.MaxParallelism != targetMaxParallelism)
                            {
                                var oldMax = channel.SemaphoreSlimLock.MaxParallelism;
                                if (channel.SemaphoreSlimLock.SetMaxParallelism(targetMaxParallelism))
                                {
                                    _logger.Information("频道 {0} 信号量最大值修改成功，原值：{1}，当前最大值：{2}",
                                        channel.ChannelId, oldMax, targetMaxParallelism);
                                }

                                Thread.Sleep(500);
                                continue;
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            _logger.Warning("频道 {0} 的SemaphoreSlimLock已释放，任务处理线程退出", channel.ChannelId);
                            return; // 直接退出线程
                        }

                        // 检查队列中是否有任务
                        if (channel.QueueTasks.TryPeek(out var queueTask))
                        {
                            var preSleep = _account.Interval;
                            if (preSleep <= 1.2m)
                            {
                                preSleep = 1.2m;
                            }

                            // 提交任务前间隔
                            // 当一个作业完成后，是否先等待一段时间再提交下一个作业
                            Thread.Sleep((int)(preSleep * 1000));

                            // 从队列中移除任务，并开始执行
                            if (channel.QueueTasks.TryDequeue(out var info))
                            {
                                try
                                {
                                    // 使用原有的ExecuteTaskAsync方法执行任务
                                    channel.TaskFutureMap[info.Item1.Id] = Task.Run(async () =>
                                    {
                                        await ExecuteTaskAsync(channel, info.Item1, info.Item2);
                                    });

                                    // 计算执行后的间隔
                                    var min = _account.AfterIntervalMin;
                                    var max = _account.AfterIntervalMax;

                                    // 计算 min ~ max随机数
                                    var afterInterval = 1200;
                                    if (max > min && min >= 1.2m)
                                    {
                                        afterInterval = new Random().Next((int)(min * 1000), (int)(max * 1000));
                                    }
                                    else if (max == min && min >= 1.2m)
                                    {
                                        afterInterval = (int)(min * 1000);
                                    }

                                    // 如果是图生文操作
                                    if (info.Item1.GetProperty<string>(Constants.TASK_PROPERTY_CUSTOM_ID, default)?.Contains("PicReader") == true)
                                    {
                                        // 批量任务操作提交间隔 1.2s + 6.8s
                                        Thread.Sleep(afterInterval + 6800);
                                    }
                                    else
                                    {
                                        // 队列提交间隔
                                        Thread.Sleep(afterInterval);
                                    }
                                }
                                catch (ObjectDisposedException)
                                {
                                    _logger.Warning("频道 {0} 资源已释放，无法执行任务，处理线程退出", channel.ChannelId);
                                    return; // 直接退出线程
                                }
                            }
                        }
                        else
                        {
                            // 如果队列为空，重置信号
                            try
                            {
                                channel.SignalControl.Reset();
                                Thread.Sleep(100);
                            }
                            catch (ObjectDisposedException)
                            {
                                _logger.Warning("频道 {0} 的SignalControl已释放，任务处理线程退出", channel.ChannelId);
                                return; // 直接退出线程
                            }
                        }
                    }
                    catch (ObjectDisposedException ex)
                    {
                        _logger.Warning(ex, "频道 {0} 资源已释放，任务处理线程退出", channel.ChannelId);
                        return; // 直接退出线程
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "频道 {0} 任务处理线程异常", channel.ChannelId);
                        // 发生异常时等待 3 秒再继续
                        Thread.Sleep(3000);
                    }
                }

                _logger.Information("频道 {0} 任务处理线程结束", channel.ChannelId);
            }, _longToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// 手动刷新频道池
        /// </summary>
        /// <returns>刷新结果</returns>
        public bool RefreshChannelPool()
        {
            try
            {
                _logger.Information("手动刷新频道池开始");

                // 从数据库获取最新的账号信息
                var latestAccount = DbHelper.Instance.AccountStore.Get(_account.Id);
                if (latestAccount != null)
                {
                    // 更新账号信息
                    _instance.ClearAccountCache(_account.Id);

                    // 准备频道ID列表
                    var newChannelIds = new List<string>();

                    // 是否开启多频道
                    if (latestAccount.ChannelIds != null && latestAccount.EnableAutoFetchChannels)
                    {
                        newChannelIds.AddRange(latestAccount.ChannelIds);
                    }

                    // 确保主频道始终存在
                    if (!newChannelIds.Contains(_account.ChannelId))
                    {
                        newChannelIds.Add(_account.ChannelId);
                    }

                    // 更新频道池
                    UpdateChannelPool(newChannelIds);

                    _logger.Information("手动刷新频道池完成");
                    return true;
                }


                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "手动刷新频道池异常");
                return false;
            }
        }

        /// <summary>
        /// 获取可用频道ID
        /// </summary>
        /// <param name="preferredChannelId">优先使用的频道ID，如果为空则自动选择</param>
        /// <returns>可用的频道ID</returns>
        public DiscordChannel SelectChannel(string preferredChannelId = null)
        {
            // 如果指定了优先频道ID并且该频道存在，优先使用该频道
            if (!string.IsNullOrWhiteSpace(preferredChannelId) && 
                _channelPool.TryGetValue(preferredChannelId, out var preferredChannel))
            {
                // 记录选择结果
                LogChannelSelection(preferredChannel.ChannelId, true);
                return preferredChannel;
            }

            // 使用选择策略获取频道ID
            var selectedChannel = _selectionStrategy.SelectChannel(_channelPool, preferredChannelId);
            
            // 如果策略未能选择频道且有可用的频道池，则选择队列最短的频道
            if (selectedChannel == null && _channelPool.Count > 0)
            {
                selectedChannel = _channelPool.Values
                    .OrderBy(c => c.QueueTasks.Count)
                    .FirstOrDefault();
                
                _logger.Information("策略未能选择频道，选择队列最短的频道: {0}", selectedChannel?.ChannelId);
            }

            // 如果找不到指定频道，根据需求是否报错
            // if (selectedChannel == null)
            // {
            //     throw new LogicException("没有可用的频道");
            // }

            // 记录选择结果
            LogChannelSelection(selectedChannel?.ChannelId, preferredChannelId != null && selectedChannel?.ChannelId == preferredChannelId);

            return selectedChannel;
        }

        /// <summary>
        /// 检查是否有可用的频道资源（包括队列资源）
        /// </summary>
        /// <param name="channelId">指定的频道ID，如果为空则检查所有频道</param>
        /// <returns>是否有可用的频道资源</returns>
        public bool HasAvailableResource(string channelId = null)
        {
            // 如果指定了频道ID，只检查该频道
            if (!string.IsNullOrWhiteSpace(channelId))
            {
                if (_channelPool.TryGetValue(channelId, out var channel))
                {
                    bool hasResource = channel.IsIdleQueue;
                    string status = hasResource ? "有空闲队列" : "队列已满";
                    _logger.Debug("检查频道 {0} 资源: {1}, 运行任务数: {2}, 队列任务数: {3}/{4}",
                        channelId, status, channel.RunningTasks.Count, channel.QueueTasks.Count, channel.QueueSize);
                    return hasResource;
                }
                return false;
            }

            // 检查所有频道是否有任一频道队列未满
            var anyAvailable = _channelPool.Values.Any(c => c.IsIdleQueue);
            
            if (!anyAvailable)
            {
                _logger.Debug("所有频道队列均已满，无法添加新任务");
                // 记录所有频道状态
                foreach (var channel in _channelPool.Values)
                {
                    _logger.Debug("频道 {0} 状态: 运行任务数: {1}, 队列任务数: {2}/{3}",
                        channel.ChannelId, channel.RunningTasks.Count, channel.QueueTasks.Count, channel.QueueSize);
                }
            }
            
            return anyAvailable;
        }

        /// <summary>
        /// 记录频道选择日志
        /// </summary>
        private void LogChannelSelection(string channelId, bool isPreferred)
        {
            if (channelId == null)
            {
                _logger.Warning("无法选择合适的频道，所有频道可能队列已满");
                return;
            }

            if (_channelPool.TryGetValue(channelId, out var channel))
            {
                _logger.Information(
                    "频道选择结果 - 服务器: {GuildId}, 是否优先频道: {IsPreferred}, 最终频道: {ChannelId}, 运行任务数: {Running}, 队列任务数: {Queue}/{MaxQueue}",
                    _account.GuildId, isPreferred, channelId, channel.RunningTasks.Count, channel.QueueTasks.Count, channel.QueueSize
                );
            }
            else
            {
                _logger.Information(
                    "频道选择结果 - 服务器: {GuildId}, 是否优先频道: {IsPreferred}, 最终频道: {ChannelId}",
                    _account.GuildId, isPreferred, channelId
                );
            }
        }

        /// <summary>
        /// 更新频道池
        /// </summary>
        /// <param name="newChannelIds">新的频道ID列表</param>
        public void UpdateChannelPool(List<string> newChannelIds)
        {
            if (newChannelIds == null)
                return;

            _logger.Information("开始更新频道池，当前频道列表：{@0}，新频道列表：{@1}", string.Join(",", _channelPool.Keys), string.Join(",", newChannelIds));

            // 找出需要添加的频道
            var channelsToAdd = newChannelIds.Where(id => !string.IsNullOrWhiteSpace(id) && !_channelPool.ContainsKey(id)).ToList();

            // 找出需要移除的频道
            var channelsToRemove = _channelPool.Keys.Where(id => !newChannelIds.Contains(id)).ToList();

            // 处理需要移除的频道
            foreach (var channelId in channelsToRemove)
            {
                RemoveChannel(channelId);
            }

            // 处理需要添加的频道
            foreach (var channelId in channelsToAdd)
            {
                AddChannel(channelId);
            }

            _logger.Information("频道池更新完成，当前频道列表：{@0}", string.Join(",", _channelPool.Keys));
        }

        /// <summary>
        /// 添加新频道到频道池
        /// </summary>
        /// <param name="channelId">频道ID</param>
        /// <param name="channelName">频道名称，可选</param>
        public void AddChannel(string channelId, string channelName = null)
        {
            if (string.IsNullOrWhiteSpace(channelId) || _channelPool.ContainsKey(channelId))
                return;

            _logger.Information("添加新频道：{0}", channelId);

            // 使用现有的InitChannel方法初始化频道
            InitChannel(channelId, channelName);

            // 获取新添加的频道
            if (_channelPool.TryGetValue(channelId, out var channel))
            {
                // 启动该频道的任务处理线程
                StartChannelTaskProcessing(channel);

                _logger.Information("新频道 {0} 添加完成并启动任务处理线程", channelId);
            }
        }

        /// <summary>
        /// 从频道池移除频道
        /// </summary>
        /// <param name="channelId">频道ID</param>
        public void RemoveChannel(string channelId)
        {
            if (string.IsNullOrWhiteSpace(channelId))
                return;

            _logger.Information("开始移除频道：{0}", channelId);

            // 尝试从频道池中获取并移除频道
            if (_channelPool.TryRemove(channelId, out var channel))
            {
                try
                {
                    // 使用频道的CancelAndClearAllTasks方法取消和清理所有任务
                    channel.CancelAndClearAllTasks("频道已移除");
                    
                    // 触发所有任务的事件通知
                    foreach (var task in channel.GetAllTasks())
                    {
                        _taskChangeEvent?.InvokeAsync(task);
                    }

                    // 释放资源
                    channel.Dispose();

                    _logger.Information("频道 {0} 已成功移除", channelId);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "移除频道 {0} 时发生错误", channelId);
                }
            }
        }

        /// <summary>
        /// 获取频道池中的所有频道
        /// </summary>
        public IEnumerable<DiscordChannel> GetAllChannels()
        {
            return _channelPool.Values;
        }

        /// <summary>
        /// 检查是否有空闲的频道
        /// </summary>
        /// <param name="channelId">指定检查的频道ID，为空则检查所有频道</param>
        /// <returns>是否有空闲的频道</returns>
        public bool IsIdleQueue(string channelId = null)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                // 如果有任何一个频道有空闲队列，就返回true
                // 这里不再要求RunningTasks.IsEmpty - 因为即使频道有运行中任务，但队列未满也应该可用
                return _channelPool.Values.Any(c => c.IsIdleQueue);
            }
            else
            {
                var channel = SelectChannel(channelId);
                // 同样，这里只检查队列是否有空间，不再检查运行中任务
                return channel?.IsIdleQueue == true;
            }
        }

        /// <summary>
        /// 异步执行任务。
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="info">任务信息</param>
        /// <param name="discordSubmit">Discord提交任务的委托</param>
        /// <returns>异步任务</returns>
        private async Task ExecuteTaskAsync(DiscordChannel channel, TaskInfo info, Func<Task<Message>> discordSubmit)
        {
            // 创建任务取消令牌
            var cts = new CancellationTokenSource();
            channel.TaskCancellationTokens.TryAdd(info.Id, cts);
            
            bool lockAcquired = false;
            try
            {
                // 标记任务添加到运行列表
                channel.RunningTasks.TryAdd(info.Id, info);
                
                // 获取信号量锁
                await channel.SemaphoreSlimLock.LockAsync();
                lockAcquired = true;
                
                // 记录当前任务持有锁的状态
                channel.TaskHoldingLock.TryAdd(info.Id, true);

                // 如果任务已被取消，则提前退出
                if (cts.Token.IsCancellationRequested)
                {
                    info.Fail("任务已被取消");
                    await _taskChangeEvent.InvokeAsync(info);
                    return;
                }

                // 判断当前实例是否可用，尝试最大等待 30s
                var waitTime = 0;
                while (!_instance.IsAlive)
                {
                    // 检查取消令牌
                    if (cts.Token.IsCancellationRequested)
                    {
                        info.Fail("任务已被取消");
                        await _taskChangeEvent.InvokeAsync(info);
                        return;
                    }
                    
                    // 等待 1s
                    await Task.Delay(1000, cts.Token);

                    // 计算等待时间
                    waitTime += 1000;
                    if (waitTime > 30 * 1000)
                    {
                        break;
                    }
                }

                // 判断当前实例是否可用
                if (!_instance.IsAlive)
                {
                    _logger.Debug("[{@0}] task error, id: {@1}, status: {@2}", info.ChannelId, info.Id, info.Status);

                    info.Fail("实例不可用");
                    await _taskChangeEvent.InvokeAsync(info);
                    return;
                }
                info.Status = TaskStatus.SUBMITTED;
                info.Progress = "0%";
                
                await _taskChangeEvent.InvokeAsync(info);

                // 检查取消令牌
                if (cts.Token.IsCancellationRequested)
                {
                    info.Fail("任务已被取消");
                    await _taskChangeEvent.InvokeAsync(info);
                    return;
                }

                // 执行实际的任务提交方法
                var result = await discordSubmit();

                // 检查取消令牌
                if (cts.Token.IsCancellationRequested)
                {
                    info.Fail("任务已被取消");
                    await _taskChangeEvent.InvokeAsync(info);
                    return;
                }

                // 判断当前实例是否可用
                if (!_instance.IsAlive)
                {
                    _logger.Debug("[{@0}] task error, id: {@1}, status: {@2}", info.ChannelId, info.Id, info.Status);

                    info.Fail("实例不可用");
                    await _taskChangeEvent.InvokeAsync(info);
                    return;
                }

                info.StartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                if (result.Code != ReturnCode.SUCCESS)
                {
                    _logger.Debug("[{@0}] task finished, id: {@1}, status: {@2}", info.ChannelId, info.Id, info.Status);

                    info.Fail(result.Description);
                    await _taskChangeEvent.InvokeAsync(info);
                    return;
                }

                if (info.Status != TaskStatus.FAILURE && info.Status != TaskStatus.SUCCESS)
                {
                    info.Status = TaskStatus.SUBMITTED;
                    info.Progress = "0%";
                }

                await Task.Delay(500, cts.Token);

                await _taskChangeEvent.InvokeAsync(info);

                // 超时处理
                var timeoutMin = _account.TimeoutMinutes;
                var sw = new Stopwatch();
                sw.Start();

                while (info.Status == TaskStatus.SUBMITTED || info.Status == TaskStatus.IN_PROGRESS)
                {
                    // 如果是悠船任务，则每 2s 获取一次
                    if (YmTaskService != null && (info.IsPartner || info.IsOfficial))
                    {
                        await YmTaskService.UpdateStatus(info);
                        await Task.Delay(1000);
                    }
                    // 检查取消令牌
                    if (cts.Token.IsCancellationRequested)
                    {
                        info.Fail("任务已被取消");
                        await _taskChangeEvent.InvokeAsync(info);
                        return;
                    }
                    
                    await _taskChangeEvent.InvokeAsync(info);

                    // 每 500ms
                    await Task.Delay(500, cts.Token);

                    if (sw.ElapsedMilliseconds > timeoutMin * 60 * 1000)
                    {
                        info.Fail($"执行超时 {timeoutMin} 分钟");
                        await _taskChangeEvent.InvokeAsync(info);
                        return;
                    }
                }

                // 任务完成后，自动读消息
                try
                {
                    // 成功才都消息
                    if (info.Status == TaskStatus.SUCCESS && !info.IsPartner && !info.IsOfficial)
                    {
                        var res = await _instance.ReadMessageAsync(info.MessageId, info.ChannelId);
                        if (res.Code == ReturnCode.SUCCESS)
                        {
                            _logger.Debug("自动读消息成功 {@0} - {@1}", info.ChannelId, info.Id);
                        }
                        else
                        {
                            _logger.Warning("自动读消息失败 {@0} - {@1}", info.ChannelId, info.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "自动读消息异常 {@0} - {@1}", info.ChannelId, info.Id);
                }

                // 任务完成后，检查是否需要自动删除消息
                try
                {
                    // 成功且是Imagine任务时尝试删除消息, 且账号设置了自动删除消息
                    if (info.Status == TaskStatus.SUCCESS &&
                        !string.IsNullOrWhiteSpace(info.MessageId) &&
                        info.Action == TaskAction.IMAGINE &&
                        _account.AutoDeleteMessages)
                    {
                        // 延迟1秒后删除，确保消息已被完全处理
                        await Task.Delay(1000, cts.Token);

                        // 使用DeleteMessageAsync方法删除消息
                        var deleteResult = await _instance.DeleteMessageAsync(info.MessageId, info.ChannelId);
                        if (deleteResult.Code == ReturnCode.SUCCESS)
                        {
                            _logger.Information("自动删除消息成功 ChannelId: {@0} - TaskId: {@1} - MessageId: {@2}",
                                info.ChannelId, info.Id, info.MessageId);
                        }
                        else
                        {
                            _logger.Warning("自动删除消息失败 ChannelId: {@0} - TaskId: {@1} - MessageId: {@2}: {3}",
                                info.ChannelId, info.Id, info.MessageId, deleteResult.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "自动删除消息异常 ChannelId: {@0} - TaskId: {@1} - MessageId: {@2}", info.ChannelId, info.Id, info.MessageId);
                }

                _logger.Debug("[{AccountDisplay}] task finished, id: {TaskId}, status: {TaskStatus}", info.ChannelId, info.Id, info.Status);

                await _taskChangeEvent.InvokeAsync(info);
            }
            catch (OperationCanceledException)
            {
                // 任务被取消的处理
                _logger.Information("[{@0}] 任务已被取消, id: {@1}", info.ChannelId, info.Id);
                info.Fail("任务已被取消");
                await _taskChangeEvent.InvokeAsync(info);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[{AccountDisplay}] task execute error, id: {TaskId}", info.ChannelId, info.Id);

                info.Fail("[Internal Server Error] " + ex.Message);

                await _taskChangeEvent.InvokeAsync(info);
            }
            finally
            {
                // 清理任务资源
                channel.TaskCancellationTokens.TryRemove(info.Id, out _);
                channel.RunningTasks.TryRemove(info.Id, out _);
                channel.TaskFutureMap.TryRemove(info.Id, out _);
                
                // 更新锁状态并释放锁
                if (lockAcquired)
                {
                    channel.TaskHoldingLock.TryRemove(info.Id, out _);
                    channel.SemaphoreSlimLock.Unlock();
                }

                await _taskChangeEvent.InvokeAsync(info);
                
                // 处理令牌资源
                try
                {
                    cts.Dispose();
                }
                catch
                {
                    // 忽略释放异常
                }
            }
        }

        /// <summary>
        /// 更新所有频道的并发数
        /// </summary>
        /// <param name="maxConcurrency">最大并发数，默认为1</param>
        /// <param name="forceUpdate">是否强制更新，忽略自动获取频道设置</param>
        /// <returns>更新成功的频道数</returns>
        public int UpdateChannelConcurrency(int maxConcurrency = 1, bool forceUpdate = false)
        {
            int successCount = 0;

            if (maxConcurrency < 1)
                maxConcurrency = 1;

            // 如果启用了自动获取频道并且不是强制更新，则所有频道并发数都为1
            if (_account.EnableAutoFetchChannels && !forceUpdate && (_account.ChannelIds?.Count > 0 || _channelPool.Count > 1))
            {
                maxConcurrency = 1;
            }

            _logger.Information("开始更新所有频道并发数为: {0}", maxConcurrency);

            foreach (var channel in _channelPool.Values)
            {
                var oldMax = channel.SemaphoreSlimLock.MaxParallelism;
                if (channel.SemaphoreSlimLock.SetMaxParallelism(maxConcurrency))
                {
                    _logger.Information("频道 {0} 并发数更新成功: {1} -> {2}",
                        channel.ChannelId, oldMax, maxConcurrency);
                    successCount++;
                }
                else
                {
                    _logger.Warning("频道 {0} 并发数更新失败: {1} -> {2}",
                        channel.ChannelId, oldMax, maxConcurrency);
                }
            }

            _logger.Information("频道并发数更新完成，成功更新 {0}/{1} 个频道",
                successCount, _channelPool.Count);

            return successCount;
        }

        /// <summary>
        /// 设置指定频道的并发数
        /// </summary>
        /// <param name="channelId">频道ID</param>
        /// <param name="maxConcurrency">最大并发数</param>
        /// <param name="forceUpdate">是否强制更新，忽略自动获取频道设置</param>
        /// <returns>是否更新成功</returns>
        public bool SetChannelConcurrency(string channelId, int maxConcurrency = 1, bool forceUpdate = false)
        {
            if (string.IsNullOrWhiteSpace(channelId) || !_channelPool.TryGetValue(channelId, out var channel))
                return false;

            if (maxConcurrency < 1)
                maxConcurrency = 1;

            // 如果启用了自动获取频道并且不是强制更新，则所有频道并发数都为1
            if (_account.EnableAutoFetchChannels && !forceUpdate && (_account.ChannelIds?.Count > 0 || _channelPool.Count > 1))
            {
                maxConcurrency = 1;
            }

            var oldMax = channel.SemaphoreSlimLock.MaxParallelism;
            if (channel.SemaphoreSlimLock.SetMaxParallelism(maxConcurrency))
            {
                _logger.Information("频道 {0} 并发数设置成功: {1} -> {2}",
                    channelId, oldMax, maxConcurrency);
                return true;
            }

            _logger.Warning("频道 {0} 并发数设置失败: {1} -> {2}",
                channelId, oldMax, maxConcurrency);
            return false;
        }

        public void Dispose()
        {
            try
            {
                // 取消长时间运行的任务
                _longToken.Cancel();
                
                // 清理频道资源
                foreach (var channel in _channelPool.Values)
                {
                    try
                    {
                        // 使用频道的CancelAndClearAllTasks方法取消和清理所有任务
                        channel.CancelAndClearAllTasks("实例已关闭");
                        
                        // 触发所有任务的事件通知
                        foreach (var task in channel.GetAllTasks())
                        {
                            _taskChangeEvent?.InvokeAsync(task);
                        }

                        // 释放资源
                        channel.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "清理频道 {0} 资源时发生异常", channel.ChannelId);
                    }
                }
                
                // 清空频道池
                _channelPool.Clear();
                
                // 释放长时间运行的任务令牌
                _longToken.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "释放ChannelPoolManager资源时发生异常");
            }
        }
    }
}