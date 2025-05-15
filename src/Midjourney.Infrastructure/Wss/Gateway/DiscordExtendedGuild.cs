using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Midjourney.Infrastructure.Dto;

namespace Midjourney.Infrastructure.Wss.Gateway;

public class DiscordExtendedGuild : DiscordGuild
{
    [JsonPropertyName("unavailable")]
    public bool? Unavailable { get; set; }

    [JsonPropertyName("member_count")]
    public int MemberCount { get; set; }

    [JsonPropertyName("large")]
    public bool Large { get; set; }

    [JsonPropertyName("presences")]
    public JsonElement Presences { get; set; }

    [JsonPropertyName("members")]
    public JsonElement Members { get; set; }

    [JsonPropertyName("channels")]
    public DiscordChannelDto[] Channels { get; set; }

    [JsonPropertyName("joined_at")]
    public DateTimeOffset JoinedAt { get; set; }

    [JsonPropertyName("threads")]
    public new DiscordChannelDto[] Threads { get; set; }

    [JsonPropertyName("guild_scheduled_events")]
    public JsonElement GuildScheduledEvents { get; set; }
}