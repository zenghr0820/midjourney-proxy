
namespace Midjourney.Base
{
    /// <summary>
    /// 用户状态
    /// </summary>
    public enum EUserStatus
    {
        /// <summary>
        /// 正常
        /// </summary>
        NORMAL = 0,

        /// <summary>
        /// 禁用
        /// </summary>
        DISABLED = 1
    }

    /// <summary>
    /// 用户角色
    /// </summary>
    public enum EUserRole
    {
        /// <summary>
        /// 普通用户
        /// </summary>
        USER = 0,

        /// <summary>
        /// 管理员
        /// </summary>
        ADMIN = 1,
    }

    /// <summary>
    /// Bot类型枚举 MID_JOURNEY | 枚举值: NIJI_JOURNEY
    /// </summary>
    public enum EBotType
    {
        /// <summary>
        /// Midjourney
        /// </summary>
        MID_JOURNEY = 0,

        /// <summary>
        /// Niji
        /// </summary>
        NIJI_JOURNEY = 1,

        /// <summary>
        /// 换脸
        /// </summary>
        INSIGHT_FACE = 2
    }

    /// <summary>
    /// 账号选择规则
    /// </summary>
    public enum AccountChooseRule
    {
        /// <summary>
        /// 最优空闲模式
        /// </summary>
        BestWaitIdle = 0,

        /// <summary>
        /// 随机模式
        /// </summary>
        Random = 1,

        /// <summary>
        /// 权重模式
        /// </summary>
        Weight = 2,

        /// <summary>
        /// 轮询模式
        /// </summary>
        Polling = 3
    }

    public enum TranslateWay
    {
        /// <summary>
        /// 百度翻译
        /// </summary>
        BAIDU,

        /// <summary>
        /// GPT翻译
        /// </summary>
        GPT,

        /// <summary>
        /// 不翻译
        /// </summary>
        NULL
    }

    /// <summary>
    /// 图片存储类型
    /// </summary>
    public enum ImageStorageType
    {
        /// <summary>
        /// 不存储
        /// </summary>
        NONE = 0,

        /// <summary>
        /// 本地存储
        /// </summary>
        LOCAL = 1,

        /// <summary>
        /// 阿里云存储 OSS
        /// </summary>
        OSS = 2,

        /// <summary>
        /// 腾讯云 COS
        /// </summary>
        COS = 3,

        /// <summary>
        /// Cloudflare R2
        /// </summary>
        R2 = 4,
    }

    /// <summary>
    /// 任务状态枚举.
    /// </summary>
    public enum TaskStatus
    {
        /// <summary>
        /// 未启动.
        /// </summary>
        NOT_START = 0,

        /// <summary>
        /// 已提交.
        /// </summary>
        SUBMITTED = 1,

        /// <summary>
        /// 执行中.
        /// </summary>
        IN_PROGRESS = 3,

        /// <summary>
        /// 失败.
        /// </summary>
        FAILURE = 4,

        /// <summary>
        /// 成功.
        /// </summary>
        SUCCESS = 5,

        /// <summary>
        /// 弹窗
        /// </summary>
        MODAL = 6,

        /// <summary>
        /// 取消
        /// </summary>
        CANCEL = 7
    }

    public static class TaskStatusExtensions
    {
        public static int GetOrder(this TaskStatus status)
        {
            // This method should return an integer that represents the order of the status
            // Replace the following line with the actual implementation
            return status switch
            {
                TaskStatus.NOT_START => 0,
                TaskStatus.SUBMITTED => 1,
                TaskStatus.IN_PROGRESS => 3,
                TaskStatus.FAILURE => 4,
                TaskStatus.SUCCESS => 5,
                _ => 0
            };
        }
    }

    /// <summary>
    /// 任务操作枚举.
    /// </summary>
    public enum TaskAction
    {
        /// <summary>
        /// 生成图片.
        /// </summary>
        IMAGINE,

        /// <summary>
        /// 选中放大.
        /// </summary>
        UPSCALE,

        /// <summary>
        /// 选中其中的一张图，生成四张相似的.
        /// </summary>
        VARIATION,

        /// <summary>
        /// 重新执行.
        /// </summary>
        REROLL,

        /// <summary>
        /// 图转 prompt.
        /// </summary>
        DESCRIBE,

        /// <summary>
        /// 多图混合.
        /// </summary>
        BLEND,

        /// <summary>
        /// 提交动作
        /// </summary>
        ACTION,

        /// <summary>
        /// 平移
        /// </summary>
        PAN,

        /// <summary>
        /// 变焦
        /// </summary>
        OUTPAINT,

        /// <summary>
        /// 局部重绘
        /// </summary>
        INPAINT,

        /// <summary>
        /// 自定义变焦
        /// </summary>
        ZOOM,

        /// <summary>
        /// SHOW 指令
        /// </summary>
        SHOW,

        /// <summary>
        /// prompt 简短指令
        /// </summary>
        SHORTEN,

        /// <summary>
        /// 换脸任务
        /// = FACE_SWAP
        /// </summary>
        SWAP_FACE,

        /// <summary>
        /// 视频换脸任务
        /// </summary>
        SWAP_VIDEO_FACE,

        /// <summary>
        ///  混图放大
        /// </summary>
        MIX_UPSCALE
    }

    /// <summary>
    /// 消息类型枚举.
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// 创建.
        /// </summary>
        CREATE,

        /// <summary>
        /// 修改.
        /// </summary>
        UPDATE,

        /// <summary>
        /// 删除.
        /// </summary>
        DELETE,

        /// <summary>
        ///
        /// </summary>
        INTERACTION_CREATE,

        /// <summary>
        ///
        /// </summary>
        INTERACTION_SUCCESS,

        /// <summary>
        ///
        /// </summary>
        INTERACTION_IFRAME_MODAL_CREATE,

        /// <summary>
        ///
        /// </summary>
        INTERACTION_MODAL_CREATE,
        
        READY,
        RESUMED,
        GUILD_CREATE,
        GUILD_UPDATE,
        GUILD_DELETE,
        CHANNEL_CREATE,
        CHANNEL_UPDATE,
        CHANNEL_DELETE
    }

    /// <summary>
    /// 生成速度模式枚举.
    /// </summary>
    public enum GenerationSpeedMode
    {
        RELAX,
        FAST,
        TURBO
    }

    public static class MessageTypeExtensions
    {
        /// <summary>
        /// 将字符串转换为对应的消息类型枚举.
        /// </summary>
        /// <param name="type">消息类型字符串.</param>
        /// <returns>对应的消息类型枚举.</returns>
        public static MessageType? Of(string type)
        {
            return type switch
            {
                "MESSAGE_CREATE" => MessageType.CREATE,
                "MESSAGE_UPDATE" => MessageType.UPDATE,
                "MESSAGE_DELETE" => MessageType.DELETE,
                "INTERACTION_CREATE" => MessageType.INTERACTION_CREATE,
                "INTERACTION_SUCCESS" => MessageType.INTERACTION_SUCCESS,
                "INTERACTION_IFRAME_MODAL_CREATE" => MessageType.INTERACTION_IFRAME_MODAL_CREATE,
                "INTERACTION_MODAL_CREATE" => MessageType.INTERACTION_MODAL_CREATE,
                "READY" => MessageType.READY,
                "RESUMED" => MessageType.RESUMED,
                "GUILD_CREATE" => MessageType.GUILD_CREATE,
                "GUILD_DELETE" => MessageType.GUILD_DELETE,
                "GUILD_UPDATE" => MessageType.GUILD_UPDATE,
                "CHANNEL_CREATE" => MessageType.CHANNEL_CREATE,
                "CHANNEL_UPDATE" => MessageType.CHANNEL_UPDATE,
                "CHANNEL_DELETE" => MessageType.CHANNEL_DELETE,
                _ => null
            };
        }
    }

    /// <summary>
    /// 图片混合维度枚举.
    /// </summary>
    public enum BlendDimensions
    {
        /// <summary>
        /// 纵向.
        /// </summary>
        PORTRAIT,

        /// <summary>
        /// 正方形.
        /// </summary>
        SQUARE,

        /// <summary>
        /// 横向.
        /// </summary>
        LANDSCAPE
    }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// NONE（未配置，或无法连接时使用，原默认值）
        /// </summary>
        NONE = 0,

        /// <summary>
        /// LiteDB
        /// </summary>
        LiteDB = 1,

        /// <summary>
        /// MongoDB
        /// </summary>
        MongoDB = 2,

        /// <summary>
        /// SQLite
        /// </summary>
        SQLite = 3,

        /// <summary>
        /// MySQL
        /// </summary>
        MySQL = 4,

        /// <summary>
        /// PostgreSQL
        /// </summary>
        PostgreSQL = 5,

        /// <summary>
        /// SQL Server
        /// </summary>
        SQLServer = 6
    }
    
    /// <summary>
    /// Discord 频道类型
    /// </summary>
    public enum ChannelType
    {
        /// <summary>
        ///     The channel is a text channel.
        /// </summary>
        Text = 0,

        /// <summary>
        ///     The channel is a Direct Message channel.
        /// </summary>
        DM = 1,

        /// <summary>
        ///     The channel is a voice channel.
        /// </summary>
        Voice = 2,

        /// <summary>
        ///     The channel is a group channel.
        /// </summary>
        Group = 3,

        /// <summary>
        ///     The channel is a category channel.
        /// </summary>
        Category = 4,

        /// <summary>
        ///     The channel is a news channel.
        /// </summary>
        News = 5,

        /// <summary>
        ///     The channel is a store channel.
        /// </summary>
        Store = 6,

        /// <summary>
        ///     The channel is a temporary thread channel under a news channel.
        /// </summary>
        NewsThread = 10,

        /// <summary>
        ///     The channel is a temporary thread channel under a text channel.
        /// </summary>
        PublicThread = 11,

        /// <summary>
        ///     The channel is a private temporary thread channel under a text channel.
        /// </summary>
        PrivateThread = 12,

        /// <summary>
        ///     The channel is a stage voice channel.
        /// </summary>
        Stage = 13,

        /// <summary>
        ///     The channel is a guild directory used in hub servers. (Unreleased)
        /// </summary>
        GuildDirectory = 14,

        /// <summary>
        ///     The channel is a forum channel containing multiple threads.
        /// </summary>
        Forum = 15,

        /// <summary>
        ///     The channel is a media channel containing multiple threads.
        /// </summary>
        Media = 16,
    }
    
    /// <summary>
    ///     Represents public flags for a channel.
    /// </summary>
    public enum ChannelFlags
    {
        /// <summary> 
        ///     Default value for flags, when none are given to a channel.
        /// </summary>
        None = 0,

        /// <summary>
        ///      Flag given to a thread channel pinned on top of parent forum channel.
        /// </summary>
        Pinned = 1 << 1,

        /// <summary>
        ///     Flag given to a forum or media channel that requires people to select tags when posting.
        /// </summary>
        RequireTag = 1 << 4,

        /// <summary>
        ///     Flag given to a media channel that hides the embedded media download options.
        /// </summary>
        HideMediaDownloadOption = 1 << 15,
    }
    
    /// <summary>
    /// 频道选择策略类型
    /// </summary>
    public enum ChannelSelectionStrategyType
    {
        /// <summary>
        /// 默认策略（最小队列优先）
        /// </summary>
        Default,
            
        /// <summary>
        /// 轮询策略
        /// </summary>
        RoundRobin,
            
        /// <summary>
        /// 最少负载策略
        /// </summary>
        LeastLoad
    }

    public static class BlendDimensionsExtensions
    {
        /// <summary>
        /// 获取图片混合维度的字符串值.
        /// </summary>
        /// <param name="dimension">图片混合维度.</param>
        /// <returns>图片混合维度的字符串值.</returns>
        public static string GetValue(this BlendDimensions dimension)
        {
            return dimension switch
            {
                BlendDimensions.PORTRAIT => "2:3",
                BlendDimensions.SQUARE => "1:1",
                BlendDimensions.LANDSCAPE => "3:2",
                _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, null)
            };
        }
    }
}