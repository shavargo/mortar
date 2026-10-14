# Items, Loot, and Save/Load (Milestone 1b) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Finish milestone 1 of `Mortar.Game`: normal and magic items, inventory, belt, equipment, gold, potions, monster loot, and versioned JSON save/load that restores an identical world.

**Architecture:** Item content (bases, affixes, loot tables) joins `ContentSet` through an `ItemCatalog`. Runtime items live in `Mortar.Game.Items` (inventory grid, belt, equipment, ground items); equipment feeds `PlayerStats.Compute` through a `GearBonus`. Monsters drop loot from their loot table when they die, and the player picks items up with a walking `PickUp` intent. Saves are a plain-data `WorldSnapshot` in `Mortar.Game` (capture and fully validated restore, no IO); `Mortar.Json.Saves.SaveFile` turns snapshots into versioned JSON files.

**Tech Stack:** C# 12, .NET Standard 2.1 (`Mortar.Game`, `Mortar.Json`), .NET 10 (tests), System.Text.Json, xUnit.

**Spec:** `docs/superpowers/specs/2026-10-14-arpg-framework-design.md` (milestone 1: "normal and magic items, inventory and belt, loot, save/load"; sections Items, Save/load, Testing). Builds on `docs/superpowers/plans/2026-10-14-game-simulation-core.md` (milestone 1a, complete).

## Global Constraints

- `Mortar.Game` targets `netstandard2.1` and references only `Mortar.Core`. No file IO, no System.Text.Json, no UnityEngine.
- Never use `System.Random`. Gameplay randomness comes from the world's `Pcg32`.
- No floating point in the simulation.
- No dictionary or hash-set enumeration in decision paths. Actors and items are processed in ascending id order; content in file order.
- Invalid commands produce a `CommandRejected` event and never throw. Invalid content produces `AuthoringResult` errors. Corrupt or unreadable saves produce a clear error and never throw.
- Clean-room: no code, formulas, data tables, or names from devilutionX or the original game. All numbers are our own and live in content files.
- No Blizzard or Diablo names in code, data, or content.
- No file over 300 lines; no function over 50 lines.
- `Nullable` enabled and warnings treated as errors in every project (already set in `Directory.Build.props`).
- Commit messages are short, one line, present tense, no prefixes.
- Stage files by explicit path. Never stage `.env` files or anything under `.superpowers/`.

## Design Interpretations

The spec leaves these choices open; this plan fixes them. Review them before execution.

1. **Gold is a counter on the player**, not items in inventory cells. Gold on the ground is a pile with an amount.
2. **Inventory is 10×4, the belt has 8 slots** (spec values), as code constants. Items are 1–2 cells wide and 1–3 tall; rings, amulets, and potions are 1×1.
3. **Uniques, durability, identification, vendors, and prices are milestone 3.** Magic items drop already identified in 1b, and item bases have no durability yet.
4. **Magic items:** each loot table has a magic chance. A magic item rolls one of three shapes with equal odds (prefix only, suffix only, both). A part with no eligible affix is skipped; if neither part rolls, the item stays normal. Potions are never magic. A dropped item's level is the monster's level, and only bases with `itemLevel` at or below it can drop.
5. **Ground items:** a drop goes to the nearest walkable cell (rings of radius 0–3 around the corpse) that has no ground item, ignoring actors; if none is free it lands on the corpse cell. A ground item keeps its item's id; gold piles take ids from the same counter (`World.NextItemId`).
6. **Requirements use base stats only** (not gear bonuses), so an item can never enable itself.
7. **Gear changes are instant** and allowed during any action. They recompute the player's profile; life and mana move by the change in their maximums (existing `Actor.ApplyProfile`).
8. **`PickUp(itemId)` walks** until the item is in reach (same cell, or adjacent without cutting a wall corner), then picks it up once the player is idle. If there is no room, a `PickupFailed` event is emitted and the item stays.
9. **Picked-up potions go to the belt first**, then the inventory. Other items go to the first free inventory spot, scanning columns left to right and each column top to bottom.
10. **Rings** equip into the first empty ring slot, or replace the left ring when both are full.
11. **Starting kit:** a class lists `startingItems`. Each is created plain (no affixes, armor at the base's maximum, no randomness used). Equippable items fill empty slots; the rest are stowed like pickups. Items that don't fit are left out.
12. **Weapon speed:** a weapon may set `swing` (`attackTicks`, `hitFrame`); otherwise the class timings apply.
13. **Loot drops whenever a monster dies**, after any experience reward.
14. **Saves:** `WorldSnapshot` (plain data, in `Mortar.Game`) is the save model; `Mortar.Json` only serializes it. Levels are regenerated from stored seeds and checked against the stored fingerprint. Monsters are restored from the save, never respawned. Version 1 is the only version; any other version is rejected with a clear message (no migrations exist yet).
15. **Auto-save on level change is the engine's job:** it watches `LevelChanged` and calls `SaveFile.Write`. The simulation never touches files. A save of a dead player loads (game-over state); the engine decides what to offer.

## Review Focus

1. **Corrupt or hand-edited saves** (bad JSON, wrong types, missing fields, nulls inside lists, unknown ids, overlapping items, an even random-generator increment, a fingerprint mismatch, huge numbers) return a clear error and never throw. Pinned in Tasks 10 and 11.
2. **Save, load, continue:** a restored world has the same state hash and produces the same event log as the original when both keep playing. Pinned in Tasks 10 and 11.
3. **A full inventory** never loses or duplicates items: a pickup leaves the item on the ground with `PickupFailed`, and an equip swap with no room is rejected with nothing changed. Pinned in Tasks 5 and 6.
4. **Loot is deterministic and never lands in a wall.** Pinned in Task 4.
5. **Unequipping restores the exact previous stats.** Pinned in Task 6.

## File Structure

```
src/Mortar.Game/Content/
  ItemSlots.cs, BonusKinds.cs              slot and bonus names
  ItemBaseDefinition.cs                    + WeaponSwing, PotionKinds, PotionEffect
  AffixDefinition.cs                       + AffixPositions
  LootTableDefinition.cs, ItemCatalog.cs
  ItemChecks.cs                            item, affix, loot table validation
  ClassDefinition.cs, MonsterDefinition.cs + startingItems, lootTableId
  ContentSet.cs, ContentValidator.cs, DefinitionChecks.cs, Stats.cs (modified)
src/Mortar.Game/Items/
  Item.cs                                  Item, RolledAffix, ItemQuality
  Inventory.cs, Belt.cs, Equipment.cs, Belongings.cs, GroundItem.cs
  GearBonus.cs, ItemFactory.cs
src/Mortar.Game/Simulation/
  StartingKit.cs, LootSystem.cs, PickupSystem.cs, EquipActions.cs, InventoryActions.cs
  World.cs, LevelState.cs, Actor.cs, IntentKind.cs, PlayerStats.cs, PlayerGrowth.cs,
  DeathSystem.cs, ActionSystem.cs, Movement.cs, CommandSystem.cs, TickRunner.cs,
  WorldHash.cs, WorldResult.cs (modified)
src/Mortar.Game/Commands/ItemCommands.cs   PickUp, Equip, Unequip, DropItem, MoveItem, PutInBelt, UseBelt
src/Mortar.Game/Events/ItemEvents.cs
src/Mortar.Game/Saves/
  WorldSnapshot.cs, ItemSnapshots.cs, SnapshotCapture.cs
  SaveException.cs (+ Check), RestoredIds.cs
  WorldRestorer.cs, LevelRestorer.cs, ActorRestorer.cs, ItemRestorer.cs, PlayerRestorer.cs
src/Mortar.Json/Content/ItemDtos.cs, ItemMapper.cs, ItemContentLoader.cs (+ ContentDtos, ContentMapper, GameContentLoader modified)
src/Mortar.Json/Saves/SaveFile.cs
content/items/*.json, content/affixes.json, content/loot.json (+ game.json, warrior.json, monsters modified)
docs/content.md (modified), docs/saves.md, README.md (modified)
tests/Mortar.Tests/Gameplay/
  ItemContentTests.cs, ItemContainerTests.cs, GearTests.cs, LootTests.cs, PickupTests.cs,
  InventoryCommandTests.cs, SaveCaptureTests.cs, SaveRestoreTests.cs
  Support/TestItems.cs (+ TestGameContent, TestWorlds, ScriptedPlayer modified)
tests/Mortar.Tests/Json/SaveFileTests.cs (+ GameContentLoaderTests modified)
```

Test commands use `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~<class>"`; the full suite is `dotnet test`.

---

### Task 1: Item content definitions and validation

**Files:**
- Create: `src/Mortar.Game/Content/ItemSlots.cs`, `BonusKinds.cs`, `ItemBaseDefinition.cs`, `AffixDefinition.cs`, `LootTableDefinition.cs`, `ItemCatalog.cs`, `ItemChecks.cs`
- Modify: `src/Mortar.Game/Content/ClassDefinition.cs`, `MonsterDefinition.cs`, `ContentSet.cs`, `ContentValidator.cs`, `DefinitionChecks.cs`, `LevelDefinition.cs`, `GameRules.cs`
- Modify: `src/Mortar.Json/Content/ContentMapper.cs`, `GameContentLoader.cs` (compile only; Task 7 maps the real fields)
- Modify: `tests/Mortar.Tests/Gameplay/Support/TestGameContent.cs`, `tests/Mortar.Tests/Gameplay/ContentValidatorTests.cs`
- Test: `tests/Mortar.Tests/Gameplay/ItemContentTests.cs`

**Interfaces:**
- Consumes: `IntRange`, `Stats`, `StatKind`, `DefinitionChecks.MaxValue`, the `errors.Add(source, message)` extension, `AuthoringResult<T>`.
- Produces:
  - `ItemSlots` (`Weapon`, `Body`, `Head`, `Shield`, `Ring`, `Amulet`, `Potion`, `IsKnown`, `IsEquippable`), `BonusKinds` (`Strength`, `Dexterity`, `Vitality`, `Magic`, `Life`, `Mana`, `ArmorClass`, `ToHit`, `Damage`, `IsKnown`), `PotionKinds` (`Life`, `Mana`, `IsKnown`), `AffixPositions` (`Prefix`, `Suffix`, `IsKnown`).
  - `WeaponSwing(int attackTicks, int hitFrame)`, `PotionEffect(string restores, int amount)`.
  - `ItemBaseDefinition(string id, string name, string iconId, string slot, int width, int height, int itemLevel, int dropWeight, Stats requirements, IntRange? damage, WeaponSwing? swing, IntRange? armor, int blockChance, PotionEffect? potion)` with `IsEquippable`, `IsPotion`.
  - `AffixDefinition(string id, string name, string position, IReadOnlyList<string> slots, IntRange itemLevel, int weight, string bonus, IntRange value)` with `Allows(string slot, int itemLevel)`.
  - `LootTableDefinition(string id, int rolls, int noneWeight, int goldWeight, int itemWeight, IntRange gold, int magicPercent)`.
  - `ItemCatalog(bases, affixes, lootTables)` with `Bases`, `Affixes`, `LootTables`, `ItemCatalog.Empty`.
  - `ClassDefinition(..., ActionTimings timings, IReadOnlyList<string> startingItems)` with `StartingItems`; `MonsterDefinition(..., MinionSpec? minions, string? lootTableId)` with `LootTableId`.
  - `ContentSet.Create(rules, classes, monsters, levels, ItemCatalog items)`; `ContentSet.Items`, `ItemBases`, `Affixes`, `TryGetMonster`, `ItemBase(id)`, `TryGetItemBase`, `TryGetAffix`, `LootTable(id)`.
  - Test helpers: `TestGameContent.Warrior(params string[] startingItems)`, `Monster(..., string? loot = null)`, `Weapon(...)`, `Armor(...)`, `Ring(...)`, `Potion(...)`, `Affix(...)`, `Loot(...)`, `Items(...)`, `Create(..., ItemCatalog? items = null)`, `Valid(monsters, levels, rules, ItemCatalog? items = null, IReadOnlyList<ClassDefinition>? classes = null)`.

- [ ] **Step 1: Update the test helpers**

Replace `tests/Mortar.Tests/Gameplay/Support/TestGameContent.cs` with:

```csharp
using Mortar.Core.Authoring;
using Mortar.Game.Content;

namespace Mortar.Tests.Gameplay.Support;

internal static class TestGameContent
{
    public static GameRules Rules(
        int minHit = 5,
        int maxHit = 95,
        int recoveryPercent = 20,
        long[]? thresholds = null,
        int experiencePercentPerLevel = 10,
        int minimumExperiencePercent = 0) =>
        new GameRules(
            minHit, maxHit, 50, 1, 1, 2, 5, 10, recoveryPercent,
            experiencePercentPerLevel, minimumExperiencePercent, thresholds ?? new long[] { 100, 300, 600 });

    public static ActionTimings Timings(int move = 4, int attack = 6, int hit = 3, int recovery = 3) =>
        new ActionTimings(move, attack, hit, recovery);

    public static ClassDefinition Warrior(params string[] startingItems) => new ClassDefinition(
        "warrior",
        "Warrior",
        "warrior",
        new Stats(20, 20, 20, 10),
        new Stats(100, 100, 100, 100),
        new ClassGrowth(10, 2, 2, 0, 1, 1, 5),
        new IntRange(2, 4),
        Timings(),
        startingItems);

    public static MonsterDefinition Monster(
        string id,
        string ai = AiKinds.MeleeChase,
        MinionSpec? minions = null,
        int life = 10,
        int sight = 6,
        int experience = 50,
        string? loot = null) =>
        new MonsterDefinition(id, id, id, 1, new IntRange(life, life), 0, 50, new IntRange(1, 2), 0, Timings(), sight, experience, ai, minions, loot);

    public static LevelDefinition Level(int depth, string monsterId = "zombie", string? bossId = null) =>
        new LevelDefinition(depth, "test", new[] { new SpawnEntry(monsterId, 1) }, bossId);

    public static ItemBaseDefinition Weapon(
        string id = "knife", int damageMin = 3, int damageMax = 5, int strength = 0, WeaponSwing? swing = null, int itemLevel = 1, int dropWeight = 1) =>
        new ItemBaseDefinition(
            id, id, id, ItemSlots.Weapon, 1, 2, itemLevel, dropWeight, new Stats(strength, 0, 0, 0),
            new IntRange(damageMin, damageMax), swing, null, 0, null);

    public static ItemBaseDefinition Armor(
        string id = "vest", string slot = ItemSlots.Body, int armor = 5, int width = 2, int height = 2, int blockChance = 0, int dropWeight = 1) =>
        new ItemBaseDefinition(
            id, id, id, slot, width, height, 1, dropWeight, default, null, null, new IntRange(armor, armor), blockChance, null);

    public static ItemBaseDefinition Ring(string id = "ring", int dropWeight = 1) =>
        new ItemBaseDefinition(id, id, id, ItemSlots.Ring, 1, 1, 1, dropWeight, default, null, null, null, 0, null);

    public static ItemBaseDefinition Potion(string id = "tonic", int amount = 20, string restores = PotionKinds.Life, int dropWeight = 1) =>
        new ItemBaseDefinition(id, id, id, ItemSlots.Potion, 1, 1, 1, dropWeight, default, null, null, null, 0, new PotionEffect(restores, amount));

    public static AffixDefinition Affix(string id, string position, string bonus, int value, params string[] slots) =>
        new AffixDefinition(id, id, position, slots, new IntRange(1, 10), 1, bonus, new IntRange(value, value));

    public static LootTableDefinition Loot(string id = "loot", int rolls = 1, int none = 0, int gold = 0, int item = 1, int magicPercent = 0) =>
        new LootTableDefinition(id, rolls, none, gold, item, new IntRange(5, 5), magicPercent);

    public static ItemCatalog Items(
        IReadOnlyList<ItemBaseDefinition>? bases = null,
        IReadOnlyList<AffixDefinition>? affixes = null,
        IReadOnlyList<LootTableDefinition>? loot = null) =>
        new ItemCatalog(
            bases ?? Array.Empty<ItemBaseDefinition>(),
            affixes ?? Array.Empty<AffixDefinition>(),
            loot ?? Array.Empty<LootTableDefinition>());

    public static AuthoringResult<ContentSet> Create(
        IReadOnlyList<MonsterDefinition>? monsters = null,
        IReadOnlyList<LevelDefinition>? levels = null,
        GameRules? rules = null,
        IReadOnlyList<ClassDefinition>? classes = null,
        ItemCatalog? items = null) =>
        ContentSet.Create(
            rules ?? Rules(),
            classes ?? new[] { Warrior() },
            monsters ?? new[] { Monster("zombie") },
            levels ?? new[] { Level(1), Level(2) },
            items ?? ItemCatalog.Empty);

    public static ContentSet Valid(
        IReadOnlyList<MonsterDefinition>? monsters = null,
        IReadOnlyList<LevelDefinition>? levels = null,
        GameRules? rules = null,
        ItemCatalog? items = null,
        IReadOnlyList<ClassDefinition>? classes = null)
    {
        var result = Create(monsters, levels, rules, classes, items);
        return result.Value ?? throw new InvalidOperationException(string.Join("; ", result.Errors));
    }
}
```

In `tests/Mortar.Tests/Gameplay/ContentValidatorTests.cs`, add the new trailing constructor argument to the four direct constructions:

- both `new MonsterDefinition(...)` calls end with `AiKinds.MeleeChase, null, null);` (was `AiKinds.MeleeChase, null);`)
- both `new ClassDefinition(...)` calls end with `warrior.Timings, Array.Empty<string>());` (was `warrior.Timings);`)

- [ ] **Step 2: Write the failing tests**

`tests/Mortar.Tests/Gameplay/ItemContentTests.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Game.Content;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class ItemContentTests
{
    private static readonly AffixDefinition Keen =
        TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon);

    [Fact]
    public void ValidItems_AreAvailableById()
    {
        var items = TestGameContent.Items(
            new[] { TestGameContent.Weapon(), TestGameContent.Potion() }, new[] { Keen }, new[] { TestGameContent.Loot() });
        var content = TestGameContent.Valid(items: items);
        Assert.Equal("knife", content.ItemBase("knife").Id);
        Assert.True(content.TryGetAffix("keen", out _));
        Assert.Equal(1, content.LootTable("loot").Rolls);
        Assert.Equal(2, content.ItemBases.Count);
    }

    [Fact]
    public void WeaponWithoutDamage_IsRejected()
    {
        var club = new ItemBaseDefinition("club", "club", "club", ItemSlots.Weapon, 1, 2, 1, 1, default, null, null, null, 0, null);
        AssertError(Create(bases: club), "item 'club'", "weapons need damage");
    }

    [Fact]
    public void ArmorOnARing_IsRejected()
    {
        var ring = new ItemBaseDefinition("ring", "ring", "ring", ItemSlots.Ring, 1, 1, 1, 1, default, null, null, new IntRange(1, 2), 0, null);
        AssertError(Create(bases: ring), "item 'ring'", "only body, head, and shield items have armor");
    }

    [Fact]
    public void LargePotion_IsRejected()
    {
        var jug = new ItemBaseDefinition("jug", "jug", "jug", ItemSlots.Potion, 1, 2, 1, 1, default, null, null, null, 0, new PotionEffect(PotionKinds.Life, 5));
        AssertError(Create(bases: jug), "item 'jug'", "rings, amulets, and potions must be 1x1");
    }

    [Fact]
    public void UnknownSlot_IsRejected()
    {
        var sash = new ItemBaseDefinition("sash", "sash", "sash", "belt", 1, 1, 1, 1, default, null, null, null, 0, null);
        AssertError(Create(bases: sash), "item 'sash'", "unknown slot 'belt'");
    }

    [Fact]
    public void SwingWithHitFrameAtTheEnd_IsRejected()
    {
        AssertError(Create(bases: TestGameContent.Weapon(swing: new WeaponSwing(8, 8))), "item 'knife'", "swing needs 1 <= hitFrame < attackTicks");
    }

    [Fact]
    public void AffixForPotions_IsRejected()
    {
        var odd = TestGameContent.Affix("odd", AffixPositions.Suffix, BonusKinds.Life, 3, ItemSlots.Potion);
        AssertError(Create(affix: odd), "affix 'odd'", "slots must list one or more equippable slots");
    }

    [Fact]
    public void LootTableWithNoWeight_IsRejected()
    {
        AssertError(Create(loot: TestGameContent.Loot(item: 0)), "loot table 'loot'", "must add up to at least 1");
    }

    [Fact]
    public void GoldWeightWithoutAGoldRange_IsRejected()
    {
        var table = new LootTableDefinition("loot", 1, 0, 1, 0, new IntRange(0, 0), 0);
        AssertError(Create(loot: table), "loot table 'loot'", "gold must satisfy 1 <= min <= max");
    }

    [Fact]
    public void UnknownStartingItem_IsRejected()
    {
        var result = TestGameContent.Create(classes: new[] { TestGameContent.Warrior("sword") });
        AssertError(result, "class 'warrior'", "starting item references unknown item 'sword'");
    }

    [Fact]
    public void UnknownLootTable_IsRejected()
    {
        var result = TestGameContent.Create(monsters: new[] { TestGameContent.Monster("zombie", loot: "rare") });
        AssertError(result, "monster 'zombie'", "loot references unknown loot table 'rare'");
    }

    [Fact]
    public void DuplicateItemIds_AreRejected()
    {
        var items = TestGameContent.Items(new[] { TestGameContent.Weapon(), TestGameContent.Weapon() });
        AssertError(TestGameContent.Create(items: items), "item 'knife'", "duplicate id");
    }

    [Fact]
    public void Content_KeepsItsOwnCopiesOfLists()
    {
        var thresholds = new long[] { 100, 300, 600 };
        var spawns = new List<SpawnEntry> { new SpawnEntry("zombie", 1) };
        var bases = new List<ItemBaseDefinition> { TestGameContent.Weapon() };
        var slots = new List<string> { ItemSlots.Weapon };
        var kit = new List<string> { "knife" };
        var affix = new AffixDefinition("keen", "Keen", AffixPositions.Prefix, slots, new IntRange(1, 5), 1, BonusKinds.ToHit, new IntRange(1, 2));
        var warrior = TestGameContent.Warrior();
        var classes = new[]
        {
            new ClassDefinition(
                warrior.Id, warrior.Name, warrior.SpriteSet, warrior.BaseStats, warrior.StatCaps,
                warrior.Growth, warrior.UnarmedDamage, warrior.Timings, kit),
        };
        var content = TestGameContent.Valid(
            levels: new[] { new LevelDefinition(1, "test", spawns, null) },
            rules: TestGameContent.Rules(thresholds: thresholds),
            items: TestGameContent.Items(bases, new[] { affix }),
            classes: classes);
        thresholds[0] = 999;
        spawns.Clear();
        bases.Clear();
        slots.Clear();
        kit.Clear();
        Assert.Equal(100, content.Rules.ExperienceThresholds[0]);
        Assert.Single(content.Level(1).Spawns);
        Assert.Single(content.ItemBases);
        Assert.Single(content.Affixes[0].Slots);
        Assert.Single(content.Classes[0].StartingItems);
    }

    private static AuthoringResult<ContentSet> Create(
        ItemBaseDefinition? bases = null, AffixDefinition? affix = null, LootTableDefinition? loot = null) =>
        TestGameContent.Create(items: TestGameContent.Items(
            bases is null ? null : new[] { bases },
            affix is null ? null : new[] { affix },
            loot is null ? null : new[] { loot }));

    private static void AssertError(AuthoringResult<ContentSet> result, string source, string fragment)
    {
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Source == source && e.Message.Contains(fragment));
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~ItemContentTests"`
Expected: build FAILS (`ItemSlots`, `ItemBaseDefinition`, `ItemCatalog` and the new constructor parameters do not exist).

- [ ] **Step 4: Add the slot, bonus, and definition types**

`src/Mortar.Game/Content/ItemSlots.cs`:

```csharp
namespace Mortar.Game.Content;

public static class ItemSlots
{
    public const string Weapon = "weapon";
    public const string Body = "body";
    public const string Head = "head";
    public const string Shield = "shield";
    public const string Ring = "ring";
    public const string Amulet = "amulet";
    public const string Potion = "potion";

    public static readonly IReadOnlyList<string> All = new[] { Weapon, Body, Head, Shield, Ring, Amulet, Potion };

    public static readonly IReadOnlyList<string> Equippable = new[] { Weapon, Body, Head, Shield, Ring, Amulet };

    public static bool IsKnown(string slot) => All.Contains(slot);

    public static bool IsEquippable(string slot) => Equippable.Contains(slot);
}
```

`src/Mortar.Game/Content/BonusKinds.cs`:

```csharp
namespace Mortar.Game.Content;

public static class BonusKinds
{
    public const string Strength = "strength";
    public const string Dexterity = "dexterity";
    public const string Vitality = "vitality";
    public const string Magic = "magic";
    public const string Life = "life";
    public const string Mana = "mana";
    public const string ArmorClass = "armorClass";
    public const string ToHit = "toHit";
    public const string Damage = "damage";

    public static readonly IReadOnlyList<string> All =
        new[] { Strength, Dexterity, Vitality, Magic, Life, Mana, ArmorClass, ToHit, Damage };

    public static bool IsKnown(string kind) => All.Contains(kind);
}
```

`src/Mortar.Game/Content/ItemBaseDefinition.cs`:

```csharp
namespace Mortar.Game.Content;

public sealed class WeaponSwing
{
    public WeaponSwing(int attackTicks, int hitFrame)
    {
        AttackTicks = attackTicks;
        HitFrame = hitFrame;
    }

    public int AttackTicks { get; }

    public int HitFrame { get; }
}

public static class PotionKinds
{
    public const string Life = "life";
    public const string Mana = "mana";

    public static bool IsKnown(string kind) => kind == Life || kind == Mana;
}

public sealed class PotionEffect
{
    public PotionEffect(string restores, int amount)
    {
        Restores = restores;
        Amount = amount;
    }

    public string Restores { get; }

    public int Amount { get; }
}

public sealed class ItemBaseDefinition
{
    public ItemBaseDefinition(
        string id,
        string name,
        string iconId,
        string slot,
        int width,
        int height,
        int itemLevel,
        int dropWeight,
        Stats requirements,
        IntRange? damage,
        WeaponSwing? swing,
        IntRange? armor,
        int blockChance,
        PotionEffect? potion)
    {
        Id = id;
        Name = name;
        IconId = iconId;
        Slot = slot;
        Width = width;
        Height = height;
        ItemLevel = itemLevel;
        DropWeight = dropWeight;
        Requirements = requirements;
        Damage = damage;
        Swing = swing;
        Armor = armor;
        BlockChance = blockChance;
        Potion = potion;
    }

    public string Id { get; }

    public string Name { get; }

    public string IconId { get; }

    public string Slot { get; }

    public int Width { get; }

    public int Height { get; }

    public int ItemLevel { get; }

    public int DropWeight { get; }

    public Stats Requirements { get; }

    public IntRange? Damage { get; }

    public WeaponSwing? Swing { get; }

    public IntRange? Armor { get; }

    public int BlockChance { get; }

    public PotionEffect? Potion { get; }

    public bool IsEquippable => ItemSlots.IsEquippable(Slot);

    public bool IsPotion => Slot == ItemSlots.Potion;
}
```

`src/Mortar.Game/Content/AffixDefinition.cs`:

```csharp
namespace Mortar.Game.Content;

public static class AffixPositions
{
    public const string Prefix = "prefix";
    public const string Suffix = "suffix";

    public static bool IsKnown(string position) => position == Prefix || position == Suffix;
}

public sealed class AffixDefinition
{
    public AffixDefinition(
        string id,
        string name,
        string position,
        IReadOnlyList<string> slots,
        IntRange itemLevel,
        int weight,
        string bonus,
        IntRange value)
    {
        Id = id;
        Name = name;
        Position = position;
        Slots = slots.ToArray();
        ItemLevel = itemLevel;
        Weight = weight;
        Bonus = bonus;
        Value = value;
    }

    public string Id { get; }

    public string Name { get; }

    public string Position { get; }

    public IReadOnlyList<string> Slots { get; }

    public IntRange ItemLevel { get; }

    public int Weight { get; }

    public string Bonus { get; }

    public IntRange Value { get; }

    public bool Allows(string slot, int itemLevel) =>
        Slots.Contains(slot) && itemLevel >= ItemLevel.Min && itemLevel <= ItemLevel.Max;
}
```

`src/Mortar.Game/Content/LootTableDefinition.cs`:

```csharp
namespace Mortar.Game.Content;

public sealed class LootTableDefinition
{
    public LootTableDefinition(string id, int rolls, int noneWeight, int goldWeight, int itemWeight, IntRange gold, int magicPercent)
    {
        Id = id;
        Rolls = rolls;
        NoneWeight = noneWeight;
        GoldWeight = goldWeight;
        ItemWeight = itemWeight;
        Gold = gold;
        MagicPercent = magicPercent;
    }

    public string Id { get; }

    public int Rolls { get; }

    public int NoneWeight { get; }

    public int GoldWeight { get; }

    public int ItemWeight { get; }

    public IntRange Gold { get; }

    public int MagicPercent { get; }
}
```

`src/Mortar.Game/Content/ItemCatalog.cs`:

```csharp
namespace Mortar.Game.Content;

public sealed class ItemCatalog
{
    public ItemCatalog(
        IReadOnlyList<ItemBaseDefinition> bases,
        IReadOnlyList<AffixDefinition> affixes,
        IReadOnlyList<LootTableDefinition> lootTables)
    {
        Bases = bases.ToArray();
        Affixes = affixes.ToArray();
        LootTables = lootTables.ToArray();
    }

    public static ItemCatalog Empty { get; } = new ItemCatalog(
        Array.Empty<ItemBaseDefinition>(), Array.Empty<AffixDefinition>(), Array.Empty<LootTableDefinition>());

    public IReadOnlyList<ItemBaseDefinition> Bases { get; }

    public IReadOnlyList<AffixDefinition> Affixes { get; }

    public IReadOnlyList<LootTableDefinition> LootTables { get; }
}
```

- [ ] **Step 5: Extend classes and monsters**

In `src/Mortar.Game/Content/ClassDefinition.cs`, add a final constructor parameter `IReadOnlyList<string> startingItems`, assign it, and add the property:

```csharp
    public ClassDefinition(
        string id,
        string name,
        string spriteSet,
        Stats baseStats,
        Stats statCaps,
        ClassGrowth growth,
        IntRange unarmedDamage,
        ActionTimings timings,
        IReadOnlyList<string> startingItems)
    {
        Id = id;
        Name = name;
        SpriteSet = spriteSet;
        BaseStats = baseStats;
        StatCaps = statCaps;
        Growth = growth;
        UnarmedDamage = unarmedDamage;
        Timings = timings;
        StartingItems = startingItems.ToArray();
    }

    // ... existing properties ...

    public IReadOnlyList<string> StartingItems { get; }
```

In `src/Mortar.Game/Content/MonsterDefinition.cs`, add a final constructor parameter `string? lootTableId` after `MinionSpec? minions`, assign `LootTableId = lootTableId;`, and add:

```csharp
    public string? LootTableId { get; }
```

- [ ] **Step 6: Add item validation**

`src/Mortar.Game/Content/ItemChecks.cs`:

```csharp
using Mortar.Core.Authoring;

namespace Mortar.Game.Content;

internal static class ItemChecks
{
    public const int MaxItemWidth = 2;
    public const int MaxItemHeight = 3;
    public const int MaxLootRolls = 10;

    private const int MaxValue = DefinitionChecks.MaxValue;

    public static void Validate(
        ItemCatalog items,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        List<AuthoringError> errors)
    {
        ContentValidator.CheckUniqueIds(items.Bases.Select(b => b.Id), "item", errors);
        ContentValidator.CheckUniqueIds(items.Affixes.Select(a => a.Id), "affix", errors);
        ContentValidator.CheckUniqueIds(items.LootTables.Select(t => t.Id), "loot table", errors);
        foreach (var item in items.Bases) CheckBase(item, errors);
        foreach (var affix in items.Affixes) CheckAffix(affix, errors);
        foreach (var table in items.LootTables) CheckLootTable(table, errors);
        CheckWeightTotal(items.Bases.Select(b => (long)Math.Max(0, b.DropWeight)), "items", "dropWeight", errors);
        CheckWeightTotal(items.Affixes.Select(a => (long)Math.Max(0, a.Weight)), "affixes", "weight", errors);
        CheckReferences(items, classes, monsters, errors);
    }

    private static void CheckBase(ItemBaseDefinition item, List<AuthoringError> errors)
    {
        var source = $"item '{item.Id}'";
        if (string.IsNullOrWhiteSpace(item.Name)) errors.Add(source, "name is required");
        if (string.IsNullOrWhiteSpace(item.IconId)) errors.Add(source, "icon is required");
        if (!ItemSlots.IsKnown(item.Slot))
        {
            errors.Add(source, $"unknown slot '{item.Slot}'");
            return;
        }
        CheckSize(source, item, errors);
        if (item.ItemLevel < 1 || item.ItemLevel > MaxValue) errors.Add(source, $"itemLevel must be between 1 and {MaxValue}");
        if (item.DropWeight < 0 || item.DropWeight > MaxValue) errors.Add(source, $"dropWeight must be between 0 and {MaxValue}");
        foreach (var kind in Stats.Kinds)
        {
            var value = item.Requirements.Get(kind);
            if (value < 0 || value > MaxValue)
            {
                errors.Add(source, $"{kind.ToString().ToLowerInvariant()} requirement must be between 0 and {MaxValue}");
            }
        }
        CheckWeapon(source, item, errors);
        CheckArmor(source, item, errors);
        CheckPotion(source, item, errors);
    }

    private static void CheckSize(string source, ItemBaseDefinition item, List<AuthoringError> errors)
    {
        if (item.Width < 1 || item.Width > MaxItemWidth || item.Height < 1 || item.Height > MaxItemHeight)
        {
            errors.Add(source, $"size must be 1-{MaxItemWidth} wide and 1-{MaxItemHeight} tall");
        }
        var small = item.Slot == ItemSlots.Ring || item.Slot == ItemSlots.Amulet || item.Slot == ItemSlots.Potion;
        if (small && (item.Width != 1 || item.Height != 1)) errors.Add(source, "rings, amulets, and potions must be 1x1");
    }

    private static void CheckWeapon(string source, ItemBaseDefinition item, List<AuthoringError> errors)
    {
        var isWeapon = item.Slot == ItemSlots.Weapon;
        if (isWeapon && item.Damage is null) errors.Add(source, "weapons need damage");
        if (!isWeapon && (item.Damage is not null || item.Swing is not null)) errors.Add(source, "only weapons have damage or swing");
        if (item.Damage is { } damage) DefinitionChecks.CheckRange(source, "damage", damage, 1, errors);
        if (item.Swing is { } swing && (swing.HitFrame < 1 || swing.HitFrame >= swing.AttackTicks || swing.AttackTicks > MaxValue))
        {
            errors.Add(source, $"swing needs 1 <= hitFrame < attackTicks <= {MaxValue}");
        }
    }

    private static void CheckArmor(string source, ItemBaseDefinition item, List<AuthoringError> errors)
    {
        var armored = item.Slot == ItemSlots.Body || item.Slot == ItemSlots.Head || item.Slot == ItemSlots.Shield;
        if (armored && item.Armor is null) errors.Add(source, "body, head, and shield items need armor");
        if (!armored && item.Armor is not null) errors.Add(source, "only body, head, and shield items have armor");
        if (item.Armor is { } armor) DefinitionChecks.CheckRange(source, "armor", armor, 0, errors);
        if (item.BlockChance < 0 || item.BlockChance > 100) errors.Add(source, "blockChance must be between 0 and 100");
        if (item.BlockChance != 0 && item.Slot != ItemSlots.Shield) errors.Add(source, "only shields have blockChance");
    }

    private static void CheckPotion(string source, ItemBaseDefinition item, List<AuthoringError> errors)
    {
        if (item.Slot != ItemSlots.Potion)
        {
            if (item.Potion is not null) errors.Add(source, "only potions have an effect");
            return;
        }
        if (item.Potion is null)
        {
            errors.Add(source, "potions need an effect");
            return;
        }
        if (!PotionKinds.IsKnown(item.Potion.Restores)) errors.Add(source, $"unknown potion effect '{item.Potion.Restores}'");
        if (item.Potion.Amount < 1 || item.Potion.Amount > MaxValue) errors.Add(source, $"potion amount must be between 1 and {MaxValue}");
    }

    private static void CheckAffix(AffixDefinition affix, List<AuthoringError> errors)
    {
        var source = $"affix '{affix.Id}'";
        if (string.IsNullOrWhiteSpace(affix.Name)) errors.Add(source, "name is required");
        if (!AffixPositions.IsKnown(affix.Position)) errors.Add(source, $"unknown position '{affix.Position}'");
        if (affix.Slots.Count == 0 || affix.Slots.Any(slot => !ItemSlots.IsEquippable(slot)))
        {
            errors.Add(source, "slots must list one or more equippable slots");
        }
        DefinitionChecks.CheckRange(source, "itemLevel", affix.ItemLevel, 1, errors);
        if (affix.Weight < 1 || affix.Weight > MaxValue) errors.Add(source, $"weight must be between 1 and {MaxValue}");
        if (!BonusKinds.IsKnown(affix.Bonus)) errors.Add(source, $"unknown bonus '{affix.Bonus}'");
        DefinitionChecks.CheckRange(source, "value", affix.Value, 1, errors);
    }

    private static void CheckLootTable(LootTableDefinition table, List<AuthoringError> errors)
    {
        var source = $"loot table '{table.Id}'";
        if (table.Rolls < 1 || table.Rolls > MaxLootRolls) errors.Add(source, $"rolls must be between 1 and {MaxLootRolls}");
        var weights = new[] { table.NoneWeight, table.GoldWeight, table.ItemWeight };
        if (weights.Any(weight => weight < 0 || weight > MaxValue)) errors.Add(source, $"weights must be between 0 and {MaxValue}");
        else if (weights.Sum() < 1) errors.Add(source, "noneWeight, goldWeight, and itemWeight must add up to at least 1");
        if (table.GoldWeight > 0) DefinitionChecks.CheckRange(source, "gold", table.Gold, 1, errors);
        if (table.MagicPercent < 0 || table.MagicPercent > 100) errors.Add(source, "magicPercent must be between 0 and 100");
    }

    private static void CheckWeightTotal(IEnumerable<long> weights, string source, string field, List<AuthoringError> errors)
    {
        if (weights.Sum() > int.MaxValue) errors.Add(source, $"{field} values add up to more than {int.MaxValue}");
    }

    private static void CheckReferences(
        ItemCatalog items,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        List<AuthoringError> errors)
    {
        var bases = new HashSet<string>(items.Bases.Select(b => b.Id), StringComparer.Ordinal);
        var tables = new HashSet<string>(items.LootTables.Select(t => t.Id), StringComparer.Ordinal);
        foreach (var definition in classes)
        {
            foreach (var id in definition.StartingItems)
            {
                if (!bases.Contains(id)) errors.Add($"class '{definition.Id}'", $"starting item references unknown item '{id}'");
            }
        }
        foreach (var monster in monsters)
        {
            if (monster.LootTableId is not null && !tables.Contains(monster.LootTableId))
            {
                errors.Add($"monster '{monster.Id}'", $"loot references unknown loot table '{monster.LootTableId}'");
            }
        }
    }
}
```

In `src/Mortar.Game/Content/DefinitionChecks.cs`, change `private static void CheckRange(` to `public static void CheckRange(`.

In `src/Mortar.Game/Content/ContentValidator.cs`:
- add a final parameter `ItemCatalog items` to `Validate`, and call `ItemChecks.Validate(items, classes, monsters, errors);` right after `CheckLevels(levels, Find, errors);`;
- change `private static void CheckUniqueIds(` to `public static void CheckUniqueIds(`.

- [ ] **Step 7: Extend ContentSet**

Replace `src/Mortar.Game/Content/ContentSet.cs` with:

```csharp
using System.Diagnostics.CodeAnalysis;
using Mortar.Core.Authoring;

namespace Mortar.Game.Content;

public sealed class ContentSet
{
    private readonly Dictionary<string, ClassDefinition> _classes;
    private readonly Dictionary<string, MonsterDefinition> _monsters;
    private readonly Dictionary<string, ItemBaseDefinition> _itemBases;
    private readonly Dictionary<string, AffixDefinition> _affixes;
    private readonly Dictionary<string, LootTableDefinition> _lootTables;

    private ContentSet(
        GameRules rules,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        IReadOnlyList<LevelDefinition> levels,
        ItemCatalog items)
    {
        Rules = rules;
        Classes = classes.ToArray();
        Monsters = monsters.ToArray();
        Levels = levels;
        Items = items;
        _classes = classes.ToDictionary(c => c.Id, StringComparer.Ordinal);
        _monsters = monsters.ToDictionary(m => m.Id, StringComparer.Ordinal);
        _itemBases = items.Bases.ToDictionary(b => b.Id, StringComparer.Ordinal);
        _affixes = items.Affixes.ToDictionary(a => a.Id, StringComparer.Ordinal);
        _lootTables = items.LootTables.ToDictionary(t => t.Id, StringComparer.Ordinal);
    }

    public GameRules Rules { get; }

    public IReadOnlyList<ClassDefinition> Classes { get; }

    public IReadOnlyList<MonsterDefinition> Monsters { get; }

    public IReadOnlyList<LevelDefinition> Levels { get; }

    public ItemCatalog Items { get; }

    public IReadOnlyList<ItemBaseDefinition> ItemBases => Items.Bases;

    public IReadOnlyList<AffixDefinition> Affixes => Items.Affixes;

    public int DeepestLevel => Levels.Count;

    public static AuthoringResult<ContentSet> Create(
        GameRules rules,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        IReadOnlyList<LevelDefinition> levels,
        ItemCatalog items)
    {
        var errors = ContentValidator.Validate(rules, classes, monsters, levels, items);
        if (errors.Count > 0)
        {
            return AuthoringResult<ContentSet>.Failure(errors);
        }
        var ordered = levels.OrderBy(level => level.Depth).ToList();
        return AuthoringResult<ContentSet>.Success(new ContentSet(rules, classes, monsters, ordered, items));
    }

    public bool TryGetClass(string id, [NotNullWhen(true)] out ClassDefinition? definition) =>
        _classes.TryGetValue(id, out definition);

    public MonsterDefinition Monster(string id) => _monsters[id];

    public bool TryGetMonster(string id, [NotNullWhen(true)] out MonsterDefinition? definition) =>
        _monsters.TryGetValue(id, out definition);

    public LevelDefinition Level(int depth) => Levels[depth - 1];

    public ItemBaseDefinition ItemBase(string id) => _itemBases[id];

    public bool TryGetItemBase(string id, [NotNullWhen(true)] out ItemBaseDefinition? definition) =>
        _itemBases.TryGetValue(id, out definition);

    public bool TryGetAffix(string id, [NotNullWhen(true)] out AffixDefinition? definition) =>
        _affixes.TryGetValue(id, out definition);

    public LootTableDefinition LootTable(string id) => _lootTables[id];
}
```

Content keeps its own copies of every list it is given, so a caller changing its list afterwards cannot change validated content. Besides the `.ToArray()` copies above (classes, monsters, item catalog, affix slots, starting items), make two one-line changes:
- `LevelDefinition` constructor: `Spawns = spawns.ToArray();`
- `GameRules` constructor: `ExperienceThresholds = experienceThresholds.ToArray();`

- [ ] **Step 8: Keep the content loader compiling**

In `src/Mortar.Json/Content/ContentMapper.cs`, pass `Array.Empty<string>()` as the last argument of `new ClassDefinition(...)` and `null` as the last argument of `new MonsterDefinition(...)`. In `src/Mortar.Json/Content/GameContentLoader.cs`, change the create call to `ContentSet.Create(rules, classes, monsters, levels, ItemCatalog.Empty);`. Task 7 replaces all three with real values.

- [ ] **Step 9: Run the tests to verify they pass**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~ItemContentTests|FullyQualifiedName~ContentValidatorTests"`
Expected: PASS. Then run `dotnet test`; expected: all tests pass (the determinism snapshot is unchanged because nothing uses items yet).

- [ ] **Step 10: Commit**

```bash
git add src/Mortar.Game/Content tests/Mortar.Tests/Gameplay/ItemContentTests.cs tests/Mortar.Tests/Gameplay/ContentValidatorTests.cs tests/Mortar.Tests/Gameplay/Support/TestGameContent.cs src/Mortar.Json/Content/ContentMapper.cs src/Mortar.Json/Content/GameContentLoader.cs
git commit -m "add item, affix, and loot table definitions"
```

---

### Task 2: Items, inventory, belt, and equipment

**Files:**
- Create: `src/Mortar.Game/Items/Item.cs`, `Inventory.cs`, `Belt.cs`, `Equipment.cs`, `Belongings.cs`
- Create: `tests/Mortar.Tests/Gameplay/Support/TestItems.cs`
- Test: `tests/Mortar.Tests/Gameplay/ItemContainerTests.cs`

**Interfaces:**
- Consumes: `ItemBaseDefinition`, `AffixDefinition`, `ItemSlots`, `Cell`.
- Produces (namespace `Mortar.Game.Items`):
  - `ItemQuality { Normal, Magic }`; `RolledAffix` (internal ctor `(AffixDefinition definition, int value)`; `Definition`, `Value`).
  - `Item` (internal ctor `(int id, ItemBaseDefinition definition, int itemLevel, int armorValue, RolledAffix? prefix, RolledAffix? suffix)`; `Id`, `Definition`, `ItemLevel`, `ArmorValue`, `Prefix`, `Suffix`, `Quality`, `Name`).
  - `StoredItem` (`Item`, `Cell` = top-left); `Inventory` (`Width = 10`, `Height = 4`, `Items` ascending by id, `ItemAt(Cell)`, `Find(int itemId)`, `Fits(ItemBaseDefinition, Cell topLeft, int ignoredItemId = 0)`, `FindSpace(ItemBaseDefinition)`, internal `TryPlace(Item, Cell)`, `TryAdd(Item)`, `Remove(int itemId)`).
  - `Belt` (`Slots = 8`, `Get(int slot)` null when empty or out of range, `IndexOf(int itemId)` −1 when absent, `FirstEmpty()` −1 when full, internal `TryPut(Item, int slot)`, `Take(int slot)`).
  - `EquipmentSlot { Head, Body, Weapon, Shield, LeftRing, RightRing, Amulet }`; `Equipment` (`Slots` in enum order, `Get(slot)`, `SlotOf(int itemId)`, `Worn`, `SlotFor(ItemBaseDefinition)`, static `Accepts(EquipmentSlot, ItemBaseDefinition)`, internal `Set(slot, Item?)`).
  - `Belongings` (`Inventory`, `Belt`, `Equipment`, `Gold` with internal setter, `Find(int itemId)`, internal `Stow(Item)`, internal `Take(int itemId, out EquipmentSlot? fromSlot)`).
  - Test helper `TestItems.Create(ItemBaseDefinition definition, int id = 1, int armor = 0, RolledAffix? prefix = null, RolledAffix? suffix = null)` and `TestItems.Roll(AffixDefinition affix, int value)`.

- [ ] **Step 1: Write the test helper and failing tests**

`tests/Mortar.Tests/Gameplay/Support/TestItems.cs`:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Items;

namespace Mortar.Tests.Gameplay.Support;

internal static class TestItems
{
    public static Item Create(
        ItemBaseDefinition definition, int id = 1, int armor = 0, RolledAffix? prefix = null, RolledAffix? suffix = null) =>
        new Item(id, definition, definition.ItemLevel, armor, prefix, suffix);

    public static RolledAffix Roll(AffixDefinition affix, int value) => new RolledAffix(affix, value);
}
```

`tests/Mortar.Tests/Gameplay/ItemContainerTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Content;
using Mortar.Game.Items;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class ItemContainerTests
{
    private static readonly ItemBaseDefinition Vest = TestGameContent.Armor();
    private static readonly ItemBaseDefinition Knife = TestGameContent.Weapon();
    private static readonly ItemBaseDefinition Tonic = TestGameContent.Potion();

    [Fact]
    public void Inventory_FillsEachColumnTopToBottomFirst()
    {
        var inventory = new Inventory();
        Assert.True(inventory.TryAdd(TestItems.Create(Vest, 1)));
        Assert.True(inventory.TryAdd(TestItems.Create(Vest, 2)));
        Assert.True(inventory.TryAdd(TestItems.Create(Vest, 3)));
        Assert.Equal(new Cell(0, 0), inventory.Find(1)!.Cell);
        Assert.Equal(new Cell(0, 2), inventory.Find(2)!.Cell);
        Assert.Equal(new Cell(2, 0), inventory.Find(3)!.Cell);
        Assert.Equal(3, inventory.ItemAt(new Cell(2, 1)));
    }

    [Fact]
    public void Inventory_RejectsOverlapsAndCellsOutsideTheGrid()
    {
        var inventory = new Inventory();
        Assert.True(inventory.TryPlace(TestItems.Create(Vest, 1), new Cell(0, 0)));
        Assert.False(inventory.TryPlace(TestItems.Create(Vest, 2), new Cell(1, 1)));
        Assert.False(inventory.TryPlace(TestItems.Create(Vest, 3), new Cell(9, 0)));
        Assert.False(inventory.TryPlace(TestItems.Create(Knife, 4), new Cell(5, 3)));
        Assert.True(inventory.Fits(Vest, new Cell(1, 1), ignoredItemId: 1));
    }

    [Fact]
    public void Inventory_RemoveFreesTheCells()
    {
        var inventory = new Inventory();
        inventory.TryPlace(TestItems.Create(Vest, 1), new Cell(0, 0));
        Assert.True(inventory.Remove(1));
        Assert.Equal(0, inventory.ItemAt(new Cell(1, 1)));
        Assert.Empty(inventory.Items);
        Assert.False(inventory.Remove(1));
    }

    [Fact]
    public void Inventory_ListsItemsInIdOrder()
    {
        var inventory = new Inventory();
        inventory.TryAdd(TestItems.Create(Knife, 5));
        inventory.TryAdd(TestItems.Create(Knife, 2));
        Assert.Equal(new[] { 2, 5 }, inventory.Items.Select(stored => stored.Item.Id));
    }

    [Fact]
    public void Inventory_WhenFull_HasNoSpace()
    {
        var inventory = new Inventory();
        for (var id = 1; id <= 10; id++) Assert.True(inventory.TryAdd(TestItems.Create(Vest, id)));
        Assert.Null(inventory.FindSpace(Vest));
        Assert.False(inventory.TryAdd(TestItems.Create(Vest, 11)));
    }

    [Fact]
    public void Belt_HoldsOnlyPotions()
    {
        var belt = new Belt();
        Assert.False(belt.TryPut(TestItems.Create(Knife, 1), 0));
        Assert.True(belt.TryPut(TestItems.Create(Tonic, 2), 0));
        Assert.False(belt.TryPut(TestItems.Create(Tonic, 3), 0));
        Assert.False(belt.TryPut(TestItems.Create(Tonic, 4), Belt.Slots));
        Assert.Equal(1, belt.FirstEmpty());
        Assert.Equal(0, belt.IndexOf(2));
        Assert.Equal(2, belt.Take(0)!.Id);
        Assert.Null(belt.Get(0));
    }

    [Fact]
    public void Equipment_RingsFillLeftThenRightThenReplaceLeft()
    {
        var equipment = new Equipment();
        var ring = TestGameContent.Ring();
        Assert.Equal(EquipmentSlot.LeftRing, equipment.SlotFor(ring));
        equipment.Set(EquipmentSlot.LeftRing, TestItems.Create(ring, 1));
        Assert.Equal(EquipmentSlot.RightRing, equipment.SlotFor(ring));
        equipment.Set(EquipmentSlot.RightRing, TestItems.Create(ring, 2));
        Assert.Equal(EquipmentSlot.LeftRing, equipment.SlotFor(ring));
        Assert.Equal(EquipmentSlot.RightRing, equipment.SlotOf(2));
        Assert.True(Equipment.Accepts(EquipmentSlot.RightRing, ring));
        Assert.False(Equipment.Accepts(EquipmentSlot.Weapon, ring));
    }

    [Fact]
    public void Item_NameJoinsAffixesAroundTheBase()
    {
        var keen = TestGameContent.Affix("Keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon);
        var brawn = TestGameContent.Affix("of Brawn", AffixPositions.Suffix, BonusKinds.Strength, 2, ItemSlots.Weapon);
        var magic = TestItems.Create(Knife, prefix: TestItems.Roll(keen, 3), suffix: TestItems.Roll(brawn, 2));
        Assert.Equal("Keen knife of Brawn", magic.Name);
        Assert.Equal(ItemQuality.Magic, magic.Quality);
        Assert.Equal(ItemQuality.Normal, TestItems.Create(Knife).Quality);
    }

    [Fact]
    public void Belongings_StowPutsPotionsInTheBeltFirst()
    {
        var belongings = new Belongings();
        for (var id = 1; id <= Belt.Slots; id++) Assert.True(belongings.Stow(TestItems.Create(Tonic, id)));
        Assert.Equal(-1, belongings.Belt.FirstEmpty());
        Assert.True(belongings.Stow(TestItems.Create(Tonic, 9)));
        Assert.NotNull(belongings.Inventory.Find(9));
    }

    [Fact]
    public void Belongings_TakeFindsItemsWhereverTheyAre()
    {
        var belongings = new Belongings();
        belongings.Equipment.Set(EquipmentSlot.Weapon, TestItems.Create(Knife, 1));
        belongings.Stow(TestItems.Create(Tonic, 2));
        belongings.Stow(TestItems.Create(Vest, 3));
        Assert.Equal(1, belongings.Find(1)!.Id);
        Assert.Equal(1, belongings.Take(1, out var slot)!.Id);
        Assert.Equal(EquipmentSlot.Weapon, slot);
        Assert.Equal(2, belongings.Take(2, out slot)!.Id);
        Assert.Null(slot);
        Assert.Equal(3, belongings.Take(3, out _)!.Id);
        Assert.Null(belongings.Take(3, out _));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~ItemContainerTests"`
Expected: build FAILS (`Mortar.Game.Items` does not exist).

- [ ] **Step 3: Implement the items and containers**

`src/Mortar.Game/Items/Item.cs`:

```csharp
using Mortar.Game.Content;

namespace Mortar.Game.Items;

public enum ItemQuality
{
    Normal,
    Magic,
}

public sealed class RolledAffix
{
    internal RolledAffix(AffixDefinition definition, int value)
    {
        Definition = definition;
        Value = value;
    }

    public AffixDefinition Definition { get; }

    public int Value { get; }
}

public sealed class Item
{
    internal Item(int id, ItemBaseDefinition definition, int itemLevel, int armorValue, RolledAffix? prefix, RolledAffix? suffix)
    {
        Id = id;
        Definition = definition;
        ItemLevel = itemLevel;
        ArmorValue = armorValue;
        Prefix = prefix;
        Suffix = suffix;
    }

    public int Id { get; }

    public ItemBaseDefinition Definition { get; }

    public int ItemLevel { get; }

    public int ArmorValue { get; }

    public RolledAffix? Prefix { get; }

    public RolledAffix? Suffix { get; }

    public ItemQuality Quality => Prefix is null && Suffix is null ? ItemQuality.Normal : ItemQuality.Magic;

    public string Name
    {
        get
        {
            var parts = new List<string>(3);
            if (Prefix is not null) parts.Add(Prefix.Definition.Name);
            parts.Add(Definition.Name);
            if (Suffix is not null) parts.Add(Suffix.Definition.Name);
            return string.Join(" ", parts);
        }
    }
}
```

`src/Mortar.Game/Items/Inventory.cs`:

```csharp
using Mortar.Game.Content;

namespace Mortar.Game.Items;

public sealed class StoredItem
{
    internal StoredItem(Item item, Cell cell)
    {
        Item = item;
        Cell = cell;
    }

    public Item Item { get; }

    public Cell Cell { get; }
}

public sealed class Inventory
{
    public const int Width = 10;
    public const int Height = 4;

    private readonly int[] _cells = new int[Width * Height];
    private readonly List<StoredItem> _items = new List<StoredItem>();

    public IReadOnlyList<StoredItem> Items => _items;

    public int ItemAt(Cell cell) => InGrid(cell) ? _cells[Index(cell)] : 0;

    public StoredItem? Find(int itemId) => _items.Find(stored => stored.Item.Id == itemId);

    public bool Fits(ItemBaseDefinition definition, Cell topLeft, int ignoredItemId = 0)
    {
        for (var dy = 0; dy < definition.Height; dy++)
        {
            for (var dx = 0; dx < definition.Width; dx++)
            {
                var cell = new Cell(topLeft.X + dx, topLeft.Y + dy);
                if (!InGrid(cell)) return false;
                var occupant = _cells[Index(cell)];
                if (occupant != 0 && occupant != ignoredItemId) return false;
            }
        }
        return true;
    }

    public Cell? FindSpace(ItemBaseDefinition definition)
    {
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                var cell = new Cell(x, y);
                if (Fits(definition, cell)) return cell;
            }
        }
        return null;
    }

    internal bool TryPlace(Item item, Cell topLeft)
    {
        if (Find(item.Id) is not null || !Fits(item.Definition, topLeft)) return false;
        Mark(item.Definition, topLeft, item.Id);
        var index = _items.FindIndex(stored => stored.Item.Id > item.Id);
        _items.Insert(index < 0 ? _items.Count : index, new StoredItem(item, topLeft));
        return true;
    }

    internal bool TryAdd(Item item)
    {
        var space = FindSpace(item.Definition);
        return space is not null && TryPlace(item, space.Value);
    }

    internal bool Remove(int itemId)
    {
        var stored = Find(itemId);
        if (stored is null) return false;
        Mark(stored.Item.Definition, stored.Cell, 0);
        _items.Remove(stored);
        return true;
    }

    private void Mark(ItemBaseDefinition definition, Cell topLeft, int value)
    {
        for (var dy = 0; dy < definition.Height; dy++)
        {
            for (var dx = 0; dx < definition.Width; dx++)
            {
                _cells[Index(new Cell(topLeft.X + dx, topLeft.Y + dy))] = value;
            }
        }
    }

    private static bool InGrid(Cell cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

    private static int Index(Cell cell) => cell.Y * Width + cell.X;
}
```

`src/Mortar.Game/Items/Belt.cs`:

```csharp
namespace Mortar.Game.Items;

public sealed class Belt
{
    public const int Slots = 8;

    private readonly Item?[] _slots = new Item?[Slots];

    public Item? Get(int slot) => slot >= 0 && slot < Slots ? _slots[slot] : null;

    public int IndexOf(int itemId) => Array.FindIndex(_slots, item => item is not null && item.Id == itemId);

    public int FirstEmpty() => Array.FindIndex(_slots, item => item is null);

    internal bool TryPut(Item item, int slot)
    {
        if (slot < 0 || slot >= Slots || _slots[slot] is not null || !item.Definition.IsPotion) return false;
        _slots[slot] = item;
        return true;
    }

    internal Item? Take(int slot)
    {
        var item = Get(slot);
        if (item is not null) _slots[slot] = null;
        return item;
    }
}
```

`src/Mortar.Game/Items/Equipment.cs`:

```csharp
using Mortar.Game.Content;

namespace Mortar.Game.Items;

public enum EquipmentSlot
{
    Head,
    Body,
    Weapon,
    Shield,
    LeftRing,
    RightRing,
    Amulet,
}

public sealed class Equipment
{
    private readonly Item?[] _items = new Item?[Slots.Count];

    public static IReadOnlyList<EquipmentSlot> Slots { get; } = new[]
    {
        EquipmentSlot.Head, EquipmentSlot.Body, EquipmentSlot.Weapon, EquipmentSlot.Shield,
        EquipmentSlot.LeftRing, EquipmentSlot.RightRing, EquipmentSlot.Amulet,
    };

    public IEnumerable<Item> Worn => Slots.Select(Get).OfType<Item>();

    public Item? Get(EquipmentSlot slot) => _items[(int)slot];

    public EquipmentSlot? SlotOf(int itemId)
    {
        foreach (var slot in Slots)
        {
            if (_items[(int)slot]?.Id == itemId) return slot;
        }
        return null;
    }

    public EquipmentSlot SlotFor(ItemBaseDefinition definition) => definition.Slot switch
    {
        ItemSlots.Head => EquipmentSlot.Head,
        ItemSlots.Body => EquipmentSlot.Body,
        ItemSlots.Weapon => EquipmentSlot.Weapon,
        ItemSlots.Shield => EquipmentSlot.Shield,
        ItemSlots.Amulet => EquipmentSlot.Amulet,
        ItemSlots.Ring => RingSlot(),
        _ => throw new ArgumentException($"'{definition.Id}' is not equippable", nameof(definition)),
    };

    public static bool Accepts(EquipmentSlot slot, ItemBaseDefinition definition) => definition.Slot == SlotName(slot);

    internal void Set(EquipmentSlot slot, Item? item) => _items[(int)slot] = item;

    private EquipmentSlot RingSlot()
    {
        if (Get(EquipmentSlot.LeftRing) is null) return EquipmentSlot.LeftRing;
        return Get(EquipmentSlot.RightRing) is null ? EquipmentSlot.RightRing : EquipmentSlot.LeftRing;
    }

    private static string SlotName(EquipmentSlot slot) => slot switch
    {
        EquipmentSlot.Head => ItemSlots.Head,
        EquipmentSlot.Body => ItemSlots.Body,
        EquipmentSlot.Weapon => ItemSlots.Weapon,
        EquipmentSlot.Shield => ItemSlots.Shield,
        EquipmentSlot.Amulet => ItemSlots.Amulet,
        _ => ItemSlots.Ring,
    };
}
```

`src/Mortar.Game/Items/Belongings.cs`:

```csharp
namespace Mortar.Game.Items;

public sealed class Belongings
{
    public Inventory Inventory { get; } = new Inventory();

    public Belt Belt { get; } = new Belt();

    public Equipment Equipment { get; } = new Equipment();

    public long Gold { get; internal set; }

    public Item? Find(int itemId)
    {
        var worn = Equipment.SlotOf(itemId);
        return Inventory.Find(itemId)?.Item ?? Belt.Get(Belt.IndexOf(itemId)) ?? (worn is { } slot ? Equipment.Get(slot) : null);
    }

    internal bool Stow(Item item)
    {
        if (item.Definition.IsPotion)
        {
            var slot = Belt.FirstEmpty();
            if (slot >= 0) return Belt.TryPut(item, slot);
        }
        return Inventory.TryAdd(item);
    }

    internal Item? Take(int itemId, out EquipmentSlot? fromSlot)
    {
        fromSlot = Equipment.SlotOf(itemId);
        if (fromSlot is { } slot)
        {
            var worn = Equipment.Get(slot);
            Equipment.Set(slot, null);
            return worn;
        }
        var stored = Inventory.Find(itemId);
        if (stored is not null)
        {
            Inventory.Remove(itemId);
            return stored.Item;
        }
        var beltSlot = Belt.IndexOf(itemId);
        return beltSlot >= 0 ? Belt.Take(beltSlot) : null;
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~ItemContainerTests"`
Expected: PASS (10 tests).

- [ ] **Step 5: Commit**

```bash
git add src/Mortar.Game/Items tests/Mortar.Tests/Gameplay/ItemContainerTests.cs tests/Mortar.Tests/Gameplay/Support/TestItems.cs
git commit -m "add items, inventory, belt, and equipment"
```

---

### Task 3: Gear stats and the starting kit

**Files:**
- Create: `src/Mortar.Game/Items/GearBonus.cs`, `src/Mortar.Game/Items/ItemFactory.cs`, `src/Mortar.Game/Simulation/StartingKit.cs`
- Modify: `src/Mortar.Game/Content/Stats.cs`, `src/Mortar.Game/Simulation/PlayerStats.cs`, `World.cs`, `PlayerGrowth.cs`
- Modify: `tests/Mortar.Tests/Gameplay/PlayerStatsTests.cs`
- Test: `tests/Mortar.Tests/Gameplay/GearTests.cs`

**Interfaces:**
- Consumes: `Equipment`, `Belongings`, `Item`, `RolledAffix`, `BonusKinds`, `WeaponSwing`, `ContentSet.ItemBase`.
- Produces:
  - `Stats.Plus(Stats other)`.
  - `GearBonus` (`None`, `From(Equipment)`, `Stats`, `Life`, `Mana`, `ArmorClass`, `ToHit`, `Damage`, `BlockChance`, `WeaponDamage` (`IntRange?`), `Swing` (`WeaponSwing?`)).
  - `PlayerStats.Compute(ClassDefinition, PlayerProgress, GameRules, GearBonus gear)`: stats = base + gear stats; weapon damage replaces unarmed damage; affix damage adds to the strength shift; weapon swing replaces the class attack timings; armor values, block chance, and affix bonuses add on.
  - `ItemFactory.CreatePlain(World, ItemBaseDefinition)` (internal; next item id, base item level, armor at the maximum, no affixes).
  - `World.Belongings`, internal `World.NextItemId` (starts at 1), internal `World.RefreshPlayer()`.
  - `StartingKit.Give(World)` (internal; called once by `World.Create`).

- [ ] **Step 1: Write the failing tests**

In `tests/Mortar.Tests/Gameplay/PlayerStatsTests.cs`, add `using Mortar.Game.Items;` and pass `GearBonus.None` as the fourth argument to both `PlayerStats.Compute` calls.

`tests/Mortar.Tests/Gameplay/GearTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Content;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class GearTests
{
    private static CombatProfile Compute(Equipment equipment)
    {
        var warrior = TestGameContent.Warrior();
        return PlayerStats.Compute(warrior, PlayerProgress.Start(warrior), TestGameContent.Rules(), GearBonus.From(equipment));
    }

    [Fact]
    public void Weapon_ReplacesUnarmedDamageAndSwing()
    {
        var equipment = new Equipment();
        var knife = TestGameContent.Weapon(damageMin: 3, damageMax: 5, swing: new WeaponSwing(10, 5));
        equipment.Set(EquipmentSlot.Weapon, TestItems.Create(knife));
        var profile = Compute(equipment);
        Assert.Equal(5, profile.Damage.Min);
        Assert.Equal(7, profile.Damage.Max);
        Assert.Equal(10, profile.Timings.AttackTicks);
        Assert.Equal(5, profile.Timings.HitFrame);
        Assert.Equal(TestGameContent.Timings().MoveTicks, profile.Timings.MoveTicks);
    }

    [Fact]
    public void ArmorShieldAndAffixes_AddUp()
    {
        var equipment = new Equipment();
        var hale = TestGameContent.Affix("hale", AffixPositions.Prefix, BonusKinds.Life, 7, ItemSlots.Ring);
        var brawn = TestGameContent.Affix("brawn", AffixPositions.Suffix, BonusKinds.Strength, 10, ItemSlots.Ring);
        equipment.Set(EquipmentSlot.Body, TestItems.Create(TestGameContent.Armor(), 1, armor: 5));
        equipment.Set(EquipmentSlot.Shield, TestItems.Create(TestGameContent.Armor("shield", ItemSlots.Shield, blockChance: 20), 2, armor: 3));
        equipment.Set(EquipmentSlot.LeftRing, TestItems.Create(TestGameContent.Ring(), 3, prefix: TestItems.Roll(hale, 7), suffix: TestItems.Roll(brawn, 10)));
        var profile = Compute(equipment);
        Assert.Equal(12, profile.ArmorClass);
        Assert.Equal(20, profile.BlockChance);
        Assert.Equal(57, profile.MaxLife);
        Assert.Equal(5, profile.Damage.Min);
        Assert.Equal(7, profile.Damage.Max);
    }

    [Fact]
    public void World_StartsWithTheClassKit()
    {
        var world = TestWorlds.Create(KitContent("knife", "vest", "tonic", "tonic"));
        var belongings = world.Belongings;
        Assert.Equal(1, belongings.Equipment.Get(EquipmentSlot.Weapon)!.Id);
        Assert.Equal(2, belongings.Equipment.Get(EquipmentSlot.Body)!.Id);
        Assert.Equal(3, belongings.Belt.Get(0)!.Id);
        Assert.Equal(4, belongings.Belt.Get(1)!.Id);
        Assert.Equal(9, world.Player.Profile.ArmorClass);
        Assert.Equal(5, world.Player.Profile.Damage.Min);
        Assert.Equal(world.Player.Profile.MaxLife, world.Player.Life);
        Assert.Equal(5, world.NextItemId);
    }

    [Fact]
    public void World_StowsKitItemsWhoseSlotIsTaken()
    {
        var world = TestWorlds.Create(KitContent("knife", "knife"));
        Assert.Equal(1, world.Belongings.Equipment.Get(EquipmentSlot.Weapon)!.Id);
        Assert.Equal(new Cell(0, 0), world.Belongings.Inventory.Find(2)!.Cell);
    }

    private static ContentSet KitContent(params string[] kit) => TestGameContent.Valid(
        items: TestGameContent.Items(new[] { TestGameContent.Weapon(), TestGameContent.Armor(), TestGameContent.Potion() }),
        classes: new[] { TestGameContent.Warrior(kit) });
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~GearTests|FullyQualifiedName~PlayerStatsTests"`
Expected: build FAILS (`GearBonus` and `World.Belongings` do not exist).

- [ ] **Step 3: Add `Stats.Plus` and `GearBonus`**

Add to `src/Mortar.Game/Content/Stats.cs` (inside `Stats`):

```csharp
    public Stats Plus(Stats other) =>
        new Stats(Strength + other.Strength, Dexterity + other.Dexterity, Vitality + other.Vitality, Magic + other.Magic);
```

`src/Mortar.Game/Items/GearBonus.cs`:

```csharp
using Mortar.Game.Content;

namespace Mortar.Game.Items;

public sealed class GearBonus
{
    private GearBonus(Totals totals, IntRange? weaponDamage, WeaponSwing? swing)
    {
        Stats = new Stats(totals.Strength, totals.Dexterity, totals.Vitality, totals.Magic);
        Life = totals.Life;
        Mana = totals.Mana;
        ArmorClass = totals.ArmorClass;
        ToHit = totals.ToHit;
        Damage = totals.Damage;
        BlockChance = totals.BlockChance;
        WeaponDamage = weaponDamage;
        Swing = swing;
    }

    public static GearBonus None { get; } = new GearBonus(new Totals(), null, null);

    public Stats Stats { get; }

    public int Life { get; }

    public int Mana { get; }

    public int ArmorClass { get; }

    public int ToHit { get; }

    public int Damage { get; }

    public int BlockChance { get; }

    public IntRange? WeaponDamage { get; }

    public WeaponSwing? Swing { get; }

    public static GearBonus From(Equipment equipment)
    {
        var totals = new Totals();
        IntRange? weaponDamage = null;
        WeaponSwing? swing = null;
        foreach (var item in equipment.Worn)
        {
            totals.ArmorClass += item.ArmorValue;
            totals.BlockChance += item.Definition.BlockChance;
            if (item.Definition.Slot == ItemSlots.Weapon)
            {
                weaponDamage = item.Definition.Damage;
                swing = item.Definition.Swing;
            }
            if (item.Prefix is not null) totals.Apply(item.Prefix);
            if (item.Suffix is not null) totals.Apply(item.Suffix);
        }
        return new GearBonus(totals, weaponDamage, swing);
    }

    private sealed class Totals
    {
        public int Strength { get; set; }

        public int Dexterity { get; set; }

        public int Vitality { get; set; }

        public int Magic { get; set; }

        public int Life { get; set; }

        public int Mana { get; set; }

        public int ArmorClass { get; set; }

        public int ToHit { get; set; }

        public int Damage { get; set; }

        public int BlockChance { get; set; }

        public void Apply(RolledAffix affix)
        {
            var value = affix.Value;
            switch (affix.Definition.Bonus)
            {
                case BonusKinds.Strength: Strength += value; break;
                case BonusKinds.Dexterity: Dexterity += value; break;
                case BonusKinds.Vitality: Vitality += value; break;
                case BonusKinds.Magic: Magic += value; break;
                case BonusKinds.Life: Life += value; break;
                case BonusKinds.Mana: Mana += value; break;
                case BonusKinds.ArmorClass: ArmorClass += value; break;
                case BonusKinds.ToHit: ToHit += value; break;
                case BonusKinds.Damage: Damage += value; break;
            }
        }
    }
}
```

- [ ] **Step 4: Use gear in `PlayerStats`**

Replace `src/Mortar.Game/Simulation/PlayerStats.cs` with:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

public static class PlayerStats
{
    public static CombatProfile Compute(ClassDefinition definition, PlayerProgress progress, GameRules rules, GearBonus gear)
    {
        var stats = progress.BaseStats.Plus(gear.Stats);
        var growth = definition.Growth;
        var levelsGained = progress.Level - 1;
        var maxLife = growth.LifeBase + stats.Vitality * growth.LifePerVitality + levelsGained * growth.LifePerLevel + gear.Life;
        var maxMana = growth.ManaBase + stats.Magic * growth.ManaPerMagic + levelsGained * growth.ManaPerLevel + gear.Mana;
        var armorClass = stats.Dexterity / rules.DexterityPerArmor + gear.ArmorClass;
        var toHit = rules.PlayerBaseToHit + stats.Dexterity * rules.ToHitPerDexterity + progress.Level * rules.ToHitPerLevel + gear.ToHit;
        var damage = (gear.WeaponDamage ?? definition.UnarmedDamage).Shift(stats.Strength / rules.StrengthPerDamage + gear.Damage);
        return new CombatProfile(
            progress.Level, maxLife, maxMana, armorClass, toHit, damage, gear.BlockChance, Timings(definition.Timings, gear.Swing));
    }

    private static ActionTimings Timings(ActionTimings classTimings, WeaponSwing? swing) =>
        swing is null
            ? classTimings
            : new ActionTimings(classTimings.MoveTicks, swing.AttackTicks, swing.HitFrame, classTimings.RecoveryTicks);
}
```

- [ ] **Step 5: Add the item factory, the starting kit, and world belongings**

`src/Mortar.Game/Items/ItemFactory.cs`:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Simulation;

namespace Mortar.Game.Items;

internal static class ItemFactory
{
    public static Item CreatePlain(World world, ItemBaseDefinition definition) =>
        new Item(world.NextItemId++, definition, definition.ItemLevel, definition.Armor?.Max ?? 0, null, null);
}
```

`src/Mortar.Game/Simulation/StartingKit.cs`:

```csharp
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class StartingKit
{
    public static void Give(World world)
    {
        var belongings = world.Belongings;
        foreach (var id in world.PlayerClass.StartingItems)
        {
            var item = ItemFactory.CreatePlain(world, world.Content.ItemBase(id));
            if (item.Definition.IsEquippable)
            {
                var slot = belongings.Equipment.SlotFor(item.Definition);
                if (belongings.Equipment.Get(slot) is null)
                {
                    belongings.Equipment.Set(slot, item);
                    continue;
                }
            }
            belongings.Stow(item);
        }
    }
}
```

Replace `src/Mortar.Game/Simulation/World.cs` with:

```csharp
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

public sealed class World
{
    private readonly Dictionary<int, LevelState> _levels = new Dictionary<int, LevelState>();

    private World(ContentSet content, ILevelGenerator generator, uint seed, ClassDefinition playerClass, PlayerProgress progress, Pcg32 rng)
    {
        Content = content;
        Generator = generator;
        Seed = seed;
        PlayerClass = playerClass;
        Rng = rng;
        Progress = progress;
        var profile = PlayerStats.Compute(playerClass, Progress, content.Rules, GearBonus.None);
        Player = new Actor(Actor.PlayerId, ActorKind.Player, playerClass.Id, profile);
    }

    public ContentSet Content { get; }

    public uint Seed { get; }

    public long TickCount { get; internal set; }

    public int Depth { get; internal set; }

    public ClassDefinition PlayerClass { get; }

    public PlayerProgress Progress { get; }

    public Actor Player { get; }

    public Belongings Belongings { get; } = new Belongings();

    public LevelState Level => _levels[Depth];

    public bool IsGameOver => Player.IsDead;

    public IReadOnlyList<int> VisitedDepths => _levels.Keys.OrderBy(depth => depth).ToList();

    internal Pcg32 Rng { get; }

    internal ILevelGenerator Generator { get; }

    internal int NextActorId { get; set; } = Actor.PlayerId + 1;

    internal int NextItemId { get; set; } = 1;

    public static WorldResult Create(ContentSet content, ILevelGenerator generator, string classId, uint seed)
    {
        if (!content.TryGetClass(classId, out var playerClass))
        {
            return WorldResult.Failure($"unknown class '{classId}'");
        }
        var world = new World(content, generator, seed, playerClass, PlayerProgress.Start(playerClass), Pcg32.ForAttempt("world", seed, 0));
        StartingKit.Give(world);
        world.RefreshPlayer();
        world.Player.RestoreVitals();
        var error = LevelEntry.Enter(world, 1, MarkerKinds.Entrance);
        return error is null ? WorldResult.Success(world) : WorldResult.Failure(error);
    }

    public LevelState GetLevel(int depth) => _levels[depth];

    public IReadOnlyList<GameEvent> Tick(IReadOnlyList<Command> commands) => TickRunner.Run(this, commands);

    internal LevelState? FindLevel(int depth) => _levels.TryGetValue(depth, out var level) ? level : null;

    internal void AddLevel(LevelState level) => _levels.Add(level.Depth, level);

    internal void RefreshPlayer() =>
        Player.ApplyProfile(PlayerStats.Compute(PlayerClass, Progress, Content.Rules, GearBonus.From(Belongings.Equipment)));

    internal Actor SpawnMonster(LevelState level, MonsterDefinition monster, Cell cell)
    {
        var maxLife = Rng.NextInt(monster.Life.Min, monster.Life.Max + 1);
        var actor = new Actor(NextActorId++, ActorKind.Monster, monster.Id, CombatProfile.ForMonster(monster, maxLife));
        level.Add(actor, cell);
        return actor;
    }
}
```

In `src/Mortar.Game/Simulation/PlayerGrowth.cs`, replace both `Refresh(world);` calls with `world.RefreshPlayer();` and delete the private `Refresh` method.

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~GearTests|FullyQualifiedName~PlayerStatsTests"`
Expected: PASS. Then `dotnet test`; expected: all pass (the repo warrior has no kit yet, so the determinism snapshot is unchanged).

- [ ] **Step 7: Commit**

```bash
git add src/Mortar.Game/Items/GearBonus.cs src/Mortar.Game/Items/ItemFactory.cs src/Mortar.Game/Simulation/StartingKit.cs src/Mortar.Game/Content/Stats.cs src/Mortar.Game/Simulation/PlayerStats.cs src/Mortar.Game/Simulation/World.cs src/Mortar.Game/Simulation/PlayerGrowth.cs tests/Mortar.Tests/Gameplay/GearTests.cs tests/Mortar.Tests/Gameplay/PlayerStatsTests.cs
git commit -m "add gear bonuses and starting kit"
```

---

### Task 4: Loot drops

**Files:**
- Create: `src/Mortar.Game/Items/GroundItem.cs`, `src/Mortar.Game/Simulation/LootSystem.cs`, `src/Mortar.Game/Events/ItemEvents.cs`
- Modify: `src/Mortar.Game/Items/ItemFactory.cs`, `src/Mortar.Game/Simulation/LevelState.cs`, `DeathSystem.cs`
- Test: `tests/Mortar.Tests/Gameplay/LootTests.cs`

**Interfaces:**
- Consumes: `LootTableDefinition`, `MonsterDefinition.LootTableId`, `Pcg32.PickWeighted(IReadOnlyList<int>)`, `Pcg32.NextInt`.
- Produces:
  - `GroundItem` (internal ctor `(int id, Cell cell, Item? item, int gold)`; `Id`, `Cell`, `Item`, `Gold`, `IsGold`, `Describe()`).
  - `LevelState.DropRadius = 3`, `GroundItems` (ascending id), `FindGroundItem(int id)`, internal `DropCell(Cell origin)`, `AddGroundItem(GroundItem)`, `RemoveGroundItem(GroundItem)`.
  - `ItemFactory.Roll(World, int itemLevel, int magicPercent)` → `Item?` (null when no base can drop).
  - `LootSystem.Drop(World, Actor monster, EventSink)`, `LootSystem.Place(World, Cell origin, Item? item, int gold, EventSink)` (internal).
  - Event `ItemDropped(long tick, int groundItemId, Cell cell, string description)`.
- Random draws per drop, in order: one `PickWeighted` over (none, gold, item) per roll; gold: one `NextInt`; item: base `PickWeighted`, armor `NextInt` (armored bases only), and for non-potions `NextInt(100)` for the magic check, then the shape `NextInt(3)` and, per rolled part, an affix `PickWeighted` and a value `NextInt`.

- [ ] **Step 1: Write the failing tests**

`tests/Mortar.Tests/Gameplay/LootTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class LootTests
{
    private static readonly AffixDefinition[] WeaponAffixes =
    {
        TestGameContent.Affix("Keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon),
        TestGameContent.Affix("of Brawn", AffixPositions.Suffix, BonusKinds.Strength, 2, ItemSlots.Weapon),
    };

    [Fact]
    public void KilledMonster_DropsAnItemOnItsCell()
    {
        var world = CreateWorld(TestGameContent.Loot(), TestGameContent.Weapon());
        var events = KillZombie(world, out var zombieCell);
        var dropped = Assert.Single(events.OfType<ItemDropped>());
        Assert.Equal("knife", dropped.Description);
        var ground = Assert.Single(world.Level.GroundItems);
        Assert.Equal(zombieCell, ground.Cell);
        Assert.Equal(ground.Item!.Id, ground.Id);
    }

    [Fact]
    public void GoldRoll_DropsAPileOfTheTableAmount()
    {
        var world = CreateWorld(TestGameContent.Loot(gold: 1, item: 0), TestGameContent.Weapon());
        KillZombie(world, out _);
        var ground = Assert.Single(world.Level.GroundItems);
        Assert.True(ground.IsGold);
        Assert.Equal(5, ground.Gold);
        Assert.Equal("5 gold", ground.Describe());
    }

    [Fact]
    public void NoneRoll_DropsNothing()
    {
        var world = CreateWorld(TestGameContent.Loot(none: 1, item: 0), TestGameContent.Weapon());
        KillZombie(world, out _);
        Assert.Empty(world.Level.GroundItems);
    }

    [Fact]
    public void SeveralDrops_SpreadOverFreeFloor()
    {
        var world = CreateWorld(TestGameContent.Loot(rolls: 4), TestGameContent.Weapon());
        KillZombie(world, out var zombieCell);
        var cells = world.Level.GroundItems.Select(g => g.Cell).ToList();
        Assert.Equal(4, cells.Distinct().Count());
        Assert.Equal(zombieCell, cells[0]);
        Assert.All(cells, cell => Assert.True(world.Level.IsWalkable(cell)));
        Assert.All(cells, cell => Assert.True(cell.DistanceTo(zombieCell) <= LevelState.DropRadius));
    }

    [Fact]
    public void MagicChance_RollsAffixes()
    {
        var world = CreateWorld(TestGameContent.Loot(magicPercent: 100), TestGameContent.Weapon());
        KillZombie(world, out _);
        var item = world.Level.GroundItems.Single().Item!;
        Assert.Equal(ItemQuality.Magic, item.Quality);
        Assert.NotEqual("knife", item.Name);
    }

    [Fact]
    public void NoEligibleAffix_LeavesTheItemNormal()
    {
        var ringOnly = new[] { TestGameContent.Affix("Hale", AffixPositions.Prefix, BonusKinds.Life, 3, ItemSlots.Ring) };
        var world = CreateWorld(TestGameContent.Loot(magicPercent: 100), new[] { TestGameContent.Weapon() }, ringOnly);
        KillZombie(world, out _);
        Assert.Equal(ItemQuality.Normal, world.Level.GroundItems.Single().Item!.Quality);
    }

    [Fact]
    public void PotionsAreNeverMagic()
    {
        var world = CreateWorld(TestGameContent.Loot(magicPercent: 100), TestGameContent.Potion());
        KillZombie(world, out _);
        Assert.Equal(ItemQuality.Normal, world.Level.GroundItems.Single().Item!.Quality);
    }

    [Fact]
    public void BasesAboveTheMonsterLevel_NeverDrop()
    {
        var world = CreateWorld(TestGameContent.Loot(rolls: 10), TestGameContent.Weapon(), TestGameContent.Weapon("sword", itemLevel: 5));
        KillZombie(world, out _);
        Assert.Equal(10, world.Level.GroundItems.Count);
        Assert.All(world.Level.GroundItems, ground => Assert.Equal("knife", ground.Item!.Definition.Id));
    }

    [Fact]
    public void SameSeed_GivesTheSameDrops()
    {
        static string Drops()
        {
            var world = CreateWorld(TestGameContent.Loot(rolls: 3, gold: 1, magicPercent: 50), TestGameContent.Weapon(), TestGameContent.Armor());
            KillZombie(world, out _);
            return string.Join("|", world.Level.GroundItems.Select(g => $"{g.Cell}:{g.Describe()}"));
        }
        Assert.Equal(Drops(), Drops());
    }

    private static World CreateWorld(LootTableDefinition table, params ItemBaseDefinition[] bases) =>
        CreateWorld(table, bases, WeaponAffixes);

    private static World CreateWorld(LootTableDefinition table, ItemBaseDefinition[] bases, AffixDefinition[] affixes)
    {
        var content = TestGameContent.Valid(
            monsters: new[] { TestGameContent.Monster("zombie", loot: table.Id) },
            rules: TestGameContent.Rules(minHit: 95),
            items: TestGameContent.Items(bases, affixes, new[] { table }));
        return TestWorlds.Create(content, seed: 3);
    }

    private static List<GameEvent> KillZombie(World world, out Cell zombieCell)
    {
        var zombie = TestWorlds.Spawn(world, "zombie", 3, 2);
        zombie.Life = 1;
        zombieCell = zombie.Cell;
        world.Player.Life = 1000;
        var events = TestWorlds.Run(world, 120, new AttackCommand(zombie.Id));
        Assert.True(zombie.IsDead);
        return events;
    }
}
```

The zombie stands in open floor so drops can spread; a 95% hit chance and 120 ticks make the kill certain for these seeds (the assertion on `zombie.IsDead` catches a miss streak). The player's life is raised so the zombie cannot win.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~LootTests"`
Expected: build FAILS (`ItemDropped` and `LevelState.GroundItems` do not exist).

- [ ] **Step 3: Add ground items and drop cells**

`src/Mortar.Game/Items/GroundItem.cs`:

```csharp
namespace Mortar.Game.Items;

public sealed class GroundItem
{
    internal GroundItem(int id, Cell cell, Item? item, int gold)
    {
        Id = id;
        Cell = cell;
        Item = item;
        Gold = gold;
    }

    public int Id { get; }

    public Cell Cell { get; }

    public Item? Item { get; }

    public int Gold { get; }

    public bool IsGold => Item is null;

    public string Describe() => Item?.Name ?? $"{Gold} gold";
}
```

In `src/Mortar.Game/Simulation/LevelState.cs`:
- add `using Mortar.Game.Items;`;
- add `public const int DropRadius = 3;` and the field `private readonly List<GroundItem> _groundItems = new List<GroundItem>();`;
- replace `NearestFree` with a shared ring scan, and add the ground-item members:

```csharp
    public IReadOnlyList<GroundItem> GroundItems => _groundItems;

    public GroundItem? FindGroundItem(int id) => _groundItems.Find(ground => ground.Id == id);

    internal Cell? NearestFree(Cell origin, int maxRadius) => FirstInRings(origin, maxRadius, IsFree);

    internal Cell DropCell(Cell origin) =>
        FirstInRings(origin, DropRadius, cell => IsWalkable(cell) && !_groundItems.Exists(ground => ground.Cell == cell)) ?? origin;

    internal void AddGroundItem(GroundItem ground)
    {
        var index = _groundItems.FindIndex(existing => existing.Id > ground.Id);
        _groundItems.Insert(index < 0 ? _groundItems.Count : index, ground);
    }

    internal void RemoveGroundItem(GroundItem ground) => _groundItems.Remove(ground);

    private static Cell? FirstInRings(Cell origin, int maxRadius, Func<Cell, bool> accept)
    {
        for (var radius = 0; radius <= maxRadius; radius++)
        {
            for (var y = origin.Y - radius; y <= origin.Y + radius; y++)
            {
                for (var x = origin.X - radius; x <= origin.X + radius; x++)
                {
                    var cell = new Cell(x, y);
                    if (cell.DistanceTo(origin) == radius && accept(cell)) return cell;
                }
            }
        }
        return null;
    }
```

- [ ] **Step 4: Add the drop event**

`src/Mortar.Game/Events/ItemEvents.cs`:

```csharp
namespace Mortar.Game.Events;

public sealed class ItemDropped : GameEvent
{
    public ItemDropped(long tick, int groundItemId, Cell cell, string description)
        : base(tick)
    {
        GroundItemId = groundItemId;
        Cell = cell;
        Description = description;
    }

    public int GroundItemId { get; }

    public Cell Cell { get; }

    public string Description { get; }

    protected override string Describe() => $"item {GroundItemId} '{Description}' dropped at {Cell}";
}
```

- [ ] **Step 5: Roll items and drop loot**

Replace `src/Mortar.Game/Items/ItemFactory.cs` with:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Simulation;

namespace Mortar.Game.Items;

internal static class ItemFactory
{
    public static Item CreatePlain(World world, ItemBaseDefinition definition) =>
        new Item(world.NextItemId++, definition, definition.ItemLevel, definition.Armor?.Max ?? 0, null, null);

    public static Item? Roll(World world, int itemLevel, int magicPercent)
    {
        var bases = world.Content.ItemBases.Where(b => b.DropWeight > 0 && b.ItemLevel <= itemLevel).ToList();
        if (bases.Count == 0) return null;
        var definition = bases[world.Rng.PickWeighted(bases.Select(b => b.DropWeight).ToList())];
        var armor = definition.Armor is { } range ? world.Rng.NextInt(range.Min, range.Max + 1) : 0;
        RolledAffix? prefix = null;
        RolledAffix? suffix = null;
        if (!definition.IsPotion && world.Rng.NextInt(100) < magicPercent)
        {
            var shape = world.Rng.NextInt(3);
            if (shape != 1) prefix = RollAffix(world, definition, itemLevel, AffixPositions.Prefix);
            if (shape != 0) suffix = RollAffix(world, definition, itemLevel, AffixPositions.Suffix);
        }
        return new Item(world.NextItemId++, definition, itemLevel, armor, prefix, suffix);
    }

    private static RolledAffix? RollAffix(World world, ItemBaseDefinition definition, int itemLevel, string position)
    {
        var affixes = world.Content.Affixes.Where(a => a.Position == position && a.Allows(definition.Slot, itemLevel)).ToList();
        if (affixes.Count == 0) return null;
        var affix = affixes[world.Rng.PickWeighted(affixes.Select(a => a.Weight).ToList())];
        return new RolledAffix(affix, world.Rng.NextInt(affix.Value.Min, affix.Value.Max + 1));
    }
}
```

Shape 0 is prefix only, 1 is suffix only, 2 is both.

`src/Mortar.Game/Simulation/LootSystem.cs`:

```csharp
using Mortar.Game.Events;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class LootSystem
{
    private const int GoldRoll = 1;
    private const int ItemRoll = 2;

    public static void Drop(World world, Actor monster, EventSink events)
    {
        var definition = world.Content.Monster(monster.DefinitionId);
        if (definition.LootTableId is null) return;
        var table = world.Content.LootTable(definition.LootTableId);
        var weights = new[] { table.NoneWeight, table.GoldWeight, table.ItemWeight };
        for (var roll = 0; roll < table.Rolls; roll++)
        {
            var outcome = world.Rng.PickWeighted(weights);
            if (outcome == GoldRoll)
            {
                Place(world, monster.Cell, null, world.Rng.NextInt(table.Gold.Min, table.Gold.Max + 1), events);
            }
            else if (outcome == ItemRoll)
            {
                var item = ItemFactory.Roll(world, definition.Level, table.MagicPercent);
                if (item is not null) Place(world, monster.Cell, item, 0, events);
            }
        }
    }

    public static void Place(World world, Cell origin, Item? item, int gold, EventSink events)
    {
        var level = world.Level;
        var ground = new GroundItem(item?.Id ?? world.NextItemId++, level.DropCell(origin), item, gold);
        level.AddGroundItem(ground);
        events.Add(new ItemDropped(events.Tick, ground.Id, ground.Cell, ground.Describe()));
    }
}
```

Replace the loop body of `DeathSystem.Process` so loot drops for every monster death, after any reward:

```csharp
        foreach (var actor in level.Actors)
        {
            if (actor.IsDead || actor.Life > 0) continue;
            actor.MarkDead();
            level.Vacate(actor);
            events.Add(new ActorDied(events.Tick, actor.Id, actor.LastAttackerId));
            if (actor.Kind != ActorKind.Monster) continue;
            if (actor.LastAttackerId == Actor.PlayerId && !world.Player.IsDead)
            {
                PlayerGrowth.RewardKill(world, actor, events);
            }
            LootSystem.Drop(world, actor, events);
        }
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~LootTests"`
Expected: PASS (9 tests). Then `dotnet test`; expected: all pass (repo monsters have no loot table yet, so the snapshot is unchanged).

- [ ] **Step 7: Commit**

```bash
git add src/Mortar.Game/Items/GroundItem.cs src/Mortar.Game/Items/ItemFactory.cs src/Mortar.Game/Simulation/LootSystem.cs src/Mortar.Game/Simulation/LevelState.cs src/Mortar.Game/Simulation/DeathSystem.cs src/Mortar.Game/Events/ItemEvents.cs tests/Mortar.Tests/Gameplay/LootTests.cs
git commit -m "add loot drops"
```

---

### Task 5: Picking items up

**Files:**
- Create: `src/Mortar.Game/Commands/ItemCommands.cs`, `src/Mortar.Game/Simulation/PickupSystem.cs`
- Modify: `src/Mortar.Game/Simulation/IntentKind.cs`, `Actor.cs`, `ActionSystem.cs`, `Movement.cs`, `CommandSystem.cs`, `TickRunner.cs`, `src/Mortar.Game/Events/ItemEvents.cs`
- Modify: `tests/Mortar.Tests/Gameplay/Support/TestWorlds.cs`
- Test: `tests/Mortar.Tests/Gameplay/PickupTests.cs`

**Interfaces:**
- Consumes: `LevelState.FindGroundItem`, `RemoveGroundItem`, `Belongings.Stow`, `Reach.CanStrike`, `Pathfinder.FindPathNextTo`, `Movement.IsReachable`.
- Produces:
  - `PickUpCommand(int groundItemId)` ("pick up item N").
  - `IntentKind.PickUp`; `Actor.SetPickUpIntent(int groundItemId, Cell cell)` (internal).
  - `Movement.Approach(LevelState, Actor, Cell goal, EventSink)`: steps toward the goal until it is in reach; drops the intent when terrain makes the goal unreachable.
  - `PickupSystem.Start(World, PickUpCommand)` → rejection reason or null; `PickupSystem.Process(World, EventSink)` runs last in each tick; `PickupSystem.InventoryFull = "inventory is full"`.
  - Events `ItemPickedUp(tick, itemId, name)`, `GoldPickedUp(tick, amount, total)`, `PickupFailed(tick, groundItemId, reason)`.
  - Test helpers `TestWorlds.DropGold(World, int x, int y, int amount)` and `TestWorlds.DropItem(World, int x, int y, ItemBaseDefinition)` → `GroundItem`.
- Tick order becomes: commands, AI, actions, combat, deaths, pickups.

- [ ] **Step 1: Write the test helpers and failing tests**

Add to `tests/Mortar.Tests/Gameplay/Support/TestWorlds.cs` (with `using Mortar.Game.Items;`):

```csharp
    public static GroundItem DropGold(World world, int x, int y, int amount)
    {
        var ground = new GroundItem(world.NextItemId++, new Cell(x, y), null, amount);
        world.Level.AddGroundItem(ground);
        return ground;
    }

    public static GroundItem DropItem(World world, int x, int y, ItemBaseDefinition definition)
    {
        var item = ItemFactory.CreatePlain(world, definition);
        var ground = new GroundItem(item.Id, new Cell(x, y), item, 0);
        world.Level.AddGroundItem(ground);
        return ground;
    }
```

`tests/Mortar.Tests/Gameplay/PickupTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class PickupTests
{
    private static readonly ItemBaseDefinition Knife = TestGameContent.Weapon();
    private static readonly ItemBaseDefinition Vest = TestGameContent.Armor();
    private static readonly ItemBaseDefinition Tonic = TestGameContent.Potion();

    private static readonly string[] Walled =
    {
        "#########",
        "#E..#...#",
        "#...#...#",
        "#...#...#",
        "#...#..X#",
        "#########",
    };

    [Fact]
    public void AdjacentGold_IsPickedUpTheSameTick()
    {
        var world = Create();
        var gold = TestWorlds.DropGold(world, 2, 1, 7);
        var events = world.Tick(new Command[] { new PickUpCommand(gold.Id) });
        var picked = Assert.Single(events.OfType<GoldPickedUp>());
        Assert.Equal(7, picked.Amount);
        Assert.Equal(7, world.Belongings.Gold);
        Assert.Empty(world.Level.GroundItems);
        Assert.Equal(IntentKind.None, world.Player.Intent);
    }

    [Fact]
    public void FarItem_IsWalkedToThenPickedUp()
    {
        var world = Create();
        var ground = TestWorlds.DropItem(world, 6, 3, Knife);
        var events = TestWorlds.Run(world, 80, new PickUpCommand(ground.Id));
        Assert.Single(events.OfType<ItemPickedUp>());
        Assert.NotNull(world.Belongings.Inventory.Find(ground.Id));
        Assert.True(world.Player.Cell.DistanceTo(new Cell(6, 3)) <= 1);
    }

    [Fact]
    public void Potion_GoesToTheBelt()
    {
        var world = Create();
        var ground = TestWorlds.DropItem(world, 2, 2, Tonic);
        world.Tick(new Command[] { new PickUpCommand(ground.Id) });
        Assert.Equal(ground.Id, world.Belongings.Belt.Get(0)!.Id);
    }

    [Fact]
    public void FullInventory_LeavesTheItemOnTheGround()
    {
        var world = Create();
        for (var id = 100; id < 110; id++) Assert.True(world.Belongings.Inventory.TryAdd(TestItems.Create(Vest, id)));
        var ground = TestWorlds.DropItem(world, 2, 1, Vest);
        var events = world.Tick(new Command[] { new PickUpCommand(ground.Id) });
        var failed = Assert.Single(events.OfType<PickupFailed>());
        Assert.Equal(PickupSystem.InventoryFull, failed.Reason);
        Assert.Same(ground, world.Level.FindGroundItem(ground.Id));
        Assert.Equal(IntentKind.None, world.Player.Intent);
    }

    [Fact]
    public void MissingItem_IsRejected()
    {
        var world = Create();
        var events = world.Tick(new Command[] { new PickUpCommand(99) });
        Assert.Equal("no such item on this level", Assert.Single(events.OfType<CommandRejected>()).Reason);
    }

    [Fact]
    public void WalledOffItem_IsRejected()
    {
        var world = Create(new[] { Walled, TestWorlds.Room });
        var ground = TestWorlds.DropItem(world, 6, 2, Knife);
        var events = world.Tick(new Command[] { new PickUpCommand(ground.Id) });
        Assert.Equal("item is unreachable", Assert.Single(events.OfType<CommandRejected>()).Reason);
    }

    [Fact]
    public void NewCommand_ReplacesThePickup()
    {
        var world = Create();
        var ground = TestWorlds.DropItem(world, 6, 3, Knife);
        world.Tick(new Command[] { new PickUpCommand(ground.Id) });
        TestWorlds.Run(world, 60, new MoveCommand(new Cell(1, 4)));
        Assert.Same(ground, world.Level.FindGroundItem(ground.Id));
        Assert.Equal(new Cell(1, 4), world.Player.Cell);
    }

    private static World Create(string[][]? levels = null) => TestWorlds.Create(
        TestGameContent.Valid(items: TestGameContent.Items(new[] { Knife, Vest, Tonic })), levels: levels);
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~PickupTests"`
Expected: build FAILS (`PickUpCommand` does not exist).

- [ ] **Step 3: Add the command, intent, and events**

`src/Mortar.Game/Commands/ItemCommands.cs`:

```csharp
namespace Mortar.Game.Commands;

public sealed class PickUpCommand : Command
{
    public PickUpCommand(int groundItemId)
    {
        GroundItemId = groundItemId;
    }

    public int GroundItemId { get; }

    public override string Describe() => $"pick up item {GroundItemId}";
}
```

Add `PickUp` as the last member of `IntentKind`. Add to `Actor`:

```csharp
    internal void SetPickUpIntent(int groundItemId, Cell cell)
    {
        Intent = IntentKind.PickUp;
        IntentCell = cell;
        IntentTargetId = groundItemId;
    }
```

Append to `src/Mortar.Game/Events/ItemEvents.cs`:

```csharp
public sealed class ItemPickedUp : GameEvent
{
    public ItemPickedUp(long tick, int itemId, string name)
        : base(tick)
    {
        ItemId = itemId;
        Name = name;
    }

    public int ItemId { get; }

    public string Name { get; }

    protected override string Describe() => $"picked up item {ItemId} '{Name}'";
}

public sealed class GoldPickedUp : GameEvent
{
    public GoldPickedUp(long tick, int amount, long total)
        : base(tick)
    {
        Amount = amount;
        Total = total;
    }

    public int Amount { get; }

    public long Total { get; }

    protected override string Describe() => $"picked up {Amount} gold (total {Total})";
}

public sealed class PickupFailed : GameEvent
{
    public PickupFailed(long tick, int groundItemId, string reason)
        : base(tick)
    {
        GroundItemId = groundItemId;
        Reason = reason;
    }

    public int GroundItemId { get; }

    public string Reason { get; }

    protected override string Describe() => $"could not pick up item {GroundItemId}: {Reason}";
}
```

- [ ] **Step 4: Walk to items and pick them up**

Add to `src/Mortar.Game/Simulation/Movement.cs`:

```csharp
    public static void Approach(LevelState level, Actor actor, Cell goal, EventSink events)
    {
        if (actor.Cell == goal || Reach.CanStrike(actor.Cell, goal, level.IsWalkable)) return;
        var path = Pathfinder.FindPathNextTo(actor.Cell, goal, level.IsWalkable, level.IsFree);
        if (path is { Count: > 0 })
        {
            Step(level, actor, path[0], events);
            return;
        }
        if (path is null && !IsReachable(level, actor.Cell, goal)) actor.ClearIntent();
    }
```

In `ActionSystem.StartFromIntent`, add:

```csharp
            case IntentKind.PickUp:
                Movement.Approach(level, actor, actor.IntentCell, events);
                break;
```

`src/Mortar.Game/Simulation/PickupSystem.cs`:

```csharp
using Mortar.Game.Commands;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Pathing;

namespace Mortar.Game.Simulation;

internal static class PickupSystem
{
    public const string InventoryFull = "inventory is full";

    public static string? Start(World world, PickUpCommand command)
    {
        var ground = world.Level.FindGroundItem(command.GroundItemId);
        if (ground is null) return "no such item on this level";
        if (!Movement.IsReachable(world.Level, world.Player.Cell, ground.Cell)) return "item is unreachable";
        world.Player.SetPickUpIntent(ground.Id, ground.Cell);
        return null;
    }

    public static void Process(World world, EventSink events)
    {
        var player = world.Player;
        if (!player.IsAlive || player.Intent != IntentKind.PickUp || player.Action != ActionKind.Idle) return;
        var level = world.Level;
        var ground = level.FindGroundItem(player.IntentTargetId);
        if (ground is null)
        {
            player.ClearIntent();
            return;
        }
        if (player.Cell != ground.Cell && !Reach.CanStrike(player.Cell, ground.Cell, level.IsWalkable)) return;
        player.ClearIntent();
        Take(world, ground, events);
    }

    private static void Take(World world, GroundItem ground, EventSink events)
    {
        if (ground.Item is null)
        {
            world.Belongings.Gold += ground.Gold;
            world.Level.RemoveGroundItem(ground);
            events.Add(new GoldPickedUp(events.Tick, ground.Gold, world.Belongings.Gold));
            return;
        }
        if (!world.Belongings.Stow(ground.Item))
        {
            events.Add(new PickupFailed(events.Tick, ground.Id, InventoryFull));
            return;
        }
        world.Level.RemoveGroundItem(ground);
        events.Add(new ItemPickedUp(events.Tick, ground.Item.Id, ground.Item.Name));
    }
}
```

In `CommandSystem.Execute`, add before `default:`:

```csharp
            case PickUpCommand pickUp:
                return PickupSystem.Start(world, pickUp);
```

In `TickRunner.Run`, add `PickupSystem.Process(world, events);` after `DeathSystem.Process(world, events);`.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~PickupTests"`
Expected: PASS (7 tests). Then `dotnet test`; expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add src/Mortar.Game/Commands/ItemCommands.cs src/Mortar.Game/Simulation/PickupSystem.cs src/Mortar.Game/Simulation/IntentKind.cs src/Mortar.Game/Simulation/Actor.cs src/Mortar.Game/Simulation/ActionSystem.cs src/Mortar.Game/Simulation/Movement.cs src/Mortar.Game/Simulation/CommandSystem.cs src/Mortar.Game/Simulation/TickRunner.cs src/Mortar.Game/Events/ItemEvents.cs tests/Mortar.Tests/Gameplay/PickupTests.cs tests/Mortar.Tests/Gameplay/Support/TestWorlds.cs
git commit -m "add item pickup"
```

---

### Task 6: Equip, drop, move, and belt commands

**Files:**
- Create: `src/Mortar.Game/Simulation/EquipActions.cs`, `src/Mortar.Game/Simulation/InventoryActions.cs`
- Modify: `src/Mortar.Game/Commands/ItemCommands.cs`, `src/Mortar.Game/Events/ItemEvents.cs`, `src/Mortar.Game/Simulation/CommandSystem.cs`
- Modify: `tests/Mortar.Tests/Gameplay/Support/TestWorlds.cs`
- Test: `tests/Mortar.Tests/Gameplay/InventoryCommandTests.cs`

**Interfaces:**
- Consumes: `Belongings`, `Inventory.Fits`/`TryPlace`/`TryAdd`/`Remove`, `Belt`, `Equipment.SlotFor`, `World.RefreshPlayer`, `LootSystem.Place`.
- Produces:
  - Commands: `EquipCommand(int itemId)`, `UnequipCommand(EquipmentSlot slot)`, `DropItemCommand(int itemId)`, `MoveItemCommand(int itemId, Cell to)`, `PutInBeltCommand(int itemId, int slot)`, `UseBeltCommand(int slot)`.
  - Events: `ItemEquipped(tick, itemId, EquipmentSlot)`, `ItemUnequipped(tick, itemId, EquipmentSlot)`, `ItemMoved(tick, itemId, Cell)`, `ItemBelted(tick, itemId, int slot)`, `PotionDrunk(tick, itemId, string restores, int amount)`.
  - `EquipActions.Equip`/`Unequip`, `InventoryActions.Drop`/`Move`/`PutInBelt`/`UseBelt` (internal; each returns a rejection reason or null and changes nothing when it rejects).
  - Test helper `TestWorlds.Give(World, ItemBaseDefinition)` → `Item` (stows a plain item; throws when there is no room).

- [ ] **Step 1: Write the test helper and failing tests**

Add to `tests/Mortar.Tests/Gameplay/Support/TestWorlds.cs`:

```csharp
    public static Item Give(World world, ItemBaseDefinition definition)
    {
        var item = ItemFactory.CreatePlain(world, definition);
        return world.Belongings.Stow(item) ? item : throw new InvalidOperationException($"no room for {definition.Id}");
    }
```

`tests/Mortar.Tests/Gameplay/InventoryCommandTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class InventoryCommandTests
{
    private static readonly ItemBaseDefinition Knife = TestGameContent.Weapon();
    private static readonly ItemBaseDefinition Sword = TestGameContent.Weapon("sword", strength: 30);
    private static readonly ItemBaseDefinition Maul = new ItemBaseDefinition(
        "maul", "maul", "maul", ItemSlots.Weapon, 2, 3, 1, 1, default, new IntRange(5, 9), null, null, 0, null);
    private static readonly ItemBaseDefinition Vest = TestGameContent.Armor();
    private static readonly ItemBaseDefinition Ring = TestGameContent.Ring();
    private static readonly ItemBaseDefinition Tonic = TestGameContent.Potion();

    [Fact]
    public void Equip_MovesTheItemAndRaisesStats()
    {
        var world = Create();
        var knife = TestWorlds.Give(world, Knife);
        var events = Tick(world, new EquipCommand(knife.Id));
        Assert.Equal(EquipmentSlot.Weapon, Assert.Single(events.OfType<ItemEquipped>()).Slot);
        Assert.Same(knife, world.Belongings.Equipment.Get(EquipmentSlot.Weapon));
        Assert.Empty(world.Belongings.Inventory.Items);
        Assert.Equal(5, world.Player.Profile.Damage.Min);
    }

    [Fact]
    public void Equip_SwapsTheOldItemIntoTheInventory()
    {
        var world = Create();
        var first = TestWorlds.Give(world, Knife);
        var second = TestWorlds.Give(world, Knife);
        Tick(world, new EquipCommand(first.Id));
        var events = Tick(world, new EquipCommand(second.Id));
        Assert.Equal(first.Id, Assert.Single(events.OfType<ItemUnequipped>()).ItemId);
        Assert.Same(second, world.Belongings.Equipment.Get(EquipmentSlot.Weapon));
        Assert.NotNull(world.Belongings.Inventory.Find(first.Id));
    }

    [Fact]
    public void Equip_WithUnmetRequirement_IsRejected()
    {
        var world = Create();
        var sword = TestWorlds.Give(world, Sword);
        Assert.Equal("requires 30 strength", Rejection(Tick(world, new EquipCommand(sword.Id))));
    }

    [Fact]
    public void Equip_WithNoRoomForTheSwappedItem_ChangesNothing()
    {
        var world = Create();
        var maul = TestWorlds.Give(world, Maul);
        Tick(world, new EquipCommand(maul.Id));
        var knife = TestWorlds.Give(world, Knife);
        for (var i = 0; i < 38; i++) TestWorlds.Give(world, Ring);
        var before = world.Player.Profile;
        var events = Tick(world, new EquipCommand(knife.Id));
        Assert.Equal("no room in the inventory for the item being replaced", Rejection(events));
        Assert.Same(maul, world.Belongings.Equipment.Get(EquipmentSlot.Weapon));
        Assert.Equal(new Cell(0, 0), world.Belongings.Inventory.Find(knife.Id)!.Cell);
        Assert.Same(before, world.Player.Profile);
    }

    [Fact]
    public void Unequip_RestoresTheExactPreviousStats()
    {
        var world = Create();
        var before = world.Player.Profile;
        var vest = TestWorlds.Give(world, Vest);
        Tick(world, new EquipCommand(vest.Id));
        Assert.Equal(before.ArmorClass + 5, world.Player.Profile.ArmorClass);
        var events = Tick(world, new UnequipCommand(EquipmentSlot.Body));
        Assert.Single(events.OfType<ItemUnequipped>());
        var after = world.Player.Profile;
        Assert.Equal(
            (before.MaxLife, before.MaxMana, before.ArmorClass, before.ToHit, before.Damage.Min, before.Damage.Max, before.BlockChance),
            (after.MaxLife, after.MaxMana, after.ArmorClass, after.ToHit, after.Damage.Min, after.Damage.Max, after.BlockChance));
        Assert.NotNull(world.Belongings.Inventory.Find(vest.Id));
    }

    [Fact]
    public void Unequip_BadRequests_AreRejected()
    {
        var world = Create();
        Assert.Equal("nothing is equipped there", Rejection(Tick(world, new UnequipCommand(EquipmentSlot.Head))));
        Assert.Equal("unknown equipment slot", Rejection(Tick(world, new UnequipCommand((EquipmentSlot)42))));
        var vest = TestWorlds.Give(world, Vest);
        Tick(world, new EquipCommand(vest.Id));
        for (var i = 0; i < 10; i++) TestWorlds.Give(world, Vest);
        Assert.Equal(PickupSystem.InventoryFull, Rejection(Tick(world, new UnequipCommand(EquipmentSlot.Body))));
        Assert.Same(vest, world.Belongings.Equipment.Get(EquipmentSlot.Body));
    }

    [Fact]
    public void Drop_PutsTheItemUnderThePlayer()
    {
        var world = Create();
        var knife = TestWorlds.Give(world, Knife);
        var events = Tick(world, new DropItemCommand(knife.Id));
        Assert.Single(events.OfType<ItemDropped>());
        var ground = Assert.Single(world.Level.GroundItems);
        Assert.Equal(knife.Id, ground.Id);
        Assert.Equal(world.Player.Cell, ground.Cell);
        Assert.Empty(world.Belongings.Inventory.Items);
    }

    [Fact]
    public void DroppingWornGear_RefreshesStats()
    {
        var world = Create();
        var armor = world.Player.Profile.ArmorClass;
        var vest = TestWorlds.Give(world, Vest);
        Tick(world, new EquipCommand(vest.Id));
        var events = Tick(world, new DropItemCommand(vest.Id));
        Assert.Single(events.OfType<ItemUnequipped>());
        Assert.Equal(armor, world.Player.Profile.ArmorClass);
    }

    [Fact]
    public void Move_PlacesTheItemAtTheCell()
    {
        var world = Create();
        var knife = TestWorlds.Give(world, Knife);
        Assert.Single(Tick(world, new MoveItemCommand(knife.Id, new Cell(5, 1))).OfType<ItemMoved>());
        Assert.Equal(new Cell(5, 1), world.Belongings.Inventory.Find(knife.Id)!.Cell);
        Assert.Equal("item does not fit there", Rejection(Tick(world, new MoveItemCommand(knife.Id, new Cell(9, 3)))));
        var tonic = TestWorlds.Give(world, Tonic);
        Tick(world, new MoveItemCommand(tonic.Id, new Cell(3, 3)));
        Assert.Null(world.Belongings.Belt.Get(0));
        Assert.Equal(new Cell(3, 3), world.Belongings.Inventory.Find(tonic.Id)!.Cell);
    }

    [Fact]
    public void UseBelt_RestoresLifeUpToTheMaximum()
    {
        var world = Create();
        TestWorlds.Give(world, Tonic);
        TestWorlds.Give(world, Tonic);
        world.Player.Life = 10;
        Assert.Equal(20, Assert.Single(Tick(world, new UseBeltCommand(0)).OfType<PotionDrunk>()).Amount);
        Assert.Equal(30, world.Player.Life);
        Assert.Null(world.Belongings.Belt.Get(0));
        world.Player.Life = world.Player.Profile.MaxLife - 5;
        Assert.Equal(5, Assert.Single(Tick(world, new UseBeltCommand(1)).OfType<PotionDrunk>()).Amount);
        Assert.Equal(world.Player.Profile.MaxLife, world.Player.Life);
    }

    [Fact]
    public void UseBelt_BadSlots_AreRejected()
    {
        var world = Create();
        Assert.Equal("that belt slot is empty", Rejection(Tick(world, new UseBeltCommand(0))));
        Assert.Equal("belt slot must be between 0 and 7", Rejection(Tick(world, new UseBeltCommand(8))));
    }

    [Fact]
    public void PutInBelt_MovesPotionsOnly()
    {
        var world = Create();
        var potions = Enumerable.Range(0, 9).Select(_ => TestWorlds.Give(world, Tonic)).ToList();
        var knife = TestWorlds.Give(world, Knife);
        Assert.Equal("only potions go in the belt", Rejection(Tick(world, new PutInBeltCommand(knife.Id, 0))));
        Assert.Equal("that belt slot is taken", Rejection(Tick(world, new PutInBeltCommand(potions[8].Id, 0))));
        Tick(world, new UseBeltCommand(3));
        Assert.Single(Tick(world, new PutInBeltCommand(potions[8].Id, 3)).OfType<ItemBelted>());
        Assert.Same(potions[8], world.Belongings.Belt.Get(3));
        Assert.Null(world.Belongings.Inventory.Find(potions[8].Id));
        Tick(world, new UseBeltCommand(5));
        Tick(world, new PutInBeltCommand(potions[0].Id, 5));
        Assert.Null(world.Belongings.Belt.Get(0));
        Assert.Same(potions[0], world.Belongings.Belt.Get(5));
    }

    private static World Create() => TestWorlds.Create(
        TestGameContent.Valid(items: TestGameContent.Items(new[] { Knife, Sword, Maul, Vest, Ring, Tonic })));

    private static IReadOnlyList<GameEvent> Tick(World world, Command command) => world.Tick(new[] { command });

    private static string Rejection(IReadOnlyList<GameEvent> events) => Assert.Single(events.OfType<CommandRejected>()).Reason;
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~InventoryCommandTests"`
Expected: build FAILS (`EquipCommand` does not exist).

- [ ] **Step 3: Add the commands and events**

Append to `src/Mortar.Game/Commands/ItemCommands.cs` (add `using Mortar.Game.Items;` at the top):

```csharp
public sealed class EquipCommand : Command
{
    public EquipCommand(int itemId)
    {
        ItemId = itemId;
    }

    public int ItemId { get; }

    public override string Describe() => $"equip item {ItemId}";
}

public sealed class UnequipCommand : Command
{
    public UnequipCommand(EquipmentSlot slot)
    {
        Slot = slot;
    }

    public EquipmentSlot Slot { get; }

    public override string Describe() => $"unequip {Slot}";
}

public sealed class DropItemCommand : Command
{
    public DropItemCommand(int itemId)
    {
        ItemId = itemId;
    }

    public int ItemId { get; }

    public override string Describe() => $"drop item {ItemId}";
}

public sealed class MoveItemCommand : Command
{
    public MoveItemCommand(int itemId, Cell to)
    {
        ItemId = itemId;
        To = to;
    }

    public int ItemId { get; }

    public Cell To { get; }

    public override string Describe() => $"move item {ItemId} to {To}";
}

public sealed class PutInBeltCommand : Command
{
    public PutInBeltCommand(int itemId, int slot)
    {
        ItemId = itemId;
        Slot = slot;
    }

    public int ItemId { get; }

    public int Slot { get; }

    public override string Describe() => $"put item {ItemId} in belt slot {Slot}";
}

public sealed class UseBeltCommand : Command
{
    public UseBeltCommand(int slot)
    {
        Slot = slot;
    }

    public int Slot { get; }

    public override string Describe() => $"use belt slot {Slot}";
}
```

Append to `src/Mortar.Game/Events/ItemEvents.cs` (add `using Mortar.Game.Items;` at the top):

```csharp
public sealed class ItemEquipped : GameEvent
{
    public ItemEquipped(long tick, int itemId, EquipmentSlot slot)
        : base(tick)
    {
        ItemId = itemId;
        Slot = slot;
    }

    public int ItemId { get; }

    public EquipmentSlot Slot { get; }

    protected override string Describe() => $"equipped item {ItemId} as {Slot}";
}

public sealed class ItemUnequipped : GameEvent
{
    public ItemUnequipped(long tick, int itemId, EquipmentSlot slot)
        : base(tick)
    {
        ItemId = itemId;
        Slot = slot;
    }

    public int ItemId { get; }

    public EquipmentSlot Slot { get; }

    protected override string Describe() => $"unequipped item {ItemId} from {Slot}";
}

public sealed class ItemMoved : GameEvent
{
    public ItemMoved(long tick, int itemId, Cell cell)
        : base(tick)
    {
        ItemId = itemId;
        Cell = cell;
    }

    public int ItemId { get; }

    public Cell Cell { get; }

    protected override string Describe() => $"moved item {ItemId} to {Cell}";
}

public sealed class ItemBelted : GameEvent
{
    public ItemBelted(long tick, int itemId, int slot)
        : base(tick)
    {
        ItemId = itemId;
        Slot = slot;
    }

    public int ItemId { get; }

    public int Slot { get; }

    protected override string Describe() => $"put item {ItemId} in belt slot {Slot}";
}

public sealed class PotionDrunk : GameEvent
{
    public PotionDrunk(long tick, int itemId, string restores, int amount)
        : base(tick)
    {
        ItemId = itemId;
        Restores = restores;
        Amount = amount;
    }

    public int ItemId { get; }

    public string Restores { get; }

    public int Amount { get; }

    protected override string Describe() => $"drank item {ItemId}, restoring {Amount} {Restores}";
}
```

- [ ] **Step 4: Implement the actions**

`src/Mortar.Game/Simulation/EquipActions.cs`:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class EquipActions
{
    public static string? Equip(World world, int itemId, EventSink events)
    {
        var belongings = world.Belongings;
        var stored = belongings.Inventory.Find(itemId);
        if (stored is null)
        {
            return belongings.Equipment.SlotOf(itemId) is null ? "no such item in the inventory" : "item is already equipped";
        }
        var item = stored.Item;
        if (!item.Definition.IsEquippable) return "item cannot be equipped";
        var missing = MissingRequirement(world.Progress.BaseStats, item.Definition.Requirements);
        if (missing is not null) return missing;
        var slot = belongings.Equipment.SlotFor(item.Definition);
        var replaced = belongings.Equipment.Get(slot);
        belongings.Inventory.Remove(itemId);
        if (replaced is not null && !belongings.Inventory.TryAdd(replaced))
        {
            belongings.Inventory.TryPlace(item, stored.Cell);
            return "no room in the inventory for the item being replaced";
        }
        belongings.Equipment.Set(slot, item);
        world.RefreshPlayer();
        if (replaced is not null) events.Add(new ItemUnequipped(events.Tick, replaced.Id, slot));
        events.Add(new ItemEquipped(events.Tick, item.Id, slot));
        return null;
    }

    public static string? Unequip(World world, EquipmentSlot slot, EventSink events)
    {
        if (!Equipment.Slots.Contains(slot)) return "unknown equipment slot";
        var belongings = world.Belongings;
        var item = belongings.Equipment.Get(slot);
        if (item is null) return "nothing is equipped there";
        if (!belongings.Inventory.TryAdd(item)) return PickupSystem.InventoryFull;
        belongings.Equipment.Set(slot, null);
        world.RefreshPlayer();
        events.Add(new ItemUnequipped(events.Tick, item.Id, slot));
        return null;
    }

    private static string? MissingRequirement(Stats stats, Stats requirements)
    {
        foreach (var kind in Stats.Kinds)
        {
            var needed = requirements.Get(kind);
            if (stats.Get(kind) < needed) return $"requires {needed} {kind.ToString().ToLowerInvariant()}";
        }
        return null;
    }
}
```

`src/Mortar.Game/Simulation/InventoryActions.cs`:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class InventoryActions
{
    public static string? Drop(World world, int itemId, EventSink events)
    {
        var item = world.Belongings.Take(itemId, out var fromSlot);
        if (item is null) return "no such item";
        if (fromSlot is { } slot)
        {
            world.RefreshPlayer();
            events.Add(new ItemUnequipped(events.Tick, item.Id, slot));
        }
        LootSystem.Place(world, world.Player.Cell, item, 0, events);
        return null;
    }

    public static string? Move(World world, int itemId, Cell to, EventSink events)
    {
        var belongings = world.Belongings;
        var stored = belongings.Inventory.Find(itemId);
        var beltSlot = belongings.Belt.IndexOf(itemId);
        var item = stored?.Item ?? belongings.Belt.Get(beltSlot);
        if (item is null) return "no such item in the inventory or belt";
        if (!belongings.Inventory.Fits(item.Definition, to, itemId)) return "item does not fit there";
        if (stored is not null) belongings.Inventory.Remove(itemId);
        else belongings.Belt.Take(beltSlot);
        belongings.Inventory.TryPlace(item, to);
        events.Add(new ItemMoved(events.Tick, item.Id, to));
        return null;
    }

    public static string? PutInBelt(World world, int itemId, int slot, EventSink events)
    {
        if (slot < 0 || slot >= Belt.Slots) return BadSlot;
        var belongings = world.Belongings;
        var current = belongings.Belt.IndexOf(itemId);
        var item = belongings.Inventory.Find(itemId)?.Item ?? belongings.Belt.Get(current);
        if (item is null) return "no such item in the inventory or belt";
        if (!item.Definition.IsPotion) return "only potions go in the belt";
        if (belongings.Belt.Get(slot) is not null) return "that belt slot is taken";
        if (current >= 0) belongings.Belt.Take(current);
        else belongings.Inventory.Remove(itemId);
        belongings.Belt.TryPut(item, slot);
        events.Add(new ItemBelted(events.Tick, item.Id, slot));
        return null;
    }

    public static string? UseBelt(World world, int slot, EventSink events)
    {
        if (slot < 0 || slot >= Belt.Slots) return BadSlot;
        var item = world.Belongings.Belt.Take(slot);
        if (item is null) return "that belt slot is empty";
        var potion = item.Definition.Potion!;
        var player = world.Player;
        int restored;
        if (potion.Restores == PotionKinds.Life)
        {
            restored = Restorable(player.Life, player.Profile.MaxLife, potion.Amount);
            player.Life += restored;
        }
        else
        {
            restored = Restorable(player.Mana, player.Profile.MaxMana, potion.Amount);
            player.Mana += restored;
        }
        events.Add(new PotionDrunk(events.Tick, item.Id, potion.Restores, restored));
        return null;
    }

    private static string BadSlot => $"belt slot must be between 0 and {Belt.Slots - 1}";

    private static int Restorable(int current, int maximum, int amount) => Math.Max(0, Math.Min(amount, maximum - current));
}
```

Replace `CommandSystem.Execute` with:

```csharp
    private static string? Execute(World world, Command command, EventSink events)
    {
        switch (command)
        {
            case MoveCommand move:
                return Move(world, move);
            case AttackCommand attack:
                return Attack(world, attack);
            case SpendStatPointCommand spend:
                return PlayerGrowth.SpendPoint(world, spend.Stat, events);
            case UseStairsCommand _:
                return Stairs.Use(world, events);
            case PickUpCommand pickUp:
                return PickupSystem.Start(world, pickUp);
            case EquipCommand equip:
                return EquipActions.Equip(world, equip.ItemId, events);
            case UnequipCommand unequip:
                return EquipActions.Unequip(world, unequip.Slot, events);
            case DropItemCommand drop:
                return InventoryActions.Drop(world, drop.ItemId, events);
            case MoveItemCommand moveItem:
                return InventoryActions.Move(world, moveItem.ItemId, moveItem.To, events);
            case PutInBeltCommand putInBelt:
                return InventoryActions.PutInBelt(world, putInBelt.ItemId, putInBelt.Slot, events);
            case UseBeltCommand useBelt:
                return InventoryActions.UseBelt(world, useBelt.Slot, events);
            default:
                return "unsupported command";
        }
    }
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~InventoryCommandTests"`
Expected: PASS (12 tests). Then `dotnet test`; expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add src/Mortar.Game/Simulation/EquipActions.cs src/Mortar.Game/Simulation/InventoryActions.cs src/Mortar.Game/Commands/ItemCommands.cs src/Mortar.Game/Events/ItemEvents.cs src/Mortar.Game/Simulation/CommandSystem.cs tests/Mortar.Tests/Gameplay/InventoryCommandTests.cs tests/Mortar.Tests/Gameplay/Support/TestWorlds.cs
git commit -m "add equip, drop, move, and belt commands"
```

---

### Task 7: Item content files and loader

**Files:**
- Create: `content/items/*.json` (15 files), `content/affixes.json`, `content/loot.json`
- Modify: `content/game.json`, `content/classes/warrior.json`, all six `content/monsters/*.json`
- Create: `src/Mortar.Json/Content/ItemDtos.cs`, `ItemMapper.cs`, `ItemContentLoader.cs`
- Modify: `src/Mortar.Json/Content/ContentDtos.cs`, `ContentMapper.cs`, `GameContentLoader.cs`
- Modify: `docs/content.md`, `tests/Mortar.Tests/Snapshots/game-seed-7.txt` (regenerated)
- Test: `tests/Mortar.Tests/Json/GameContentLoaderTests.cs`

**Interfaces:**
- Consumes: `Fields`, `JsonFile.Read<T>(path, out AuthoringError? error)`, `SafePath.Resolve`, `GameContentLoader.LoadFolder`, `ItemCatalog`.
- Produces:
  - Manifest fields `items` (folder), `affixes` (file), `loot` (file); class field `startingItems` (optional list of item ids); monster field `loot` (optional loot table id).
  - `ItemMapper.ToItemBase(source, ItemBaseDto, errors)`, `ToAffix(source, index, AffixDto, errors)`, `ToLootTable(source, index, LootTableDto, errors)`.
  - `ItemContentLoader.LoadAffixes(manifest, directory, entry, errors)`, `LoadLootTables(manifest, directory, entry, errors)`.
- Item JSON: `id`, `name`, `icon`, `slot`, `size` (`width`, `height`), `itemLevel`, `dropWeight`, optional `requirements` (any of `strength`, `dexterity`, `vitality`, `magic`; missing ones are 0), optional `damage`, `swing` (`attackTicks`, `hitFrame`), `armor`, `blockChance`, `potion` (`restores`, `amount`).

- [ ] **Step 1: Write the content files**

`content/game.json`:

```json
{
  "rules": "rules.json",
  "classes": "classes",
  "monsters": "monsters",
  "levels": "levels.json",
  "items": "items",
  "affixes": "affixes.json",
  "loot": "loot.json"
}
```

In `content/classes/warrior.json`, add after `timings`:

```json
  "startingItems": ["iron-knife", "padded-vest", "healing-draught", "healing-draught"]
```

In each monster file, add a final `"loot"` field (after `"ai"`, or after `"minions"` where present): `zombie`, `skeleton`, and `carrion-bat` use `"loot": "common"`; `imp` uses `"swarm"`; `imp-chieftain` uses `"pack"`; `gravewarden` uses `"boss"`. In `zombie.json` the line must read exactly `"loot": "common"` (a loader test edits it).

Create one file per item in `content/items/`. All names are our own. Weapons:

```json
{
  "id": "iron-knife",
  "name": "Iron Knife",
  "icon": "iron-knife",
  "slot": "weapon",
  "size": { "width": 1, "height": 2 },
  "itemLevel": 1,
  "dropWeight": 6,
  "damage": { "min": 2, "max": 5 },
  "swing": { "attackTicks": 11, "hitFrame": 6 }
}
```

| File | name | slot | size | itemLevel | dropWeight | requirements | other fields |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `iron-knife.json` | Iron Knife | weapon | 1×2 | 1 | 6 | — | damage 2–5, swing 11/6 |
| `hatchet.json` | Hatchet | weapon | 1×2 | 2 | 5 | strength 20 | damage 3–8, swing 15/10 |
| `cudgel.json` | Cudgel | weapon | 1×3 | 3 | 4 | strength 26 | damage 4–9 |
| `broad-blade.json` | Broad Blade | weapon | 1×3 | 4 | 3 | strength 32, dexterity 18 | damage 6–12, swing 16/10 |
| `padded-vest.json` | Padded Vest | body | 2×3 | 1 | 6 | — | armor 3–6 |
| `studded-jerkin.json` | Studded Jerkin | body | 2×3 | 3 | 4 | strength 22 | armor 8–12 |
| `ring-hauberk.json` | Ring Hauberk | body | 2×3 | 4 | 2 | strength 34 | armor 14–20 |
| `leather-hood.json` | Leather Hood | head | 2×2 | 1 | 5 | — | armor 2–4 |
| `iron-pot-helm.json` | Iron Pot Helm | head | 2×2 | 3 | 3 | strength 24 | armor 5–8 |
| `plank-shield.json` | Plank Shield | shield | 2×2 | 1 | 5 | — | armor 2–4, blockChance 15 |
| `banded-buckler.json` | Banded Buckler | shield | 2×2 | 3 | 3 | strength 24 | armor 4–7, blockChance 22 |
| `copper-band.json` | Copper Band | ring | 1×1 | 2 | 2 | — | — |
| `bone-charm.json` | Bone Charm | amulet | 1×1 | 3 | 1 | — | — |
| `healing-draught.json` | Healing Draught | potion | 1×1 | 1 | 12 | — | potion: restores life, amount 30 |
| `mana-draught.json` | Mana Draught | potion | 1×1 | 1 | 4 | — | potion: restores mana, amount 20 |

Each file follows the `iron-knife.json` shape, with `"icon"` equal to the id. Requirements are written as an object, for example `"requirements": { "strength": 32, "dexterity": 18 }`; armor as `"armor": { "min": 3, "max": 6 }`; potions as `"potion": { "restores": "life", "amount": 30 }`. `padded-vest.json` must contain the line `"itemLevel": 1,` exactly (a loader test removes it).

`content/affixes.json` (all six equippable slots are written out where the table says "all"):

```json
{
  "affixes": [
    { "id": "hardened", "name": "Hardened", "position": "prefix", "slots": ["body", "head", "shield"], "itemLevel": { "min": 1, "max": 3 }, "weight": 10, "bonus": "armorClass", "value": { "min": 2, "max": 4 } },
    { "id": "tempered", "name": "Tempered", "position": "prefix", "slots": ["body", "head", "shield"], "itemLevel": { "min": 3, "max": 6 }, "weight": 6, "bonus": "armorClass", "value": { "min": 5, "max": 8 } },
    { "id": "keen", "name": "Keen", "position": "prefix", "slots": ["weapon", "ring", "amulet"], "itemLevel": { "min": 1, "max": 3 }, "weight": 10, "bonus": "toHit", "value": { "min": 3, "max": 6 } },
    { "id": "honed", "name": "Honed", "position": "prefix", "slots": ["weapon", "ring", "amulet"], "itemLevel": { "min": 3, "max": 6 }, "weight": 6, "bonus": "toHit", "value": { "min": 7, "max": 12 } },
    { "id": "barbed", "name": "Barbed", "position": "prefix", "slots": ["weapon"], "itemLevel": { "min": 1, "max": 4 }, "weight": 8, "bonus": "damage", "value": { "min": 1, "max": 2 } },
    { "id": "serrated", "name": "Serrated", "position": "prefix", "slots": ["weapon"], "itemLevel": { "min": 4, "max": 6 }, "weight": 4, "bonus": "damage", "value": { "min": 3, "max": 4 } },
    { "id": "hale", "name": "Hale", "position": "prefix", "slots": ["body", "ring", "amulet"], "itemLevel": { "min": 1, "max": 6 }, "weight": 8, "bonus": "life", "value": { "min": 5, "max": 10 } },
    { "id": "of-brawn", "name": "of Brawn", "position": "suffix", "slots": ["weapon", "body", "head", "shield", "ring", "amulet"], "itemLevel": { "min": 1, "max": 3 }, "weight": 10, "bonus": "strength", "value": { "min": 2, "max": 4 } },
    { "id": "of-great-brawn", "name": "of Great Brawn", "position": "suffix", "slots": ["weapon", "body", "head", "shield", "ring", "amulet"], "itemLevel": { "min": 3, "max": 6 }, "weight": 5, "bonus": "strength", "value": { "min": 5, "max": 8 } },
    { "id": "of-deftness", "name": "of Deftness", "position": "suffix", "slots": ["weapon", "body", "head", "shield", "ring", "amulet"], "itemLevel": { "min": 1, "max": 3 }, "weight": 10, "bonus": "dexterity", "value": { "min": 2, "max": 4 } },
    { "id": "of-hearth", "name": "of Hearth", "position": "suffix", "slots": ["weapon", "body", "head", "shield", "ring", "amulet"], "itemLevel": { "min": 1, "max": 3 }, "weight": 10, "bonus": "vitality", "value": { "min": 2, "max": 4 } },
    { "id": "of-lore", "name": "of Lore", "position": "suffix", "slots": ["head", "ring", "amulet"], "itemLevel": { "min": 1, "max": 6 }, "weight": 6, "bonus": "magic", "value": { "min": 2, "max": 4 } },
    { "id": "of-wells", "name": "of Wells", "position": "suffix", "slots": ["head", "ring", "amulet"], "itemLevel": { "min": 1, "max": 6 }, "weight": 6, "bonus": "mana", "value": { "min": 5, "max": 10 } }
  ]
}
```

`content/loot.json`:

```json
{
  "tables": [
    { "id": "swarm", "rolls": 1, "noneWeight": 75, "goldWeight": 20, "itemWeight": 5, "gold": { "min": 2, "max": 6 }, "magicPercent": 10 },
    { "id": "common", "rolls": 1, "noneWeight": 55, "goldWeight": 30, "itemWeight": 15, "gold": { "min": 4, "max": 14 }, "magicPercent": 15 },
    { "id": "pack", "rolls": 2, "noneWeight": 45, "goldWeight": 30, "itemWeight": 25, "gold": { "min": 8, "max": 20 }, "magicPercent": 20 },
    { "id": "boss", "rolls": 4, "noneWeight": 0, "goldWeight": 1, "itemWeight": 3, "gold": { "min": 60, "max": 140 }, "magicPercent": 60 }
  ]
}
```

- [ ] **Step 2: Write the failing tests**

Add to `tests/Mortar.Tests/Json/GameContentLoaderTests.cs` (with `using Mortar.Game.Items;`):

```csharp
    [Fact]
    public void RepoContent_HasItemsAffixesAndLoot()
    {
        var content = GameContentLoader.Load(ContentFixture.Manifest).Value!.Content;
        Assert.Equal(15, content.ItemBases.Count);
        Assert.Equal(13, content.Affixes.Count);
        Assert.Equal(4, content.Items.LootTables.Count);
        Assert.Equal("common", content.Monster("zombie").LootTableId);
        Assert.True(content.TryGetClass("warrior", out var warrior));
        Assert.Equal(new[] { "iron-knife", "padded-vest", "healing-draught", "healing-draught" }, warrior!.StartingItems);
    }

    [Fact]
    public void RepoContent_StartsTheWarriorEquipped()
    {
        var content = GameContentLoader.Load(ContentFixture.Manifest).Value!;
        var world = World.Create(content.Content, content.CreateLevelGenerator(), "warrior", 1).World!;
        Assert.Equal("iron-knife", world.Belongings.Equipment.Get(EquipmentSlot.Weapon)!.Definition.Id);
        Assert.Equal("padded-vest", world.Belongings.Equipment.Get(EquipmentSlot.Body)!.Definition.Id);
        Assert.Equal("healing-draught", world.Belongings.Belt.Get(1)!.Definition.Id);
    }

    [Fact]
    public void MissingItemField_NamesTheFile()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var vest = Path.Combine(temp.Root, "items", "padded-vest.json");
        File.WriteAllText(vest, File.ReadAllText(vest).Replace("\"itemLevel\": 1,", ""));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == vest && e.Message == "'itemLevel' is required");
    }

    [Fact]
    public void NullAffixEntry_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var affixes = Path.Combine(temp.Root, "affixes.json");
        File.WriteAllText(affixes, File.ReadAllText(affixes).Replace("\"affixes\": [", "\"affixes\": [null,"));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == affixes && e.Message == "affixes entry 0 is null");
    }

    [Fact]
    public void UnknownLootTable_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var zombie = Path.Combine(temp.Root, "monsters", "zombie.json");
        File.WriteAllText(zombie, File.ReadAllText(zombie).Replace("\"loot\": \"common\"", "\"loot\": \"epic\""));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == "monster 'zombie'" && e.Message == "loot references unknown loot table 'epic'");
    }

    [Fact]
    public void MissingItemsFolder_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        Directory.Delete(Path.Combine(temp.Root, "items"), recursive: true);
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == manifest && e.Message == "'items' must name an existing folder");
    }
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~GameContentLoaderTests"`
Expected: FAIL. Every load of the repo content reports `invalid JSON` errors for the new `items`, `affixes`, `loot`, `startingItems`, and `loot` fields, because the loader does not know them yet.

- [ ] **Step 4: Add the DTOs**

Add to `ManifestDto` in `src/Mortar.Json/Content/ContentDtos.cs`:

```csharp
    public string? Items { get; set; }

    public string? Affixes { get; set; }

    public string? Loot { get; set; }
```

Add `public List<string?>? StartingItems { get; set; }` to `ClassDto` and `public string? Loot { get; set; }` to `MonsterDto`.

`src/Mortar.Json/Content/ItemDtos.cs`:

```csharp
namespace Mortar.Json.Content;

internal sealed class SizeDto
{
    public int? Width { get; set; }

    public int? Height { get; set; }
}

internal sealed class SwingDto
{
    public int? AttackTicks { get; set; }

    public int? HitFrame { get; set; }
}

internal sealed class PotionDto
{
    public string? Restores { get; set; }

    public int? Amount { get; set; }
}

internal sealed class ItemBaseDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public string? Slot { get; set; }

    public SizeDto? Size { get; set; }

    public int? ItemLevel { get; set; }

    public int? DropWeight { get; set; }

    public StatsDto? Requirements { get; set; }

    public RangeDto? Damage { get; set; }

    public SwingDto? Swing { get; set; }

    public RangeDto? Armor { get; set; }

    public int? BlockChance { get; set; }

    public PotionDto? Potion { get; set; }
}

internal sealed class AffixListDto
{
    public List<AffixDto?>? Affixes { get; set; }
}

internal sealed class AffixDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Position { get; set; }

    public List<string?>? Slots { get; set; }

    public RangeDto? ItemLevel { get; set; }

    public int? Weight { get; set; }

    public string? Bonus { get; set; }

    public RangeDto? Value { get; set; }
}

internal sealed class LootListDto
{
    public List<LootTableDto?>? Tables { get; set; }
}

internal sealed class LootTableDto
{
    public string? Id { get; set; }

    public int? Rolls { get; set; }

    public int? NoneWeight { get; set; }

    public int? GoldWeight { get; set; }

    public int? ItemWeight { get; set; }

    public RangeDto? Gold { get; set; }

    public int? MagicPercent { get; set; }
}
```

- [ ] **Step 5: Map and load the item files**

`src/Mortar.Json/Content/ItemMapper.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Game.Content;

namespace Mortar.Json.Content;

internal static class ItemMapper
{
    public static ItemBaseDefinition ToItemBase(string source, ItemBaseDto dto, List<AuthoringError> errors)
    {
        var fields = new Fields(source, errors);
        var size = fields.Present(dto.Size, "size") ? dto.Size! : new SizeDto { Width = 0, Height = 0 };
        return new ItemBaseDefinition(
            fields.Text(dto.Id, "id"),
            fields.Text(dto.Name, "name"),
            fields.Text(dto.Icon, "icon"),
            fields.Text(dto.Slot, "slot"),
            fields.Int(size.Width, "size.width"),
            fields.Int(size.Height, "size.height"),
            fields.Int(dto.ItemLevel, "itemLevel"),
            fields.Int(dto.DropWeight, "dropWeight"),
            ToRequirements(dto.Requirements),
            dto.Damage is null ? null : (IntRange?)fields.ReadRange(dto.Damage, "damage"),
            ToSwing(fields, dto.Swing),
            dto.Armor is null ? null : (IntRange?)fields.ReadRange(dto.Armor, "armor"),
            dto.BlockChance ?? 0,
            ToPotion(fields, dto.Potion));
    }

    public static AffixDefinition ToAffix(string source, int index, AffixDto dto, List<AuthoringError> errors)
    {
        var fields = new Fields(source, errors, $"affixes entry {index}");
        return new AffixDefinition(
            fields.Text(dto.Id, "id"),
            fields.Text(dto.Name, "name"),
            fields.Text(dto.Position, "position"),
            ToSlots(fields, dto.Slots),
            fields.ReadRange(dto.ItemLevel, "itemLevel"),
            fields.Int(dto.Weight, "weight"),
            fields.Text(dto.Bonus, "bonus"),
            fields.ReadRange(dto.Value, "value"));
    }

    public static LootTableDefinition ToLootTable(string source, int index, LootTableDto dto, List<AuthoringError> errors)
    {
        var fields = new Fields(source, errors, $"tables entry {index}");
        return new LootTableDefinition(
            fields.Text(dto.Id, "id"),
            fields.Int(dto.Rolls, "rolls"),
            dto.NoneWeight ?? 0,
            dto.GoldWeight ?? 0,
            dto.ItemWeight ?? 0,
            dto.Gold is null ? default : fields.ReadRange(dto.Gold, "gold"),
            dto.MagicPercent ?? 0);
    }

    private static Stats ToRequirements(StatsDto? dto) =>
        dto is null ? default : new Stats(dto.Strength ?? 0, dto.Dexterity ?? 0, dto.Vitality ?? 0, dto.Magic ?? 0);

    private static WeaponSwing? ToSwing(Fields fields, SwingDto? dto) =>
        dto is null ? null : new WeaponSwing(fields.Int(dto.AttackTicks, "swing.attackTicks"), fields.Int(dto.HitFrame, "swing.hitFrame"));

    private static PotionEffect? ToPotion(Fields fields, PotionDto? dto) =>
        dto is null ? null : new PotionEffect(fields.Text(dto.Restores, "potion.restores"), fields.Int(dto.Amount, "potion.amount"));

    private static IReadOnlyList<string> ToSlots(Fields fields, List<string?>? slots) =>
        fields.Present(slots, "slots")
            ? slots!.Select((slot, i) => fields.Text(slot, $"slots[{i}]")).ToList()
            : new List<string>();
}
```

`src/Mortar.Json/Content/ItemContentLoader.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Game.Content;

namespace Mortar.Json.Content;

internal static class ItemContentLoader
{
    public static List<AffixDefinition> LoadAffixes(string manifest, string directory, string? entry, List<AuthoringError> errors) =>
        LoadList<AffixListDto, AffixDto, AffixDefinition>(
            manifest, directory, entry, "affixes", "affixes", file => file.Affixes, ItemMapper.ToAffix, errors);

    public static List<LootTableDefinition> LoadLootTables(string manifest, string directory, string? entry, List<AuthoringError> errors) =>
        LoadList<LootListDto, LootTableDto, LootTableDefinition>(
            manifest, directory, entry, "loot", "tables", file => file.Tables, ItemMapper.ToLootTable, errors);

    private static List<TDefinition> LoadList<TFile, TEntry, TDefinition>(
        string manifest,
        string directory,
        string? entry,
        string field,
        string listName,
        Func<TFile, List<TEntry?>?> entries,
        Func<string, int, TEntry, List<AuthoringError>, TDefinition> map,
        List<AuthoringError> errors)
        where TFile : class
        where TEntry : class
    {
        var definitions = new List<TDefinition>();
        var path = SafePath.Resolve(directory, entry);
        if (path is null)
        {
            errors.Add(new AuthoringError(manifest, $"'{field}' is empty or an invalid path"));
            return definitions;
        }
        var file = JsonFile.Read<TFile>(path, out var error);
        if (file is null)
        {
            errors.Add(error!);
            return definitions;
        }
        var list = entries(file);
        if (list is null)
        {
            errors.Add(new AuthoringError(path, $"'{listName}' is required"));
            return definitions;
        }
        for (var i = 0; i < list.Count; i++)
        {
            var dto = list[i];
            if (dto is null) errors.Add(new AuthoringError(path, $"{listName} entry {i} is null"));
            else definitions.Add(map(path, i, dto, errors));
        }
        return definitions;
    }
}
```

In `ContentMapper.ToClass`, replace the temporary `Array.Empty<string>()` with `ToStartingItems(fields, dto.StartingItems)` and add:

```csharp
    private static IReadOnlyList<string> ToStartingItems(Fields fields, List<string?>? ids) =>
        ids is null ? Array.Empty<string>() : ids.Select((id, i) => fields.Text(id, $"startingItems[{i}]")).ToList();
```

In `ContentMapper.ToMonster`, replace the temporary `null` with `string.IsNullOrWhiteSpace(dto.Loot) ? null : dto.Loot`.

In `GameContentLoader.Load`, load the item files after the monsters and pass a real catalog:

```csharp
        var items = LoadFolder<ItemBaseDto, ItemBaseDefinition>(source, directory, manifest.Items, "items", ItemMapper.ToItemBase, errors);
        var affixes = ItemContentLoader.LoadAffixes(source, directory, manifest.Affixes, errors);
        var loot = ItemContentLoader.LoadLootTables(source, directory, manifest.Loot, errors);
```

```csharp
        var content = ContentSet.Create(rules, classes, monsters, levels, new ItemCatalog(items, affixes, loot));
```

- [ ] **Step 6: Document the content**

In `docs/content.md`:
- in the first paragraph's number limit, say "Monster, class, and item numbers …";
- change the `game.json` sentence to list `rules` (file), `classes` (folder), `monsters` (folder), `levels` (file), `items` (folder), `affixes` (file), and `loot` (file);
- append to the `classes/*.json` section: "Optional `startingItems` lists item ids the class starts with. Each is created plain (no affixes, armor at its maximum). Equippable items fill empty slots; potions go to the belt; the rest go to the inventory. Items that don't fit are left out.";
- append to the `monsters/*.json` field list: "and optional `loot` (a loot table id; monsters without one drop nothing)";
- add these sections at the end:

```markdown
## `items/*.json`

One item base per file: `id`, `name`, `icon`, `slot`, `size` (`width` 1–2, `height` 1–3), `itemLevel` (only monsters of at least this level drop it), `dropWeight` (0 never drops), and optional `requirements` (`strength`, `dexterity`, `vitality`, `magic`; missing ones are 0, compared against the player's base stats).

`slot` is one of `weapon`, `body`, `head`, `shield`, `ring`, `amulet`, `potion`. Rings, amulets, and potions are 1×1.

- Weapons need `damage` (`min`, `max`), which replaces the class's unarmed damage. Optional `swing` (`attackTicks`, `hitFrame`) replaces the class's attack timings.
- Body, head, and shield items need `armor` (`min`, `max`); each dropped item rolls its armor value once. Shields may have `blockChance` (percent).
- Potions need `potion` (`restores`: `life` or `mana`, and `amount`).

The inventory is 10×4 cells and the belt holds 8 potions. Gold is a counter, not an item.

## `affixes.json`

`affixes` is a list. Each affix has `id`, `name` (shown before the base name for prefixes, after it for suffixes), `position` (`prefix` or `suffix`), `slots` (equippable slots it can appear on), `itemLevel` (`min`, `max`), `weight`, `bonus`, and `value` (`min`, `max`; rolled once per item).

`bonus` is one of `strength`, `dexterity`, `vitality`, `magic`, `life`, `mana`, `armorClass`, `toHit`, `damage` (added to both ends of the damage range).

## `loot.json`

`tables` is a list. Each table has `id`, `rolls` (1–10), `noneWeight`, `goldWeight`, `itemWeight` (each roll picks one of nothing, a gold pile, or an item by these weights), `gold` (`min`, `max`; required when `goldWeight` is above 0), and `magicPercent` (the chance that an item is magic).

A magic item rolls a prefix, a suffix, or both (equal odds), from affixes allowed on its slot at its item level. Potions are never magic. Drops land on the nearest floor cells around the corpse, up to 3 tiles away.
```

- [ ] **Step 7: Run the loader tests**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~GameContentLoaderTests"`
Expected: PASS.

- [ ] **Step 8: Regenerate the determinism snapshot**

The warrior now starts with a knife and vest, and monsters drop loot, so the scripted run changes. Delete `tests/Mortar.Tests/Snapshots/game-seed-7.txt` and run `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~DeterminismTests"`. `ScriptedRun_MatchesSnapshot` fails once, saying it created the snapshot. Read the new file: it should show the player attacking monsters, `item … dropped at` and gold lines after kills, and no `rejected` lines. Run the same command again; expected: PASS. Then `dotnet test`; expected: all pass.

- [ ] **Step 9: Commit**

```bash
git add content/game.json content/classes/warrior.json content/monsters content/items content/affixes.json content/loot.json src/Mortar.Json/Content docs/content.md tests/Mortar.Tests/Json/GameContentLoaderTests.cs tests/Mortar.Tests/Snapshots/game-seed-7.txt
git commit -m "add item content files and loader"
```

---

### Task 8: Hash coverage and a looting scripted player

**Files:**
- Modify: `src/Mortar.Game/Simulation/WorldHash.cs`
- Modify: `tests/Mortar.Tests/Gameplay/Support/ScriptedPlayer.cs`, `tests/Mortar.Tests/Gameplay/DeterminismTests.cs`, `tests/Mortar.Tests/Snapshots/game-seed-7.txt` (regenerated)

**Interfaces:**
- Consumes: `World.Belongings`, `World.NextItemId`, `LevelState.GroundItems`, `Movement.IsReachable`, the item commands.
- Produces:
  - `WorldHash.Compute` additionally covers `NextItemId`, gold, every inventory item with its cell, every belt slot, every equipment slot, and every ground item on every level (id, cell, gold, base, item level, armor value, affix ids and values).
  - `ScriptedPlayer.Play(World world, int ticks)` → event log lines (used by the save round-trip tests in Tasks 10 and 11). The bot drinks a life potion below 40% life, equips found gear into empty slots, and picks up reachable loot within 8 tiles when it has room.

- [ ] **Step 1: Write the failing tests**

In `tests/Mortar.Tests/Gameplay/DeterminismTests.cs`, add `using Mortar.Game.Content;`, `using Mortar.Game.Items;`, and replace the hash theory with:

```csharp
    [Theory]
    [InlineData("rng")]
    [InlineData("monster life")]
    [InlineData("player intent")]
    [InlineData("unspent points")]
    [InlineData("next actor id")]
    [InlineData("gold")]
    [InlineData("item cell")]
    [InlineData("equipment")]
    [InlineData("belt")]
    [InlineData("ground item")]
    [InlineData("next item id")]
    public void WorldHash_ChangesWhenOnePieceOfStateChanges(string change)
    {
        var knife = TestGameContent.Weapon();
        var tonic = TestGameContent.Potion();
        var world = TestWorlds.Create(TestGameContent.Valid(items: TestGameContent.Items(new[] { knife, tonic })));
        var zombie = TestWorlds.Spawn(world, "zombie", 5, 3);
        var item = TestWorlds.Give(world, knife);
        var potion = TestWorlds.Give(world, tonic);
        var before = WorldHash.Compute(world);
        Assert.Equal(before, WorldHash.Compute(world));
        Mutate(world, zombie, item, potion, change);
        Assert.NotEqual(before, WorldHash.Compute(world));
    }

    [Fact]
    public void ScriptedRun_PicksUpLoot()
    {
        var run = ScriptedPlayer.Run(Content(), 7, RunTicks);
        Assert.Contains(run.Log, line => line.Contains("picked up"));
    }

    private static void Mutate(World world, Actor zombie, Item item, Item potion, string change)
    {
        var belongings = world.Belongings;
        switch (change)
        {
            case "rng": world.Rng.NextInt(100); break;
            case "monster life": zombie.Life--; break;
            case "player intent": world.Player.SetMoveIntent(new Cell(3, 1)); break;
            case "unspent points": world.Progress.UnspentPoints++; break;
            case "next actor id": world.NextActorId++; break;
            case "gold": belongings.Gold++; break;
            case "item cell":
                belongings.Inventory.Remove(item.Id);
                belongings.Inventory.TryPlace(item, new Cell(5, 0));
                break;
            case "equipment":
                belongings.Inventory.Remove(item.Id);
                belongings.Equipment.Set(EquipmentSlot.Weapon, item);
                break;
            case "belt":
                belongings.Belt.Take(0);
                belongings.Belt.TryPut(potion, 4);
                break;
            case "ground item": world.Level.AddGroundItem(new GroundItem(99, new Cell(3, 3), null, 5)); break;
            case "next item id": world.NextItemId++; break;
        }
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~DeterminismTests"`
Expected: FAIL. The hash cases `gold`, `item cell`, `equipment`, `belt`, `ground item`, and `next item id` fail because the hash ignores items. `ScriptedRun_PicksUpLoot` may pass or fail, depending on whether the current bot happens to walk over loot (it never sends `PickUp`, so expect a fail).

- [ ] **Step 3: Hash the items**

Replace `src/Mortar.Game/Simulation/WorldHash.cs` with:

```csharp
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

public static class WorldHash
{
    public static string Compute(World world)
    {
        var hasher = new StateHasher();
        hasher.Add(world.Seed).Add(world.TickCount).Add((long)world.Depth).Add((long)world.NextActorId).Add((long)world.NextItemId);
        hasher.Add(world.Rng.State).Add(world.Rng.Increment);
        AddProgress(hasher, world.Progress);
        AddBelongings(hasher, world.Belongings);
        foreach (var depth in world.VisitedDepths)
        {
            var level = world.GetLevel(depth);
            hasher.Add((long)level.Depth).Add(level.Seed).Add(level.Layout.Fingerprint);
            foreach (var actor in level.Actors) AddActor(hasher, actor);
            hasher.Add((long)level.GroundItems.Count);
            foreach (var ground in level.GroundItems) AddGround(hasher, ground);
        }
        return hasher.Value.ToString("x16");
    }

    private static void AddProgress(StateHasher hasher, PlayerProgress progress)
    {
        var stats = progress.BaseStats;
        hasher.Add((long)progress.Level).Add(progress.Experience).Add((long)progress.UnspentPoints);
        hasher.Add((long)stats.Strength).Add((long)stats.Dexterity).Add((long)stats.Vitality).Add((long)stats.Magic);
    }

    private static void AddActor(StateHasher hasher, Actor actor)
    {
        hasher.Add((long)actor.Id).Add((long)actor.Kind).Add(actor.DefinitionId);
        hasher.Add((long)actor.Cell.X).Add((long)actor.Cell.Y).Add((long)actor.Facing);
        hasher.Add((long)actor.Life).Add((long)actor.Mana).Add(actor.IsDead).Add(actor.IsAwake);
        hasher.Add((long)actor.LeaderId).Add((long)actor.LastAttackerId);
        hasher.Add((long)actor.Action).Add((long)actor.ActionElapsed).Add((long)actor.ActionDuration).Add((long)actor.ActionTargetId);
        hasher.Add((long)actor.Intent).Add((long)actor.IntentCell.X).Add((long)actor.IntentCell.Y).Add((long)actor.IntentTargetId);
        hasher.Add((long)actor.Profile.Level).Add((long)actor.Profile.MaxLife).Add((long)actor.Profile.MaxMana);
    }

    private static void AddBelongings(StateHasher hasher, Belongings belongings)
    {
        hasher.Add(belongings.Gold).Add((long)belongings.Inventory.Items.Count);
        foreach (var stored in belongings.Inventory.Items)
        {
            hasher.Add((long)stored.Cell.X).Add((long)stored.Cell.Y);
            AddItem(hasher, stored.Item);
        }
        for (var slot = 0; slot < Belt.Slots; slot++) AddOptional(hasher, belongings.Belt.Get(slot));
        foreach (var slot in Equipment.Slots) AddOptional(hasher, belongings.Equipment.Get(slot));
    }

    private static void AddGround(StateHasher hasher, GroundItem ground)
    {
        hasher.Add((long)ground.Id).Add((long)ground.Cell.X).Add((long)ground.Cell.Y).Add((long)ground.Gold);
        AddOptional(hasher, ground.Item);
    }

    private static void AddOptional(StateHasher hasher, Item? item)
    {
        hasher.Add(item is not null);
        if (item is not null) AddItem(hasher, item);
    }

    private static void AddItem(StateHasher hasher, Item item)
    {
        hasher.Add((long)item.Id).Add(item.Definition.Id).Add((long)item.ItemLevel).Add((long)item.ArmorValue);
        AddAffix(hasher, item.Prefix);
        AddAffix(hasher, item.Suffix);
    }

    private static void AddAffix(StateHasher hasher, RolledAffix? affix) =>
        hasher.Add(affix?.Definition.Id ?? "").Add((long)(affix?.Value ?? 0));
}
```

- [ ] **Step 4: Teach the scripted player to loot**

Replace the `ScriptedPlayer` class in `tests/Mortar.Tests/Gameplay/Support/ScriptedPlayer.cs` (keep `ScriptedRun`; add `using Mortar.Game.Items;`):

```csharp
/// <summary>A simple bot: drink when hurt, fight awake monsters nearby, spend points on strength, wear found gear in empty slots, pick up nearby loot, otherwise head for the exit and descend.</summary>
internal static class ScriptedPlayer
{
    private const int EngageDistance = 6;
    private const int LootDistance = 8;
    private const int DrinkBelowPercent = 40;

    public static ScriptedRun Run(GameContent content, uint seed, int ticks)
    {
        var created = World.Create(content.Content, content.CreateLevelGenerator(), "warrior", seed);
        var world = created.World ?? throw new InvalidOperationException(created.Error);
        var log = Play(world, ticks);
        return new ScriptedRun(log, WorldHash.Compute(world));
    }

    public static List<string> Play(World world, int ticks)
    {
        var log = new List<string>();
        for (var i = 0; i < ticks && !world.IsGameOver; i++)
        {
            log.AddRange(world.Tick(Decide(world)).Select(e => e.ToString()));
        }
        return log;
    }

    private static IReadOnlyList<Command> Decide(World world)
    {
        var player = world.Player;
        var drink = DrinkIfHurt(world);
        if (drink is not null) return new[] { drink };
        var target = NearestAwakeMonster(world);
        if (player.Intent == IntentKind.Attack)
        {
            return target is not null && ShouldSwitchTo(world, target) ? new Command[] { new AttackCommand(target.Id) } : Array.Empty<Command>();
        }
        if (target is not null) return new Command[] { new AttackCommand(target.Id) };
        if (player.Intent != IntentKind.None) return Array.Empty<Command>();
        var chore = SpendPoint(world) ?? EquipIntoEmptySlot(world) ?? PickUpNearbyLoot(world);
        if (chore is not null) return new[] { chore };
        var exit = world.Level.FindMarker(MarkerKinds.Exit);
        if (exit is null) return Array.Empty<Command>();
        return player.Cell.DistanceTo(exit.Value) <= 1
            ? new Command[] { new UseStairsCommand() }
            : new Command[] { new MoveCommand(exit.Value) };
    }

    private static Command? DrinkIfHurt(World world)
    {
        var player = world.Player;
        if (player.Life * 100 >= player.Profile.MaxLife * DrinkBelowPercent) return null;
        for (var slot = 0; slot < Belt.Slots; slot++)
        {
            if (world.Belongings.Belt.Get(slot)?.Definition.Potion?.Restores == PotionKinds.Life) return new UseBeltCommand(slot);
        }
        return null;
    }

    private static Command? SpendPoint(World world) =>
        world.Progress.UnspentPoints > 0 && world.Progress.BaseStats.Strength < world.PlayerClass.StatCaps.Strength
            ? new SpendStatPointCommand(StatKind.Strength)
            : null;

    private static Command? EquipIntoEmptySlot(World world)
    {
        var belongings = world.Belongings;
        foreach (var stored in belongings.Inventory.Items)
        {
            var definition = stored.Item.Definition;
            if (!definition.IsEquippable || !MeetsRequirements(world, definition)) continue;
            if (belongings.Equipment.Get(belongings.Equipment.SlotFor(definition)) is null) return new EquipCommand(stored.Item.Id);
        }
        return null;
    }

    private static bool MeetsRequirements(World world, ItemBaseDefinition definition) =>
        Stats.Kinds.All(kind => world.Progress.BaseStats.Get(kind) >= definition.Requirements.Get(kind));

    private static Command? PickUpNearbyLoot(World world)
    {
        var player = world.Player;
        GroundItem? best = null;
        foreach (var ground in world.Level.GroundItems)
        {
            var distance = ground.Cell.DistanceTo(player.Cell);
            if (distance > LootDistance || !HasRoomFor(world.Belongings, ground)) continue;
            if (best is not null && distance >= best.Cell.DistanceTo(player.Cell)) continue;
            if (Movement.IsReachable(world.Level, player.Cell, ground.Cell)) best = ground;
        }
        return best is null ? null : new PickUpCommand(best.Id);
    }

    private static bool HasRoomFor(Belongings belongings, GroundItem ground)
    {
        if (ground.Item is null) return true;
        var definition = ground.Item.Definition;
        return (definition.IsPotion && belongings.Belt.FirstEmpty() >= 0) || belongings.Inventory.FindSpace(definition) is not null;
    }

    /// <summary>Drop a target that is out of reach (for example boxed in by its pack) when another monster can be struck now.</summary>
    private static bool ShouldSwitchTo(World world, Actor target)
    {
        var player = world.Player;
        var current = world.Level.Find(player.IntentTargetId);
        return current is not null
            && !Reach.CanStrike(player.Cell, current.Cell, world.Level.IsWalkable)
            && Reach.CanStrike(player.Cell, target.Cell, world.Level.IsWalkable);
    }

    private static Actor? NearestAwakeMonster(World world)
    {
        Actor? best = null;
        foreach (var actor in world.Level.Actors)
        {
            if (actor.Kind != ActorKind.Monster || !actor.IsAlive || !actor.IsAwake) continue;
            var distance = actor.Cell.DistanceTo(world.Player.Cell);
            if (distance > EngageDistance) continue;
            if (best is null || distance < best.Cell.DistanceTo(world.Player.Cell)) best = actor;
        }
        return best;
    }
}
```

- [ ] **Step 5: Regenerate the snapshot and run everything**

Delete `tests/Mortar.Tests/Snapshots/game-seed-7.txt`, run `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~DeterminismTests"` once to recreate it, and read it: expect kills, drops, `picked up` lines, and possibly `equipped` and `drank` lines, and no `rejected` lines. If the log shows the bot repeating the same pickup without progress, stop and report it instead of accepting the snapshot. Run `dotnet test`; expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add src/Mortar.Game/Simulation/WorldHash.cs tests/Mortar.Tests/Gameplay/Support/ScriptedPlayer.cs tests/Mortar.Tests/Gameplay/DeterminismTests.cs tests/Mortar.Tests/Snapshots/game-seed-7.txt
git commit -m "hash items and teach the scripted player to loot"
```

---

### Task 9: World snapshots

**Files:**
- Create: `src/Mortar.Game/Saves/WorldSnapshot.cs`, `src/Mortar.Game/Saves/ItemSnapshots.cs`, `src/Mortar.Game/Saves/SnapshotCapture.cs`
- Test: `tests/Mortar.Tests/Gameplay/SaveCaptureTests.cs`

**Interfaces:**
- Consumes: everything a world holds: `World` (seed, tick, depth, id counters, random generator state, progress, belongings, visited levels), `LevelState` (depth, seed, layout fingerprint, actors, ground items), `Actor`, `Item`.
- Produces (namespace `Mortar.Game.Saves`; plain classes with public get/set properties so `Mortar.Json` can serialize them; every reference-typed member is nullable because a file may omit it):
  - `WorldSnapshot` (`CurrentVersion = 1`; `Version`, `Seed`, `Tick`, `Depth`, `NextActorId`, `NextItemId`, `RngState`, `RngIncrement`, `Player`, `Levels`; static `Capture(World)`).
  - `PlayerSnapshot` (`ClassId`, `Level`, `Experience`, `Stats`, `UnspentPoints`, `Gold`, `Actor`, `Inventory`, `Belt`, `Equipment`), `StatsSnapshot`, `LevelSnapshot` (`Depth`, `Seed`, `Fingerprint`, `Monsters`, `GroundItems`), `ActorSnapshot` (enums stored by name).
  - `ItemSnapshot` (`Id`, `Base`, `ItemLevel`, `Armor`, `Prefix`, `Suffix`), `AffixRollSnapshot` (`Id`, `Value`), `StoredItemSnapshot` (`X`, `Y`, `Item`), `BeltItemSnapshot` (`Slot`, `Item`), `EquippedItemSnapshot` (`Slot` name, `Item`), `GroundItemSnapshot` (`Id`, `X`, `Y`, `Gold`, `Item`).
  - Levels are captured in depth order; monsters, inventory items, and ground items in id order; belt and equipment slots in slot order (empty slots omitted).

- [ ] **Step 1: Write the failing tests**

`tests/Mortar.Tests/Gameplay/SaveCaptureTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Content;
using Mortar.Game.Saves;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class SaveCaptureTests
{
    private static readonly AffixDefinition Keen =
        TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon);

    [Fact]
    public void Capture_RecordsTheWorldAndThePlayer()
    {
        var world = Create();
        TestWorlds.Run(world, 5);
        var snapshot = WorldSnapshot.Capture(world);
        Assert.Equal(WorldSnapshot.CurrentVersion, snapshot.Version);
        Assert.Equal(world.Seed, snapshot.Seed);
        Assert.Equal(5, snapshot.Tick);
        Assert.Equal(1, snapshot.Depth);
        Assert.Equal(world.NextItemId, snapshot.NextItemId);
        Assert.Equal(world.Rng.State, snapshot.RngState);
        var player = snapshot.Player!;
        Assert.Equal("warrior", player.ClassId);
        Assert.Equal(20, player.Stats!.Strength);
        Assert.Equal(Actor.PlayerId, player.Actor!.Id);
        Assert.Equal("Idle", player.Actor.Action);
        Assert.Equal(new[] { "Body", "Weapon" }, player.Equipment!.Select(e => e!.Slot));
        Assert.Equal(new[] { 0, 1 }, player.Belt!.Select(b => b!.Slot));
        var level = Assert.Single(snapshot.Levels!)!;
        Assert.Equal(world.Level.Layout.Fingerprint, level.Fingerprint);
    }

    [Fact]
    public void Capture_RecordsMonstersGroundItemsAndAffixes()
    {
        var world = Create();
        var zombie = TestWorlds.Spawn(world, "zombie", 5, 3);
        TestWorlds.DropGold(world, 4, 4, 9);
        var keenKnife = TestItems.Create(TestGameContent.Weapon(), world.NextItemId++, prefix: TestItems.Roll(Keen, 3));
        Assert.True(world.Belongings.Inventory.TryPlace(keenKnife, new Cell(4, 0)));
        var snapshot = WorldSnapshot.Capture(world);
        var level = snapshot.Levels![0]!;
        var monster = Assert.Single(level.Monsters!)!;
        Assert.Equal(zombie.Id, monster.Id);
        Assert.Equal("zombie", monster.Definition);
        Assert.Equal(9, Assert.Single(level.GroundItems!)!.Gold);
        var stored = Assert.Single(snapshot.Player!.Inventory!)!;
        Assert.Equal((4, 0), (stored.X, stored.Y));
        Assert.Equal("keen", stored.Item!.Prefix!.Id);
        Assert.Equal(3, stored.Item.Prefix.Value);
        Assert.Null(stored.Item.Suffix);
    }

    private static World Create() => TestWorlds.Create(TestGameContent.Valid(
        items: TestGameContent.Items(new[] { TestGameContent.Weapon(), TestGameContent.Armor(), TestGameContent.Potion() }, new[] { Keen }),
        classes: new[] { TestGameContent.Warrior("knife", "vest", "tonic", "tonic") }));
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~SaveCaptureTests"`
Expected: build FAILS (`Mortar.Game.Saves` does not exist).

- [ ] **Step 3: Add the snapshot model**

`src/Mortar.Game/Saves/WorldSnapshot.cs`:

```csharp
using Mortar.Game.Simulation;

namespace Mortar.Game.Saves;

/// <summary>Plain data for a whole world. Values read from a file may be missing or invalid; restoring checks all of them.</summary>
public sealed class WorldSnapshot
{
    public const int CurrentVersion = 1;

    public int Version { get; set; }

    public uint Seed { get; set; }

    public long Tick { get; set; }

    public int Depth { get; set; }

    public int NextActorId { get; set; }

    public int NextItemId { get; set; }

    public ulong RngState { get; set; }

    public ulong RngIncrement { get; set; }

    public PlayerSnapshot? Player { get; set; }

    public List<LevelSnapshot?>? Levels { get; set; }

    public static WorldSnapshot Capture(World world) => SnapshotCapture.Capture(world);
}

public sealed class PlayerSnapshot
{
    public string? ClassId { get; set; }

    public int Level { get; set; }

    public long Experience { get; set; }

    public StatsSnapshot? Stats { get; set; }

    public int UnspentPoints { get; set; }

    public long Gold { get; set; }

    public ActorSnapshot? Actor { get; set; }

    public List<StoredItemSnapshot?>? Inventory { get; set; }

    public List<BeltItemSnapshot?>? Belt { get; set; }

    public List<EquippedItemSnapshot?>? Equipment { get; set; }
}

public sealed class StatsSnapshot
{
    public int Strength { get; set; }

    public int Dexterity { get; set; }

    public int Vitality { get; set; }

    public int Magic { get; set; }
}

public sealed class LevelSnapshot
{
    public int Depth { get; set; }

    public uint Seed { get; set; }

    public string? Fingerprint { get; set; }

    public List<ActorSnapshot?>? Monsters { get; set; }

    public List<GroundItemSnapshot?>? GroundItems { get; set; }
}

public sealed class ActorSnapshot
{
    public int Id { get; set; }

    public string? Definition { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public string? Facing { get; set; }

    public int Life { get; set; }

    public int Mana { get; set; }

    public int MaxLife { get; set; }

    public bool Dead { get; set; }

    public bool Awake { get; set; }

    public int LeaderId { get; set; }

    public int LastAttackerId { get; set; }

    public string? Action { get; set; }

    public int ActionElapsed { get; set; }

    public int ActionDuration { get; set; }

    public int ActionTargetId { get; set; }

    public string? Intent { get; set; }

    public int IntentX { get; set; }

    public int IntentY { get; set; }

    public int IntentTargetId { get; set; }
}
```

`src/Mortar.Game/Saves/ItemSnapshots.cs`:

```csharp
namespace Mortar.Game.Saves;

public sealed class ItemSnapshot
{
    public int Id { get; set; }

    public string? Base { get; set; }

    public int ItemLevel { get; set; }

    public int Armor { get; set; }

    public AffixRollSnapshot? Prefix { get; set; }

    public AffixRollSnapshot? Suffix { get; set; }
}

public sealed class AffixRollSnapshot
{
    public string? Id { get; set; }

    public int Value { get; set; }
}

public sealed class StoredItemSnapshot
{
    public int X { get; set; }

    public int Y { get; set; }

    public ItemSnapshot? Item { get; set; }
}

public sealed class BeltItemSnapshot
{
    public int Slot { get; set; }

    public ItemSnapshot? Item { get; set; }
}

public sealed class EquippedItemSnapshot
{
    public string? Slot { get; set; }

    public ItemSnapshot? Item { get; set; }
}

public sealed class GroundItemSnapshot
{
    public int Id { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public int Gold { get; set; }

    public ItemSnapshot? Item { get; set; }
}
```

- [ ] **Step 4: Capture worlds**

`src/Mortar.Game/Saves/SnapshotCapture.cs`:

```csharp
using Mortar.Game.Items;
using Mortar.Game.Simulation;

namespace Mortar.Game.Saves;

internal static class SnapshotCapture
{
    public static WorldSnapshot Capture(World world) => new WorldSnapshot
    {
        Version = WorldSnapshot.CurrentVersion,
        Seed = world.Seed,
        Tick = world.TickCount,
        Depth = world.Depth,
        NextActorId = world.NextActorId,
        NextItemId = world.NextItemId,
        RngState = world.Rng.State,
        RngIncrement = world.Rng.Increment,
        Player = CapturePlayer(world),
        Levels = world.VisitedDepths.Select(depth => (LevelSnapshot?)CaptureLevel(world.GetLevel(depth))).ToList(),
    };

    private static PlayerSnapshot CapturePlayer(World world)
    {
        var progress = world.Progress;
        var stats = progress.BaseStats;
        var belongings = world.Belongings;
        return new PlayerSnapshot
        {
            ClassId = world.PlayerClass.Id,
            Level = progress.Level,
            Experience = progress.Experience,
            Stats = new StatsSnapshot { Strength = stats.Strength, Dexterity = stats.Dexterity, Vitality = stats.Vitality, Magic = stats.Magic },
            UnspentPoints = progress.UnspentPoints,
            Gold = belongings.Gold,
            Actor = CaptureActor(world.Player),
            Inventory = belongings.Inventory.Items
                .Select(stored => (StoredItemSnapshot?)new StoredItemSnapshot { X = stored.Cell.X, Y = stored.Cell.Y, Item = CaptureItem(stored.Item) })
                .ToList(),
            Belt = Enumerable.Range(0, Belt.Slots)
                .Where(slot => belongings.Belt.Get(slot) is not null)
                .Select(slot => (BeltItemSnapshot?)new BeltItemSnapshot { Slot = slot, Item = CaptureItem(belongings.Belt.Get(slot)!) })
                .ToList(),
            Equipment = Equipment.Slots
                .Where(slot => belongings.Equipment.Get(slot) is not null)
                .Select(slot => (EquippedItemSnapshot?)new EquippedItemSnapshot { Slot = slot.ToString(), Item = CaptureItem(belongings.Equipment.Get(slot)!) })
                .ToList(),
        };
    }

    private static LevelSnapshot CaptureLevel(LevelState level) => new LevelSnapshot
    {
        Depth = level.Depth,
        Seed = level.Seed,
        Fingerprint = level.Layout.Fingerprint,
        Monsters = level.Actors.Where(actor => actor.Kind == ActorKind.Monster).Select(actor => (ActorSnapshot?)CaptureActor(actor)).ToList(),
        GroundItems = level.GroundItems.Select(ground => (GroundItemSnapshot?)new GroundItemSnapshot
        {
            Id = ground.Id,
            X = ground.Cell.X,
            Y = ground.Cell.Y,
            Gold = ground.Gold,
            Item = ground.Item is null ? null : CaptureItem(ground.Item),
        }).ToList(),
    };

    private static ActorSnapshot CaptureActor(Actor actor) => new ActorSnapshot
    {
        Id = actor.Id,
        Definition = actor.DefinitionId,
        X = actor.Cell.X,
        Y = actor.Cell.Y,
        Facing = actor.Facing.ToString(),
        Life = actor.Life,
        Mana = actor.Mana,
        MaxLife = actor.Profile.MaxLife,
        Dead = actor.IsDead,
        Awake = actor.IsAwake,
        LeaderId = actor.LeaderId,
        LastAttackerId = actor.LastAttackerId,
        Action = actor.Action.ToString(),
        ActionElapsed = actor.ActionElapsed,
        ActionDuration = actor.ActionDuration,
        ActionTargetId = actor.ActionTargetId,
        Intent = actor.Intent.ToString(),
        IntentX = actor.IntentCell.X,
        IntentY = actor.IntentCell.Y,
        IntentTargetId = actor.IntentTargetId,
    };

    private static ItemSnapshot CaptureItem(Item item) => new ItemSnapshot
    {
        Id = item.Id,
        Base = item.Definition.Id,
        ItemLevel = item.ItemLevel,
        Armor = item.ArmorValue,
        Prefix = CaptureAffix(item.Prefix),
        Suffix = CaptureAffix(item.Suffix),
    };

    private static AffixRollSnapshot? CaptureAffix(RolledAffix? affix) =>
        affix is null ? null : new AffixRollSnapshot { Id = affix.Definition.Id, Value = affix.Value };
}
```
- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~SaveCaptureTests"`
Expected: PASS (2 tests).

- [ ] **Step 6: Commit**

```bash
git add src/Mortar.Game/Saves tests/Mortar.Tests/Gameplay/SaveCaptureTests.cs
git commit -m "add world snapshots"
```

---

### Task 10: Restoring worlds from snapshots

**Files:**
- Create: `src/Mortar.Game/Saves/SaveException.cs`, `RestoredIds.cs`, `WorldRestorer.cs`, `LevelRestorer.cs`, `ActorRestorer.cs`, `ItemRestorer.cs`, `PlayerRestorer.cs`
- Modify: `src/Mortar.Game/Simulation/World.cs`, `LevelState.cs`, `src/Mortar.Game/Saves/WorldSnapshot.cs` (summary cref)
- Test: `tests/Mortar.Tests/Gameplay/SaveRestoreTests.cs`

**Interfaces:**
- Consumes: the snapshot model, `ContentSet.TryGetClass`/`TryGetMonster`/`TryGetItemBase`/`TryGetAffix`, `ILevelGenerator.Generate`, `Pcg32.Restore`, `Inventory.TryPlace`, `Belt.TryPut`, `Equipment.Accepts`.
- Produces:
  - `World.Restore(WorldSnapshot snapshot, ContentSet content, ILevelGenerator generator)` → `WorldResult`. On failure the error starts with `save is invalid: ` and names the bad value; it never throws.
  - Internal `World.ForRestore(content, generator, seed, playerClass, progress, rng)`; internal `LevelState.AddCorpse(Actor, Cell)`.
  - Internal `SaveException`, `Check` (`That`, `Present`, `Text`, `Range`, `EnumValue<T>`), `RestoredIds` (`ClaimActor`, `ClaimItem`, `MaxActorId`, `MaxItemId`).
- Restore order: header checks (version, tick, odd increment), class and progress, levels (each regenerated from its seed and fingerprint-checked; then monsters; then ground items), belongings, player profile refresh, player placement on the current depth, id counters.

- [ ] **Step 1: Write the failing tests**

`tests/Mortar.Tests/Gameplay/SaveRestoreTests.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Saves;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class SaveRestoreTests
{
    private static readonly ContentSet Content = TestGameContent.Valid(
        monsters: new[] { TestGameContent.Monster("zombie", loot: "loot") },
        rules: TestGameContent.Rules(minHit: 95),
        items: TestGameContent.Items(
            new[] { TestGameContent.Weapon(), TestGameContent.Armor(), TestGameContent.Potion() },
            new[] { TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon) },
            new[] { TestGameContent.Loot(gold: 1, item: 1, magicPercent: 50) }),
        classes: new[] { TestGameContent.Warrior("knife", "vest", "tonic", "tonic") });

    private static readonly Dictionary<string, (Action<WorldSnapshot> Corrupt, string Error)> Corruptions = new()
    {
        ["version"] = (s => s.Version = 2, "unsupported version 2"),
        ["tick"] = (s => s.Tick = -1, "tick must not be negative"),
        ["rng"] = (s => s.RngIncrement = 2, "rngIncrement must be odd"),
        ["missing player"] = (s => s.Player = null, "player is missing"),
        ["class"] = (s => s.Player!.ClassId = "bard", "unknown class 'bard'"),
        ["stat"] = (s => s.Player!.Stats!.Strength = 999, "player strength must be between 20 and 100"),
        ["level"] = (s => s.Player!.Level = 0, "player level must be between 1 and 4"),
        ["experience"] = (s => s.Player!.Experience = -1, "player experience must not be negative"),
        ["gold"] = (s => s.Player!.Gold = -1, "player gold must not be negative"),
        ["null level"] = (s => s.Levels!.Add(null), "level is missing"),
        ["level twice"] = (s => s.Levels![1]!.Depth = 1, "level 1 appears twice"),
        ["fingerprint"] = (s => s.Levels![0]!.Fingerprint = "0", "does not match"),
        ["monster definition"] = (s => Living(s).Definition = "ghost", "unknown definition 'ghost'"),
        ["monster max life"] = (s => Living(s).MaxLife = 99, "maxLife is outside"),
        ["dead with life"] = (s => Corpse(s).Life = 5, "is dead but has life left"),
        ["shared cell"] = (s => { Living(s).X = Hero(s).X; Living(s).Y = Hero(s).Y; }, "shares cell"),
        ["wall"] = (s => { Hero(s).X = 0; Hero(s).Y = 0; }, "player stands on a wall"),
        ["player life"] = (s => Hero(s).Life = 0, "player life must be between 1"),
        ["facing"] = (s => Hero(s).Facing = "Up", "player facing 'Up' is not valid"),
        ["numeric enum"] = (s => Hero(s).Action = "3", "player action '3' is not valid"),
        ["elapsed"] = (s => Hero(s).ActionElapsed = 99, "player actionElapsed must be between 0"),
        ["null inventory"] = (s => s.Player!.Inventory = null, "player inventory is missing"),
        ["overlap"] = (s => { Stored(s, 1).X = Stored(s, 0).X; Stored(s, 1).Y = Stored(s, 0).Y; }, "overlaps another item"),
        ["duplicate item"] = (s => s.Player!.Belt![1]!.Item!.Id = s.Player.Belt[0]!.Item!.Id, "is used twice"),
        ["unknown base"] = (s => Stored(s, 0).Item!.Base = "sword", "unknown base 'sword'"),
        ["affix slot"] = (s => s.Player!.Belt![0]!.Item!.Prefix = new AffixRollSnapshot { Id = "keen", Value = 3 }, "cannot appear on this item"),
        ["affix value"] = (s => Worn(s, "Weapon").Prefix = new AffixRollSnapshot { Id = "keen", Value = 99 }, "value must be between 3 and 3"),
        ["armor"] = (s => Worn(s, "Body").Armor = 99, "armor must be between 5 and 5"),
        ["wrong slot"] = (s => s.Player!.Equipment!.First(e => e!.Slot == "Body")!.Slot = "Head", "cannot be worn as Head"),
        ["belt slot"] = (s => s.Player!.Belt![0]!.Slot = 8, "needs an empty slot"),
        ["ground gold"] = (s => GroundGold(s).Gold = 0, "gold must be between 1"),
        ["next item id"] = (s => s.NextItemId = 1, "nextItemId must be greater than every item id"),
        ["next actor id"] = (s => s.NextActorId = 2, "nextActorId must be greater than every actor id"),
    };

    public static IEnumerable<object[]> CorruptionNames => Corruptions.Keys.Select(name => new object[] { name });

    [Fact]
    public void RoundTrip_KeepsTheSameHash()
    {
        var world = BusyWorld();
        Assert.Equal(WorldHash.Compute(world), WorldHash.Compute(Restore(WorldSnapshot.Capture(world))));
    }

    [Fact]
    public void RestoredWorld_PlaysOnIdentically()
    {
        var world = BusyWorld();
        var restored = Restore(WorldSnapshot.Capture(world));
        Assert.Equal(Play(world), Play(restored));
        Assert.Equal(WorldHash.Compute(world), WorldHash.Compute(restored));
    }

    [Theory]
    [MemberData(nameof(CorruptionNames))]
    public void CorruptSave_IsRejectedWithAClearError(string name)
    {
        var snapshot = WorldSnapshot.Capture(BusyWorld());
        var (corrupt, error) = Corruptions[name];
        corrupt(snapshot);
        var result = World.Restore(snapshot, Content, Generator());
        Assert.False(result.Succeeded);
        Assert.StartsWith("save is invalid: ", result.Error);
        Assert.Contains(error, result.Error);
    }

    [Fact]
    public void NullSnapshot_IsRejected()
    {
        Assert.Equal("save is empty", World.Restore(null!, Content, Generator()).Error);
    }

    private static World BusyWorld()
    {
        var world = TestWorlds.Create(Content);
        TestWorlds.Give(world, TestGameContent.Weapon());
        TestWorlds.Give(world, TestGameContent.Weapon());
        Assert.Null(LevelEntry.Enter(world, 2, MarkerKinds.Entrance));
        Assert.Null(LevelEntry.Enter(world, 1, MarkerKinds.Exit));
        var victim = TestWorlds.Spawn(world, "zombie", 6, 4);
        victim.Life = 1;
        TestWorlds.Spawn(world, "zombie", 2, 2);
        TestWorlds.Run(world, 30, new AttackCommand(victim.Id));
        Assert.True(victim.IsDead);
        var gold = TestWorlds.DropGold(world, 1, 4, 9);
        world.Tick(new Command[] { new PickUpCommand(gold.Id) });
        return world;
    }

    private static World Restore(WorldSnapshot snapshot)
    {
        var result = World.Restore(snapshot, Content, Generator());
        return result.World ?? throw new InvalidOperationException(result.Error);
    }

    private static FixedLevelGenerator Generator() => new FixedLevelGenerator(TestWorlds.Room, TestWorlds.Room);

    private static List<string> Play(World world) => TestWorlds.Run(world, 200).Select(e => e.ToString()).ToList();

    private static ActorSnapshot Hero(WorldSnapshot s) => s.Player!.Actor!;

    private static LevelSnapshot FirstLevel(WorldSnapshot s) => s.Levels!.First(level => level!.Depth == 1)!;

    private static ActorSnapshot Living(WorldSnapshot s) => FirstLevel(s).Monsters!.First(m => !m!.Dead)!;

    private static ActorSnapshot Corpse(WorldSnapshot s) => FirstLevel(s).Monsters!.First(m => m!.Dead)!;

    private static GroundItemSnapshot GroundGold(WorldSnapshot s) => FirstLevel(s).GroundItems!.First(g => g!.Item is null)!;

    private static StoredItemSnapshot Stored(WorldSnapshot s, int index) => s.Player!.Inventory![index]!;

    private static ItemSnapshot Worn(WorldSnapshot s, string slot) => s.Player!.Equipment!.First(e => e!.Slot == slot)!.Item!;
}
```

`BusyWorld` visits both levels, leaves a corpse and a living monster, loot and a gold pile on the ground, a pickup walk in progress, two knives in the inventory, two potions in the belt, and a knife and vest equipped. The test rules have three experience thresholds, so the maximum level is 4.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~SaveRestoreTests"`
Expected: build FAILS (`World.Restore` does not exist).

- [ ] **Step 3: Add the checking helpers**

`src/Mortar.Game/Saves/SaveException.cs`:

```csharp
namespace Mortar.Game.Saves;

internal sealed class SaveException : Exception
{
    public SaveException(string message)
        : base(message)
    {
    }
}

internal static class Check
{
    public static void That(bool condition, string message)
    {
        if (!condition) throw new SaveException(message);
    }

    public static T Present<T>(T? value, string name)
        where T : class =>
        value ?? throw new SaveException($"{name} is missing");

    public static string Text(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new SaveException($"{name} is missing") : value!;

    public static int Range(int value, int min, int max, string name)
    {
        That(value >= min && value <= max, $"{name} must be between {min} and {max}");
        return value;
    }

    public static TEnum EnumValue<TEnum>(string? value, string name)
        where TEnum : struct, Enum
    {
        var valid = !int.TryParse(value, out _)
            && Enum.TryParse<TEnum>(value, ignoreCase: false, out var parsed)
            && Enum.IsDefined(typeof(TEnum), parsed);
        return valid ? Enum.Parse<TEnum>(value!) : throw new SaveException($"{name} '{value}' is not valid");
    }
}
```

`src/Mortar.Game/Saves/RestoredIds.cs`:

```csharp
namespace Mortar.Game.Saves;

internal sealed class RestoredIds
{
    private readonly HashSet<int> _actors = new HashSet<int>();
    private readonly HashSet<int> _items = new HashSet<int>();

    public int MaxActorId { get; private set; }

    public int MaxItemId { get; private set; }

    public void ClaimActor(int id)
    {
        Check.That(id >= 1, "actor ids must be positive");
        Check.That(_actors.Add(id), $"actor id {id} is used twice");
        MaxActorId = Math.Max(MaxActorId, id);
    }

    public void ClaimItem(int id)
    {
        Check.That(id >= 1, "item ids must be positive");
        Check.That(_items.Add(id), $"item id {id} is used twice");
        MaxItemId = Math.Max(MaxItemId, id);
    }
}
```

- [ ] **Step 4: Restore items, actors, and levels**

`src/Mortar.Game/Saves/ItemRestorer.cs`:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Items;

namespace Mortar.Game.Saves;

internal static class ItemRestorer
{
    public static Item Restore(ContentSet content, ItemSnapshot snapshot, RestoredIds ids, string where)
    {
        ids.ClaimItem(snapshot.Id);
        var baseId = Check.Text(snapshot.Base, $"{where} base");
        Check.That(content.TryGetItemBase(baseId, out var definition), $"{where} has unknown base '{baseId}'");
        var itemLevel = Check.Range(snapshot.ItemLevel, 1, DefinitionChecks.MaxValue, $"{where} itemLevel");
        var armorRange = definition!.Armor ?? new IntRange(0, 0);
        var armor = Check.Range(snapshot.Armor, armorRange.Min, armorRange.Max, $"{where} armor");
        var prefix = RestoreAffix(content, snapshot.Prefix, definition, itemLevel, AffixPositions.Prefix, $"{where} prefix");
        var suffix = RestoreAffix(content, snapshot.Suffix, definition, itemLevel, AffixPositions.Suffix, $"{where} suffix");
        return new Item(snapshot.Id, definition, itemLevel, armor, prefix, suffix);
    }

    private static RolledAffix? RestoreAffix(
        ContentSet content, AffixRollSnapshot? snapshot, ItemBaseDefinition definition, int itemLevel, string position, string where)
    {
        if (snapshot is null) return null;
        var id = Check.Text(snapshot.Id, $"{where} id");
        Check.That(content.TryGetAffix(id, out var affix), $"{where} has unknown affix '{id}'");
        Check.That(affix!.Position == position && affix.Allows(definition.Slot, itemLevel), $"{where} '{id}' cannot appear on this item");
        return new RolledAffix(affix, Check.Range(snapshot.Value, affix.Value.Min, affix.Value.Max, $"{where} value"));
    }
}
```

`src/Mortar.Game/Saves/ActorRestorer.cs`:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Simulation;

namespace Mortar.Game.Saves;

internal static class ActorRestorer
{
    public static void RestoreMonster(World world, LevelState level, ActorSnapshot snapshot, RestoredIds ids)
    {
        var where = $"monster {snapshot.Id}";
        Check.That(snapshot.Id != Actor.PlayerId, $"{where} uses the player's id");
        ids.ClaimActor(snapshot.Id);
        var definitionId = Check.Text(snapshot.Definition, $"{where} definition");
        Check.That(world.Content.TryGetMonster(definitionId, out var definition), $"{where} has unknown definition '{definitionId}'");
        Check.That(
            snapshot.MaxLife >= definition!.Life.Min && snapshot.MaxLife <= definition.Life.Max,
            $"{where} maxLife is outside its definition's life range");
        var actor = new Actor(snapshot.Id, ActorKind.Monster, definition.Id, CombatProfile.ForMonster(definition, snapshot.MaxLife));
        Place(level, actor, snapshot, where);
    }

    public static void Place(LevelState level, Actor actor, ActorSnapshot snapshot, string where)
    {
        var cell = new Cell(snapshot.X, snapshot.Y);
        Check.That(level.IsWalkable(cell), $"{where} stands on a wall or outside the level");
        Check.Range(snapshot.Mana, 0, actor.Profile.MaxMana, $"{where} mana");
        if (snapshot.Dead) Check.That(snapshot.Life <= 0, $"{where} is dead but has life left");
        else Check.Range(snapshot.Life, 1, actor.Profile.MaxLife, $"{where} life");
        ApplyState(level, actor, snapshot, where);
        actor.Life = snapshot.Life;
        actor.Mana = snapshot.Mana;
        if (snapshot.Dead)
        {
            level.AddCorpse(actor, cell);
            actor.MarkDead();
            return;
        }
        Check.That(level.IsFree(cell), $"{where} shares cell {cell} with another actor");
        level.Add(actor, cell);
    }

    private static void ApplyState(LevelState level, Actor actor, ActorSnapshot snapshot, string where)
    {
        actor.Facing = Check.EnumValue<Direction>(snapshot.Facing, $"{where} facing");
        actor.IsAwake = snapshot.Awake;
        actor.LeaderId = Check.Range(snapshot.LeaderId, 0, int.MaxValue, $"{where} leaderId");
        actor.LastAttackerId = Check.Range(snapshot.LastAttackerId, 0, int.MaxValue, $"{where} lastAttackerId");
        var action = Check.EnumValue<ActionKind>(snapshot.Action, $"{where} action");
        var duration = Check.Range(snapshot.ActionDuration, 0, DefinitionChecks.MaxValue, $"{where} actionDuration");
        actor.StartAction(action, duration, Check.Range(snapshot.ActionTargetId, 0, int.MaxValue, $"{where} actionTargetId"));
        actor.ActionElapsed = Check.Range(snapshot.ActionElapsed, 0, duration, $"{where} actionElapsed");
        var intent = Check.EnumValue<IntentKind>(snapshot.Intent, $"{where} intent");
        var intentCell = new Cell(snapshot.IntentX, snapshot.IntentY);
        var intentTarget = Check.Range(snapshot.IntentTargetId, 0, int.MaxValue, $"{where} intentTargetId");
        if (intent == IntentKind.MoveTo || intent == IntentKind.PickUp)
        {
            Check.That(level.InBounds(intentCell), $"{where} intent cell is outside the level");
        }
        switch (intent)
        {
            case IntentKind.MoveTo: actor.SetMoveIntent(intentCell); break;
            case IntentKind.Attack: actor.SetAttackIntent(intentTarget); break;
            case IntentKind.PickUp: actor.SetPickUpIntent(intentTarget, intentCell); break;
            default: actor.ClearIntent(); break;
        }
    }
}
```

`src/Mortar.Game/Saves/LevelRestorer.cs`:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Items;
using Mortar.Game.Simulation;

namespace Mortar.Game.Saves;

internal static class LevelRestorer
{
    public static void Restore(World world, LevelSnapshot snapshot, RestoredIds ids)
    {
        var depth = snapshot.Depth;
        Check.That(depth >= 1 && depth <= world.Content.DeepestLevel, $"level depth {depth} is not in the content");
        Check.That(world.FindLevel(depth) is null, $"level {depth} appears twice");
        var result = world.Generator.Generate(world.Content.Level(depth), snapshot.Seed);
        Check.That(result.Succeeded, $"level {depth} could not be regenerated: {result.Error}");
        var layout = result.Layout!;
        Check.That(
            layout.Fingerprint == snapshot.Fingerprint,
            $"level {depth} does not match: the save has fingerprint '{snapshot.Fingerprint}', the regenerated level has '{layout.Fingerprint}'");
        var level = new LevelState(depth, snapshot.Seed, layout);
        world.AddLevel(level);
        foreach (var monster in Check.Present(snapshot.Monsters, $"level {depth} monsters"))
        {
            ActorRestorer.RestoreMonster(world, level, Check.Present(monster, $"level {depth} monster"), ids);
        }
        foreach (var ground in Check.Present(snapshot.GroundItems, $"level {depth} groundItems"))
        {
            level.AddGroundItem(RestoreGround(world, level, Check.Present(ground, $"level {depth} ground item"), ids));
        }
    }

    private static GroundItem RestoreGround(World world, LevelState level, GroundItemSnapshot snapshot, RestoredIds ids)
    {
        var cell = new Cell(snapshot.X, snapshot.Y);
        var where = $"level {level.Depth} ground item {snapshot.Id}";
        Check.That(level.IsWalkable(cell), $"{where} lies on a wall or outside the level");
        if (snapshot.Item is null)
        {
            ids.ClaimItem(snapshot.Id);
            return new GroundItem(snapshot.Id, cell, null, Check.Range(snapshot.Gold, 1, int.MaxValue, $"{where} gold"));
        }
        Check.That(snapshot.Gold == 0, $"{where} has both gold and an item");
        var item = ItemRestorer.Restore(world.Content, snapshot.Item, ids, where);
        Check.That(item.Id == snapshot.Id, $"{where} must have the same id as its item");
        return new GroundItem(item.Id, cell, item, 0);
    }
}
```

Add to `src/Mortar.Game/Simulation/LevelState.cs`, and make `Add` reuse the same sorted insert:

```csharp
    internal void AddCorpse(Actor actor, Cell cell)
    {
        actor.Cell = cell;
        InsertById(actor);
    }

    private void InsertById(Actor actor)
    {
        var index = _actors.FindIndex(existing => existing.Id > actor.Id);
        _actors.Insert(index < 0 ? _actors.Count : index, actor);
        _byId[actor.Id] = actor;
    }
```

(`Add` keeps its free-cell check and occupant write, then calls `InsertById(actor)` instead of its own three insert lines.)

- [ ] **Step 5: Restore the player and the world**

`src/Mortar.Game/Saves/PlayerRestorer.cs`:

```csharp
using Mortar.Core.Rng;
using Mortar.Game.Content;
using Mortar.Game.Items;
using Mortar.Game.Simulation;

namespace Mortar.Game.Saves;

internal static class PlayerRestorer
{
    public static World CreateWorld(WorldSnapshot snapshot, PlayerSnapshot player, ContentSet content, ILevelGenerator generator)
    {
        var classId = Check.Text(player.ClassId, "player classId");
        Check.That(content.TryGetClass(classId, out var definition), $"unknown class '{classId}'");
        var progress = RestoreProgress(player, definition!, content.Rules);
        var rng = Pcg32.Restore(snapshot.RngState, snapshot.RngIncrement);
        return World.ForRestore(content, generator, snapshot.Seed, definition!, progress, rng);
    }

    public static void Place(World world, PlayerSnapshot player, int depth, RestoredIds ids)
    {
        RestoreBelongings(world, player, ids);
        world.RefreshPlayer();
        var level = world.FindLevel(depth);
        Check.That(level is not null, $"current depth {depth} is not a saved level");
        var actor = Check.Present(player.Actor, "player actor");
        Check.That(actor.Id == Actor.PlayerId, $"player actor id must be {Actor.PlayerId}");
        ids.ClaimActor(actor.Id);
        Check.That(actor.Definition == world.PlayerClass.Id, "player actor definition must match the class");
        world.Depth = depth;
        ActorRestorer.Place(level!, world.Player, actor, "player");
    }

    private static PlayerProgress RestoreProgress(PlayerSnapshot player, ClassDefinition definition, GameRules rules)
    {
        var saved = Check.Present(player.Stats, "player stats");
        var stats = new Stats(saved.Strength, saved.Dexterity, saved.Vitality, saved.Magic);
        foreach (var kind in Stats.Kinds)
        {
            var name = $"player {kind.ToString().ToLowerInvariant()}";
            Check.Range(stats.Get(kind), definition.BaseStats.Get(kind), definition.StatCaps.Get(kind), name);
        }
        Check.Range(player.Level, 1, rules.MaxLevel, "player level");
        Check.That(player.Experience >= 0, "player experience must not be negative");
        Check.Range(player.UnspentPoints, 0, DefinitionChecks.MaxValue, "player unspentPoints");
        return new PlayerProgress(definition.Id, player.Level, player.Experience, stats, player.UnspentPoints);
    }

    private static void RestoreBelongings(World world, PlayerSnapshot player, RestoredIds ids)
    {
        var belongings = world.Belongings;
        Check.That(player.Gold >= 0, "player gold must not be negative");
        belongings.Gold = player.Gold;
        foreach (var stored in Check.Present(player.Inventory, "player inventory"))
        {
            var entry = Check.Present(stored, "inventory entry");
            var item = ItemRestorer.Restore(world.Content, Check.Present(entry.Item, "inventory item"), ids, "inventory item");
            Check.That(
                belongings.Inventory.TryPlace(item, new Cell(entry.X, entry.Y)),
                $"inventory item {item.Id} overlaps another item or lies outside the grid");
        }
        foreach (var belted in Check.Present(player.Belt, "player belt"))
        {
            var entry = Check.Present(belted, "belt entry");
            var item = ItemRestorer.Restore(world.Content, Check.Present(entry.Item, "belt item"), ids, "belt item");
            Check.That(
                belongings.Belt.TryPut(item, entry.Slot),
                $"belt item {item.Id} must be a potion and needs an empty slot from 0 to {Belt.Slots - 1}");
        }
        foreach (var worn in Check.Present(player.Equipment, "player equipment"))
        {
            var entry = Check.Present(worn, "equipment entry");
            var slot = Check.EnumValue<EquipmentSlot>(entry.Slot, "equipment slot");
            var item = ItemRestorer.Restore(world.Content, Check.Present(entry.Item, $"{slot} item"), ids, $"{slot} item");
            Check.That(Equipment.Accepts(slot, item.Definition), $"item {item.Id} cannot be worn as {slot}");
            Check.That(belongings.Equipment.Get(slot) is null, $"{slot} is equipped twice");
            belongings.Equipment.Set(slot, item);
        }
    }
}
```

The "belt slot" corruption sets slot 8, so `TryPut` fails and the message contains "needs an empty slot"; the "affix slot" corruption puts a weapon prefix on a potion.

`src/Mortar.Game/Saves/WorldRestorer.cs`:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Simulation;

namespace Mortar.Game.Saves;

internal static class WorldRestorer
{
    public static WorldResult Restore(WorldSnapshot? snapshot, ContentSet content, ILevelGenerator generator)
    {
        if (snapshot is null) return WorldResult.Failure("save is empty");
        try
        {
            return WorldResult.Success(Build(snapshot, content, generator));
        }
        catch (SaveException ex)
        {
            return WorldResult.Failure($"save is invalid: {ex.Message}");
        }
    }

    private static World Build(WorldSnapshot snapshot, ContentSet content, ILevelGenerator generator)
    {
        Check.That(
            snapshot.Version == WorldSnapshot.CurrentVersion,
            $"unsupported version {snapshot.Version}; this build reads version {WorldSnapshot.CurrentVersion}");
        Check.That(snapshot.Tick >= 0, "tick must not be negative");
        Check.That((snapshot.RngIncrement & 1UL) == 1UL, "rngIncrement must be odd");
        var player = Check.Present(snapshot.Player, "player");
        var world = PlayerRestorer.CreateWorld(snapshot, player, content, generator);
        var ids = new RestoredIds();
        foreach (var level in Check.Present(snapshot.Levels, "levels"))
        {
            LevelRestorer.Restore(world, Check.Present(level, "level"), ids);
        }
        PlayerRestorer.Place(world, player, snapshot.Depth, ids);
        Check.That(snapshot.NextActorId > ids.MaxActorId, $"nextActorId must be greater than every actor id ({ids.MaxActorId})");
        Check.That(snapshot.NextItemId > ids.MaxItemId, $"nextItemId must be greater than every item id ({ids.MaxItemId})");
        world.TickCount = snapshot.Tick;
        world.NextActorId = snapshot.NextActorId;
        world.NextItemId = snapshot.NextItemId;
        return world;
    }
}
```

Add to `src/Mortar.Game/Simulation/World.cs` (with `using Mortar.Game.Saves;`):

```csharp
    public static WorldResult Restore(WorldSnapshot snapshot, ContentSet content, ILevelGenerator generator) =>
        WorldRestorer.Restore(snapshot, content, generator);

    internal static World ForRestore(
        ContentSet content, ILevelGenerator generator, uint seed, ClassDefinition playerClass, PlayerProgress progress, Pcg32 rng) =>
        new World(content, generator, seed, playerClass, progress, rng);
```
- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~SaveRestoreTests"`
Expected: PASS (2 facts, 33 theory cases, and the null test). If a corruption case fails because its message differs slightly, fix the restorer message to say what is wrong in plain words, not the test. Then `dotnet test`; expected: all pass.

- [ ] **Step 7: Commit**

```bash
git add src/Mortar.Game/Saves src/Mortar.Game/Simulation/World.cs src/Mortar.Game/Simulation/LevelState.cs tests/Mortar.Tests/Gameplay/SaveRestoreTests.cs
git commit -m "restore worlds from snapshots"
```

---

### Task 11: Save files

**Files:**
- Create: `src/Mortar.Json/Saves/SaveFile.cs`, `docs/saves.md`
- Modify: `src/Mortar.Game/Simulation/WorldResult.cs`, `README.md`
- Test: `tests/Mortar.Tests/Json/SaveFileTests.cs`

**Interfaces:**
- Consumes: `WorldSnapshot.Capture`, `World.Restore`, `SafePath.Resolve`, `ScriptedPlayer.Play`.
- Produces:
  - `WorldResult.Failure(string error)` becomes public, so `Mortar.Json` can report file errors in the same result type.
  - `SaveFile.MaxBytes` (16 MiB), `SaveFile.ToJson(World)`, `SaveFile.FromJson(string json, ContentSet, ILevelGenerator)` → `WorldResult`, `SaveFile.Write(string path, World)` → error or null, `SaveFile.Read(string path, ContentSet, ILevelGenerator)` → `WorldResult`.
- JSON format: camelCase, indented, unknown properties rejected. `FromJson` checks `version` before deserializing so a newer save gets a clear message rather than a property error. `Write` writes `<path>.tmp` and then replaces or moves it into place, so a crash mid-write never leaves a half-written save.

- [ ] **Step 1: Write the failing tests**

`tests/Mortar.Tests/Json/SaveFileTests.cs`:

```csharp
using System.Text.RegularExpressions;
using Mortar.Game.Content;
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using Mortar.Json.Saves;
using Mortar.Tests.Gameplay.Support;
using Mortar.Tests.Support;

namespace Mortar.Tests.Json;

public class SaveFileTests
{
    private static readonly ContentSet TestContent = TestGameContent.Valid(
        items: TestGameContent.Items(new[] { TestGameContent.Weapon(), TestGameContent.Armor(), TestGameContent.Potion() }),
        classes: new[] { TestGameContent.Warrior("knife", "vest", "tonic") });

    [Fact]
    public void ScriptedGame_SavedAndLoaded_ContinuesIdentically()
    {
        var content = GameContentLoader.Load(ContentFixture.Manifest).Value!;
        var world = World.Create(content.Content, content.CreateLevelGenerator(), "warrior", 7).World!;
        var firstHalf = ScriptedPlayer.Play(world, 1500);
        var loaded = SaveFile.FromJson(SaveFile.ToJson(world), content.Content, content.CreateLevelGenerator());
        Assert.True(loaded.Succeeded, loaded.Error);
        Assert.Equal(WorldHash.Compute(world), WorldHash.Compute(loaded.World!));
        Assert.Equal(ScriptedPlayer.Play(world, 1500), ScriptedPlayer.Play(loaded.World!, 1500));
        Assert.Equal(WorldHash.Compute(world), WorldHash.Compute(loaded.World!));
        Assert.Contains(firstHalf, line => line.Contains("picked up"));
    }

    [Fact]
    public void WriteAndRead_RoundTripsThroughAFile()
    {
        using var temp = new TempDir();
        var path = Path.Combine(temp.Root, "slot1.json");
        var world = TestWorlds.Create(TestContent);
        TestWorlds.Run(world, 10);
        Assert.Null(SaveFile.Write(path, world));
        TestWorlds.Run(world, 10);
        Assert.Null(SaveFile.Write(path, world));
        var loaded = SaveFile.Read(path, TestContent, Generator());
        Assert.True(loaded.Succeeded, loaded.Error);
        Assert.Equal(WorldHash.Compute(world), WorldHash.Compute(loaded.World!));
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Read_MissingFile_IsAnError()
    {
        using var temp = new TempDir();
        var result = SaveFile.Read(Path.Combine(temp.Root, "none.json"), TestContent, Generator());
        Assert.Contains("file not found", result.Error);
    }

    [Fact]
    public void Read_HugeFile_IsAnError()
    {
        using var temp = new TempDir();
        var path = Path.Combine(temp.Root, "huge.json");
        File.WriteAllText(path, new string(' ', (int)SaveFile.MaxBytes + 1));
        Assert.Contains("larger than", SaveFile.Read(path, TestContent, Generator()).Error);
    }

    [Theory]
    [InlineData("not json", "save is not valid JSON")]
    [InlineData("[]", "save must be a JSON object")]
    [InlineData("{}", "save has no version number")]
    [InlineData("{\"version\": \"one\"}", "save has no version number")]
    [InlineData("{\"version\": 9}", "unsupported save version 9")]
    public void FromJson_RejectsBrokenFiles(string json, string error)
    {
        Assert.Contains(error, SaveFile.FromJson(json, TestContent, Generator()).Error);
    }

    [Theory]
    [InlineData("\"version\": 1,", "\"version\": 1, \"cheat\": 1,", "save is not valid")]
    [InlineData("\"nextItemId\": ", "\"nextItemId\": 99999999999, \"unused\": ", "save is not valid")]
    [InlineData("\"levels\": [", "\"levels\": [ null,", "save is invalid: level is missing")]
    public void FromJson_RejectsEditedSaves(string find, string replace, string error)
    {
        var json = SaveFile.ToJson(TestWorlds.Create(TestContent));
        Assert.Contains(find, json);
        Assert.Contains(error, SaveFile.FromJson(json.Replace(find, replace), TestContent, Generator()).Error);
    }

    [Fact]
    public void FromJson_WrongTypeOrFingerprint_IsAnError()
    {
        var json = SaveFile.ToJson(TestWorlds.Create(TestContent));
        var badTick = Regex.Replace(json, "\"tick\": \\d+", "\"tick\": \"soon\"");
        Assert.Contains("save is not valid", SaveFile.FromJson(badTick, TestContent, Generator()).Error);
        var badLevel = Regex.Replace(json, "\"fingerprint\": \"[^\"]*\"", "\"fingerprint\": \"0000\"");
        Assert.Contains("does not match", SaveFile.FromJson(badLevel, TestContent, Generator()).Error);
    }

    private static FixedLevelGenerator Generator() => new FixedLevelGenerator(TestWorlds.Room, TestWorlds.Room);
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~SaveFileTests"`
Expected: build FAILS (`Mortar.Json.Saves` does not exist).

- [ ] **Step 3: Implement save files**

In `src/Mortar.Game/Simulation/WorldResult.cs`, change `internal static WorldResult Failure` to `public static WorldResult Failure`.

`src/Mortar.Json/Saves/SaveFile.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Mortar.Game.Content;
using Mortar.Game.Saves;
using Mortar.Game.Simulation;

namespace Mortar.Json.Saves;

public static class SaveFile
{
    public const long MaxBytes = 16L * 1024 * 1024;

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static string ToJson(World world) => JsonSerializer.Serialize(WorldSnapshot.Capture(world), Options);

    public static WorldResult FromJson(string json, ContentSet content, ILevelGenerator generator)
    {
        var versionError = CheckVersion(json);
        if (versionError is not null) return WorldResult.Failure(versionError);
        WorldSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<WorldSnapshot>(json, Options);
        }
        catch (JsonException ex)
        {
            return WorldResult.Failure($"save is not valid: {ex.Message}");
        }
        return snapshot is null ? WorldResult.Failure("save is empty") : World.Restore(snapshot, content, generator);
    }

    public static string? Write(string path, World world)
    {
        var target = SafePath.Resolve(null, path);
        if (target is null) return $"{path}: invalid path";
        var temporary = target + ".tmp";
        try
        {
            File.WriteAllText(temporary, ToJson(world));
            if (File.Exists(target)) File.Replace(temporary, target, null);
            else File.Move(temporary, target);
            return null;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return $"{target}: cannot write save: {ex.Message}";
        }
    }

    public static WorldResult Read(string path, ContentSet content, ILevelGenerator generator)
    {
        var source = SafePath.Resolve(null, path);
        if (source is null) return WorldResult.Failure($"{path}: invalid path");
        try
        {
            var info = new FileInfo(source);
            if (!info.Exists) return WorldResult.Failure($"{source}: file not found");
            if (info.Length > MaxBytes) return WorldResult.Failure($"{source}: save is larger than {MaxBytes} bytes");
            var result = FromJson(File.ReadAllText(source), content, generator);
            return result.Succeeded ? result : WorldResult.Failure($"{source}: {result.Error}");
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return WorldResult.Failure($"{source}: cannot read save: {ex.Message}");
        }
    }

    private static string? CheckVersion(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return "save must be a JSON object";
            if (!root.TryGetProperty("version", out var version)
                || version.ValueKind != JsonValueKind.Number
                || !version.TryGetInt32(out var number))
            {
                return "save has no version number";
            }
            return number == WorldSnapshot.CurrentVersion
                ? null
                : $"unsupported save version {number}; this build reads version {WorldSnapshot.CurrentVersion}";
        }
        catch (JsonException ex)
        {
            return $"save is not valid JSON: {ex.Message}";
        }
    }
}
```
- [ ] **Step 4: Document saves**

`docs/saves.md`:

```markdown
# Saves

`Mortar.Json.Saves.SaveFile` writes and reads whole worlds as JSON.

- `SaveFile.Write(path, world)` returns null on success or an error message. It writes `<path>.tmp` first and then swaps it into place, so an interrupted write never damages the previous save.
- `SaveFile.Read(path, content, generator)` returns a `WorldResult`. Files over 16 MiB, broken JSON, unknown properties, wrong value types, and saves that don't fit the content (unknown ids, impossible stats or positions, levels whose layout no longer matches) are all reported as errors, never thrown.

A save stores the seed, the tick, the random generator state, the player (class, level, experience, stats, gold, inventory, belt, equipment, and current action), and every visited level: its seed, a fingerprint of its layout, its monsters (alive or dead), and the items on the ground. Levels are rebuilt from their seeds when loading; if the rebuilt layout's fingerprint differs (for example because an area file changed), the save is rejected.

Loading a save and continuing produces exactly the same game as never having saved.

`version` is 1. A save with any other version is rejected with a message naming both versions.

The simulation never writes files on its own. The engine layer saves when it chooses to, for example after each `LevelChanged` event.
```

In `README.md`, replace "Items, inventory, and save/load come next." with "Items with magic affixes, an inventory and potion belt, monster loot, and save files (see `docs/saves.md`) complete the first milestone; the town and more classes come next."

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~SaveFileTests"`
Expected: PASS. Then run `dotnet test` and `dotnet build -c Release`; expected: all tests pass and the build has no warnings.

- [ ] **Step 6: Commit**

```bash
git add src/Mortar.Json/Saves/SaveFile.cs src/Mortar.Game/Simulation/WorldResult.cs docs/saves.md README.md tests/Mortar.Tests/Json/SaveFileTests.cs
git commit -m "add save files"
```

