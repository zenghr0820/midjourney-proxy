
using System.Text.Json;
using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.LoadBalancer;

namespace Midjourney.Infrastructure.Wss.Handle;

/// <summary>
/// Midjourney 设置信息处理器
/// </summary>
public class SettingInfoHandler : BaseMessageHandler
{
    public override string MessageHandleType => "SettingInfoHandler";

    protected override bool CanHandle(MessageWrapper message)
    {
        // 获取交互元数据
        var interactionMetadata = message.InteractionMetadata;
        if (interactionMetadata?.Name == "settings" || interactionMetadata?.Name == "info")
        {
            return true;
        }
        return false;
    }

    protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
    {

        if (messageType != MessageType.CREATE && messageType != MessageType.UPDATE)
        {
            return;
        }

        //  处理 Info 指令
        if (message.InteractionMetadata?.Name == "info")
        {
            HandleInfo(instance, message);
        }
        //  处理 Settings 指令
        else if (message.InteractionMetadata?.Name == "settings")
        {
            HandleSettings(instance, message);
        }
        
        // 标记消息已处理
        message.HasHandle = true;
    }

    private void HandleInfo(DiscordInstance instance, MessageWrapper message)
    {
        var application = message.Application;
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

        foreach (var embed in embeds)
        {
            if (embed.Title.Contains("Your info"))
            {
                var description = embed.Description;
                var dic = ParseDiscordData(description);
                foreach (var d in dic)
                {
                    if (d.Key == "Job Mode")
                    {
                        if (application?.Id == Constants.NIJI_APPLICATION_ID)
                        {
                            instance.Account.SetProperty($"Niji {d.Key}", d.Value);
                        }
                        else if (application?.Id == Constants.MJ_APPLICATION_ID)
                        {
                            instance.Account.SetProperty(d.Key, d.Value);
                        }
                    }
                    else
                    {
                        instance.Account.SetProperty(d.Key, d.Value);
                    }
                }

                instance.Account.InfoUpdated = DateTime.Now;
                DbHelper.Instance.AccountStore.Update("InfoUpdated,Properties", instance.Account);
                instance.ClearAccountCache(instance.Account.Id);
            }
        }
    }

    private void HandleSettings(DiscordInstance instance, MessageWrapper message)
    {
        var eventData = message.EventData;
        var application = message.Application;
        var account = instance.Account;
        if (application?.Id == Constants.NIJI_APPLICATION_ID)
        {
            account.NijiComponents = eventData.Components;
            account.NijiSettingsMessageId = message.Id;
            DbHelper.Instance.AccountStore.Update("NijiComponents,NijiSettingsMessageId", account);
            instance.ClearAccountCache(account.Id);
        }
        else if (application?.Id == Constants.MJ_APPLICATION_ID)
        {
            account.Components = eventData.Components;
            account.SettingsMessageId = message.Id;
            DbHelper.Instance.AccountStore.Update("Components,SettingsMessageId", account);
            instance.ClearAccountCache(account.Id);
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