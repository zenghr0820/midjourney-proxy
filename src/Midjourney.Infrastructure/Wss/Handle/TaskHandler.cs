using Serilog;
using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.LoadBalancer;

namespace Midjourney.Infrastructure.Wss.Handle
{
    public static class TaskConstants
    {
        public const string TASK_PROPERTY_UPSCALE_INDEX = "upscale_index";
    }

    public class TaskHandler()
    {
        /// <summary>
        /// 查找任务并更新任务完成
        /// </summary>
        /// <param name="instance">实例</param>
        /// <param name="message">消息包装器</param>
        /// <param name="finalPrompt">最终提示词</param>
        /// <param name="action">任务动作</param>
        /// <returns>任务</returns>
        public TaskInfo FindAndFinishTask(
            DiscordInstance instance,
            TaskAction action,
            string finalPrompt,
            MessageWrapper message)
        {
            var task = FindTask(instance, message, action, finalPrompt);

            if (task == null || task.Status == TaskStatus.SUCCESS || task.Status == TaskStatus.FAILURE)
            {
                return null;
            }

            UpdateTaskProperties(task, message, finalPrompt);

            FinishTask(task, message);
            task.Awake();

            return task;
        }

        /// <summary>
        /// 查找放大任务并更新任务完成  
        /// </summary>
        /// <param name="instance">实例</param>
        /// <param name="message">消息包装器</param>
        /// <param name="finalPrompt">最终提示词</param>
        /// <param name="index">放大任务索引</param>
        /// <returns>任务</returns>
        public TaskInfo FindAndFinishUTask(
            DiscordInstance instance,
            MessageWrapper message,
            string finalPrompt,
            int index)
        {
            var task = FindTask(instance, message, TaskAction.MIX_UPSCALE, finalPrompt);

            if (task == null || task.Status == TaskStatus.SUCCESS || task.Status == TaskStatus.FAILURE)
            {
                return null;
            }

            UpdateTaskProperties(task, message, finalPrompt);

            FinishTask(task, message);
            task.Awake();

            return task;
        }


        /// <summary>
        /// 查找图生文任务并更新任务完成
        /// </summary>
        /// <param name="instance">实例</param>
        /// <param name="message">消息包装器</param>
        /// <param name="finalPrompt"></param>
        /// <returns>任务</returns>
        public TaskInfo FindAndFinishDescribeTask(
            DiscordInstance instance,
            MessageWrapper message,
            string finalPrompt)
        {
            var task = FindTask(instance, message, TaskAction.DESCRIBE, finalPrompt);

            if (task == null)
            {
                return null;
            }

            UpdateDescribeTaskProperties(task, message);

            FinishTask(task, message);
            task.Awake();

            return task;
        }

        /// <summary>
        /// 查找任务
        /// </summary>
        /// <param name="instance">实例</param>
        /// <param name="message">消息包装器</param>
        /// <param name="action">动作</param>
        /// <param name="finalPrompt">最终提示词</param>
        /// <returns>任务</returns>
        public TaskInfo FindTask(
            DiscordInstance instance,
            MessageWrapper message,
            TaskAction action,
            string finalPrompt)
        {
            var messageId = message.Id;
            var interactionMetadataId = message.InteractionId;
            var fullPrompt = message.FullPrompt;
            var botType = message.BotType;

            // 使用??=操作符简化任务查找逻辑
            TaskInfo task = null;

            // 1. 通过消息ID查找
            task = instance.FindRunningTask(c =>
                (c.Status == TaskStatus.IN_PROGRESS || c.Status == TaskStatus.SUBMITTED) &&
                c.MessageId == messageId).FirstOrDefault();

            // 2. 通过交互元数据ID查找
            if (task == null && !string.IsNullOrEmpty(interactionMetadataId))
            {
                task = FindTaskByInteractionId(instance, interactionMetadataId);

                // 如果通过 meta id 找到任务，但是 full prompt 为空，则更新 full prompt
                if (task != null && string.IsNullOrWhiteSpace(task.PromptFull))
                {
                    task.PromptFull = fullPrompt;
                }
            }
            

            // 3. 通过完整提示词查找
            task ??= FindTaskByFullPrompt(instance, fullPrompt, botType);

            // 如果 finalPrompt 为空，则直接返回任务
            if (string.IsNullOrWhiteSpace(finalPrompt))
            {
                return task;
            }

            // 4. 通过格式化提示词查找
            task ??= FindTaskByPrompt(instance, finalPrompt, botType, action, true);

            // 5. 通过参数化提示词查找
            task ??= FindTaskByPrompt(instance, finalPrompt, botType, action, false);

            // 6. 通过 show job 查找任务
            task ??= FindTaskByShowJob(instance, message.MessageHash, botType, action);

            return task;
        }

        /// <summary>
        /// 通过消息ID查找任务
        /// </summary>
        /// <param name="instance">实例</param>
        /// <param name="messageId">消息ID</param>
        /// <returns>任务</returns>
        public TaskInfo FindTaskByMessageId(DiscordInstance instance, string messageId)
        {
            return instance.FindRunningTask(c => c.MessageId == messageId).FirstOrDefault();
        }

        /// <summary>
        /// 通过交互ID查找任务
        /// </summary>
        /// <param name="instance">实例</param>
        /// <param name="interactionId">交互ID</param>
        /// <returns>任务</returns>
        public TaskInfo FindTaskByInteractionId(DiscordInstance instance, string interactionId)
        {
            return instance.FindRunningTask(c => (c.Status == TaskStatus.IN_PROGRESS || c.Status == TaskStatus.SUBMITTED) && 
                            c.InteractionMetadataId == interactionId).FirstOrDefault();
        }

        /// <summary>
        /// 通过完整提示词查找任务
        /// </summary>
        /// <param name="instance">实例</param>
        /// <param name="fullPrompt">完整提示词</param>
        /// <param name="botType">机器人类型</param>
        /// <returns>任务信息</returns>
        public TaskInfo FindTaskByFullPrompt(DiscordInstance instance, string fullPrompt, EBotType botType)
        {
            if (string.IsNullOrWhiteSpace(fullPrompt))
                return null;

            return instance.FindRunningTask(c =>
                (c.Status == TaskStatus.IN_PROGRESS || c.Status == TaskStatus.SUBMITTED) &&
                (c.BotType == botType || c.RealBotType == botType) &&
                c.PromptFull == fullPrompt)
                .OrderBy(c => c.StartTime)
                .FirstOrDefault();
        }

        /// <summary>
        /// 通过提示词查找任务
        /// </summary>
        /// <param name="instance">实例</param>
        /// <param name="prompt">提示词</param>
        /// <param name="botType">机器人类型</param>
        /// <param name="action">动作</param>
        /// <param name="useFormatPrompt">是否使用格式化提示词</param>
        /// <returns>任务信息</returns>
        private TaskInfo FindTaskByPrompt(DiscordInstance instance, string prompt, EBotType botType, TaskAction action, bool useFormatPrompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return null;

            var formattedPrompt = useFormatPrompt ? prompt.FormatPrompt() : prompt.FormatPromptParam();


            if (useFormatPrompt && string.IsNullOrWhiteSpace(formattedPrompt) && action != TaskAction.MIX_UPSCALE)
            {
                // 如果最终提示词为空，则可能是重绘、混图等任务
                return instance
                     .FindRunningTask(c => (c.Status == TaskStatus.IN_PROGRESS || c.Status == TaskStatus.SUBMITTED)
                     && (c.BotType == botType || c.RealBotType == botType) && c.Action == action)
                     .OrderBy(c => c.StartTime).FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(formattedPrompt))
            {
                return instance.FindRunningTask(c =>
                                            (c.Status == TaskStatus.IN_PROGRESS || c.Status == TaskStatus.SUBMITTED) &&
                                            (c.BotType == botType || c.RealBotType == botType) &&
                                            !string.IsNullOrWhiteSpace(c.PromptEn) &&
                                            (c.PromptEn.FormatPrompt() == formattedPrompt ||
                                             c.PromptEn.FormatPrompt().EndsWith(formattedPrompt) ||
                                             formattedPrompt.StartsWith(c.PromptEn.FormatPrompt())))
                                            .OrderBy(c => c.StartTime)
                                            .FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// 通过 show job 查找任务
        /// </summary>
        /// <param name="instance">实例</param>
        /// <param name="messageHash">消息哈希</param>
        /// <param name="botType">机器人类型</param>
        /// <param name="action">动作</param>
        /// <returns>任务</returns>
        private static TaskInfo FindTaskByShowJob(DiscordInstance instance, string messageHash, EBotType botType, TaskAction action)
        {
            if (string.IsNullOrWhiteSpace(messageHash) || action != TaskAction.SHOW)
                return null;

            return instance.FindRunningTask(c => (c.Status == TaskStatus.IN_PROGRESS || c.Status == TaskStatus.SUBMITTED) &&
            (c.BotType == botType || c.RealBotType == botType) && c.Action == TaskAction.SHOW && c.JobId == messageHash).OrderBy(c => c.StartTime).FirstOrDefault();
        }

        /// <summary>
        /// 更新任务字段
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="message">消息包装器</param>
        /// <param name="finalPrompt">最终提示词</param>
        public void UpdateTaskProperties(TaskInfo task, MessageWrapper message, string finalPrompt)
        {
            var messageId = message.Id;
            var messageHash = message.MessageHash;

            task.MessageId = messageId;
            if (!task.MessageIds.Contains(messageId))
                task.MessageIds.Add(messageId);

            task.SetProperty(Constants.MJ_MESSAGE_HANDLED, true);
            task.SetProperty(Constants.TASK_PROPERTY_FINAL_PROMPT, finalPrompt);
            task.SetProperty(Constants.TASK_PROPERTY_MESSAGE_HASH, messageHash);
            task.SetProperty(Constants.TASK_PROPERTY_MESSAGE_CONTENT, message.Content);

            task.ImageUrl = message.ImageUrl();
            task.JobId = messageHash;
        }

        /// <summary>
        /// 更新图生文任务字段
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="message">消息包装器</param>
        public void UpdateDescribeTaskProperties(TaskInfo task, MessageWrapper message)
        {
            var imageUrl = message.Embeds.First().Image?.Url;
            var messageHash = DiscordHelper.GetMessageHash(imageUrl);
            var finalPrompt = message.Embeds.First().Description;
            task.PromptEn = finalPrompt;
            task.MessageId = message.Id;

            if (!task.MessageIds.Contains(message.Id))
                task.MessageIds.Add(message.Id);

            task.SetProperty(Constants.MJ_MESSAGE_HANDLED, true);
            task.SetProperty(Constants.TASK_PROPERTY_FINAL_PROMPT, finalPrompt);
            task.SetProperty(Constants.TASK_PROPERTY_MESSAGE_HASH, messageHash);

            task.ImageUrl = imageUrl;
            task.JobId = messageHash;

        }

        /// <summary>
        /// 完成任务成功时更新字段
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="message"></param>
        public void FinishTask(TaskInfo task, MessageWrapper message)
        {
            // 设置图片信息
            var image = message.Attachments?.FirstOrDefault();
            if (task != null && image != null)
            {
                task.Width = image.Width;
                task.Height = image.Height;
                task.Url = image.Url;
                task.ProxyUrl = image.ProxyUrl;
                task.Size = image.Size;
                task.ContentType = image.ContentType;
            }

            task.SetProperty(Constants.TASK_PROPERTY_MESSAGE_ID, message.Id);
            task.SetProperty(Constants.TASK_PROPERTY_FLAGS, Convert.ToInt32(message.Flags));
            task.SetProperty(Constants.TASK_PROPERTY_MESSAGE_HASH, DiscordHelper.GetMessageHash(task.ImageUrl));

            task.Buttons = message.Components.SelectMany(x => x.Components)
                .Select(btn =>
                {
                    return new CustomComponentModel
                    {
                        CustomId = btn.CustomId ?? string.Empty,
                        Emoji = btn.Emoji?.Name ?? string.Empty,
                        Label = btn.Label ?? string.Empty,
                        Style = (int?)btn.Style ?? 0,
                        Type = (int?)btn.Type ?? 0,
                    };
                }).Where(c => c != null && !string.IsNullOrWhiteSpace(c.CustomId)).ToList();

            if (string.IsNullOrWhiteSpace(task.Description))
            {
                task.Description = "Submit success";
            }

            if (string.IsNullOrWhiteSpace(task.FailReason))
            {
                task.FailReason = "";
            }

            if (string.IsNullOrWhiteSpace(task.State))
            {
                task.State = "";
            }

            task.Success();

            Log.Debug("由 {@0} 确认消息处理完成 {@1}", message.MessageHandler, message.Id);
        }

        /// <summary>
        /// 选中放大任务成功时更新字段
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="index">放大任务索引</param>
        private static void FinishUTask(TaskInfo task, int index)
        {
            task.Action = TaskAction.UPSCALE;
            task.Status = TaskStatus.SUCCESS;
            task.SetProperty("end_time", DateTime.Now);
            task.SetProperty(TaskConstants.TASK_PROPERTY_UPSCALE_INDEX, index);
            task.SetProperty("finished", true);
        }

       
    }
}