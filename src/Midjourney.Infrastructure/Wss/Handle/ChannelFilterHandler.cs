using System.Text.Json;
using System.Text.RegularExpressions;
using Midjourney.Infrastructure.Services;
using Serilog;

namespace Midjourney.Infrastructure.Wss.Handle;

/// <summary>
/// 根据频道ID过滤消息处理器
/// </summary>
public class ChannelFilterHandler : BaseMessageHandler
{
    public override string MessageHandleType => "ChannelFilterHandler";

    protected override bool CanHandle(MessageWrapper message)
    {
        // 获取序列化的事件数据
        var data = message.GatewayData;
        if (data.TryGetProperty("channel_id", out JsonElement channelIdElement))
        {
            message.SetProperty("channelId", channelIdElement.GetString());
            return true;
        }
        return false;
    }

    protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
    {
        var account = instance.Account;
        var isPrivareChannel = false;
        var cid = message.GetProperty<string>("channelId");
        if (cid == account.PrivateChannelId || cid == account.NijiBotChannelId)
        {
            isPrivareChannel = true;
        }

        if (instance.AllChannelIds.Contains(cid))
        {
            isPrivareChannel = false;
        }

        message.SetProperty("isPrivareChannel", isPrivareChannel);

        // 如果不是私信频道，也不是当前实例下的频道列表消息，也不是子频道消息则忽略
        if (!isPrivareChannel && !instance.AllChannelIds.Contains(cid)
            && !account.SubChannelValues.ContainsKey(cid))
        {
            Log.Information("当前服务器：{0}, 匹配不到频道ID: {1}, 已忽略该数据 - {2}", instance.GuildId, cid, message.Id);
            // 标记消息已处理
            message.HasHandle = true;
            return;
        }

        // 日志输出：当前服务器和匹配的频道ID
        Log.Information("当前服务器：{0}, 匹配的频道ID: {1}", instance.GuildId, cid);

        // 处理私信频道
        if (isPrivareChannel)
        {
            ProcessPrivateChannelMessage(instance, message);
            message.HasHandle = true;
        }

    }

    /// <summary>
    /// 处理私信频道消息
    /// </summary>
    /// <param name="instance">服务器实例</param>
    /// <param name="message">消息</param>
    private void ProcessPrivateChannelMessage(DiscordInstance instance, MessageWrapper message)
    {
        var id = message.Id;
        var content = message.Content;

        // 定义正则表达式模式
        // "**girl**\n**Job ID**: 6243686b-7ab1-4174-a9fe-527cca66a829\n**seed** 1259687673"
        var pattern = @"\*\*Job ID\*\*:\s*(?<jobId>[a-fA-F0-9-]{36})\s*\*\*seed\*\*\s*(?<seed>\d+)";

        // 创建正则表达式对象
        var regex = new Regex(pattern);

        // 尝试匹配输入字符串
        var match = regex.Match(content);

        if (match.Success)
        {
            // 提取Job ID和seed
            var jobId = match.Groups["jobId"].Value;
            var seed = match.Groups["seed"].Value;

            // 如果Job ID和seed不为空，则更新任务的seed
            if (!string.IsNullOrWhiteSpace(jobId) && !string.IsNullOrWhiteSpace(seed))
            {
                // 根据 jobId 查找任务，更新任务的seed
                // var task = instance.FindRunningTask(c => c.GetProperty<string>(Constants.TASK_PROPERTY_MESSAGE_HASH, default) == jobId).FirstOrDefault();
                var task = DbHelper.Instance.TaskStore.Where(t => t.JobId == jobId).FirstOrDefault();
                if (task != null)
                {
                    if (!task.MessageIds.Contains(id))
                    {
                        task.MessageIds.Add(id);
                    }

                    task.Seed = seed;
                    DbHelper.Instance.TaskStore.Update("Seed", task);
                }
            }
        }
        else
        {
            // 处理附件信息
            ProcessAttachments(instance, message);
        }

    }

    /// <summary>
    /// 处理附件信息
    /// </summary>
    /// <param name="instance">服务器实例</param>
    /// <param name="message">消息</param>
    private void ProcessAttachments(DiscordInstance instance, MessageWrapper message)
    {
        if (message.Attachments?.Count > 0)
        {
            var attachment = message.Attachments.First();
            var imgUrl = attachment.Url;

            if (!string.IsNullOrWhiteSpace(imgUrl))
            {
                var hash = DiscordHelper.GetMessageHash(imgUrl);
                if (!string.IsNullOrWhiteSpace(hash))
                {
                    var task = instance.FindRunningTask(c =>
                        c.GetProperty<string>(Constants.TASK_PROPERTY_MESSAGE_HASH, default) == hash).FirstOrDefault();

                    if (task != null)
                    {
                        if (!task.MessageIds.Contains(message.Id))
                        {
                            task.MessageIds.Add(message.Id);
                        }
                        task.SeedMessageId = message.Id;
                    }
                }
            }
        }
    }
}