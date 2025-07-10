using Midjourney.Infrastructure.Services;

namespace Midjourney.Infrastructure.Wss.Handle;

/// <summary>
/// 生成模式切换处理器
/// </summary>
public class GenerationModeHandler : BaseMessageHandler
{
    public override string MessageHandleType => "GenerationModeHandler";

    protected override bool CanHandle(MessageWrapper message)
    {
        // 获取交互元数据
        var interactionMetadata = message.InteractionMetadata;
        return (interactionMetadata?.Name == "fast" || interactionMetadata?.Name == "relax" || interactionMetadata?.Name == "turbo") &&
               !string.IsNullOrWhiteSpace(message.Content);
    }

    protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
    {
        // 获取交互元数据
        var interactionMetadata = message.InteractionMetadata;
        var metaName = interactionMetadata?.Name;
        var content = message.Content;
        var account = instance.Account;

        // MJ
        // Done! Your jobs now do not consume fast-hours, but might take a little longer. You can always switch back with /fast
        if (content.StartsWith("Done!"))
        {
            // 获取要应用的样式配置
            var styleConfig = GetStyleConfig(metaName);

            // 更新MJ组件样式
            UpdateComponentStyles(account.Components, styleConfig);

            // 更新NIJI组件样式
            UpdateComponentStyles(account.NijiComponents, styleConfig);

            // 更新数据库和缓存
            DbHelper.Instance.AccountStore.Update("Components,NijiComponents", account);
            instance.ClearAccountCache(account.Id);
        }

        // 标记消息已处理
        message.HasHandle = true;
    }

    private Dictionary<string, int> GetStyleConfig(string mode)
    {
        return mode switch
        {
            "fast" => new Dictionary<string, int> {
                { "Fast mode", 2 },
                { "Relax mode", 2 },
                { "Turbo mode", 3 }
            },
            "turbo" => new Dictionary<string, int> {
                { "Fast mode", 3 },
                { "Relax mode", 2 },
                { "Turbo mode", 2 }
            },
            "relax" => new Dictionary<string, int> {
                { "Fast mode", 2 },
                { "Relax mode", 3 },
                { "Turbo mode", 2 }
            },
            _ => new Dictionary<string, int>()
        };
    }

    private void UpdateComponentStyles(List<Component> components, Dictionary<string, int> styleConfig)
    {
        foreach (var item in components)
        {
            foreach (var sub in item.Components)
            {
                if (styleConfig.TryGetValue(sub.Label, out int style))
                {
                    sub.Style = style;
                }
            }
        }
    }
}