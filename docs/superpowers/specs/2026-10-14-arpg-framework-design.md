# Mortar ARPG Framework — Design

Date: 2026-10-14
Status: Approved design, pending implementation plans

## Goal

Grow Mortar from a level generator into a full action-RPG framework for Unity
(Unreal later), modeled on the design and mechanics of the original Diablo.
The first target is a playable slice: a town plus four cathedral-style dungeon
levels, three classes, monsters, items, spells, vendors, and save/load.

## Ground rules

- **Clean-room.** Mechanics are recreated from public knowledge of how the game
  plays. No code, data tables, formulas, or assets are copied or translated from
  devilutionX (Sustainable Use License) or the original game. All numbers are
  our own and live in data files.
- **License.** Everything in this repo stays MIT.
- **Naming.** No Blizzard or Diablo names, trademarks, or lore in code, data,
  art prompts, or shipped content.
- **Unity first.** Unreal stays limited to consuming Mortar layout files until
  the Unity slice is playable; deeper Unreal support is a later decision.
- **Engine-independent core.** All gameplay rules are plain C# with no engine
  references, so they are testable headlessly and portable later.

## Projects

| Project | Engine-free | Responsibility |
|---|---|---|
| `Mortar.Core` + `Mortar.Json` (exists) | yes | Level generation; extended with cathedral-style levels 1–4 and a town |
| `Mortar.Game` (new) | yes | Tick-based simulation: actors, stats, combat, projectiles, spells, AI, pathfinding, items, inventory, vendors, save/load |
| `Mortar.Content` (new) | yes | JSON data for classes, monsters, items, affixes, uniques, spells, vendors, spawn rules |
| `Mortar.Unity` (new) | no | Unity package: rendering, camera, input-to-command mapping, HUD and screens, audio hooks |
| `tools/art` (new) | n/a | Art pipeline scripts |
| Sample game (new) | no | Unity project that assembles the slice |

Everything lives in this repo and one solution. The Unity package lives under
`unity/`, art tools under `tools/art/`.

`docs/design.md` remains the level-generation design. It will later be updated
to describe itself as the level-generation part of this framework.

## Build order

1. `Mortar.Game` milestone 1 (core simulation, warrior, melee, items).
2. Cathedral-style dungeon and town generation in `Mortar.Core`.
3. `Mortar.Unity` with placeholder sprites — first playable warrior.
4. `Mortar.Game` milestone 2 (rogue, sorcerer, projectiles, spells).
5. `Mortar.Game` milestone 3 (uniques, durability, identification, town).
6. Art pipeline and real art.
7. Sample game polish.

Each step gets its own implementation plan. Steps 4–6 may run in parallel with
Unity work once step 3 is done.

## Simulation (`Mortar.Game`)

### Loop

- Fixed rate of 20 ticks per second.
- `World.Tick(IReadOnlyList<Command>)` advances one tick and returns the
  events produced during it.
- Commands are player intents: `MoveTo(cell)`, `Attack(actorId)`,
  `Cast(spellId, target)`, `PickUp(itemId)`, `UseBelt(slot)`,
  `Equip(itemId, slot)`, `Buy`/`Sell`/`Repair`/`Identify`, and similar.
- Events describe outcomes: `ActorMoved`, `AttackSwung`, `DamageDealt`,
  `ActorDied`, `ItemDropped`, `ProjectileSpawned`, `LevelChanged`,
  `CommandRejected`, and similar.
- The engine layer only sends commands, reads state, and reacts to events. It
  never mutates simulation state.

### Determinism

- One seeded `Pcg32` per world (reused from `Mortar.Core`) is the only source of
  randomness.
- Same seed plus same command sequence always yields the same state. This
  enables headless tests, replays, and future lockstep multiplayer.
- No wall-clock time, unordered collection iteration, or floating-point
  accumulation affects simulation results.

### State

- `World` holds the current level (a Mortar `Layout` plus runtime overlays:
  actors, ground items, projectiles, door states), the visited-level cache,
  the player, vendors, the tick count, and the random generator state.
- Actors occupy one tile; movement is tile to tile. Display interpolation is
  the engine layer's job.
- Pathfinding is A* over the level's walkable grid.

### Systems

Small stateless classes run in a fixed order each tick:

1. Commands
2. AI
3. Movement
4. Projectiles
5. Combat and spell resolution
6. Death and loot
7. Pickups

Each system is unit-testable alone and stays under the 300-line file limit.

### Errors

- Invalid commands (out of range, dead target, not enough mana, can't afford)
  produce a `CommandRejected` event with a reason. They never throw.
- Invalid content data fails at load time with clear, path-qualified messages.

## Gameplay

### Stats and leveling

- Core stats: Strength, Dexterity, Vitality, Magic.
- Derived: life, mana, armor class, to-hit, damage bonus, block chance, spell
  damage bonus.
- Experience from kills; level-ups grant stat points, life, and mana.
- Effective stats = base + equipment + affixes, recomputed when equipment
  changes or durability reaches 0.

### Classes

| Class | Combat style | Class skill |
|---|---|---|
| Warrior | Melee | Field repair (restores durability, permanently lowers max durability) |
| Rogue | Archery | Disarm trap (hook only; traps deferred) |
| Sorcerer | Spells | Recharge staff |

Each class defines starting stats, per-level gains, stat caps, starting kit,
and class skill in data.

### Melee

- An attack command starts a swing whose wind-up depends on weapon speed.
- The hit resolves on a defined hit-frame tick so animation can sync to it.
- To-hit: attacker accuracy vs. defender armor class and level difference,
  clamped to 5–95%.
- Damage: weapon range plus strength bonus. A successful shield block cancels
  the hit.
- A hit above a threshold interrupts the target (hit recovery).

### Projectiles

- Simulation objects that advance along a line each tick, stop at the first
  actor or wall, then apply damage or an effect.
- Shared by bows, ranged monsters, and bolt spells.

### Spells

- Definition: mana cost, effect, shape (projectile, area, self), cast time,
  per-spell-level scaling.
- Sources: learned spells (from spellbooks), single-use scrolls, staff
  charges.
- Starter set: Firebolt, Healing, Lightning, Fireball, Town Portal, Identify.

### Monsters

- Definition: stats, damage, speed, sprite set ID, AI behavior, loot table.
- AI behaviors: `melee-chase`, `ranged-kite`, `pack-leader` (minions follow).
- Spawn rules per dungeon level come from data and use the spawn markers
  Mortar levels already emit.
- Initial roster (original designs): skeleton warrior, skeleton archer, zombie,
  imp pack with leader, carrion bat, level-4 boss.

### Items

- Bases: type, slot (head, body, shield, weapon, two rings, amulet), inventory
  size, damage or armor range, stat requirements, item level, max durability.
- Quality:
  - Normal: no affixes.
  - Magic: up to one prefix and one suffix, rolled by item level and slot.
  - Unique: fixed name and affix list, minimum drop level. About 8 for the
    slice.
- Affixes: data-driven stat modifiers with item-level range and weight.
- Durability: weapons lose it on hits, armor when struck. At 0 the item gives
  no stats until repaired.
- Identification: magic and unique items drop unidentified with hidden
  affixes. Identified via scroll, spell, or the sage.
- Inventory: 10×4 grid with multi-cell items; 8-slot belt for potions only;
  gold stacks.
- Potions: healing and mana, used from the belt.

### Player death

The engine layer shows a death screen and offers to reload the last save.
Nothing is lost permanently in this slice.

### Town

- Safe zone: no monster spawns.
- Smith: sells, buys, and repairs weapons and armor.
- Healer: restores life, sells potions.
- Sage: identifies items, sells scrolls and spellbooks.
- Vendor stock regenerates on return from the dungeon using the world's seeded
  random generator.
- Gold is the only currency.
- Town Portal links dungeon and town both ways.

### Deferred

Traps, quests, additional dungeon themes (catacombs, caves, hell), multiplayer
networking, Unreal integration.

## `Mortar.Game` milestones

1. **Core:** ticks, commands, events, world, pathfinding, stats and leveling,
   warrior, melee, monsters and AI (melee-chase, pack-leader), normal and magic
   items, inventory and belt, loot, save/load.
2. **Ranged and magic:** projectiles, bows, rogue, sorcerer, spell system,
   spellbooks, scrolls, staves, ranged-kite AI, skeleton archer.
3. **Item depth and town:** uniques, durability and repair, identification,
   vendors and stock, gold economy, Town Portal.

Each milestone is fully tested headlessly before it is considered done.

## Content (`Mortar.Content`)

- JSON files, loaded with the same patterns and validation style as
  `Mortar.Json` (path-qualified errors, size limits, safe path handling).
- Loaded into immutable definition objects. The simulation never reads files.
- Sprite set and icon IDs are plain strings resolved by the engine layer.

## Save/load

- Versioned JSON: `{ "version": 1, ... }`.
- Contents: world seed, visited level IDs and seeds, current level, player
  (class, stats, experience, inventory, belt, equipment, spells, gold), each
  visited level's actors, ground items, and door states, vendor stock, random
  generator state, tick count.
- Levels are regenerated from seeds, not stored. Each level's fingerprint is
  stored; a mismatch on load is a clear error, not a silent broken map.
- Manual save plus auto-save on every level change.
- Version bumps ship with a migration from the previous version.
- Corrupt or unreadable saves produce a clear error and never crash.

## Unity layer (`Mortar.Unity`)

- Target: Unity 6 LTS.
- Distribution: UPM package containing prebuilt `Mortar.Core`, `Mortar.Json`,
  `Mortar.Content`, and `Mortar.Game` DLLs (netstandard2.1) plus Unity scripts.
- Frame loop: gather input into commands, run the simulation at a fixed 20 Hz,
  consume events, render by interpolating between the last two ticks.
- Rendering: Unity isometric tilemap for levels; 8-direction sprite sheets for
  actors with idle, walk, attack, hit, death, and cast animations driven by
  state and events.
- Camera: isometric, follows the player.
- UI: HUD with life and mana orbs and belt; inventory and equipment,
  character sheet, spellbook, vendor screens, save/load menus.
- A mapping asset resolves content sprite and icon IDs to Unity assets, so art
  can change without code changes.

## Art pipeline (`tools/art`)

- **AI-generated stills** via Cloudflare Workers AI (FLUX models): item icons,
  portraits, UI frames, loading screens, and flat seamless textures that are
  projected into isometric tiles by script.
- **3D-to-sprite bake** for anything animated: one 3D model per creature
  (free/CC0 or AI-generated), free animations (e.g. Mixamo), and a Blender
  script that renders 8 directions × each animation into sprite sheets with a
  fixed camera, lighting, and palette.
- Credentials come from environment variables or a gitignored `.env`; a
  committed `.env.example` lists names only. Generated art is reviewed by a
  person before it is committed.
- Prompts never reference protected names or styles of specific games.

A test run on 2026-10-14 confirmed the split: single icons and textures were
usable after rerolls, but separately generated character directions were
inconsistent in design, equipment, and facing.

## Testing

- `Mortar.Game`: unit tests per system; snapshot tests that run scripted
  command sequences and compare event logs.
- Determinism: replay the same seed and commands twice and compare state
  hashes.
- Save/load: round-trip produces identical state; every older save version
  migrates.
- Content: shipped data loads cleanly; malformed data fails with clear errors.
- Unity: a small play-mode suite checks level load and the command/event
  bridge.
- CI runs everything except Unity tests (which need a Unity license).

## Success criteria for the slice

- Start a new game as any of the three classes in town.
- Enter the dungeon and descend through levels 1–4, fighting the full monster
  roster with melee, bow, or spells.
- Find normal, magic, and unique items; identify, equip, repair, buy, and sell
  them in town.
- Defeat the level-4 boss.
- Save at any point, quit, reload, and continue with identical state.
