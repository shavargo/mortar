# Ranged and Magic (Milestone 2) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver milestone 2 of `Mortar.Game` headlessly: projectiles, bows and stackable ammo, rogue and sorcerer, six spells with books/scrolls/staves, class skills, ranged-kite AI, skeleton archer, two-way town portals, unidentified magic + Identify, and save v2 with v1 migration.

**Architecture:** A `Projectile` list on `LevelState` advances each tick after melee resolution. Ranged `Attack` and bolt spells spawn projectiles with a shared hit path. Spells live in a `SpellCatalog` on `ContentSet`; the player’s known spells sit on `PlayerProgress`. Item bases gain ranged/ammo/stack/spell/charges fields. TickRunner gains a projectile step. Saves bump to version 2.

**Tech Stack:** C# 12, .NET Standard 2.1 (`Mortar.Game`, `Mortar.Json`), .NET 10 (tests), System.Text.Json, xUnit.

**Spec:** `docs/superpowers/specs/2026-10-16-ranged-and-magic-design.md`. Builds on milestone 1 (core + items/saves).

## Global Constraints

- `Mortar.Game` targets `netstandard2.1` and references only `Mortar.Core`. No file IO, no System.Text.Json, no UnityEngine.
- Never use `System.Random`. Gameplay randomness comes from the world's `Pcg32`.
- No floating point in the simulation.
- No dictionary or hash-set enumeration in decision paths. Actors, items, and projectiles are processed in ascending id order; content in file order.
- Invalid commands produce a `CommandRejected` event and never throw. Invalid content produces `AuthoringResult` errors. Corrupt saves produce a clear error and never throw.
- Clean-room: no code, formulas, data tables, or names from devilutionX or the original game. All numbers are our own and live in content files.
- No Blizzard or Diablo names in code, data, or content.
- No file over 300 lines; no function over 50 lines.
- `Nullable` enabled and warnings treated as errors.
- Commit messages are short, one line, present tense, no prefixes.
- Stage files by explicit path. Never stage `.env` or `.superpowers/`.
- Unity package / sample art for new icons may land as placeholders or reuse existing icons; no Unity gameplay UI in this plan.

## Design Interpretations

1. **Projectile step is one cell per tick** along a discrete line from origin toward the aim cell (Bresenham-style). Max range from content (bow or spell).
2. **Friendly fire:** player-owned projectiles ignore the player and other future allies; they hit monsters. Monster-owned hit only the player.
3. **Ammo** uses item slot `ammo`, `stackMax` > 1. Spending decrements stack size; at 0 the item is removed. Belt is searched before inventory.
4. **Ranged weapons** set `ranged: true` and `ammo: "<baseId>"` on the item base. Melee weapons omit these.
5. **Unidentified magic:** `Item.Identified`; affixes ignored in `GearBonus` when false. Loot rolls magic unidentified. Kits and quest-like gear (books, scrolls, staves, potions, ammo, normal items) are identified.
6. **Spell level** is per known spell on the player (1…cap from rules). A second book of the same id increases level by 1.
7. **Staff cast** uses the equipped weapon’s `spell` id and spends `charges`; learned mana cast uses `CastCommand` and mana.
8. **Town portal anchor** in town: prefer a cell marked in area content; else the town up-stairs cell (or spawn). Recast replaces the pair. In-flight projectiles on a level are discarded when the player leaves that level.
9. **Fireball:** projectile that explodes in a Chebyshev radius on impact (actor or wall/floor at end of range), damaging hostile actors in range.
10. **Disarm:** `ClassSkillCommand` for rogue always rejects with a stable reason string (e.g. "no trap here") until traps exist.
11. **Save v1 migration:** on read, set version to 2 semantics: all items identified, no spells/portals/projectiles, staff charges = maxCharges when applicable.
12. **Skeleton archer** does not consume ammo; monster ranged attack spawns an arrow-like projectile from monster damage stats.
13. **Class select:** `World.Create` already takes `classId`; content gains rogue and sorcerer. Warrior unchanged.
14. **AiKinds** gains `ranged-kite`. Melee monsters keep setting Attack intent each tick; kite logic lives in `AiSystem` / a helper and may set Move or Attack.

## Review Focus

1. Projectile hits, wall stops, and range expiry are deterministic and id-ordered.
2. No ammo / no mana / no charges / unknown spell reject clearly without mutating state.
3. Town portal round-trips both ways; only one pair exists.
4. Identify enables affixes; unidentified magic gives no affix bonuses.
5. Save v2 round-trip preserves hash continuity; v1 saves migrate and load.

## File Structure

```
content/
  classes/rogue.json, sorcerer.json
  spells/*.json                         six spells
  items/…                               bow, arrows, books, scrolls, staves
  monsters/skeleton-archer.json
  areas/*.json                          spawn weight for archer (as needed)
  rules.json                            spell level cap, portal rules hooks
src/Mortar.Game/Content/
  SpellDefinition.cs, SpellCatalog.cs, SpellShapes.cs, SpellChecks.cs
  AiKinds.cs, ItemSlots.cs, ItemBaseDefinition.cs, ItemChecks.cs
  ClassDefinition.cs, MonsterDefinition.cs, ContentSet.cs, GameRules.cs (+ loaders)
src/Mortar.Game/Items/
  Item.cs                               Identified, StackCount, Charges
  ItemFactory.cs, GearBonus.cs, Belongings.cs (ammo find/spend)
src/Mortar.Game/Simulation/
  Projectile.cs, ProjectileSystem.cs, SpellSystem.cs, PortalSystem.cs
  CombatSystem.cs, ActionSystem.cs, ActionKind.cs, IntentKind.cs
  AiSystem.cs, CommandSystem.cs, TickRunner.cs, PlayerProgress.cs
  StartingKit.cs, LevelState.cs, World.cs, WorldHash.cs
src/Mortar.Game/Commands/
  CastCommand.cs, ClassSkillCommand.cs, UseItem extensions / ItemCommands
src/Mortar.Game/Events/
  ProjectileEvents.cs, SpellEvents.cs, PortalEvents.cs
src/Mortar.Game/Saves/
  WorldSnapshot.cs (v2), SnapshotCapture.cs, *Restorer.cs, migration
tests/Mortar.Tests/…                   mirror areas
docs/content.md, docs/saves.md          update
```

## Tasks

### Task 1: Content shapes — spells, ammo, ranged fields

**Files:** Content loaders/validators; `ItemSlots` (+ `ammo`); item/spell JSON schemas; tests.

- [ ] Add `SpellDefinition` / `SpellCatalog` / shape constants; validate six spells load.
- [ ] Extend item bases: `ranged`, `ammo`, `stackMax`, `spell`, `maxCharges`.
- [ ] Add ammo slot; stacking rules in factory/belongings tests (unit, no world yet).
- [ ] Commit: `add spell and ranged item content shapes`

### Task 2: Projectiles

**Files:** `Projectile`, `ProjectileSystem`, `LevelState`, `TickRunner`, events; tests.

- [ ] Spawn, step, wall stop, actor hit, range expiry; id ordering.
- [ ] Wire tick after melee resolve.
- [ ] Commit: `add projectile flight and hits`

### Task 3: Bows and ammo

**Files:** Combat/command paths; belongings spend; rogue class JSON; bow/arrows items; tests.

- [ ] Ranged attack starts shoot action; consumes ammo; spawns arrow.
- [ ] Reject without ammo or without ranged weapon as appropriate.
- [ ] Rogue content + starting kit.
- [ ] Commit: `add bows and stackable ammo`

### Task 4: Spell casting core

**Files:** `SpellSystem`, `CastCommand`, mana, action casting, Firebolt/Healing/Lightning; sorcerer JSON; tests.

- [ ] Learn list on `PlayerProgress`; cast learned spell for mana.
- [ ] Firebolt/Lightning projectiles; Healing self.
- [ ] Sorcerer kit with book or known spell + mana potion.
- [ ] Commit: `add spell casting for bolt and heal`

### Task 5: Fireball, books, scrolls, staves

**Files:** SpellSystem; item use; staff charges; content; tests.

- [ ] Fireball area on impact.
- [ ] Spellbook learn/level; scroll one-shot; staff charge cast.
- [ ] Commit: `add fireball and spell item sources`

### Task 6: Identify and unidentified loot

**Files:** `Item.Identified`, loot factory, GearBonus, Identify spell/scroll; tests.

- [ ] Magic loot unidentified; bonuses gated; Identify sets flag.
- [ ] Commit: `add unidentified magic and identify`

### Task 7: Town Portal

**Files:** `PortalSystem`, world portal pair, cast + enter; tests.

- [ ] Open/replace pair; warp both directions; reject cast in town if desired by design (cast only in dungeon).
- [ ] Commit: `add two-way town portals`

### Task 8: Class skills and ranged-kite archer

**Files:** `ClassSkillCommand`, AiSystem kite, skeleton-archer, spawns; tests.

- [ ] Disarm stub reject; recharge staff.
- [ ] Ranged-kite behavior; archer monster + area spawns.
- [ ] Commit: `add class skills and skeleton archer kite`

### Task 9: Saves v2 + docs

**Files:** Snapshots, restorers, migration, `docs/saves.md`, `docs/content.md`; tests.

- [x] Persist spells, portals, projectiles, identified, charges, stacks.
- [x] v1 migration; round-trip hash test.
- [x] Commit: `save spells portals and ammo as version 2`

### Task 10: Final review

- [x] `dotnet test Mortar.slnx` green; Release 0 warnings.
- [x] Whole-branch review against Review Focus; fix Important issues.
- [x] Commit any fixes; mark milestone 2 sim complete.

## Unity follow-up (not this plan)

A later plan covers: cast/shoot animations, projectile views, portal markers, spellbook UI, class select including rogue/sorcerer in the sample menu, and art for new items/archer.
