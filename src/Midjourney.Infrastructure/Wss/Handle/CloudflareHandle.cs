using System.Net;
using System.Text.Json;
using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.LoadBalancer;
using Midjourney.Infrastructure.Util;
using RestSharp;
using Serilog;

namespace Midjourney.Infrastructure.Wss.Handle
{
    public class CloudflareHandle : MessageHandler
    {
        public CloudflareHandle()
        {
        }

        public override string MessageHandleType => "CloudflareHandle";

        public override int Order() => 99999;

        protected override void HandleMessage(DiscordInstance instance, MessageType messageType, MessageWrapper message)
        {
            DiscordAccount account = instance.Account;
            Log.Information("开始处理Discord账号[{0}] 的 CF 真人验证 - messageId: {@1}", account.Id, message.Id);
            // 全局锁定中
            // 等待人工处理或者自动处理
            // 重试最多 3 次，最多处理 5 分钟
            LocalLock.TryLock($"cf_{account.GuildId}", TimeSpan.FromSeconds(10), () =>
            {
                try
                {
                    var custom_id = message?.CustomId;
                    var application_id = message?.Application?.Id.ToString();
                    if (!string.IsNullOrWhiteSpace(custom_id) && !string.IsNullOrWhiteSpace(application_id))
                    {
                        account.Lock = true;

                        // MJ::iframe::U3NmeM-lDTrmTCN_QY5n4DXvjrQRPGOZrQiLa-fT9y3siLA2AGjhj37IjzCqCtVzthUhGBj4KKqNSntQ
                        var hash = custom_id.Split("::").LastOrDefault();
                        var hashUrl = $"https://{application_id}.discordsays.com/captcha/api/c/{hash}/ack?hash=1";

                        // 验证中，处于锁定模式
                        account.DisabledReason = "CF 自动验证中...";
                        account.CfHashUrl = hashUrl;
                        account.CfHashCreated = DateTime.Now;

                        DbHelper.Instance.AccountStore.Update(account);
                        instance.ClearAccountCache(account.Id);

                        try
                        {
                            // 通知验证服务器
                            if (!string.IsNullOrWhiteSpace(GlobalConfiguration.Setting.CaptchaNotifyHook) && !string.IsNullOrWhiteSpace(GlobalConfiguration.Setting.CaptchaServer))
                            {
                                // 使用 restsharp 通知，最多 3 次
                                var notifyCount = 0;
                                do
                                {
                                    if (notifyCount > 3)
                                    {
                                        break;
                                    }

                                    notifyCount++;
                                    var notifyUrl = $"{GlobalConfiguration.Setting.CaptchaServer.Trim().TrimEnd('/')}/cf/verify";
                                    var client = new RestClient();
                                    var request = new RestRequest(notifyUrl, Method.Post);
                                    request.AddHeader("Content-Type", "application/json");
                                    var body = new CaptchaVerfyRequest
                                    {
                                        Url = hashUrl,
                                        State = account.ChannelId,
                                        NotifyHook = GlobalConfiguration.Setting.CaptchaNotifyHook,
                                        Secret = GlobalConfiguration.Setting.CaptchaNotifySecret
                                    };
                                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(body);
                                    request.AddJsonBody(json);
                                    var response = client.Execute(request);
                                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        // 已通知自动验证服务器
                                        Log.Information("CF 验证，已通知服务器 {@0}, {@1}", account.GuildId, hashUrl);

                                        break;
                                    }

                                    Thread.Sleep(1000);
                                } while (true);

                                // 发送邮件
                                EmailJob.Instance.EmailSend(GlobalConfiguration.Setting.Smtp, $"CF自动真人验证-{account.GuildId}", hashUrl);
                            }
                            else
                            {
                                // 发送 hashUrl GET 请求, 返回 {"hash":"OOUxejO94EQNxsCODRVPbg","token":"dXDm-gSb4Zlsx-PCkNVyhQ"}
                                // 通过 hash 和 token 拼接验证 CF 验证 URL

                                WebProxy webProxy = null;
                                var proxy = GlobalConfiguration.Setting.Proxy;
                                if (!string.IsNullOrEmpty(proxy?.Host))
                                {
                                    webProxy = new WebProxy(proxy.Host, proxy.Port ?? 80);
                                }
                                var hch = new HttpClientHandler
                                {
                                    UseProxy = webProxy != null,
                                    Proxy = webProxy
                                };

                                var httpClient = new HttpClient(hch);
                                var response = httpClient.GetAsync(hashUrl).Result;
                                var con = response.Content.ReadAsStringAsync().Result;
                                if (!string.IsNullOrWhiteSpace(con))
                                {
                                    // 解析
                                    var json = JsonSerializer.Deserialize<JsonElement>(con);
                                    if (json.TryGetProperty("hash", out var h) && json.TryGetProperty("token", out var to))
                                    {
                                        var hashStr = h.GetString();
                                        var token = to.GetString();

                                        if (!string.IsNullOrWhiteSpace(hashStr) && !string.IsNullOrWhiteSpace(token))
                                        {
                                            // 发送验证 URL
                                            // 通过 hash 和 token 拼接验证 CF 验证 URL
                                            // https://editor.midjourney.com/captcha/challenge/index.html?hash=OOUxejO94EQNxsCODRVPbg&token=dXDm-gSb4Zlsx-PCkNVyhQ

                                            var url = $"https://editor.midjourney.com/captcha/challenge/index.html?hash={hashStr}&token={token}";

                                            Log.Information($"{account.GuildId}, CF 真人验证 URL: {url}");

                                            account.CfUrl = url;

                                            // 发送邮件
                                            EmailJob.Instance.EmailSend(GlobalConfiguration.Setting.Smtp, $"CF手动真人验证-{account.GuildId}", url);
                                        }
                                    }
                                }

                                account.DisabledReason = "CF 人工验证...";

                                DbHelper.Instance.AccountStore.Update(account);
                                instance.ClearAccountCache(account.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "CF 真人验证处理失败 {@0}", account.GuildId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "CF 真人验证处理异常 {@0}", account.GuildId);
                }
                finally
                {
                    // 标记为已处理
                    CacheHelper<string, bool>.AddOrUpdate(message.Id, true);
                }
            });

        }
    }
}
