using Midjourney.Infrastructure.Services;
using Serilog;

namespace Midjourney.Infrastructure.Wss.Handle;

/// <summary>
/// Nonce 随机数处理器
/// </summary>
public class NonceHandler : BaseMessageHandler
{
    public override string MessageHandleType => "NonceHandler";

    protected override bool CanHandle(MessageWrapper message)
    {
        // 获取交互元数据
        if (!string.IsNullOrWhiteSpace(message.Nonce))
        {
            return true;
        }
        return false;
    }

    protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
    {
        var id = message.Id;
        var nonce = message.Nonce;
        var isPrivareChannel = message.GetProperty<bool>("isPrivareChannel", false);

        Log.Debug($"用户消息, {messageType}, id: {id}, nonce: {nonce}");

        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(nonce))
        {
            // 设置 none 对应的任务 id
            var task = instance.GetRunningTaskByNonce(nonce);
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
                        && !string.IsNullOrWhiteSpace(message.CustomId))
                    {
                        task.SetProperty(Constants.TASK_PROPERTY_IFRAME_MODAL_CREATE_CUSTOM_ID, message.CustomId);

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
                        if (!string.IsNullOrWhiteSpace(message.Content) && message.Content.Contains("(Waiting to start)"))
                        {
                            if (string.IsNullOrWhiteSpace(task.PromptFull))
                            {
                                task.PromptFull = ConvertUtils.GetFullPrompt(message.Content);
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