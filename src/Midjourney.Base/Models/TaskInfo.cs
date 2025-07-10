﻿

using FreeSql.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;
using Midjourney.Base.Data;
using Midjourney.Base.Dto;
using Midjourney.Base.Storage;
using Serilog;

namespace Midjourney.Base.Models
{
    /// <summary>
    /// 任务类，表示一个任务的基本信息。
    /// </summary>
    [BsonCollection("task")]
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    [Serializable]
    [Index("i_UserId", "UserId")]
    [Index("i_ClientIp", "ClientIp")]
    [Index("i_InstanceId", "InstanceId")]
    [Index("i_ChannelId", "ChannelId")]
    [Index("i_SubmitTime", "SubmitTime")]
    [Index("i_Status", "Status")]
    [Index("i_Action", "Action")]
    [Index("i_ParentId", "ParentId")]
    public class TaskInfo : DomainObject
    {
        public TaskInfo()
        {
        }

        /// <summary>
        /// 父级 ID
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// bot 类型，mj(默认)或niji
        /// MID_JOURNEY | 枚举值: NIJI_JOURNEY
        /// </summary>
        public EBotType BotType { get; set; }

        /// <summary>
        /// 真实的 bot 类型，mj(默认)或niji
        /// MID_JOURNEY | 枚举值: NIJI_JOURNEY
        /// 当开启 niji 转 mj 时，这里记录的是 mj bot
        /// </summary>
        public EBotType? RealBotType { get; set; }

        /// <summary>
        /// 绘画用户 ID
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 白名单用户（加入白名单不受限流控制）
        /// </summary>
        public bool IsWhite { get; set; } = false;

        /// <summary>
        /// 提交作业的唯一ID。
        /// </summary>
        public string Nonce { get; set; }

        /// <summary>
        /// 与 MJ 交互成功后消息 ID。
        /// INTERACTION_SUCCESS
        /// </summary>
        public string InteractionMetadataId { get; set; }

        /// <summary>
        /// 消息 ID（MJ 消息 ID，Nonce 与 MessageId 对应）
        /// 最终消息 ID
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// Remix 模式时，返回的消息 ID
        /// Remix Modal 消息 ID
        /// </summary>
        public string RemixModalMessageId { get; set; }

        /// <summary>
        /// 表示是否为 Remix 自动提交任务
        /// </summary>
        public bool RemixAutoSubmit { get; set; }

        /// <summary>
        /// Remix 模式，处于弹窗模式中时
        /// </summary>
        public bool RemixModaling { get; set; }

        /// <summary>
        /// 账号实例 ID = 服务器ID
        /// </summary>
        public string InstanceId { get; set; }
        
        /// <summary>
        /// 频道 ID
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// 子频道 ID
        /// </summary>
        public string SubInstanceId { get; set; }

        /// <summary>
        /// 消息 ID
        /// 创建消息 ID -> 进度消息 ID -> 完成消息 ID
        /// </summary>
        [JsonMap]
        public List<string> MessageIds { get; set; } = new List<string>();

        /// <summary>
        /// 任务类型。
        /// </summary>
        public TaskAction? Action { get; set; }

        /// <summary>
        /// 任务状态。
        /// </summary>
        public TaskStatus? Status { get; set; }

        /// <summary>
        /// 提示词。
        /// </summary>
        [Column(StringLength = -1)]
        public string Prompt { get; set; }

        /// <summary>
        /// 提示词（英文）。
        /// </summary>
        [Column(StringLength = -1)]
        public string PromptEn { get; set; }

        /// <summary>
        /// 提示词（由 mj 返回的完整提示词）
        /// </summary>
        [Column(StringLength = -1)]
        public string PromptFull { get; set; }

        /// <summary>
        /// 任务描述。
        /// </summary>
        [Column(StringLength = -1)]
        public string Description { get; set; }

        /// <summary>
        /// 自定义参数。
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// 提交时间。
        /// </summary>
        public long? SubmitTime { get; set; }

        /// <summary>
        /// 开始执行时间。
        /// </summary>
        public long? StartTime { get; set; }

        /// <summary>
        /// 结束时间。
        /// </summary>
        public long? FinishTime { get; set; }

        /// <summary>
        /// 图片URL。
        /// </summary>
        [Column(StringLength = 1024)]
        public string ImageUrl { get; set; }

        /// <summary>
        /// 缩略图 url
        /// </summary>
        [Column(StringLength = 1024)]
        public string ThumbnailUrl { get; set; }

        /// <summary>
        /// 任务进度。
        /// </summary>
        public string Progress { get; set; }

        /// <summary>
        /// 失败原因。
        /// </summary>
        [Column(StringLength = -1)]
        public string FailReason { get; set; }

        /// <summary>
        /// 是否为悠船任务
        /// </summary>
        public bool IsPartner { get; set; }

        /// <summary>
        /// 悠船任务 ID
        /// </summary>
        public string PartnerTaskId { get; set; }

        /// <summary>
        /// 悠船任务
        /// </summary>
        [JsonMap]
        public YouChuanTask PartnerTaskInfo { get; set; }

        /// <summary>
        /// 是否为官方任务
        /// </summary>
        public bool IsOfficial { get; set; }

        /// <summary>
        /// 官方任务 ID
        /// </summary>
        public string OfficialTaskId { get; set; }

        /// <summary>
        /// 官方任务
        /// </summary>
        [JsonMap]
        public OfficialJobStatus OfficialTaskInfo { get; set; }

        /// <summary>
        /// 按钮
        /// </summary>
        [JsonMap]
        public List<CustomComponentModel> Buttons { get; set; } = new List<CustomComponentModel>();

        /// <summary>
        /// 任务的显示信息。
        /// </summary>
        [LiteDB.BsonIgnore]
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        [Column(IsIgnore = true)]
        public Dictionary<string, object> Displays
        {
            get
            {
                var dic = new Dictionary<string, object>();

                // 状态
                dic["status"] = Status.ToString();

                // 转为可视化时间
                dic["submitTime"] = SubmitTime?.ToDateTimeString();
                dic["startTime"] = StartTime?.ToDateTimeString();
                dic["finishTime"] = FinishTime?.ToDateTimeString();

                // 行为
                dic["action"] = Action.ToString();

                // discord 实例 ID
                dic["discordInstanceId"] = Properties.ContainsKey("discordInstanceId") ? Properties["discordInstanceId"] : "";

                return dic;
            }
        }

        /// <summary>
        /// 任务的种子。
        /// </summary>
        public string Seed { get; set; }

        /// <summary>
        /// Seed 消息 ID
        /// </summary>
        public string SeedMessageId { get; set; }

        /// <summary>
        /// 绘图任务客户的 IP 地址
        /// </summary>
        public string ClientIp { get; set; }

        /// <summary>
        /// 图片 ID / 图片 hash
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// 是否为 replicate 任务
        /// </summary>
        public bool IsReplicate { get; set; }

        /// <summary>
        /// 人脸源图片
        /// </summary>
        [Column(StringLength = 1024)]
        public string ReplicateSource { get; set; }

        /// <summary>
        /// 目标图片/目标视频
        /// </summary>
        [Column(StringLength = 1024)]
        public string ReplicateTarget { get; set; }

        /// <summary>
        /// 当前绘画客户端指定的速度模式
        /// </summary>
        public GenerationSpeedMode? Mode { get; set; }

        /// <summary>
        /// 账号过滤
        /// </summary>
        [JsonMap]
        public AccountFilter AccountFilter { get; set; }

        /// <summary>
        /// 原始内容 - 获取图片的 URL
        /// </summary>
        [Column(StringLength = 1024)]
        public string Url { get; set; }

        /// <summary>
        /// 原始内容 - 获取图片的代理 URL
        /// </summary>
        [Column(StringLength = 1024)]
        public string ProxyUrl { get; set; }

        /// <summary>
        /// 原始内容 - 图片高度
        /// </summary>
        public int? Height { get; set; }

        /// <summary>
        /// 原始内容 - 图片宽度
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// 原始内容 - 图片大小
        /// </summary>
        public long? Size { get; set; }

        /// <summary>
        /// 原始内容 - 内容类型
        /// </summary>
        [Column(StringLength = 200)]
        public string ContentType { get; set; }

        /// <summary>
        /// 原始内容 - 图片宽度
        /// </summary>
        [LiteDB.BsonIgnore]
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
        [Column(IsIgnore = true)]
        public int? ImageHeight => Height;

        /// <summary>
        /// 原始内容 - 图片高度
        /// </summary>
        [LiteDB.BsonIgnore]
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
        [Column(IsIgnore = true)]
        public int? ImageWidth => Width;

        /// <summary>
        /// 获取当前绘图的速度模式
        /// </summary>
        /// <returns></returns>
        public GenerationSpeedMode? GetMode()
        {
            // 如果自身有速度模式
            if (Mode != null)
            {
                return Mode;
            }

            // 如果过滤参数中有速度模式，则直接返回
            if (AccountFilter != null && AccountFilter.Modes?.Count > 0)
            {
                return AccountFilter.Modes.FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(Prompt))
            {
                // 解析提示词
                var prompt = Prompt.ToLower();

                // 解析速度模式
                if (prompt.Contains("--fast"))
                {
                    return GenerationSpeedMode.FAST;
                }
                else if (prompt.Contains("--relax"))
                {
                    return GenerationSpeedMode.RELAX;
                }
                else if (prompt.Contains("--turbo"))
                {
                    return GenerationSpeedMode.TURBO;
                }
                else
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取当前绘图的速度模式字符串表示。
        /// </summary>
        /// <returns></returns>
        public string GetModeString()
        {
            return Mode switch
            {
                GenerationSpeedMode.FAST => "fast",
                GenerationSpeedMode.RELAX => "relax",
                GenerationSpeedMode.TURBO => "turbo",
                _ => "relax"
            };
        }

        /// <summary>
        /// 启动任务。
        /// </summary>
        public void Start()
        {
            StartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Status = TaskStatus.SUBMITTED;
            Progress = "0%";
        }

        /// <summary>
        /// 任务成功。
        /// </summary>
        public void Success()
        {
            try
            {
                // 保存图片
                StorageHelper.DownloadFile(this);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "保存图片失败 {@0}", ImageUrl);
            }

            // 调整图片 ACTION
            // 如果是 show 时
            if (Action == TaskAction.SHOW)
            {
                // 根据 buttons 调整
                if (Buttons.Count > 0)
                {
                    // U1
                    if (Buttons.Any(x => x.CustomId?.Contains("MJ::JOB::upsample::1") == true))
                    {
                        Action = TaskAction.IMAGINE;
                    }
                    // 局部重绘说明是放大
                    else if (Buttons.Any(x => x.CustomId?.Contains("MJ::Inpaint::") == true))
                    {
                        Action = TaskAction.UPSCALE;
                    }
                    // MJ::Job::PicReader
                    else if (Buttons.Any(x => x.CustomId?.Contains("MJ::Job::PicReader") == true))
                    {
                        Action = TaskAction.DESCRIBE;
                    }
                }
            }

            FinishTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Status = TaskStatus.SUCCESS;
            Progress = "100%";

            UpdateUserDrawCount();
        }

        /// <summary>
        /// 任务失败。
        /// </summary>
        /// <param name="reason">失败原因。</param>
        public void Fail(string reason)
        {
            FinishTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Status = TaskStatus.FAILURE;
            FailReason = reason;
            Progress = "";

            if (!string.IsNullOrWhiteSpace(reason))
            {
                if (reason.Contains("Banned prompt detected", StringComparison.OrdinalIgnoreCase)
                    || reason.Contains("Image denied", StringComparison.OrdinalIgnoreCase))
                {
                    // 触发提示提示词封锁
                    var band = GlobalConfiguration.Setting?.BannedLimiting;
                    var cache = GlobalConfiguration.MemoryCache;

                    // 记录累计触发次数
                    if (band?.Enable == true && cache != null)
                    {
                        if (!string.IsNullOrWhiteSpace(UserId))
                        {
                            // user band
                            var bandKey = $"banned:{DateTime.Now.Date:yyyyMMdd}:{UserId}";
                            cache.TryGetValue(bandKey, out int limit);
                            limit++;
                            cache.Set(bandKey, limit, TimeSpan.FromDays(1));
                        }

                        if (true)
                        {
                            // ip band
                            var bandKey = $"banned:{DateTime.Now.Date:yyyyMMdd}:{ClientIp}";
                            cache.TryGetValue(bandKey, out int limit);
                            limit++;
                            cache.Set(bandKey, limit, TimeSpan.FromDays(1));
                        }
                    }
                }
            }

            UpdateUserDrawCount();
        }

        /// <summary>
        /// 更新用户绘图次数。
        /// </summary>
        public void UpdateUserDrawCount()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(UserId))
                {
                    var model = DbHelper.Instance.UserStore.Get(UserId);
                    if (model != null)
                    {
                        if (model.TotalDrawCount <= 0)
                        {
                            // 重新计算
                            model.TotalDrawCount = (int)DbHelper.Instance.TaskStore.Count(x => x.UserId == UserId);
                        }
                        else
                        {
                            model.TotalDrawCount += 1;
                        }

                        // 今日日期
                        var nowDate = new DateTimeOffset(DateTime.Now.Date).ToUnixTimeMilliseconds();

                        // 计算今日绘图次数
                        model.DayDrawCount = (int)DbHelper.Instance.TaskStore.Count(x => x.SubmitTime >= nowDate && x.UserId == UserId);

                        DbHelper.Instance.UserStore.Update("DayDrawCount,TotalDrawCount", model);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新用户绘图次数失败");
            }
        }

        /// <summary>
        /// 设置放大按钮。
        /// </summary>
        public void SetUpscaleButtons(string id, string version = "v6")
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            // 清除现有按钮
            Buttons.Clear();

            // 替换占位符
            var buttonsJson = GlobalConfiguration.ResourcesParamsMap["upscale_buttons"].Replace("{{id}}", id).Replace("{{version}}", version);
            try
            {
                // 反序列化 JSON 并添加到按钮列表

                var buttons = System.Text.Json.JsonSerializer.Deserialize<List<CustomComponentModel>>(buttonsJson, new System.Text.Json.JsonSerializerOptions()
                {
                    // 忽略大小写
                    PropertyNameCaseInsensitive = true,

                    //// 允许 Unicode 字符（包括 emoji）不被转义
                    //Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                if (buttons != null)
                {
                    Buttons.AddRange(buttons);
                }

                // 增加重绘操作
                Buttons.Add(new CustomComponentModel
                {
                    CustomId = $"MJ::JOB::reroll::0::{id}::SOLO",
                    Label = "",
                    Emoji = "\uD83D\uDD04",
                    Style = 2,
                    Type = 2
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "设置放大按钮失败，JSON 解析错误");
            }
        }

        /// <summary>
        /// 设置描述按钮。
        /// </summary>
        /// <param name="result"></param>
        public void SetDescribeButtions(string[] result)
        {
            if (result == null || result.Length <= 0)
                return;

            var json = """
                        [
                          {
                            "customId": "MJ::Job::PicReader::1",
                            "emoji": "\u0031\u20e3",
                            "label": "",
                            "style": 2,
                            "type": 2
                          },
                          {
                            "customId": "MJ::Job::PicReader::2",
                            "emoji": "\u0032\u20e3",
                            "label": "",
                            "style": 2,
                            "type": 2
                          },
                          {
                            "customId": "MJ::Job::PicReader::3",
                            "emoji": "\u0033\u20e3",
                            "label": "",
                            "style": 2,
                            "type": 2
                          },
                          {
                            "customId": "MJ::Job::PicReader::4",
                            "emoji": "\u0034\u20e3",
                            "label": "",
                            "style": 2,
                            "type": 2
                          },
                          {
                            "customId": "MJ::Picread::Retry",
                            "emoji": "\ud83d\udd04",
                            "label": "",
                            "style": 2,
                            "type": 2
                          }
                        ]
                        """;

            try
            {
                // 反序列化 JSON 并添加到按钮列表
                var buttons = System.Text.Json.JsonSerializer.Deserialize<List<CustomComponentModel>>(json, new System.Text.Json.JsonSerializerOptions()
                {
                    // 忽略大小写
                    PropertyNameCaseInsensitive = true,
                });
                if (buttons != null)
                {
                    Buttons.Clear();
                    Buttons.AddRange(buttons);
                }

                // 设置描述结果
                for (int i = 0; i < result.Length; i++)
                {
                    if (i < Buttons.Count)
                    {
                        Buttons[i].Label = result[i];
                    }
                }

                // 移除 label 为空的 button
                Buttons.RemoveAll(c => string.IsNullOrWhiteSpace(c.Label));

                foreach (var item in Buttons)
                {
                    item.Label = "";
                }

                PromptEn = string.Join("\r\n", result);
                SetProperty(Constants.TASK_PROPERTY_FINAL_PROMPT, string.Join("\r\n", result));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "设置描述按钮失败，JSON 解析错误");
            }
        }
    }
}