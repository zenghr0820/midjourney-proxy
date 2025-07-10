using Midjourney.Infrastructure.Services;
using Serilog;
using Serilog.Context;

namespace Midjourney.Infrastructure.Wss.Handle;

/// <summary>
/// 消息前置处理器 pre-processor
/// </summary>
public abstract class BaseMessageHandler
{
    protected readonly ILogger _logger = Log.Logger;
    protected BaseMessageHandler _nextHandler;

    /// <summary>
    /// 消息处理器类型
    /// </summary>
    public abstract string MessageHandleType { get; }

    /// <summary>
    /// 设置下一个处理器
    /// </summary>
    public BaseMessageHandler SetNext(BaseMessageHandler nextHandler)
    {
        _nextHandler = nextHandler;
        return nextHandler;
    }

    /// <summary>
    /// 处理消息
    /// </summary>
    public virtual void Handle(DiscordInstance instance, MessageType messageType, MessageWrapper message)
    {
        if (CanHandle(message))
        {
            using (LogContext.PushProperty("LogPrefix", MessageHandleType))
            {
                HandleMessage(instance, messageType, message);
                if (message.HasHandle)
                    return;
            }
        }

        _nextHandler?.Handle(instance, messageType, message);
    }

    /// <summary>
    /// 判断是否可以处理此消息
    /// </summary>
    protected abstract bool CanHandle(MessageWrapper message);

    /// <summary>
    /// 处理具体消息
    /// </summary>
    protected abstract void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message);
}