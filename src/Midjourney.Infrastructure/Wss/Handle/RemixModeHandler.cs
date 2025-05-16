
using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.LoadBalancer;

namespace Midjourney.Infrastructure.Wss.Handle;

/// <summary>
/// Remix模式切换处理器
/// </summary>
public class RemixModeHandler : BaseMessageHandler
{
    public override string MessageHandleType => "RemixModeHandler";

    protected override bool CanHandle(MessageWrapper message)
    {
        // 获取交互元数据
        var interactionMetadata = message.InteractionMetadata;
        if (interactionMetadata == null)
        {
            return false;
        }
        var metaName = interactionMetadata.Name;
        return metaName == "prefer remix" && !string.IsNullOrWhiteSpace(message.Content);
    }

    protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
    {
        var authId = message.Author?.Id;
        var content = message.Content;
        var account = instance.Account;
        var mjComponents = account.Components;
        var nijiComponents = account.NijiComponents;

        // 处理MJ
        if (authId == Constants.MJ_APPLICATION_ID)
        {
            if (content.StartsWith("Remix mode turned on"))
            {
                UpdateRemixModeStyle(mjComponents, true);
            }
            else if (content.StartsWith("Remix mode turned off"))
            {
                UpdateRemixModeStyle(mjComponents, false);
            }
            
        }
        // 处理NIJI
        else if (authId == Constants.NIJI_APPLICATION_ID)
        {
             if (content.StartsWith("Remix mode turned on"))
            {
                UpdateRemixModeStyle(nijiComponents, true);
            }
            else if (content.StartsWith("Remix mode turned off"))
            {
                UpdateRemixModeStyle(nijiComponents, false);
            }
        }
        
        // 更新数据库和缓存
        DbHelper.Instance.AccountStore.Update("Components,NijiComponents", account);
        instance.ClearAccountCache(account.Id);
        
        // 标记消息已处理
        message.HasHandle = true;
    }
    
    /// <summary>
    /// 更新 Remix 模式样式
    /// </summary>
    /// <param name="components"></param>
    /// <param name="isOn"></param>
    private void UpdateRemixModeStyle(List<Component> components, bool isOn)
    {
        foreach (var item in components)
        {
            foreach (var sub in item.Components)
            {
                if (sub.Label == "Remix mode")
                {
                    sub.Style = isOn ? 3 : 2;
                }
            }
        }
    }
}