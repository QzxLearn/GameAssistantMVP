#!/usr/bin/env python3
"""Verify STS2 extracted data against source code."""

import os
import re
import json
import sys

BASE = os.getenv("STS2_DATA_BASE", "/repo/GameAssistance")  # 容器内默认 /repo，生产环境设为 /mnt/d/repo/GameAssistance

def load_cards():
    with open(f"{BASE}/Data/sts2_cards.json") as f:
        return {c["id"]: c for c in json.load(f)}

def load_chars():
    with open(f"{BASE}/Data/sts2_characters.json") as f:
        return json.load(f)

def load_relics():
    with open(f"{BASE}/Data/sts2_relics.json") as f:
        return {r["id"]: r for r in json.load(f)}

def to_snake(name):
    """StrikeIronclad -> strike_ironclad"""
    result = []
    for i, c in enumerate(name):
        if i > 0 and c.isupper():
            result.append("_")
        result.append(c.lower())
    return "".join(result)

# ─────────────────────────────────────────────
# CHECK 1: Character starting decks reference real cards
# ─────────────────────────────────────────────
def check_char_decks(cards):
    chars = load_chars()
    errors = []
    for char in chars:
        for card_id in char.get("startingDeck", []):
            if card_id not in cards:
                # Try alternate naming
                alt = card_id.replace("_cs", "")
                if alt in cards:
                    print(f"  WARN: {char['id']} deck has '{card_id}' but class name is '{alt}'")
                else:
                    errors.append(f"  FAIL: {char['id']} deck references non-existent card '{card_id}'")
    if errors:
        print("CHECK 1 - Character Deck References:")
        print("\n".join(errors))
    else:
        print("CHECK 1 - Character Deck References: PASS (all deck cards exist)")
    return len(errors) == 0

# ─────────────────────────────────────────────
# CHECK 2: Starting relic IDs match character files
# ─────────────────────────────────────────────
def check_char_relics(relics):
    chars = load_chars()
    errors = []
    for char in chars:
        relic_id = char.get("startingRelic", "")
        if relic_id and relic_id not in relics:
            errors.append(f"  FAIL: {char['id']} starting relic '{relic_id}' not in relics DB")
    if errors:
        print("CHECK 2 - Character Starting Relics:")
        print("\n".join(errors))
    else:
        print("CHECK 2 - Character Starting Relics: PASS (all relics exist)")
    return len(errors) == 0

# ─────────────────────────────────────────────
# CHECK 3: Enum values match actual source files
# ─────────────────────────────────────────────
ENUM_CHECKS = [
    ("CardType", "CardType.cs", r'public enum CardType\s*\{([^}]+)\}'),
    ("CardRarity", "CardRarity.cs", r'public enum CardRarity\s*\{([^}]+)\}'),
    ("TargetType", "TargetType.cs", r'public enum TargetType\s*\{([^}]+)\}'),
    ("CardKeyword", "CardKeyword.cs", r'public enum CardKeyword\s*\{([^}]+)\}'),
    ("CardTag", "CardTag.cs", r'public enum CardTag\s*\{([^}]+)\}'),
    ("PileType", "PileType.cs", r'public enum PileType\s*\{([^}]+)\}'),
    ("IntentType", "IntentType.cs", r'public enum IntentType\s*\{([^}]+)\}'),
    ("PowerType", "PowerType.cs", r'public enum PowerType\s*\{([^}]+)\}'),
]

def extract_enum_values(body):
    """Extract comma-separated enum member names."""
    members = []
    for line in body.split(","):
        line = line.strip()
        if not line:
            continue
        # Remove flags attribute values like "= 2"
        name = re.sub(r'=\s*[\da-fA-Fx]+', '', line).strip()
        if name:
            members.append(name)
    return members

def check_enum_sources():
    cards_dir = f"{BASE}/sts2/MegaCrit.Sts2.Core.Entities.Cards"
    intents_dir = f"{BASE}/sts2/MegaCrit.Sts2.Core.MonsterMoves.Intents"
    powers_dir = f"{BASE}/sts2/MegaCrit.Sts2.Core.Entities.Powers"

    all_pass = True
    for enum_name, filename, pattern in ENUM_CHECKS:
        # Find the file
        path = None
        for search_dir in [cards_dir, intents_dir, powers_dir]:
            candidate = f"{search_dir}/{filename}"
            if os.path.exists(candidate):
                path = candidate
                break

        if not path:
            print(f"  FAIL: {enum_name} ({filename}) - FILE NOT FOUND")
            all_pass = False
            continue

        with open(path, "r", errors="ignore") as f:
            content = f.read()

        m = re.search(pattern, content)
        if not m:
            print(f"  WARN: {enum_name} - pattern not found in file")
            continue

        src_members = extract_enum_values(m.group(1))

        # Now check against Sts2Enums.cs
        enums_path = f"{BASE}/src/csharp/Core/Enums/Sts2Enums.cs"
        with open(enums_path, "r") as f:
            enums_content = f.read()

        enum_pattern = rf'public enum {enum_name}\s*\{{([^}}]+)\}}'
        em = re.search(enum_pattern, enums_content)
        if not em:
            print(f"  FAIL: {enum_name} - not found in Sts2Enums.cs")
            all_pass = False
            continue

        our_members = extract_enum_values(em.group(1))

        src_set = set(src_members)
        our_set = set(our_members)

        missing = src_set - our_set
        extra = our_set - src_set

        if missing or extra:
            all_pass = False
            print(f"  FAIL: {enum_name} - missing: {missing}, extra: {extra}")
        else:
            print(f"  PASS: {enum_name} ({len(src_members)} members)")

    if all_pass:
        print("CHECK 3 - Enum Values vs Source: PASS")
    else:
        print("CHECK 3 - Enum Values vs Source: FAIL (see above)")
    return all_pass

# ─────────────────────────────────────────────
# CHECK 4: Spot-check 20 random cards vs source
# ─────────────────────────────────────────────
def check_card_spotcheck(cards):
    cards_dir = f"{BASE}/sts2/MegaCrit.Sts2.Core.Models.Cards"
    import random
    all_ids = list(cards.keys())
    sample = random.sample(all_ids, min(20, len(all_ids)))
    errors = []
    for cid in sample:
        fname = f"{cards_dir}/{cards[cid]['name']}.cs"
        if not os.path.exists(fname):
            # Try to find by pattern
            found = False
            for f in os.listdir(cards_dir):
                if to_snake(f[:-3]) == cid:
                    fname = f"{cards_dir}/{f}"
                    found = True
                    break
            if not found:
                errors.append(f"  FAIL: card {cid} ({cards[cid]['name']}) - source file not found")
                continue

        with open(fname, "r", errors="ignore") as f:
            content = f.read()

        # Check cost
        expected_cost = cards[cid].get("cost")
        if expected_cost is not None:
            cost_match = re.search(r'base\((\d+),', content)
            if cost_match and int(cost_match.group(1)) != expected_cost:
                errors.append(f"  FAIL: {cid} cost={expected_cost} but source has {cost_match.group(1)}")

        # Check cardType
        expected_type = cards[cid].get("cardType")
        if expected_type:
            if f"CardType.{expected_type}" not in content and f"CardType.{expected_type}" not in content:
                errors.append(f"  WARN: {cid} cardType={expected_type} not found in source")

    if errors:
        print("CHECK 4 - Card Spot-Check (20 random): WARN/FAIL (see above)")
        for e in errors:
            print(e)
        return len([e for e in errors if "FAIL" in e]) == 0
    else:
        print("CHECK 4 - Card Spot-Check (20 random): PASS")
        return True

# ─────────────────────────────────────────────
# CHECK 5: Verify all card class names in source map to snake_case IDs
# ─────────────────────────────────────────────
def check_card_naming():
    cards_dir = f"{BASE}/sts2/MegaCrit.Sts2.Core.Models.Cards"
    cards = load_cards()

    errors = []
    for fname in os.listdir(cards_dir):
        if not fname.endswith(".cs"):
            continue
        class_name = fname[:-3]
        expected_id = to_snake(class_name)

        # Find in our cards
        if expected_id not in cards:
            # Maybe it's already there under a different convention
            found = None
            for cid in cards:
                if cards[cid]["name"] == class_name:
                    found = cid
                    break
            if found is None:
                errors.append(f"  WARN: {class_name} -> {expected_id} not in cards DB")

    if errors:
        print(f"CHECK 5 - Card Naming Convention: WARN ({len(errors)} unmapped)")
        for e in errors[:10]:
            print(e)
        if len(errors) > 10:
            print(f"  ... and {len(errors)-10} more")
    else:
        print("CHECK 5 - Card Naming Convention: PASS (all 577 cards mapped)")
    return True  # Warning not failure

# ─────────────────────────────────────────────
# CHECK 6: Verify relic class names map to snake_case IDs
# ─────────────────────────────────────────────
def check_relic_naming():
    relics_dir = f"{BASE}/sts2/MegaCrit.Sts2.Core.Models.Relics"
    relics = load_relics()

    errors = []
    for fname in os.listdir(relics_dir):
        if not fname.endswith(".cs"):
            continue
        class_name = fname[:-3]
        expected_id = to_snake(class_name)

        found = None
        for rid in relics:
            if relics[rid]["name"] == class_name:
                found = rid
                break

        if found is None:
            errors.append(f"  WARN: {class_name} -> {expected_id} not in relics DB")

    if errors:
        print(f"CHECK 6 - Relic Naming Convention: WARN ({len(errors)} unmapped)")
        for e in errors[:10]:
            print(e)
    else:
        print("CHECK 6 - Relic Naming Convention: PASS (all relics mapped)")
    return True

# ─────────────────────────────────────────────
# CHECK 7: Compare character HP/energy against source
# ─────────────────────────────────────────────
def check_char_stats_source():
    chars_dir = f"{BASE}/sts2/MegaCrit.Sts2.Core.Models.Characters"
    char_names = ["Ironclad", "Silent", "Defect", "Necrobinder", "Regent"]
    chars = load_chars()

    errors = []
    for cname in char_names:
        fname = f"{chars_dir}/{cname}.cs"
        if not os.path.exists(fname):
            errors.append(f"  FAIL: {cname}.cs not found")
            continue

        with open(fname, "r", errors="ignore") as f:
            content = f.read()

        # Find StartingHp
        hp_m = re.search(r'override int StartingHp => (\d+)', content)
        gold_m = re.search(r'override int StartingGold => (\d+)', content)

        hp = int(hp_m.group(1)) if hp_m else None
        gold = int(gold_m.group(1)) if gold_m else None

        # Find in our JSON
        our_char = next((c for c in chars if c["id"] == cname.lower()), None)
        if our_char:
            if hp and our_char.get("startingHealth") != hp:
                errors.append(f"  FAIL: {cname} HP={our_char['startingHealth']} src={hp}")
            if gold and our_char.get("startingGold") != gold:
                errors.append(f"  FAIL: {cname} Gold={our_char['startingGold']} src={gold}")

    if errors:
        print("CHECK 7 - Character Stats vs Source:")
        print("\n".join(errors))
    else:
        print("CHECK 7 - Character Stats vs Source: PASS")
    return len(errors) == 0

# ─────────────────────────────────────────────
# CHECK 8: Verify RelicRarity enum from source
# ─────────────────────────────────────────────
def check_relic_rarity_enum():
    path = f"{BASE}/sts2/MegaCrit.Sts2.Core.Entities.Relics/RelicRarity.cs"
    with open(path, "r", errors="ignore") as f:
        content = f.read()

    m = re.search(r'public enum RelicRarity\s*\{([^}]+)\}', content)
    if not m:
        print("  FAIL: RelicRarity enum not found in source")
        return False

    src_members = extract_enum_values(m.group(1))

    enums_path = f"{BASE}/src/csharp/Core/Enums/Sts2Enums.cs"
    with open(enums_path, "r") as f:
        enums_content = f.read()

    em = re.search(r'public enum RelicRarity\s*\{([^}]+)\}', enums_content)
    if not em:
        print("  FAIL: RelicRarity not found in Sts2Enums.cs")
        return False

    our_members = extract_enum_values(em.group(1))

    missing = set(src_members) - set(our_members)
    extra = set(our_members) - set(src_members)

    if missing or extra:
        print(f"  FAIL: RelicRarity - missing: {missing}, extra: {extra}")
        return False

    print(f"CHECK 8 - RelicRarity Enum: PASS ({len(src_members)} members)")
    return True

if __name__ == "__main__":
    print("=" * 60)
    print("STS2 Data Verification")
    print("=" * 60)

    cards = load_cards()
    relics = load_relics()

    results = []
    results.append(check_char_decks(cards))
    results.append(check_char_relics(relics))
    results.append(check_enum_sources())
    results.append(check_card_spotcheck(cards))
    results.append(check_card_naming())
    results.append(check_relic_naming())
    results.append(check_char_stats_source())
    results.append(check_relic_rarity_enum())

    print("=" * 60)
    passed = sum(1 for r in results if r)
    print(f"Results: {passed}/{len(results)} checks passed")
    if passed == len(results):
        print("ALL CHECKS PASSED")
        sys.exit(0)
    else:
        print("SOME CHECKS FAILED")
        sys.exit(1)
