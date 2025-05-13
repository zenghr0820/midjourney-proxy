using System.Text.Json;
using Discord.WebSocket;
using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.Util;

namespace Midjourney.Infrastructure.Wss.Handle
{
    /// <summary>
    /// 消息包装类，用于统一处理 SocketMessage 和 EventData
    /// </summary>
    public class MessageWrapper
    {
        private readonly SocketMessage _socketMessage;
        private readonly EventData _eventData;
        private readonly DiscordHelper _discordHelper;
        private readonly bool _isSocketMessage;
        private readonly InteractionMetadata _interactionMetadata;
        private readonly List<EventDataEmbed> _embeds;
        private readonly List<Attachment> _attachments;
        private readonly List<Component> _components;
        private readonly InteractionUser _author;

        public MessageWrapper(SocketMessage socketMessage, DiscordHelper discordHelper)
        {
            _socketMessage = socketMessage;
            _discordHelper = discordHelper;
            _isSocketMessage = true;

            // 获取交互元数据
            if (_socketMessage is SocketUserMessage umsg && umsg?.InteractionMetadata != null)
            {
                _interactionMetadata = JsonSerializer.Deserialize<InteractionMetadata>(JsonSerializer.Serialize(umsg?.InteractionMetadata));
            }

            // 获取附件数据
            _attachments = JsonSerializer.Deserialize<List<Attachment>>(JsonSerializer.Serialize(_socketMessage.Attachments));

            // 获取embeds数据
            _embeds = JsonSerializer.Deserialize<List<EventDataEmbed>>(JsonSerializer.Serialize(_socketMessage.Embeds));

            // 获取组件数据
            _components = JsonSerializer.Deserialize<List<Component>>(JsonSerializer.Serialize(_socketMessage.Components));

            // 获取作者
            _author = JsonSerializer.Deserialize<InteractionUser>(JsonSerializer.Serialize(_socketMessage.Author));
        }

        public MessageWrapper(EventData eventData, DiscordHelper discordHelper)
        {
            _eventData = eventData;
            _discordHelper = discordHelper;
            _isSocketMessage = false;

            // 获取交互元数据
            _interactionMetadata = eventData.InteractionMetadata;

            // 获取附件数据
            _attachments = eventData.Attachments;

            // 获取embeds数据
            _embeds = eventData.Embeds;

            // 获取组件数据
            _components = eventData.Components;

            // 获取作者
            _author = eventData.Author;
        }

        public string MessageHandler => _isSocketMessage ? "Bot Wss" : "User Wss";

        /// <summary>
        /// 获取消息内容
        /// </summary>
        public string Content => _isSocketMessage ? _socketMessage?.Content : _eventData?.Content;

        /// <summary>
        /// 获取消息ID
        /// </summary>
        public string Id => _isSocketMessage ? _socketMessage?.Id.ToString() : _eventData?.Id;

        /// <summary>
        /// 获取消息类型
        /// </summary>
        public int Type => (int)(_isSocketMessage ? (int)_socketMessage?.Type : _eventData?.Type);

        /// <summary>
        /// 是否包含图片
        /// </summary>
        public bool HasImage => Attachments?.Count > 0;

        /// <summary>
        /// 获取作者
        /// </summary>
        public InteractionUser Author => _author;

        /// <summary>
        /// 获取交互ID
        /// </summary>
        public string InteractionId => _interactionMetadata?.Id;

        public InteractionMetadata InteractionMetadata => _interactionMetadata;

        /// <summary>
        /// 获取完整提示
        /// </summary>
        public string FullPrompt => ConvertUtils.GetFullPrompt(Content);


        public int Flags => _isSocketMessage ? Convert.ToInt32(_socketMessage.Flags) : _eventData.Flags;

        /// <summary>
        /// 获取机器人类型
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
        /// 获取消息哈希
        /// </summary>
        public string MessageHash => string.IsNullOrEmpty(ImageUrl()) ? null : DiscordHelper.GetMessageHash(ImageUrl());

        public SocketMessage SocketMessage => _isSocketMessage ? _socketMessage : null;

        public EventData EventData => _isSocketMessage ? null : _eventData;

        public bool IsSocketMessage => _isSocketMessage;

        public List<EventDataEmbed> Embeds => _embeds;

        /// <summary>
        /// 附件列表。
        /// </summary>
        public List<Attachment> Attachments => _attachments;

        /// <summary>
        /// 组件列表。
        /// </summary>
        public List<Component> Components => _components;

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

        public string ReplaceCdnUrl(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return imageUrl;

            string cdn = _discordHelper.GetCdn();
            if (imageUrl.StartsWith(cdn)) return imageUrl;

            return imageUrl.Replace(DiscordHelper.DISCORD_CDN_URL, cdn);
        }

    }
}