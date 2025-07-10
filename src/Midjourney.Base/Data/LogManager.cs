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

        // 直接使用ForContext添加的上下文属性，不再创建新的LogEvent
        // 这样可以确保日志前缀能够正确显示在控制台输出中
        _innerLogger.Write(logEvent);
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