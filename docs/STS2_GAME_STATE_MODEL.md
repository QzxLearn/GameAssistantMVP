# STS2 Game State Model

> Research from: `CombatState.cs`, `Creature.cs`, `Player.cs`, `Monster.cs`, `AbstractIntent.cs`, and all `Core.MonsterMoves.Intents/` subclasses.

---

## CombatState

| Field | Type | Description |
|-------|------|-------------|
| `Allies` | `List<Creature>` | Player-side creatures (includes Player) |
| `Enemies` | `List<Creature>` | Enemy creatures |
| `RoundNumber` | `int` | Current round/turn number |
| `CurrentSide` | `CombatSide` | Whose turn (Player/Enemy) |

---

## Creature (base class for Player and Monster)

| Field | Type | Description |
|-------|------|-------------|
| `CurrentHp` | `int` | Current health |
| `MaxHp` | `int` | Maximum health |
| `Block` | `int` | Current blocking value |
| `Powers` | `List<PowerModel>` | Active buff/debuff powers |
| `Side` | `CombatSide` | Player or Enemy side |

---

## Player

| Field | Type | Description |
|-------|------|-------------|
| `Gold` | `int` | Current gold amount |
| `MaxEnergy` | `int` | Maximum energy |
| `Deck` | `CardPile` | Draw/discard/exhaust piles |
| `Relics` | `List<RelicModel>` | Equipped relics |
| `PotionSlots` | `List<PotionSlot>` | Available potion slots |
| `Creature` | `Creature` | The player's Creature (hp/block/powers) |
| `PlayerCombatState` | `PlayerCombatState` | Combat-specific state |

### PlayerCombatState

| Field | Type | Description |
|-------|------|-------------|
| `Energy` | `int` | Current energy remaining |
| `Hand` | `CardPile` | Cards in hand |
| `OrbQueue` | `OrbQueue` | Defect's orb slots (max 3) |

---

## Monster

| Field | Type | Description |
|-------|------|-------------|
| `Creature` | `Creature` | HP, Block, Powers |
| `MonsterModel` | `MonsterModel` | Static monster prototype |
| `NextMove` | `MonsterMove` | Current intent/next action |
| `SlotName` | `string` | Battlefield position name |
| `MonsterMaxHpBeforeModification` | `int` | Base HP before modifiers |

---

## Intent System

### IntentType Enum

```
Attack, Buff, Debuff, DebuffStrong, Defend, Escape, Heal,
Hidden, Summon, Sleep, Stun, StatusCard, CardDebuff,
DeathBlow, Unknown
```

### Intent Subclasses

| Class | IntentType | Has Damage? | Key Fields |
|-------|------------|-------------|------------|
| `SingleAttackIntent` | Attack | ✅ | `DamageCalc` func, target count |
| `MultiAttackIntent` | Attack | ✅ | `Repeats` count, damage each |
| `DeathBlowIntent` | DeathBlow | ✅ | High damage execute |
| `BuffIntent` | Buff | ❌ | — |
| `DebuffIntent` | Debuff | ❌ | — |
| `DebuffStrongIntent` | DebuffStrong | ❌ | — |
| `DefendIntent` | Defend | ❌ | — |
| `StunIntent` | Stun | ❌ | — |
| `HealIntent` | Heal | ❌ | — |
| `SleepIntent` | Sleep | ❌ | — |
| `EscapeIntent` | Escape | ❌ | — |
| `SummonIntent` | Summon | ❌ | — |
| `StatusIntent` | StatusCard | ❌ | `CardCount` |
| `CardDebuffIntent` | CardDebuff | ❌ | — |
| `HiddenIntent` | Hidden | ❌ | — |
| `UnknownIntent` | Unknown | ❌ | — |

### Attack Intent Damage Extraction

Single/Multi attack intents have a `DamageCalc` function. For vision model purposes, the intent icon + number displayed on screen corresponds to the damage value.

---

## Power / Affliction / Enchantment

### PowerModel

| Field | Type | Description |
|-------|------|-------------|
| `Amount` | `int` | Stack count |
| `Type` | `PowerType` | Buff or Debuff |
| `StackType` | `PowerStackType` | Counter or Single |
| `Owner` | `Creature` | Which creature has it |

### AfflictionModel

| Field | Type | Description |
|-------|------|-------------|
| `Amount` | `int` | Stack count |
| `SourceCard` | `CardModel`? | Card that applied it |

### EnchantmentModel

| Field | Type | Description |
|-------|------|-------------|
| `Amount` | `int` | Stack count |
| `Status` | `string` | e.g., "Poison" |
| `SourceCard` | `CardModel`? | Card that applied it |

---

## Vision-Visible UI Elements

| UI Element | Source Field | Visual Clue |
|------------|--------------|-------------|
| Player HP | `Player.Creature.CurrentHp / MaxHp` | Red heart bar |
| Player Block | `Player.Creature.Block` | White shield icon |
| Player Energy | `Player.PlayerCombatState.Energy` | Energy orb icons |
| Monster HP | `Monster.Creature.CurrentHp / MaxHp` | Red bar above monster |
| Monster Block | `Monster.Creature.Block` | White shield icon |
| Monster Intent | `Monster.NextMove.Intents[0].IntentType` | Icon above monster head |
| Intent Damage | Intent `DamageCalc` result | Number on intent icon |
| Hand Cards | `Player.PlayerCombatState.Hand.Cards` | Cards at bottom of screen |
| Gold | `Player.Gold` | Coin icon in top bar |
| Potions | `Player.PotionSlots` | Flask icons |
| Relics | `Player.Relics` | Icon to the left of HP bar |
| Orbs | `Player.PlayerCombatState.OrbQueue.Orbs` | Icons below energy |
| Draw/Discard | `Player.Deck.DrawPile / DiscardPile` | Stack counts |

---

## OrbQueue (Defect)

| Field | Type | Description |
|-------|------|-------------|
| `Orbs` | `List<OrbModel>` | Currently channeled orbs |
| `Capacity` | `int` | Max slots (base: 3) |

### Orb Types

| Orb | PassiveVal | EvokeVal |
|-----|-----------|----------|
| Lightning | 3 | 8 |
| Frost | 2 (block) | 5 (block) |
| Plasma | 1 (energy) | 2 (energy) |
| Dark | 6 (+6/turn) | cumulative |
| Glass | 4 | 8 |
