using System.Text.Json;
using System.Text.Json.Serialization;

namespace Midjourney.Base.Dto;

public class DiscordSocketMessage
{
    
    #region DiscordSocketMessage
    
    /// <summary>
    /// 事件类型。
    /// </summary>
    [JsonPropertyName("t")]
    public string Type { get; set; }

    /// <summary>
    /// 序列号。
    /// </summary>
    [JsonPropertyName("s")]
    public int Sequence { get; set; }

    /// <summary>
    /// 操作码。
    /// </summary>
    [JsonPropertyName("op")]
    public int OperationCode { get; set; }

    /// <summary>
    /// 事件数据。
    /// </summary>
    [JsonPropertyName("d")]
    public JsonElement Data { get; set; }
    
    #endregion
    
}