# Game content

The `content/` folder defines everything the game simulation needs besides level layouts. `GameContentLoader.Load("content/game.json")` reads it; any problem is reported with the file or definition it came from.

Monster, class, and item numbers (life, damage, armor class, to-hit, experience, stats, caps, growth) must be at most 1,000,000.

All times are in **ticks**. The simulation runs at 20 ticks per second, so `moveTicks: 7` is 0.35 seconds per tile.

## `game.json`

Points at the other files, relative to itself: `rules` (file), `classes` (folder), `monsters` (folder), `levels` (file), `items` (folder), `spells` (folder), `affixes` (file), and `loot` (file).

## `rules.json`

| Field | Meaning |
| --- | --- |
| `minHitChance`, `maxHitChance` | Hit chance is clamped to this range (percent). |
| `playerBaseToHit`, `toHitPerDexterity`, `toHitPerLevel` | Player accuracy = base + dexterity × per-dex + level × per-level. |
| `toHitPerLevelDifference` | Added per level the attacker is above the defender. |
| `dexterityPerArmor` | Player armor class = dexterity ÷ this. |
| `strengthPerDamage` | Player bonus damage = strength ÷ this. |
| `hitRecoveryPercent` | A hit of at least this percent of max life staggers the target. |
| `experiencePercentPerLevel` | Percent of a monster's base experience added per level it is above the player (removed per level below); 0–100. |
| `minimumExperiencePercent` | Kill experience never drops below this percent of base experience; 0–100. |
| `experienceThresholds` | Total experience to reach level 2, 3, …; strictly increasing. The maximum level is the number of thresholds + 1. |
| `maxSpellLevel` | Cap for a known spell's level (books raise it up to this). |

Hit chance = attacker accuracy − defender armor class + level difference × `toHitPerLevelDifference`.

Kill experience = base experience × max(`minimumExperiencePercent`, 100 + `experiencePercentPerLevel` × (monster level − player level)) ÷ 100, rounded down.

Leveling up restores the player's life and mana.

## `classes/*.json`

`id`, `name`, `spriteSet`, `baseStats` and `statCaps` (`strength`, `dexterity`, `vitality`, `magic`), `growth` (`lifeBase`, `lifePerVitality`, `lifePerLevel`, `manaBase`, `manaPerMagic`, `manaPerLevel`, `statPointsPerLevel`), `unarmedDamage` (`min`, `max`), and `timings`.

Optional `startingItems` lists item ids the class starts with. Each is created plain (no affixes, armor at its maximum). Equippable items fill empty slots; potions go to the belt; the rest go to the inventory. Items that don't fit are left out.

## `monsters/*.json`

`id`, `name`, `spriteSet`, `level`, `life` (`min`, `max`; rolled per monster), `armorClass`, `toHit`, `damage` (`min`, `max`), optional `blockChance` (percent, default 0), `timings`, `sightRadius` (tiles), `experience`, `ai`, and optional `loot` (a loot table id; monsters without one drop nothing). A monster's `level` affects hit chance (both ways) and the experience it awards.

`ai` is one of:

- `melee-chase`: wakes when the player comes within `sightRadius` or when hit, then chases and attacks.
- `pack-leader`: as `melee-chase`, and spawns with `minions` (`monster`, `count` with `min` and `max`, at most 12). The whole pack wakes together.
- `ranged-kite`: keeps about `preferredRange` tiles away and shoots; does not consume ammo.

## `timings`

`moveTicks` per tile, `attackTicks` per swing, `hitFrame` (the tick within the swing when the hit lands; at least 1 and less than `attackTicks`), and `recoveryTicks` (how long a stagger lasts).

## `levels.json`

`levels` is a list with one entry per dungeon depth, numbered 1, 2, 3, … with no gaps. Each entry has `depth`, `area` (a Mortar area file, relative to `levels.json`), `spawns` (`monster` and `weight`; one is picked per spawn marker), and optional `boss` (spawned at the area's boss marker). Spawn markers within 5 tiles of the entrance stay empty.

An optional `town` entry (`{ "area": "…" }`) adds a safe town at depth 0: its area must use mode `fixed` with exactly one exit (`X`), it has no spawns or boss, and new games start there. The shipped content has a town and four cathedral levels (`content/areas/`).

The shipped levels are built from these files: `content/tilesets/cathedral.json` and `content/tilesets/town.json` (the town adds `grass`, `road`, `water`, and `tree` grounds), the cathedral rooms in `content/rooms/cathedral/`, the areas in `content/areas/` (`town` and `cathedral-1` to `cathedral-4`), and the town map in `content/maps/town.json`. The town tileset declares custom markers `F` smith, `H` healer, and `G` sage; they are recorded in the layout for later milestones and do nothing yet.

## `items/*.json`

One item base per file: `id`, `name`, `icon`, `slot`, `size` (`width` 1–2, `height` 1–3), `itemLevel` (only monsters of at least this level drop it), `dropWeight` (0 never drops), and optional `requirements` (`strength`, `dexterity`, `vitality`, `magic`; missing ones are 0, compared against the player's base stats).

`slot` is one of `weapon`, `body`, `head`, `shield`, `ring`, `amulet`, `potion`, `ammo`. Rings, amulets, potions, and ammo are 1×1.

- Weapons need `damage` (`min`, `max`), which replaces the class's unarmed damage. Optional `swing` (`attackTicks`, `hitFrame`) replaces the class's attack timings. Ranged weapons set `ranged: true` and `ammo` (an ammo base id). Staves may set `spell` and `maxCharges`.
- Body, head, and shield items need `armor` (`min`, `max`); each dropped item rolls its armor value once. Shields may have `blockChance` (percent).
- Potions need `potion` (`restores`: `life` or `mana`, and `amount`).
- Ammo needs `stackMax` greater than 1. Optional `stackMax` on other bases defaults to 1. Spellbooks and scrolls use `spell`; books teach or raise that spell, scrolls cast once and are consumed.

The inventory is 10×4 cells and the belt holds 8 potions or ammo stacks. Gold is a counter, not an item. Magic loot may drop unidentified; affix bonuses apply only while identified.

## `spells/*.json`

One spell per file: `id`, `name`, `manaCost`, `castTicks`, `hitFrame` (1…`castTicks`−1), and `shape` (`self`, `projectile`, or `area`).

- `self`: healing uses `healAmount` and optional `healPerLevel`.
- `projectile`: needs `range` and `damage` (`min`, `max`); optional `damagePerLevel`.
- `area`: as projectile plus `radius` (Chebyshev) for the explosion on impact.

Town Portal and Identify are special ids handled by the simulation (open a dungeon↔town pair; mark an item identified).

## `affixes.json`

`affixes` is a list. Each affix has `id`, `name` (shown before the base name for prefixes, after it for suffixes), `position` (`prefix` or `suffix`), `slots` (equippable slots it can appear on), `itemLevel` (`min`, `max`), `weight`, `bonus`, and `value` (`min`, `max`; rolled once per item).

`bonus` is one of `strength`, `dexterity`, `vitality`, `magic`, `life`, `mana`, `armorClass`, `toHit`, `damage` (added to both ends of the damage range).

## `loot.json`

`tables` is a list. Each table has `id`, `rolls` (1–10), `noneWeight`, `goldWeight`, `itemWeight` (each roll picks one of nothing, a gold pile, or an item by these weights), `gold` (`min`, `max`; required when `goldWeight` is above 0), and `magicPercent` (the chance that an item is magic).

A magic item rolls a prefix, a suffix, or both (equal odds), from affixes allowed on its slot at its item level. Potions are never magic. Drops land on the nearest floor cells around the corpse, up to 3 tiles away.
