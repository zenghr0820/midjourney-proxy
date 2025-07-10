using Midjourney.Infrastructure.LoadBalancer;

namespace Midjourney.Infrastructure.Services
{
    /// <summary>
    /// Discord 负载均衡器。
    /// </summary>
    public class DiscordLoadBalancer
    {
        private readonly IRule _rule;
        private readonly HashSet<DiscordInstance> _instances = [];

        public DiscordLoadBalancer(IRule rule)
        {
            _rule = rule;
        }

        /// <summary>
        /// 获取所有实例。
        /// </summary>
        /// <returns>所有实例列表。</returns>
        public List<DiscordInstance> GetAllInstances() => _instances.ToList();

        /// <summary>
        /// 获取存活的实例。
        /// </summary>
        /// <returns>存活的实例列表。</returns>
        public List<DiscordInstance> GetAliveInstances() =>
            _instances.Where(c => c is { IsAlive: true }).ToList() ?? [];

        /// <summary>
        /// 选择一个实例。
        /// </summary>
        /// <returns>选择的实例。</returns>
        /// <param name="accountFilter">账号过滤条件</param>
        /// <param name="isNewTask">是否过滤只接收新任务的实例</param>
        /// <param name="botType">过滤开启指定机器人的账号</param>
        /// <param name="blend">过滤支持 Blend 的账号</param>
        /// <param name="describe">过滤支持 Describe 的账号</param>
        /// <param name="isDomain">过滤垂直领域的账号</param>
        /// <param name="domainIds">过滤垂直领域 ID</param>
        /// <param name="ids">指定 ids 账号</param>
        /// <param name="shorten">过滤支持 Shorten 的账号</param>
        /// <param name="channelId">指定频道 ID</param>
        /// <param name="speedMode"></param>
        public DiscordInstance ChooseInstance(
            AccountFilter accountFilter = null,
            bool? isNewTask = null,
            EBotType? botType = null,
            bool? blend = null,
            bool? describe = null,
            bool? isDomain = null,
            List<string> domainIds = null,
            List<string> ids = null,
            bool? shorten = null,
            string channelId = null,
            GenerationSpeedMode? speedMode = null)
        {
            // 如果指定了频道ID，先尝试查找含有该频道的实例
            if (!string.IsNullOrWhiteSpace(channelId))
            {
                var instanceWithChannel = GetAliveInstances()
                    .FirstOrDefault(instance => instance.AllChannelIds.Contains(channelId));

                if (instanceWithChannel != null)
                {
                    return instanceWithChannel;
                }
            }

            var list = GetAliveInstances()

                // 过滤有空闲队列的实例
                .Where(c => c.IsIdleQueue(channelId))

                // 允许继续绘图
                .Where(c => c.Account.IsContinueDrawing && c.Account.Enable == true)

                // 悠船绘图判断
                .Where(c => c.Account.IsYouChuanContinueDrawing(speedMode))

                // 指定 ID 的实例
                .WhereIf(!string.IsNullOrWhiteSpace(accountFilter.InstanceId), c => c.GuildId == accountFilter.InstanceId)

                // 指定速度模式过滤
                .WhereIf(accountFilter?.Modes.Count > 0,
                    c => c.Account.Mode == null || accountFilter.Modes.Contains(c.Account.Mode.Value))

                // 允许速度模式过滤
                // 或者有交集的
                .WhereIf(accountFilter?.Modes.Count > 0,
                    c => c.Account.AllowModes == null || c.Account.AllowModes.Count <= 0 ||
                         c.Account.AllowModes.Any(x => accountFilter.Modes.Contains(x)))

                // 如果速度模式中，包含快速模式，则过滤掉不支持快速模式的实例
                .WhereIf(accountFilter?.Modes.Contains(GenerationSpeedMode.FAST) == true ||
                         accountFilter?.Modes.Contains(GenerationSpeedMode.TURBO) == true,
                    c => c.Account.FastExhausted == false)

                // Discord 绘图判断
                .WhereIf(accountFilter?.Modes.Count > 0, c => c.Account.IsDiscordContinueDrawing(accountFilter.Modes.ToArray()))

                // Midjourney Remix 过滤
                .WhereIf(accountFilter?.Remix == true,
                    c => c.Account.MjRemixOn == accountFilter.Remix || !c.Account.RemixAutoSubmit)
                .WhereIf(accountFilter?.Remix == false, c => c.Account.MjRemixOn == accountFilter.Remix)

                // Niji Remix 过滤
                .WhereIf(accountFilter?.NijiRemix == true,
                    c => c.Account.NijiRemixOn == accountFilter.NijiRemix || !c.Account.RemixAutoSubmit)
                .WhereIf(accountFilter?.NijiRemix == false, c => c.Account.NijiRemixOn == accountFilter.NijiRemix)

                // Remix 自动提交过滤
                .WhereIf(accountFilter?.RemixAutoConsidered.HasValue == true,
                    c => c.Account.RemixAutoSubmit == accountFilter.RemixAutoConsidered)

                // 过滤只接收新任务的实例
                .WhereIf(isNewTask == true, c => c.Account.IsAcceptNewTask == true)

                // 过滤开启 niji mj 的账号
                .WhereIf(botType == EBotType.NIJI_JOURNEY, c => c.Account.EnableNiji == true)
                .WhereIf(botType == EBotType.MID_JOURNEY, c => c.Account.EnableMj == true)

                // 过滤开启功能的账号
                .WhereIf(blend == true, c => c.Account.IsBlend)
                .WhereIf(describe == true, c => c.Account.IsDescribe)
                .WhereIf(shorten == true, c => c.Account.IsShorten)

                // 领域过滤
                .WhereIf(isDomain == true && domainIds?.Count > 0,
                    c => c.Account.IsVerticalDomain && c.Account.VerticalDomainIds.Any(x => domainIds.Contains(x)))
                .WhereIf(isDomain == false, c => c.Account.IsVerticalDomain != true)

                // 过滤指定账号
                .WhereIf(ids?.Count > 0, c =>
                {
                    var isContains = false;
                    foreach (var cId in c.AllChannelIds)
                    {
                        if (ids.Contains(cId))
                        {
                            isContains = true;
                            break;
                        }
                    }

                    return isContains;
                })

                // 如果指定了频道ID，则过滤包含该频道的实例
                .WhereIf(!string.IsNullOrWhiteSpace(channelId), c => c.AllChannelIds.Contains(channelId))
                .ToList();

            return _rule.Choose(list);
        }

        /// <summary>
        /// 获取指定ID的实例（不判断是否存活）
        /// </summary>
        /// <param name="instanceId">实例ID/渠道ID</param>
        /// <returns>实例。</returns>
        public DiscordInstance GetDiscordInstance(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return null;
            }

            // 先检查是否有实例的主频道ID匹配
            var instance = _instances.FirstOrDefault(c => c.GuildId == instanceId);
            if (instance != null)
            {
                return instance;
            }

            // 如果没有找到，检查是否有实例包含此频道ID
            // return _instances.FirstOrDefault(c => c.AllChannelIds.Contains(channelId));
            return null;
        }

        /// <summary>
        /// 获取指定ID的实例（必须是存活的）
        /// </summary>
        /// <param name="instanceId">实例ID/渠道ID</param>
        /// <returns>实例。</returns>
        public DiscordInstance GetDiscordInstanceIsAlive(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return null;
            }

            // 先检查是否有存活实例的主频道ID匹配
            var instance = _instances.FirstOrDefault(c => c.GuildId == instanceId && c.IsAlive);
            return instance;
            // // 如果没有找到，检查是否有存活实例包含此频道ID
            // return _instances.FirstOrDefault(c => c.AllChannelIds.Contains(instanceId) && c.IsAlive);
        }

        /// <summary>
        /// 获取指定频道ID的实例 
        /// </summary>
        /// <param name="channelId">频道ID</param>
        /// <param name="isAlive">是否存活</param>
        /// <returns>实例。</returns>
        public DiscordInstance GetDiscordInstanceByChannelId(string channelId, bool isAlive = false)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                return null;
            }

            DiscordInstance instance = null;
            // 先检查是否有存活实例的主频道ID匹配
            if (isAlive)
            {
                instance = _instances.FirstOrDefault(c => c.ChannelId == channelId && c.IsAlive);
                // 如果没有找到，检查是否有存活实例包含此频道ID
                instance ??= _instances.FirstOrDefault(c => c.AllChannelIds.Contains(channelId) && c.IsAlive);
            }
            else
            {
                instance = _instances.FirstOrDefault(c => c.ChannelId == channelId);
                instance ??= _instances.FirstOrDefault(c => c.AllChannelIds.Contains(channelId));
            }

            return instance;
        }

        // /// <summary>
        // /// 获取排队任务的ID集合。
        // /// </summary>
        // /// <returns>排队任务的ID集合。</returns>
        // public HashSet<string> GetQueueTaskIds()
        // {
        //     var taskIds = new HashSet<string>();
        //     foreach (var instance in GetAliveInstances())
        //     {
        //         foreach (var taskId in instance.GetRunningFutures().Keys)
        //         {
        //             taskIds.Add(taskId);
        //         }
        //     }
        //     return taskIds;
        // }

        /// <summary>
        /// 获取排队任务列表。
        /// </summary>
        /// <returns>排队任务列表。</returns>
        public List<TaskInfo> GetQueueTasks()
        {
            var tasks = new List<TaskInfo>();

            var ins = GetAliveInstances();
            if (ins?.Count > 0)
            {
                foreach (var instance in ins)
                {
                    var ts = instance.GetQueueTasks();
                    if (ts?.Count > 0)
                    {
                        tasks.AddRange(ts);
                    }
                }
            }

            return tasks;
        }

        /// <summary>
        /// 获取执行中的任务列表
        /// </summary>
        /// <returns></returns>
        public List<TaskInfo> GetRunningTasks()
        {
            var tasks = new List<TaskInfo>();
            var ins = GetAliveInstances();
            if (ins?.Count > 0)
            {
                foreach (var instance in ins)
                {
                    var ts = instance.GetRunningTasks();
                    if (ts?.Count > 0)
                    {
                        tasks.AddRange(ts);
                    }
                }
            }
            return tasks;
        }

        /// <summary>
        /// 添加 Discord 实例
        /// </summary>
        /// <param name="instance"></param>
        public void AddInstance(DiscordInstance instance) => _instances.Add(instance);

        /// <summary>
        /// 移除
        /// </summary>
        /// <param name="instance"></param>
        public void RemoveInstance(DiscordInstance instance) => _instances.Remove(instance);

        /// <summary>
        /// 根据任务获取所属的Discord实例
        /// </summary>
        /// <param name="task">任务信息</param>
        /// <returns>Discord实例</returns>
        public DiscordInstance GetInstanceByTask(TaskInfo task)
        {
            if (task == null)
            {
                return null;
            }

            // 根据任务的InstanceId获取实例
            if (!string.IsNullOrWhiteSpace(task.InstanceId))
            {
                var instance = GetDiscordInstanceIsAlive(task.InstanceId);
                if (instance != null && instance.IsAlive)
                {
                    return instance;
                }
            }

            // 根据任务的ChannelId获取实例
            if (!string.IsNullOrWhiteSpace(task.ChannelId))
            {
                var instance = GetDiscordInstanceByChannelId(task.ChannelId, true);
                if (instance != null)
                {
                    return instance;
                }
            }

            return null;
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="isCancelRunning">是否取消运行中的任务</param>
        public void CancelTask(string taskId, bool isCancelRunning = false)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                throw new LogicException("任务ID不能为空");
            }

            // 获取任务信息
            var dbTask = DbHelper.Instance.TaskStore.Get(taskId);
            if (dbTask == null || dbTask.Status == TaskStatus.FAILURE || dbTask.Status == TaskStatus.SUCCESS)
            {
                throw new LogicException("任务已完成或已失败");
            }

            // 尝试找到可能处理这个任务的实例
            var instance = GetInstanceByTask(dbTask);
            if (instance == null)
            {
                throw new LogicException($"未找到任务对应的实例");
            }
            instance.RemoveTask(dbTask, "主动取消任务");
        }
    }
}