using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Midjourney.Infrastructure.Wss
{
    /// <summary>
    /// WebSocket心跳服务
    /// </summary>
    public class HeartbeatService : IDisposable
    {
        private readonly ILogger _logger;
        private readonly Func<Task> _heartbeatAction;
        private readonly string _logContext;
        private readonly int _heartbeatInterval;
        private readonly double _heartbeatFactor;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _heartbeatTask;
        private bool _heartbeatAck = true;
        private int _latency = 0;

        /// <summary>
        /// 最后一次接收消息的时间
        /// </summary>
        public long LastMessageTime { get; set; }

        /// <summary>
        /// 心跳延迟
        /// </summary>
        public int Latency => _latency;

        /// <summary>
        /// 心跳超时事件
        /// </summary>
        public event Action<string> HeartbeatTimedOut;

        /// <summary>
        /// 心跳Ack未确认事件
        /// </summary>
        public event Action<string> HeartbeatAckNotReceived;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="heartbeatAction">心跳回调</param>
        /// <param name="heartbeatInterval">心跳间隔</param>
        /// <param name="heartbeatFactor">心跳因子</param>
        /// <param name="logContext">日志上下文</param>
        public HeartbeatService(
            ILogger logger,
            Func<Task> heartbeatAction,
            int heartbeatInterval,
            double heartbeatFactor,
            string logContext)
        {
            _logger = logger;
            _heartbeatAction = heartbeatAction;
            _heartbeatInterval = heartbeatInterval;
            _heartbeatFactor = heartbeatFactor;
            _logContext = logContext;
            LastMessageTime = Environment.TickCount;
        }

        /// <summary>
        /// 启动心跳服务
        /// </summary>
        public void Start()
        {
            if (_heartbeatTask != null)
            {
                Stop();
            }

            _heartbeatAck = true;
            _latency = 0;
            LastMessageTime = Environment.TickCount;
            _cancellationTokenSource = new CancellationTokenSource();
            _heartbeatTask = RunHeartbeatAsync(_heartbeatInterval, _cancellationTokenSource.Token);

            _logger.Information("心跳服务已启动 {@0}", _logContext);
        }

        /// <summary>
        /// 停止心跳服务
        /// </summary>
        public void Stop()
        {
            try
            {
                _cancellationTokenSource?.Cancel();

                if (_heartbeatTask != null && !_heartbeatTask.IsCompleted)
                {
                    var waitTask = Task.WhenAny(_heartbeatTask, Task.Delay(1000));
                    waitTask.ConfigureAwait(false).GetAwaiter().GetResult();
                }

                _heartbeatTask = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                _logger.Information("心跳服务已停止 {@0}", _logContext);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "停止心跳服务时发生异常 {@0}", _logContext);
            }
        }

        /// <summary>
        /// 记录心跳确认
        /// </summary>
        /// <param name="sentTime">发送时间</param>
        public void Acknowledge(long sentTime)
        {
            _latency = (int)(Environment.TickCount - sentTime);
            _heartbeatAck = true;

            _logger.Information("收到心跳确认，延迟: {0}ms {@1}", _latency, _logContext);
        }

        /// <summary>
        /// 检查心跳是否收到ACK 
        /// </summary>
        /// <returns>是否收到ACK</returns>
        public bool IsHeartbeatAck()
        {
            if (!_heartbeatAck)
            {
                int now = Environment.TickCount;
                _logger.Warning("未收到心跳ACK, 最后消息时间: {0}ms前 => {@1}", now - LastMessageTime, _logContext);
                HeartbeatAckNotReceived?.Invoke("服务器未收到心跳ACK => " + _logContext);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 检查心跳是否超时
        /// </summary>
        /// <returns>是否超时</returns>
        public bool IsHeartbeatTimeout()
        {

            int now = Environment.TickCount;
            if ((now - LastMessageTime) > _heartbeatInterval)
            {
                _logger.Warning("心跳超时，最后消息时间: {0}ms前 => {@1}", now - LastMessageTime, _logContext);
                HeartbeatTimedOut?.Invoke("服务器未响应上次的心跳 => " + _logContext);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 心跳开始时标记
        /// </summary>
        public void MarkHeartbeatSent()
        {
            _heartbeatAck = false;
        }

        /// <summary>
        /// 异步运行心跳
        /// </summary>
        /// <param name="intervalMillis">心跳间隔</param>
        /// <param name="cancelToken">取消令牌</param>
        /// <returns>异步任务</returns>
        private async Task RunHeartbeatAsync(int intervalMillis, CancellationToken cancelToken)
        {
            // 生成心跳因子之间的随机数
            var random = new Random();
            var factor = 1 - random.NextDouble() * (1 - _heartbeatFactor);
            var delayInterval = (int)(intervalMillis * factor);

            try
            {
                _logger.Information("心跳任务已启动，间隔: {0}ms, 因子: {1:F2} {@2}",
                    delayInterval, factor, _logContext);

                while (!cancelToken.IsCancellationRequested)
                {
                    // 发送心跳
                    try
                    {
                        // 检查服务器响应
                        if (IsHeartbeatTimeout())
                        {
                            break;
                        }
                        // 检查 ack 状态
                        if (!IsHeartbeatAck())
                        {
                            // 退出心跳循环
                            break;
                        }
                        // 重置Ack状态为未确认
                        MarkHeartbeatSent();
                        await _heartbeatAction().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "发送心跳时发生异常 {@0}", _logContext);
                    }

                    // 考虑延迟计算
                    int delay = Math.Max(100, delayInterval - _latency);
                    await Task.Delay(delay, cancelToken).ConfigureAwait(false);
                }

                _logger.Information("心跳任务已停止 {@0}", _logContext);
            }
            catch (OperationCanceledException)
            {
                _logger.Information("心跳任务已取消 {@0}", _logContext);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "心跳任务异常 {@0}", _logContext);
            }
        }

        /// <summary>
        /// 资源释放
        /// </summary>
        public void Dispose()
        {
            Stop();
        }
    }
}