using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameAssistant.Core.Models;

public class GameSessionRecord
{
    [Key]
    public int Id { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    public string GameName { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "TEXT")]
    public string GameStateJson { get; set; } = string.Empty;

    /// <summary>
    /// 截图保存路径
    /// </summary>
    public string? ScreenshotPath { get; set; }

    /// <summary>
    /// OCR 原始识别结果
    /// </summary>
    public string? OcrResult { get; set; }

    /// <summary>
    /// 是否已人工审查/编辑
    /// </summary>
    public bool IsReviewed { get; set; } = false;

    /// <summary>
    /// 审查/编辑时间
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// 卡牌类型（用于训练数据）
    /// </summary>
    public string? CardType { get; set; }

    /// <summary>
    /// 置信度
    /// </summary>
    public int Confidence { get; set; } = 100;
}
