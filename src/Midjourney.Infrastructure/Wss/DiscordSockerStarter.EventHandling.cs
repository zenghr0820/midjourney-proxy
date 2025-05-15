using System.Collections.Concurrent;
using Midjourney.Infrastructure.Wss.Handle;
using Serilog;
using System.Text.Json;
using System.Threading.Channels;
using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.Util;
using RestSharp;
using System.Net;
using System.Text.RegularExpressions;
using Midjourney.Infrastructure.Wss.Gateway;
using System.Threading.Tasks;
using Midjourney.Infrastructure.LoadBalancer;

namespace Midjourney.Infrastructure.Wss
{
    partial class DiscordSockerStarter
    {
        // private readonly ILogger _logger;
        private readonly string _logContext;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _processingTask;

        // 使用Channel替代ConcurrentQueue，提高消息处理效率
        private readonly Channel<JsonElement> _messageChannel = Channel.CreateUnbounded<JsonElement>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        private readonly IEnumerable<MessageHandler> _messageHandlers;
        private readonly ProxyProperties _properties;

        private readonly ConcurrentDictionary<string, DiscordChannelDto> _dmChannels = new();
        private readonly ConcurrentDictionary<string, DiscordExtendedGuild> _guilds = new();



        /// <summary>
        /// 添加消息到队列
        /// </summary>
        /// <param name="message">JSON消息</param>
        private void EnqueueMessage(JsonElement message)
        {
            if (_messageChannel.Writer.TryWrite(message))
            {
                _logger.Debug("[{0}] - 消息已加入队列", _logContext);
            }
            else
            {
                _logger.Warning("[{0}] - 消息加入队列失败", _logContext);
            }
        }

        /// <summary>
        /// 处理网关消息
        /// </summary>
        /// <param name="raw"></param>
        private async Task HandleGatewayEnvent(JsonElement raw)
        {
            if (!raw.TryGetProperty("t", out var messageTypeElement))
            {
                return;
            }

            // 过滤消息
            var messageType = MessageTypeExtensions.Of(messageTypeElement.GetString());
            if (messageType is null or MessageType.DELETE || !raw.TryGetProperty("d", out var data))
            {
                return;
            }

            var gatewayMessage = raw.Deserialize<DiscordSocketMessage>();
            var type = gatewayMessage.Type;
            var opCode = gatewayMessage.OperationCode;

            _logger.Information("handle [{0}] Gateway Event", type);

            try
            {
                switch (type)
                {
                    #region Guilds
                    case "GUILD_CREATE":
                        {
                            _logger.Debug("Received Dispatch (GUILD_CREATE) - Try Update Guild Data -");
                            var guild = data.Deserialize<DiscordExtendedGuild>();
                            if (guild is not null)
                            {

                                _logger.Debug("- Update Guild[{0}] Data, channelIds count [{1}] -", guild.Id, guild?.Channels.Length);
                                _guilds.AddOrUpdate(guild.Id, guild, (id, old) => guild);
                            }
                        }
                        break;
                    case "GUILD_UPDATE":
                        {
                            _logger.Debug("Received Dispatch (GUILD_UPDATE)- Try Update Guild Data -");
                            var guild = data.Deserialize<DiscordExtendedGuild>();
                            if (guild is not null)
                            {

                                _logger.Debug("- Update Guild[{0}] Data, channelIds count [{1}] -", guild.Id, guild?.Channels.Length);
                                _guilds.AddOrUpdate(guild.Id, guild, (id, old) => guild);
                            }
                        }
                        break;
                    case "GUILD_DELETE":
                        {
                            _logger.Debug("Received Dispatch (GUILD_DELETE)- Try DELETE Guild Data -");
                            var guild = data.Deserialize<DiscordExtendedGuild>();
                            if (guild != null)
                            {
                                _logger.Information("- DELETE Guild[{0}] Data, channelIds count [{1}] -", guild.Id, guild?.Channels.Length);
                                // 判断是否该账号下的Guild 实例被删除
                                if (guild.Id == _discordInstance.GuildId)
                                {
                                    // TODO 通知账号的服务器被删除 - 事件订阅
                                }
                                _guilds.TryRemove(guild.Id, out guild);
                            }
                        }
                        break;
                    #endregion
                    #region Channel
                    case "CHANNEL_CREATE":
                    case "CHANNEL_DELETE":
                        {
                            _logger.Debug("Received Dispatch ({0})- Try DELETE Channel Data -", type);
                            var channel = data.Deserialize<DiscordChannelDto>();
                            // 获取对应的服务器数据
                            if (channel != null && _guilds.TryGetValue(channel.GuildId, out var guild) && guild is not null)
                            {
                                _logger.Information("服务器 - {0} - 变更频道 [{@1}] - {@2}", guild?.Id, channel.Id, channel.Name);
                                var newChannels = new List<DiscordChannelDto>(guild.Channels);
                                if (messageType == MessageType.CHANNEL_CREATE)
                                {
                                    newChannels.Add(channel);
                                }
                                else
                                {
                                    newChannels.Remove(channel);
                                }
                                guild.Channels = newChannels.ToArray();
                                _guilds.AddOrUpdate(guild.Id, guild, (id, old) => guild);
                                // 触发事件订阅
                                await _channelSubscribeEvent.InvokeAsync(_guilds);
                            }
                        }
                        break;
                    case "CHANNEL_UPDATE":
                        {
                            _logger.Debug("Received Dispatch (CHANNEL_UPDATE)- Try Update Channel Data -");
                        }
                        break;
                    #endregion
                    #region 触发 CF 真人验证
                    case "INTERACTION_IFRAME_MODAL_CREATE":
                        {
                            if (data.TryGetProperty("title", out var t))
                            {
                                if (t.GetString() == "Action required to continue")
                                {
                                    _logger.Warning("CF 验证 {@0}, {@1}", Account.ChannelId, raw.ToString());
                                    var cfData = data.Deserialize<EventData>();
                                    var handler = _messageHandlers.FirstOrDefault(x => x.GetType() == typeof(CloudflareHandle));
                                    // 交给 CloudflareHandle 处理
                                    handler?.Handle(_discordInstance, MessageType.CREATE, cfData);
                                    return;
                                }
                            }
                        }
                        break;
                    #endregion
                    default:
                        // 内容
                        var contentStr = string.Empty;
                        if (data.TryGetProperty("content", out JsonElement content))
                        {
                            contentStr = content.GetString();
                        }

                        // 作者
                        var authorName = string.Empty;
                        var authId = string.Empty;
                        if (data.TryGetProperty("author", out JsonElement author)
                            && author.TryGetProperty("username", out JsonElement username)
                            && author.TryGetProperty("id", out JsonElement uid))
                        {
                            authorName = username.GetString();
                            authId = uid.GetString();
                        }

                        // 应用 ID 即机器人 ID
                        var applicationId = string.Empty;
                        if (data.TryGetProperty("application_id", out JsonElement application))
                        {
                            applicationId = application.GetString();
                        }

                        // 交互元数据 id
                        var metaId = string.Empty;
                        var metaName = string.Empty;
                        if (data.TryGetProperty("interaction_metadata", out JsonElement meta) && meta.TryGetProperty("id", out var mid))
                        {
                            metaId = mid.GetString();

                            metaName = meta.TryGetProperty("name", out var n) ? n.GetString() : string.Empty;
                        }

                        // 处理 remix 开关
                        if (metaName == "prefer remix" && !string.IsNullOrWhiteSpace(contentStr))
                        {
                            // MJ
                            if (authId == Constants.MJ_APPLICATION_ID)
                            {
                                if (contentStr.StartsWith("Remix mode turned off"))
                                {
                                    foreach (var item in Account.Components)
                                    {
                                        foreach (var sub in item.Components)
                                        {
                                            if (sub.Label == "Remix mode")
                                            {
                                                sub.Style = 2;
                                            }
                                        }
                                    }
                                }
                                else if (contentStr.StartsWith("Remix mode turned on"))
                                {
                                    foreach (var item in Account.Components)
                                    {
                                        foreach (var sub in item.Components)
                                        {
                                            if (sub.Label == "Remix mode")
                                            {
                                                sub.Style = 3;
                                            }
                                        }
                                    }
                                }
                            }
                            // NIJI
                            else if (authId == Constants.NIJI_APPLICATION_ID)
                            {
                                if (contentStr.StartsWith("Remix mode turned off"))
                                {
                                    foreach (var item in Account.NijiComponents)
                                    {
                                        foreach (var sub in item.Components)
                                        {
                                            if (sub.Label == "Remix mode")
                                            {
                                                sub.Style = 2;
                                            }
                                        }
                                    }
                                }
                                else if (contentStr.StartsWith("Remix mode turned on"))
                                {
                                    foreach (var item in Account.NijiComponents)
                                    {
                                        foreach (var sub in item.Components)
                                        {
                                            if (sub.Label == "Remix mode")
                                            {
                                                sub.Style = 3;
                                            }
                                        }
                                    }
                                }
                            }

                            DbHelper.Instance.AccountStore.Update("Components,NijiComponents", Account);
                            _discordInstance.ClearAccountCache(Account.Id);

                            return;
                        }
                        // 同步 settings 和 remix
                        else if (metaName == "settings")
                        {
                            // settings 指令
                            var eventDataMsg = data.Deserialize<EventData>();
                            if (eventDataMsg != null && eventDataMsg.InteractionMetadata?.Name == "settings" && eventDataMsg.Components?.Count > 0)
                            {
                                if (applicationId == Constants.NIJI_APPLICATION_ID)
                                {
                                    Account.NijiComponents = eventDataMsg.Components;
                                    DbHelper.Instance.AccountStore.Update("NijiComponents", Account);
                                    _discordInstance.ClearAccountCache(Account.Id);
                                }
                                else if (applicationId == Constants.MJ_APPLICATION_ID)
                                {
                                    Account.Components = eventDataMsg.Components;
                                    DbHelper.Instance.AccountStore.Update("Components", Account);
                                    _discordInstance.ClearAccountCache(Account.Id);
                                }
                            }
                        }
                        // 切换 fast 和 relax
                        else if (metaName == "fast" || metaName == "relax" || metaName == "turbo")
                        {
                            // MJ
                            // Done! Your jobs now do not consume fast-hours, but might take a little longer. You can always switch back with /fast
                            if (metaName == "fast" && contentStr.StartsWith("Done!"))
                            {
                                foreach (var item in Account.Components)
                                {
                                    foreach (var sub in item.Components)
                                    {
                                        if (sub.Label == "Fast mode")
                                        {
                                            sub.Style = 2;
                                        }
                                        else if (sub.Label == "Relax mode")
                                        {
                                            sub.Style = 2;
                                        }
                                        else if (sub.Label == "Turbo mode")
                                        {
                                            sub.Style = 3;
                                        }
                                    }
                                }
                                foreach (var item in Account.NijiComponents)
                                {
                                    foreach (var sub in item.Components)
                                    {
                                        if (sub.Label == "Fast mode")
                                        {
                                            sub.Style = 2;
                                        }
                                        else if (sub.Label == "Relax mode")
                                        {
                                            sub.Style = 2;
                                        }
                                        else if (sub.Label == "Turbo mode")
                                        {
                                            sub.Style = 3;
                                        }
                                    }
                                }
                            }
                            else if (metaName == "turbo" && contentStr.StartsWith("Done!"))
                            {
                                foreach (var item in Account.Components)
                                {
                                    foreach (var sub in item.Components)
                                    {
                                        if (sub.Label == "Fast mode")
                                        {
                                            sub.Style = 3;
                                        }
                                        else if (sub.Label == "Relax mode")
                                        {
                                            sub.Style = 2;
                                        }
                                        else if (sub.Label == "Turbo mode")
                                        {
                                            sub.Style = 2;
                                        }
                                    }
                                }
                                foreach (var item in Account.NijiComponents)
                                {
                                    foreach (var sub in item.Components)
                                    {
                                        if (sub.Label == "Fast mode")
                                        {
                                            sub.Style = 3;
                                        }
                                        else if (sub.Label == "Relax mode")
                                        {
                                            sub.Style = 2;
                                        }
                                        else if (sub.Label == "Turbo mode")
                                        {
                                            sub.Style = 2;
                                        }
                                    }
                                }
                            }
                            else if (metaName == "relax" && contentStr.StartsWith("Done!"))
                            {
                                foreach (var item in Account.Components)
                                {
                                    foreach (var sub in item.Components)
                                    {
                                        if (sub.Label == "Fast mode")
                                        {
                                            sub.Style = 2;
                                        }
                                        else if (sub.Label == "Relax mode")
                                        {
                                            sub.Style = 3;
                                        }
                                        else if (sub.Label == "Turbo mode")
                                        {
                                            sub.Style = 2;
                                        }
                                    }
                                }
                                foreach (var item in Account.NijiComponents)
                                {
                                    foreach (var sub in item.Components)
                                    {
                                        if (sub.Label == "Fast mode")
                                        {
                                            sub.Style = 2;
                                        }
                                        else if (sub.Label == "Relax mode")
                                        {
                                            sub.Style = 3;
                                        }
                                        else if (sub.Label == "Turbo mode")
                                        {
                                            sub.Style = 2;
                                        }
                                    }
                                }
                            }

                            DbHelper.Instance.AccountStore.Update("Components,NijiComponents", Account);
                            _discordInstance.ClearAccountCache(Account.Id);

                            return;
                        }

                        // 私信频道
                        var isPrivareChannel = false;
                        if (data.TryGetProperty("channel_id", out JsonElement channelIdElement))
                        {
                            var cid = channelIdElement.GetString();
                            if (cid == Account.PrivateChannelId || cid == Account.NijiBotChannelId)
                            {
                                isPrivareChannel = true;
                            }

                            if (_discordInstance.AllChannelIds.Contains(cid))
                            {
                                isPrivareChannel = false;
                            }

                            // 都不相同
                            // 如果有渠道 id，但不是当前渠道 id，则忽略
                            if (!_discordInstance.AllChannelIds.Contains(cid)
                                && cid != Account.PrivateChannelId
                                && cid != Account.NijiBotChannelId)
                            {
                                // 如果也不是子频道 id, 则忽略
                                if (!Account.SubChannelValues.ContainsKey(cid))
                                {
                                    return;
                                }
                            }
                        }

                        if (isPrivareChannel)
                        {
                            // 私信频道
                            if (messageType == MessageType.CREATE && data.TryGetProperty("id", out JsonElement subIdElement))
                            {
                                var id = subIdElement.GetString();

                                // 定义正则表达式模式
                                // "**girl**\n**Job ID**: 6243686b-7ab1-4174-a9fe-527cca66a829\n**seed** 1259687673"
                                var pattern = @"\*\*Job ID\*\*:\s*(?<jobId>[a-fA-F0-9-]{36})\s*\*\*seed\*\*\s*(?<seed>\d+)";

                                // 创建正则表达式对象
                                var regex = new Regex(pattern);

                                // 尝试匹配输入字符串
                                var match = regex.Match(contentStr);

                                if (match.Success)
                                {
                                    // 提取 Job ID 和 seed
                                    var jobId = match.Groups["jobId"].Value;
                                    var seed = match.Groups["seed"].Value;

                                    if (!string.IsNullOrWhiteSpace(jobId) && !string.IsNullOrWhiteSpace(seed))
                                    {
                                        var task = _discordInstance.FindRunningTask(c => c.GetProperty<string>(Constants.TASK_PROPERTY_MESSAGE_HASH, default) == jobId).FirstOrDefault();
                                        if (task != null)
                                        {
                                            if (!task.MessageIds.Contains(id))
                                            {
                                                task.MessageIds.Add(id);
                                            }

                                            task.Seed = seed;
                                        }
                                    }
                                }
                                else
                                {
                                    // 获取附件对象 attachments 中的第一个对象的 url 属性
                                    // seed 消息处理
                                    if (data.TryGetProperty("attachments", out JsonElement attachments) && attachments.ValueKind == JsonValueKind.Array)
                                    {
                                        if (attachments.EnumerateArray().Count() > 0)
                                        {
                                            var item = attachments.EnumerateArray().First();

                                            if (item.ValueKind != JsonValueKind.Null
                                                && item.TryGetProperty("url", out JsonElement url)
                                                && url.ValueKind != JsonValueKind.Null)
                                            {
                                                var imgUrl = url.GetString();
                                                if (!string.IsNullOrWhiteSpace(imgUrl))
                                                {
                                                    var hash = DiscordHelper.GetMessageHash(imgUrl);
                                                    if (!string.IsNullOrWhiteSpace(hash))
                                                    {
                                                        var task = _discordInstance.FindRunningTask(c => c.GetProperty<string>(Constants.TASK_PROPERTY_MESSAGE_HASH, default) == hash).FirstOrDefault();
                                                        if (task != null)
                                                        {
                                                            if (!task.MessageIds.Contains(id))
                                                            {
                                                                task.MessageIds.Add(id);
                                                            }
                                                            task.SeedMessageId = id;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            return;
                        }

                        // 任务 id
                        // 任务 nonce
                        if (data.TryGetProperty("id", out JsonElement idElement))
                        {
                            var id = idElement.GetString();

                            _logger.Information($"用户消息, {messageType}, {Account.GetDisplay()} - id: {id}, mid: {metaId}, {authorName}, content: {contentStr}");

                            var isEm = data.TryGetProperty("embeds", out var em);
                            if ((messageType == MessageType.CREATE || messageType == MessageType.UPDATE) && isEm)
                            {
                                if (metaName == "info" && messageType == MessageType.UPDATE)
                                {
                                    // info 指令
                                    if (em.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (JsonElement item in em.EnumerateArray())
                                        {
                                            if (item.TryGetProperty("title", out var emtitle) && emtitle.GetString().Contains("Your info"))
                                            {
                                                if (item.TryGetProperty("description", out var description))
                                                {
                                                    var dic = ParseDiscordData(description.GetString());
                                                    foreach (var d in dic)
                                                    {
                                                        if (d.Key == "Job Mode")
                                                        {
                                                            if (applicationId == Constants.NIJI_APPLICATION_ID)
                                                            {
                                                                Account.SetProperty($"Niji {d.Key}", d.Value);
                                                            }
                                                            else if (applicationId == Constants.MJ_APPLICATION_ID)
                                                            {
                                                                Account.SetProperty(d.Key, d.Value);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            Account.SetProperty(d.Key, d.Value);
                                                        }
                                                    }

                                                    var db = DbHelper.Instance.AccountStore;
                                                    Account.InfoUpdated = DateTime.Now;

                                                    db.Update("InfoUpdated,Properties", Account);
                                                    _discordInstance?.ClearAccountCache(Account.Id);
                                                }
                                            }
                                        }
                                    }

                                    return;
                                }
                                else if (metaName == "settings" && data.TryGetProperty("components", out var components))
                                {
                                    // settings 指令
                                    var eventDataMsg = data.Deserialize<EventData>();
                                    if (eventDataMsg != null && eventDataMsg.InteractionMetadata?.Name == "settings" && eventDataMsg.Components?.Count > 0)
                                    {
                                        if (applicationId == Constants.NIJI_APPLICATION_ID)
                                        {
                                            Account.NijiComponents = eventDataMsg.Components;
                                            Account.NijiSettingsMessageId = id;

                                            DbHelper.Instance.AccountStore.Update("NijiComponents,NijiSettingsMessageId", Account);
                                            _discordInstance?.ClearAccountCache(Account.Id);
                                        }
                                        else if (applicationId == Constants.MJ_APPLICATION_ID)
                                        {
                                            Account.Components = eventDataMsg.Components;
                                            Account.SettingsMessageId = id;

                                            DbHelper.Instance.AccountStore.Update("Components,SettingsMessageId", Account);
                                            _discordInstance?.ClearAccountCache(Account.Id);
                                        }
                                    }

                                    return;
                                }

                                // em 是一个 JSON 数组
                                if (em.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (JsonElement item in em.EnumerateArray())
                                    {
                                        if (item.TryGetProperty("title", out var emTitle))
                                        {
                                            // 判断账号是否用量已经用完
                                            var title = emTitle.GetString();

                                            // 16711680 error, 65280 success, 16776960 warning
                                            var color = item.TryGetProperty("color", out var colorEle) ? colorEle.GetInt32() : 0;

                                            // 描述
                                            var desc = item.GetProperty("description").GetString();

                                            _logger.Information($"用户 embeds 消息, {messageType}, {Account.GetDisplay()} - id: {id}, mid: {metaId}, {authorName}, embeds: {title}, {color}, {desc}");

                                            // 无效参数、违规的提示词、无效提示词
                                            var errorTitles = new[] {
                                        "Invalid prompt", // 无效提示词
                                        "Invalid parameter", // 无效参数
                                        "Banned prompt detected", // 违规提示词
                                        "Invalid link", // 无效链接
                                        "Request cancelled due to output filters",
                                        "Queue full", // 执行中的队列已满
                                    };

                                            // 跳过的 title
                                            var continueTitles = new[] { "Action needed to continue" };

                                            // fast 用量已经使用完了
                                            if (title == "Credits exhausted")
                                            {
                                                // 你的处理逻辑
                                                _logger.Information($"账号 {Account.GetDisplay()} 用量已经用完");

                                                var task = _discordInstance.FindRunningTask(c => c.MessageId == id).FirstOrDefault();
                                                if (task == null && !string.IsNullOrWhiteSpace(metaId))
                                                {
                                                    task = _discordInstance.FindRunningTask(c => c.InteractionMetadataId == metaId).FirstOrDefault();
                                                }

                                                if (task != null)
                                                {
                                                    task.Fail("账号用量已经用完");
                                                }

                                                // 标记快速模式已经用完了
                                                Account.FastExhausted = true;

                                                // 自动设置慢速，如果快速用完
                                                if (Account.FastExhausted == true && Account.EnableAutoSetRelax == true)
                                                {
                                                    Account.AllowModes = new List<GenerationSpeedMode>() { GenerationSpeedMode.RELAX };

                                                    if (Account.CoreSize > 3)
                                                    {
                                                        Account.CoreSize = 3;
                                                    }
                                                }

                                                DbHelper.Instance.AccountStore.Update("AllowModes,FastExhausted,CoreSize", Account);
                                                _discordInstance?.ClearAccountCache(Account.Id);

                                                // 如果开启自动切换慢速模式
                                                if (Account.EnableFastToRelax == true)
                                                {
                                                    // 切换到慢速模式
                                                    // 加锁切换到慢速模式
                                                    // 执行切换慢速命令
                                                    // 如果当前不是慢速，则切换慢速，加锁切换
                                                    if (Account.MjFastModeOn || Account.NijiFastModeOn)
                                                    {
                                                        _ = AsyncLocalLock.TryLockAsync($"relax:{Account.GuildId}", TimeSpan.FromSeconds(5), async () =>
                                                        {
                                                            try
                                                            {
                                                                Thread.Sleep(2500);
                                                                await _discordInstance?.RelaxAsync(SnowFlake.NextId(), EBotType.MID_JOURNEY);

                                                                Thread.Sleep(2500);
                                                                await _discordInstance?.RelaxAsync(SnowFlake.NextId(), EBotType.NIJI_JOURNEY);
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                _logger.Error(ex, "切换慢速异常 {@0}", Account.GuildId);
                                                            }
                                                        });
                                                    }
                                                }
                                                else
                                                {
                                                    // 你的处理逻辑
                                                    _logger.Warning($"账号 {Account.GetDisplay()} 用量已经用完, 自动禁用账号");

                                                    // 5s 后禁用账号
                                                    _ = Task.Run(() =>
                                                    {
                                                        try
                                                        {
                                                            Thread.Sleep(5 * 1000);

                                                            // 保存
                                                            Account.Enable = false;
                                                            Account.DisabledReason = "账号用量已经用完";

                                                            DbHelper.Instance.AccountStore.Update(Account);
                                                            _discordInstance?.ClearAccountCache(Account.Id);
                                                            _discordInstance?.Dispose();


                                                            // 发送邮件
                                                            EmailJob.Instance.EmailSend(_properties.Smtp, $"MJ账号禁用通知-{Account.GuildId}",
                                                                $"{Account.GuildId}, {Account.DisabledReason}");
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Log.Error(ex, "账号用量已经用完, 禁用账号异常 {@0}", Account.GuildId);
                                                        }
                                                    });
                                                }

                                                return;
                                            }
                                            // 临时禁止/订阅取消/订阅过期/订阅暂停
                                            else if (title == "Pending mod message"
                                                || title == "Blocked"
                                                || title == "Plan Cancelled"
                                                || title == "Subscription required"
                                                || title == "Subscription paused")
                                            {
                                                // 你的处理逻辑
                                                _logger.Warning($"账号 {Account.GetDisplay()} {title}, 自动禁用账号");

                                                var task = _discordInstance.FindRunningTask(c => c.MessageId == id).FirstOrDefault();
                                                if (task == null && !string.IsNullOrWhiteSpace(metaId))
                                                {
                                                    task = _discordInstance.FindRunningTask(c => c.InteractionMetadataId == metaId).FirstOrDefault();
                                                }

                                                if (task != null)
                                                {
                                                    task.Fail(title);
                                                }

                                                // 5s 后禁用账号
                                                _ = Task.Run(() =>
                                                {
                                                    try
                                                    {
                                                        Thread.Sleep(5 * 1000);

                                                        // 保存
                                                        Account.Enable = false;
                                                        Account.DisabledReason = $"{title}, {desc}";

                                                        DbHelper.Instance.AccountStore.Update(Account);

                                                        _discordInstance?.ClearAccountCache(Account.Id);
                                                        _discordInstance?.Dispose();

                                                        // 发送邮件
                                                        EmailJob.Instance.EmailSend(_properties.Smtp, $"MJ账号禁用通知-{Account.GuildId}",
                                                            $"{Account.GuildId}, {Account.DisabledReason}");
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Log.Error(ex, "{@0}, 禁用账号异常 {@1}", title, Account.GuildId);
                                                    }
                                                });

                                                return;
                                            }
                                            // 执行中的任务已满（一般超过 3 个时）
                                            else if (title == "Job queued")
                                            {
                                                if (data.TryGetProperty("nonce", out JsonElement noneEle))
                                                {
                                                    var nonce = noneEle.GetString();
                                                    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(nonce))
                                                    {
                                                        // 设置 none 对应的任务 id
                                                        var task = _discordInstance.GetRunningTaskByNonce(nonce);
                                                        if (task != null)
                                                        {
                                                            if (messageType == MessageType.CREATE)
                                                            {
                                                                // 不需要赋值
                                                                //task.MessageId = id;

                                                                task.Description = $"{title}, {desc}";

                                                                if (!task.MessageIds.Contains(id))
                                                                {
                                                                    task.MessageIds.Add(id);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            // 暂时跳过的业务处理
                                            else if (continueTitles.Contains(title))
                                            {
                                                _logger.Warning("跳过 embeds {@0}, {@1}", Account.GuildId, data.ToString());
                                            }
                                            // 其他错误消息
                                            else if (errorTitles.Contains(title)
                                                || color == 16711680
                                                || title.Contains("Invalid")
                                                || title.Contains("error")
                                                || title.Contains("denied"))
                                            {

                                                if (data.TryGetProperty("nonce", out JsonElement noneEle))
                                                {
                                                    var nonce = noneEle.GetString();
                                                    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(nonce))
                                                    {
                                                        // 设置 none 对应的任务 id
                                                        var task = _discordInstance.GetRunningTaskByNonce(nonce);
                                                        if (task != null)
                                                        {
                                                            // 需要用户同意 Tos
                                                            if (title.Contains("Tos not accepted"))
                                                            {
                                                                try
                                                                {
                                                                    var tosData = data.Deserialize<EventData>();
                                                                    var customId = tosData?.Components?.SelectMany(x => x.Components)
                                                                        .Where(x => x.Label == "Accept ToS")
                                                                        .FirstOrDefault()?.CustomId;

                                                                    if (!string.IsNullOrWhiteSpace(customId))
                                                                    {
                                                                        var nonce2 = SnowFlake.NextId();
                                                                        var tosRes = _discordInstance.ActionAsync(id, customId, tosData.Flags, nonce2, task)
                                                                            .ConfigureAwait(false).GetAwaiter().GetResult();

                                                                        if (tosRes?.Code == ReturnCode.SUCCESS)
                                                                        {
                                                                            _logger.Information("处理 Tos 成功 {@0}", Account.GuildId);
                                                                            return;
                                                                        }
                                                                        else
                                                                        {
                                                                            _logger.Information("处理 Tos 失败 {@0}, {@1}", Account.GuildId, tosRes);
                                                                        }
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    _logger.Error(ex, "处理 Tos 异常 {@0}", Account.GuildId);
                                                                }
                                                            }

                                                            var error = $"{title}, {desc}";

                                                            task.MessageId = id;
                                                            task.Description = error;

                                                            if (!task.MessageIds.Contains(id))
                                                            {
                                                                task.MessageIds.Add(id);
                                                            }

                                                            task.Fail(error);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    // 如果 meta 是 show
                                                    // 说明是 show 任务出错了
                                                    if (metaName == "show" && !string.IsNullOrWhiteSpace(desc))
                                                    {
                                                        // 设置 none 对应的任务 id
                                                        var task = _discordInstance.GetRunningTasks().Where(c => c.Action == TaskAction.SHOW && desc.Contains(c.JobId)).FirstOrDefault();
                                                        if (task != null)
                                                        {
                                                            if (messageType == MessageType.CREATE)
                                                            {
                                                                var error = $"{title}, {desc}";

                                                                task.MessageId = id;
                                                                task.Description = error;

                                                                if (!task.MessageIds.Contains(id))
                                                                {
                                                                    task.MessageIds.Add(id);
                                                                }

                                                                task.Fail(error);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // 没有获取到 none 尝试使用 mid 获取 task
                                                        var task = _discordInstance.GetRunningTasks()
                                                            .Where(c => c.MessageId == metaId || c.MessageIds.Contains(metaId) || c.InteractionMetadataId == metaId)
                                                            .FirstOrDefault();
                                                        if (task != null)
                                                        {
                                                            var error = $"{title}, {desc}";
                                                            task.Fail(error);
                                                        }
                                                        else
                                                        {
                                                            // 如果没有获取到 none
                                                            _logger.Error("未知 embeds 错误 {@0}, {@1}", Account.GuildId, data.ToString());
                                                        }
                                                    }
                                                }
                                            }
                                            // 未知消息
                                            else
                                            {
                                                if (data.TryGetProperty("nonce", out JsonElement noneEle))
                                                {
                                                    var nonce = noneEle.GetString();
                                                    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(nonce))
                                                    {
                                                        // 设置 none 对应的任务 id
                                                        var task = _discordInstance.GetRunningTaskByNonce(nonce);
                                                        if (task != null)
                                                        {
                                                            if (messageType == MessageType.CREATE)
                                                            {
                                                                task.MessageId = id;
                                                                task.Description = $"{title}, {desc}";

                                                                if (!task.MessageIds.Contains(id))
                                                                {
                                                                    task.MessageIds.Add(id);
                                                                }

                                                                _logger.Warning($"未知消息: {title}, {desc}, {Account.GuildId}");
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }


                            if (data.TryGetProperty("nonce", out JsonElement noneElement))
                            {
                                var nonce = noneElement.GetString();

                                _logger.Debug($"用户消息, {messageType}, id: {id}, nonce: {nonce}");

                                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(nonce))
                                {
                                    // 设置 none 对应的任务 id
                                    var task = _discordInstance.GetRunningTaskByNonce(nonce);
                                    if (task != null && task.Status != TaskStatus.SUCCESS && task.Status != TaskStatus.FAILURE)
                                    {
                                        if (isPrivareChannel)
                                        {
                                            // 私信频道
                                        }
                                        else
                                        {
                                            // 绘画频道

                                            // MJ 交互成功后
                                            if (messageType == MessageType.INTERACTION_SUCCESS)
                                            {
                                                task.InteractionMetadataId = id;
                                            }
                                            // MJ 局部重绘完成后
                                            else if (messageType == MessageType.INTERACTION_IFRAME_MODAL_CREATE
                                                && data.TryGetProperty("custom_id", out var custom_id))
                                            {
                                                task.SetProperty(Constants.TASK_PROPERTY_IFRAME_MODAL_CREATE_CUSTOM_ID, custom_id.GetString());

                                                //task.MessageId = id;

                                                if (!task.MessageIds.Contains(id))
                                                {
                                                    task.MessageIds.Add(id);
                                                }
                                            }
                                            else
                                            {
                                                //task.MessageId = id;

                                                if (!task.MessageIds.Contains(id))
                                                {
                                                    task.MessageIds.Add(id);
                                                }
                                            }

                                            // 只有 CREATE 才会设置消息 id
                                            if (messageType == MessageType.CREATE)
                                            {
                                                task.MessageId = id;

                                                // 设置 prompt 完整词
                                                if (!string.IsNullOrWhiteSpace(contentStr) && contentStr.Contains("(Waiting to start)"))
                                                {
                                                    if (string.IsNullOrWhiteSpace(task.PromptFull))
                                                    {
                                                        task.PromptFull = ConvertUtils.GetFullPrompt(contentStr);
                                                    }
                                                }
                                            }

                                            // 如果任务是 remix 自动提交任务
                                            if (task.RemixAutoSubmit
                                                && task.RemixModaling == true
                                                && messageType == MessageType.INTERACTION_SUCCESS)
                                            {
                                                task.RemixModalMessageId = id;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        var eventData = data.Deserialize<EventData>();

                        // 如果消息类型是 CREATE
                        // 则再次处理消息确认事件，确保消息的高可用
                        if (messageType == MessageType.CREATE)
                        {
                            Thread.Sleep(50);

                            if (eventData != null &&
                                (_discordInstance.AllChannelIds.Contains(eventData.ChannelId) || Account.SubChannelValues.ContainsKey(eventData.ChannelId)))
                            {
                                foreach (var messageHandler in _messageHandlers.OrderBy(h => h.Order()))
                                {
                                    // 处理过了
                                    if (eventData.GetProperty<bool?>(Constants.MJ_MESSAGE_HANDLED, default) == true)
                                    {
                                        return;
                                    }

                                    // 消息加锁处理
                                    LocalLock.TryLock($"lock_{eventData.Id}", TimeSpan.FromSeconds(10), () =>
                                    {
                                        messageHandler.Handle(_discordInstance, messageType.Value, eventData);
                                    });
                                }
                            }
                        }
                        // describe 重新提交
                        // MJ::Picread::Retry
                        else if (eventData.Embeds.Count > 0 && eventData.Author?.Bot == true && eventData.Components.Count > 0
                            && eventData.Components.First().Components.Any(x => x.CustomId?.Contains("PicReader") == true))
                        {
                            // 消息加锁处理
                            LocalLock.TryLock($"lock_{eventData.Id}", TimeSpan.FromSeconds(10), () =>
                            {
                                var em = eventData.Embeds.FirstOrDefault();
                                if (em != null && !string.IsNullOrWhiteSpace(em.Description))
                                {
                                    var handler = _messageHandlers.FirstOrDefault(x => x.GetType() == typeof(DescribeSuccessHandler));
                                    handler?.Handle(_discordInstance, MessageType.CREATE, eventData);
                                }
                            });
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(eventData.Content)
                                  && eventData.Content.Contains("%")
                                  && eventData.Author?.Bot == true)
                            {
                                // 消息加锁处理
                                LocalLock.TryLock($"lock_{eventData.Id}", TimeSpan.FromSeconds(10), () =>
                                {
                                    var handler = _messageHandlers.FirstOrDefault(x => x.GetType() == typeof(StartAndProgressHandler));
                                    handler?.Handle(_discordInstance, MessageType.UPDATE, eventData);
                                });
                            }
                            else if (eventData.InteractionMetadata?.Name == "describe")
                            {
                                // 消息加锁处理
                                LocalLock.TryLock($"lock_{eventData.Id}", TimeSpan.FromSeconds(10), () =>
                                {
                                    var handler = _messageHandlers.FirstOrDefault(x => x.GetType() == typeof(DescribeSuccessHandler));
                                    handler?.Handle(_discordInstance, MessageType.CREATE, eventData);
                                });
                            }
                            else if (eventData.InteractionMetadata?.Name == "shorten"
                                // shorten show details -> PromptAnalyzerExtended
                                || eventData.Embeds?.FirstOrDefault()?.Footer?.Text.Contains("Click on a button to imagine one of the shortened prompts") == true)
                            {
                                // 消息加锁处理
                                LocalLock.TryLock($"lock_{eventData.Id}", TimeSpan.FromSeconds(10), () =>
                                {
                                    var handler = _messageHandlers.FirstOrDefault(x => x.GetType() == typeof(ShortenSuccessHandler));
                                    handler?.Handle(_discordInstance, MessageType.CREATE, eventData);
                                });
                            }
                        }
                        break;
                }

            }
            catch (Exception ex)
            {
                ex.Data["opcode"] = opCode;
                ex.Data["type"] = type;
                ex.Data["payload_data"] = data.ToString();
                _logger.Error(ex, $"Error handling {opCode}{(type != null ? $" ({type})" : "")}");
            }

        }

        /// <summary>
        /// 处理消息队列
        /// </summary>
        private async Task ProcessMessages()
        {
            try
            {
                _logger.Information("消息处理任务已启动 {@0}", _logContext);

                // 等待直到有消息可读或被取消
                while (await _messageChannel.Reader.WaitToReadAsync(_cancellationTokenSource.Token).ConfigureAwait(false))
                {
                    // 尝试读取所有可用消息
                    while (_messageChannel.Reader.TryRead(out var message))
                    {
                        try
                        {
                            //  事件处理
                            _ = HandleGatewayEnvent(message);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "处理消息时发生异常 {@0}", _logContext);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Information("消息处理任务已取消 {@0}", _logContext);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "消息处理任务异常 {@0}", _logContext);
            }
            finally
            {
                _logger.Information("消息处理任务已结束 {@0}", _logContext);
            }
        }

        /// <summary>
        /// 资源释放
        /// </summary>
        private void MessageProcessorDispose()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                _messageChannel.Writer.Complete();

                // 等待处理任务完成
                var waitTask = Task.WhenAny(_processingTask, Task.Delay(1000));
                waitTask.ConfigureAwait(false).GetAwaiter().GetResult();

                _cancellationTokenSource.Dispose();

                _logger.Information("消息处理器已释放 {@0}", _logContext);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "释放消息处理器时发生异常 {@0}", _logContext);
            }
        }


        private static Dictionary<string, string> ParseDiscordData(string input)
        {
            var data = new Dictionary<string, string>();

            foreach (var line in input.Split('\n'))
            {
                var parts = line.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Replace("**", "").Trim();
                    var value = parts[1].Trim();
                    data[key] = value;
                }
            }

            return data;
        }

    }
}
