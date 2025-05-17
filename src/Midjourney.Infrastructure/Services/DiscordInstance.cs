using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.Services;
using Midjourney.Infrastructure.Storage;
using Midjourney.Infrastructure.Util;
using Midjourney.Infrastructure.Wss;
using Midjourney.Infrastructure.Wss.Gateway;
using Newtonsoft.Json.Linq;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Midjourney.Infrastructure.LoadBalancer
{
    /// <summary>
    /// Discord 实例
    /// 实现了IDiscordInstance接口，负责处理Discord相关的任务管理和执行。
    /// </summary>
    public class DiscordInstance
    {
        private readonly object _lockAccount = new object();

        private readonly ILogger _logger = Log.Logger;

        private readonly ITaskStoreService _taskStoreService;
        private readonly INotifyService _notifyService;


        private readonly CancellationTokenSource _longToken;
        private readonly Task _longTaskCache;

        private readonly HttpClient _httpClient;
        private readonly DiscordHelper _discordHelper;
        private readonly Dictionary<string, string> _paramsMap;

        private readonly string _discordInteractionUrl;
        private readonly IMemoryCache _cache;
        private readonly ITaskService _taskService;

        /// <summary>
        /// Discord频道池
        /// </summary>
        private readonly ChannelPoolManager _channelPoolManager;

        public ChannelPoolManager ChannelPool => _channelPoolManager;

        private DiscordAccount _account;

        /// <summary>
        /// 获取Discord账号信息。
        /// </summary>
        /// <returns>Discord账号</returns>
        public DiscordAccount Account
        {
            get
            {
                try
                {
                    lock (_lockAccount)
                    {
                        if (!string.IsNullOrWhiteSpace(_account?.Id))
                        {
                            _account = _cache.GetOrCreate($"account:{_account.Id}", (c) =>
                            {
                                c.SetAbsoluteExpiration(TimeSpan.FromMinutes(2));

                                // 必须数据库中存在
                                var acc = DbHelper.Instance.AccountStore.Get(_account.Id);
                                if (acc != null)
                                {
                                    return acc;
                                }

                                // 如果账号被删除了
                                IsInit = false;

                                return _account;
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to get account. {@0}", _account?.Id ?? "unknown");
                }

                return _account;
            }
        }

        /// <summary>
        /// 默认会话ID。
        /// </summary>
        public string DefaultSessionId { get; set; } = "f1a313a09ce079ce252459dc70231f30";

        /// <summary>
        /// 获取实例ID = 服务器ID
        /// </summary>
        /// <returns>实例ID</returns>
        public string GuildId => Account.GuildId;

        /// <summary>
        /// 获取频道ID
        /// </summary>
        /// <returns>频道ID</returns>
        public string ChannelId => Account.ChannelId;

        /// <summary>
        /// 获取所有频道ID
        /// </summary>
        public List<string> AllChannelIds => _channelPoolManager.Channels.Select(c => c.ChannelId).ToList();

        /// <summary>
        /// 是否已初始化完成
        /// </summary>
        public bool IsInit { get; set; }

        /// <summary>
        /// 判断实例是否存活
        /// </summary>
        /// <returns>是否存活</returns>
        public bool IsAlive => IsInit && Account != null
             && Account.Enable != null && Account.Enable == true
             && WebSocketStarter != null
             && WebSocketStarter.IsRunning == true
             && Account.Lock == false;

        public IWebSocketStarter WebSocketStarter { get; set; }

        public DiscordInstance(
            IMemoryCache memoryCache,
            DiscordAccount account,
            ITaskStoreService taskStoreService,
            INotifyService notifyService,
            DiscordHelper discordHelper,
            Dictionary<string, string> paramsMap,
            IWebProxy webProxy,
            ITaskService taskService)
        {
            _logger = Log.Logger.ForContext("LogPrefix", $"{account.GuildId} - instance");

            var hch = new HttpClientHandler
            {
                UseProxy = webProxy != null,
                Proxy = webProxy
            };

            _httpClient = new HttpClient(hch)
            {
                Timeout = TimeSpan.FromMinutes(10),
            };

            _taskService = taskService;
            _cache = memoryCache;
            _paramsMap = paramsMap;
            _discordHelper = discordHelper;

            _account = account;
            _taskStoreService = taskStoreService;
            _notifyService = notifyService;

            // 初始化频道池管理器
            _channelPoolManager = new ChannelPoolManager(this);
            // 订阅事件
            _channelPoolManager.TaskChangeEvent += SaveAndNotify;

            var discordServer = _discordHelper.GetServer();
            _discordInteractionUrl = $"{discordServer}/api/v9/interactions";

            // 后台任务取消 token
            _longToken = new CancellationTokenSource();

            // 启动缓存处理任务
            _longTaskCache = new Task(RuningCache, _longToken.Token, TaskCreationOptions.LongRunning);
            _longTaskCache.Start();

        }

        /// <summary>
        /// 清理账号缓存
        /// </summary>
        /// <param name="id"></param>
        public void ClearAccountCache(string id)
        {
            _cache.Remove($"account:{id}");
        }

        /// <summary>
        /// 获取正在运行的任务列表。
        /// </summary>
        /// <returns>正在运行的任务列表</returns>
        public List<TaskInfo> GetRunningTasks(string channelId = null)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                // 返回所有频道的任务
                var tasks = new List<TaskInfo>();
                foreach (var channel in _channelPoolManager.Channels)
                {
                    tasks.AddRange(channel.GetRunningTasks());
                }
                return tasks;
            }
            else
            {
                // 返回指定频道的任务
                var channel = _channelPoolManager.SelectChannel(channelId);
                return channel?.GetRunningTasks() ?? [];
            }
        }

        /// <summary>
        /// 获取队列中的任务列表。
        /// </summary>
        /// <returns>队列中的任务列表</returns>
        public List<TaskInfo> GetQueueTasks(string channelId = null)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                // 返回所有频道的任务
                var tasks = new List<TaskInfo>();
                foreach (var channel in _channelPoolManager.Channels)
                {
                    tasks.AddRange(channel.GetQueueTasks());
                }
                return tasks;
            }
            else
            {
                // 返回指定频道的任务
                var channel = _channelPoolManager.SelectChannel(channelId);
                return channel?.GetQueueTasks() ?? [];
            }
        }

        /// <summary>
        /// 是否存在空闲队列，即：队列是否已满，是否可加入新的任务
        /// </summary>
        public bool IsIdleQueue(string channelId = null)
        {
            return _channelPoolManager.IsIdleQueue(channelId);
        }

        /// <summary>
        /// 退出任务并进行保存和通知。
        /// </summary>
        /// <param name="task">任务信息</param>
        public void ExitTask(TaskInfo task)
        {
            foreach (var channel in _channelPoolManager.Channels)
            {
                // 移除 TaskFutureMap 中指定的任务
                channel.TaskFutureMap.TryRemove(task.Id, out _);
                // 移除 QueueTasks 队列中指定的任务
                if (channel.QueueTasks.Any(c => c.Item1.Id == task.Id))
                {
                    var tempQueue = new ConcurrentQueue<(TaskInfo, Func<Task<Message>>)>();

                    // 将不需要移除的元素加入到临时队列中
                    while (channel.QueueTasks.TryDequeue(out var item))
                    {
                        if (item.Item1.Id != task.Id)
                        {
                            tempQueue.Enqueue(item);
                        }
                    }
                    // 交换队列引用
                    channel.QueueTasks = tempQueue;
                }
                break;
            }

            SaveAndNotify(task);
        }

        /// <summary>
        /// 获取正在运行的任务Future映射。
        /// </summary>
        /// <returns>任务Future映射</returns>
        public Dictionary<string, Task> GetRunningFutures(string channelId = null)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                // 返回所有频道的任务
                var futures = new Dictionary<string, Task>();
                foreach (var channel in _channelPoolManager.Channels)
                {
                    foreach (var item in channel.TaskFutureMap)
                    {
                        futures[item.Key] = item.Value;
                    }
                }
                return futures;
            }
            else
            {
                // 返回指定频道的任务
                var channel = _channelPoolManager.SelectChannel(channelId);
                return channel?.TaskFutureMap.ToDictionary(k => k.Key, v => v.Value) ?? new Dictionary<string, Task>();
            }
        }

        /// <summary>
        /// 提交任务。
        /// </summary>
        /// <param name="info">任务信息</param>
        /// <param name="discordSubmit">Discord提交任务的委托</param>
        /// <param name="channelId">频道ID，为空则智能选择可用频道</param>
        /// <returns>任务提交结果</returns>
        public SubmitResultVO SubmitTaskAsync(TaskInfo info, Func<Task<Message>> discordSubmit, string channelId = null)
        {
            var channel = _channelPoolManager.SelectChannel(info.ChannelId ?? channelId);
            _logger.Information("选择的频道为：{0}, {1}", channel?.GuildId, channel?.ChannelId);
            if (channel == null)
            {
                return SubmitResultVO.Fail(ReturnCode.FAILURE, $"提交失败，频道 {channelId} 不存在")
                    .SetProperty(Constants.TASK_PROPERTY_DISCORD_INSTANCE_ID, GuildId);
            }

            // 在任务提交时，前面的的任务数量
            var currentWaitNumbers = channel.QueueTasks.Count;
            if (Account.QueueSize > 0 && currentWaitNumbers >= Account.QueueSize)
            {
                return SubmitResultVO.Fail(ReturnCode.FAILURE, "提交失败，队列已满，请稍后重试")
                    .SetProperty(Constants.TASK_PROPERTY_DISCORD_INSTANCE_ID, channel.GuildId);
            }

            info.InstanceId = channel.GuildId;
            info.ChannelId = channel.ChannelId;
            info.SetProperty(Constants.TASK_PROPERTY_DISCORD_CHANNEL_ID, channel.ChannelId);
            info.SetProperty(Constants.TASK_PROPERTY_DISCORD_INSTANCE_ID, Account.GuildId);
            _taskStoreService.Save(info);

            try
            {
                channel.QueueTasks.Enqueue((info, discordSubmit));

                // 通知后台服务有新的任务
                channel.SignalControl.Set();

                if (currentWaitNumbers == 0)
                {
                    return SubmitResultVO.Of(ReturnCode.SUCCESS, "提交成功", info.Id)
                        .SetProperty(Constants.TASK_PROPERTY_DISCORD_INSTANCE_ID, channel.GuildId)
                        .SetProperty(Constants.TASK_PROPERTY_DISCORD_CHANNEL_ID, channel.ChannelId);
                }
                else
                {
                    return SubmitResultVO.Of(ReturnCode.IN_QUEUE, $"排队中，前面还有{currentWaitNumbers}个任务", info.Id)
                        .SetProperty("numberOfQueues", currentWaitNumbers)
                        .SetProperty(Constants.TASK_PROPERTY_DISCORD_INSTANCE_ID, channel.GuildId)
                        .SetProperty(Constants.TASK_PROPERTY_DISCORD_CHANNEL_ID, channel.ChannelId);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "submit task error");

                _taskStoreService.Delete(info.Id);

                return SubmitResultVO.Fail(ReturnCode.FAILURE, "提交失败，系统异常")
                     .SetProperty(Constants.TASK_PROPERTY_DISCORD_INSTANCE_ID, channel.GuildId)
                     .SetProperty(Constants.TASK_PROPERTY_DISCORD_CHANNEL_ID, channel.ChannelId);
            }
        }

        public void AddRunningTask(TaskInfo task)
        {
            var channel = _channelPoolManager.SelectChannel();
            if (channel != null)
            {
                channel.RunningTasks.TryAdd(task.Id, task);
            }
        }

        public void RemoveRunningTask(TaskInfo task)
        {
            // 获取任务对应的频道
            var channelIds = _channelPoolManager.Channels.Where(c => c.RunningTasks.Keys.Contains(task.Id)).Select(c => c.ChannelId).ToList();
            if (channelIds.Count > 0)
            {
                foreach (var channelId in channelIds)
                {
                    var channel = _channelPoolManager.SelectChannel(channelId);
                    channel?.RunningTasks.TryRemove(task.Id, out _);
                }
            }
        }

        /// <summary>
        /// 保存并通知任务状态变化。
        /// </summary>
        /// <param name="task">任务信息</param>
        private Task SaveAndNotify(TaskInfo task)
        {
            try
            {
                _taskStoreService.Save(task);
                _notifyService.NotifyTaskChange(task);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "作业通知执行异常 {@0}", task.Id);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 查找符合条件的正在运行的任务。
        /// </summary>
        /// <param name="condition">条件</param>
        /// <returns>符合条件的正在运行的任务列表</returns>
        public IEnumerable<TaskInfo> FindRunningTask(Func<TaskInfo, bool> condition)
        {
            return GetRunningTasks().Where(condition);
        }

        /// <summary>
        /// 根据ID获取正在运行的任务。
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <returns>任务信息</returns>
        public TaskInfo GetRunningTask(string id)
        {
            foreach (var channel in _channelPoolManager.Channels)
            {
                if (channel.RunningTasks.TryGetValue(id, out var task))
                {
                    return task;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据 ID 获取历史任务
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TaskInfo GetTask(string id)
        {
            return _taskStoreService.Get(id);
        }

        /// <summary>
        /// 根据随机数获取正在运行的任务。
        /// </summary>
        /// <param name="nonce">随机数</param>
        /// <returns>任务信息</returns>
        public TaskInfo GetRunningTaskByNonce(string nonce)
        {
            if (string.IsNullOrWhiteSpace(nonce))
            {
                return null;
            }

            return FindRunningTask(c => c.Nonce == nonce).FirstOrDefault();
        }

        /// <summary>
        /// 根据消息ID获取正在运行的任务。
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <returns>任务信息</returns>
        public TaskInfo GetRunningTaskByMessageId(string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
            {
                return null;
            }

            return FindRunningTask(c => c.MessageId == messageId).FirstOrDefault();
        }


        /// <summary>
        /// 替换通用交互参数
        /// </summary>
        private string ReplaceInteractionParams(string content, string nonce, EBotType? botType = null, string guid = null, string channelId = null)
        {
            // 使用智能选择逻辑获取频道ID
            var channel = _channelPoolManager.SelectChannel(channelId);

            var str = content
                .Replace("$guild_id", guid ?? Account.GuildId)
                .Replace("$channel_id", channel?.ChannelId ?? Account.ChannelId)
                .Replace("$session_id", DefaultSessionId)
                .Replace("$nonce", nonce);

            if (botType == EBotType.MID_JOURNEY)
            {
                str = str.Replace("$application_id", Constants.MJ_APPLICATION_ID);
            }
            else if (botType == EBotType.NIJI_JOURNEY)
            {
                str = str.Replace("$application_id", Constants.NIJI_APPLICATION_ID);
            }

            return str;
        }

        /// <summary>
        /// imagine 任务
        /// </summary>
        public async Task<Message> ImagineAsync(TaskInfo info, string prompt, string nonce)
        {

            prompt = GetPrompt(prompt, info);

            var json = (info.RealBotType ?? info.BotType) == EBotType.MID_JOURNEY ? _paramsMap["imagine"] : _paramsMap["imagineniji"];
            var paramsStr = ReplaceInteractionParams(json, nonce, null, null, info.ChannelId);

            JObject paramsJson = JObject.Parse(paramsStr);
            paramsJson["data"]["options"][0]["value"] = prompt;

            return await PostJsonAndCheckStatusAsync(paramsJson.ToString());
        }

        /// <summary>
        /// 放大
        /// </summary>
        public async Task<Message> UpscaleAsync(string messageId, int index, string messageHash, int messageFlags, string nonce, EBotType botType, string channelId = null)
        {

            string paramsStr = ReplaceInteractionParams(_paramsMap["upscale"], nonce, botType, null, channelId)
                .Replace("$message_id", messageId)
                .Replace("$index", index.ToString())
                .Replace("$message_hash", messageHash);

            var obj = JObject.Parse(paramsStr);

            if (obj.ContainsKey("message_flags"))
            {
                obj["message_flags"] = messageFlags;
            }
            else
            {
                obj.Add("message_flags", messageFlags);
            }

            paramsStr = obj.ToString();
            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 变化
        /// </summary>
        public async Task<Message> VariationAsync(string messageId, int index, string messageHash, int messageFlags, string nonce, EBotType botType, string channelId = null)
        {

            string paramsStr = ReplaceInteractionParams(_paramsMap["variation"], nonce, botType, null, channelId)
                .Replace("$message_id", messageId)
                .Replace("$index", index.ToString())
                .Replace("$message_hash", messageHash);
            var obj = JObject.Parse(paramsStr);

            if (obj.ContainsKey("message_flags"))
            {
                obj["message_flags"] = messageFlags;
            }
            else
            {
                obj.Add("message_flags", messageFlags);
            }

            paramsStr = obj.ToString();
            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 执行动作
        /// </summary>
        public async Task<Message> ActionAsync(
            string messageId,
            string customId,
            int messageFlags,
            string nonce,
            TaskInfo info)
        {

            var botType = info.RealBotType ?? info.BotType;

            string guid = null;
            string channelId = info.ChannelId;
            if (!string.IsNullOrWhiteSpace(info.SubInstanceId) && info.SubInstanceId != channelId)
            {
                if (Account.SubChannelValues.ContainsKey(info.SubInstanceId))
                {
                    guid = Account.SubChannelValues[info.SubInstanceId];
                    channelId = info.SubInstanceId;
                }
            }

            var paramsStr = ReplaceInteractionParams(_paramsMap["action"], nonce, botType,
                guid, channelId)
                .Replace("$message_id", messageId);

            var obj = JObject.Parse(paramsStr);

            if (obj.ContainsKey("message_flags"))
            {
                obj["message_flags"] = messageFlags;
            }
            else
            {
                obj.Add("message_flags", messageFlags);
            }

            obj["data"]["custom_id"] = customId;

            paramsStr = obj.ToString();
            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 自定义变焦
        /// </summary>
        public async Task<Message> ZoomAsync(TaskInfo info, string messageId, string customId, string prompt, string nonce)
        {

            customId = customId.Replace("MJ::CustomZoom::", "MJ::OutpaintCustomZoomModal::");
            prompt = GetPrompt(prompt, info);

            string paramsStr = ReplaceInteractionParams(_paramsMap["zoom"], nonce, info.RealBotType ?? info.BotType, null, info.ChannelId)
                .Replace("$message_id", messageId);

            var obj = JObject.Parse(paramsStr);

            obj["data"]["custom_id"] = customId;
            obj["data"]["components"][0]["components"][0]["value"] = prompt;

            paramsStr = obj.ToString();
            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 图生文 - 生图
        /// </summary>
        public async Task<Message> PicReaderAsync(TaskInfo info, string messageId, string customId, string prompt, string nonce, EBotType botType)
        {

            var index = customId.Split("::").LastOrDefault();
            prompt = GetPrompt(prompt, info);

            string paramsStr = ReplaceInteractionParams(_paramsMap["picreader"], nonce, botType, null, info.ChannelId)
                .Replace("$message_id", messageId)
                .Replace("$index", index);

            var obj = JObject.Parse(paramsStr);
            obj["data"]["components"][0]["components"][0]["value"] = prompt;
            paramsStr = obj.ToString();

            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// Remix 操作
        /// </summary>
        public async Task<Message> RemixAsync(TaskInfo info, TaskAction action, string messageId, string modal, string customId, string prompt, string nonce, EBotType botType)
        {

            prompt = GetPrompt(prompt, info);

            string paramsStr = ReplaceInteractionParams(_paramsMap["remix"], nonce, botType, null, info.ChannelId)
                .Replace("$message_id", messageId)
                .Replace("$custom_id", customId)
                .Replace("$modal", modal);

            var obj = JObject.Parse(paramsStr);
            obj["data"]["components"][0]["components"][0]["value"] = prompt;
            paramsStr = obj.ToString();

            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 执行 info 操作
        /// </summary>
        public async Task<Message> InfoAsync(string nonce, EBotType botType, string channelId = null)
        {

            var content = botType == EBotType.MID_JOURNEY ? _paramsMap["info"] : _paramsMap["infoniji"];

            var paramsStr = ReplaceInteractionParams(content, nonce, null, null, channelId);
            var obj = JObject.Parse(paramsStr);
            paramsStr = obj.ToString();
            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 执行 settings button 操作
        /// </summary>
        public async Task<Message> SettingButtonAsync(string nonce, string custom_id, EBotType botType)
        {

            var paramsStr = ReplaceInteractionParams(_paramsMap["settingbutton"], nonce)
                .Replace("$custom_id", custom_id);

            if (botType == EBotType.NIJI_JOURNEY)
            {
                paramsStr = paramsStr
                    .Replace("$application_id", Constants.NIJI_APPLICATION_ID)
                    .Replace("$message_id", Account.NijiSettingsMessageId);
            }
            else if (botType == EBotType.MID_JOURNEY)
            {
                paramsStr = paramsStr
                    .Replace("$application_id", Constants.MJ_APPLICATION_ID)
                    .Replace("$message_id", Account.SettingsMessageId);
            }

            var obj = JObject.Parse(paramsStr);
            paramsStr = obj.ToString();
            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// MJ 执行 settings select 操作
        /// </summary>
        public async Task<Message> SettingSelectAsync(string nonce, string values)
        {

            var paramsStr = ReplaceInteractionParams(_paramsMap["settingselect"], nonce)
              .Replace("$message_id", Account.SettingsMessageId)
              .Replace("$values", values);
            var obj = JObject.Parse(paramsStr);
            paramsStr = obj.ToString();
            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 执行 setting 操作
        /// </summary>
        public async Task<Message> SettingAsync(string nonce, EBotType botType, string channelId = null)
        {

            var content = botType == EBotType.NIJI_JOURNEY ? _paramsMap["settingniji"] : _paramsMap["setting"];

            var paramsStr = ReplaceInteractionParams(content, nonce, null, null, channelId);

            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 根据 job id 显示任务信息
        /// </summary>
        public async Task<Message> ShowAsync(string jobId, string nonce, EBotType botType, string channelId = null)
        {

            var content = botType == EBotType.MID_JOURNEY ? _paramsMap["show"] : _paramsMap["showniji"];

            var paramsStr = ReplaceInteractionParams(content, nonce, null, null, channelId)
                .Replace("$value", jobId);

            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 图片 seed 值
        /// </summary>
        public async Task<Message> SeedAsync(string jobId, string nonce, EBotType botType)
        {
            // 私聊频道
            var json = botType == EBotType.MID_JOURNEY ? _paramsMap["seed"] : _paramsMap["seedniji"];
            var paramsStr = json
              .Replace("$channel_id", botType == EBotType.MID_JOURNEY ? Account.PrivateChannelId : Account.NijiBotChannelId)
              .Replace("$session_id", DefaultSessionId)
              .Replace("$nonce", nonce)
              .Replace("$job_id", jobId);

            var obj = JObject.Parse(paramsStr);
            paramsStr = obj.ToString();
            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 图片 seed 值消息
        /// </summary>
        public async Task<Message> SeedMessagesAsync(string url)
        {
            try
            {
                // 解码
                url = System.Web.HttpUtility.UrlDecode(url);

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = new StringContent("", Encoding.UTF8, "application/json")
                };

                request.Headers.UserAgent.ParseAdd(Account.UserAgent);

                // 设置 request Authorization 为 UserToken，不需要 Bearer 前缀
                request.Headers.Add("Authorization", Account.UserToken);

                var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return Message.Success();
                }

                _logger.Error("Seed Http 请求执行失败 {@0}, {@1}, {@2}", url, response.StatusCode, response.Content);

                return Message.Of((int)response.StatusCode, "请求失败");
            }
            catch (HttpRequestException e)
            {
                _logger.Error(e, "Seed Http 请求执行异常 {@0}", url);

                return Message.Of(ReturnCode.FAILURE, e.Message?.Substring(0, Math.Min(e.Message.Length, 100)) ?? "未知错误");
            }
        }

        /// <summary>
        /// 局部重绘
        /// </summary>
        public async Task<Message> InpaintAsync(TaskInfo info, string customId, string prompt, string maskBase64)
        {
            try
            {
                prompt = GetPrompt(prompt, info);

                customId = customId?.Replace("MJ::iframe::", "");

                // mask.replace(/^data:.+?;base64,/, ''),
                maskBase64 = maskBase64?.Replace("data:image/png;base64,", "");

                var obj = new
                {
                    customId = customId,
                    //full_prompt = null,
                    mask = maskBase64,
                    prompt = prompt,
                    userId = "0",
                    username = "0",
                };
                var paramsStr = Newtonsoft.Json.JsonConvert.SerializeObject(obj);

                // NIJI 也是这个链接
                var response = await PostJsonAsync("https://936929561302675456.discordsays.com/inpaint/api/submit-job",
                    paramsStr);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return Message.Success();
                }

                return Message.Of((int)response.StatusCode, "提交失败");
            }
            catch (HttpRequestException e)
            {
                _logger.Error(e, "局部重绘请求执行异常 {@0}", info);

                return Message.Of(ReturnCode.FAILURE, e.Message?.Substring(0, Math.Min(e.Message.Length, 100)) ?? "未知错误");
            }
        }

        /// <summary>
        /// 重新生成
        /// </summary>
        public async Task<Message> RerollAsync(string messageId, string messageHash, int messageFlags, string nonce, EBotType botType, string channelId = null)
        {

            string paramsStr = ReplaceInteractionParams(_paramsMap["reroll"], nonce, botType, null, channelId)
                .Replace("$message_id", messageId)
                .Replace("$message_hash", messageHash);
            var obj = JObject.Parse(paramsStr);

            if (obj.ContainsKey("message_flags"))
            {
                obj["message_flags"] = messageFlags;
            }
            else
            {
                obj.Add("message_flags", messageFlags);
            }

            paramsStr = obj.ToString();
            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 解析描述
        /// </summary>
        public async Task<Message> DescribeAsync(string finalFileName, string nonce, EBotType botType)
        {

            string fileName = finalFileName.Substring(finalFileName.LastIndexOf("/") + 1);

            var json = botType == EBotType.NIJI_JOURNEY ? _paramsMap["describeniji"] : _paramsMap["describe"];
            string paramsStr = ReplaceInteractionParams(json, nonce)
                .Replace("$file_name", fileName)
                .Replace("$final_file_name", finalFileName);
            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 解析描述
        /// </summary>
        public async Task<Message> DescribeByLinkAsync(string link, string nonce, EBotType botType, string channelId = null)
        {

            var json = botType == EBotType.NIJI_JOURNEY ? _paramsMap["describenijilink"] : _paramsMap["describelink"];
            string paramsStr = ReplaceInteractionParams(json, nonce, null, null, channelId)
                .Replace("$link", link);
            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 上传一个较长的提示词，mj 可以返回一组简要的提示词
        /// </summary>
        public async Task<Message> ShortenAsync(TaskInfo info, string prompt, string nonce, EBotType botType)
        {

            prompt = GetPrompt(prompt, info);

            var json = botType == EBotType.MID_JOURNEY || prompt.Contains("--niji") ? _paramsMap["shorten"] : _paramsMap["shortenniji"];
            var paramsStr = ReplaceInteractionParams(json, nonce, null, null, info.ChannelId);

            var obj = JObject.Parse(paramsStr);
            obj["data"]["options"][0]["value"] = prompt;
            paramsStr = obj.ToString();

            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 合成
        /// </summary>
        public async Task<Message> BlendAsync(List<string> finalFileNames, BlendDimensions dimensions, string nonce, EBotType botType, string channelId = null)
        {

            var json = botType == EBotType.MID_JOURNEY || GlobalConfiguration.Setting.EnableConvertNijiToMj ? _paramsMap["blend"] : _paramsMap["blendniji"];

            string paramsStr = ReplaceInteractionParams(json, nonce, null, null, channelId);
            JObject paramsJson = JObject.Parse(paramsStr);
            JArray options = (JArray)paramsJson["data"]["options"];
            JArray attachments = (JArray)paramsJson["data"]["attachments"];
            for (int i = 0; i < finalFileNames.Count; i++)
            {
                string finalFileName = finalFileNames[i];
                string fileName = finalFileName.Substring(finalFileName.LastIndexOf("/") + 1);
                JObject attachment = new JObject
                {
                    ["id"] = i.ToString(),
                    ["filename"] = fileName,
                    ["uploaded_filename"] = finalFileName
                };
                attachments.Add(attachment);
                JObject option = new JObject
                {
                    ["type"] = 11,
                    ["name"] = $"image{i + 1}",
                    ["value"] = i
                };
                options.Add(option);
            }
            options.Add(new JObject
            {
                ["type"] = 3,
                ["name"] = "dimensions",
                ["value"] = $"--ar {dimensions.GetValue()}"
            });
            return await PostJsonAndCheckStatusAsync(paramsJson.ToString());
        }

        /// <summary>
        /// 发送PUT文件
        /// </summary>
        private async Task PutFileAsync(string uploadUrl, DataUrl dataUrl)
        {
            uploadUrl = _discordHelper.GetDiscordUploadUrl(uploadUrl);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
            {
                Content = new ByteArrayContent(dataUrl.Data)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(dataUrl.MimeType);
            request.Content.Headers.ContentLength = dataUrl.Data.Length;
            request.Headers.UserAgent.ParseAdd(Account.UserAgent);
            await _httpClient.SendAsync(request);
        }

        /// <summary>
        /// 发送POST请求
        /// </summary>
        private async Task<HttpResponseMessage> PostJsonAsync(string url, string paramsStr)
        {
            _logger.Debug("PostJsonAsync url = {@0}, paramsStr = {@1}", url, paramsStr);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(paramsStr, Encoding.UTF8, "application/json")
            };

            request.Headers.UserAgent.ParseAdd(Account.UserAgent);

            // 设置 request Authorization 为 UserToken，不需要 Bearer 前缀
            request.Headers.Add("Authorization", Account.UserToken);

            return await _httpClient.SendAsync(request);
        }

        /// <summary>
        /// 发送POST请求并检查状态
        /// </summary>
        private async Task<Message> PostJsonAndCheckStatusAsync(string paramsStr)
        {
            // 如果 TooManyRequests 请求失败，则重拾最多 3 次
            var count = 5;

            // 已处理的 message id
            var messageIds = new List<string>();
            do
            {
                HttpResponseMessage response = null;
                try
                {
                    response = await PostJsonAsync(_discordInteractionUrl, paramsStr);
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        return Message.Success();
                    }
                    else if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        count--;
                        if (count > 0)
                        {
                            // 等待 3~6 秒
                            var random = new Random();
                            var seconds = random.Next(3, 6);
                            await Task.Delay(seconds * 1000);

                            _logger.Warning("Http 请求执行频繁，等待重试 {@0}, {@1}, {@2}", paramsStr, response.StatusCode, response.Content);
                            continue;
                        }
                    }
                    else if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        count--;

                        if (count > 0)
                        {
                            // 等待 3~6 秒
                            var random = new Random();
                            var seconds = random.Next(3, 6);
                            await Task.Delay(seconds * 1000);

                            // 当是 NotFound 时
                            // 可能是 message id 错乱导致
                            if (paramsStr.Contains("message_id") && paramsStr.Contains("nonce"))
                            {
                                var obj = JObject.Parse(paramsStr);
                                if (obj.ContainsKey("message_id") && obj.ContainsKey("nonce"))
                                {
                                    var nonce = obj["nonce"].ToString();
                                    var message_id = obj["message_id"].ToString();
                                    if (!string.IsNullOrEmpty(nonce) && !string.IsNullOrWhiteSpace(message_id))
                                    {
                                        messageIds.Add(message_id);

                                        var t = GetRunningTaskByNonce(nonce);
                                        if (t != null && !string.IsNullOrWhiteSpace(t.ParentId))
                                        {
                                            var p = GetTask(t.ParentId);
                                            if (p != null)
                                            {
                                                var newMessageId = p.MessageIds.Where(c => !messageIds.Contains(c)).FirstOrDefault();
                                                if (!string.IsNullOrWhiteSpace(newMessageId))
                                                {
                                                    obj["message_id"] = newMessageId;

                                                    var oldStr = paramsStr;
                                                    paramsStr = obj.ToString();

                                                    _logger.Warning("Http 可能消息错乱，等待重试 {@0}, {@1}, {@2}, {@3}", oldStr, paramsStr, response.StatusCode, response.Content);
                                                    continue;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // _logger.Error("Http 请求执行失败 {@0}, {@1}, {@2}", paramsStr, response.StatusCode, response.Content);
                    // 正确读取响应内容
                    string responseBody = await response.Content.ReadAsStringAsync();
                    _logger.Error(
                        "Http 请求执行失败 | 参数: {Params} | 状态码: {StatusCode} | 响应内容: {Content}",
                        paramsStr,
                        (int)response.StatusCode,
                        responseBody);

                    var error = $"{response.StatusCode}: {paramsStr.Substring(0, Math.Min(paramsStr.Length, 1000))}";

                    // 如果是 403 则直接禁用账号
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        _logger.Error("Http 请求没有操作权限，禁用账号 {@0}", paramsStr);

                        Account.Enable = false;
                        Account.DisabledReason = "Http 请求没有操作权限，禁用账号";
                        DbHelper.Instance.AccountStore.Update(Account);
                        ClearAccountCache(Account.Id);

                        return Message.Of(ReturnCode.FAILURE, "请求失败，禁用账号");
                    }

                    return Message.Of((int)response.StatusCode, error);
                }
                catch (HttpRequestException e)
                {
                    _logger.Error(e, "Http 请求执行异常 {@0}", paramsStr);

                    return Message.Of(ReturnCode.FAILURE, e.Message?.Substring(0, Math.Min(e.Message.Length, 100)) ?? "未知错误");
                }
            } while (true);
        }

        /// <summary>
        /// 发送DELETE请求到指定URL
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <returns>HTTP响应消息</returns>
        private async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, url);

            // 添加请求头
            request.Headers.UserAgent.ParseAdd(Account.UserAgent);
            // 设置 request Authorization 为 UserToken，不需要 Bearer 前缀
            request.Headers.Add("Authorization", Account.UserToken);

            // 发送请求
            return await _httpClient.SendAsync(request);
        }

        /// <summary>
        /// 缓存处理
        /// </summary>
        private void RuningCache()
        {
            while (true)
            {
                if (_longToken.Token.IsCancellationRequested)
                {
                    // 清理资源（如果需要）
                    break;
                }

                try
                {
                    // 当前时间转为 Unix 时间戳
                    // 今日 0 点 Unix 时间戳
                    var now = new DateTimeOffset(DateTime.Now.Date).ToUnixTimeMilliseconds();
                    var count = (int)DbHelper.Instance.TaskStore.Count(c => c.SubmitTime >= now && c.InstanceId == Account.GuildId);

                    if (Account.DayDrawCount != count)
                    {
                        Account.DayDrawCount = count;

                        DbHelper.Instance.AccountStore.Update("DayDrawCount", Account);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "RuningCache 异常");
                }

                // 每 2 分钟执行一次
                Thread.Sleep(60 * 1000 * 2);
            }
        }

        /// <summary>
        /// 全局切换 fast 模式
        /// </summary>
        /// <param name="nonce"></param>
        /// <param name="botType"></param>
        /// <returns></returns>
        public async Task<Message> FastAsync(string nonce, EBotType botType)
        {

            if (botType == EBotType.NIJI_JOURNEY && Account.EnableNiji != true)
            {
                return Message.Success("忽略提交，未开启 niji");
            }

            if (botType == EBotType.MID_JOURNEY && Account.EnableMj != true)
            {
                return Message.Success("忽略提交，未开启 mj");
            }

            var json = botType == EBotType.MID_JOURNEY ? _paramsMap["fast"] : _paramsMap["fastniji"];
            var paramsStr = ReplaceInteractionParams(json, nonce, null, null, null);
            var obj = JObject.Parse(paramsStr);
            paramsStr = obj.ToString();
            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 全局切换 relax 模式
        /// </summary>
        /// <param name="nonce"></param>
        /// <param name="botType"></param>
        /// <returns></returns>
        public async Task<Message> RelaxAsync(string nonce, EBotType botType)
        {

            if (botType == EBotType.NIJI_JOURNEY && Account.EnableNiji != true)
            {
                return Message.Success("忽略提交，未开启 niji");
            }

            if (botType == EBotType.MID_JOURNEY && Account.EnableMj != true)
            {
                return Message.Success("忽略提交，未开启 mj");
            }

            var json = botType == EBotType.NIJI_JOURNEY ? _paramsMap["relax"] : _paramsMap["relaxniji"];
            var paramsStr = ReplaceInteractionParams(json, nonce, null, null, null);
            var obj = JObject.Parse(paramsStr);
            paramsStr = obj.ToString();
            return await PostJsonAndCheckStatusAsync(paramsStr);
        }

        /// <summary>
        /// 全局切换快速模式检查
        /// </summary>
        /// <returns></returns>
        public async Task RelaxToFastValidate()
        {
            try
            {
                // 快速用完时
                // 并且开启快速切换慢速模式时
                if (Account != null && Account.FastExhausted && Account.EnableRelaxToFast == true)
                {
                    // 每 6~12 小时，和启动时检查账号是否有快速时长
                    await RandomSyncInfo();

                    // 判断 info 检查时间是否在 5 分钟内
                    if (Account.InfoUpdated != null && Account.InfoUpdated.Value.AddMinutes(5) >= DateTime.Now)
                    {
                        _logger.Information("自动切换快速模式，验证 {@0}", Account.GuildId);

                        // 提取 fastime
                        // 如果检查完之后，快速超过 1 小时，则标记为快速未用完
                        var fastTime = Account.FastTimeRemaining?.ToString()?.Split('/')?.FirstOrDefault()?.Trim();
                        if (!string.IsNullOrWhiteSpace(fastTime) && double.TryParse(fastTime, out var ftime) && ftime >= 1)
                        {
                            _logger.Information("自动切换快速模式，开始 {@0}", Account.GuildId);

                            // 标记未用完快速
                            Account.FastExhausted = false;
                            DbHelper.Instance.AccountStore.Update("FastExhausted", Account);

                            // 如果开启了自动切换到快速，则自动切换到快速
                            try
                            {
                                if (Account.EnableRelaxToFast == true)
                                {
                                    Thread.Sleep(2500);
                                    await FastAsync(SnowFlake.NextId(), EBotType.MID_JOURNEY);

                                    Thread.Sleep(2500);
                                    await FastAsync(SnowFlake.NextId(), EBotType.NIJI_JOURNEY);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex, "自动切换快速模式，执行异常 {@0}", Account.GuildId);
                            }

                            ClearAccountCache(Account.Id);

                            _logger.Information("自动切换快速模式，执行完成 {@0}", Account.GuildId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "快速切换慢速模式，检查执行异常 {@0}", Account.GuildId);
            }
        }

        /// <summary>
        /// 随机 6-12 小时 同步一次账号信息
        /// </summary>
        /// <returns></returns>
        public async Task RandomSyncInfo()
        {
            // 每 6~12 小时
            if (Account.InfoUpdated == null || Account.InfoUpdated.Value.AddMinutes(5) < DateTime.Now)
            {
                var key = $"fast_exhausted_{Account.Id}";
                await _cache.GetOrCreateAsync(key, async c =>
                {
                    try
                    {
                        _logger.Information("随机同步账号信息开始 {@0}", Account.GuildId);

                        // 随机 6~12 小时
                        var random = new Random();
                        var minutes = random.Next(360, 600);
                        c.SetAbsoluteExpiration(TimeSpan.FromMinutes(minutes));

                        await _taskService.InfoSetting(Account.Id);

                        _logger.Information("随机同步账号信息完成 {@0}", Account.GuildId);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "随机同步账号信息异常 {@0}", Account.GuildId);
                    }

                    return false;
                });
            }
        }

        /// <summary>
        /// 获取 prompt 格式化
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public string GetPrompt(string prompt, TaskInfo info)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return prompt;
            }

            // 如果开启 niji 转 mj
            if (info.RealBotType == EBotType.MID_JOURNEY && info.BotType == EBotType.NIJI_JOURNEY)
            {
                if (!prompt.Contains("--niji"))
                {
                    prompt += " --niji";
                }
            }

            // 将 2 个空格替换为 1 个空格
            // 将 " -- " 替换为 " "
            prompt = prompt.Replace(" -- ", " ")
                .Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Trim();

            // 任务指定速度模式
            if (info != null && info.Mode != null)
            {
                // 移除 prompt 可能的的参数
                prompt = prompt.Replace("--fast", "").Replace("--relax", "").Replace("--turbo", "");

                // 如果任务指定了速度模式
                if (info.Mode != null)
                {
                    switch (info.Mode.Value)
                    {
                        case GenerationSpeedMode.RELAX:
                            prompt += " --relax";
                            break;

                        case GenerationSpeedMode.FAST:
                            prompt += " --fast";
                            break;

                        case GenerationSpeedMode.TURBO:
                            prompt += " --turbo";
                            break;

                        default:
                            break;
                    }
                }
            }

            // 允许速度模式
            if (Account.AllowModes?.Count > 0)
            {
                // 计算不允许的速度模式，并删除相关参数
                var notAllowModes = new List<string>();
                if (!Account.AllowModes.Contains(GenerationSpeedMode.RELAX))
                {
                    notAllowModes.Add("--relax");
                }
                if (!Account.AllowModes.Contains(GenerationSpeedMode.FAST))
                {
                    notAllowModes.Add("--fast");
                }
                if (!Account.AllowModes.Contains(GenerationSpeedMode.TURBO))
                {
                    notAllowModes.Add("--turbo");
                }

                // 移除 prompt 可能的的参数
                foreach (var mode in notAllowModes)
                {
                    prompt = prompt.Replace(mode, "");
                }
            }

            // 如果快速模式用完了，且启用自动切换慢速
            if (Account.FastExhausted && Account.EnableAutoSetRelax == true)
            {
                // 移除 prompt 可能的的参数
                prompt = prompt.Replace("--fast", "").Replace("--relax", "").Replace("--turbo", "");

                prompt += " --relax";
            }

            // 指定生成速度模式
            if (Account.Mode != null)
            {
                // 移除 prompt 可能的的参数
                prompt = prompt.Replace("--fast", "").Replace("--relax", "").Replace("--turbo", "");

                switch (Account.Mode.Value)
                {
                    case GenerationSpeedMode.RELAX:
                        prompt += " --relax";
                        break;

                    case GenerationSpeedMode.FAST:
                        prompt += " --fast";
                        break;

                    case GenerationSpeedMode.TURBO:
                        prompt += " --turbo";
                        break;

                    default:
                        break;
                }
            }

            prompt = FormatUrls(prompt).ConfigureAwait(false).GetAwaiter().GetResult();

            return prompt;
        }

        /// <summary>
        /// 对 prompt 中含有 url 的进行转换为官方 url 处理
        /// 同一个 url 1 小时内有效缓存
        /// </summary>
        /// <param name="prompt"></param>
        /// <returns></returns>
        public async Task<string> FormatUrls(string prompt)
        {
            var setting = GlobalConfiguration.Setting;
            if (!setting.EnableConvertOfficialLink)
            {
                return prompt;
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                return prompt;
            }

            // 使用正则提取所有的 url
            var urls = Regex.Matches(prompt, @"(https?|ftp|file)://[-A-Za-z0-9+&@#/%?=~_|!:,.;]+[-A-Za-z0-9+&@#/%=~_|]")
                .Select(c => c.Value).Distinct().ToList();

            if (urls?.Count > 0)
            {
                var urlDic = new Dictionary<string, string>();
                foreach (var url in urls)
                {
                    try
                    {
                        // url 缓存默认 24 小时有效
                        var okUrl = await _cache.GetOrCreateAsync($"tmp:{url}", async entry =>
                        {
                            entry.AbsoluteExpiration = DateTimeOffset.Now.AddHours(24);

                            var ff = new FileFetchHelper();
                            var res = await ff.FetchFileAsync(url);
                            if (res.Success && !string.IsNullOrWhiteSpace(res.Url))
                            {
                                return res.Url;
                            }
                            else if (res.Success && res.FileBytes.Length > 0)
                            {
                                // 上传到 Discord 服务器
                                var uploadResult = await UploadAsync(res.FileName, new DataUrl(res.ContentType, res.FileBytes));
                                if (uploadResult.Code != ReturnCode.SUCCESS)
                                {
                                    throw new LogicException(uploadResult.Code, uploadResult.Description);
                                }

                                if (uploadResult.Description.StartsWith("http"))
                                {
                                    return uploadResult.Description;
                                }
                                else
                                {
                                    var finalFileName = uploadResult.Description;
                                    var sendImageResult = await SendImageMessageAsync("upload image: " + finalFileName, finalFileName);
                                    if (sendImageResult.Code != ReturnCode.SUCCESS)
                                    {
                                        throw new LogicException(sendImageResult.Code, sendImageResult.Description);
                                    }

                                    return sendImageResult.Description;
                                }
                            }

                            throw new LogicException($"解析链接失败 {url}, {res?.Msg}");
                        });

                        urlDic[url] = okUrl;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "解析 url 异常 {0}", url);
                    }
                }

                // 替换 url
                foreach (var item in urlDic)
                {
                    prompt = prompt.Replace(item.Key, item.Value);
                }
            }

            return prompt;
        }

        /// <summary>
        /// 上传
        /// </summary>
        public async Task<Message> UploadAsync(string fileName, DataUrl dataUrl, bool useDiscordUpload = false, string channelId = null)
        {
            var channel = _channelPoolManager.SelectChannel(channelId);

            if (channel == null)
            {
                return Message.Of(ReturnCode.VALIDATION_ERROR, "频道不存在");
            }

            // 保存用户上传的 base64 到文件存储
            if (GlobalConfiguration.Setting.EnableSaveUserUploadBase64 && !useDiscordUpload)
            {
                try
                {
                    var localPath = $"attachments/{DateTime.Now:yyyyMMdd}/{fileName}";

                    var mt = MimeKit.MimeTypes.GetMimeType(Path.GetFileName(localPath));
                    if (string.IsNullOrWhiteSpace(mt))
                    {
                        mt = "image/png";
                    }

                    var stream = new MemoryStream(dataUrl.Data);
                    var res = StorageHelper.Instance?.SaveAsync(stream, localPath, dataUrl.MimeType ?? mt);
                    if (string.IsNullOrWhiteSpace(res?.Url))
                    {
                        throw new Exception("上传图片到加速站点失败");
                    }

                    var url = res.Url;

                    return Message.Success(url);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "上传图片到加速站点异常");

                    return Message.Of(ReturnCode.FAILURE, "上传图片到加速站点异常");
                }
            }
            else
            {
                try
                {
                    JObject fileObj = new JObject
                    {
                        ["filename"] = fileName,
                        ["file_size"] = dataUrl.Data.Length,
                        ["id"] = "0"
                    };
                    JObject paramsJson = new JObject
                    {
                        ["files"] = new JArray { fileObj }
                    };

                    var discordAttachmentUrl = $"{_discordHelper.GetServer()}/api/v9/channels/{channel.ChannelId}/attachments";
                    HttpResponseMessage response = await PostJsonAsync(discordAttachmentUrl, paramsJson.ToString());
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        _logger.Error("上传图片到discord失败, status: {StatusCode}, msg: {Body}", response.StatusCode, await response.Content.ReadAsStringAsync());
                        return Message.Of(ReturnCode.VALIDATION_ERROR, "上传图片到discord失败");
                    }
                    JArray array = JObject.Parse(await response.Content.ReadAsStringAsync())["attachments"] as JArray;
                    if (array == null || array.Count == 0)
                    {
                        return Message.Of(ReturnCode.VALIDATION_ERROR, "上传图片到discord失败");
                    }
                    string uploadUrl = array[0]["upload_url"].ToString();
                    string uploadFilename = array[0]["upload_filename"].ToString();

                    await PutFileAsync(uploadUrl, dataUrl);

                    return Message.Success(uploadFilename);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "上传图片到discord失败");

                    return Message.Of(ReturnCode.FAILURE, "上传图片到discord失败");
                }
            }
        }

        public async Task<Message> SendImageMessageAsync(string content, string finalFileName, string channelId = null)
        {
            var channel = _channelPoolManager.SelectChannel(channelId);

            if (channel == null)
            {
                return Message.Of(ReturnCode.VALIDATION_ERROR, "频道不存在");
            }

            string fileName = finalFileName.Substring(finalFileName.LastIndexOf("/") + 1);
            string paramsStr = _paramsMap["message"]
                .Replace("$content", content)
                .Replace("$channel_id", channel.ChannelId)
                .Replace("$file_name", fileName)
                .Replace("$final_file_name", finalFileName);

            var discordMessageUrl = $"{_discordHelper.GetServer()}/api/v9/channels/{channel.ChannelId}/messages";
            HttpResponseMessage response = await PostJsonAsync(discordMessageUrl, paramsStr);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                _logger.Error("发送图片消息到discord失败, status: {StatusCode}, msg: {Body}", response.StatusCode, await response.Content.ReadAsStringAsync());
                return Message.Of(ReturnCode.VALIDATION_ERROR, "发送图片消息到discord失败");
            }
            JObject result = JObject.Parse(await response.Content.ReadAsStringAsync());
            JArray attachments = result["attachments"] as JArray;
            if (attachments != null && attachments.Count > 0)
            {
                return Message.Success(attachments[0]["url"].ToString());
            }
            return Message.Failure("发送图片消息到discord失败: 图片不存在");
        }

        /// <summary>
        /// 自动读 discord 最后一条消息（设置为已读）
        /// </summary>
        /// <param name="lastMessageId">最后一条消息ID</param>
        /// <param name="channelId">频道ID</param>
        /// <returns></returns>
        public async Task<Message> ReadMessageAsync(string lastMessageId, string channelId = null)
        {
            var channel = _channelPoolManager.SelectChannel(channelId);

            if (string.IsNullOrWhiteSpace(lastMessageId))
            {
                return Message.Of(ReturnCode.VALIDATION_ERROR, "lastMessageId 不能为空");
            }

            if (channel == null)
            {
                return Message.Of(ReturnCode.VALIDATION_ERROR, "频道不存在");
            }

            var paramsStr = @"{""token"":null,""last_viewed"":3496}";
            var discordMessageUrl = $"{_discordHelper.GetServer()}/api/v9/channels/{channel.ChannelId}/messages";
            var url = $"{discordMessageUrl}/{lastMessageId}/ack";

            HttpResponseMessage response = await PostJsonAsync(url, paramsStr);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.Error("自动读discord消息失败, status: {StatusCode}, msg: {Body}", response.StatusCode, await response.Content.ReadAsStringAsync());
                return Message.Of(ReturnCode.VALIDATION_ERROR, "自动读discord消息失败");
            }
            return Message.Success();
        }

        /// <summary>
        /// 删除Discord消息
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <param name="channelId">频道ID</param>
        /// <returns>删除结果</returns>
        public async Task<Message> DeleteMessageAsync(string messageId, string channelId = null)
        {
            var channel = _channelPoolManager.SelectChannel(channelId);

            if (string.IsNullOrWhiteSpace(messageId))
            {
                return Message.Of(ReturnCode.VALIDATION_ERROR, "messageId 不能为空");
            }

            if (channel == null)
            {
                return Message.Of(ReturnCode.VALIDATION_ERROR, "频道不存在");
            }

            var discordMessageUrl = $"{_discordHelper.GetServer()}/api/v9/channels/{channel.ChannelId}/messages";
            var url = $"{discordMessageUrl}/{messageId}";

            try
            {
                HttpResponseMessage response = await DeleteAsync(url);
                if (response.StatusCode != HttpStatusCode.NoContent &&
                    response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.Error("删除Discord消息失败, url: {Url}, status: {StatusCode}, msg: {Body}", url, response.StatusCode, await response.Content.ReadAsStringAsync());
                    return Message.Of(ReturnCode.VALIDATION_ERROR, "删除Discord消息失败");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "删除Discord消息失败, url: {Url}", url);
                return Message.Of(ReturnCode.VALIDATION_ERROR, "删除Discord消息失败");
            }

            return Message.Success();
        }

        /// <summary>
        /// 获取带前缀的 token
        /// </summary>
        /// <returns>带前缀的 token</returns>
        public string GetPrefixedToken()
        {
            if (Account.UseBotWss && !string.IsNullOrEmpty(Account.BotToken))
            {
                return $"Bot {Account.BotToken}";
            }
            return Account.UserToken;
        }

        public Task OnChannelSubscribe(ConcurrentDictionary<string, DiscordExtendedGuild> guilds)
        {
            if (!Account.EnableAutoFetchChannels) return Task.CompletedTask;
            _logger.Information("ChannelSubscribe 频道事件触发，开始更新频道数据");
            // 匹配对应的服务器
            if (!guilds.TryGetValue(Account.GuildId, out var guild) || guild == null) return Task.CompletedTask;
            // 获取所有文本频道
            var newChannelIds = guild.Channels?.Where(c => c.Type == ChannelType.Text).Select(c => c.Id).ToList();
            if (newChannelIds == null || newChannelIds.Count <= 0)
            {
                return Task.CompletedTask;
            }
            _logger.Information("匹配对应的服务器[{0}], 当前频道数为 - [{1}], 频道变更后数量为 - [{2}]", guild.Id,
                _channelPoolManager.Channels.Count, newChannelIds.Count);
            // 更新账号的 频道id 列表
            Account.ChannelIds = newChannelIds;
            DbHelper.Instance.AccountStore.Update("ChannelIds", Account);
            ClearAccountCache(Account.Id);
            // 更新频道数据
            _channelPoolManager.UpdateChannelPool(newChannelIds);

            return Task.CompletedTask;
        }

        public Task OnDmChannelSubscribe(ConcurrentDictionary<string, DiscordChannelDto> channels)
        {
            if (!Account.EnableAutoFetchChannels) return Task.CompletedTask;
            _logger.Information("DmChannelSubscribe 私信频道事件触发，开始更新账号的私信频道数据");

            if (channels != null && channels.Count > 0)
            {
                var nijiBotChannelId = string.Empty;
                var privateChannelId = string.Empty;
                foreach (var channel in channels)
                {
                    if (channel.Value.RecipientsIds != null && channel.Value.RecipientsIds.Length > 0)
                    {
                        // 匹配 niji 机器人
                        if (channel.Value.RecipientsIds.Contains(Constants.NIJI_APPLICATION_ID))
                        {
                            _logger.Information("匹配 NIJI 机器人私信频道: {0}", channel.Key);
                            nijiBotChannelId = channel.Key;
                        }
                        // 匹配 midjourney 机器人
                        if (channel.Value.RecipientsIds.Contains(Constants.MJ_APPLICATION_ID))
                        {
                            _logger.Information("匹配 MIDJOURNEY 机器人私信频道: {0}", channel.Key);
                            privateChannelId = channel.Key;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(nijiBotChannelId) && !string.IsNullOrEmpty(privateChannelId))
                {
                    Account.NijiBotChannelId = nijiBotChannelId;
                    Account.PrivateChannelId = privateChannelId;
                    DbHelper.Instance.AccountStore.Update("NijiBotChannelId,PrivateChannelId", Account);
                    ClearAccountCache(Account.Id);
                }
            }


            return Task.CompletedTask;
        }


        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                _logger.Information("Discord实例 {0}-{1} 开始释放资源", Account?.GuildId, Account?.Remark);

                // 清除缓存
                ClearAccountCache(Account?.Id);

                WebSocketStarter?.CloseSocket();

                // 任务取消
                _longToken.Cancel();

                // 清理频道资源
                _channelPoolManager.Dispose();

                _logger.Information("Discord实例 {0}-{1} 资源释放完成", Account?.GuildId, Account?.Remark);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Discord实例 {0}-{1} 释放资源时发生异常", Account?.GuildId, Account?.Remark);
            }
        }

        /// <summary>
        /// 移除指定任务（包括队列和运行中的任务）
        /// </summary>
        /// <param name="task">要移除的任务</param>
        /// <param name="reason">失败原因，如果不为空则将任务标记为失败</param>
        public void RemoveTask(TaskInfo task, string reason = null)
        {
            if (task == null)
            {
                return;
            }

            // 如果提供了失败原因，则标记任务为失败
            if (!string.IsNullOrWhiteSpace(reason))
            {
                task.Fail(reason);
            }

            // 遍历所有频道，查找并移除任务
            foreach (var channel in _channelPoolManager.Channels)
            {
                // 使用频道的RemoveTask方法移除任务
                channel.RemoveTask(task.Id, reason);
            }

            // 无论任务是否被移除，都通知任务状态变化
            SaveAndNotify(task);
        }

    }
}