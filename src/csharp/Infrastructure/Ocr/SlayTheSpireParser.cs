using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;
using System.Text.RegularExpressions;

namespace GameAssistant.Infrastructure.Ocr;

/// <summary>
/// 杀戮尖塔（Slay the Spire）游戏状态解析器
/// 将 OCR 识别的原始文本转换为结构化游戏状态
/// </summary>
public class SlayTheSpireParser : IGameStateParser
{
    public string GameName => "SlayTheSpire";

    public GameState Parse(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
            return CreateEmpty();

        var state = new SlayTheSpireGameState
        {
            GameName = GameName,
            Timestamp = DateTime.UtcNow,
            RawOcrText = ocrText,
        };

        var lines = ocrText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var fullText = ocrText.Replace("\n", " ");

        // 1. 解析 Floor 和 Act
        (state.Floor, state.Act) = ParseFloorAndAct(fullText);

        // 2. 解析玩家状态（HP, Block, Gold）
        (state.Player.CurrentHp, state.Player.MaxHp) = ParsePlayerHp(fullText);
        state.Player.Block = ParseBlock(fullText);
        state.Player.Gold = ParseGold(fullText);

        // 3. 解析能量
        state.Energy = ParseEnergy(fullText);

        // 4. 解析抽牌堆/弃牌堆
        (state.DrawPileCount, state.DiscardPileCount) = ParsePiles(fullText);

        // 5. 解析手牌
        state.Hand = ParseHand(lines);

        // 6. 解析敌人
        state.Enemies = ParseEnemies(lines);

        // 7. 解析当前意图
        state.CurrentIntent = ParseCurrentIntent(fullText);

        // 8. 判断游戏阶段
        state.Phase = DetectPhase(fullText, state);

        // 9. 解析遗物
        state.Player.Relics = ParseRelics(fullText);

        return state;
    }

    public T Parse<T>(string ocrText) where T : GameState
    {
        var state = Parse(ocrText);
        if (state is T typed)
            return typed;

        throw new InvalidOperationException(
            $"Cannot convert {nameof(SlayTheSpireGameState)} to {typeof(T).Name}");
    }

    // === 私有解析方法 ===

    private static (int floor, int act) ParseFloorAndAct(string text)
    {
        int floor = 0, act = 1;

        // "Floor 12" 或 "12F" 或 "Floor12"
        var floorMatch = Regex.Match(text, @"(?:Floor|F)\s*[:.]?\s*(\d+)", RegexOptions.IgnoreCase);
        if (floorMatch.Success && int.TryParse(floorMatch.Groups[1].Value, out int f))
            floor = f;

        // "Act 2" 或 "Act2"
        var actMatch = Regex.Match(text, @"Act\s*[:.]?\s*(\d+)", RegexOptions.IgnoreCase);
        if (actMatch.Success && int.TryParse(actMatch.Groups[1].Value, out int a))
            act = a;

        // 隐式推断：根据 floor 推断 act（每 act 约 15 层）
        if (floor > 0 && act == 1)
            act = (floor - 1) / 15 + 1;

        return (floor, act);
    }

    private static (int current, int max) ParsePlayerHp(string text)
    {
        // "HP: 65/80" 或 "65/80" 或 "Hit Points 65/80"
        var match = Regex.Match(text, @"HP\s*[:.\s]*(\d+)\s*/\s*(\d+)", RegexOptions.IgnoreCase);
        if (match.Success &&
            int.TryParse(match.Groups[1].Value, out int current) &&
            int.TryParse(match.Groups[2].Value, out int max))
            return (current, max);

        // "65 / 80" 或 "65/ 80"
        var simple = Regex.Match(text, @"(\d+)\s*/\s*(\d+)", RegexOptions.IgnoreCase);
        if (simple.Success &&
            int.TryParse(simple.Groups[1].Value, out int c) &&
            int.TryParse(simple.Groups[2].Value, out int m))
            return (c, m);

        return (0, 0);
    }

    private static int ParseBlock(string text)
    {
        var match = Regex.Match(text, @"Block\s*[:.\s]*(\d+)", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out int b) ? b : 0;
    }

    private static int ParseGold(string text)
    {
        var match = Regex.Match(text, @"(?:Gold|G)\s*[:.\s]*(\d+)", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out int gold) ? gold : 0;
    }

    private static (int current, int max) ParseEnergy(string text)
    {
        // "Energy: 3/3" 或能量球数量（如 "●●○" 已转为文字 "2/3"）
        var match = Regex.Match(text, @"Energy\s*[:.\s]*(\d+)\s*/\s*(\d+)", RegexOptions.IgnoreCase);
        if (match.Success &&
            int.TryParse(match.Groups[1].Value, out int cur) &&
            int.TryParse(match.Groups[2].Value, out int max))
            return (cur, max);

        // 纯数字 "3/3"（周围有能量关键字）
        if (Regex.IsMatch(text, @"Energy|Orb|Power", RegexOptions.IgnoreCase))
        {
            var simple = Regex.Match(text, @"(\d+)\s*/\s*(\d+)", RegexOptions.IgnoreCase);
            if (simple.Success &&
                int.TryParse(simple.Groups[1].Value, out int c) &&
                int.TryParse(simple.Groups[2].Value, out int m))
                return (c, m);
        }

        return (0, 0);
    }

    private static (int draw, int discard) ParsePiles(string text)
    {
        int draw = 0, discard = 0;

        // "Draw: 5" 或 "Draw 5"
        var drawMatch = Regex.Match(text, @"Draw\s*(?:Pile)?\s*[:.\s]*(\d+)", RegexOptions.IgnoreCase);
        if (drawMatch.Success && int.TryParse(drawMatch.Groups[1].Value, out int d))
            draw = d;

        // "Discard: 3" 或 "Discard 3"
        var discMatch = Regex.Match(text, @"Discard\s*(?:Pile)?\s*[:.\s]*(\d+)", RegexOptions.IgnoreCase);
        if (discMatch.Success && int.TryParse(discMatch.Groups[1].Value, out int disc))
            discard = disc;

        return (draw, discard);
    }

    private static List<CardData> ParseHand(string[] lines)
    {
        var cards = new List<CardData>();

        // 常见起始牌（用于识别卡牌行）
        var knownCards = GetKnownCards();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.Length < 3)
                continue;

            // 识别卡牌：格式如 "Strike 1" "Defend 1" "Bash 2+"
            var cardMatch = Regex.Match(trimmed, @"^([A-Za-z][A-Za-z\s\+\-']+?)\s+(\d+)\s*$");
            if (cardMatch.Success)
            {
                var name = cardMatch.Groups[1].Value.Trim();
                var costStr = cardMatch.Groups[2].Value;
                int cost = int.TryParse(costStr.Replace("+", ""), out int c) ? c : 0;

                // 查找已知卡牌确定类型
                var (type, isUpgraded) = knownCards.TryGetValue(name, out var info)
                    ? info
                    : (CardType.Unknown, false);

                cards.Add(new CardData
                {
                    Name = name,
                    Cost = cost,
                    Type = type,
                    IsUpgraded = isUpgraded || trimmed.Contains('+'),
                    Confidence = knownCards.ContainsKey(name) ? 95 : 60
                });
                continue;
            }

            // 备选：识别 "X CardName" 格式（如 "X Strike" 在未使用能量时）
            var altMatch = Regex.Match(trimmed, @"^(X|[0-9])\s+([A-Za-z][A-Za-z\s\-']+)$");
            if (altMatch.Success)
            {
                var costStr = altMatch.Groups[1].Value;
                var name = altMatch.Groups[2].Value.Trim();
                int cost = costStr == "X" ? 0 : (int.TryParse(costStr, out int c) ? c : 0);

                var (type, isUpgraded) = knownCards.TryGetValue(name, out var info2)
                    ? info2
                    : (CardType.Unknown, false);

                cards.Add(new CardData
                {
                    Name = name,
                    Cost = cost,
                    Type = type,
                    IsUpgraded = isUpgraded,
                    Confidence = 50
                });
            }
        }

        return cards;
    }

    private static List<EnemyState> ParseEnemies(string[] lines)
    {
        var enemies = new List<EnemyState>();
        var knownEnemies = GetKnownEnemies();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length < 3) continue;

            // 识别敌人名（查找已知敌人）
            foreach (var (name, type) in knownEnemies)
            {
                if (trimmed.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    var enemy = new EnemyState
                    {
                        Name = name,
                        Type = type,
                    };

                    // 尝试从同行提取 HP "65/80"
                    var hpMatch = Regex.Match(trimmed, @"(\d+)\s*/\s*(\d+)");
                    if (hpMatch.Success &&
                        int.TryParse(hpMatch.Groups[1].Value, out int curHp) &&
                        int.TryParse(hpMatch.Groups[2].Value, out int maxHp))
                    {
                        enemy.CurrentHp = curHp;
                        enemy.MaxHp = maxHp;
                    }

                    // 尝试提取 Intent "Attack 12" "Defend 8"
                    var intentMatch = Regex.Match(trimmed, @"(Attack|Defend|Buff|Debuff|Skill)\s*(\d+)?", RegexOptions.IgnoreCase);
                    if (intentMatch.Success)
                    {
                        enemy.Intent = ParseIntentType(intentMatch.Groups[1].Value);
                        if (intentMatch.Groups[2].Success &&
                            int.TryParse(intentMatch.Groups[2].Value, out int dmg))
                            enemy.IntentDamage = dmg;
                    }

                    enemies.Add(enemy);
                    break;
                }
            }
        }

        return enemies;
    }

    private static IntentType? ParseCurrentIntent(string text)
    {
        var match = Regex.Match(text, @"Intent\s*[:.\s]*(Attack|Defend|Buff|Debuff|Skill|Summon)", RegexOptions.IgnoreCase);
        return match.Success ? ParseIntentType(match.Groups[1].Value) : null;
    }

    private static IntentType ParseIntentType(string intent)
    {
        return intent.ToUpperInvariant() switch
        {
            "ATTACK" => IntentType.Attack,
            "DEFEND" => IntentType.Defend,
            "BUFF" => IntentType.Buff,
            "DEBUFF" => IntentType.Debuff,
            "SKILL" => IntentType.Skill,
            "SUMMON" => IntentType.Summon,
            _ => IntentType.Unknown
        };
    }

    private static GamePhase DetectPhase(string text, SlayTheSpireGameState state)
    {
        // 战斗中有手牌 = Combat
        if (state.Hand.Count > 0)
            return GamePhase.Combat;

        // 关键字检测
        if (Regex.IsMatch(text, @"Card Reward|Choose a card|Select", RegexOptions.IgnoreCase))
            return GamePhase.CardReward;

        if (Regex.IsMatch(text, @"Boss Reward|Boss|Spire", RegexOptions.IgnoreCase))
            return GamePhase.Boss;

        if (Regex.IsMatch(text, @"Map|Node|Floor|Act", RegexOptions.IgnoreCase) &&
            state.Enemies.Count == 0)
            return GamePhase.Map;

        return GamePhase.Unknown;
    }

    private static List<string> ParseRelics(string text)
    {
        var relics = new List<string>();
        var knownRelics = GetKnownRelics();

        foreach (var relic in knownRelics)
        {
            if (text.Contains(relic, StringComparison.OrdinalIgnoreCase))
                relics.Add(relic);
        }

        return relics;
    }

    private static SlayTheSpireGameState CreateEmpty() => new()
    {
        GameName = "SlayTheSpire",
        Timestamp = DateTime.UtcNow
    };

    // === 知识库：杀戮尖塔常见卡牌/敌人/遗物名称 ===

    private static Dictionary<string, (CardType type, bool upgraded)> GetKnownCards()
    {
        return new Dictionary<string, (CardType, bool)>(StringComparer.OrdinalIgnoreCase)
        {
            // ---- 起始牌（Ironclad）----
            ["Strike"] = (CardType.Attack, false),
            ["Defend"] = (CardType.Skill, false),
            ["Bash"] = (CardType.Attack, false),
            // 升级版
            ["Strike+"] = (CardType.Attack, true),
            ["Defend+"] = (CardType.Skill, true),
            ["Bash+"] = (CardType.Attack, true),
            // ---- 常见攻击牌 ----
            ["Pommel Strike"] = (CardType.Attack, false),
            ["Pommel Strike+"] = (CardType.Attack, true),
            ["Twin Strike"] = (CardType.Attack, false),
            ["Cleave"] = (CardType.Attack, false),
            ["Cleave+"] = (CardType.Attack, true),
            ["Inflame"] = (CardType.Power, false),
            ["Inflame+"] = (CardType.Power, true),
            ["Demon Form"] = (CardType.Power, false),
            ["Demon Form+"] = (CardType.Power, true),
            ["Impervious"] = (CardType.Skill, false),
            ["Impervious+"] = (CardType.Skill, true),
            ["Reaper"] = (CardType.Attack, false),
            // ---- 常见技能牌 ----
            ["Shrug It Off"] = (CardType.Skill, false),
            ["Shrug It Off+"] = (CardType.Skill, true),
            ["Armaments"] = (CardType.Skill, false),
            ["Armaments+"] = (CardType.Skill, true),
            ["Entrench"] = (CardType.Skill, false),
            ["Entrench+"] = (CardType.Skill, true),
            ["Fortress"] = (CardType.Skill, false),
            // ---- 诅咒牌 ----
            ["Curse"] = (CardType.Curse, false),
            ["Doubt"] = (CardType.Curse, false),
            ["Regret"] = (CardType.Curse, false),
            ["Pain"] = (CardType.Status, false),
        };
    }

    private static List<(string name, EnemyType type)> GetKnownEnemies()
    {
        return new List<(string, EnemyType)>
        {
            // Act 1
            ("Slime B.", EnemyType.Normal),
            ("Acid Slime", EnemyType.Normal),
            ("Fungi Be.", EnemyType.Normal),
            ("Jaw Worm", EnemyType.Normal),
            ("Cultist", EnemyType.Normal),
            ("Looter", EnemyType.Normal),
            ("Red Sla.", EnemyType.Normal),
            ("Blue Sla.", EnemyType.Normal),
            ("寄生怪", EnemyType.Elite),  // 可能有中文OCR
            // Act 1 Elite
            ("Lagavulin", EnemyType.Elite),
            ("Sentries", EnemyType.Elite),
            ("Guardian", EnemyType.Elite),
            // Act 2
            ("Chosen", EnemyType.Normal),
            ("Mystic", EnemyType.Normal),
            ("Snake Plant", EnemyType.Normal),
            ("Sentry", EnemyType.Normal),
            ("Dagger", EnemyType.Normal),
            // Act 2 Elite
            ("Slime Boss", EnemyType.Elite),
            ("Book of Stab", EnemyType.Elite),
            ("Bronze Automaton", EnemyType.Elite),
            // Act 3 Boss
            ("Awakened One", EnemyType.Boss),
            ("Donu", EnemyType.Boss),
            ("Deca", EnemyType.Boss),
            ("Time Eater", EnemyType.Boss),
            ("Spire Shield", EnemyType.Boss),
            ("Spire Spear", EnemyType.Boss),
        };
    }

    private static List<string> GetKnownRelics()
    {
        return new List<string>
        {
            // 初始遗物
            "Burning Blood",
            // 常见 Strong (金边)
            "Kunai",
            "Shuriken",
            "Orichalcum",
            "Panacea",
            "Calipers",
            "Threading",
            // 常见 Act 1
            "Bottled Flame",
            "Bottled Lightning",
            "Bottled Tornado",
            "Pen Nib",
            "Pocketwatch",
            "Juzu Bracelet",
            // Boss 遗物
            "Fusion Hammer",
            "Snecko Skull",
            "Pandora's Box",
            " radioactive canister",
            "Cursed Key",
            "Coffee Dripper",
        };
    }
}
