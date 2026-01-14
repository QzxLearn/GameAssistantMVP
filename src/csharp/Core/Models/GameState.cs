using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GameAssistant.Core.Models
{
    /// <summary>
    /// 所有游戏状态的基类
    /// </summary>
    public abstract class GameState
    {
        [JsonInclude]
        public string GameName { get; init; } = string.Empty;

        [JsonInclude]
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// 可选：原始 OCR 文本（用于调试/回放）
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RawOcrText { get; set; }
    }

    // 示例：具体游戏状态（由各 Adapter 实现）
    public class GenericGameState : GameState
    {
        public string RecognizedText { get; set; } = string.Empty;
    }
}
