using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GameAssistant.Core.Models;

/// <summary>
/// 游戏配置文件（可通过 JSON 加载）
/// </summary>
public class GameProfile
{
    [JsonInclude]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 多个 OCR 区域（支持血条+蓝条+日志等）
    /// </summary>
    [JsonInclude]
    public List<CaptureRegion> OcrRegions { get; set; } = new();

    /// <summary>
    /// 图像预处理参数
    /// </summary>
    [JsonInclude]
    public ImagePreprocessOptions Preprocess { get; set; } = new();
}