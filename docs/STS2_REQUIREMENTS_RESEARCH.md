# STS2 Requirements Research Plan (Parallel & Tiny)

> Date: 2026-04-17
> Goal: Extract structural requirements from STS2 source for GameAssistance感知层 + Brain层

---

## Research Tasks (All Parallel)

### R1: Enum Extraction
**Owner**: Agent
**Files**: `MegaCrit.Sts2.Core.Entities.Cards/`, `MegaCrit.Sts2.Core.Entities.Powers/`, `Core.MonsterMoves.Intents/`

| Enum | Values | File |
|------|--------|------|
| `CardType` | Attack, Skill, Power, Status, Curse, Quest | CardType.cs |
| `CardRarity` | Basic, Common, Uncommon, Rare, Ancient, Event, Token, Status, Curse, Quest | CardRarity.cs |
| `IntentType` | Attack, Buff, Debuff, DebuffStrong, Defend, Escape, Heal, Hidden, Summon, Sleep, Stun, StatusCard, CardDebuff, DeathBlow, Unknown | IntentType.cs |
| `PowerType` | Buff, Debuff | PowerType.cs |
| `PileType` | DrawPile, DiscardPile, Hand, Deck... | PileType.cs |
| `CardKeyword` | 40+ keywords | CardKeyword.cs |

**Output**: `Core/Enums/Sts2Enums.cs`

---

### R2: Card Model Database (577 cards)
**Owner**: Agent (batch parallel)
**Files**: `MegaCrit.Sts2.Core.Models.Cards/`

Extract per card:
- `Id` (class name, e.g., `StrikeIronclad` → `strike_ironclad`)
- `Title` (localization key)
- `BaseCost`
- `CardType`, `CardRarity`
- `TargetType`
- `CanonicalTags` / `CanonicalKeywords`
- `BaseDamage` / `BaseBlock` if present

**Batch splits** (4 parallel agents):
- A-E: 150 files
- F-K: 150 files
- L-R: 140 files
- S-Z: 137 files

**Output**: `Data/sts2_cards.json`

---

### R3: Character Models
**Owner**: Agent
**Files**: `MegaCrit.Sts2.Core.Models.Characters/`

Extract: Ironclad, Silent, Defect, Necrobinder, Regent
- Initial HP, Max HP
- Starting energy
- Starting relic
- Starting deck (list of card IDs)
- Starting potions

**Output**: `Data/sts2_characters.json`

---

### R4: Relic Models
**Owner**: Agent
**Files**: `MegaCrit.Sts2.Core.Models.Relics/` (~120 files)

Extract: Id, Title, Rarity, Description

**Output**: `Data/sts2_relics.json`

---

### R5: Orb/Power/Affliction/Enchantment Models
**Owner**: Agent

- `Core.Models.Orbs/`: LightningOrb, FrostOrb, PlasmaOrb, DarkOrb, GlassOrb
  - PassiveVal, EvokeVal per orb
- `Core.Models.Powers/`: ~50 power models
- `Core.Models.Afflictions/`: Poison, Smog, etc.
- `Core.Models.Enchantments/`: Enchantment models

**Output**: `Data/sts2_orbs.json`, `Data/sts2_powers.json`

---

### R6: Combat/GameState Structure
**Owner**: Agent
**Files**: `MegaCrit.Sts2.Core.Combat/`, `MegaCrit.Sts2.Core.Entities.Creatures/`

Understand:
- `CombatState`: manages battle flow
- `Creature`: base for Player/Enemy (HP, Block, Powers)
- `Player`: energy, orb slots, relic, potions, card piles
- `Monster`: intent handling per enemy

**Output**: `docs/STS2_GAME_STATE_MODEL.md`

---

### R7: Intent System Deep Dive
**Owner**: Agent
**Files**: `Core.MonsterMoves.Intents/`

Understand:
- `AbstractIntent` base class
- Intent subclasses: AttackIntent, BuffIntent, DefendIntent, etc.
- How intent data is structured (damage values, target counts)

**Output**: `docs/STS2_INTENT_SYSTEM.md`

---

## Key Findings from Initial Scan

### CardType (updated from STS2 source)
```
Attack, Skill, Power, Status, Curse, Quest
```
(Note: Quest is new vs simplified version)

### CardRarity (updated)
```
Basic, Common, Uncommon, Rare, Ancient, Event, Token, Status, Curse, Quest
```

### IntentType (complete)
```
Attack, Buff, Debuff, DebuffStrong, Defend, Escape, Heal, Hidden, Summon, Sleep, Stun, StatusCard, CardDebuff, DeathBlow, Unknown
```

### PowerType
```
Buff, Debuff
```

### Orb System (Defect)
- Lightning: PassiveVal=3, EvokeVal=8
- Frost: passive block, evoke damage
- Plasma: passive energy
- Dark: evoke damage to all
- Glass: scales with passive hits

### Enchantment/Affliction
- Separate from Power
- Afflictions: Poison-like DOT effects
- Enchantments: card modification effects

---

## Dependency Graph

```
R1 (Enums) ───────────────────────┐
                                   ├──→ R6 (GameState)
R2 (Cards 577) ────────────────────┤
                                   │
R3 (Characters) ───────────────────┤
                                   ├──→ R7 (Intents)
R4 (Relics) ───────────────────────┤
                                   │
R5 (Orb/Power/Afflict/Enchant) ────┘
```

All R1-R5 are **parallel** (no inter-dependencies).
R6 and R7 depend on R1-R5 outputs but can start once enums are clear.

---

## Output Files Summary

| Task | Output | Priority |
|------|--------|----------|
| R1 | `Core/Enums/Sts2Enums.cs` | P0 |
| R2 | `Data/sts2_cards.json` | P0 |
| R3 | `Data/sts2_characters.json` | P0 |
| R4 | `Data/sts2_relics.json` | P1 |
| R5 | `Data/sts2_orbs.json`, `Data/sts2_powers.json` | P1 |
| R6 | `docs/STS2_GAME_STATE_MODEL.md` | P0 |
| R7 | `docs/STS2_INTENT_SYSTEM.md` | P0 |

---

## Next: Sync with STS2_TASK_PLAN.md

After R1-R7 complete:
1. Update `SlayTheSpireGameState.cs` (module B1)
2. Generate `st32_card_name_map.json` (module B2)
3. Design `STS2_DB_SCHEMA.md` (module D1)
