using Midjourney.Infrastructure.Services;
using Serilog;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 通用的任务开始和进度处理程序
    /// </summary>
    public class StartAndProgressHandler : MessageHandler
    {

        public StartAndProgressHandler()
        {
        }

        public override int Order() => 90;

        public override string MessageHandleType => "StartAndProgressHandler";

        /// <summary>
        /// 处理开始和进度消息
        /// </summary>
        protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
        {
            Log.Information("StartAndProgressHandler - Handling message:{0} {1}", MessageHandleType, messageType);
            try
            {
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

                string content = message.Content;
                string msgId = message.Id;
                var fullPrompt = message.FullPrompt;
                var parseData = ConvertUtils.ParseContent(content);

                if (messageType == MessageType.CREATE && !string.IsNullOrWhiteSpace(msgId))
                {
                    // 通过消息ID查找任务
                    var task = taskHandler.FindTaskByMessageId(instance, msgId);

                    task ??= taskHandler.FindTaskByInteractionId(instance, message.InteractionId);
                    if (task == null && !string.IsNullOrWhiteSpace(message.InteractionId))
                    {
                        task = taskHandler.FindTaskByInteractionId(instance, message.InteractionId);

                        // 如果通过 meta id 找到任务，但是 full prompt 为空，则更新 full prompt
                        if (task != null && string.IsNullOrWhiteSpace(task.PromptFull))
                        {
                            task.PromptFull = fullPrompt;
                        }
                    }

                    var botType = message.BotType;

                    task ??= taskHandler.FindTaskByFullPrompt(instance, fullPrompt, botType);

                    if (task == null || task.Status == TaskStatus.SUCCESS || task.Status == TaskStatus.FAILURE)
                    {
                        return;
                    }

                    if (!task.MessageIds.Contains(msgId))
                        task.MessageIds.Add(msgId);

                    task.SetProperty(Constants.MJ_MESSAGE_HANDLED, true);
                    task.SetProperty(Constants.TASK_PROPERTY_PROGRESS_MESSAGE_ID, message.Id);

                    // 兼容少数content为空的场景
                    if (parseData != null)
                    {
                        task.SetProperty(Constants.TASK_PROPERTY_FINAL_PROMPT, parseData.Prompt);
                    }
                    task.Status = TaskStatus.IN_PROGRESS;
                    task.Awake();
                }
                else if (messageType == MessageType.UPDATE && parseData != null)
                {
                    // 任务进度
                    if (parseData.Status == "Stopped")
                        return;

                    var task = taskHandler.FindTask(
                        instance,
                        message,
                        (TaskAction)(-1), // TODO 这里不知道传什么
                        null);

                    if (task == null || task.Status == TaskStatus.SUCCESS || task.Status == TaskStatus.FAILURE)
                    {
                        return;
                    }

                    if (!task.MessageIds.Contains(msgId))
                        task.MessageIds.Add(msgId);

                    task.SetProperty(Constants.MJ_MESSAGE_HANDLED, true);
                    task.SetProperty(Constants.TASK_PROPERTY_FINAL_PROMPT, parseData.Prompt);
                    task.Status = TaskStatus.IN_PROGRESS;
                    task.Progress = parseData.Status; // 这里 Progress 是 string 类型

                    string imageUrl = message.ImageUrl();

                    // 如果启用保存过程图片
                    if (GlobalConfiguration.Setting.EnableSaveIntermediateImage
                        && !string.IsNullOrWhiteSpace(imageUrl))
                    {
                        var ff = new FileFetchHelper();
                        var url = ff.FetchFileToStorageAsync(imageUrl).ConfigureAwait(false).GetAwaiter().GetResult();
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            imageUrl = url;
                        }

                        // 必须确保任务仍是 IN_PROGRESS 状态
                        if (task.Status == TaskStatus.IN_PROGRESS)
                        {
                            task.ImageUrl = imageUrl;
                            task.SetProperty(Constants.TASK_PROPERTY_MESSAGE_HASH, DiscordHelper.GetMessageHash(imageUrl));
                            task.Awake();
                        }
                    }
                    else
                    {
                        task.ImageUrl = imageUrl;
                        task.SetProperty(Constants.TASK_PROPERTY_MESSAGE_HASH, DiscordHelper.GetMessageHash(imageUrl));
                        task.Awake();
                    }
                }
                else
                {
                    Log.Debug("未处理的消息类型: {0}", messageType);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "处理进度消息异常");
            }
        }
    }
}