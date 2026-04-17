using System.Text.Json.Serialization;

namespace GameAssistant.Core.Models;

/// <summary>
/// 杀戮尖塔（Slay the Spire）屏幕区域定义
/// 坐标为 1920x1080 全屏/窗口模式下的相对比例（0.0-1.0）
/// 实际使用时按游戏窗口分辨率等比缩放
/// </summary>
public static class SlayTheSpireCaptureRegions
{
    // === 战斗场景区域 ===

    /// <summary>
    /// 手牌区域（底部）
    /// </summary>
    public static CaptureRegion HandArea => new(0.15, 0.78, 0.70, 0.18);

    /// <summary>
    /// 能量指示器（左下角）
    /// </summary>
    public static CaptureRegion EnergyIndicator => new(0.01, 0.85, 0.05, 0.05);

    /// <summary>
    /// 抽牌堆计数器（左下）
    /// </summary>
    public static CaptureRegion DrawPileCounter => new(0.07, 0.85, 0.04, 0.04);

    /// <summary>
    /// 弃牌堆计数器（右下）
    /// </summary>
    public static CaptureRegion DiscardPileCounter => new(0.89, 0.85, 0.04, 0.04);

    /// <summary>
    /// 玩家状态区（左下，包含 HP/Block/能量）
    /// </summary>
    public static CaptureRegion PlayerStatusArea => new(0.01, 0.75, 0.12, 0.15);

    /// <summary>
    /// 敌人区域（屏幕中上方）
    /// </summary>
    public static CaptureRegion EnemyArea => new(0.25, 0.20, 0.50, 0.45);

    /// <summary>
    /// 单个敌人状态框（多敌人时循环裁剪）
    /// </summary>
    public static CaptureRegion EnemyStatusBox => new(0.30, 0.22, 0.18, 0.12);

    /// <summary>
    /// 敌人意图图标区域
    /// </summary>
    public static CaptureRegion IntentArea => new(0.30, 0.35, 0.18, 0.06);

    // === 地图场景区域 ===

    /// <summary>
    /// 地图主区域
    /// </summary>
    public static CaptureRegion MapArea => new(0.10, 0.10, 0.80, 0.80);

    /// <summary>
    /// 地图节点（可用节点高亮）
    /// </summary>
    public static CaptureRegion MapNodes => new(0.15, 0.15, 0.70, 0.70);

    /// <summary>
    /// 当前楼层指示器（右上角）
    /// </summary>
    public static CaptureRegion FloorIndicator => new(0.85, 0.02, 0.10, 0.05);

    /// <summary>
    /// 金币/资源显示（左上角）
    /// </summary>
    public static CaptureRegion ResourceDisplay => new(0.01, 0.02, 0.12, 0.05);

    // === OCR 目标区域（用于合成大图） ===

    /// <summary>
    /// 组合所有关键区域用于批量 OCR
    /// </summary>
    public static IEnumerable<(string name, CaptureRegion region)> AllRegions => new[]
    {
        ("hand_area",       HandArea),
        ("energy",           EnergyIndicator),
        ("draw_pile",        DrawPileCounter),
        ("discard_pile",     DiscardPileCounter),
        ("player_status",    PlayerStatusArea),
        ("enemy_area",       EnemyArea),
        ("floor_indicator",  FloorIndicator),
        ("resources",        ResourceDisplay),
    };
}
