# Item Depth (Milestone 3, items pass) — Design

Date: 2026-10-17. Build-order step 5 of `2026-10-14-arpg-framework-design.md`
(item depth and town), **items slice** after town-first. Status: Approved.

## Goal

Finish headless item depth in `Mortar.Game`: unique items, broken-gear rules
(durability 0 contributes nothing), and warrior field repair. Unity presentation
waits until the framework is complete.

## Scope

**In**

- Unique quality: content uniques tied to an item base; fixed name and power
  list; drop via per-base chance after the base is rolled; once-per-world flags.
- Identification for uniques matches magic: dungeon drops unidentified (base
  name, powers inert); shops sell identified; scroll / spell / sage reveal.
- Durability 0: equipped item contributes **no** armor, weapon damage, swing,
  block, or affix/unique powers until repaired (still equippable).
- Warrior field repair: class skill targets **one equipped** durable item;
  restores current durability while permanently reducing instance max
  (formula below); can destroy the item if max reaches 0.
- Instance `MaxDurability` is mutable (already separate from the base def
  ceiling for current; field repair and saves need writable instance max).
- Save version 4: instance max durability, unique id on items, world unique
  dropped flags. Migrate v3 → v4.

**Out**

- Unity shop / unique tooltip chrome beyond what the sim already exposes.
- Rogue disarm / sorcerer recharge changes (recharge already exists).
- Buyback, gambler vendor, quests, traps.
- Dozens of uniques — about **8** authored for this slice.

## Understanding summary

- Town loop already ships; this closes the deferred items half of M3.
- Study DevilutionX for *behavior* only (unique roll order, identify naming,
  field repair loop, once-per-game flags). Invent Mortar names, powers, and
  content numbers — clean-room.
- Demo stays warrior-only; field repair is the warrior class skill.

## Locked decisions

| Topic | Choice |
| --- | --- |
| Broken gear | All contributions off at durability 0 |
| Field repair target | Single equipped item |
| Unique pipeline | Per-base: roll base, then unique chance for that base |
| Unique scarcity | Content chances + once-per-world dropped flags |
| Unique identify | Like magic / DevilutionX: unidentified shows base name |

## DevilutionX-aligned behavior (defaults)

| Topic | Behavior |
| --- | --- |
| Unique chance | After base pick: `uniqueChance` from content — **1** for normal monster loot, **15** for tough/boss-style tables (out of 100). Rules/loot tables own the numbers. |
| Unique pick | Eligible uniques: same `baseItemId`, drop level ≥ `minLevel`, not yet flagged dropped. Pick among remaining by content order / weighted list (deterministic). |
| Once-per-world | On unique drop, set flag for that unique id; later rolls skip it. Saved on the world. |
| Identify | Unidentified unique: `Name` = base name; powers ignored. Identified: unique display name; powers apply (if durability > 0). |
| Smith repair | Unchanged: set current = instance max; **no** max penalty. |
| Field repair | While current < max: each step reduces max by `max(max / (level + 9), 1)`; accumulate repair amount `level + rng(0..level-1)` until current + repair ≥ max (or max hits 0 → item destroyed / unequipped and removed). Then clamp current to max. Uses player level and world RNG. |

## Approach (chosen)

**Content `UniqueDefinition` + loot promotion in `ItemFactory`**, plus
`GearBonus` / durability gates and a `FieldRepair` command for the warrior
skill.

| Approach | Why not |
| --- | --- |
| Global unique table (ignore base) | Diverges from DevilutionX; weaker base identity |
| Boss-only uniques | Too sparse for a framework slice |
| Fixed % field repair | DevilutionX uses level-scaled iterative max decay |
| Affixes-only at 0 dur | Rejected; framework “no stats until repaired” |

## Data model

- `UniqueDefinition`: `id`, `name`, `baseItemId`, `minLevel`, ordered
  `powers` (`affixId` + fixed `value`), optional `value` override for pricing.
- `Item`: `UniqueId?`; `ItemQuality.Unique`; mutable `MaxDurability`;
  `Quality` prefers Unique over Magic/Normal.
- `World`: `HashSet`/ordered flags of dropped unique ids (deterministic
  enumeration for hash/save).
- Loot tables / monster treasure: expose `uniqueChance` (default 1; bosses 15).

## Systems

- **Loot:** `ItemFactory.Roll*` — after choosing base, before magic affixes:
  if chance hits and an eligible unique remains, create unique item and flag
  it; else existing magic/normal path.
- **GearBonus:** skip item entirely when `MaxDurability > 0 && Durability == 0`;
  apply unique powers when identified (same gate as affixes).
- **FieldRepairCommand(itemId):** warrior only; item must be equipped and
  durable; reject otherwise; emit events (`ItemRepaired` / `ItemDestroyed`).
- **Saves v4:** persist instance max, `uniqueId`, dropped-unique list;
  v3 migrate: max = def max; no unique ids; empty flags.

## Content (~8 uniques)

Author about eight uniques across existing bases (weapon, armor, shield, helm,
ring/amulet mix), original names and powers, `minLevel` spread across
cathedral depths. No Blizzard/Diablo names.

## Testing

Headless: unique roll determinism and once-per-world; identify naming/powers;
0-dur contributes nothing (flip today’s “affixes still apply” test); field
repair max decay and destroy path; smith still full restore; save v3→v4 round
trip and world hash.

## Non-goals reminder

No Unity work in this pass. No copy of DevilutionX formulas beyond the
documented behavior contracts above; Mortar content and clean-room code only.
