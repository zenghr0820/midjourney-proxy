using System.Text.Json;
using System.Text.Json.Serialization;

namespace Midjourney.Base.Dto;

public class DiscordGuild
{
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("icon")]
        public string Icon { get; set; }
        [JsonPropertyName("splash")]
        public string Splash { get; set; }
        [JsonPropertyName("discovery_splash")]
        public string DiscoverySplash { get; set; }
        [JsonPropertyName("owner_id")]
        public string OwnerId { get; set; }
        [JsonPropertyName("region")]
        public string Region { get; set; }
        [JsonPropertyName("afk_channel_id")]
        public string AFKChannelId { get; set; }
        [JsonPropertyName("afk_timeout")]
        public int AFKTimeout { get; set; }
        [JsonPropertyName("verification_level")]
        public JsonDocument VerificationLevel { get; set; }
        [JsonPropertyName("default_message_notifications")]
        public JsonDocument DefaultMessageNotifications { get; set; }
        [JsonPropertyName("explicit_content_filter")]
        public JsonDocument ExplicitContentFilter { get; set; }
        [JsonPropertyName("voice_states")]
        public JsonDocument[] VoiceStates { get; set; }
        [JsonPropertyName("roles")]
        public JsonDocument Roles { get; set; }
        [JsonPropertyName("emojis")]
        public Emoji[] Emojis { get; set; }
        [JsonPropertyName("features")]
        public JsonDocument Features { get; set; }
        [JsonPropertyName("mfa_level")]
        public JsonDocument MfaLevel { get; set; }
        [JsonPropertyName("application_id")]
        public string ApplicationId { get; set; }
        [JsonPropertyName("widget_enabled")]
        public bool WidgetEnabled { get; set; }
        [JsonPropertyName("widget_channel_id")]
        public string WidgetChannelId { get; set; }
        [JsonPropertyName("safety_alerts_channel_id")]
        public string SafetyAlertsChannelId { get; set; }
        [JsonPropertyName("system_channel_id")]
        public string SystemChannelId { get; set; }
        [JsonPropertyName("premium_tier")]
        public JsonDocument PremiumTier { get; set; }
        [JsonPropertyName("vanity_url_code")]
        public string VanityURLCode { get; set; }
        [JsonPropertyName("banner")]
        public string Banner { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        // this value is inverted, flags set will turn OFF features
        [JsonPropertyName("system_channel_flags")]
        public JsonDocument SystemChannelFlags { get; set; }
        [JsonPropertyName("rules_channel_id")]
        public string RulesChannelId { get; set; }
        [JsonPropertyName("max_presences")]
        public int? MaxPresences { get; set; }
        [JsonPropertyName("max_members")]
        public int MaxMembers { get; set; }
        [JsonPropertyName("premium_subscription_count")]
        public int? PremiumSubscriptionCount { get; set; }
        [JsonPropertyName("preferred_locale")]
        public string PreferredLocale { get; set; }
        [JsonPropertyName("public_updates_channel_id")]
        public string PublicUpdatesChannelId { get; set; }
        [JsonPropertyName("max_video_channel_users")]
        public int MaxVideoChannelUsers { get; set; }
        [JsonPropertyName("approximate_member_count")]
        public int ApproximateMemberCount { get; set; }
        [JsonPropertyName("approximate_presence_count")]
        public int ApproximatePresenceCount { get; set; }
        [JsonPropertyName("threads")]
        public JsonDocument Threads { get; set; }
        [JsonPropertyName("nsfw_level")]
        public JsonDocument NsfwLevel { get; set; }
        [JsonPropertyName("stickers")]
        public JsonDocument Stickers { get; set; }
        [JsonPropertyName("premium_progress_bar_enabled")]
        public bool IsBoostProgressBarEnabled { get; set; }

        [JsonPropertyName("welcome_screen")]
        public JsonDocument WelcomeScreen { get; set; }

        [JsonPropertyName("max_stage_video_channel_users")]
        public int MaxStageVideoChannelUsers { get; set; }

        [JsonPropertyName("inventory_settings")]
        public JsonDocument InventorySettings { get; set; }

        [JsonPropertyName("incidents_data")]
        public JsonDocument IncidentsData { get; set; }
}