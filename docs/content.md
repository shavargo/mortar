# Game content

The `content/` folder defines everything the game simulation needs besides level layouts. `GameContentLoader.Load("content/game.json")` reads it; any problem is reported with the file or definition it came from.

All times are in **ticks**. The simulation runs at 20 ticks per second, so `moveTicks: 7` is 0.35 seconds per tile.

## `game.json`

Points at the other files, relative to itself: `rules` (file), `classes` (folder), `monsters` (folder), `levels` (file).

## `rules.json`

| Field | Meaning |
| --- | --- |
| `minHitChance`, `maxHitChance` | Hit chance is clamped to this range (percent). |
| `playerBaseToHit`, `toHitPerDexterity`, `toHitPerLevel` | Player accuracy = base + dexterity × per-dex + level × per-level. |
| `toHitPerLevelDifference` | Added per level the attacker is above the defender. |
| `dexterityPerArmor` | Player armor class = dexterity ÷ this. |
| `strengthPerDamage` | Player bonus damage = strength ÷ this. |
| `hitRecoveryPercent` | A hit of at least this percent of max life staggers the target. |
| `experienceThresholds` | Total experience to reach level 2, 3, …; strictly increasing. |

Hit chance = attacker accuracy − defender armor class + level difference × `toHitPerLevelDifference`.

## `classes/*.json`

`id`, `name`, `spriteSet`, `baseStats` and `statCaps` (`strength`, `dexterity`, `vitality`, `magic`), `growth` (`lifeBase`, `lifePerVitality`, `lifePerLevel`, `manaBase`, `manaPerMagic`, `manaPerLevel`, `statPointsPerLevel`), `unarmedDamage` (`min`, `max`), and `timings`.

## `monsters/*.json`

`id`, `name`, `spriteSet`, `level`, `life` (`min`, `max`; rolled per monster), `armorClass`, `toHit`, `damage` (`min`, `max`), optional `blockChance` (percent, default 0), `timings`, `sightRadius` (tiles), `experience`, and `ai`:

- `melee-chase`: wakes when the player comes within `sightRadius` or when hit, then chases and attacks.
- `pack-leader`: as `melee-chase`, and spawns with `minions` (`monster`, `count` with `min` and `max`, at most 12). The whole pack wakes together.

## `timings`

`moveTicks` per tile, `attackTicks` per swing, `hitFrame` (the tick within the swing when the hit lands; at least 1 and less than `attackTicks`), and `recoveryTicks` (how long a stagger lasts).

## `levels.json`

`levels` is a list with one entry per dungeon depth, numbered 1, 2, 3, … with no gaps. Each entry has `depth`, `area` (a Mortar area file, relative to `levels.json`), `spawns` (`monster` and `weight`; one is picked per spawn marker), and optional `boss` (spawned at the area's boss marker). Spawn markers within 5 tiles of the entrance stay empty.
