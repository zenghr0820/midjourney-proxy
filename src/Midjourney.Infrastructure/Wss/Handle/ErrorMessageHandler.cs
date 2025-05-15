using Midjourney.Infrastructure.LoadBalancer;
using Serilog;
namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用的错误消息处理程序
    /// </summary>
    public class ErrorMessageHandler : MessageHandler
    {

        public override int Order() => 2;

        public override string MessageHandleType => "Error-Message-Handler";

        /// <summary>
        /// 处理通用消息
        /// </summary>
        protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
        {

            // 不需要处理，因为已经处理了
            return;

             /*
            var content = GetMessageContent(message);
            var msgId = GetMessageId(message);
            if (content.StartsWith("Failed"))
            {
                var task = instance.GetRunningTaskByMessageId(msgId);

                if (task == null && message.InteractionMetadata?.Id != null)
                {
                    task = instance.FindRunningTask(c => c.InteractionMetadataId == message.InteractionMetadata.Id.ToString()).FirstOrDefault();
                }

                if (task != null)
                {
                    task.MessageId = msgId;

                    if (!task.MessageIds.Contains(msgId))
                        task.MessageIds.Add(msgId);

                    // mj官方异常
                    task.SetProperty(Constants.MJ_MESSAGE_HANDLED, true);
                    task.Fail(content);
                    task.Awake();
                }
                return;
            }

            var embedsOptional = message.Embeds;
            if (embedsOptional == null || !embedsOptional.Any())
                return;

            var embed = embedsOptional.FirstOrDefault();
            string title = embed.Title;
            if (string.IsNullOrWhiteSpace(title)) return;

            string description = embed.Description;
            string footerText = embed.Footer?.Text ?? string.Empty;
            var color = embed.Color?.RawValue ?? 0;

            if (color == 16239475)
            {
                _logger.LogWarning($"{instance.GetInstanceId} - MJ警告信息: {title}\n{description}\nfooter: {footerText}");
            }
            else if (color == 16711680)
            {
                _logger.LogError($"{instance.GetInstanceId} - MJ异常信息: {title}\n{description}\nfooter: {footerText}");

                var taskInfo = FindTaskWhenError(instance, messageType, message);
                if (taskInfo == null && message.InteractionMetadata?.Id != null)
                {
                    taskInfo = instance.FindRunningTask(c => c.InteractionMetadataId == message.InteractionMetadata.Id.ToString()).FirstOrDefault();
                }

                if (taskInfo != null)
                {
                    taskInfo.MessageId = msgId;

                    if (!taskInfo.MessageIds.Contains(msgId))
                        taskInfo.MessageIds.Add(msgId);

                    taskInfo.SetProperty(Constants.MJ_MESSAGE_HANDLED, true);
                    taskInfo.Fail($"[{title}] {description}");
                    taskInfo.Awake();
                }
            }
            else
            {
                if (embed.Type == Discord.EmbedType.Link || string.IsNullOrWhiteSpace(description))
                    return;

                var taskInfo = FindTaskWhenError(instance, messageType, message);
                if (taskInfo == null && message.InteractionMetadata?.Id != null)
                {
                    taskInfo = instance.FindRunningTask(c => c.InteractionMetadataId == message.InteractionMetadata.Id.ToString()).FirstOrDefault();
                }

                if (taskInfo != null)
                {
                    taskInfo.MessageId = msgId;

                    if (!taskInfo.MessageIds.Contains(msgId))
                        taskInfo.MessageIds.Add(msgId);

                    _logger.LogWarning($"{instance.GetInstanceId} - MJ可能的异常信息: {title}\n{description}\nfooter: {footerText}");

                    taskInfo.SetProperty(Constants.MJ_MESSAGE_HANDLED, true);
                    taskInfo.Fail($"[{title}] {description}");
                    taskInfo.Awake();
                }
            }


            */
        }
    }
} 