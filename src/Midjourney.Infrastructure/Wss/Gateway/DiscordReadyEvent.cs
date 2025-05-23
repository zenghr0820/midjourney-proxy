﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Midjourney.Infrastructure.Dto;

namespace Midjourney.Infrastructure.Wss.Gateway;

public class DiscordReadyEvent
{
    // public class ReadState
    // {
    //     [JsonPropertyName("id")]
    //     public string ChannelId { get; set; }
    //     [JsonPropertyName("mention_count")]
    //     public int MentionCount { get; set; }
    //     [JsonPropertyName("last_message_id")]
    //     public string LastMessageId { get; set; }
    // }

    [JsonPropertyName("v")]
    public int Version { get; set; }
    [JsonPropertyName("user")]
    public User User { get; set; }
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; }
    [JsonPropertyName("resume_gateway_url")]
    public string ResumeGatewayUrl { get; set; }
    // [JsonPropertyName("read_state")]
    // public ReadState[] ReadStates { get; set; }
    [JsonPropertyName("guilds")]
    public DiscordExtendedGuild[] Guilds { get; set; }
    [JsonPropertyName("private_channels")]
    public DiscordChannelDto[] PrivateChannels { get; set; }
    [JsonPropertyName("relationships")]
    public JsonDocument Relationships { get; set; }
    [JsonPropertyName("application")]
    public PartialApplication Application { get; set; }

    //Ignored
    /*[JsonPropertyName("user_settings")]
    [JsonPropertyName("user_guild_settings")]
    [JsonPropertyName("tutorial")]*/
}