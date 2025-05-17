using System.Text.Json;
using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.Util;
using Midjourney.Infrastructure.Wss.Gateway;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 消息包装类，用于统一处理网关消息数据并在处理程序链中传递
    /// </summary>
    public class MessageWrapper
    {
        private readonly JsonElement _gatewayData;

        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();
        private EventData _eventData;
        // 常用字段缓存
        private string _id;
        private string _nonce;
        private string _content;
        private int? _type;
        private int? _flags;
        private InteractionMetadata _interactionMetadata;
        private List<EventDataEmbed> _embeds;
        private List<Attachment> _attachments;
        private List<Component> _components;
        private InteractionUser _author;
        private string _customId;
        private string _channelId;
        private PartialApplication _application;
        private bool _hasHandle;

        /// <summary>
        /// 使用JSON元素创建消息包装器
        /// </summary>
        /// <param name="payload">JSON数据</param>
        public MessageWrapper(JsonElement payload)
        {
            _gatewayData = payload;
            InitializeFromGateway();
        }

        /// <summary>
        /// 使用事件数据创建消息包装器
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public MessageWrapper(EventData eventData)
        {
            _eventData = eventData;

            // 获取基本属性
            _id = eventData.Id;
            _channelId = eventData.ChannelId;
            _content = eventData.Content;
            _type = eventData.Type;
            _flags = eventData.Flags;
            _customId = eventData.CustomId;

            // 获取复杂属性
            _interactionMetadata = eventData.InteractionMetadata;
            _embeds = eventData.Embeds;
            _attachments = eventData.Attachments;
            _components = eventData.Components;
            _author = eventData.Author;
            _application = eventData.Application;
        }

        /// <summary>
        /// 从网关数据初始化基本属性
        /// </summary>
        private void InitializeFromGateway()
        {
            if (_gatewayData.ValueKind == JsonValueKind.Undefined || _gatewayData.ValueKind == JsonValueKind.Array)
                return;

            // 提取基本字段
            if (_gatewayData.TryGetProperty("id", out var idElement))
                _id = idElement.GetString();
            if (_gatewayData.TryGetProperty("nonce", out var nonceElement))
                _nonce = nonceElement.GetString();

            if (_gatewayData.TryGetProperty("content", out var contentElement))
                _content = contentElement.GetString();

            if (_gatewayData.TryGetProperty("type", out var typeElement))
                _type = typeElement.GetInt32();

            if (_gatewayData.TryGetProperty("flags", out var flagsElement))
                _flags = flagsElement.GetInt32();

            if (_gatewayData.TryGetProperty("custom_id", out var customIdElement))
                _customId = customIdElement.GetString();

            if (_gatewayData.TryGetProperty("channel_id", out var channelIdElement))
                _channelId = channelIdElement.GetString();

            // 完整的事件数据
            _eventData = _gatewayData.Deserialize<EventData>();

            // 提取复杂字段
            ExtractAuthor();
            ExtractInteractionMetadata();
            ExtractAttachments();
            ExtractEmbeds();
            ExtractComponents();
            ExtractApplication();
        }

        #region 属性提取方法

        /// <summary>
        /// 提取作者信息
        /// </summary>
        private void ExtractAuthor()
        {
            if (_gatewayData.TryGetProperty("author", out var authorElement))
            {
                try
                {
                    _author = JsonSerializer.Deserialize<InteractionUser>(authorElement.GetRawText());
                }
                catch
                {
                    // 解析失败时保持尝试使用完整的事件数据
                    _author = _eventData.Author;
                }
            }
        }

        /// <summary>
        /// 提取交互元数据
        /// </summary>
        private void ExtractInteractionMetadata()
        {
            if (_gatewayData.TryGetProperty("interaction_metadata", out var metadataElement))
            {
                try
                {
                    _interactionMetadata = JsonSerializer.Deserialize<InteractionMetadata>(metadataElement.GetRawText());
                }
                catch
                {
                    // 解析失败时保持尝试使用完整的事件数据
                    _interactionMetadata = _eventData.InteractionMetadata;
                }
            }
        }

        /// <summary>
        /// 提取附件信息
        /// </summary>
        private void ExtractAttachments()
        {
            if (_gatewayData.TryGetProperty("attachments", out var attachmentsElement) &&
                attachmentsElement.ValueKind == JsonValueKind.Array)
            {
                try
                {
                    _attachments = JsonSerializer.Deserialize<List<Attachment>>(attachmentsElement.GetRawText());
                }
                catch
                {
                    // 解析失败时保持尝试使用完整的事件数据
                    _attachments = _eventData.Attachments;
                }
            }
            else
            {
                _attachments = new List<Attachment>();
            }
        }

        /// <summary>
        /// 提取嵌入内容
        /// </summary>
        private void ExtractEmbeds()
        {
            if (_gatewayData.TryGetProperty("embeds", out var embedsElement) &&
                embedsElement.ValueKind == JsonValueKind.Array)
            {
                try
                {
                    _embeds = JsonSerializer.Deserialize<List<EventDataEmbed>>(embedsElement.GetRawText());
                }
                catch
                {
                    // 解析失败时保持尝试使用完整的事件数据
                    _embeds = _eventData.Embeds;
                }
            }
            else
            {
                _embeds = new List<EventDataEmbed>();
            }
        }

        /// <summary>
        /// 提取组件信息
        /// </summary>
        private void ExtractComponents()
        {
            if (_gatewayData.TryGetProperty("components", out var componentsElement) &&
                componentsElement.ValueKind == JsonValueKind.Array)
            {
                try
                {
                    _components = JsonSerializer.Deserialize<List<Component>>(componentsElement.GetRawText());
                }
                catch
                {
                    // 解析失败时保持尝试使用完整的事件数据
                    _components = _eventData.Components;
                }
            }
            else
            {
                _components = new List<Component>();
            }
        }

        /// <summary>
        /// 提取应用信息
        /// </summary>
        private void ExtractApplication()
        {
            if (_gatewayData.TryGetProperty("application", out var applicationElement))
            {
                try
                {
                    _application = JsonSerializer.Deserialize<PartialApplication>(applicationElement.GetRawText());
                }
                catch
                {
                    // 解析失败时保持尝试使用完整的事件数据
                    _application = _eventData.Application;
                }
            }

            if (_application == null && _gatewayData.TryGetProperty("application_id", out var appIdElement))
            {
                PartialApplication app = new();
                app.Id = appIdElement.GetString();
                _application = app;
            }
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 处理这个消息的处理程序类型
        /// </summary>
        public string MessageHandler { get; set; } = "Gateway";

        /// <summary>
        /// 原始网关数据
        /// </summary>
        public JsonElement GatewayData => _gatewayData;

        /// <summary>
        /// 事件数据
        /// </summary>
        public EventData EventData => _eventData;

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content
        {
            get => _content;
            set => _content = value;
        }

        /// <summary>
        /// 消息ID
        /// </summary>
        public string Id => _id;

        /// <summary>
        /// 频道ID
        /// </summary>
        public string ChannelId => _channelId;

        /// <summary>
        /// 随机数
        /// </summary>
        public string Nonce => _nonce;

        /// <summary>
        /// 消息类型
        /// </summary>
        public int Type => _type ?? 0;

        /// <summary>
        /// 是否包含图片
        /// </summary>
        public bool HasImage => Attachments?.Count > 0;

        /// <summary>
        /// 作者信息
        /// </summary>
        public InteractionUser Author
        {
            get => _author;
            set => _author = value;
        }

        /// <summary>
        /// 交互ID
        /// </summary>
        public string InteractionId => _interactionMetadata?.Id;

        /// <summary>
        /// 交互元数据
        /// </summary>
        public InteractionMetadata InteractionMetadata
        {
            get => _interactionMetadata;
            set => _interactionMetadata = value;
        }

        /// <summary>
        /// 自定义ID
        /// </summary>
        public string CustomId
        {
            get => _customId;
            set => _customId = value;
        }

        /// <summary>
        /// 应用信息
        /// </summary>
        public PartialApplication Application
        {
            get => _application;
            set => _application = value;
        }

        /// <summary>
        /// 完整提示词
        /// </summary>
        public string FullPrompt => ConvertUtils.GetFullPrompt(Content);

        /// <summary>
        /// 消息标志
        /// </summary>
        public int Flags => _flags ?? 0;

        /// <summary>
        /// 机器人类型
        /// </summary>
        public EBotType BotType
        {
            get
            {
                var botId = Author?.Id;
                if (botId == Constants.NIJI_APPLICATION_ID)
                    return EBotType.NIJI_JOURNEY;
                else if (botId == Constants.MJ_APPLICATION_ID)
                    return EBotType.MID_JOURNEY;

                // 默认返回MJ类型
                return EBotType.MID_JOURNEY;
            }
        }

        /// <summary>
        /// 消息哈希
        /// </summary>
        public string MessageHash => string.IsNullOrEmpty(ImageUrl()) ? null : DiscordHelper.GetMessageHash(ImageUrl());

        /// <summary>
        /// 嵌入内容
        /// </summary>
        public List<EventDataEmbed> Embeds
        {
            get => _embeds;
            set => _embeds = value;
        }

        /// <summary>
        /// 附件列表
        /// </summary>
        public List<Attachment> Attachments
        {
            get => _attachments;
            set => _attachments = value;
        }

        /// <summary>
        /// 组件列表
        /// </summary>
        public List<Component> Components
        {
            get => _components;
            set => _components = value;
        }

        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool HasHandle
        {
            get => _hasHandle;
            set => _hasHandle = value;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取图片URL
        /// </summary>
        public string ImageUrl()
        {
            if (Attachments?.Count > 0)
            {
                return ReplaceCdnUrl(Attachments.FirstOrDefault()?.Url);
            }

            return default;
        }

        /// <summary>
        /// 替换CDN URL
        /// </summary>
        public string ReplaceCdnUrl(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return imageUrl;

            string cdn = DiscordHelper.GetCdn();
            if (imageUrl.StartsWith(cdn)) return imageUrl;

            return imageUrl.Replace(DiscordHelper.DISCORD_CDN_URL, cdn);
        }

        /// <summary>
        /// 从网关数据中获取指定属性的字符串值
        /// </summary>
        public string GetString(string propertyName)
        {
            if (_gatewayData.TryGetProperty(propertyName, out var element) &&
                element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }
            return null;
        }

        /// <summary>
        /// 从网关数据中获取指定属性的整数值
        /// </summary>
        public int? GetInt(string propertyName)
        {
            if (_gatewayData.TryGetProperty(propertyName, out var element) &&
                element.ValueKind == JsonValueKind.Number)
            {
                return element.GetInt32();
            }
            return null;
        }

        /// <summary>
        /// 从网关数据中获取指定属性的布尔值
        /// </summary>
        public bool? GetBool(string propertyName)
        {
            if (_gatewayData.TryGetProperty(propertyName, out var element) &&
                element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
            {
                return element.GetBoolean();
            }
            return null;
        }

        /// <summary>
        /// 从网关数据中获取指定属性的JsonElement
        /// </summary>
        public JsonElement? GetElement(string propertyName)
        {
            if (_gatewayData.TryGetProperty(propertyName, out var element))
            {
                return element;
            }
            return null;
        }

        /// <summary>
        /// 设置自定义属性
        /// </summary>
        public void SetProperty<T>(string key, T value)
        {
            _properties[key] = value;
        }

        /// <summary>
        /// 获取自定义属性
        /// </summary>
        public T GetProperty<T>(string key, T defaultValue = default)
        {
            if (_properties.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// 是否包含指定的自定义属性
        /// </summary>
        public bool HasProperty(string key)
        {
            return _properties.ContainsKey(key);
        }

        #endregion
    }
}