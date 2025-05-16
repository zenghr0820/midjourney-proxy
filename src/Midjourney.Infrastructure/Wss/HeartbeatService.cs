using Serilog;
using System;
using System.Diagnostics;
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
        private readonly Func<Task> _sendHeartbeatAsync;
        private readonly int _interval;
        private readonly Timer _timer;
        private readonly object _lock = new object();

        // 心跳超时系数，默认为0.7
        private readonly double _timeoutFactor;

        // 最后一次收到消息的时间（毫秒）
        public int LastMessageTime { get; set; }

        // 最后一次发送心跳的时间（毫秒）
        private int _lastHeartbeatTime;

        // 最后一次收到心跳确认的时间（毫秒）
        private int _lastAckTime;

        // 是否已经发送了心跳
        private volatile bool _heartbeatSent;

        // 是否已经停止
        private volatile bool _stopped;

        // 连续未收到ACK的次数
        private int _missedAckCount = 0;

        // 最大允许的连续未收到ACK的次数
        private const int MaxMissedAcks = 3;

        /// <summary>
        /// 心跳超时事件
        /// </summary>
        public event Action<string> HeartbeatTimedOut;

        /// <summary>
        /// 心跳ACK未收到事件
        /// </summary>
        public event Action<string> HeartbeatAckNotReceived;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="sendHeartbeatAsync">发送心跳的方法</param>
        /// <param name="interval">心跳间隔（毫秒）</param>
        /// <param name="timeoutFactor">超时系数，默认0.7</param>
        /// <param name="logContext">日志上下文</param>
        public HeartbeatService(Func<Task> sendHeartbeatAsync, int interval, double timeoutFactor = 0.7, string logContext = null)
        {
            _logger = Log.Logger.ForContext("LogPrefix", $"HeartbeatService - {logContext}");

            _sendHeartbeatAsync = sendHeartbeatAsync;
            _interval = Math.Max(interval, 1000); // 确保最小间隔1秒
            _timeoutFactor = timeoutFactor;

            // 初始化时间
            var now = Environment.TickCount;
            LastMessageTime = now;
            _lastHeartbeatTime = now;
            _lastAckTime = now;

            // 创建定时器，但不启动
            _timer = new Timer(RunHeartbeatAsync, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// 启动心跳服务
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (_stopped)
                    return;

                var now = Environment.TickCount;
                LastMessageTime = now;
                _lastHeartbeatTime = now;
                _lastAckTime = now;
                _heartbeatSent = false;
                _missedAckCount = 0;

                // 启动定时器，立即开始第一次检查，然后每隔1秒检查一次
                _timer.Change(0, 1000);

                _logger.Information("心跳服务已启动，间隔：{0}ms", _interval);
            }
        }

        /// <summary>
        /// 停止心跳服务
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                _stopped = true;
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _logger.Information("心跳服务已停止");
            }
        }

        /// <summary>
        /// 标记心跳已发送
        /// </summary>
        public void MarkHeartbeatSent()
        {
            _lastHeartbeatTime = Environment.TickCount;
            _heartbeatSent = true;
        }

        /// <summary>
        /// 确认心跳
        /// </summary>
        /// <param name="time">时间（毫秒）</param>
        public void Acknowledge(int time)
        {
            if (_heartbeatSent)
            {
                _lastAckTime = time;
                _heartbeatSent = false;
                _missedAckCount = 0;
                
                // 记录心跳往返时间
                var rtt = _lastAckTime - _lastHeartbeatTime;
                if (rtt > 0 && rtt < 60000) // 防止时间回绕导致的异常值
                {
                    _logger.Debug("心跳往返时间：{0}ms", rtt);
                }
            }
        }

        /// <summary>
        /// 运行心跳检查
        /// </summary>
        /// <param name="state">状态</param>
        private async void RunHeartbeatAsync(object state)
        {
            if (_stopped)
                return;

            try
            {
                var now = Environment.TickCount;
                var sinceLastMessage = ComputeTimeDelta(now, LastMessageTime);
                var sinceLastHeartbeat = ComputeTimeDelta(now, _lastHeartbeatTime);
                var sinceLastAck = ComputeTimeDelta(now, _lastAckTime);

                // 1. 检查是否接收超时（长时间未收到任何消息）
                int timeoutThreshold = (int)(_interval * 2); // 2倍心跳间隔
                if (sinceLastMessage > timeoutThreshold)
                {
                    _logger.Warning("长时间未收到消息，可能连接已断开。最后消息时间：{0}ms", sinceLastMessage);
                    Stop();
                    HeartbeatTimedOut?.Invoke($"超过{timeoutThreshold / 1000}秒未收到任何消息");
                    return;
                }

                // 2. 检查是否需要发送心跳（上次心跳后已过间隔时间）
                if (sinceLastHeartbeat >= _interval)
                {
                    // 如果已发送心跳但未收到ACK，检查是否超时
                    if (_heartbeatSent)
                    {
                        _missedAckCount++;
                        
                        if (_missedAckCount >= MaxMissedAcks)
                        {
                            _logger.Warning("连续{0}次未收到心跳确认，可能连接已断开。", MaxMissedAcks);
                            Stop();
                            HeartbeatAckNotReceived?.Invoke($"连续{MaxMissedAcks}次未收到心跳确认");
                            return;
                        }
                        else
                        {
                            _logger.Warning("未收到上次心跳确认，但将继续发送。未确认次数：{0}", _missedAckCount);
                        }
                    }

                    // 发送新的心跳
                    try
                    {
                        await _sendHeartbeatAsync();
                        MarkHeartbeatSent();
                        _logger.Debug("已发送心跳。");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "发送心跳时发生异常。");
                    }
                }
                // 3. 如果已发送心跳但长时间未收到ACK，可能需要重连
                else if (_heartbeatSent && sinceLastHeartbeat > (int)(_interval * 1.5))
                {
                    _logger.Warning("长时间未收到心跳确认，距上次心跳：{0}ms", sinceLastHeartbeat);
                        
                    _missedAckCount++;
                    if (_missedAckCount >= MaxMissedAcks)
                    {
                        Stop();
                        HeartbeatAckNotReceived?.Invoke("长时间未收到心跳确认");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "心跳检查过程中发生异常。");
            }
        }

        /// <summary>
        /// 计算两个时间点之间的差值，考虑环绕问题
        /// </summary>
        /// <param name="now">当前时间</param>
        /// <param name="previous">之前的时间</param>
        /// <returns>时间差（毫秒）</returns>
        private int ComputeTimeDelta(int now, int previous)
        {
            // 处理时间环绕问题
            if (now < previous)
            {
                // 假设环绕，返回一个合理的小值而不是负值
                return 1000; // 返回1秒作为合理的差值
            }
            return now - previous;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                Stop();
                _timer?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "释放心跳服务资源时发生异常。");
            }
        }
    }
}