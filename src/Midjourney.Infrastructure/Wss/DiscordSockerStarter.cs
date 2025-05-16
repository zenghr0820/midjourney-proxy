using Microsoft.Extensions.Caching.Memory;
using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.LoadBalancer;
using Midjourney.Infrastructure.Util;
using Midjourney.Infrastructure.Wss.Gateway;
using Midjourney.Infrastructure.Wss.Handle;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using UAParser;

namespace Midjourney.Infrastructure.Wss
{
    /// <summary>
    /// User WebSocket启动器，实现WebSocketStarter和IDiscordInstanceProvider接口
    /// </summary>
    public partial class DiscordSockerStarter : IWebSocketStarter, IDiscordInstanceProvider, IDisposable
    {

        /// <summary>
        /// 重连错误码
        /// </summary>
        public const int CLOSE_CODE_RECONNECT = 2001;

        /// <summary>
        /// 异常错误码（创建新的连接）
        /// </summary>
        public const int CLOSE_CODE_EXCEPTION = 1011;

        private readonly ILogger _logger;
        private readonly DiscordHelper _discordHelper;
        private readonly WebProxy _webProxy;
        private readonly DiscordInstance _discordInstance;
        private readonly IMemoryCache _memoryCache;
        private readonly WebSocketConfig _config;

        /// <summary>
        /// 表示是否已释放资源
        /// </summary>
        private bool _isDispose = false;

        /// <summary>
        /// 当前连接状态
        /// </summary>
        private ConnectionState _connectionState = ConnectionState.Disconnected;

        /// <summary>
        /// 压缩的消息
        /// </summary>
        private MemoryStream _compressed;

        /// <summary>
        /// 解压缩器
        /// </summary>
        private DeflateStream _decompressor;

        /// <summary>
        /// wss
        /// </summary>
        public ClientWebSocket WebSocket { get; private set; }

        /// <summary>
        /// 心跳服务
        /// </summary>
        private HeartbeatService _heartbeatService;

        /// <summary>
        /// wss 客户端收到的最后一个会话 ID
        /// </summary>
        private string _sessionId;

        /// <summary>
        /// wss 客户端收到的最后一个序列号
        /// </summary>
        private int? _sequence;

        /// <summary>
        /// wss 网关恢复 url
        /// </summary>
        private string _resumeGatewayUrl;

        /// <summary>
        /// wss 接收消息 token
        /// </summary>
        private CancellationTokenSource _receiveTokenSource;

        /// <summary>
        /// wss 接收消息进程
        /// </summary>
        private Task _receiveTask;

        /// <summary>
        /// wss 是否运行中
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 获取当前连接状态
        /// </summary>
        public ConnectionState ConnectionState
        {
            get => _connectionState;
            private set
            {
                if (_connectionState != value)
                {
                    _logger.Information("连接状态变更: {0} -> {1} ({2})", _connectionState, value, Account?.GuildId);
                    _connectionState = value;
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="discordHelper">Discord工具</param>
        /// <param name="webProxy">Web代理</param>
        /// <param name="discordInstance">Discord实例</param>
        /// <param name="messageHandlers"></param>
        /// <param name="memoryCache">内存缓存</param>
        /// <param name="config">WebSocket配置</param>
        public DiscordSockerStarter(
            DiscordHelper discordHelper,
            WebProxy webProxy,
            DiscordInstance discordInstance,
            IEnumerable<MessageHandler> messageHandlers,
            IMemoryCache memoryCache,
            WebSocketConfig config = null)
        {
            _discordHelper = discordHelper;
            _webProxy = webProxy;
            _discordInstance = discordInstance;
            _memoryCache = memoryCache;
            _config = config ?? new WebSocketConfig();

            _logger = Log.Logger.ForContext("LogPrefix", $"{Account.GuildId} - socket");

            _properties = GlobalConfiguration.Setting;

            _cancellationTokenSource = new CancellationTokenSource();
            _processingTask = Task.Run(ProcessMessages, _cancellationTokenSource.Token);

            // 创建无界Channel
            _messageChannel = Channel.CreateUnbounded<JsonElement>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            _messageHandlers = messageHandlers;
        }

        private DiscordAccount Account => _discordInstance?.Account;

        /// <summary>
        /// 实现IDiscordInstanceProvider接口，获取Discord账号信息
        /// </summary>
        /// <returns>Discord账号</returns>
        public DiscordAccount GetAccount()
        {
            return Account;
        }

        /// <summary>
        /// 实现IDiscordInstanceProvider接口，获取带前缀的Token
        /// </summary>
        /// <returns>带前缀的Token</returns>
        public string GetPrefixedToken()
        {
            return _discordInstance.GetPrefixedToken();
        }

        /// <summary>
        /// 实现IDiscordInstanceProvider接口，设置WebSocketStarter
        /// </summary>
        /// <param name="webSocketStarter">WebSocket启动器</param>
        public void SetWebSocketStarter(IWebSocketStarter webSocketStarter)
        {
            // 这里不需要实现，因为DiscordSockerStarter本身就是WebSocketStarter
        }

        /// <summary>
        /// 异步启动 WebSocket 连接
        /// </summary>
        /// <param name="reconnect">是否重新连接</param>
        /// <returns>连接是否成功</returns>
        public async Task<bool> StartAsync(bool reconnect = false)
        {
            try
            {
                // 如果资源已释放则，不再处理
                // 或者账号已禁用
                if (_isDispose || Account?.Enable != true)
                {
                    _logger.Warning("已禁用或资源已释放 {@0},{@1}", Account?.ChannelId, _isDispose);
                    return false;
                }

                ConnectionState = reconnect ? ConnectionState.Reconnecting : ConnectionState.Connecting;

                var isLock = await AsyncLocalLock.TryLockAsync($"user_contact_{Account.Id}", TimeSpan.FromMinutes(1), async () =>
                {
                    // 关闭现有连接并取消相关任务
                    CloseSocket(reconnect);

                    // 重置 token
                    _receiveTokenSource = new CancellationTokenSource();

                    WebSocket = new ClientWebSocket();

                    if (_webProxy != null)
                    {
                        WebSocket.Options.Proxy = _webProxy;
                    }

                    WebSocket.Options.SetRequestHeader("User-Agent", Account.UserAgent);
                    WebSocket.Options.SetRequestHeader("Accept-Encoding", "gzip, deflate, br");
                    WebSocket.Options.SetRequestHeader("Accept-Language", "zh-CN,zh;q=0.9");
                    WebSocket.Options.SetRequestHeader("Cache-Control", "no-cache");
                    WebSocket.Options.SetRequestHeader("Pragma", "no-cache");
                    WebSocket.Options.SetRequestHeader("Sec-WebSocket-Extensions", "permessage-deflate; client_max_window_bits");

                    // 获取网关地址
                    var gatewayUrl = FormatGatewayUrl(reconnect ? _resumeGatewayUrl : null);

                    // 重新连接
                    if (reconnect && !string.IsNullOrWhiteSpace(_sessionId) && _sequence.HasValue)
                    {
                        // 恢复
                        await WebSocket.ConnectAsync(new Uri(gatewayUrl), CancellationToken.None);

                        // 尝试恢复会话
                        await ResumeSessionAsync();
                    }
                    else
                    {
                        await WebSocket.ConnectAsync(new Uri(gatewayUrl), CancellationToken.None);

                        // 新连接，发送身份验证消息
                        await SendIdentifyMessageAsync();
                    }

                    _receiveTask = ReceiveMessagesAsync(_receiveTokenSource.Token);

                    _logger.Information("WebSocket 连接已建立 {@0}", Account.ChannelId);
                });

                if (!isLock)
                {
                    _logger.Information($"取消处理, 未获取到锁, 重连: {reconnect}, {Account.ChannelId}");
                    ConnectionState = ConnectionState.Error;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "WebSocket 连接异常 {@0}", Account.ChannelId);
                ConnectionState = ConnectionState.Error;
                HandleFailure(CLOSE_CODE_EXCEPTION, "WebSocket 连接异常");
            }

            return false;
        }

        /// <summary>
        /// 获取网关
        /// </summary>
        /// <param name="gatewayUrl">恢复网关URL</param>
        /// <returns>网关URL</returns>
        private string FormatGatewayUrl(string gatewayUrl = null)
        {
            var url = !string.IsNullOrWhiteSpace(gatewayUrl) ? gatewayUrl : _discordHelper.GetWss();
            return $"{url}?v={DiscordHelper.API_VERSION}&encoding={DiscordHelper.GatewayEncoding}&compress=zlib-stream";
        }

        /// <summary>
        ///  订阅Ready事件触发 
        /// </summary>
        /// <param name="readyEvent"></param>
        private void OnReady(DiscordReadyEvent readyEvent)
        {
            _resumeGatewayUrl = readyEvent.ResumeGatewayUrl;
            _sessionId = readyEvent.SessionId;
            OnSocketSuccess();
        }

        /// <summary>
        /// 订阅Resumed事件触发
        /// </summary>
        private void OnResumed()
        {
            OnSocketSuccess();
        }

        /// <summary>
        /// 发送身份验证消息
        /// </summary>
        /// <returns>发送任务</returns>
        private async Task SendIdentifyMessageAsync()
        {
            var authData = CreateAuthData();
            var identifyMessage = new { op = 2, d = authData };
            await SendMessageAsync(identifyMessage);

            _logger.Information("已发送 IDENTIFY 消息 {@0}", identifyMessage.ToString());
        }

        /// <summary>
        /// 重新恢复连接
        /// </summary>
        /// <returns>恢复任务</returns>
        private async Task ResumeSessionAsync()
        {
            var resumeMessage = new
            {
                op = 6, // RESUME 操作码
                d = new
                {
                    token = GetPrefixedToken(),
                    session_id = _sessionId,
                    seq = _sequence,
                }
            };

            await SendMessageAsync(resumeMessage);

            _logger.Information("已发送 RESUME 消息 {@0}", Account.ChannelId);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">要发送的消息对象</param>
        /// <returns>发送任务</returns>
        private async Task SendMessageAsync(object message)
        {
            if (WebSocket.State != WebSocketState.Open)
            {
                _logger.Warning("WebSocket 已关闭，无法发送消息 {@0}", Account.ChannelId);
                return;
            }

            var messageJson = JsonSerializer.Serialize(message);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);
            await WebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>接收任务</returns>
        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            const int MaxMessageSize = 1024 * 1024 * 10; // 10MB
            try
            {
                if (WebSocket == null || WebSocket.State != WebSocketState.Open)
                {
                    _logger.Warning("WebSocket 未初始化或已关闭");
                    return;
                }

                var buffer = new byte[1024 * 4];

                while (!cancellationToken.IsCancellationRequested &&
               WebSocket?.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;

                    using (var ms = new MemoryStream())
                    {
                        try
                        {
                            // 设置接收超时保护
                            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60)); // 60秒超时
                            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                            
                            // 接收单条消息的所有分片
                            do
                            {
                                try
                                {
                                    result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), linkedCts.Token);
                                    if (ms.Length + result.Count > MaxMessageSize)
                                    {
                                        throw new InvalidDataException($"消息大小超过限制: {MaxMessageSize} bytes");
                                    }
                                    ms.Write(buffer, 0, result.Count);
                                }
                                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                                {
                                    _logger.Warning("接收消息超时，重新连接 {@0}", Account.ChannelId);
                                    HandleFailure(CLOSE_CODE_RECONNECT, "接收消息超时");
                                    return;
                                }
                            } while (!result.EndOfMessage);
                            
                            // 处理完整消息
                            ms.Seek(0, SeekOrigin.Begin);
                            switch (result.MessageType)
                            {
                                case WebSocketMessageType.Binary:
                                    buffer = ms.ToArray();
                                    await HandleBinaryMessageAsync(buffer);
                                    break;
                                case WebSocketMessageType.Text:
                                    var message = Encoding.UTF8.GetString(ms.ToArray());
                                    HandleMessage(message);
                                    break;
                                case WebSocketMessageType.Close:
                                    _logger.Warning(
                                        "WebSocket 连接关闭。状态: {Status}, 描述: {Description}",
                                        result.CloseStatus, result.CloseStatusDescription);

                                    await WebSocket.CloseAsync(
                                        WebSocketCloseStatus.NormalClosure,
                                        string.Empty,
                                        cancellationToken);
                                    HandleFailure((int)result.CloseStatus, result.CloseStatusDescription);
                                    return;
                                default:
                                    _logger.Warning("收到未知消息类型: {MessageType}", result.MessageType);
                                    break;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.Information("接收消息任务已取消 {@0}", Account.ChannelId);
                            return;
                        }
                        catch (InvalidDataException ex)
                        {
                            _logger.Error(ex, "消息大小超过限制");
                            await WebSocket.CloseAsync(
                                WebSocketCloseStatus.MessageTooBig,
                                "Message too large",
                                cancellationToken);
                            return;
                        }
                        catch (WebSocketException ex)
                        {
                            _logger.Error(ex, "WebSocket连接异常: {Message}", ex.Message);
                            // 检查是否是"连接关闭而未完成握手"的特定错误
                            if (ex.Message.Contains("without completing the close handshake"))
                            {
                                // 对于这类错误，直接中止连接并触发重连
                                if (WebSocket?.State != WebSocketState.Closed && WebSocket?.State != WebSocketState.Aborted)
                                {
                                    try
                                    {
                                        WebSocket?.Abort();
                                    }
                                    catch { }
                                }
                                _logger.Error(ex, "远程服务器未完成关闭握手: {Message}", ex.Message);
                                // HandleFailure(CLOSE_CODE_RECONNECT, "远程服务器未完成关闭握手");
                            }
                            else
                            {
                                // 对于其他错误，尝试正常关闭
                                if (WebSocket?.State == WebSocketState.Open)
                                {
                                    try
                                    {
                                        await WebSocket.CloseAsync(
                                            WebSocketCloseStatus.InternalServerError,
                                            "WebSocket error",
                                            cancellationToken);
                                    }
                                    catch
                                    {
                                        WebSocket?.Abort();
                                    }
                                }
                                _logger.Error(ex, "WebSocket异常: {Message}", ex.Message);
                                // HandleFailure(CLOSE_CODE_EXCEPTION, $"WebSocket异常: {ex.Message}");
                            }
                            return;
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "接收消息时发生异常");
                            if (WebSocket?.State == WebSocketState.Open)
                            {
                                try
                                {
                                    await WebSocket.CloseAsync(
                                        WebSocketCloseStatus.InternalServerError,
                                        "Internal error",
                                        cancellationToken);
                                }
                                catch
                                {
                                    // 如果关闭失败，强制中止
                                    WebSocket?.Abort();
                                }
                            }
                            // HandleFailure(CLOSE_CODE_EXCEPTION, $"接收消息异常: {ex.Message}");
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "接收消息处理异常 {@0}", Account.ChannelId);
            }
        }

        /// <summary>
        /// 处理二进制消息
        /// </summary>
        /// <param name="buffer">二进制数据</param>
        /// <returns>处理任务</returns>
        private async Task HandleBinaryMessageAsync(byte[] buffer)
        {
            using (var decompressed = new MemoryStream())
            {
                if (_compressed == null)
                    _compressed = new MemoryStream();
                if (_decompressor == null)
                    _decompressor = new DeflateStream(_compressed, CompressionMode.Decompress);

                if (buffer[0] == 0x78)
                {
                    _compressed.Write(buffer, 2, buffer.Length - 2);
                    _compressed.SetLength(buffer.Length - 2);
                }
                else
                {
                    _compressed.Write(buffer, 0, buffer.Length);
                    _compressed.SetLength(buffer.Length);
                }

                _compressed.Position = 0;
                await _decompressor.CopyToAsync(decompressed);
                _compressed.Position = 0;
                decompressed.Position = 0;

                using (var reader = new StreamReader(decompressed, Encoding.UTF8))
                {
                    var messageContent = await reader.ReadToEndAsync();
                    HandleMessage(messageContent);
                }
            }
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="message">消息内容</param>
        private void HandleMessage(string message)
        {
            // 不再等待消息处理完毕，直接返回
            _ = Task.Run(async () =>
            {
                try
                {
                    var data = JsonDocument.Parse(message).RootElement;
                    var opCode = data.GetProperty("op").GetInt32();
                    var seq = data.TryGetProperty("s", out var seqElement) && seqElement.ValueKind == JsonValueKind.Number ? (int?)seqElement.GetInt32() : null;
                    var type = data.TryGetProperty("t", out var typeElement) ? typeElement.GetString() : null;

                    await ProcessMessageAsync((GatewayOpCode)opCode, seq, type, data);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "处理接收到的 WebSocket 消息失败 {@0}", Account.ChannelId);
                }
            });
        }

        /// <summary>
        /// 创建授权信息
        /// </summary>
        /// <returns>授权数据</returns>
        private JsonElement CreateAuthData()
        {
            var uaParser = Parser.GetDefault();
            var agent = uaParser.Parse(Account.UserAgent);
            var connectionProperties = new
            {
                browser = agent.UA.Family,
                browser_user_agent = Account.UserAgent,
                browser_version = agent.UA.Major + "." + agent.UA.Minor,
                client_build_number = 222963,
                client_event_source = (string)null,
                device = agent.Device.Model,
                os = agent.OS.Family,
                referer = "https://www.midjourney.com",
                referring_domain = "www.midjourney.com",
                release_channel = "stable",
                system_locale = "zh-CN"
            };

            var presence = new
            {
                activities = Array.Empty<object>(),
                afk = false,
                since = 0,
                status = "online"
            };

            var clientState = new
            {
                api_code_version = 0,
                guild_versions = new { },
                highest_last_message_id = "0",
                private_channels_version = "0",
                read_state_version = 0,
                user_guild_settings_version = -1,
                user_settings_version = -1
            };

            var authData = new
            {
                capabilities = 16381,
                client_state = clientState,
                compress = false,
                presence = presence,
                properties = connectionProperties,
                // intents = 32767,
                intents = 49087,
                //  Discord.GatewayIntents intents = Discord.GatewayIntents.AllUnprivileged &
                //                        ~(Discord.GatewayIntents.GuildScheduledEvents | Discord.GatewayIntents.GuildInvites) |
                //                        Discord.GatewayIntents.MessageContent,
                token = GetPrefixedToken()
            };

            _logger.Information("创建授权信息 {@0}", authData.token);

            return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(authData));
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="opCode">操作码</param>
        /// <param name="seq">序列号</param>
        /// <param name="type">消息类型</param>
        /// <param name="payload">消息数据</param>
        /// <returns>处理任务</returns>
        private async Task ProcessMessageAsync(GatewayOpCode opCode, int? seq, string type, JsonElement payload)
        {
            if (seq != null)
            {
                _sequence = seq.Value;
            }

            // 更新心跳服务最后消息时间
            if (_heartbeatService != null)
            {
                _heartbeatService.LastMessageTime = Environment.TickCount;
            }

            try
            {
                switch (opCode)
                {
                    case GatewayOpCode.Hello:
                        {
                            _logger.Information("Received Hello {@0}", Account.ChannelId);
                            var heartbeatInterval = payload.GetProperty("d").GetProperty("heartbeat_interval").GetInt64();

                            // 初始化心跳服务
                            InitHeartbeatService(heartbeatInterval);
                        }
                        break;

                    case GatewayOpCode.Heartbeat:
                        {
                            _logger.Information("Received Heartbeat {@0}", Account.ChannelId);

                            // 立即发送心跳
                            await SendHeartbeatAsync();

                            _logger.Information("Received Heartbeat 消息已发送 {@0}", Account.ChannelId);
                        }
                        break;

                    case GatewayOpCode.HeartbeatAck:
                        {
                            _logger.Information("Received HeartbeatAck {@0}", Account.ChannelId);

                            // 心跳确认处理
                            if (_heartbeatService != null)
                            {
                                _heartbeatService.Acknowledge(Environment.TickCount);
                            }
                        }
                        break;

                    case GatewayOpCode.InvalidSession:
                        {
                            _logger.Warning("Received InvalidSession {@0}", Account.ChannelId);
                            _logger.Warning("Failed to resume previous session {@0}", Account.ChannelId);

                            _sessionId = null;
                            _sequence = null;
                            _resumeGatewayUrl = null;

                            HandleFailure(CLOSE_CODE_EXCEPTION, "无效授权，创建新的连接");
                        }
                        break;

                    case GatewayOpCode.Reconnect:
                        {
                            _logger.Warning("Received Reconnect {@0}", Account.ChannelId);

                            HandleFailure(CLOSE_CODE_RECONNECT, "收到重连请求，将自动重连");
                        }
                        break;

                    case GatewayOpCode.Resume:
                        {
                            _logger.Information("Resume {@0}", Account.ChannelId);

                            OnSocketSuccess();
                        }
                        break;

                    case GatewayOpCode.Dispatch:
                        {
                            _logger.Information("Received Dispatch {@0}, {@1}", type, Account.GuildId);
                            await HandleDispatch(payload);
                        }
                        break;

                    default:
                        _logger.Warning("Unknown OpCode ({@0}) {@1}", opCode, Account.ChannelId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error handling {opCode}{(type != null ? $" ({type})" : "")}, {Account.ChannelId}");
            }
        }

        /// <summary>
        /// 初始化心跳服务
        /// </summary>
        /// <param name="heartbeatInterval">心跳间隔</param>
        private void InitHeartbeatService(long heartbeatInterval)
        {
            // 释放旧的心跳服务
            _heartbeatService?.Dispose();

            // 创建新的心跳服务
            _heartbeatService = new HeartbeatService(
                SendHeartbeatAsync,
                (int)heartbeatInterval,
                _config.HeartbeatFactor,
                Account.GuildId);

            // 订阅超时处理服务
            _heartbeatService.HeartbeatTimedOut += reason =>
            {
                HandleFailure(CLOSE_CODE_RECONNECT, $"心跳超时: {reason}，将进行重连");
            };

            // 订阅心跳ACK未确认处理服务
            _heartbeatService.HeartbeatAckNotReceived += reason =>
            {
                HandleFailure(CLOSE_CODE_RECONNECT, $"心跳ACK未确认: {reason}，将进行重连");
            };

            // 启动心跳服务
            _heartbeatService.Start();
        }

        /// <summary>
        /// 发送心跳
        /// </summary>
        /// <returns>发送任务</returns>
        private async Task SendHeartbeatAsync()
        {

            var heartbeatMessage = new { op = 1, d = _sequence };

            await SendMessageAsync(heartbeatMessage);
            _logger.Information("已发送 HEARTBEAT 消息 {@0}", Account.ChannelId);

            // 标记心跳已发送
            if (_heartbeatService != null)
            {
                _heartbeatService.MarkHeartbeatSent();
            }
        }

        /// <summary>
        /// 处理分派消息
        /// </summary>
        /// <param name="data">消息数据</param>
        private async Task HandleDispatch(JsonElement data)
        {
            #region Connection
            if (data.TryGetProperty("t", out var t) && t.GetString() == "READY")
            {
                _sessionId = data.GetProperty("d").GetProperty("session_id").GetString();
                _resumeGatewayUrl = data.GetProperty("d").GetProperty("resume_gateway_url").GetString();

                OnSocketSuccess();

                if (Account.EnableAutoFetchChannels)
                {
                    // 开始获取频道列表
                    await InitChannelHandler(data);
                }
            }
            else if (data.TryGetProperty("t", out var resumed) && resumed.GetString() == "RESUMED")
            {
                OnSocketSuccess();
                // await _resumedEvent.InvokeAsync();
            }
            #endregion
            else
            {
                EnqueueMessage(data);
            }
        }

        public async Task InitChannelHandler(JsonElement data)
        {
            if (!data.TryGetProperty("d", out JsonElement payload))
            {
                return;
            }

            // 获取服务器ID
            if (payload.TryGetProperty("guilds", out JsonElement guildsElement))
            {
                _logger.Debug("- Try Get Guilds -");
                var guilds = guildsElement.Deserialize<DiscordExtendedGuild[]>();
                foreach (var guild in guilds)
                {
                    if (guild == null) continue;
                    _logger.Debug("Get Guild[{@0}] - ChannelIds => {@1}", guild?.Id, guild.Channels?.Length);
                    _guilds.AddOrUpdate(guild.Id, guild, (id, old) => guild);
                }
            }
            _logger.Information("当前账号下的服务器数 [{@0}] - {@1}", _guilds.Count, string.Join(", ", _guilds.Keys));
            // 获取私信频道列表
            if (payload.TryGetProperty("private_channels", out JsonElement dmChannelElement))
            {
                _logger.Debug("- Try Get DM Channels -");
                var dmChannels = dmChannelElement.Deserialize<DiscordChannelDto[]>();
                foreach (var channel in dmChannels)
                {
                    if (channel == null) continue;
                    _logger.Debug("recipient_ids = {@0}", string.Join(", ", channel?.RecipientsIds ?? Array.Empty<string>()));
                    _logger.Debug("Get DM Channel ID[{@0}] - Name => {@1}", channel?.Id, channel?.Name);
                    _dmChannels.AddOrUpdate(channel.Id, channel, (id, old) => channel);
                }
            }
            _logger.Information("当前账号下的私信频道数 [{@0}] - {@1}", _dmChannels.Count, string.Join(", ", _dmChannels.Keys));

            // // 事件订阅
            // await _readyEvent.InvokeAsync(readyEvent);
            // // 频道订阅
            await _dmChannelEvent.InvokeAsync(_dmChannels);
            await _channelSubscribeEvent.InvokeAsync(_guilds);
        }

        /// <summary>
        /// 处理错误
        /// </summary>
        /// <param name="code">错误码</param>
        /// <param name="reason">错误原因</param>
        private void HandleFailure(int code, string reason)
        {
            _logger.Error("WebSocket 连接失败, 代码 {0}: {1}, {2}", code, reason, Account.ChannelId);

            if (!IsRunning)
            {
                NotifyWss(code, reason);
            }

            IsRunning = false;
            ConnectionState = ConnectionState.Error;

            if (code >= 4000)
            {
                _logger.Warning("无法重新连接， 由 {0}({1}) 关闭 {2}, 尝试新连接... ", code, reason, Account.ChannelId);
                TryNewConnect();
            }
            else if (code == CLOSE_CODE_RECONNECT)
            {
                _logger.Warning("由 {0}({1}) 关闭, 尝试重新连接... {2}", code, reason, Account.ChannelId);
                TryReconnect();
            }
            else
            {
                _logger.Warning("由 {0}({1}) 关闭, 尝试新连接... {2}", code, reason, Account.ChannelId);
                TryNewConnect();
            }
        }

        /// <summary>
        /// 尝试重新连接
        /// </summary>
        public void TryReconnect()
        {
            try
            {
                if (_isDispose)
                {
                    return;
                }

                ConnectionState = ConnectionState.Reconnecting;

                var success = StartAsync(true).ConfigureAwait(false).GetAwaiter().GetResult();
                if (!success)
                {
                    _logger.Warning("重新连接失败 {@0}，尝试新连接", Account.ChannelId);

                    Thread.Sleep(_config.ReconnectDelay);
                    TryNewConnect();
                }
            }
            catch (Exception e)
            {
                _logger.Warning(e, "重新连接异常 {@0}，尝试新连接", Account.ChannelId);

                Thread.Sleep(_config.ReconnectDelay);
                TryNewConnect();
            }
        }

        /// <summary>
        /// 尝试新的连接
        /// </summary>
        public void TryNewConnect()
        {
            if (_isDispose)
            {
                return;
            }

            var isLock = LocalLock.TryLock("UserTryNewConnect", TimeSpan.FromSeconds(3), () =>
            {
                for (int i = 1; i <= _config.ConnectRetryLimit; i++)
                {
                    try
                    {
                        // 如果 5 分钟内失败次数超过限制，则禁用账号
                        var ncKey = $"UserTryNewConnect_{Account.ChannelId}";
                        _memoryCache.TryGetValue(ncKey, out int count);
                        if (count > _config.ConnectRetryLimit)
                        {
                            _logger.Warning("新的连接失败次数超过限制，禁用账号");
                            DisableAccount("新的连接失败次数超过限制，禁用账号");
                            return;
                        }
                        _memoryCache.Set(ncKey, count + 1, TimeSpan.FromMinutes(5));

                        var success = StartAsync(false).ConfigureAwait(false).GetAwaiter().GetResult();
                        if (success)
                        {
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Warning(e, "新连接失败, 第 {@0} 次, {@1}", i, Account.ChannelId);

                        Thread.Sleep(_config.ReconnectDelay);
                    }
                }

                if (WebSocket == null || WebSocket.State != WebSocketState.Open)
                {
                    _logger.Error("由于无法重新连接，自动禁用用户账号");

                    DisableAccount("由于无法重新连接，自动禁用账号");
                }
            });

            if (!isLock)
            {
                _logger.Warning("新的连接作业正在执行中，禁止重复执行");
            }
        }

        /// <summary>
        /// 停止并禁用账号
        /// </summary>
        /// <param name="msg">禁用原因</param>
        public void DisableAccount(string msg)
        {
            try
            {
                // 保存
                Account.Enable = false;
                Account.DisabledReason = msg;

                DbHelper.Instance.AccountStore.Update(Account);

                _discordInstance?.ClearAccountCache(Account.Id);
                _discordInstance?.Dispose();

                // 尝试自动登录
                var sw = new Stopwatch();
                var setting = GlobalConfiguration.Setting;
                var info = new StringBuilder();
                var account = Account;
                if (setting.EnableAutoLogin)
                {
                    sw.Stop();
                    info.AppendLine($"{account.Id}尝试自动登录...");
                    sw.Restart();
                    try
                    {
                        // 开始尝试自动登录
                        var suc = DiscordAccountHelper.AutoLogin(account, true);
                        if (suc)
                        {
                            sw.Stop();
                            info.AppendLine($"{account.Id}自动登录请求成功...");
                            sw.Restart();
                        }
                        else
                        {
                            sw.Stop();
                            info.AppendLine($"{account.Id}自动登录请求失败...");
                            sw.Restart();
                        }
                    }
                    catch (Exception exa)
                    {
                        _logger.Error(exa, "Account({@0}) auto login fail, disabled: {@1}", account.ChannelId, exa.Message);
                        sw.Stop();
                        info.AppendLine($"{account.Id}自动登录请求异常...");
                        sw.Restart();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "禁用用户账号失败 {@0}", Account.ChannelId);
            }
            finally
            {
                // 邮件通知
                var smtp = GlobalConfiguration.Setting?.Smtp;
                EmailJob.Instance.EmailSend(smtp, $"MJ账号禁用通知-{Account.ChannelId}",
                    $"{Account.ChannelId}, {Account.DisabledReason}");
            }
        }

        /// <summary>
        /// 写 info 消息
        /// </summary>
        /// <param name="msg"></param>
        private void LogInfo(string msg)
        {
            _logger.Information(msg + ", {@ChannelId}", Account.ChannelId);
        }

        /// <summary>
        /// 如果打开了，则关闭 wss
        /// </summary>
        /// <param name="reconnect">是否重新连接</param>
        public void CloseSocket(bool reconnect = false)
        {
            try
            {
                // 更新状态
                ConnectionState = ConnectionState.Disconnected;

                try
                {
                    // 停止心跳服务
                    _heartbeatService?.Stop();
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "停止心跳服务异常");
                }

                try
                {
                    if (_receiveTokenSource != null)
                    {
                        LogInfo("强制取消消息 token");
                        _receiveTokenSource?.Cancel();
                        _receiveTokenSource?.Dispose();
                        _receiveTokenSource = null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "取消接收任务token异常");
                }

                try
                {
                    if (_receiveTask != null)
                    {
                        LogInfo("强制释放消息 task");
                        
                        // 等待接收任务完成，最多等待2秒
                        if (!_receiveTask.IsCompleted && !_receiveTask.IsCanceled && !_receiveTask.IsFaulted)
                        {
                            if (!_receiveTask.Wait(TimeSpan.FromSeconds(2)))
                            {
                                _logger.Warning("接收任务等待超时");
                            }
                        }
                        
                        _receiveTask?.Dispose();
                        _receiveTask = null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "释放接收任务异常");
                }

                try
                {
                    // 处理WebSocket关闭
                    if (WebSocket != null)
                    {
                        var currentState = WebSocket.State;
                        LogInfo($"准备关闭WebSocket，当前状态: {currentState}");

                        if (currentState == WebSocketState.Open || currentState == WebSocketState.CloseReceived || currentState == WebSocketState.CloseSent)
                        {
                            // 正常关闭
                            try
                            {
                                if (reconnect)
                                {
                                    // 重连时使用 4000 断开
                                    var status = (WebSocketCloseStatus)4000;
                                    var closeTask = Task.Run(() => WebSocket.CloseOutputAsync(status, "准备重连", CancellationToken.None));
                                    if (!closeTask.Wait(TimeSpan.FromSeconds(3)))
                                    {
                                        _logger.Warning("WebSocket 关闭操作超时，强制中止 {@0}", Account.ChannelId);
                                        WebSocket.Abort();
                                    }
                                }
                                else
                                {
                                    var closeTask = Task.Run(() => WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "正常关闭", CancellationToken.None));
                                    if (!closeTask.Wait(TimeSpan.FromSeconds(3)))
                                    {
                                        _logger.Warning("WebSocket 关闭操作超时，强制中止 {@0}", Account.ChannelId);
                                        WebSocket.Abort();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Warning(ex, "关闭WebSocket异常，执行强制中止");
                                WebSocket.Abort();
                            }
                        }
                        else if (currentState != WebSocketState.Closed && currentState != WebSocketState.Aborted)
                        {
                            // 对于其他状态，直接强制中止
                            LogInfo("WebSocket不处于可关闭状态，执行强制中止");
                            WebSocket.Abort();
                        }
                        
                        // 释放资源
                        WebSocket.Dispose();
                        WebSocket = null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "关闭WebSocket异常");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "CloseSocket方法异常");
            }
            finally
            {
                WebSocket = null;
                _receiveTokenSource = null;
                _receiveTask = null;
                _heartbeatService = null;

                // 释放解压缩资源
                try
                {
                    _decompressor?.Dispose();
                    _compressed?.Dispose();
                    _decompressor = null;
                    _compressed = null;
                }
                catch { }

                LogInfo("WebSocket 资源已释放");
            }
        }

        /// <summary>
        /// 通知错误或成功
        /// </summary>
        /// <param name="code">错误码</param>
        /// <param name="reason">错误原因</param>
        private void NotifyWss(int code, string reason)
        {
            if (!Account.Lock)
            {
                Account.DisabledReason = reason;
            }

            // 保存
            DbHelper.Instance.AccountStore.Update("Enable,DisabledReason", Account);
            _discordInstance?.ClearAccountCache(Account.Id);
        }

        /// <summary>
        /// 资源释放
        /// </summary>
        public void Dispose()
        {
            try
            {
                _isDispose = true;

                CloseSocket();

                // 释放消息处理器
                MessageProcessorDispose();
                // _messageProcessor = null;

                _heartbeatService?.Dispose();
            }
            catch
            {
            }

            try
            {
                WebSocket?.Dispose();
            }
            catch
            {
            }
        }

        /// <summary>
        /// 连接成功
        /// </summary>
        private void OnSocketSuccess()
        {
            IsRunning = true;
            ConnectionState = ConnectionState.Connected;
            _discordInstance.DefaultSessionId = _sessionId;

            NotifyWss(ReturnCode.SUCCESS, "");
        }
    }

    /// <summary>
    /// Discord 网关操作码
    /// </summary>
    internal enum GatewayOpCode : byte
    {
        Dispatch = 0,
        Heartbeat = 1,
        Identify = 2,
        PresenceUpdate = 3,
        VoiceStateUpdate = 4,
        VoiceServerPing = 5,
        Resume = 6,
        Reconnect = 7,
        RequestGuildMembers = 8,
        InvalidSession = 9,
        Hello = 10,
        HeartbeatAck = 11,
        GuildSync = 12
    }
}