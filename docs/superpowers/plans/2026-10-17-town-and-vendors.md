# Town and Vendors (Milestone 3, town-first) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the town-first half of framework milestone 3 headlessly: smith/healer/sage sessions, gold buy/sell/repair/identify/heal, timed stock regen, minimal durability, save v3.

**Architecture:** `ActiveVendor` + per-vendor stock lists on `World` (portal-style). Talk arms a session; vendor actions re-check town + Chebyshev ≤ 1. Stock regenerates every 144 000 world ticks. Item bases gain `value` and `maxDurability`; combat applies minimal wear; smith restores current to max.

**Tech Stack:** C# 12, .NET Standard 2.1 (`Mortar.Game`, `Mortar.Json`), .NET 10 (tests), System.Text.Json, xUnit.

**Spec:** `docs/superpowers/specs/2026-10-17-town-and-vendors-design.md`. Builds on milestone 2.

## Global Constraints

- `Mortar.Game` targets `netstandard2.1` and references only `Mortar.Core`. No file IO, no System.Text.Json, no UnityEngine.
- Never use `System.Random`. Gameplay randomness comes from the world's `Pcg32`.
- No floating point in the simulation.
- No dictionary or hash-set enumeration in decision paths. Stock and belongings processed in stable id / index order; content in file order.
- Invalid commands produce a `CommandRejected` event and never throw. Invalid content produces `AuthoringResult` errors. Corrupt saves produce a clear error and never throw.
- Invent Mortar numbers and names in content. No Blizzard or Diablo proper names in code, data, or content. External repos may be studied for *behavior* only.
- No file over 300 lines; no function over 50 lines.
- `Nullable` enabled and warnings treated as errors.
- Commit messages are short, one line, present tense, no prefixes.
- Stage files by explicit path. Never stage `.env` or `.superpowers/`.
- No Unity gameplay UI in this plan (demo stays warrior-only).

## Design Interpretations

1. **Proximity:** Talk and vendor actions require `Depth == 0` and Chebyshev distance ≤ 1 to that vendor’s town marker (same family as portal/stairs).
2. **Hybrid session:** `TalkCommand` sets `ActiveVendor`. Buy/Sell/Repair/Identify/Heal require matching active vendor + adjacency. `LeaveVendorCommand` clears. Leaving town or stepping out of range clears and emits `VendorSessionEnded`.
3. **Stock timer:** `NextStockRegenTick`; when `TickCount >=` deadline, refill all three vendors and add `VendorStockRegenTicks` (default **144000**). Runs as a world tick step, not on Talk.
4. **Stock level:** item level = `max(1, max visited depth among depths > 0)`; if never left town, use 1.
5. **Shop goods are identified.** Sage identify remains for dungeon finds.
6. **No buyback:** sold items are destroyed for gold; not inserted into stock.
7. **Pricing:** buy = `value` (× `magicBuyMult` if magic); sell = buy × `sellPercent` / 100; repair = `value × missing × repairPercent / (max × 100)` min 1 if damaged; identify/heal = flat fees. All integer, floored.
8. **Default rules (tune in content):** `sellPercent` 25, `magicBuyMult` 3, `repairPercent` 50, `identifyFee` 50, `healFee` 0; smith 8 slots / healer 6 / sage 6; smith `magicPercent` 40.
9. **Durability minimal:** weapon −1 on successful player hit; body + shield −1 when player is hit; at 0 still grant affixes (items pass later); potions/ammo/books/scrolls `maxDurability` 0 (N/A).
10. **Vendor accept rules:** smith buys/sells/repairs weapons, body, head, shield, rings, amulets; healer sells potions + heal service; sage sells scrolls/books + identify service. Cross-type sell rejects.
11. **Save v3:** durability, stocks, `ActiveVendor`, `NextStockRegenTick`. v2→v3: gear `current=max`, empty stocks + schedule regen, no session.
12. **Items pass deferred:** uniques, 0-dur disables bonuses, warrior field repair.

## Review Focus

1. Session arm/clear and adjacency rejects never leave the player in a stuck shop state.
2. Buy/sell/repair gold paths never mutate on reject (afford, full bag, wrong vendor).
3. Stock regen is deterministic for a given seed + tick + visited depths.
4. Durability wear and smith repair round-trip in WorldHash.
5. Save v3 hash continuity; v2 migrates cleanly.

## Default rules additions

```json
"vendorStockRegenTicks": 144000,
"vendorSellPercent": 25,
"vendorMagicBuyMult": 3,
"vendorRepairPercent": 50,
"vendorIdentifyFee": 50,
"vendorHealFee": 0,
"vendorSmithSlots": 8,
"vendorHealerSlots": 6,
"vendorSageSlots": 6,
"vendorSmithMagicPercent": 40
```

## File Structure

```
content/rules.json                      vendor + durability-related rules
content/items/*.json                    value, maxDurability on bases
src/Mortar.Game/Content/
  GameRules.cs, ItemBaseDefinition.cs, ItemChecks.cs, VendorKinds.cs
src/Mortar.Game/Items/
  Item.cs                               Durability / MaxDurability
  ItemFactory.cs, Pricing.cs
src/Mortar.Game/Simulation/
  VendorState.cs, VendorStockSystem.cs, TownServices.cs, DurabilitySystem.cs
  World.cs, TickRunner.cs, CommandSystem.cs, CombatSystem.cs, WorldHash.cs
src/Mortar.Game/Commands/
  TalkCommand.cs, LeaveVendorCommand.cs, BuyCommand.cs, SellCommand.cs,
  RepairCommand.cs, IdentifyServiceCommand.cs, HealServiceCommand.cs
src/Mortar.Game/Events/
  VendorEvents.cs
src/Mortar.Game/Saves/
  WorldSnapshot.cs (v3), SnapshotCapture, restorers, SaveMigration
src/Mortar.Json/…                       rules/item loaders
tests/Mortar.Tests/Gameplay/            Vendor*, Durability*, Save*
docs/content.md, docs/saves.md
```

## Tasks

### Task 1: Content shapes — value, durability, vendor rules

**Files:** `GameRules`, `ItemBaseDefinition`, loaders/validators, `content/rules.json`, item JSON samples; tests.

- [ ] Add rules fields listed above; validate ranges.
- [ ] Add `value` (≥ 0) and `maxDurability` (≥ 0) on item bases; gear that can be worn/wielded needs `maxDurability` ≥ 1 except rings/amulets may use a small default if omitted (plan: require explicit on weapon/body/head/shield; rings/amulets default 20 in loader or require explicit — **require explicit for all equippable**).
- [ ] Commit: `add item value durability and vendor rules`

### Task 2: Minimal durability in combat

**Files:** `Item` current/max; factory spawn full; `DurabilitySystem` / hooks in `CombatSystem`; tests.

- [ ] Weapon −1 on successful player hit; body+shield −1 when player takes damage.
- [ ] Floor at 0; no bonus gating yet.
- [ ] Commit: `add minimal item durability wear`

### Task 3: Vendor state, Talk, Leave, session

**Files:** `VendorKinds`, `VendorState`, World fields, Talk/Leave commands, events, CommandSystem; tests with town content.

- [ ] Talk at marker arms vendor; Leave clears; out-of-range / leave town auto-clears + event.
- [ ] Reject Talk in dungeon / not adjacent / unknown marker.
- [ ] Commit: `add vendor talk session`

### Task 4: Stock regen

**Files:** `VendorStockSystem`, TickRunner step, content pools (filter bases by slot + dropWeight), World create initial fill; tests.

- [ ] Regen all vendors on deadline; schedule next; event.
- [ ] Item level from deepest visited dungeon depth; identified shop items.
- [ ] Commit: `add timed vendor stock regen`

### Task 5: Pricing + Buy / Sell

**Files:** `Pricing`, Buy/Sell commands, TownServices; tests.

- [ ] Buy removes stock, stows, spends; sell takes item, grants gold; wrong vendor / can't afford / bag full reject cleanly.
- [ ] Commit: `add vendor buy and sell`

### Task 6: Repair, Identify service, Heal service

**Files:** TownServices; smith repair; sage identify (reuse `SpellSystem.IdentifyItem` or shared helper); healer full life (+ mana? **life only** this slice); tests.

- [ ] Repair fee then `current = max`.
- [ ] Identify fee then identify.
- [ ] Heal fee then `Life = MaxLife`.
- [ ] Commit: `add vendor repair identify and heal`

### Task 7: Saves v3 + docs

**Files:** Snapshots, capture, restorers, migration, `docs/saves.md`, `docs/content.md`; tests.

- [ ] Persist durability, stocks, active vendor, next regen tick.
- [ ] v2→v3 migration; round-trip hash.
- [ ] Commit: `save vendors and durability as version 3`

### Task 8: Final review

- [ ] `dotnet test Mortar.slnx` green; Release 0 warnings.
- [ ] Review against Review Focus; fix Important issues.
- [ ] Commit any fixes; mark town-first milestone complete.

## Later (not this plan)

- **Items pass:** uniques (~8), 0-durability disables bonuses, warrior field repair.
- **Unity:** vendor UI, class select, M2 presentation (projectiles, portals, spellbook).
