using System.Text.Json;
using System.Text.Json.Serialization;

namespace Midjourney.Infrastructure.Dto;


public class DiscordChannelDto
{
    //Shared
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("type")]
    public ChannelType Type { get; set; }
    [JsonPropertyName("last_message_id")]
    public string LastMessageId { get; set; }

    //GuildChannel
    [JsonPropertyName("guild_id")]
    public string GuildId { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("position")]
    public int Position { get; set; }
    [JsonPropertyName("permission_overwrites")]
    public JsonElement PermissionOverwrites { get; set; }
    [JsonPropertyName("parent_id")]
    public string CategoryId { get; set; }

    //TextChannel
    [JsonPropertyName("topic")]
    public string Topic { get; set; }
    [JsonPropertyName("last_pin_timestamp")]
    public DateTimeOffset LastPinTimestamp { get; set; }
    [JsonPropertyName("nsfw")]
    public bool Nsfw { get; set; }
    [JsonPropertyName("rate_limit_per_user")]
    public int SlowMode { get; set; }

    //VoiceChannel
    [JsonPropertyName("bitrate")]
    public int Bitrate { get; set; }
    [JsonPropertyName("user_limit")]
    public int UserLimit { get; set; }
    [JsonPropertyName("rtc_region")]
    public string RTCRegion { get; set; }

    [JsonPropertyName("video_quality_mode")]
    public JsonElement VideoQualityMode { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    //PrivateChannel
    [JsonPropertyName("recipients")]
    public JsonElement Recipients { get; set; }

    //GroupChannel
    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    //ThreadChannel
    [JsonPropertyName("member")]
    public JsonElement ThreadMember { get; set; }

    [JsonPropertyName("thread_metadata")]
    public JsonElement ThreadMetadata { get; set; }

    [JsonPropertyName("owner_id")]
    public string OwnerId { get; set; }

    [JsonPropertyName("message_count")]
    public int MessageCount { get; set; }

    [JsonPropertyName("member_count")]
    public int MemberCount { get; set; }

    //ForumChannel
    [JsonPropertyName("available_tags")]
    public JsonElement ForumTags { get; set; }

    [JsonPropertyName("applied_tags")]
    public ulong[] AppliedTags { get; set; }

    [JsonPropertyName("default_auto_archive_duration")]
    public JsonElement AutoArchiveDuration { get; set; }

    [JsonPropertyName("default_thread_rate_limit_per_user")]
    public int ThreadRateLimitPerUser { get; set; }

    [JsonPropertyName("flags")]
    public ChannelFlags Flags { get; set; }

    [JsonPropertyName("default_sort_order")]
    public JsonElement DefaultSortOrder { get; set; }

    [JsonPropertyName("default_reaction_emoji")]
    public JsonElement DefaultReactionEmoji { get; set; }

    [JsonPropertyName("default_forum_layout")]
    public JsonElement DefaultForumLayout { get; set; }
}