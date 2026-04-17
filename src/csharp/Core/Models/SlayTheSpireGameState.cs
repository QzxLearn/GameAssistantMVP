using System.Text.Json.Serialization;

namespace GameAssistant.Core.Models;

/// <summary>
/// 杀戮尖塔（Slay the Spire）完整游戏状态
/// </summary>
public class SlayTheSpireGameState : GameState
{
    public SlayTheSpireGameState()
    {
        GameName = "SlayTheSpire";
    }

    /// <summary>
    /// 当前楼层
    /// </summary>
    public int Floor { get; set; }

    /// <summary>
    /// 当前 act（1=血裔/下水道, 2=墓穴/遗忘港湾, 3=封印之井...）
    /// </summary>
    public int Act { get; set; }

    /// <summary>
    /// 玩家状态
    /// </summary>
    public PlayerState Player { get; set; } = new();

    /// <summary>
    /// 当前手牌
    /// </summary>
    public List<CardData> Hand { get; set; } = new();

    /// <summary>
    /// 抽牌堆数量
    /// </summary>
    public int DrawPileCount { get; set; }

    /// <summary>
    /// 弃牌堆数量
    /// </summary>
    public int DiscardPileCount { get; set; }

    /// <summary>
    /// 能量（当前/最大）
    /// </summary>
    public (int current, int max) Energy { get; set; }

    /// <summary>
    /// 当前敌人列表
    /// </summary>
    public List<EnemyState> Enemies { get; set; } = new();

    /// <summary>
    /// 当前意图（敌人即将做什么）
    /// </summary>
    public IntentType? CurrentIntent { get; set; }

    /// <summary>
    /// 战斗还是地图（探索中）
    /// </summary>
    public GamePhase Phase { get; set; } = GamePhase.Combat;

    /// <summary>
    /// 战斗结果（若有）
    /// </summary>
    public CombatResult? CombatResult { get; set; }
}

/// <summary>
/// 玩家状态
/// </summary>
public class PlayerState
{
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public int Gold { get; set; }
    public int Block { get; set; }

    /// <summary>
    /// 当前遗物列表（名称）
    /// </summary>
    public List<string> Relics { get; set; } = new();

    /// <summary>
    /// 能力列表（buff/debuff）
    /// </summary>
    public List<PowerData> Powers { get; set; } = new();

    public double HpPercent => MaxHp > 0 ? (double)CurrentHp / MaxHp : 0;
}

/// <summary>
/// 卡牌数据
/// </summary>
public class CardData
{
    /// <summary>
    /// 识别出的卡牌名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 卡牌类型
    /// </summary>
    public CardType Type { get; set; }

    /// <summary>
    /// 费用
    /// </summary>
    public int Cost { get; set; }

    /// <summary>
    /// 是否升级
    /// </summary>
    public bool IsUpgraded { get; set; }

    /// <summary>
    /// 描述文本（原始 OCR）
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 识别置信度 0-100
    /// </summary>
    public int Confidence { get; set; }
}

/// <summary>
/// 敌人状态
/// </summary>
public class EnemyState
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 敌人类型（普通/精英/Boss）
    /// </summary>
    public EnemyType Type { get; set; }

    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public int Block { get; set; }

    /// <summary>
    /// 意图（即将攻击/防御/准备...）
    /// </summary>
    public IntentType? Intent { get; set; }

    /// <summary>
    /// 若意图是攻击，显示伤害值
    /// </summary>
    public int? IntentDamage { get; set; }

    /// <summary>
    /// 能力（buff/debuff）
    /// </summary>
    public List<PowerData> Powers { get; set; } = new();

    public double HpPercent => MaxHp > 0 ? (double)CurrentHp / MaxHp : 0;
}

/// <summary>
/// 能力数据（Buff/Debuff）
/// </summary>
public class PowerData
{
    public string Name { get; set; } = string.Empty;
    public int Amount { get; set; }
    public PowerType Type { get; set; }
}

/// <summary>
/// 游戏阶段
/// </summary>
public enum GamePhase
{
    Map,       // 地图探索
    Combat,   // 战斗
    CardReward, // 卡牌奖励
    Boss,      // Boss 选择
    Unknown
}

/// <summary>
/// 战斗结果
/// </summary>
public enum CombatResult
{
    Victory,
    Defeat,
    Escaped
}

/// <summary>
/// 敌人意图类型
/// </summary>
public enum IntentType
{
    Attack,
    Defend,
    Buff,
    Debuff,
    Skill,
    Summon,
    Unknown
}

/// <summary>
/// 卡牌类型
/// </summary>
public enum CardType
{
    Attack,
    Skill,
    Power,
    Status,
    Curse,
    Unknown
}

/// <summary>
/// 敌人类型
/// </summary>
public enum EnemyType
{
    Normal,
    Elite,
    Boss,
    Unknown
}

/// <summary>
/// 能力类型
/// </summary>
public enum PowerType
{
    Buff,
    Debuff,
    Unknown
}
