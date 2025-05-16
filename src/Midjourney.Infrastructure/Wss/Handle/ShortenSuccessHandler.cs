using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.LoadBalancer;
using Midjourney.Infrastructure.Util;
using Serilog;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用的缩短提示词成功处理程序
    /// </summary>
    public class ShortenSuccessHandler : MessageHandler
    {
        public ShortenSuccessHandler()
        {
        }

        public override int Order() => 68888;

        public override string MessageHandleType => "ShortenSuccessHandler";

        /// <summary>
        /// 处理缩短提示词成功消息
        /// </summary>
        protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
        {

            if (message.InteractionMetadata?.Name != "shorten"
                && message.Embeds?.FirstOrDefault()?.Footer?.Text.Contains("Click on a button to imagine one of the shortened prompts") != true)
            {
                return;
            }

            if (MessageParser.IsWaitingToStart(message.Content))
            {
                return;
            }

            // 判断消息是否处理过了
            CacheHelper<string, bool>.TryAdd(message.Id, false);
            if (CacheHelper<string, bool>.Get(message.Id))
            {
                Log.Debug("消息已经处理过了 {@0}", message.Id);
                return;
            }

            if (messageType == MessageType.CREATE
                && message.Author.Bot == true
                && message.Author.Username.Contains("journey Bot", StringComparison.OrdinalIgnoreCase))
            {
                // 分析 prompt 完成
                if (message.Embeds.Count > 0)
                {
                    var msgId = message.Id;

                    var task = taskHandler.FindTaskByMessageId(instance, msgId);

                    task ??= taskHandler.FindTaskByInteractionId(instance, message.InteractionMetadata.Id);

                    if (task == null)
                    {
                        return;
                    }

                    var desc = message.Embeds.First().Description;

                    task.Description = desc;
                    task.MessageId = msgId;

                    if (!task.MessageIds.Contains(msgId))
                        task.MessageIds.Add(msgId);

                    task.SetProperty(Constants.MJ_MESSAGE_HANDLED, true);
                    task.SetProperty(Constants.TASK_PROPERTY_FINAL_PROMPT, desc);

                    taskHandler.FinishTask(task, message);
                    task.Awake();
                }
            }
        }
    }
}