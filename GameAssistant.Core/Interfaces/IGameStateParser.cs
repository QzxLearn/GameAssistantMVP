using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using GameAssistant.Core.Models;

namespace GameAssistant.Core.Interfaces
{
    /// <summary>
    /// 将 OCR 识别出的原始文本解析为结构化游戏状态
    /// </summary>
    public interface IGameStateParser
    {
        /// <summary>
        /// 游戏名称标识（如 "Diablo2", "StardewValley"）
        /// </summary>
        string GameName { get; }

        /// <summary>
        /// 解析 OCR 文本为游戏状态
        /// </summary>
        /// <param name="ocrText">Tesseract 识别出的原始文本</param>
        /// <returns>结构化状态对象</returns>
        GameState Parse(string ocrText);
    }
}
