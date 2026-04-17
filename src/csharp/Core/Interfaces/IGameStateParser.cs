using GameAssistant.Core.Models;

namespace GameAssistant.Core.Interfaces;

/// <summary>
/// 将 OCR 识别出的原始文本解析为结构化游戏状态
/// </summary>
public interface IGameStateParser
{
    /// <summary>
    /// 游戏名称标识，如 "SlayTheSpire", "Diablo2"
    /// </summary>
    string GameName { get; }

    /// <summary>
    /// 将 OCR 文本解析为结构化状态
    /// </summary>
    /// <param name="ocrText">Tesseract 识别的原始文本</param>
    /// <returns>结构化状态对象</returns>
    GameState Parse(string ocrText);

    /// <summary>
    /// 将 OCR 文本解析为特定游戏的完整状态（扩展方法）
    /// </summary>
    T Parse<T>(string ocrText) where T : GameState;
}
