using System.Text.Json.Serialization;

namespace GameAssistant.Core.Models;

/// <summary>
/// 所有游戏状态的抽象基类
/// </summary>
public abstract class GameState
{
    [JsonInclude]
    public string GameName { get; init; } = string.Empty;

    [JsonInclude]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
   /// 原始 OCR 文本，用于调试/回放
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RawOcrText { get; set; }
}

/// <summary>
/// 通用游戏状态（无特定游戏结构时使用）
/// </summary>
public class GenericGameState : GameState
{
    public string RecognizedText { get; set; } = string.Empty;
}
