using System.Collections.Concurrent;
using Midjourney.Infrastructure.Wss.Handle;
using System.Text.Json;
using System.Threading.Channels;

namespace Midjourney.Infrastructure.Wss
{
    partial class DiscordSockerStarter
    {
        // private readonly ILogger _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _processingTask;

        // 使用Channel替代ConcurrentQueue，提高消息处理效率
        private readonly Channel<JsonElement> _messageChannel = Channel.CreateUnbounded<JsonElement>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        private readonly IEnumerable<MessageHandler> _messageHandlers;

        private readonly ConcurrentDictionary<string, DiscordChannelDto> _dmChannels = new();
        private readonly ConcurrentDictionary<string, DiscordExtendedGuild> _guilds = new();



        /// <summary>
        /// 添加消息到队列
        /// </summary>
        /// <param name="message">JSON消息</param>
        private void EnqueueMessage(JsonElement message)
        {
            if (_messageChannel.Writer.TryWrite(message))
            {
                _logger.Debug("消息已加入队列");
            }
            else
            {
                _logger.Warning("消息加入队列失败");
            }
        }

        /// <summary>
        /// 处理网关消息
        /// </summary>
        /// <param name="raw"></param>
        private async Task HandleGatewayEnvent(JsonElement raw)
        {
            if (!raw.TryGetProperty("t", out var messageTypeElement))
            {
                return;
            }

            // 过滤消息
            var messageType = MessageTypeExtensions.Of(messageTypeElement.GetString());
            if (messageType is null or MessageType.DELETE || !raw.TryGetProperty("d", out var data))
            {
                return;
            }
            
            // 获取 content
            if (data.TryGetProperty("content", out var contentEl))
            {
            }

            var gatewayMessage = raw.Deserialize<DiscordSocketMessage>();
            var type = gatewayMessage.Type;
            var opCode = gatewayMessage.OperationCode;
#if DEBUG
            _logger.Information("Received [{0}] Gateway Event => {@1}", type, data.ToString());
#endif
            _logger.Information("Received [{0}] Gateway Event => {@1}", type, contentEl.ToString());

            // 创建MessageWrapper
            MessageWrapper message = new MessageWrapper(data);

            // 创建处理器链
            // Remix模式切换处理器
            var remixHandler = new RemixModeHandler();
            // 账号设置信息处理器
            var settingsHandler = new SettingInfoHandler();
            // 生成模式切换处理器
            var modeHandler = new GenerationModeHandler();
            // 频道过滤处理器
            var channelHandler = new ChannelFilterHandler();
            // 随机数处理器
            var nonceHandler = new NonceHandler();

            // 组装处理器链
            remixHandler
                .SetNext(settingsHandler)
                .SetNext(modeHandler)
                .SetNext(channelHandler)
                .SetNext(nonceHandler);

            // 开始处理
            remixHandler.Handle(_discordInstance, (MessageType)messageType, message);

            // 如果消息已经处理过，则跳过
            if (message.HasHandle == true)
            {
                return;
            }

            try
            {
                switch (type)
                {
                    #region Guilds
                    case "GUILD_CREATE":
                        {
                            _logger.Debug("Received Dispatch (GUILD_CREATE) - Try Update Guild Data -");
                            var guild = data.Deserialize<DiscordExtendedGuild>();
                            if (guild is not null)
                            {

                                _logger.Debug("- Update Guild[{0}] Data, channelIds count [{1}] -", guild.Id, guild.Channels?.Length);
                                _guilds.AddOrUpdate(guild.Id, guild, (id, old) => guild);
                                await _channelSubscribeEvent.InvokeAsync(_guilds);
                            }
                        }
                        break;
                    case "GUILD_UPDATE":
                        {
                            _logger.Debug("Received Dispatch (GUILD_UPDATE)- Try Update Guild Data -");
                            var guild = data.Deserialize<DiscordExtendedGuild>();
                            if (guild is not null)
                            {

                                _logger.Debug("- Update Guild[{0}] Data, channelIds count [{1}] -", guild.Id, guild?.Channels.Length);
                                _guilds.AddOrUpdate(guild.Id, guild, (id, old) => guild);
                            }
                        }
                        break;
                    case "GUILD_DELETE":
                        {
                            _logger.Debug("Received Dispatch (GUILD_DELETE)- Try DELETE Guild Data -");
                            var guild = data.Deserialize<DiscordExtendedGuild>();
                            if (guild != null)
                            {
                                _logger.Information("- DELETE Guild[{0}] Data, channelIds count [{1}] -", guild.Id, guild?.Channels.Length);
                                // 判断是否该账号下的Guild 实例被删除
                                if (guild.Id == _discordInstance.GuildId)
                                {
                                    // TODO 通知账号的服务器被删除 - 事件订阅
                                }
                                _guilds.TryRemove(guild.Id, out guild);
                            }
                        }
                        break;
                    #endregion
                    #region Channel
                    case "CHANNEL_CREATE":
                    case "CHANNEL_DELETE":
                        {
                            _logger.Debug("Received Dispatch ({0})- Try DELETE Channel Data -", type);
                            var channel = data.Deserialize<DiscordChannelDto>();
                            // 获取对应的服务器数据
                            if (channel != null && _guilds.TryGetValue(channel.GuildId, out var guild) && guild is not null)
                            {
                                _logger.Information("服务器 - {0} - 变更频道 [{@1}] - {@2}", guild?.Id, channel.Id, channel.Name);
                                var newChannels = new List<DiscordChannelDto>();
                                if (guild?.Channels.Length > 0)
                                {
                                    newChannels.AddRange(guild.Channels.Where(c => c.Id != channel.Id));
                                }

                                if (messageType == MessageType.CHANNEL_CREATE)
                                {
                                    newChannels.Add(channel);
                                }
                                guild.Channels = newChannels.ToArray();
                                _guilds.AddOrUpdate(guild.Id, guild, (id, old) => guild);
                                // 触发事件订阅
                                await _channelSubscribeEvent.InvokeAsync(_guilds);
                            }
                        }
                        break;
                    case "CHANNEL_UPDATE":
                        {
                            _logger.Debug("Received Dispatch (CHANNEL_UPDATE)- Try Update Channel Data -");
                        }
                        break;
                    #endregion
                    #region 触发 CF 真人验证
                    case "INTERACTION_IFRAME_MODAL_CREATE":
                        {
                            if (data.TryGetProperty("title", out var t))
                            {
                                if (t.GetString() == "Action required to continue")
                                {
                                    _logger.Warning("CF 验证 {@0}, {@1}", Account.ChannelId, raw.ToString());
                                    CloudflareHandle cfHandle = new();
                                    // 交给 CloudflareHandle 处理
                                    cfHandle?.Handle(_discordInstance, MessageType.CREATE, new MessageWrapper(data));
                                    return;
                                }
                            }
                        }
                        break;
                    #endregion
                    default:
                        var eventData = message.EventData;

                        // 如果消息类型是 CREATE
                        // 则再次处理消息确认事件，确保消息的高可用
                        if (messageType == MessageType.CREATE)
                        {
                            Thread.Sleep(50);

                            if (eventData != null &&
                                (_discordInstance.AllChannelIds.Contains(eventData.ChannelId) || Account.SubChannelValues.ContainsKey(eventData.ChannelId)))
                            {
                                foreach (var messageHandler in _messageHandlers.OrderBy(h => h.Order()))
                                {
                                    // 处理过了
                                    if (eventData.GetProperty<bool?>(Constants.MJ_MESSAGE_HANDLED, default) == true)
                                    {
                                        return;
                                    }
                                    // 判断消息是否处理过了
                                    CacheHelper<string, bool>.TryAdd(message.Id, false);
                                    if (CacheHelper<string, bool>.Get(message.Id))
                                    {
                                        return;
                                    }

                                    // 消息加锁处理
                                    LocalLock.TryLock($"lock_{eventData.Id}", TimeSpan.FromSeconds(10), () =>
                                    {
                                        messageHandler.Handle(_discordInstance, messageType.Value, message);
                                    });
                                }
                            }
                        }
                        // describe 重新提交
                        // MJ::Picread::Retry
                        else if (eventData.Embeds.Count > 0 && eventData.Author?.Bot == true && eventData.Components.Count > 0
                            && eventData.Components.First().Components.Any(x => x.CustomId?.Contains("PicReader") == true))
                        {
                            // 消息加锁处理
                            LocalLock.TryLock($"lock_{eventData.Id}", TimeSpan.FromSeconds(10), () =>
                            {
                                var em = eventData.Embeds.FirstOrDefault();
                                if (em != null && !string.IsNullOrWhiteSpace(em.Description))
                                {
                                    var handler = _messageHandlers.FirstOrDefault(x => x.GetType() == typeof(DescribeSuccessHandler));
                                    handler?.Handle(_discordInstance, MessageType.CREATE, message);
                                }
                            });
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(eventData.Content)
                                  && eventData.Content.Contains("%")
                                  && eventData.Author?.Bot == true)
                            {
                                // 消息加锁处理
                                LocalLock.TryLock($"lock_{eventData.Id}", TimeSpan.FromSeconds(10), () =>
                                {
                                    var handler = _messageHandlers.FirstOrDefault(x => x.GetType() == typeof(StartAndProgressHandler));
                                    handler?.Handle(_discordInstance, MessageType.UPDATE, message);
                                });
                            }
                            else if (eventData.InteractionMetadata?.Name == "describe")
                            {
                                // 消息加锁处理
                                LocalLock.TryLock($"lock_{eventData.Id}", TimeSpan.FromSeconds(10), () =>
                                {
                                    var handler = _messageHandlers.FirstOrDefault(x => x.GetType() == typeof(DescribeSuccessHandler));
                                    handler?.Handle(_discordInstance, MessageType.CREATE, message);
                                });
                            }
                            else if (eventData.InteractionMetadata?.Name == "shorten"
                                // shorten show details -> PromptAnalyzerExtended
                                || eventData.Embeds?.FirstOrDefault()?.Footer?.Text.Contains("Click on a button to imagine one of the shortened prompts") == true)
                            {
                                // 消息加锁处理
                                LocalLock.TryLock($"lock_{eventData.Id}", TimeSpan.FromSeconds(10), () =>
                                {
                                    var handler = _messageHandlers.FirstOrDefault(x => x.GetType() == typeof(ShortenSuccessHandler));
                                    handler?.Handle(_discordInstance, MessageType.CREATE, message);
                                });
                            }
                        }
                        break;
                }

            }
            catch (Exception ex)
            {
                ex.Data["opcode"] = opCode;
                ex.Data["type"] = type;
                ex.Data["payload_data"] = data.ToString();
                _logger.Error(ex, $"Error handling {opCode}{(type != null ? $" ({type})" : "")}");
            }

        }

        /// <summary>
        /// 处理消息队列
        /// </summary>
        private async Task ProcessMessages()
        {
            try
            {
                _logger.Information("消息处理任务已启动");

                // 等待直到有消息可读或被取消
                while (await _messageChannel.Reader.WaitToReadAsync(_cancellationTokenSource.Token).ConfigureAwait(false))
                {
                    // 尝试读取所有可用消息
                    while (_messageChannel.Reader.TryRead(out var message))
                    {
                        try
                        {
                            //  事件处理
                            _ = HandleGatewayEnvent(message);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "处理消息时发生异常");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Information("消息处理任务已取消");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "消息处理任务异常");
            }
            finally
            {
                _logger.Information("消息处理任务已结束");
            }
        }

        /// <summary>
        /// 资源释放
        /// </summary>
        private void MessageProcessorDispose()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                _messageChannel.Writer.Complete();

                // 等待处理任务完成
                var waitTask = Task.WhenAny(_processingTask, Task.Delay(1000));
                waitTask.ConfigureAwait(false).GetAwaiter().GetResult();

                _cancellationTokenSource.Dispose();

                _logger.Information("消息处理器已释放");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "释放消息处理器时发生异常");
            }
        }


        private static Dictionary<string, string> ParseDiscordData(string input)
        {
            var data = new Dictionary<string, string>();

            foreach (var line in input.Split('\n'))
            {
                var parts = line.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Replace("**", "").Trim();
                    var value = parts[1].Trim();
                    data[key] = value;
                }
            }

            return data;
        }

    }
}
