using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;
using System.Linq;

public class PrefixLogger : ILogger
{
    private readonly ILogger _innerLogger;
    private readonly string _prefix;

    public PrefixLogger(ILogger innerLogger, string prefix)
    {
        _innerLogger = innerLogger.ForContext("HandlerName", prefix);
        _prefix = prefix;
    }

    public void Write(LogEvent logEvent)
    {
        if (logEvent == null) return;

        // 合并前缀到消息模板
        var originalTemplate = logEvent.MessageTemplate;
        var prefixedTemplate = new MessageTemplate(
            $"[{_prefix}] {originalTemplate.Text}",
            originalTemplate.Tokens
        );

        // 创建新的 LogEvent（继承原始属性）
        var newEvent = new LogEvent(
            logEvent.Timestamp,
            logEvent.Level,
            logEvent.Exception,
            prefixedTemplate,
            logEvent.Properties
                .Select(kv => new LogEventProperty(kv.Key, kv.Value))
                .ToList()
        );

        // 传递到内部 Logger
        _innerLogger.Write(newEvent);
    }

    // 实现其他必要接口方法
    public bool IsEnabled(LogEventLevel level) => _innerLogger.IsEnabled(level);

    public ILogger ForContext(ILogEventEnricher enricher)
        => new PrefixLogger(_innerLogger.ForContext(enricher), _prefix);

    public ILogger ForContext(string propertyName, object value, bool destructureObjects = false)
        => new PrefixLogger(_innerLogger.ForContext(propertyName, value, destructureObjects), _prefix);

    public ILogger ForContext<TSource>()
        => new PrefixLogger(_innerLogger.ForContext<TSource>(), _prefix);
}