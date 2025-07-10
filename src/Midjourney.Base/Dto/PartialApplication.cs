using System.Text.Json.Serialization;

namespace Midjourney.Base.Dto;

public class PartialApplication
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("flags")]
    public ulong Flags { get; set; }
}