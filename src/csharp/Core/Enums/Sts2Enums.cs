namespace MegaCrit.Sts2.Core.Enums;

public enum AutoPlayType
{
	None,
	Default,
	SlyDiscard
}

public enum CardCostColor
{
	Unmodified,
	Increased,
	Decreased,
	InsufficientResources
}

public enum CardKeyword
{
	None,
	Exhaust,
	Ethereal,
	Innate,
	Unplayable,
	Retain,
	Sly,
	Eternal
}

public enum CardMultiplayerConstraint
{
	None,
	MultiplayerOnly,
	SingleplayerOnly
}

public enum CardPreviewMode
{
	None,
	Normal,
	Upgrade,
	MultiCreatureTargeting
}

public enum CardRarity
{
	None,
	Basic,
	Common,
	Uncommon,
	Rare,
	Ancient,
	Event,
	Token,
	Status,
	Curse,
	Quest
}

public enum CardScope
{
	None,
	Run,
	Combat
}

public enum CardTag
{
	None,
	Strike,
	Defend,
	Minion,
	OstyAttack,
	Shiv
}

public enum CardType
{
	None,
	Attack,
	Skill,
	Power,
	Status,
	Curse,
	Quest
}

public enum CardUpgradePreviewType
{
	None,
	Deck,
	Combat
}

public enum IntentType
{
	Attack,
	Buff,
	Debuff,
	DebuffStrong,
	Defend,
	Escape,
	Heal,
	Hidden,
	Summon,
	Sleep,
	Stun,
	StatusCard,
	CardDebuff,
	DeathBlow,
	Unknown
}

public enum LocalCostType
{
	None,
	Absolute,
	Relative
}

public enum OrbEvokeType
{
	None,
	Front,
	All
}

public enum PileType
{
	None,
	Draw,
	Hand,
	Discard,
	Exhaust,
	Play,
	Deck
}

public enum PowerStackType
{
	None,
	Counter,
	Single
}

public enum PowerType
{
	None,
	Buff,
	Debuff
}

public enum TargetType
{
	None,
	Self,
	AnyEnemy,
	AllEnemies,
	RandomEnemy,
	AnyPlayer,
	AnyAlly,
	AllAllies,
	TargetedNoCreature,
	Osty
}

public enum CharacterGender
{
	Neutral,
	Feminine,
	Masculine
}

public enum RelicRarity
{
	None,
	Starter,
	Common,
	Uncommon,
	Rare,
	Shop,
	Event,
	Ancient
}

[Flags]
public enum UnplayableReason
{
	None = 0,
	HasUnplayableKeyword = 2,
	BlockedByHook = 4,
	BlockedByCardLogic = 8,
	EnergyCostTooHigh = 0x10,
	StarCostTooHigh = 0x20,
	NoLivingAllies = 0x40
}
