// using Midjourney.Infrastructure.Data;
// using Midjourney.Infrastructure.LoadBalancer;
// using Midjourney.Infrastructure.Util;
// using Serilog;
// using System.Collections.Concurrent;
// using System.Net;
// using System.Text.Json;
// using Discord;
// using Discord.WebSocket;
// using Discord.Net.WebSockets;
// using Discord.Net.Rest;
// using Midjourney.Infrastructure.Wss.Handle;
// using Microsoft.Extensions.Caching.Memory;

// namespace Midjourney.Infrastructure.Wss.Bot
// {
//     /// <summary>
//     /// Bot WebSocket启动器，实现WebSocketStarter接口
//     /// </summary>
//     public class BotWebSocketStarter : IWebSocketStarter, IDisposable
//     {
//         /// <summary>
//         /// 新连接最大重试次数
//         /// </summary>
//         private const int CONNECT_RETRY_LIMIT = 5;

//         /// <summary>
//         /// 重连错误码
//         /// </summary>
//         public const int CLOSE_CODE_RECONNECT = 2001;

//         /// <summary>
//         /// 异常错误码（创建新的连接）
//         /// </summary>
//         public const int CLOSE_CODE_EXCEPTION = 1011;

//         private const string Prefix = "[Bot WebSocket]";

//         private readonly ILogger _logger;
//         private readonly DiscordHelper _discordHelper;
//         // private readonly MessageListener _botListener;
//         private readonly WebProxy _webProxy;
//         private readonly DiscordInstance _discordInstance;
//         private readonly IMemoryCache _memoryCache;

//         /// <summary>
//         /// 消息处理器
//         /// </summary>
//         private MessageProcessor _messageProcessor;

//         /// <summary>
//         /// 消息处理器集合
//         /// </summary>
//         private readonly IEnumerable<MessageHandler> _messageHandlers;

//         /// <summary>
//         /// 表示是否已释放资源
//         /// </summary>
//         private bool _isDispose = false;

//         /// <summary>
//         /// Discord Socket客户端
//         /// </summary>
//         private DiscordSocketClient _client;

//         /// <summary>
//         /// 消息队列
//         /// </summary>
//         private readonly ConcurrentQueue<JsonElement> _messageQueue = new ConcurrentQueue<JsonElement>();

//         private readonly Task _messageQueueTask;

//         /// <summary>
//         /// 是否正在运行
//         /// </summary>
//         public bool IsRunning { get; private set; }

//         public BotWebSocketStarter(
//             DiscordHelper discordHelper,
//             WebProxy webProxy,
//             DiscordInstance discordInstance,
//             IEnumerable<MessageHandler> messageHandlers,
//             IMemoryCache memoryCache,
//             WebSocketConfig config = null)
//         {
//             _discordHelper = discordHelper;
//             _webProxy = webProxy;
//             _discordInstance = discordInstance;
//             _memoryCache = memoryCache;
//             _messageHandlers = messageHandlers;

//             _logger = Log.Logger;

//             // 初始化消息处理器
//             _messageProcessor = new MessageProcessor(Account?.ChannelId ?? "unknown", _messageHandlers, _discordInstance);

//             _messageQueueTask = new Task(MessageQueueDoWork, TaskCreationOptions.LongRunning);
//             _messageQueueTask.Start();
//         }

//         private DiscordAccount Account => _discordInstance?.Account;

//         /// <summary>
//         /// 异步启动 WebSocket 连接
//         /// </summary>
//         /// <param name="reconnect">是否重新连接</param>
//         /// <returns>连接是否成功</returns>
//         public async Task<bool> StartAsync(bool reconnect = false)
//         {
//             try
//             {
//                 // 如果资源已释放则，不再处理
//                 // 或者账号已禁用
//                 if (_isDispose || Account?.Enable != true)
//                 {
//                     _logger.Warning("Bot已禁用或资源已释放 {@0},{@1}", Account.ChannelId, _isDispose);
//                     return false;
//                 }

//                 bool result = false;
//                 var isLock = await AsyncLocalLock.TryLockAsync($"bot_contact_{Account.Id}", TimeSpan.FromMinutes(1), async () =>
//                 {
//                     // 关闭现有连接并取消相关任务
//                     CloseSocket(reconnect);

//                     // new DiscordSocketApiClient()
//                     // 初始化Discord客户端
//                     _client = new DiscordSocketClient(new DiscordSocketConfig
//                     {
//                         LogLevel = LogSeverity.Info,
//                         RestClientProvider = _webProxy != null ? CustomRestClientProvider.Create(_webProxy, true)
//                          : DefaultRestClientProvider.Create(true),


//                         WebSocketProvider = DefaultWebSocketProvider.Create(_webProxy),

//                         // 读取消息权限 GatewayIntents.MessageContent
//                         GatewayIntents = Discord.GatewayIntents.AllUnprivileged &
//                                        ~(Discord.GatewayIntents.GuildScheduledEvents | Discord.GatewayIntents.GuildInvites) |
//                                        Discord.GatewayIntents.MessageContent
//                     });

//                     // 注册事件处理程序
//                     _client.Log += LogAction;
//                     _client.MessageReceived += MessageReceivedAsync;
//                     _client.MessageUpdated += MessageUpdatedAsync;
//                     _client.Connected += OnConnected;
//                     _client.Ready += OnClientReady;
//                     _client.Disconnected += OnDisconnected;

//                     // 登录并启动
//                     if (string.IsNullOrWhiteSpace(Account.BotToken))
//                     {
//                         LogError(null, $"Bot Token为空, 无法连接 {Account.GuildId}");
//                         result = false;
//                     }
//                     else
//                     {
//                         await _client.LoginAsync(TokenType.Bot, Account.BotToken);
//                         await _client.StartAsync();

//                         // 是否自动获取频道
//                         if (Account.EnableAutoFetchChannels)
//                         {
//                             GetTextChannels();
//                         }

//                         LogInfo($"Bot WebSocket 连接已建立 {Account.GuildId}");
//                         result = true;
//                     }
//                 });

//                 if (!isLock)
//                 {
//                     LogInfo($"取消处理, 未获取到锁, 重连: {reconnect}, {Account.GuildId}");
//                     return false;
//                 }

//                 return result;
//             }
//             catch (Exception ex)
//             {
//                 LogError(ex, $"Bot WebSocket 连接异常 {Account.GuildId}");
//                 HandleFailure(CLOSE_CODE_EXCEPTION, "Bot WebSocket 连接异常");
//             }

//             return false;
//         }

//         /// <summary>
//         ///   自动获取服务器下所有的文本频道
//         /// </summary>
//         private void GetTextChannels()
//         {
//             var guild = _client.GetGuild(ulong.Parse(Account.GuildId));
//             if (guild == null)
//             {
//                 LogError(null, $"自动获取所有文本频道时未找到对应的服务器guildId: {Account.GuildId}");
//                 return;
//             }
//             var textChannels = guild.Channels.Where(c => c is SocketTextChannel);
//             var channelIds = new List<string>();
//             foreach (SocketTextChannel channel in textChannels)
//             {

//                 LogInfo($"自动获取服务器{guild.Id}下的文本频道: id - {channel.Id}, name - {channel.Name}");
//                 channelIds.Add(channel.Id.ToString());
//             }

//             // 保存频道数据
//             Account.ChannelIds = channelIds;
//             // 更新账号数据
//             DbHelper.Instance.AccountStore.Update(Account);
//             // 清除账号缓存
//             _discordInstance?.ClearAccountCache(Account.Id);
//         }
//         /// <summary>
//         /// Discord日志处理
//         /// </summary>
//         private Task LogAction(LogMessage msg)
//         {
//             switch (msg.Severity)
//             {
//                 case LogSeverity.Critical:
//                 case LogSeverity.Error:
//                     _logger.Error(msg.Exception, "Discord: {0} {@1}", msg.Message, Account.ChannelId);
//                     break;
//                 case LogSeverity.Warning:
//                     _logger.Warning("Discord: {0} {@1}", msg.Message, Account.ChannelId);
//                     break;
//                 case LogSeverity.Info:
//                     LogInfo($"Discord: {msg.Message} {Account.GuildId}");
//                     break;
//                 case LogSeverity.Debug:
//                 case LogSeverity.Verbose:
//                     _logger.Debug("Discord: {0} {@1}", msg.Message, Account.ChannelId);
//                     break;
//             }

//             // 解析 Bot WebSocket Log 消息
//             try
//             {
//                 var data = JsonDocument.Parse(msg.Message).RootElement;
//                 var opCode = data.GetProperty("op").GetInt32();
//                 var seq = data.TryGetProperty("s", out var seqElement) && seqElement.ValueKind == JsonValueKind.Number ? (int?)seqElement.GetInt32() : null;
//                 var type = data.TryGetProperty("t", out var typeElement) ? typeElement.GetString() : null;

//                 switch ((GatewayOpCode)opCode)
//                 {
//                     case GatewayOpCode.Hello:
//                     case GatewayOpCode.Heartbeat:
//                     case GatewayOpCode.HeartbeatAck:
//                     case GatewayOpCode.InvalidSession:
//                     case GatewayOpCode.Reconnect:
//                     case GatewayOpCode.Resume:
//                         break;
//                     case GatewayOpCode.Dispatch:
//                         {
//                             // 解析 READY 事件数据（包含 session_id 和 resume_gateway_url）
//                             if (data.TryGetProperty("t", out var t) && t.GetString() == "READY")
//                             {
//                                 LogInfo($"解析 READY 事件数据: {data}");
//                                 var sessionId = data.GetProperty("d").GetProperty("session_id").GetString();
//                                 var resumeGatewayUrl = data.GetProperty("d").GetProperty("resume_gateway_url").GetString() + "/?encoding=json&v=9&compress=zlib-stream";
//                             }
//                         }
//                         break;

//                     default:
//                         _logger.Warning("[Bot WebSocket] - Unknown OpCode ({@0}) {@1}", opCode, Account.ChannelId);
//                         break;
//                 }
//             }
//             catch (Exception ex)
//             {
//                 LogError(ex, $"解析 Bot WebSocket Log 消息失败 {Account.GuildId}");
//             }


//             return Task.CompletedTask;
//         }

//         /// <summary>
//         /// 检查频道是否属于当前账号
//         /// </summary>
//         private bool IsChannelBelongsToAccount(ISocketMessageChannel channel)
//         {
//             if (channel == null)
//             {
//                 return false;
//             }

//             string channelId = channel.Id.ToString();

//             // 检查是否为主频道
//             if (channelId == Account.ChannelId)
//             {
//                 return true;
//             }

//             // 同一个服务器下的所有频道
//             if (Account.ChannelIds != null && Account.ChannelIds.Contains(channelId))
//             {
//                 return true;
//             }

//             // 检查是否为子频道 - TODO 原始代码不知道子频道是什么意思保留
//             if (Account.SubChannelValues != null && Account.SubChannelValues.ContainsKey(channelId))
//             {
//                 return true;
//             }

//             // 检查是否为私信频道
//             if (channel is SocketDMChannel &&
//                 (channelId == Account.PrivateChannelId || channelId == Account.NijiBotChannelId))
//             {
//                 return true;
//             }

//             return false;
//         }



//         /// <summary>
//         /// 处理接收到的消息
//         /// </summary>
//         private Task MessageReceivedAsync(SocketMessage message)
//         {
//             try
//             {
//                 // 检查是否属于当前账号
//                 if (!IsChannelBelongsToAccount(message.Channel))
//                 {
//                     return Task.CompletedTask;
//                 }

//                 // 检查是否为Bot消息
//                 if (!message.Author.IsBot)
//                 {
//                     return Task.CompletedTask;
//                 }

//                 // 使用消息处理器处理Bot消息
//                 _messageProcessor.EnqueueBotMessage(message, MessageType.CREATE);
//             }
//             catch (Exception ex)
//             {
//                 LogError(ex, $"处理接收到的消息异常 {Account.GuildId}");
//             }

//             return Task.CompletedTask;
//         }

//         /// <summary>
//         /// 处理更新的消息
//         /// </summary>
//         private Task MessageUpdatedAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
//         {
//             try
//             {

//                 var msg = after as IUserMessage;
//                 if (msg == null)
//                     return Task.CompletedTask;
//                 // 检查是否属于当前账号
//                 if (!IsChannelBelongsToAccount(channel))
//                 {
//                     return Task.CompletedTask;
//                 }

//                 if (!string.IsNullOrWhiteSpace(msg.Content)
//                     && msg.Content.Contains("%")
//                     && msg.Author.IsBot)
//                 {
//                     // 使用消息处理器处理Bot消息
//                     _messageProcessor.EnqueueBotMessage(after, MessageType.UPDATE);
//                 }
//                 else if (msg.InteractionMetadata is ApplicationCommandInteractionMetadata metadata && metadata.Name == "describe")
//                 {
//                     // 使用消息处理器处理Bot消息
//                     _messageProcessor.EnqueueBotMessage(after, MessageType.UPDATE);
//                 }

//             }
//             catch (Exception ex)
//             {
//                 _logger.Error(ex, "处理更新的消息异常 {@0}", Account.ChannelId);
//             }

//             return Task.CompletedTask;
//         }

//         private Task OnClientReady()
//         {
//             LogInfo($"触发 Ready 事件, Discord 账号ID = {Account.Id},  Discord 服务器 ID = {Account.GuildId}");
//             // 通过服务器ID获取服务器
//             if (ulong.TryParse(Account.GuildId, out ulong guildId))
//             {
//                 LogInfo($"尝试获取服务器: {guildId}, 下的所有文本频道");
//                 var guild = _client.GetGuild(guildId);
//                 if (guild == null)
//                 {
//                     LogError(null, $"[Bot WebSocket] - 未找到指定服务器guildId: {Account.GuildId}");
//                     return Task.CompletedTask;
//                 }
//                 // 获取所有文本频道
//                 var textChannels = guild.Channels.Where(c => c is SocketTextChannel);
//                 foreach (SocketTextChannel channel in textChannels)
//                 {
//                     LogInfo($"自动获取服务器{guildId}下的文本频道: id - {channel.Id}, name - {channel.Name}");
//                 }
//             }

//             return Task.CompletedTask;
//         }

//         /// <summary>
//         /// 连接事件处理
//         /// </summary>
//         private Task OnConnected()
//         {
//             IsRunning = true;
//             NotifyWss(ReturnCode.SUCCESS, "");
//             LogInfo($"Bot WebSocket 已连接 {Account.GuildId}");
//             return Task.CompletedTask;
//         }

//         /// <summary>
//         /// 断开连接事件处理
//         /// </summary>
//         private Task OnDisconnected(Exception exception)
//         {
//             IsRunning = false;

//             if (exception != null)
//             {
//                 LogError(exception, $"Bot WebSocket 断开连接 {Account.GuildId}");
//                 HandleFailure(1000, exception.Message);
//             }
//             else
//             {
//                 LogInfo($"Bot WebSocket 断开连接 {Account.GuildId}");
//                 TryReconnect();
//             }

//             return Task.CompletedTask;
//         }

//         /// <summary>
//         /// 处理消息队列
//         /// </summary>
//         private void MessageQueueDoWork()
//         {
//             while (true)
//             {
//                 while (_messageQueue.TryDequeue(out var message))
//                 {
//                     try
//                     {
//                     }
//                     catch (Exception ex)
//                     {
//                         LogError(ex, $"处理消息队列时发生异常 {Account.GuildId}");
//                     }
//                 }

//                 Thread.Sleep(10);
//             }
//         }

//         /// <summary>
//         /// 处理错误
//         /// </summary>
//         /// <param name="code">错误码</param>
//         /// <param name="reason">错误原因</param>
//         private void HandleFailure(int code, string reason)
//         {
//             LogError(null, $"Bot WebSocket 连接失败, 代码 {code}: {reason}, {Account.GuildId}");

//             if (!IsRunning)
//             {
//                 NotifyWss(code, reason);
//             }

//             IsRunning = false;

//             if (code >= 4000)
//             {
//                 LogInfo($"无法重新连接， 由 {code}({reason}) 关闭 {Account.GuildId}, 尝试新连接... ");
//                 TryNewConnect();
//             }
//             else
//             {
//                 LogInfo($"由 {code}({reason}) 关闭, 尝试重新连接... {Account.GuildId}");
//                 TryReconnect();
//             }
//         }

//         /// <summary>
//         /// 尝试重新连接
//         /// </summary>
//         public void TryReconnect()
//         {
//             try
//             {
//                 if (_isDispose)
//                 {
//                     return;
//                 }

//                 var success = StartAsync(true).ConfigureAwait(false).GetAwaiter().GetResult();
//                 if (!success)
//                 {
//                     LogInfo($"重新连接失败 {Account.GuildId}，尝试新连接");

//                     Thread.Sleep(1000);
//                     TryNewConnect();
//                 }
//             }
//             catch (Exception e)
//             {
//                 LogError(e, $"重新连接异常 {Account.GuildId}，尝试新连接");

//                 Thread.Sleep(1000);
//                 TryNewConnect();
//             }
//         }

//         /// <summary>
//         /// 尝试新的连接
//         /// </summary>
//         public void TryNewConnect()
//         {
//             if (_isDispose)
//             {
//                 return;
//             }

//             var isLock = LocalLock.TryLock("BotTryNewConnect", TimeSpan.FromSeconds(3), () =>
//             {
//                 for (int i = 1; i <= CONNECT_RETRY_LIMIT; i++)
//                 {
//                     try
//                     {
//                         // 如果 5 分钟内失败次数超过限制，则禁用账号
//                         var ncKey = $"BotTryNewConnect_{Account.GuildId}";
//                         _memoryCache.TryGetValue(ncKey, out int count);
//                         if (count > CONNECT_RETRY_LIMIT)
//                         {
//                             LogInfo("新的连接失败次数超过限制，禁用账号");
//                             DisableAccount("新的连接失败次数超过限制，禁用账号");
//                             return;
//                         }
//                         _memoryCache.Set(ncKey, count + 1, TimeSpan.FromMinutes(5));

//                         var success = StartAsync(false).ConfigureAwait(false).GetAwaiter().GetResult();
//                         if (success)
//                         {
//                             return;
//                         }
//                     }
//                     catch (Exception e)
//                     {
//                         LogError(e, $"新连接失败, 第 {i} 次, {Account.GuildId}");

//                         Thread.Sleep(5000);
//                     }
//                 }

//                 if (_client == null || _client.ConnectionState != Discord.ConnectionState.Connected)
//                 {
//                     LogError(null, $"由于无法重新连接, 自动禁用Discord账号 {Account.Id}");

//                     DisableAccount("由于无法重新连接，自动禁用账号");
//                 }
//             });

//             if (!isLock)
//             {
//                 LogInfo("新的连接作业正在执行中，禁止重复执行");
//             }
//         }

//         /// <summary>
//         /// 停止并禁用账号
//         /// </summary>
//         /// <param name="msg">禁用原因</param>
//         public void DisableAccount(string msg)
//         {
//             try
//             {
//                 // 保存
//                 Account.Enable = false;
//                 Account.DisabledReason = msg;

//                 DbHelper.Instance.AccountStore.Update(Account);

//                 _discordInstance?.ClearAccountCache(Account.Id);
//                 _discordInstance?.Dispose();

//                 // 尝试自动登录
//                 var setting = GlobalConfiguration.Setting;
//                 var account = Account;
//                 if (setting.EnableAutoLogin)
//                 {
//                     try
//                     {
//                         // 开始尝试自动登录
//                         var suc = DiscordAccountHelper.AutoLogin(account, true);
//                     }
//                     catch (Exception exa)
//                     {
//                         LogError(exa, $"Bot Account({Account.GuildId}) auto login fail, disabled: {exa.Message}");
//                     }
//                 }
//             }
//             catch (Exception ex)
//             {
//                 LogError(ex, $"禁用Bot账号失败 {Account.GuildId}");
//             }
//             finally
//             {
//                 // 邮件通知
//                 var smtp = GlobalConfiguration.Setting?.Smtp;
//                 EmailJob.Instance.EmailSend(smtp, $"Bot账号禁用通知-{Account.GuildId}",
//                     $"{Account.GuildId}, {Account.DisabledReason}");
//             }
//         }

//         /// <summary>
//         /// 如果打开了，则关闭 wss
//         /// </summary>
//         /// <param name="reconnect">是否重新连接</param>
//         public void CloseSocket(bool reconnect = false)
//         {
//             try
//             {
//                 if (_client != null)
//                 {
//                     // 注销事件处理程序
//                     _client.Log -= LogAction;
//                     _client.MessageReceived -= MessageReceivedAsync;
//                     _client.MessageUpdated -= MessageUpdatedAsync;
//                     _client.Connected -= OnConnected;
//                     _client.Disconnected -= OnDisconnected;

//                     // 停止并注销
//                     try
//                     {
//                         _client.StopAsync().GetAwaiter().GetResult();
//                         _client.LogoutAsync().GetAwaiter().GetResult();
//                     }
//                     catch (Exception ex)
//                     {
//                         LogError(ex, $"Bot WebSocket 关闭异常 {Account.GuildId}");
//                     }

//                     _client.Dispose();
//                     _client = null;
//                 }
//             }
//             catch (Exception ex)
//             {
//                 LogError(ex, $"Bot WebSocket 关闭异常 {Account.GuildId}");
//             }
//             finally
//             {
//                 _client = null;
//                 LogInfo("Bot WebSocket 资源已释放");
//             }
//         }

//         /// <summary>
//         /// 通知错误或成功
//         /// </summary>
//         /// <param name="code">错误码</param>
//         /// <param name="reason">错误原因</param>
//         private void NotifyWss(int code, string reason)
//         {
//             if (!Account.Lock)
//             {
//                 Account.DisabledReason = reason;
//             }

//             // 保存
//             DbHelper.Instance.AccountStore.Update("Enable,DisabledReason", Account);
//             _discordInstance?.ClearAccountCache(Account.Id);
//         }

//         /// <summary>
//         /// 资源释放
//         /// </summary>
//         public void Dispose()
//         {
//             try
//             {
//                 CloseSocket();

//                 // 释放消息处理器
//                 _messageProcessor?.Dispose();
//                 _messageProcessor = null;

//                 IsRunning = false;
//                 _isDispose = true;

//                 // 尝试释放任务
//                 try
//                 {
//                     _messageQueueTask?.Dispose();
//                 }
//                 catch
//                 {
//                 }

//                 _client?.Dispose();
//                 _client = null;

//                 _logger.Information("Bot WebSocket 资源已释放");
//             }
//             catch (Exception ex)
//             {
//                 _logger.Error(ex, "释放 Bot WebSocket 资源异常");
//             }
//         }


//         public void LogInfo(string message)
//         {
//             _logger.Information($"{Prefix} - {message}");
//         }
//         public void LogError(Exception ex, string message)
//         {
//             _logger.Error(ex, $"{Prefix} - {message}");
//         }
//     }
// }