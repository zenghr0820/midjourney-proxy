using System.Text.Json;
using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.LoadBalancer;
using Midjourney.Infrastructure.Util;
using Serilog;

namespace Midjourney.Infrastructure.Wss.Handle;

/// <summary>
/// 嵌入数据处理器
/// </summary>
public class EmbedDataHandler : BaseMessageHandler
{
    public override string MessageHandleType => "EmbedDataHandler";

    protected override bool CanHandle(MessageWrapper message)
    {
        if (message.Embeds?.Count > 0 || message.GatewayData.TryGetProperty("embeds", out _))
        {
            return true;
        }
        return false;
    }

    protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
    {
        var account = instance.Account;
        // 获取元数据
        var metadata = message.InteractionMetadata;
        var metaId = metadata?.Id;
        //  获取嵌入数据
        var embeds = message.Embeds;
        if (embeds?.Count <= 0 && message.GatewayData.TryGetProperty("embeds", out var ems))
        {
            List<EventDataEmbed> embedList = ems.Deserialize<List<EventDataEmbed>>();
            if (embedList != null)
            {
                embeds.AddRange(embedList);
            }
        }

        foreach (var item in embeds)
        {
            HandleEmbed(instance, messageType, message, item, account, metadata, metaId);
        }
    }

    private void HandleEmbed(DiscordInstance instance, MessageType messageType, MessageWrapper message,
        EventDataEmbed embed, DiscordAccount account, InteractionMetadata metadata, string metaId)
    {
        // 判断账号是否用量已经用完
        var title = embed.Title;
        // 16711680 error, 65280 success, 16776960 warning
        var color = embed.Color;
        // 描述
        var desc = embed.Description;

        Log.Information($"embeds 消息, {messageType}, {account.GetDisplay()} - id: {message.Id}, mid: {message.InteractionMetadata?.Id}, {message?.Author?.Username}, embeds: {title}, {color}, {desc}");

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
            HandleCreditsExhausted(instance, message, account, metaId);
            return;
        }
        // 临时禁止/订阅取消/订阅过期/订阅暂停
        else if (IsAccountDisableTitle(title))
        {
            HandleAccountDisable(instance, message, account, title, desc, metaId);
            return;
        }
        // 执行中的任务已满（一般超过 3 个时）
        else if (title == "Job queued")
        {
            HandleJobQueued(instance, messageType, message, title, desc);
        }
        // 暂时跳过的业务处理
        else if (continueTitles.Contains(title))
        {
            Log.Warning("跳过 embeds {@0}, {@1}", account.GetDisplay(), message.GatewayData.ToString());
        }
        // 其他错误消息
        else if (IsErrorTitle(title, color, errorTitles))
        {
            HandleErrorMessage(instance, messageType, message, metadata, metaId, title, desc);
        }
        // 未知消息
        else
        {
            HandleUnknownMessage(instance, messageType, message, title, desc);
        }
    }

    private bool IsAccountDisableTitle(string title)
    {
        return title == "Pending mod message"
            || title == "Blocked"
            || title == "Plan Cancelled"
            || title == "Subscription required"
            || title == "Subscription paused";
    }

    private bool IsErrorTitle(string title, int color, string[] errorTitles)
    {
        return errorTitles.Contains(title)
            || color == 16711680
            || title.Contains("Invalid")
            || title.Contains("error")
            || title.Contains("denied");
    }

    private void HandleCreditsExhausted(DiscordInstance instance, MessageWrapper message, DiscordAccount account, string metaId)
    {
        // 你的处理逻辑
        Log.Information($"账号 {account.GetDisplay()} 用量已经用完");

        var task = instance.FindRunningTask(c => c.MessageId == message.Id).FirstOrDefault();
        if (task == null && !string.IsNullOrWhiteSpace(metaId))
        {
            task = instance.FindRunningTask(c => c.InteractionMetadataId == metaId).FirstOrDefault();
        }

        if (task != null)
        {
            task.Fail("账号用量已经用完");
        }

        // 标记快速模式已经用完了
        account.FastExhausted = true;

        // 自动设置慢速，如果快速用完
        if (account.FastExhausted == true && account.EnableAutoSetRelax == true)
        {
            account.AllowModes = new List<GenerationSpeedMode>() { GenerationSpeedMode.RELAX };

            if (account.CoreSize > 3)
            {
                account.CoreSize = 3;
            }
        }

        DbHelper.Instance.AccountStore.Update("AllowModes,FastExhausted,CoreSize", account);
        instance?.ClearAccountCache(account.Id);

        // 如果开启自动切换慢速模式
        if (account.EnableFastToRelax == true)
        {
            HandleSwitchToRelaxMode(instance, account);
        }
        else
        {
            HandleDisableAccount(instance, account, "账号用量已经用完");
        }
    }

    private void HandleSwitchToRelaxMode(DiscordInstance instance, DiscordAccount account)
    {
        // 切换到慢速模式
        // 加锁切换到慢速模式
        // 执行切换慢速命令
        // 如果当前不是慢速，则切换慢速，加锁切换
        if (account.MjFastModeOn || account.NijiFastModeOn)
        {
            _ = AsyncLocalLock.TryLockAsync($"relax:{account.ChannelId}", TimeSpan.FromSeconds(5), async () =>
            {
                try
                {
                    Thread.Sleep(2500);
                    await instance?.RelaxAsync(SnowFlake.NextId(), EBotType.MID_JOURNEY);

                    Thread.Sleep(2500);
                    await instance?.RelaxAsync(SnowFlake.NextId(), EBotType.NIJI_JOURNEY);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "切换慢速异常 {@0}", account.GetDisplay());
                }
            });
        }
    }

    private void HandleDisableAccount(DiscordInstance instance, DiscordAccount account, string reason)
    {
        Log.Warning($"账号 {account.GetDisplay()} {reason}, 自动禁用账号");

        // 5s 后禁用账号
        Task.Run(() =>
        {
            try
            {
                Thread.Sleep(5 * 1000);

                // 保存
                account.Enable = false;
                account.DisabledReason = reason;

                DbHelper.Instance.AccountStore.Update(account);
                instance?.ClearAccountCache(account.Id);
                instance?.Dispose();

                // 发送邮件
                EmailJob.Instance.EmailSend(GlobalConfiguration.Setting?.Smtp, $"MJ账号禁用通知-{account.GetDisplay()}",
                    $"{account.GetDisplay()}, {account.DisabledReason}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "账号禁用异常 {@0}", account.GetDisplay());
            }
        });
    }

    private void HandleAccountDisable(DiscordInstance instance, MessageWrapper message, DiscordAccount account, string title, string desc, string metaId)
    {
        Log.Warning($"账号 {account.GetDisplay()} {title}, 自动禁用账号");

        var task = instance.FindRunningTask(c => c.MessageId == message.Id).FirstOrDefault();
        if (task == null && !string.IsNullOrWhiteSpace(metaId))
        {
            task = instance.FindRunningTask(c => c.InteractionMetadataId == metaId).FirstOrDefault();
        }

        if (task != null)
        {
            task.Fail(title);
        }

        // 组合禁用原因
        string disableReason = $"{title}, {desc}";
        HandleDisableAccount(instance, account, disableReason);
    }

    private void HandleJobQueued(DiscordInstance instance, MessageType messageType, MessageWrapper message, string title, string desc)
    {
        if (message.GatewayData.TryGetProperty("nonce", out JsonElement noneEle))
        {
            var nonce = noneEle.GetString();
            if (!string.IsNullOrWhiteSpace(message.Id) && !string.IsNullOrWhiteSpace(nonce))
            {
                // 设置 none 对应的任务 id
                var task = instance.GetRunningTaskByNonce(nonce);
                if (task != null && messageType == MessageType.CREATE)
                {
                    // 不需要赋值
                    //task.MessageId = id;

                    task.Description = $"{title}, {desc}";

                    if (!task.MessageIds.Contains(message.Id))
                    {
                        task.MessageIds.Add(message.Id);
                    }
                }
            }
        }
    }

    private void HandleErrorMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message, InteractionMetadata metadata, string metaId, string title, string desc)
    {
        if (!string.IsNullOrWhiteSpace(message.Nonce))
        {
            // 设置 none 对应的任务 id
            var task = instance.GetRunningTaskByNonce(message.Nonce);
            if (task != null)
            {
                // 需要用户同意 Tos
                if (title.Contains("Tos not accepted"))
                {
                    if (HandleTosNotAccepted(instance, message, task))
                    {
                        return;
                    }
                }

                var error = $"{title}, {desc}";

                task.MessageId = message.Id;
                task.Description = error;

                if (!task.MessageIds.Contains(message.Id))
                {
                    task.MessageIds.Add(message.Id);
                }

                task.Fail(error);
            }
        }
        else
        {
            HandleErrorWithoutNonce(instance, messageType, message, metadata, metaId, title, desc);
        }
    }

    private bool HandleTosNotAccepted(DiscordInstance instance, MessageWrapper message, TaskInfo task)
    {
        try
        {
            var tosData = message.EventData;
            var customId = tosData?.Components?.SelectMany(x => x.Components)
                .Where(x => x.Label == "Accept ToS")
                .FirstOrDefault()?.CustomId;

            if (!string.IsNullOrWhiteSpace(customId))
            {
                var nonce2 = SnowFlake.NextId();
                var tosRes = instance.ActionAsync(message.Id, customId, tosData.Flags, nonce2, task)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                if (tosRes?.Code == ReturnCode.SUCCESS)
                {
                    Log.Information("处理 Tos 成功 {@0}", instance.Account.GetDisplay());
                    return true;
                }
                else
                {
                    Log.Information("处理 Tos 失败 {@0}, {@1}", instance.Account.GetDisplay(), tosRes);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "处理 Tos 异常 {@0}", instance.Account.GetDisplay());
        }
        return false;
    }

    private void HandleErrorWithoutNonce(DiscordInstance instance, MessageType messageType, MessageWrapper message, InteractionMetadata metadata, string metaId, string title, string desc)
    {
        // 如果 meta 是 show
        // 说明是 show 任务出错了
        if (metadata?.Name == "show" && !string.IsNullOrWhiteSpace(desc))
        {
            // 设置 none 对应的任务 id
            var task = instance.GetRunningTasks().Where(c => c.Action == TaskAction.SHOW && desc.Contains(c.JobId)).FirstOrDefault();
            if (task != null && messageType == MessageType.CREATE)
            {
                var error = $"{title}, {desc}";

                task.MessageId = message.Id;
                task.Description = error;

                if (!task.MessageIds.Contains(message.Id))
                {
                    task.MessageIds.Add(message.Id);
                }

                task.Fail(error);
            }
        }
        else
        {
            // 没有获取到 none 尝试使用 mid 获取 task
            var task = instance.GetRunningTasks()
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
                Log.Error("未知 embeds 错误 {@0}, {@1}", instance.Account.GetDisplay(), message.GatewayData.ToString());
            }
        }
    }

    private void HandleUnknownMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message, string title, string desc)
    {
        if (!string.IsNullOrWhiteSpace(message.Id) && !string.IsNullOrWhiteSpace(message.Nonce))
        {
            // 设置 none 对应的任务 id
            var task = instance.GetRunningTaskByNonce(message.Nonce);
            if (task != null && messageType == MessageType.CREATE)
            {
                task.MessageId = message.Id;
                task.Description = $"{title}, {desc}";

                if (!task.MessageIds.Contains(message.Id))
                {
                    task.MessageIds.Add(message.Id);
                }

                Log.Warning($"未知消息: {title}, {desc}, {instance.Account.GetDisplay()}");
            }
        }
    }
}