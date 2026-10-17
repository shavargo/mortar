# Item Depth (Milestone 3, items pass) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Finish headless item depth: uniques (per-base, once-per-world), durability 0 contributes nothing, warrior field repair, save v4.

**Architecture:** `UniqueDefinition` content + promotion in `ItemFactory` after base pick; world-ordered dropped-unique flags; `GearBonus` skips broken gear; warrior `ClassSkills.Repair` via `ClassSkillCommand` with target item id; mutable instance `MaxDurability`; save v4.

**Tech Stack:** C# 12, .NET Standard 2.1 (`Mortar.Game`, `Mortar.Json`), .NET 10 (tests), System.Text.Json, xUnit.

**Spec:** `docs/superpowers/specs/2026-10-17-items-depth-design.md` (approved). Builds on town-first M3.

## Global Constraints

- `Mortar.Game` targets `netstandard2.1` and references only `Mortar.Core`. No file IO, no System.Text.Json, no UnityEngine.
- Never use `System.Random`. Gameplay randomness comes from the world's `Pcg32`.
- No floating point in the simulation.
- No dictionary or hash-set enumeration in decision paths. Dropped-unique flags: store as sorted `List<string>` (content id order when adding) or scan content-ordered unique list against a set for membership only — never `foreach` a HashSet for gameplay. Prefer `List<string>` kept sorted + binary search, or a `bool[]` indexed by unique catalog order.
- Invalid commands → `CommandRejected`. Invalid content → `AuthoringResult` errors. Corrupt saves → clear error, never throw.
- Invent Mortar names/numbers. No Blizzard/Diablo proper names. DevilutionX is behavior reference only.
- No file over 300 lines; no function over 50 lines.
- `Nullable` enabled; warnings as errors.
- Commit messages: short, one line, present tense, no prefixes.
- Stage by explicit path. Never stage `.env` or `.superpowers/`.
- No Unity work in this plan.

## Design Interpretations

1. **Broken gear:** If `item.MaxDurability > 0 && item.Durability == 0`, `GearBonus` ignores the item entirely (no armor, block, weapon damage/swing, affixes, or unique powers). Still equippable.
2. **Unique pipeline:** After base is chosen in `ItemFactory.RollFrom`, before magic: if `uniqueChance` roll succeeds (`NextInt(100) < uniqueChance`) and an eligible unique exists for that base (minLevel ≤ itemLevel, not yet dropped), create unique item, mark dropped, skip magic. Else existing magic/normal path.
3. **UniqueChance:** New field on `LootTableDefinition` (0–100). Content defaults: swarm/common/pack = **1**, boss = **15**. `ItemFactory.Roll` / `RollFrom` gain `uniqueChance` parameter; `LootSystem` passes table value; vendor stock uses **0** (shops never roll uniques this slice).
4. **Unique pick:** Eligible candidates in **content file order**; pick with equal weight via `PickWeighted` or `NextInt(count)`. First match wins among remaining.
5. **Once-per-world:** On unique create, record unique id on `World`. Later eligibility excludes flagged ids. Persist in save v4.
6. **Identify:** Unidentified unique `Name` = `Definition.Name` (base). Identified = unique's `Name`. Powers apply only when `Identified` (and not broken). Shop stock stays identified; uniques are not sold this slice.
7. **Quality:** `ItemQuality.Unique` when `UniqueId` set. Pricing: treat Unique like Magic for `VendorMagicBuyMult` (or use unique's optional `value` override if set — prefer **unique definition `value` if > 0, else base value × magic mult**).
8. **Powers:** `UniqueDefinition.Powers` = ordered list of `{ affixId, value }` (fixed). Stored on item as `IReadOnlyList<RolledAffix>` or looked up from content via `UniqueId` at gear time — **look up from content** so saves only store `uniqueId` (affix list not duplicated). Validate values in range at load.
9. **Field repair:** `ClassSkills.Repair = "repair"`. Warrior `classSkill: "repair"`. Extend `ClassSkillCommand` with optional `int? ItemId`. Repair path: item must be in equipment, `MaxDurability > 0`, not already full. Loop (DevilutionX-style): `rep += level + Rng.NextInt(level)` (if level < 1 treat as 1); `max -= Max(max / (level + 9), 1)`; if max == 0 destroy (unequip + remove from belongings, emit destroyed); else when `durability + rep >= max` set durability = min(durability+rep, max) and stop. Emit `ItemFieldRepaired`.
10. **Smith repair:** unchanged — `Durability = MaxDurability`, no max loss. Uses **instance** max.
11. **Instance max:** `Item.MaxDurability` becomes settable (`internal set`). Create at definition max. Saves store both current and max. Restore: `1 ≤ max ≤ definition.MaxDurability` (or 0 for N/A), `0 ≤ current ≤ max`.
12. **Save v4:** `uniqueId` on item snapshots; `maxDurability` on item snapshots; `DroppedUniques` list on world. v3→v4: max = def max (from base id); uniqueId null; dropped empty. Chain v1/v2 migrations through v3 then v4.
13. **Content:** ~8 uniques in `content/uniques.json` (or folder); wire via `game.json` `"uniques": "uniques.json"`. Original names only.

## Review Focus

1. Unique rolls are deterministic; once dropped, that id never rolls again in the same world.
2. Unidentified unique shows base name; powers inert until identify.
3. Durability 0 contributes nothing; flip / replace `AffixesStillApplyAtZeroDurability`.
4. Field repair reduces max, can destroy; smith does not reduce max.
5. Save v3→v4 + round-trip WorldHash.

## File Structure

```
content/game.json                       + uniques entry
content/uniques.json                    ~8 UniqueDefinitions
content/loot.json                       uniqueChance per table
content/classes/warrior.json            classSkill: repair
src/Mortar.Game/Content/
  UniqueDefinition.cs, ClassSkills.cs, ItemCatalog.cs, ContentSet.cs,
  LootTableDefinition.cs, ItemChecks.cs, DefinitionChecks / manifest
src/Mortar.Game/Items/
  Item.cs (UniqueId, Quality, Name, mutable MaxDurability)
  ItemFactory.cs, GearBonus.cs, Pricing.cs
src/Mortar.Game/Simulation/
  ClassSkillSystem.cs, World.cs, WorldHash.cs, LootSystem.cs, VendorStockSystem.cs
src/Mortar.Game/Commands/ClassSkillCommand.cs
src/Mortar.Game/Events/…                ItemFieldRepaired, ItemDestroyed (or reuse)
src/Mortar.Game/Saves/                  v4 snapshots, migration, capture, restore
src/Mortar.Json/Content/                DTOs, mappers, loaders
tests/Mortar.Tests/Gameplay/            Unique*, Durability*, FieldRepair*, Save*
docs/content.md, docs/saves.md
```

## Tasks

### Task 1: Unique content model + loaders

**Files:** `UniqueDefinition`, catalog/ContentSet, JSON DTO/mapper/loader, `game.json`, empty/stub `uniques.json`, validators; tests in `ItemContentTests` or new `UniqueContentTests`.

- [x] `UniqueDefinition(id, name, baseItemId, minLevel, powers[], value?)`.
- [x] Power = affix id + fixed value; validate affix exists, value in affix range, base exists, unique ids unique, one unique per base **or** multiple per base allowed (DevilutionX allows multiple — **allow multiple per base**).
- [x] `LootTableDefinition` + DTO: `uniqueChance` (default 0 if omitted in tests; content sets 1/15).
- [x] Commit: `add unique item content model`

### Task 2: Item unique quality + GearBonus broken gear

**Files:** `Item.cs`, `GearBonus.cs`, `Pricing.cs`; flip durability test; unit tests for name/quality/bonus.

- [x] `ItemQuality.Unique`; `UniqueId` string?; mutable `MaxDurability`.
- [x] `Name`: unidentified unique → base name; identified → unique name from content (Item needs ContentSet or store display name — **store `UniqueName` on item at create** OR resolve via `world.Content` in a helper; prefer resolve in `Item.DisplayName(ContentSet)` only if Name stays parameter-free — simplest: store `uniqueName` string on item when created, empty if not unique).
- [x] `GearBonus.From`: skip items with max>0 and dur==0; apply unique powers from content when Identified (pass ContentSet into From, or bake powers as `IReadOnlyList<RolledAffix>` on the item at create — **bake RolledAffix list on item at create** so GearBonus stays ContentSet-free).
- [x] Replace `AffixesStillApplyAtZeroDurability` with `BrokenGearContributesNothing`.
- [x] Commit: `gate gear bonuses on durability and unique quality`

### Task 3: Loot promotion + once-per-world flags

**Files:** `ItemFactory`, `World` dropped list, `LootSystem`, `VendorStockSystem` (uniqueChance 0), WorldHash; tests.

- [ ] `World.WasUniqueDropped` / `MarkUniqueDropped` with ordered storage.
- [ ] Factory: unique chance → eligible → create unique (identified false, full dur, powers baked, UniqueId set) → mark dropped.
- [ ] Determinism test: same seed same unique; second eligible roll skips flagged id.
- [ ] Commit: `roll uniques from loot with once-per-world flags`

### Task 4: Author ~8 uniques + loot chances

**Files:** `content/uniques.json`, `content/loot.json`; content load smoke test.

- [ ] Eight original uniques across existing bases (knife, blade, hatchet, vest, hauberk, helm, shield, ring/amulet).
- [ ] loot uniqueChance: 1 / 1 / 1 / 15 for swarm/common/pack/boss.
- [ ] Commit: `add unique item content and loot chances`

### Task 5: Warrior field repair

**Files:** `ClassSkills`, warrior.json, `ClassSkillCommand(ItemId?)`, `ClassSkillSystem`, events; tests.

- [ ] Repair skill path with DX-style loop; reject non-warrior / missing / unequipped / N/A / full.
- [ ] Destroy path unequips and removes item.
- [ ] Commit: `add warrior field repair skill`

### Task 6: Saves v4 + docs

**Files:** snapshots, capture, restorers, `SaveMigration`, `WorldSnapshot.CurrentVersion = 4`, docs; tests.

- [ ] Persist maxDurability, uniqueId, dropped uniques; migrate v3→v4; round-trip hash.
- [ ] Update `docs/saves.md`, `docs/content.md`.
- [ ] Commit: `save uniques and instance max durability as version 4`

### Task 7: Final review

- [ ] `dotnet test` on `Mortar.slnx` green; Release 0 warnings.
- [ ] Review against Review Focus; fix Important issues.
- [ ] Commit any fixes; mark items pass complete.

## Later (not this plan)

- Unity unique tooltips / field-repair UI / vendor presentation.
- Shop-sold uniques; more than ~8 uniques.
