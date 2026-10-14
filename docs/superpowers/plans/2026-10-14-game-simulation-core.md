# Game Simulation Core (Milestone 1a) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `Mortar.Game`, an engine-independent, deterministic, tick-based ARPG simulation with a warrior, melee combat, monsters with AI, stairs between dungeon levels, and JSON-loaded content.

**Architecture:** `World.Tick(commands)` advances the simulation by one tick (20 per second) and returns events. Small stateless systems run in a fixed order: commands, AI, actions (movement and swing timers), combat, death. All randomness comes from one seeded `Pcg32` per world; levels come from Mortar's generator through an `ILevelGenerator` seam so tests can use hand-drawn maps. Content (classes, monsters, levels, rules) is plain definition objects validated by `ContentSet.Create`; `Mortar.Json` loads them from the `content/` folder.

**Tech Stack:** C# 12, .NET Standard 2.1 (`Mortar.Game`, `Mortar.Json`), .NET 10 (tests), System.Text.Json, xUnit.

**Spec:** `docs/superpowers/specs/2026-10-14-arpg-framework-design.md`. This plan covers milestone 1 except items, inventory, belt, loot, gold, potions, and save/load, which are the next plan (milestone 1b).

## Global Constraints

- `Mortar.Game` targets `netstandard2.1` and references only `Mortar.Core`. No file IO, no System.Text.Json, no UnityEngine.
- Never use `System.Random`. Gameplay randomness comes from the world's `Pcg32`; level layouts come from Mortar's generator.
- No floating point in the simulation.
- No dictionary or hash-set enumeration in decision paths. Actors are processed in ascending id order; markers in layout order.
- The simulation runs at 20 ticks per second (`GameClock.TicksPerSecond`).
- Invalid commands produce a `CommandRejected` event and never throw. Invalid content produces `AuthoringResult` errors.
- Clean-room: no code, formulas, data tables, or names from devilutionX or the original game. All numbers are our own and live in content files.
- No Blizzard or Diablo names in code, data, or content.
- No file over 300 lines; no function over 50 lines.
- `Nullable` enabled and warnings treated as errors in every project (already set in `Directory.Build.props`).
- Commit messages are short, one line, present tense, no prefixes.
- Never stage `.env` files or anything under `.superpowers/`.

## Design Interpretations

The spec leaves these choices open; this plan fixes them. Review them before execution.

1. **`Mortar.Content` is the `content/` data folder.** Loader code lives in `Mortar.Json` (namespace `Mortar.Json.Content`), which already owns every file format.
2. **Movement is 8-directional, one tile per step.** Straight and diagonal steps take the same time. A diagonal step is not allowed past a wall corner (both orthogonal neighbors must be walkable).
3. **An actor occupies its destination tile as soon as a step starts.** Engines interpolate the sprite over `durationTicks`.
4. **Paths are recomputed every step** with A* (bounded to 4096 expanded tiles), so moving targets and blocking actors are handled without path invalidation logic.
5. **Actor ids are `int`; the player is always id 1.** Monster ids count up from 2 in spawn order.
6. **Only the current level ticks.** Monsters on other visited levels are frozen until the player returns.
7. **No town yet.** The player starts at the level 1 entrance; going up from level 1 is rejected. The town arrives with the town generation plan.
8. **Levels 1–4 use the sample `crypt` area** with different level seeds and spawn tables until cathedral generation lands.
9. **Spawn markers within 5 tiles of the entrance are skipped** so the player never arrives inside a fight.
10. **Initial monster population emits no events.** `LevelChanged` tells engines to re-read the level's actors.
11. **Pack-leader behavior is a shared wake-up.** Minions spawn around their leader and the whole group wakes when any member notices the player or is hit.
12. **An attack intent persists** until the target dies or the player issues another command, so the player keeps swinging at an adjacent target.
13. **The hit frame must be before the end of the swing** (`1 <= hitFrame < attackTicks`), so a hit always resolves while the swing is still in progress.
14. **Validation errors name the definition** (`monster 'zombie': ...`); file-read and missing-field errors name the file path.
15. **Life and mana are whole numbers.** No regeneration in this plan.

## Review Focus

1. **Determinism:** the same seed and command sequence must produce the same event log and state hash. Pinned in Task 11.
2. **Combat ordering:** swings that land on the same tick resolve in actor id order, and an attacker staggered earlier in that tick does not land its swing. Pinned in Task 7.
3. **Pathfinding never cuts wall corners and always terminates.** Pinned in Task 3.
4. **Bad content fails with clear messages** instead of exceptions. Pinned in Tasks 2 and 10.
5. **A dead player cannot act:** all later commands are rejected. Pinned in Task 7.

## File Structure

```
src/Mortar.Core/Rng/Pcg32.cs                 + State, Increment, Restore
src/Mortar.Game/
  Mortar.Game.csproj
  Cell.cs, Direction.cs, Directions.cs, GameClock.cs
  Content/                                   immutable definitions + validation
    IntRange.cs, Stats.cs, ActionTimings.cs, ClassGrowth.cs
    ClassDefinition.cs, MonsterDefinition.cs, AiKinds.cs
    LevelDefinition.cs, GameRules.cs, ContentSet.cs
    ContentValidator.cs, DefinitionChecks.cs
  Pathing/MinHeap.cs, Pathing/Pathfinder.cs
  Commands/Command.cs, MoveCommand.cs, AttackCommand.cs,
           SpendStatPointCommand.cs, UseStairsCommand.cs
  Events/GameEvent.cs, EventSink.cs, ActorMoved.cs, CommandRejected.cs,
         CombatEvents.cs, ProgressEvents.cs, MonsterAwoke.cs, LevelChanged.cs
  Simulation/
    CombatProfile.cs, PlayerProgress.cs, PlayerStats.cs, Leveling.cs
    ActorKind.cs, ActionKind.cs, IntentKind.cs, Actor.cs
    LevelState.cs, ILevelGenerator.cs, LevelBuildResult.cs, LevelSeeds.cs
    World.cs, WorldResult.cs, LevelEntry.cs
    TickRunner.cs, CommandSystem.cs, ActionSystem.cs, Movement.cs
    HitChance.cs, CombatSystem.cs, DeathSystem.cs, PlayerGrowth.cs
    MonsterSpawner.cs, AiSystem.cs, Stairs.cs
    AreaLevelGenerator.cs, StateHasher.cs, WorldHash.cs
src/Mortar.Json/Content/
  ContentDtos.cs, Fields.cs, ContentMapper.cs, GameContent.cs,
  GameContentLoader.cs, LevelFileLoader.cs
content/
  game.json, rules.json, levels.json, classes/warrior.json, monsters/*.json
docs/content.md
tests/Mortar.Tests/Gameplay/
  Support/TestGameContent.cs, GridText.cs, TestLevels.cs, TestWorlds.cs, ScriptedPlayer.cs
  CellTests.cs, ContentValidatorTests.cs, PathfinderTests.cs, PlayerStatsTests.cs,
  LevelingTests.cs, LevelStateTests.cs, WorldCreationTests.cs, MovementTests.cs,
  CombatTests.cs, ProgressionTests.cs, SpawnerTests.cs, AiTests.cs, StairsTests.cs,
  DeterminismTests.cs
tests/Mortar.Tests/Json/GameContentLoaderTests.cs
tests/Mortar.Tests/Snapshots/game-seed-7.txt   (created by the first run in Task 11)
```

Test namespaces use `Mortar.Tests.Gameplay` (not `...Game`) so they never shadow the `Mortar.Game` namespace.

---

### Task 1: Game project, cells, directions, and RNG state restore

**Files:**
- Modify: `src/Mortar.Core/Rng/Pcg32.cs`
- Create: `src/Mortar.Game/Mortar.Game.csproj`, `src/Mortar.Game/Cell.cs`, `src/Mortar.Game/Direction.cs`, `src/Mortar.Game/Directions.cs`, `src/Mortar.Game/GameClock.cs`
- Modify: `Mortar.slnx`, `tests/Mortar.Tests/Mortar.Tests.csproj`
- Test: `tests/Mortar.Tests/Rng/Pcg32Tests.cs`, `tests/Mortar.Tests/Gameplay/CellTests.cs`

**Interfaces:**
- Produces: `Pcg32.State`, `Pcg32.Increment`, `Pcg32.Restore(ulong state, ulong increment)`; `Cell(int x, int y)` with `X`, `Y`, `DistanceTo(Cell)` (Chebyshev), `IsAdjacentTo(Cell)`, `==`/`!=`; `Direction` enum (`South, SouthWest, West, NorthWest, North, NorthEast, East, SouthEast`); `Directions.All`, `Direction.DeltaX()`, `Direction.DeltaY()`, `Direction.IsDiagonal()`, `Cell.Step(Direction)`, `Directions.Toward(Cell from, Cell to)`; `GameClock.TicksPerSecond = 20`.

- [ ] **Step 1: Create the project and wire it into the solution**

`src/Mortar.Game/Mortar.Game.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <RootNamespace>Mortar.Game</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Mortar.Tests" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Mortar.Core\Mortar.Core.csproj" />
  </ItemGroup>

</Project>
```

`Mortar.slnx` becomes:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/Mortar.Cli/Mortar.Cli.csproj" />
    <Project Path="src/Mortar.Core/Mortar.Core.csproj" />
    <Project Path="src/Mortar.Game/Mortar.Game.csproj" />
    <Project Path="src/Mortar.Json/Mortar.Json.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/Mortar.Tests/Mortar.Tests.csproj" />
  </Folder>
</Solution>
```

In `tests/Mortar.Tests/Mortar.Tests.csproj`, add to the `ProjectReference` item group:

```xml
    <ProjectReference Include="..\..\src\Mortar.Game\Mortar.Game.csproj" />
```

- [ ] **Step 2: Write the failing tests**

Append to the `Pcg32Tests` class in `tests/Mortar.Tests/Rng/Pcg32Tests.cs`:

```csharp
    [Fact]
    public void Restore_ContinuesTheSameSequence()
    {
        var original = Pcg32.ForAttempt("world", 9, 0);
        original.NextUInt();
        var copy = Pcg32.Restore(original.State, original.Increment);
        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(original.NextUInt(), copy.NextUInt());
        }
    }

    [Fact]
    public void Restore_RejectsEvenIncrement()
    {
        Assert.Throws<ArgumentException>(() => Pcg32.Restore(1, 2));
    }
```

`tests/Mortar.Tests/Gameplay/CellTests.cs`:

```csharp
using Mortar.Game;

namespace Mortar.Tests.Gameplay;

public class CellTests
{
    [Fact]
    public void DistanceTo_UsesChebyshevDistance()
    {
        Assert.Equal(3, new Cell(1, 1).DistanceTo(new Cell(4, 3)));
        Assert.True(new Cell(1, 1).IsAdjacentTo(new Cell(2, 2)));
        Assert.False(new Cell(1, 1).IsAdjacentTo(new Cell(1, 1)));
    }

    [Theory]
    [InlineData(5, 5, 5, 9, Direction.South)]
    [InlineData(5, 5, 1, 9, Direction.SouthWest)]
    [InlineData(5, 5, 1, 5, Direction.West)]
    [InlineData(5, 5, 9, 1, Direction.NorthEast)]
    [InlineData(5, 5, 5, 5, Direction.South)]
    public void Toward_UsesTheSignOfEachAxis(int fromX, int fromY, int toX, int toY, Direction expected)
    {
        Assert.Equal(expected, Directions.Toward(new Cell(fromX, fromY), new Cell(toX, toY)));
    }

    [Fact]
    public void Step_MovesOneCellInEachDirection()
    {
        var origin = new Cell(3, 3);
        foreach (var direction in Directions.All)
        {
            var next = origin.Step(direction);
            Assert.True(origin.IsAdjacentTo(next));
            Assert.Equal(direction, Directions.Toward(origin, next));
        }
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~Pcg32Tests|FullyQualifiedName~Gameplay.CellTests"`
Expected: build FAILS (`Pcg32` has no `Restore`; `Mortar.Game` types do not exist).

- [ ] **Step 4: Implement**

In `src/Mortar.Core/Rng/Pcg32.cs`, change `private readonly ulong _increment;` to `private ulong _increment;` and add after the constructor:

```csharp
    public ulong State => _state;

    public ulong Increment => _increment;

    public static Pcg32 Restore(ulong state, ulong increment)
    {
        if ((increment & 1UL) == 0)
        {
            throw new ArgumentException("increment must be odd", nameof(increment));
        }
        var rng = new Pcg32(0, 0);
        rng._state = state;
        rng._increment = increment;
        return rng;
    }
```

`src/Mortar.Game/Cell.cs`:

```csharp
namespace Mortar.Game;

public readonly struct Cell : IEquatable<Cell>
{
    public Cell(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }

    public int DistanceTo(Cell other) => Math.Max(Math.Abs(other.X - X), Math.Abs(other.Y - Y));

    public bool IsAdjacentTo(Cell other) => DistanceTo(other) == 1;

    public bool Equals(Cell other) => X == other.X && Y == other.Y;

    public override bool Equals(object? obj) => obj is Cell other && Equals(other);

    public override int GetHashCode() => unchecked((X * 397) ^ Y);

    public override string ToString() => $"({X},{Y})";

    public static bool operator ==(Cell left, Cell right) => left.Equals(right);

    public static bool operator !=(Cell left, Cell right) => !left.Equals(right);
}
```

`src/Mortar.Game/Direction.cs`:

```csharp
namespace Mortar.Game;

public enum Direction
{
    South,
    SouthWest,
    West,
    NorthWest,
    North,
    NorthEast,
    East,
    SouthEast,
}
```

`src/Mortar.Game/Directions.cs`:

```csharp
namespace Mortar.Game;

public static class Directions
{
    private static readonly int[] Dx = { 0, -1, -1, -1, 0, 1, 1, 1 };
    private static readonly int[] Dy = { 1, 1, 0, -1, -1, -1, 0, 1 };

    public static IReadOnlyList<Direction> All { get; } = new[]
    {
        Direction.South, Direction.SouthWest, Direction.West, Direction.NorthWest,
        Direction.North, Direction.NorthEast, Direction.East, Direction.SouthEast,
    };

    public static int DeltaX(this Direction direction) => Dx[(int)direction];

    public static int DeltaY(this Direction direction) => Dy[(int)direction];

    public static bool IsDiagonal(this Direction direction) => direction.DeltaX() != 0 && direction.DeltaY() != 0;

    public static Cell Step(this Cell cell, Direction direction) =>
        new Cell(cell.X + direction.DeltaX(), cell.Y + direction.DeltaY());

    public static Direction Toward(Cell from, Cell to)
    {
        var dx = Math.Sign(to.X - from.X);
        var dy = Math.Sign(to.Y - from.Y);
        for (var i = 0; i < Dx.Length; i++)
        {
            if (Dx[i] == dx && Dy[i] == dy)
            {
                return (Direction)i;
            }
        }
        return Direction.South;
    }
}
```

`src/Mortar.Game/GameClock.cs`:

```csharp
namespace Mortar.Game;

public static class GameClock
{
    public const int TicksPerSecond = 20;
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~Pcg32Tests|FullyQualifiedName~Gameplay.CellTests"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Mortar.Core/Rng/Pcg32.cs src/Mortar.Game Mortar.slnx tests/Mortar.Tests/Mortar.Tests.csproj tests/Mortar.Tests/Rng/Pcg32Tests.cs tests/Mortar.Tests/Gameplay/CellTests.cs
git commit -m "add game project, cells, and rng state restore"
```

---

### Task 2: Content definitions and validation

**Files:**
- Create: `src/Mortar.Game/Content/IntRange.cs`, `Stats.cs`, `ActionTimings.cs`, `ClassGrowth.cs`, `ClassDefinition.cs`, `MonsterDefinition.cs`, `AiKinds.cs`, `LevelDefinition.cs`, `GameRules.cs`, `ContentSet.cs`, `ContentValidator.cs`, `DefinitionChecks.cs`
- Test: `tests/Mortar.Tests/Gameplay/Support/TestGameContent.cs`, `tests/Mortar.Tests/Gameplay/ContentValidatorTests.cs`

**Interfaces:**
- Consumes: `Mortar.Core.Authoring.AuthoringError`, `AuthoringResult<T>`.
- Produces (namespace `Mortar.Game.Content`):
  - `IntRange(int min, int max)` with `Min`, `Max`, `Shift(int)`.
  - `StatKind { Strength, Dexterity, Vitality, Magic }`; `Stats(int strength, int dexterity, int vitality, int magic)` with `Get(StatKind)`, `With(StatKind, int)`, static `Stats.Kinds`.
  - `ActionTimings(int moveTicks, int attackTicks, int hitFrame, int recoveryTicks)`.
  - `ClassGrowth(int lifeBase, int lifePerVitality, int lifePerLevel, int manaBase, int manaPerMagic, int manaPerLevel, int statPointsPerLevel)`.
  - `ClassDefinition(string id, string name, string spriteSet, Stats baseStats, Stats statCaps, ClassGrowth growth, IntRange unarmedDamage, ActionTimings timings)`.
  - `MinionSpec(string monsterId, IntRange count)`; `MonsterDefinition(string id, string name, string spriteSet, int level, IntRange life, int armorClass, int toHit, IntRange damage, int blockChance, ActionTimings timings, int sightRadius, int experience, string ai, MinionSpec? minions)`.
  - `AiKinds.MeleeChase = "melee-chase"`, `AiKinds.PackLeader = "pack-leader"`, `AiKinds.IsKnown(string)`.
  - `SpawnEntry(string monsterId, int weight)`; `LevelDefinition(int depth, string areaId, IReadOnlyList<SpawnEntry> spawns, string? bossId)`.
  - `GameRules(int minHitChance, int maxHitChance, int playerBaseToHit, int toHitPerDexterity, int toHitPerLevel, int toHitPerLevelDifference, int dexterityPerArmor, int strengthPerDamage, int hitRecoveryPercent, IReadOnlyList<long> experienceThresholds)` with `MaxLevel`.
  - `ContentSet.Create(GameRules, IReadOnlyList<ClassDefinition>, IReadOnlyList<MonsterDefinition>, IReadOnlyList<LevelDefinition>) -> AuthoringResult<ContentSet>`; members `Rules`, `Classes`, `Monsters`, `Levels` (sorted by depth), `DeepestLevel`, `TryGetClass(string, out ClassDefinition?)`, `Monster(string)`, `Level(int depth)`.

- [ ] **Step 1: Write the test content helper**

`tests/Mortar.Tests/Gameplay/Support/TestGameContent.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Game.Content;

namespace Mortar.Tests.Gameplay.Support;

internal static class TestGameContent
{
    public static GameRules Rules(int minHit = 5, int maxHit = 95, int recoveryPercent = 20, long[]? thresholds = null) =>
        new GameRules(minHit, maxHit, 50, 1, 1, 2, 5, 10, recoveryPercent, thresholds ?? new long[] { 100, 300, 600 });

    public static ActionTimings Timings(int move = 4, int attack = 6, int hit = 3, int recovery = 3) =>
        new ActionTimings(move, attack, hit, recovery);

    public static ClassDefinition Warrior() => new ClassDefinition(
        "warrior",
        "Warrior",
        "warrior",
        new Stats(20, 20, 20, 10),
        new Stats(100, 100, 100, 100),
        new ClassGrowth(10, 2, 2, 0, 1, 1, 5),
        new IntRange(2, 4),
        Timings());

    public static MonsterDefinition Monster(
        string id,
        string ai = AiKinds.MeleeChase,
        MinionSpec? minions = null,
        int life = 10,
        int sight = 6,
        int experience = 50) =>
        new MonsterDefinition(id, id, id, 1, new IntRange(life, life), 0, 50, new IntRange(1, 2), 0, Timings(), sight, experience, ai, minions);

    public static LevelDefinition Level(int depth, string monsterId = "zombie", string? bossId = null) =>
        new LevelDefinition(depth, "test", new[] { new SpawnEntry(monsterId, 1) }, bossId);

    public static AuthoringResult<ContentSet> Create(
        IReadOnlyList<MonsterDefinition>? monsters = null,
        IReadOnlyList<LevelDefinition>? levels = null,
        GameRules? rules = null,
        IReadOnlyList<ClassDefinition>? classes = null) =>
        ContentSet.Create(
            rules ?? Rules(),
            classes ?? new[] { Warrior() },
            monsters ?? new[] { Monster("zombie") },
            levels ?? new[] { Level(1), Level(2) });

    public static ContentSet Valid(
        IReadOnlyList<MonsterDefinition>? monsters = null,
        IReadOnlyList<LevelDefinition>? levels = null,
        GameRules? rules = null)
    {
        var result = Create(monsters, levels, rules);
        return result.Value ?? throw new InvalidOperationException(string.Join("; ", result.Errors));
    }
}
```

- [ ] **Step 2: Write the failing tests**

`tests/Mortar.Tests/Gameplay/ContentValidatorTests.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Game.Content;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class ContentValidatorTests
{
    [Fact]
    public void ValidContent_Succeeds()
    {
        var result = TestGameContent.Create();
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        Assert.Equal(2, result.Value!.DeepestLevel);
        Assert.True(result.Value.TryGetClass("warrior", out _));
    }

    [Fact]
    public void Levels_AreSortedByDepth()
    {
        var content = TestGameContent.Valid(levels: new[] { TestGameContent.Level(2), TestGameContent.Level(1) });
        Assert.Equal(new[] { 1, 2 }, content.Levels.Select(l => l.Depth));
    }

    [Fact]
    public void DuplicateMonsterIds_AreRejected()
    {
        var monsters = new[] { TestGameContent.Monster("zombie"), TestGameContent.Monster("zombie") };
        AssertError(TestGameContent.Create(monsters: monsters), "monster 'zombie'", "duplicate id");
    }

    [Fact]
    public void DepthGap_IsRejected()
    {
        var levels = new[] { TestGameContent.Level(1), TestGameContent.Level(3) };
        AssertError(TestGameContent.Create(levels: levels), "levels", "depths must be 1, 2, 3");
    }

    [Fact]
    public void UnknownSpawnMonster_IsRejected()
    {
        var levels = new[] { TestGameContent.Level(1, monsterId: "ghost") };
        AssertError(TestGameContent.Create(levels: levels), "level 1", "unknown monster 'ghost'");
    }

    [Fact]
    public void UnknownBoss_IsRejected()
    {
        var levels = new[] { TestGameContent.Level(1, bossId: "lich") };
        AssertError(TestGameContent.Create(levels: levels), "level 1", "boss references unknown monster 'lich'");
    }

    [Fact]
    public void PackLeaderWithoutMinions_IsRejected()
    {
        var monsters = new[] { TestGameContent.Monster("zombie"), TestGameContent.Monster("chief", AiKinds.PackLeader) };
        AssertError(TestGameContent.Create(monsters: monsters), "monster 'chief'", "need minions");
    }

    [Fact]
    public void MinionsThatArePackLeaders_AreRejected()
    {
        var spec = new MinionSpec("chief", new IntRange(1, 2));
        var monsters = new[] { TestGameContent.Monster("zombie"), TestGameContent.Monster("chief", AiKinds.PackLeader, spec) };
        AssertError(TestGameContent.Create(monsters: monsters), "monster 'chief'", "minions cannot be pack leaders");
    }

    [Fact]
    public void MinionsOnNonLeader_AreRejected()
    {
        var spec = new MinionSpec("zombie", new IntRange(1, 2));
        var monsters = new[] { TestGameContent.Monster("zombie"), TestGameContent.Monster("imp", AiKinds.MeleeChase, spec) };
        AssertError(TestGameContent.Create(monsters: monsters), "monster 'imp'", "only pack-leader monsters can have minions");
    }

    [Fact]
    public void HitFrameAtEndOfSwing_IsRejected()
    {
        var bad = new MonsterDefinition(
            "zombie", "Zombie", "zombie", 1, new IntRange(5, 5), 0, 50, new IntRange(1, 2), 0,
            TestGameContent.Timings(attack: 6, hit: 6), 5, 10, AiKinds.MeleeChase, null);
        AssertError(TestGameContent.Create(monsters: new[] { bad }), "monster 'zombie'", "hitFrame must be at least 1 and less than attackTicks");
    }

    [Fact]
    public void ExperienceThresholds_MustIncrease()
    {
        var rules = TestGameContent.Rules(thresholds: new long[] { 100, 100 });
        AssertError(TestGameContent.Create(rules: rules), "rules", "strictly increasing");
    }

    [Fact]
    public void StatCapBelowBase_IsRejected()
    {
        var warrior = TestGameContent.Warrior();
        var capped = new ClassDefinition(
            warrior.Id, warrior.Name, warrior.SpriteSet, warrior.BaseStats, new Stats(10, 100, 100, 100),
            warrior.Growth, warrior.UnarmedDamage, warrior.Timings);
        AssertError(TestGameContent.Create(classes: new[] { capped }), "class 'warrior'", "strength cap must be at least the base value");
    }

    private static void AssertError(AuthoringResult<ContentSet> result, string source, string fragment)
    {
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Source == source && e.Message.Contains(fragment));
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay.ContentValidatorTests"`
Expected: build FAILS (`Mortar.Game.Content` does not exist).

- [ ] **Step 4: Implement the value types**

`src/Mortar.Game/Content/IntRange.cs`:

```csharp
namespace Mortar.Game.Content;

public readonly struct IntRange
{
    public IntRange(int min, int max)
    {
        Min = min;
        Max = max;
    }

    public int Min { get; }

    public int Max { get; }

    public IntRange Shift(int amount) => new IntRange(Min + amount, Max + amount);

    public override string ToString() => $"{Min}-{Max}";
}
```

`src/Mortar.Game/Content/Stats.cs`:

```csharp
namespace Mortar.Game.Content;

public enum StatKind
{
    Strength,
    Dexterity,
    Vitality,
    Magic,
}

public readonly struct Stats
{
    public Stats(int strength, int dexterity, int vitality, int magic)
    {
        Strength = strength;
        Dexterity = dexterity;
        Vitality = vitality;
        Magic = magic;
    }

    public static IReadOnlyList<StatKind> Kinds { get; } =
        new[] { StatKind.Strength, StatKind.Dexterity, StatKind.Vitality, StatKind.Magic };

    public int Strength { get; }

    public int Dexterity { get; }

    public int Vitality { get; }

    public int Magic { get; }

    public int Get(StatKind kind) => kind switch
    {
        StatKind.Strength => Strength,
        StatKind.Dexterity => Dexterity,
        StatKind.Vitality => Vitality,
        _ => Magic,
    };

    public Stats With(StatKind kind, int value) => kind switch
    {
        StatKind.Strength => new Stats(value, Dexterity, Vitality, Magic),
        StatKind.Dexterity => new Stats(Strength, value, Vitality, Magic),
        StatKind.Vitality => new Stats(Strength, Dexterity, value, Magic),
        _ => new Stats(Strength, Dexterity, Vitality, value),
    };
}
```

`src/Mortar.Game/Content/ActionTimings.cs`:

```csharp
namespace Mortar.Game.Content;

public sealed class ActionTimings
{
    public ActionTimings(int moveTicks, int attackTicks, int hitFrame, int recoveryTicks)
    {
        MoveTicks = moveTicks;
        AttackTicks = attackTicks;
        HitFrame = hitFrame;
        RecoveryTicks = recoveryTicks;
    }

    public int MoveTicks { get; }

    public int AttackTicks { get; }

    public int HitFrame { get; }

    public int RecoveryTicks { get; }
}
```

`src/Mortar.Game/Content/ClassGrowth.cs`:

```csharp
namespace Mortar.Game.Content;

public sealed class ClassGrowth
{
    public ClassGrowth(
        int lifeBase,
        int lifePerVitality,
        int lifePerLevel,
        int manaBase,
        int manaPerMagic,
        int manaPerLevel,
        int statPointsPerLevel)
    {
        LifeBase = lifeBase;
        LifePerVitality = lifePerVitality;
        LifePerLevel = lifePerLevel;
        ManaBase = manaBase;
        ManaPerMagic = manaPerMagic;
        ManaPerLevel = manaPerLevel;
        StatPointsPerLevel = statPointsPerLevel;
    }

    public int LifeBase { get; }

    public int LifePerVitality { get; }

    public int LifePerLevel { get; }

    public int ManaBase { get; }

    public int ManaPerMagic { get; }

    public int ManaPerLevel { get; }

    public int StatPointsPerLevel { get; }
}
```

`src/Mortar.Game/Content/ClassDefinition.cs`:

```csharp
namespace Mortar.Game.Content;

public sealed class ClassDefinition
{
    public ClassDefinition(
        string id,
        string name,
        string spriteSet,
        Stats baseStats,
        Stats statCaps,
        ClassGrowth growth,
        IntRange unarmedDamage,
        ActionTimings timings)
    {
        Id = id;
        Name = name;
        SpriteSet = spriteSet;
        BaseStats = baseStats;
        StatCaps = statCaps;
        Growth = growth;
        UnarmedDamage = unarmedDamage;
        Timings = timings;
    }

    public string Id { get; }

    public string Name { get; }

    public string SpriteSet { get; }

    public Stats BaseStats { get; }

    public Stats StatCaps { get; }

    public ClassGrowth Growth { get; }

    public IntRange UnarmedDamage { get; }

    public ActionTimings Timings { get; }
}
```

`src/Mortar.Game/Content/MonsterDefinition.cs`:

```csharp
namespace Mortar.Game.Content;

public sealed class MinionSpec
{
    public MinionSpec(string monsterId, IntRange count)
    {
        MonsterId = monsterId;
        Count = count;
    }

    public string MonsterId { get; }

    public IntRange Count { get; }
}

public sealed class MonsterDefinition
{
    public MonsterDefinition(
        string id,
        string name,
        string spriteSet,
        int level,
        IntRange life,
        int armorClass,
        int toHit,
        IntRange damage,
        int blockChance,
        ActionTimings timings,
        int sightRadius,
        int experience,
        string ai,
        MinionSpec? minions)
    {
        Id = id;
        Name = name;
        SpriteSet = spriteSet;
        Level = level;
        Life = life;
        ArmorClass = armorClass;
        ToHit = toHit;
        Damage = damage;
        BlockChance = blockChance;
        Timings = timings;
        SightRadius = sightRadius;
        Experience = experience;
        Ai = ai;
        Minions = minions;
    }

    public string Id { get; }

    public string Name { get; }

    public string SpriteSet { get; }

    public int Level { get; }

    public IntRange Life { get; }

    public int ArmorClass { get; }

    public int ToHit { get; }

    public IntRange Damage { get; }

    public int BlockChance { get; }

    public ActionTimings Timings { get; }

    public int SightRadius { get; }

    public int Experience { get; }

    public string Ai { get; }

    public MinionSpec? Minions { get; }
}
```

`src/Mortar.Game/Content/AiKinds.cs`:

```csharp
namespace Mortar.Game.Content;

public static class AiKinds
{
    public const string MeleeChase = "melee-chase";
    public const string PackLeader = "pack-leader";

    public static bool IsKnown(string ai) => ai == MeleeChase || ai == PackLeader;
}
```

`src/Mortar.Game/Content/LevelDefinition.cs`:

```csharp
namespace Mortar.Game.Content;

public sealed class SpawnEntry
{
    public SpawnEntry(string monsterId, int weight)
    {
        MonsterId = monsterId;
        Weight = weight;
    }

    public string MonsterId { get; }

    public int Weight { get; }
}

public sealed class LevelDefinition
{
    public LevelDefinition(int depth, string areaId, IReadOnlyList<SpawnEntry> spawns, string? bossId)
    {
        Depth = depth;
        AreaId = areaId;
        Spawns = spawns;
        BossId = bossId;
    }

    public int Depth { get; }

    public string AreaId { get; }

    public IReadOnlyList<SpawnEntry> Spawns { get; }

    public string? BossId { get; }
}
```

`src/Mortar.Game/Content/GameRules.cs`:

```csharp
namespace Mortar.Game.Content;

public sealed class GameRules
{
    public GameRules(
        int minHitChance,
        int maxHitChance,
        int playerBaseToHit,
        int toHitPerDexterity,
        int toHitPerLevel,
        int toHitPerLevelDifference,
        int dexterityPerArmor,
        int strengthPerDamage,
        int hitRecoveryPercent,
        IReadOnlyList<long> experienceThresholds)
    {
        MinHitChance = minHitChance;
        MaxHitChance = maxHitChance;
        PlayerBaseToHit = playerBaseToHit;
        ToHitPerDexterity = toHitPerDexterity;
        ToHitPerLevel = toHitPerLevel;
        ToHitPerLevelDifference = toHitPerLevelDifference;
        DexterityPerArmor = dexterityPerArmor;
        StrengthPerDamage = strengthPerDamage;
        HitRecoveryPercent = hitRecoveryPercent;
        ExperienceThresholds = experienceThresholds;
    }

    public int MinHitChance { get; }

    public int MaxHitChance { get; }

    public int PlayerBaseToHit { get; }

    public int ToHitPerDexterity { get; }

    public int ToHitPerLevel { get; }

    public int ToHitPerLevelDifference { get; }

    public int DexterityPerArmor { get; }

    public int StrengthPerDamage { get; }

    public int HitRecoveryPercent { get; }

    /// <summary>Total experience needed to reach level 2, 3, ...; index 0 is level 2.</summary>
    public IReadOnlyList<long> ExperienceThresholds { get; }

    public int MaxLevel => ExperienceThresholds.Count + 1;
}
```

- [ ] **Step 5: Implement the content set and validation**

`src/Mortar.Game/Content/ContentSet.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;
using Mortar.Core.Authoring;

namespace Mortar.Game.Content;

public sealed class ContentSet
{
    private readonly Dictionary<string, ClassDefinition> _classes;
    private readonly Dictionary<string, MonsterDefinition> _monsters;

    private ContentSet(
        GameRules rules,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        IReadOnlyList<LevelDefinition> levels)
    {
        Rules = rules;
        Classes = classes;
        Monsters = monsters;
        Levels = levels;
        _classes = classes.ToDictionary(c => c.Id, StringComparer.Ordinal);
        _monsters = monsters.ToDictionary(m => m.Id, StringComparer.Ordinal);
    }

    public GameRules Rules { get; }

    public IReadOnlyList<ClassDefinition> Classes { get; }

    public IReadOnlyList<MonsterDefinition> Monsters { get; }

    public IReadOnlyList<LevelDefinition> Levels { get; }

    public int DeepestLevel => Levels.Count;

    public static AuthoringResult<ContentSet> Create(
        GameRules rules,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        IReadOnlyList<LevelDefinition> levels)
    {
        var errors = ContentValidator.Validate(rules, classes, monsters, levels);
        if (errors.Count > 0)
        {
            return AuthoringResult<ContentSet>.Failure(errors);
        }
        var ordered = levels.OrderBy(level => level.Depth).ToList();
        return AuthoringResult<ContentSet>.Success(new ContentSet(rules, classes, monsters, ordered));
    }

    public bool TryGetClass(string id, [NotNullWhen(true)] out ClassDefinition? definition) =>
        _classes.TryGetValue(id, out definition);

    public MonsterDefinition Monster(string id) => _monsters[id];

    public LevelDefinition Level(int depth) => Levels[depth - 1];
}
```

`src/Mortar.Game/Content/ContentValidator.cs`:

```csharp
using Mortar.Core.Authoring;

namespace Mortar.Game.Content;

internal static class ContentValidator
{
    public static List<AuthoringError> Validate(
        GameRules rules,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        IReadOnlyList<LevelDefinition> levels)
    {
        var errors = new List<AuthoringError>();
        CheckRules(rules, errors);
        if (classes.Count == 0)
        {
            errors.Add("classes", "at least one class is required");
        }
        CheckUniqueIds(classes.Select(c => c.Id), "class", errors);
        CheckUniqueIds(monsters.Select(m => m.Id), "monster", errors);
        var byId = new Dictionary<string, MonsterDefinition>(StringComparer.Ordinal);
        foreach (var monster in monsters)
        {
            if (!byId.ContainsKey(monster.Id)) byId.Add(monster.Id, monster);
        }
        MonsterDefinition? Find(string id) => byId.TryGetValue(id, out var found) ? found : null;
        foreach (var definition in classes) DefinitionChecks.CheckClass(definition, errors);
        foreach (var monster in monsters) DefinitionChecks.CheckMonster(monster, Find, errors);
        CheckLevels(levels, Find, errors);
        return errors;
    }

    private static void CheckRules(GameRules rules, List<AuthoringError> errors)
    {
        const string source = "rules";
        if (rules.MinHitChance < 0 || rules.MinHitChance > rules.MaxHitChance || rules.MaxHitChance > 100)
        {
            errors.Add(source, "hit chance limits must satisfy 0 <= minHitChance <= maxHitChance <= 100");
        }
        if (rules.PlayerBaseToHit < 0 || rules.ToHitPerDexterity < 0 || rules.ToHitPerLevel < 0 || rules.ToHitPerLevelDifference < 0)
        {
            errors.Add(source, "to-hit values must not be negative");
        }
        if (rules.DexterityPerArmor < 1 || rules.StrengthPerDamage < 1)
        {
            errors.Add(source, "dexterityPerArmor and strengthPerDamage must be at least 1");
        }
        if (rules.HitRecoveryPercent < 1 || rules.HitRecoveryPercent > 100)
        {
            errors.Add(source, "hitRecoveryPercent must be between 1 and 100");
        }
        for (var i = 0; i < rules.ExperienceThresholds.Count; i++)
        {
            var previous = i == 0 ? 0 : rules.ExperienceThresholds[i - 1];
            if (rules.ExperienceThresholds[i] <= previous)
            {
                errors.Add(source, "experienceThresholds must be positive and strictly increasing");
                break;
            }
        }
    }

    private static void CheckUniqueIds(IEnumerable<string> ids, string kind, List<AuthoringError> errors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id)) errors.Add(kind, "id must not be empty");
            else if (!seen.Add(id)) errors.Add($"{kind} '{id}'", "duplicate id");
        }
    }

    private static void CheckLevels(
        IReadOnlyList<LevelDefinition> levels,
        Func<string, MonsterDefinition?> find,
        List<AuthoringError> errors)
    {
        if (levels.Count == 0)
        {
            errors.Add("levels", "at least one level is required");
            return;
        }
        var depths = levels.Select(level => level.Depth).OrderBy(depth => depth).ToList();
        for (var i = 0; i < depths.Count; i++)
        {
            if (depths[i] != i + 1)
            {
                errors.Add("levels", "depths must be 1, 2, 3, ... with no gaps or repeats");
                break;
            }
        }
        foreach (var level in levels) CheckLevel(level, find, errors);
    }

    private static void CheckLevel(LevelDefinition level, Func<string, MonsterDefinition?> find, List<AuthoringError> errors)
    {
        var source = $"level {level.Depth}";
        if (string.IsNullOrWhiteSpace(level.AreaId)) errors.Add(source, "area is required");
        if (level.Spawns.Count == 0) errors.Add(source, "at least one spawn entry is required");
        long total = 0;
        foreach (var spawn in level.Spawns)
        {
            if (spawn.Weight < 1) errors.Add(source, $"spawn '{spawn.MonsterId}' weight must be at least 1");
            if (find(spawn.MonsterId) is null) errors.Add(source, $"spawn references unknown monster '{spawn.MonsterId}'");
            total += Math.Max(0, spawn.Weight);
        }
        if (total > int.MaxValue) errors.Add(source, "spawn weights add up to more than 2147483647");
        if (level.BossId is not null && find(level.BossId) is null)
        {
            errors.Add(source, $"boss references unknown monster '{level.BossId}'");
        }
    }
}
```

`src/Mortar.Game/Content/DefinitionChecks.cs`:

```csharp
using Mortar.Core.Authoring;

namespace Mortar.Game.Content;

internal static class DefinitionChecks
{
    public const int MaxMinions = 12;

    public static void Add(this List<AuthoringError> errors, string source, string message) =>
        errors.Add(new AuthoringError(source, message));

    public static void CheckClass(ClassDefinition definition, List<AuthoringError> errors)
    {
        var source = $"class '{definition.Id}'";
        if (string.IsNullOrWhiteSpace(definition.Name)) errors.Add(source, "name is required");
        foreach (var kind in Stats.Kinds)
        {
            var name = kind.ToString().ToLowerInvariant();
            if (definition.BaseStats.Get(kind) < 0) errors.Add(source, $"base {name} must not be negative");
            if (definition.StatCaps.Get(kind) < definition.BaseStats.Get(kind))
            {
                errors.Add(source, $"{name} cap must be at least the base value");
            }
        }
        var growth = definition.Growth;
        if (growth.LifeBase < 1) errors.Add(source, "lifeBase must be at least 1");
        if (growth.LifePerVitality < 0 || growth.LifePerLevel < 0 || growth.ManaBase < 0
            || growth.ManaPerMagic < 0 || growth.ManaPerLevel < 0 || growth.StatPointsPerLevel < 0)
        {
            errors.Add(source, "growth values must not be negative");
        }
        CheckRange(source, "unarmedDamage", definition.UnarmedDamage, 0, errors);
        CheckTimings(source, definition.Timings, errors);
    }

    public static void CheckMonster(MonsterDefinition monster, Func<string, MonsterDefinition?> find, List<AuthoringError> errors)
    {
        var source = $"monster '{monster.Id}'";
        if (string.IsNullOrWhiteSpace(monster.Name)) errors.Add(source, "name is required");
        if (monster.Level < 1) errors.Add(source, "level must be at least 1");
        CheckRange(source, "life", monster.Life, 1, errors);
        CheckRange(source, "damage", monster.Damage, 0, errors);
        if (monster.ArmorClass < 0 || monster.ToHit < 0 || monster.Experience < 0)
        {
            errors.Add(source, "armorClass, toHit, and experience must not be negative");
        }
        if (monster.BlockChance < 0 || monster.BlockChance > 100) errors.Add(source, "blockChance must be between 0 and 100");
        if (monster.SightRadius < 1) errors.Add(source, "sightRadius must be at least 1");
        CheckTimings(source, monster.Timings, errors);
        CheckAi(source, monster, find, errors);
    }

    private static void CheckAi(string source, MonsterDefinition monster, Func<string, MonsterDefinition?> find, List<AuthoringError> errors)
    {
        if (!AiKinds.IsKnown(monster.Ai))
        {
            errors.Add(source, $"unknown ai '{monster.Ai}'");
            return;
        }
        if (monster.Ai != AiKinds.PackLeader)
        {
            if (monster.Minions is not null) errors.Add(source, "only pack-leader monsters can have minions");
            return;
        }
        if (monster.Minions is null)
        {
            errors.Add(source, "pack-leader monsters need minions");
            return;
        }
        CheckRange(source, "minions count", monster.Minions.Count, 0, errors);
        if (monster.Minions.Count.Max > MaxMinions) errors.Add(source, $"minions count max must be at most {MaxMinions}");
        var minion = find(monster.Minions.MonsterId);
        if (minion is null) errors.Add(source, $"minions reference unknown monster '{monster.Minions.MonsterId}'");
        else if (minion.Ai == AiKinds.PackLeader) errors.Add(source, "minions cannot be pack leaders");
    }

    private static void CheckRange(string source, string field, IntRange range, int minimum, List<AuthoringError> errors)
    {
        if (range.Min < minimum || range.Max < range.Min)
        {
            errors.Add(source, $"{field} must satisfy {minimum} <= min <= max");
        }
    }

    private static void CheckTimings(string source, ActionTimings timings, List<AuthoringError> errors)
    {
        if (timings.MoveTicks < 1 || timings.AttackTicks < 1 || timings.RecoveryTicks < 1)
        {
            errors.Add(source, "moveTicks, attackTicks, and recoveryTicks must be at least 1");
        }
        if (timings.HitFrame < 1 || timings.HitFrame >= timings.AttackTicks)
        {
            errors.Add(source, "hitFrame must be at least 1 and less than attackTicks");
        }
    }
}
```

The `Add(this List<AuthoringError>, string, string)` extension is visible to `ContentValidator` because both are in `Mortar.Game.Content`.

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay.ContentValidatorTests"`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src/Mortar.Game/Content tests/Mortar.Tests/Gameplay
git commit -m "add game content definitions and validation"
```

---

### Task 3: Pathfinding

**Files:**
- Create: `src/Mortar.Game/Pathing/MinHeap.cs`, `src/Mortar.Game/Pathing/Pathfinder.cs`
- Test: `tests/Mortar.Tests/Gameplay/Support/GridText.cs`, `tests/Mortar.Tests/Gameplay/PathfinderTests.cs`

**Interfaces:**
- Consumes: `Cell`, `Directions` (Task 1).
- Produces: `Pathfinder.MaxExpanded = 4096`; `Pathfinder.FindPath(Cell start, Cell goal, Func<Cell,bool> walkable, Func<Cell,bool> free) -> IReadOnlyList<Cell>?` and `Pathfinder.FindPathNextTo(Cell start, Cell target, Func<Cell,bool> walkable, Func<Cell,bool> free) -> IReadOnlyList<Cell>?`. Paths exclude the start cell; an empty list means already there; `null` means unreachable or too far. `walkable` is terrain only (used for corner checks); `free` is terrain plus no actor.

- [ ] **Step 1: Write the grid helper**

`tests/Mortar.Tests/Gameplay/Support/GridText.cs`:

```csharp
using Mortar.Game;

namespace Mortar.Tests.Gameplay.Support;

internal sealed class GridText
{
    private readonly string[] _rows;

    public GridText(params string[] rows)
    {
        _rows = rows;
    }

    public bool Walkable(Cell cell) =>
        cell.Y >= 0 && cell.Y < _rows.Length && cell.X >= 0 && cell.X < _rows[cell.Y].Length && _rows[cell.Y][cell.X] != '#';

    public Cell Find(char symbol)
    {
        for (var y = 0; y < _rows.Length; y++)
        {
            var x = _rows[y].IndexOf(symbol);
            if (x >= 0) return new Cell(x, y);
        }
        throw new InvalidOperationException($"'{symbol}' not found");
    }
}
```

- [ ] **Step 2: Write the failing tests**

`tests/Mortar.Tests/Gameplay/PathfinderTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Pathing;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class PathfinderTests
{
    private static bool Always(Cell cell) => true;

    [Fact]
    public void StraightLine_TakesTheDirectRoute()
    {
        var grid = new GridText("S....G");
        var path = Pathfinder.FindPath(grid.Find('S'), grid.Find('G'), grid.Walkable, grid.Walkable);
        Assert.NotNull(path);
        Assert.Equal(5, path!.Count);
        Assert.Equal(grid.Find('G'), path[^1]);
    }

    [Fact]
    public void Diagonal_CountsAsOneStep()
    {
        var grid = new GridText("S....", ".....", ".....", ".....", "....G");
        var path = Pathfinder.FindPath(grid.Find('S'), grid.Find('G'), grid.Walkable, grid.Walkable);
        Assert.Equal(4, path!.Count);
    }

    [Fact]
    public void WallCorners_AreNotCut()
    {
        var grid = new GridText("S#", ".G");
        var path = Pathfinder.FindPath(grid.Find('S'), grid.Find('G'), grid.Walkable, grid.Walkable);
        Assert.Equal(new[] { new Cell(0, 1), new Cell(1, 1) }, path);
    }

    [Fact]
    public void Unreachable_ReturnsNull()
    {
        var grid = new GridText("S#G");
        Assert.Null(Pathfinder.FindPath(grid.Find('S'), grid.Find('G'), grid.Walkable, grid.Walkable));
    }

    [Fact]
    public void OccupiedCells_AreAvoided()
    {
        var grid = new GridText("...", "S.G", "...");
        var blocked = new Cell(1, 1);
        var path = Pathfinder.FindPath(grid.Find('S'), grid.Find('G'), grid.Walkable, cell => grid.Walkable(cell) && cell != blocked);
        Assert.Equal(2, path!.Count);
        Assert.DoesNotContain(blocked, path);
    }

    [Fact]
    public void NextTo_StopsBesideTheTarget()
    {
        var grid = new GridText("S....T");
        var path = Pathfinder.FindPathNextTo(grid.Find('S'), grid.Find('T'), grid.Walkable, grid.Walkable);
        Assert.Equal(4, path!.Count);
        Assert.Equal(new Cell(4, 0), path[^1]);
    }

    [Fact]
    public void NextTo_AlreadyAdjacent_ReturnsEmpty()
    {
        var path = Pathfinder.FindPathNextTo(new Cell(0, 0), new Cell(1, 1), Always, Always);
        Assert.Empty(path!);
    }

    [Fact]
    public void SameInputs_GiveTheSamePath()
    {
        var first = Pathfinder.FindPath(new Cell(0, 0), new Cell(7, 3), Always, Always);
        var second = Pathfinder.FindPath(new Cell(0, 0), new Cell(7, 3), Always, Always);
        Assert.Equal(first, second);
    }

    [Fact]
    public void EndlessOpenArea_GivesUpAfterTheLimit()
    {
        static bool Quadrant(Cell cell) => cell.X >= 0 && cell.Y >= 0;
        Assert.Null(Pathfinder.FindPath(new Cell(0, 0), new Cell(-5, -5), Quadrant, Quadrant));
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay.PathfinderTests"`
Expected: build FAILS (`Mortar.Game.Pathing` does not exist).

- [ ] **Step 4: Implement**

`src/Mortar.Game/Pathing/MinHeap.cs`:

```csharp
namespace Mortar.Game.Pathing;

internal sealed class MinHeap
{
    private readonly List<Entry> _items = new List<Entry>();
    private int _sequence;

    public int Count => _items.Count;

    public void Push(Cell cell, int cost, int estimate)
    {
        _items.Add(new Entry(cell, cost + estimate, estimate, _sequence++));
        var index = _items.Count - 1;
        while (index > 0)
        {
            var parent = (index - 1) / 2;
            if (!Less(_items[index], _items[parent])) break;
            Swap(index, parent);
            index = parent;
        }
    }

    public Cell Pop()
    {
        var top = _items[0].Cell;
        var last = _items.Count - 1;
        _items[0] = _items[last];
        _items.RemoveAt(last);
        var index = 0;
        while (true)
        {
            var left = index * 2 + 1;
            if (left >= _items.Count) break;
            var right = left + 1;
            var child = right < _items.Count && Less(_items[right], _items[left]) ? right : left;
            if (!Less(_items[child], _items[index])) break;
            Swap(index, child);
            index = child;
        }
        return top;
    }

    private static bool Less(Entry a, Entry b)
    {
        if (a.Total != b.Total) return a.Total < b.Total;
        if (a.Estimate != b.Estimate) return a.Estimate < b.Estimate;
        return a.Sequence < b.Sequence;
    }

    private void Swap(int a, int b)
    {
        var held = _items[a];
        _items[a] = _items[b];
        _items[b] = held;
    }

    private readonly struct Entry
    {
        public Entry(Cell cell, int total, int estimate, int sequence)
        {
            Cell = cell;
            Total = total;
            Estimate = estimate;
            Sequence = sequence;
        }

        public Cell Cell { get; }

        public int Total { get; }

        public int Estimate { get; }

        public int Sequence { get; }
    }
}
```

`src/Mortar.Game/Pathing/Pathfinder.cs`:

```csharp
namespace Mortar.Game.Pathing;

public static class Pathfinder
{
    public const int MaxExpanded = 4096;

    public static IReadOnlyList<Cell>? FindPath(Cell start, Cell goal, Func<Cell, bool> walkable, Func<Cell, bool> free) =>
        Search(start, cell => cell == goal, cell => cell.DistanceTo(goal), walkable, free);

    public static IReadOnlyList<Cell>? FindPathNextTo(Cell start, Cell target, Func<Cell, bool> walkable, Func<Cell, bool> free) =>
        Search(start, cell => cell.IsAdjacentTo(target), cell => Math.Max(0, cell.DistanceTo(target) - 1), walkable, free);

    private static IReadOnlyList<Cell>? Search(
        Cell start,
        Func<Cell, bool> isGoal,
        Func<Cell, int> estimate,
        Func<Cell, bool> walkable,
        Func<Cell, bool> free)
    {
        var open = new MinHeap();
        var cost = new Dictionary<Cell, int> { [start] = 0 };
        var cameFrom = new Dictionary<Cell, Cell>();
        var closed = new HashSet<Cell>();
        open.Push(start, 0, estimate(start));
        while (open.Count > 0)
        {
            var current = open.Pop();
            if (!closed.Add(current)) continue;
            if (isGoal(current)) return Rebuild(cameFrom, start, current);
            if (closed.Count > MaxExpanded) return null;
            foreach (var direction in Directions.All)
            {
                var next = current.Step(direction);
                if (closed.Contains(next) || !CanStep(current, direction, walkable, free)) continue;
                var nextCost = cost[current] + 1;
                if (cost.TryGetValue(next, out var known) && known <= nextCost) continue;
                cost[next] = nextCost;
                cameFrom[next] = current;
                open.Push(next, nextCost, estimate(next));
            }
        }
        return null;
    }

    private static bool CanStep(Cell from, Direction direction, Func<Cell, bool> walkable, Func<Cell, bool> free)
    {
        var to = from.Step(direction);
        if (!free(to)) return false;
        if (!direction.IsDiagonal()) return true;
        return walkable(new Cell(to.X, from.Y)) && walkable(new Cell(from.X, to.Y));
    }

    private static IReadOnlyList<Cell> Rebuild(Dictionary<Cell, Cell> cameFrom, Cell start, Cell end)
    {
        var path = new List<Cell>();
        for (var cell = end; cell != start; cell = cameFrom[cell])
        {
            path.Add(cell);
        }
        path.Reverse();
        return path;
    }
}
```

`free` must already imply walkable; the level's `IsFree` does (Task 5).

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay.PathfinderTests"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Mortar.Game/Pathing tests/Mortar.Tests/Gameplay
git commit -m "add deterministic a-star pathfinding"
```

---

### Task 4: Player stats and leveling

**Files:**
- Create: `src/Mortar.Game/Simulation/CombatProfile.cs`, `PlayerProgress.cs`, `PlayerStats.cs`, `Leveling.cs`
- Test: `tests/Mortar.Tests/Gameplay/PlayerStatsTests.cs`, `tests/Mortar.Tests/Gameplay/LevelingTests.cs`

**Interfaces:**
- Consumes: Task 2 definitions.
- Produces (namespace `Mortar.Game.Simulation`):
  - `CombatProfile(int level, int maxLife, int maxMana, int armorClass, int toHit, IntRange damage, int blockChance, ActionTimings timings)`; `CombatProfile.ForMonster(MonsterDefinition, int maxLife)`.
  - `PlayerProgress(string classId, int level, long experience, Stats baseStats, int unspentPoints)` with `Level`, `Experience`, `BaseStats`, `UnspentPoints` (internal setters); `PlayerProgress.Start(ClassDefinition)`.
  - `PlayerStats.Compute(ClassDefinition, PlayerProgress, GameRules) -> CombatProfile`.
  - `Leveling.KillExperience(int baseExperience, int monsterLevel, int playerLevel) -> long`; `Leveling.AddExperience(PlayerProgress, long, ClassDefinition, GameRules) -> int` (levels gained); `Leveling.SpendPoint(PlayerProgress, StatKind, ClassDefinition) -> string?` (null on success, otherwise the reason).

- [ ] **Step 1: Write the failing tests**

`tests/Mortar.Tests/Gameplay/PlayerStatsTests.cs`:

```csharp
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class PlayerStatsTests
{
    [Fact]
    public void StartingWarrior_DerivesFromBaseStats()
    {
        var warrior = TestGameContent.Warrior();
        var profile = PlayerStats.Compute(warrior, PlayerProgress.Start(warrior), TestGameContent.Rules());
        Assert.Equal(1, profile.Level);
        Assert.Equal(50, profile.MaxLife);
        Assert.Equal(10, profile.MaxMana);
        Assert.Equal(4, profile.ArmorClass);
        Assert.Equal(71, profile.ToHit);
        Assert.Equal(4, profile.Damage.Min);
        Assert.Equal(6, profile.Damage.Max);
        Assert.Equal(0, profile.BlockChance);
    }

    [Fact]
    public void HigherLevel_AddsLifeManaAndToHit()
    {
        var warrior = TestGameContent.Warrior();
        var progress = new PlayerProgress("warrior", 3, 0, warrior.BaseStats, 0);
        var profile = PlayerStats.Compute(warrior, progress, TestGameContent.Rules());
        Assert.Equal(54, profile.MaxLife);
        Assert.Equal(12, profile.MaxMana);
        Assert.Equal(73, profile.ToHit);
    }
}
```

`tests/Mortar.Tests/Gameplay/LevelingTests.cs`:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class LevelingTests
{
    [Fact]
    public void KillExperience_ScalesWithLevelDifference()
    {
        Assert.Equal(50, Leveling.KillExperience(50, 1, 1));
        Assert.Equal(60, Leveling.KillExperience(50, 3, 1));
        Assert.Equal(0, Leveling.KillExperience(50, 1, 15));
    }

    [Fact]
    public void AddExperience_CanGainSeveralLevels()
    {
        var warrior = TestGameContent.Warrior();
        var progress = PlayerProgress.Start(warrior);
        var gained = Leveling.AddExperience(progress, 350, warrior, TestGameContent.Rules());
        Assert.Equal(2, gained);
        Assert.Equal(3, progress.Level);
        Assert.Equal(10, progress.UnspentPoints);
        Assert.Equal(350, progress.Experience);
    }

    [Fact]
    public void AddExperience_StopsAtMaxLevel()
    {
        var warrior = TestGameContent.Warrior();
        var progress = PlayerProgress.Start(warrior);
        Assert.Equal(3, Leveling.AddExperience(progress, 10_000, warrior, TestGameContent.Rules()));
        Assert.Equal(4, progress.Level);
    }

    [Fact]
    public void SpendPoint_NeedsUnspentPoints()
    {
        var warrior = TestGameContent.Warrior();
        Assert.Equal("no unspent stat points", Leveling.SpendPoint(PlayerProgress.Start(warrior), StatKind.Strength, warrior));
    }

    [Fact]
    public void SpendPoint_RaisesTheStat()
    {
        var warrior = TestGameContent.Warrior();
        var progress = new PlayerProgress("warrior", 2, 0, warrior.BaseStats, 1);
        Assert.Null(Leveling.SpendPoint(progress, StatKind.Strength, warrior));
        Assert.Equal(21, progress.BaseStats.Strength);
        Assert.Equal(0, progress.UnspentPoints);
    }

    [Fact]
    public void SpendPoint_RespectsTheCap()
    {
        var warrior = TestGameContent.Warrior();
        var progress = new PlayerProgress("warrior", 2, 0, new Stats(100, 20, 20, 10), 1);
        Assert.Equal("strength is at its cap", Leveling.SpendPoint(progress, StatKind.Strength, warrior));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay.PlayerStatsTests|FullyQualifiedName~Gameplay.LevelingTests"`
Expected: build FAILS (`Mortar.Game.Simulation` does not exist).

- [ ] **Step 3: Implement**

`src/Mortar.Game/Simulation/CombatProfile.cs`:

```csharp
using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public sealed class CombatProfile
{
    public CombatProfile(
        int level,
        int maxLife,
        int maxMana,
        int armorClass,
        int toHit,
        IntRange damage,
        int blockChance,
        ActionTimings timings)
    {
        Level = level;
        MaxLife = maxLife;
        MaxMana = maxMana;
        ArmorClass = armorClass;
        ToHit = toHit;
        Damage = damage;
        BlockChance = blockChance;
        Timings = timings;
    }

    public int Level { get; }

    public int MaxLife { get; }

    public int MaxMana { get; }

    public int ArmorClass { get; }

    public int ToHit { get; }

    public IntRange Damage { get; }

    public int BlockChance { get; }

    public ActionTimings Timings { get; }

    public static CombatProfile ForMonster(MonsterDefinition monster, int maxLife) =>
        new CombatProfile(monster.Level, maxLife, 0, monster.ArmorClass, monster.ToHit, monster.Damage, monster.BlockChance, monster.Timings);
}
```

`src/Mortar.Game/Simulation/PlayerProgress.cs`:

```csharp
using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public sealed class PlayerProgress
{
    public PlayerProgress(string classId, int level, long experience, Stats baseStats, int unspentPoints)
    {
        ClassId = classId;
        Level = level;
        Experience = experience;
        BaseStats = baseStats;
        UnspentPoints = unspentPoints;
    }

    public string ClassId { get; }

    public int Level { get; internal set; }

    public long Experience { get; internal set; }

    public Stats BaseStats { get; internal set; }

    public int UnspentPoints { get; internal set; }

    public static PlayerProgress Start(ClassDefinition definition) =>
        new PlayerProgress(definition.Id, 1, 0, definition.BaseStats, 0);
}
```

`src/Mortar.Game/Simulation/PlayerStats.cs`:

```csharp
using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public static class PlayerStats
{
    public static CombatProfile Compute(ClassDefinition definition, PlayerProgress progress, GameRules rules)
    {
        var stats = progress.BaseStats;
        var growth = definition.Growth;
        var levelsGained = progress.Level - 1;
        var maxLife = growth.LifeBase + stats.Vitality * growth.LifePerVitality + levelsGained * growth.LifePerLevel;
        var maxMana = growth.ManaBase + stats.Magic * growth.ManaPerMagic + levelsGained * growth.ManaPerLevel;
        var armorClass = stats.Dexterity / rules.DexterityPerArmor;
        var toHit = rules.PlayerBaseToHit + stats.Dexterity * rules.ToHitPerDexterity + progress.Level * rules.ToHitPerLevel;
        var damage = definition.UnarmedDamage.Shift(stats.Strength / rules.StrengthPerDamage);
        return new CombatProfile(progress.Level, maxLife, maxMana, armorClass, toHit, damage, 0, definition.Timings);
    }
}
```

`src/Mortar.Game/Simulation/Leveling.cs`:

```csharp
using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public static class Leveling
{
    public static long KillExperience(int baseExperience, int monsterLevel, int playerLevel) =>
        Math.Max(0L, (long)baseExperience * (10 + monsterLevel - playerLevel) / 10);

    public static int AddExperience(PlayerProgress progress, long amount, ClassDefinition definition, GameRules rules)
    {
        progress.Experience += amount;
        var gained = 0;
        while (progress.Level < rules.MaxLevel && progress.Experience >= rules.ExperienceThresholds[progress.Level - 1])
        {
            progress.Level++;
            progress.UnspentPoints += definition.Growth.StatPointsPerLevel;
            gained++;
        }
        return gained;
    }

    public static string? SpendPoint(PlayerProgress progress, StatKind stat, ClassDefinition definition)
    {
        if (progress.UnspentPoints <= 0)
        {
            return "no unspent stat points";
        }
        var current = progress.BaseStats.Get(stat);
        if (current >= definition.StatCaps.Get(stat))
        {
            return $"{stat.ToString().ToLowerInvariant()} is at its cap";
        }
        progress.BaseStats = progress.BaseStats.With(stat, current + 1);
        progress.UnspentPoints--;
        return null;
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay.PlayerStatsTests|FullyQualifiedName~Gameplay.LevelingTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Mortar.Game/Simulation tests/Mortar.Tests/Gameplay
git commit -m "add player stats and leveling"
```

---

### Task 5: World state and creation

**Files:**
- Create: `src/Mortar.Game/Simulation/ActorKind.cs`, `ActionKind.cs`, `IntentKind.cs`, `Actor.cs`, `LevelState.cs`, `ILevelGenerator.cs`, `LevelBuildResult.cs`, `LevelSeeds.cs`, `World.cs`, `WorldResult.cs`, `LevelEntry.cs`
- Test: `tests/Mortar.Tests/Gameplay/Support/TestLevels.cs`, `tests/Mortar.Tests/Gameplay/Support/TestWorlds.cs`, `tests/Mortar.Tests/Gameplay/LevelStateTests.cs`, `tests/Mortar.Tests/Gameplay/WorldCreationTests.cs`

**Interfaces:**
- Consumes: `Mortar.Core.Layouts.Layout`, `Marker`; `Mortar.Core.Model.MarkerKinds`; `Mortar.Core.Rng.Pcg32`, `Hashing`; Tasks 1–4.
- Produces (namespace `Mortar.Game.Simulation`):
  - `ActorKind { Player, Monster }`, `ActionKind { Idle, Moving, Attacking, Recovering }`, `IntentKind { None, MoveTo, Attack }`.
  - `Actor` with `PlayerId = 1`; public getters `Id`, `Kind`, `DefinitionId`, `Profile`, `Cell`, `Facing`, `Life`, `Mana`, `IsDead`, `IsAwake`, `LeaderId`, `LastAttackerId`, `Action`, `ActionElapsed`, `ActionDuration`, `ActionTargetId`, `Intent`, `IntentCell`, `IntentTargetId`, `IsAlive`; internal `StartAction(ActionKind, int duration, int targetId = 0)`, `ClearAction()`, `SetMoveIntent(Cell)`, `SetAttackIntent(int)`, `ClearIntent()`, `ApplyProfile(CombatProfile)`, `RestoreVitals()`, `MarkDead()`.
  - `LevelState` with `Depth`, `Seed`, `Layout`, `Actors` (ascending id), `InBounds`, `IsWalkable`, `IsFree`, `OccupantAt`, `Find(int)`, `FindMarker(string kind) -> Cell?`; internal `NearestFree(Cell, int maxRadius) -> Cell?`, `Add(Actor, Cell)`, `Move(Actor, Cell)`, `Vacate(Actor)`, `Remove(Actor)`.
  - `ILevelGenerator.Generate(LevelDefinition, uint seed) -> LevelBuildResult`; `LevelBuildResult.Success(Layout)` / `Failure(string)` with `Layout`, `Error`, `Succeeded`.
  - `LevelSeeds.For(uint worldSeed, int depth) -> uint`.
  - `World.Create(ContentSet, ILevelGenerator, string classId, uint seed) -> WorldResult`; `World` members `Content`, `Seed`, `TickCount`, `Depth`, `PlayerClass`, `Progress`, `Player`, `Level`, `IsGameOver`, `VisitedDepths`, `GetLevel(int)`; internal `Rng`, `Generator`, `FindLevel(int)`, `AddLevel(LevelState)`, `SpawnMonster(LevelState, MonsterDefinition, Cell) -> Actor`.
  - `WorldResult` with `World`, `Error`, `Succeeded`.
  - Internal `LevelEntry.Enter(World, int depth, string arrivalMarker) -> string?` (null on success).

- [ ] **Step 1: Write the test helpers**

`tests/Mortar.Tests/Gameplay/Support/TestLevels.cs`:

```csharp
using Mortar.Core.Layouts;
using Mortar.Core.Model;
using Mortar.Game.Content;
using Mortar.Game.Simulation;

namespace Mortar.Tests.Gameplay.Support;

/// <summary>'#' is wall; every other character is floor. Marker characters (E, X, S, B, W, C) also place markers.</summary>
internal static class TestLevels
{
    public static Layout FromAscii(params string[] rows)
    {
        var height = rows.Length;
        var width = rows[0].Length;
        var ground = new int[width * height];
        var walkable = new bool[width * height];
        var markers = new List<Marker>();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var symbol = rows[y][x];
                var index = y * width + x;
                ground[index] = symbol == '#' ? 1 : 0;
                walkable[index] = symbol != '#';
                if (MarkerChars.TryGetKind(symbol, out var kind)) markers.Add(new Marker(kind, null, x, y));
            }
        }
        var cells = new LayoutCells(
            width, height, new[] { "floor", "wall" }, ground, new[] { "floor" },
            new int[width * height], new int[width * height], walkable);
        return new Layout("test", 0, 0, cells, markers, "test", Layout.CurrentFormatVersion);
    }
}

internal sealed class FixedLevelGenerator : ILevelGenerator
{
    private readonly string[][] _levels;

    public FixedLevelGenerator(params string[][] levels)
    {
        _levels = levels;
    }

    public LevelBuildResult Generate(LevelDefinition level, uint seed) =>
        level.Depth <= _levels.Length
            ? LevelBuildResult.Success(TestLevels.FromAscii(_levels[level.Depth - 1]))
            : LevelBuildResult.Failure("no test layout");
}
```

`tests/Mortar.Tests/Gameplay/Support/TestWorlds.cs`:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Simulation;

namespace Mortar.Tests.Gameplay.Support;

internal static class TestWorlds
{
    public static readonly string[] Room =
    {
        "#########",
        "#E......#",
        "#.......#",
        "#.......#",
        "#......X#",
        "#########",
    };

    public static World Create(ContentSet? content = null, uint seed = 1, string[][]? levels = null)
    {
        var generator = new FixedLevelGenerator(levels ?? new[] { Room, Room });
        var result = World.Create(content ?? TestGameContent.Valid(), generator, "warrior", seed);
        return result.World ?? throw new InvalidOperationException(result.Error);
    }

    public static Actor Spawn(World world, string monsterId, int x, int y) =>
        world.SpawnMonster(world.Level, world.Content.Monster(monsterId), new Cell(x, y));
}
```

Add `using Mortar.Game;` to the top of `TestWorlds.cs`.

- [ ] **Step 2: Write the failing tests**

`tests/Mortar.Tests/Gameplay/LevelStateTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class LevelStateTests
{
    private static LevelState Level() =>
        new LevelState(1, 0, TestLevels.FromAscii("#####", "#...#", "#...#", "#####"));

    private static Actor Monster(int id) =>
        new Actor(id, ActorKind.Monster, "zombie", CombatProfile.ForMonster(TestGameContent.Monster("zombie"), 10));

    [Fact]
    public void IsWalkable_IsFalseForWallsAndOutsideTheMap()
    {
        var level = Level();
        Assert.False(level.IsWalkable(new Cell(-1, 0)));
        Assert.False(level.IsWalkable(new Cell(5, 1)));
        Assert.False(level.IsWalkable(new Cell(0, 0)));
        Assert.True(level.IsWalkable(new Cell(1, 1)));
    }

    [Fact]
    public void Add_KeepsActorsInIdOrderAndMarksOccupancy()
    {
        var level = Level();
        level.Add(Monster(3), new Cell(1, 1));
        level.Add(Monster(2), new Cell(2, 1));
        Assert.Equal(new[] { 2, 3 }, level.Actors.Select(a => a.Id));
        Assert.Equal(3, level.OccupantAt(new Cell(1, 1)));
        Assert.False(level.IsFree(new Cell(1, 1)));
        Assert.Equal(new Cell(2, 1), level.Find(2)!.Cell);
    }

    [Fact]
    public void Move_UpdatesOccupancy()
    {
        var level = Level();
        var monster = Monster(2);
        level.Add(monster, new Cell(1, 1));
        level.Move(monster, new Cell(2, 2));
        Assert.True(level.IsFree(new Cell(1, 1)));
        Assert.Equal(2, level.OccupantAt(new Cell(2, 2)));
        Assert.Equal(new Cell(2, 2), monster.Cell);
    }

    [Fact]
    public void NearestFree_PrefersTheOriginThenTheClosestRing()
    {
        var level = Level();
        Assert.Equal(new Cell(1, 1), level.NearestFree(new Cell(1, 1), 2));
        level.Add(Monster(2), new Cell(1, 1));
        Assert.Equal(new Cell(2, 1), level.NearestFree(new Cell(1, 1), 2));
    }
}
```

`tests/Mortar.Tests/Gameplay/WorldCreationTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class WorldCreationTests
{
    [Fact]
    public void Create_PlacesThePlayerAtTheEntrance()
    {
        var world = TestWorlds.Create();
        Assert.Equal(1, world.Depth);
        Assert.Equal(0, world.TickCount);
        Assert.Equal(new Cell(1, 1), world.Player.Cell);
        Assert.Equal(world.Player.Profile.MaxLife, world.Player.Life);
        Assert.Same(world.Player, world.Level.Find(Actor.PlayerId));
        Assert.Equal(new[] { 1 }, world.VisitedDepths);
    }

    [Fact]
    public void Create_UnknownClass_Fails()
    {
        var result = World.Create(TestGameContent.Valid(), new FixedLevelGenerator(TestWorlds.Room), "bard", 1);
        Assert.False(result.Succeeded);
        Assert.Equal("unknown class 'bard'", result.Error);
    }

    [Fact]
    public void Create_GeneratorFailure_Fails()
    {
        var result = World.Create(TestGameContent.Valid(), new FixedLevelGenerator(), "warrior", 1);
        Assert.Equal("level 1 could not be generated: no test layout", result.Error);
    }

    [Fact]
    public void Create_MissingEntrance_Fails()
    {
        var generator = new FixedLevelGenerator(new[] { "###", "#.#", "###" });
        var result = World.Create(TestGameContent.Valid(), generator, "warrior", 1);
        Assert.Equal("level 1 has no entrance marker", result.Error);
    }

    [Fact]
    public void LevelSeeds_DifferByDepthAndWorldSeed()
    {
        Assert.NotEqual(LevelSeeds.For(1, 1), LevelSeeds.For(1, 2));
        Assert.NotEqual(LevelSeeds.For(1, 1), LevelSeeds.For(2, 1));
        Assert.Equal(LevelSeeds.For(5, 3), LevelSeeds.For(5, 3));
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay.LevelStateTests|FullyQualifiedName~Gameplay.WorldCreationTests"`
Expected: build FAILS (`Actor`, `LevelState`, `World` do not exist).

- [ ] **Step 4: Implement the actor types**

`src/Mortar.Game/Simulation/ActorKind.cs`:

```csharp
namespace Mortar.Game.Simulation;

public enum ActorKind
{
    Player,
    Monster,
}
```

`src/Mortar.Game/Simulation/ActionKind.cs`:

```csharp
namespace Mortar.Game.Simulation;

public enum ActionKind
{
    Idle,
    Moving,
    Attacking,
    Recovering,
}
```

`src/Mortar.Game/Simulation/IntentKind.cs`:

```csharp
namespace Mortar.Game.Simulation;

public enum IntentKind
{
    None,
    MoveTo,
    Attack,
}
```

`src/Mortar.Game/Simulation/Actor.cs`:

```csharp
namespace Mortar.Game.Simulation;

public sealed class Actor
{
    public const int PlayerId = 1;

    internal Actor(int id, ActorKind kind, string definitionId, CombatProfile profile)
    {
        Id = id;
        Kind = kind;
        DefinitionId = definitionId;
        Profile = profile;
        Life = profile.MaxLife;
        Mana = profile.MaxMana;
    }

    public int Id { get; }

    public ActorKind Kind { get; }

    public string DefinitionId { get; }

    public CombatProfile Profile { get; private set; }

    public Cell Cell { get; internal set; }

    public Direction Facing { get; internal set; }

    public int Life { get; internal set; }

    public int Mana { get; internal set; }

    public bool IsDead { get; private set; }

    public bool IsAwake { get; internal set; }

    public int LeaderId { get; internal set; }

    public int LastAttackerId { get; internal set; }

    public ActionKind Action { get; private set; }

    public int ActionElapsed { get; internal set; }

    public int ActionDuration { get; private set; }

    public int ActionTargetId { get; private set; }

    public IntentKind Intent { get; private set; }

    public Cell IntentCell { get; private set; }

    public int IntentTargetId { get; private set; }

    public bool IsAlive => !IsDead && Life > 0;

    internal void StartAction(ActionKind kind, int duration, int targetId = 0)
    {
        Action = kind;
        ActionDuration = duration;
        ActionElapsed = 0;
        ActionTargetId = targetId;
    }

    internal void ClearAction() => StartAction(ActionKind.Idle, 0);

    internal void SetMoveIntent(Cell cell)
    {
        Intent = IntentKind.MoveTo;
        IntentCell = cell;
        IntentTargetId = 0;
    }

    internal void SetAttackIntent(int targetId)
    {
        Intent = IntentKind.Attack;
        IntentCell = default;
        IntentTargetId = targetId;
    }

    internal void ClearIntent()
    {
        Intent = IntentKind.None;
        IntentCell = default;
        IntentTargetId = 0;
    }

    internal void ApplyProfile(CombatProfile profile)
    {
        Life = Math.Max(1, Life + profile.MaxLife - Profile.MaxLife);
        Mana = Math.Max(0, Mana + profile.MaxMana - Profile.MaxMana);
        Profile = profile;
    }

    internal void RestoreVitals()
    {
        Life = Profile.MaxLife;
        Mana = Profile.MaxMana;
    }

    internal void MarkDead()
    {
        IsDead = true;
        Life = Math.Min(Life, 0);
        ClearAction();
        ClearIntent();
    }
}
```

- [ ] **Step 5: Implement the level state**

`src/Mortar.Game/Simulation/LevelState.cs`:

```csharp
using Mortar.Core.Layouts;

namespace Mortar.Game.Simulation;

public sealed class LevelState
{
    private readonly int[] _occupants;
    private readonly List<Actor> _actors = new List<Actor>();
    private readonly Dictionary<int, Actor> _byId = new Dictionary<int, Actor>();

    internal LevelState(int depth, uint seed, Layout layout)
    {
        Depth = depth;
        Seed = seed;
        Layout = layout;
        _occupants = new int[layout.Width * layout.Height];
    }

    public int Depth { get; }

    public uint Seed { get; }

    public Layout Layout { get; }

    public IReadOnlyList<Actor> Actors => _actors;

    public bool InBounds(Cell cell) =>
        cell.X >= 0 && cell.Y >= 0 && cell.X < Layout.Width && cell.Y < Layout.Height;

    public bool IsWalkable(Cell cell) => InBounds(cell) && Layout.Cells.IsWalkable(cell.X, cell.Y);

    public bool IsFree(Cell cell) => IsWalkable(cell) && _occupants[Index(cell)] == 0;

    public int OccupantAt(Cell cell) => InBounds(cell) ? _occupants[Index(cell)] : 0;

    public Actor? Find(int id) => _byId.TryGetValue(id, out var actor) ? actor : null;

    public Cell? FindMarker(string kind)
    {
        foreach (var marker in Layout.Markers)
        {
            if (marker.Kind == kind) return new Cell(marker.X, marker.Y);
        }
        return null;
    }

    internal Cell? NearestFree(Cell origin, int maxRadius)
    {
        for (var radius = 0; radius <= maxRadius; radius++)
        {
            for (var y = origin.Y - radius; y <= origin.Y + radius; y++)
            {
                for (var x = origin.X - radius; x <= origin.X + radius; x++)
                {
                    var cell = new Cell(x, y);
                    if (cell.DistanceTo(origin) == radius && IsFree(cell)) return cell;
                }
            }
        }
        return null;
    }

    internal void Add(Actor actor, Cell cell)
    {
        if (!IsFree(cell))
        {
            throw new InvalidOperationException($"cell {cell} is not free");
        }
        actor.Cell = cell;
        _occupants[Index(cell)] = actor.Id;
        var index = _actors.FindIndex(existing => existing.Id > actor.Id);
        _actors.Insert(index < 0 ? _actors.Count : index, actor);
        _byId[actor.Id] = actor;
    }

    internal void Move(Actor actor, Cell to)
    {
        Vacate(actor);
        _occupants[Index(to)] = actor.Id;
        actor.Cell = to;
    }

    internal void Vacate(Actor actor)
    {
        var index = Index(actor.Cell);
        if (_occupants[index] == actor.Id) _occupants[index] = 0;
    }

    internal void Remove(Actor actor)
    {
        Vacate(actor);
        _actors.Remove(actor);
        _byId.Remove(actor.Id);
    }

    private int Index(Cell cell) => cell.Y * Layout.Width + cell.X;
}
```

- [ ] **Step 6: Implement level generation seams and the world**

`src/Mortar.Game/Simulation/ILevelGenerator.cs`:

```csharp
using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public interface ILevelGenerator
{
    LevelBuildResult Generate(LevelDefinition level, uint seed);
}
```

`src/Mortar.Game/Simulation/LevelBuildResult.cs`:

```csharp
using Mortar.Core.Layouts;

namespace Mortar.Game.Simulation;

public sealed class LevelBuildResult
{
    private LevelBuildResult(Layout? layout, string? error)
    {
        Layout = layout;
        Error = error;
    }

    public Layout? Layout { get; }

    public string? Error { get; }

    public bool Succeeded => Layout is not null;

    public static LevelBuildResult Success(Layout layout) => new LevelBuildResult(layout, null);

    public static LevelBuildResult Failure(string error) => new LevelBuildResult(null, error);
}
```

`src/Mortar.Game/Simulation/LevelSeeds.cs`:

```csharp
using Mortar.Core.Rng;

namespace Mortar.Game.Simulation;

public static class LevelSeeds
{
    public static uint For(uint worldSeed, int depth) =>
        (uint)(Hashing.SplitMix64(((ulong)worldSeed << 16) ^ (uint)depth) >> 32);
}
```

`src/Mortar.Game/Simulation/WorldResult.cs`:

```csharp
namespace Mortar.Game.Simulation;

public sealed class WorldResult
{
    private WorldResult(World? world, string? error)
    {
        World = world;
        Error = error;
    }

    public World? World { get; }

    public string? Error { get; }

    public bool Succeeded => World is not null;

    internal static WorldResult Success(World world) => new WorldResult(world, null);

    internal static WorldResult Failure(string error) => new WorldResult(null, error);
}
```

`src/Mortar.Game/Simulation/World.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public sealed class World
{
    private readonly Dictionary<int, LevelState> _levels = new Dictionary<int, LevelState>();

    private World(ContentSet content, ILevelGenerator generator, uint seed, ClassDefinition playerClass)
    {
        Content = content;
        Generator = generator;
        Seed = seed;
        PlayerClass = playerClass;
        Rng = Pcg32.ForAttempt("world", seed, 0);
        Progress = PlayerProgress.Start(playerClass);
        var profile = PlayerStats.Compute(playerClass, Progress, content.Rules);
        Player = new Actor(Actor.PlayerId, ActorKind.Player, playerClass.Id, profile);
    }

    public ContentSet Content { get; }

    public uint Seed { get; }

    public long TickCount { get; internal set; }

    public int Depth { get; internal set; }

    public ClassDefinition PlayerClass { get; }

    public PlayerProgress Progress { get; }

    public Actor Player { get; }

    public LevelState Level => _levels[Depth];

    public bool IsGameOver => Player.IsDead;

    public IReadOnlyList<int> VisitedDepths => _levels.Keys.OrderBy(depth => depth).ToList();

    internal Pcg32 Rng { get; }

    internal ILevelGenerator Generator { get; }

    internal int NextActorId { get; set; } = Actor.PlayerId + 1;

    public static WorldResult Create(ContentSet content, ILevelGenerator generator, string classId, uint seed)
    {
        if (!content.TryGetClass(classId, out var playerClass))
        {
            return WorldResult.Failure($"unknown class '{classId}'");
        }
        var world = new World(content, generator, seed, playerClass);
        var error = LevelEntry.Enter(world, 1, MarkerKinds.Entrance);
        return error is null ? WorldResult.Success(world) : WorldResult.Failure(error);
    }

    public LevelState GetLevel(int depth) => _levels[depth];

    internal LevelState? FindLevel(int depth) => _levels.TryGetValue(depth, out var level) ? level : null;

    internal void AddLevel(LevelState level) => _levels.Add(level.Depth, level);

    internal Actor SpawnMonster(LevelState level, MonsterDefinition monster, Cell cell)
    {
        var maxLife = Rng.NextInt(monster.Life.Min, monster.Life.Max + 1);
        var actor = new Actor(NextActorId++, ActorKind.Monster, monster.Id, CombatProfile.ForMonster(monster, maxLife));
        level.Add(actor, cell);
        return actor;
    }
}
```

`src/Mortar.Game/Simulation/LevelEntry.cs`:

```csharp
namespace Mortar.Game.Simulation;

internal static class LevelEntry
{
    public const int ArrivalSearchRadius = 3;

    public static string? Enter(World world, int depth, string arrivalMarker)
    {
        var level = world.FindLevel(depth);
        if (level is null)
        {
            level = Build(world, depth, out var buildError);
            if (level is null) return buildError;
        }
        var anchor = level.FindMarker(arrivalMarker);
        if (anchor is null)
        {
            return $"level {depth} has no {arrivalMarker} marker";
        }
        var cell = level.NearestFree(anchor.Value, ArrivalSearchRadius);
        if (cell is null)
        {
            return $"level {depth} has no free cell near its {arrivalMarker}";
        }
        if (world.Depth != 0)
        {
            world.Level.Remove(world.Player);
        }
        world.Player.ClearAction();
        world.Player.ClearIntent();
        level.Add(world.Player, cell.Value);
        world.Depth = depth;
        return null;
    }

    private static LevelState? Build(World world, int depth, out string? error)
    {
        var definition = world.Content.Level(depth);
        var seed = LevelSeeds.For(world.Seed, depth);
        var result = world.Generator.Generate(definition, seed);
        if (!result.Succeeded)
        {
            error = $"level {depth} could not be generated: {result.Error}";
            return null;
        }
        var level = new LevelState(depth, seed, result.Layout!);
        world.AddLevel(level);
        error = null;
        return level;
    }
}
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay.LevelStateTests|FullyQualifiedName~Gameplay.WorldCreationTests"`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add src/Mortar.Game/Simulation tests/Mortar.Tests/Gameplay
git commit -m "add world, level state, and actors"
```

---

### Task 6: Tick loop, commands, events, and movement

**Files:**
- Create: `src/Mortar.Game/Commands/Command.cs`, `src/Mortar.Game/Commands/MoveCommand.cs`
- Create: `src/Mortar.Game/Events/GameEvent.cs`, `EventSink.cs`, `ActorMoved.cs`, `CommandRejected.cs`
- Create: `src/Mortar.Game/Simulation/TickRunner.cs`, `CommandSystem.cs`, `ActionSystem.cs`, `Movement.cs`
- Modify: `src/Mortar.Game/Simulation/World.cs`
- Test: `tests/Mortar.Tests/Gameplay/MovementTests.cs`

**Interfaces:**
- Consumes: Tasks 1–5.
- Produces:
  - `Mortar.Game.Commands.Command` (abstract, `Describe()`); `MoveCommand(Cell destination)` with `Destination`.
  - `Mortar.Game.Events.GameEvent` (abstract, `Tick`, `ToString()` = `"{Tick} {description}"`); `ActorMoved(long tick, int actorId, Cell from, Cell to, int durationTicks)`; `CommandRejected(long tick, Command command, string reason)`.
  - Internal `EventSink(long tick)` with `Tick`, `Events`, `Add(GameEvent)`, `Reject(Command, string)`.
  - `World.Tick(IReadOnlyList<Command>) -> IReadOnlyList<GameEvent>`.
  - Internal `ActionSystem.Advance(World, EventSink) -> List<Actor>` (actors whose swing reaches its hit frame this tick); `Movement.Step(LevelState, Actor, Cell, EventSink)`; `Movement.ContinueMoveTo(LevelState, Actor, EventSink)`; `CommandSystem.Apply(World, IReadOnlyList<Command>, EventSink)`.

- [ ] **Step 1: Write the failing tests**

`tests/Mortar.Tests/Gameplay/MovementTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class MovementTests
{
    private static readonly Command[] None = Array.Empty<Command>();

    [Fact]
    public void MoveCommand_StartsAStepImmediately()
    {
        var world = TestWorlds.Create();
        var events = world.Tick(new Command[] { new MoveCommand(new Cell(4, 1)) });
        var moved = Assert.IsType<ActorMoved>(Assert.Single(events));
        Assert.Equal(Actor.PlayerId, moved.ActorId);
        Assert.Equal(new Cell(1, 1), moved.From);
        Assert.Equal(new Cell(2, 1), moved.To);
        Assert.Equal(4, moved.DurationTicks);
        Assert.Equal(new Cell(2, 1), world.Player.Cell);
        Assert.Equal(Direction.East, world.Player.Facing);
        Assert.Equal(ActionKind.Moving, world.Player.Action);
    }

    [Fact]
    public void Walking_FollowsThePathUntilTheDestination()
    {
        var world = TestWorlds.Create();
        var moves = new List<ActorMoved>();
        moves.AddRange(world.Tick(new Command[] { new MoveCommand(new Cell(4, 1)) }).OfType<ActorMoved>());
        for (var i = 0; i < 12; i++)
        {
            moves.AddRange(world.Tick(None).OfType<ActorMoved>());
        }
        Assert.Equal(new long[] { 1, 5, 9 }, moves.Select(m => m.Tick));
        Assert.Equal(new Cell(4, 1), world.Player.Cell);
        Assert.Equal(ActionKind.Idle, world.Player.Action);
        Assert.Equal(IntentKind.None, world.Player.Intent);
    }

    [Fact]
    public void MoveIntoAWall_IsRejected()
    {
        var world = TestWorlds.Create();
        var events = world.Tick(new Command[] { new MoveCommand(new Cell(0, 0)) });
        var rejected = Assert.IsType<CommandRejected>(Assert.Single(events));
        Assert.Equal("destination is not walkable", rejected.Reason);
        Assert.Equal("1 rejected 'move to (0,0)': destination is not walkable", rejected.ToString());
    }

    [Fact]
    public void NewDestination_TakesEffectAfterTheCurrentStep()
    {
        var world = TestWorlds.Create();
        world.Tick(new Command[] { new MoveCommand(new Cell(4, 1)) });
        var events = new List<GameEvent>();
        events.AddRange(world.Tick(new Command[] { new MoveCommand(new Cell(1, 4)) }));
        for (var i = 0; i < 3; i++)
        {
            events.AddRange(world.Tick(None));
        }
        var moved = Assert.IsType<ActorMoved>(Assert.Single(events));
        Assert.Equal(5, moved.Tick);
        Assert.Equal(new Cell(2, 1), moved.From);
        Assert.Equal(new Cell(2, 2), moved.To);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay.MovementTests"`
Expected: build FAILS (`Mortar.Game.Commands` does not exist).

- [ ] **Step 3: Implement commands and events**

`src/Mortar.Game/Commands/Command.cs`:

```csharp
namespace Mortar.Game.Commands;

public abstract class Command
{
    public abstract string Describe();

    public override string ToString() => Describe();
}
```

`src/Mortar.Game/Commands/MoveCommand.cs`:

```csharp
namespace Mortar.Game.Commands;

public sealed class MoveCommand : Command
{
    public MoveCommand(Cell destination)
    {
        Destination = destination;
    }

    public Cell Destination { get; }

    public override string Describe() => $"move to {Destination}";
}
```

`src/Mortar.Game/Events/GameEvent.cs`:

```csharp
namespace Mortar.Game.Events;

public abstract class GameEvent
{
    protected GameEvent(long tick)
    {
        Tick = tick;
    }

    public long Tick { get; }

    public override string ToString() => $"{Tick} {Describe()}";

    protected abstract string Describe();
}
```

`src/Mortar.Game/Events/EventSink.cs`:

```csharp
using Mortar.Game.Commands;

namespace Mortar.Game.Events;

internal sealed class EventSink
{
    private readonly List<GameEvent> _events = new List<GameEvent>();

    public EventSink(long tick)
    {
        Tick = tick;
    }

    public long Tick { get; }

    public IReadOnlyList<GameEvent> Events => _events;

    public void Add(GameEvent gameEvent) => _events.Add(gameEvent);

    public void Reject(Command command, string reason) => _events.Add(new CommandRejected(Tick, command, reason));
}
```

`src/Mortar.Game/Events/ActorMoved.cs`:

```csharp
namespace Mortar.Game.Events;

public sealed class ActorMoved : GameEvent
{
    public ActorMoved(long tick, int actorId, Cell from, Cell to, int durationTicks)
        : base(tick)
    {
        ActorId = actorId;
        From = from;
        To = to;
        DurationTicks = durationTicks;
    }

    public int ActorId { get; }

    public Cell From { get; }

    public Cell To { get; }

    public int DurationTicks { get; }

    protected override string Describe() => $"#{ActorId} moved {From}->{To} in {DurationTicks}";
}
```

`src/Mortar.Game/Events/CommandRejected.cs`:

```csharp
using Mortar.Game.Commands;

namespace Mortar.Game.Events;

public sealed class CommandRejected : GameEvent
{
    public CommandRejected(long tick, Command command, string reason)
        : base(tick)
    {
        Command = command;
        Reason = reason;
    }

    public Command Command { get; }

    public string Reason { get; }

    protected override string Describe() => $"rejected '{Command.Describe()}': {Reason}";
}
```

- [ ] **Step 4: Implement the tick loop and movement**

`src/Mortar.Game/Simulation/TickRunner.cs`:

```csharp
using Mortar.Game.Commands;
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class TickRunner
{
    public static IReadOnlyList<GameEvent> Run(World world, IReadOnlyList<Command> commands)
    {
        world.TickCount++;
        var events = new EventSink(world.TickCount);
        if (world.IsGameOver)
        {
            foreach (var command in commands) events.Reject(command, "the player is dead");
            return events.Events;
        }
        CommandSystem.Apply(world, commands, events);
        ActionSystem.Advance(world, events);
        return events.Events;
    }
}
```

`src/Mortar.Game/Simulation/CommandSystem.cs`:

```csharp
using Mortar.Game.Commands;
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class CommandSystem
{
    public static void Apply(World world, IReadOnlyList<Command> commands, EventSink events)
    {
        foreach (var command in commands)
        {
            var reason = Execute(world, command, events);
            if (reason is not null) events.Reject(command, reason);
        }
    }

    private static string? Execute(World world, Command command, EventSink events)
    {
        switch (command)
        {
            case MoveCommand move:
                return Move(world, move);
            default:
                return "unsupported command";
        }
    }

    private static string? Move(World world, MoveCommand move)
    {
        if (!world.Level.IsWalkable(move.Destination))
        {
            return "destination is not walkable";
        }
        world.Player.SetMoveIntent(move.Destination);
        return null;
    }
}
```

`Execute` takes `events` now because the commands added in Tasks 7 and 9 emit events.

`src/Mortar.Game/Simulation/ActionSystem.cs`:

```csharp
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class ActionSystem
{
    public static List<Actor> Advance(World world, EventSink events)
    {
        var level = world.Level;
        var due = new List<Actor>();
        foreach (var actor in level.Actors)
        {
            if (actor.IsAlive) Progress(actor, due);
        }
        foreach (var actor in level.Actors)
        {
            if (actor.IsAlive && actor.Action == ActionKind.Idle) StartFromIntent(level, actor, events);
        }
        return due;
    }

    private static void Progress(Actor actor, List<Actor> due)
    {
        if (actor.Action == ActionKind.Idle) return;
        actor.ActionElapsed++;
        if (actor.Action == ActionKind.Attacking && actor.ActionElapsed == actor.Profile.Timings.HitFrame)
        {
            due.Add(actor);
        }
        if (actor.ActionElapsed >= actor.ActionDuration) actor.ClearAction();
    }

    private static void StartFromIntent(LevelState level, Actor actor, EventSink events)
    {
        if (actor.Intent == IntentKind.MoveTo) Movement.ContinueMoveTo(level, actor, events);
    }
}
```

`src/Mortar.Game/Simulation/Movement.cs`:

```csharp
using Mortar.Game.Events;
using Mortar.Game.Pathing;

namespace Mortar.Game.Simulation;

internal static class Movement
{
    public static void ContinueMoveTo(LevelState level, Actor actor, EventSink events)
    {
        if (actor.Cell == actor.IntentCell)
        {
            actor.ClearIntent();
            return;
        }
        var path = Pathfinder.FindPath(actor.Cell, actor.IntentCell, level.IsWalkable, level.IsFree);
        if (path is null)
        {
            actor.ClearIntent();
            return;
        }
        Step(level, actor, path[0], events);
        if (actor.Cell == actor.IntentCell) actor.ClearIntent();
    }

    public static void Step(LevelState level, Actor actor, Cell next, EventSink events)
    {
        var from = actor.Cell;
        var duration = actor.Profile.Timings.MoveTicks;
        actor.Facing = Directions.Toward(from, next);
        level.Move(actor, next);
        actor.StartAction(ActionKind.Moving, duration);
        events.Add(new ActorMoved(events.Tick, actor.Id, from, next, duration));
    }
}
```

In `src/Mortar.Game/Simulation/World.cs`, add `using Mortar.Game.Commands;` and `using Mortar.Game.Events;` at the top and add this member after `GetLevel`:

```csharp
    public IReadOnlyList<GameEvent> Tick(IReadOnlyList<Command> commands) => TickRunner.Run(this, commands);
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay"`
Expected: PASS (all gameplay tests so far).

- [ ] **Step 6: Commit**

```bash
git add src/Mortar.Game tests/Mortar.Tests/Gameplay
git commit -m "add tick loop, commands, events, and movement"
```

---

### Task 7: Melee combat, death, and experience

**Files:**
- Create: `src/Mortar.Game/Commands/AttackCommand.cs`, `src/Mortar.Game/Commands/SpendStatPointCommand.cs`
- Create: `src/Mortar.Game/Events/CombatEvents.cs`, `src/Mortar.Game/Events/ProgressEvents.cs`
- Create: `src/Mortar.Game/Simulation/HitChance.cs`, `CombatSystem.cs`, `DeathSystem.cs`, `PlayerGrowth.cs`
- Modify: `src/Mortar.Game/Simulation/CommandSystem.cs`, `ActionSystem.cs`, `TickRunner.cs`
- Modify: `tests/Mortar.Tests/Gameplay/Support/TestWorlds.cs`
- Test: `tests/Mortar.Tests/Gameplay/CombatTests.cs`, `tests/Mortar.Tests/Gameplay/ProgressionTests.cs`

**Interfaces:**
- Consumes: Tasks 1–6.
- Produces:
  - `AttackCommand(int targetId)` with `TargetId`; `SpendStatPointCommand(StatKind stat)` with `Stat`.
  - `AttackOutcome { Miss, Blocked, Hit }`; events `AttackStarted(long tick, int attackerId, int targetId, int durationTicks)`, `AttackResolved(long tick, int attackerId, int targetId, AttackOutcome outcome, int damage)`, `ActorStaggered(long tick, int actorId, int durationTicks)`, `ActorDied(long tick, int actorId, int killerId)`, `ExperienceGained(long tick, long amount, long total)`, `LeveledUp(long tick, int level)`, `StatIncreased(long tick, StatKind stat, int value)`.
  - `HitChance.Compute(CombatProfile attacker, CombatProfile defender, GameRules rules) -> int` (percent).
  - Internal `CombatSystem.StartAttack(Actor, Actor, EventSink)`, `CombatSystem.Resolve(World, IReadOnlyList<Actor>, EventSink)`, `DeathSystem.Process(World, EventSink)`, `PlayerGrowth.RewardKill(World, Actor, EventSink)`, `PlayerGrowth.SpendPoint(World, StatKind, EventSink) -> string?`.
  - Test helper `TestWorlds.Run(World, int ticks, params Command[] first) -> List<GameEvent>`.

Tests filter events by type because Task 8 adds monster AI, which makes spawned monsters fight back and emit their own events.

- [ ] **Step 1: Add the run helper to `TestWorlds`**

Add `using Mortar.Game.Commands;` and `using Mortar.Game.Events;` to `tests/Mortar.Tests/Gameplay/Support/TestWorlds.cs` and add this method to the class:

```csharp
    public static List<GameEvent> Run(World world, int ticks, params Command[] first)
    {
        var events = new List<GameEvent>(world.Tick(first));
        for (var i = 1; i < ticks; i++)
        {
            events.AddRange(world.Tick(Array.Empty<Command>()));
        }
        return events;
    }
```

- [ ] **Step 2: Write the failing tests**

`tests/Mortar.Tests/Gameplay/CombatTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class CombatTests
{
    private static World Arena(GameRules? rules = null) =>
        TestWorlds.Create(TestGameContent.Valid(rules: rules ?? TestGameContent.Rules(minHit: 100, maxHit: 100)));

    [Fact]
    public void Attack_SwingsAndLandsOnTheHitFrame()
    {
        var world = Arena();
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        var events = TestWorlds.Run(world, 4, new AttackCommand(zombie.Id));
        var started = events.OfType<AttackStarted>().First(e => e.AttackerId == Actor.PlayerId);
        Assert.Equal(1, started.Tick);
        Assert.Equal(6, started.DurationTicks);
        var hit = events.OfType<AttackResolved>().Single(e => e.AttackerId == Actor.PlayerId);
        Assert.Equal(4, hit.Tick);
        Assert.Equal(AttackOutcome.Hit, hit.Outcome);
        Assert.InRange(hit.Damage, 4, 6);
        Assert.Equal(10 - hit.Damage, zombie.Life);
    }

    [Fact]
    public void AttackingUntilDeath_RewardsExperience()
    {
        var world = Arena();
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        var events = TestWorlds.Run(world, 40, new AttackCommand(zombie.Id));
        var died = events.OfType<ActorDied>().Single();
        Assert.Equal(zombie.Id, died.ActorId);
        Assert.Equal(Actor.PlayerId, died.KillerId);
        Assert.True(zombie.IsDead);
        Assert.True(world.Level.IsFree(new Cell(2, 1)));
        Assert.Equal(50, events.OfType<ExperienceGained>().Single().Amount);
        Assert.Equal(IntentKind.None, world.Player.Intent);
    }

    [Fact]
    public void ZeroHitChance_AlwaysMisses()
    {
        var world = Arena(TestGameContent.Rules(minHit: 0, maxHit: 0));
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        var events = TestWorlds.Run(world, 4, new AttackCommand(zombie.Id));
        var miss = events.OfType<AttackResolved>().Single(e => e.AttackerId == Actor.PlayerId);
        Assert.Equal(AttackOutcome.Miss, miss.Outcome);
        Assert.Equal(0, miss.Damage);
        Assert.Equal(10, zombie.Life);
    }

    [Fact]
    public void StrongHit_StaggersTheTarget()
    {
        var world = Arena();
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        var events = TestWorlds.Run(world, 4, new AttackCommand(zombie.Id));
        var staggered = events.OfType<ActorStaggered>().Single();
        Assert.Equal(zombie.Id, staggered.ActorId);
        Assert.Equal(3, staggered.DurationTicks);
        Assert.Equal(ActionKind.Recovering, zombie.Action);
    }

    [Fact]
    public void StaggeredAttacker_DoesNotLandItsSwing()
    {
        var world = Arena();
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        zombie.SetAttackIntent(Actor.PlayerId);
        var events = TestWorlds.Run(world, 4, new AttackCommand(zombie.Id));
        Assert.Equal(2, events.OfType<AttackStarted>().Count());
        var resolved = Assert.Single(events.OfType<AttackResolved>());
        Assert.Equal(Actor.PlayerId, resolved.AttackerId);
        Assert.Equal(world.Player.Profile.MaxLife, world.Player.Life);
    }

    [Fact]
    public void AttackingAMissingOrDeadTarget_IsRejected()
    {
        var world = Arena();
        var missing = world.Tick(new Command[] { new AttackCommand(99) }).OfType<CommandRejected>().Single();
        Assert.Equal("no such monster on this level", missing.Reason);
        var self = world.Tick(new Command[] { new AttackCommand(Actor.PlayerId) }).OfType<CommandRejected>().Single();
        Assert.Equal("no such monster on this level", self.Reason);
        var zombie = TestWorlds.Spawn(world, "zombie", 5, 3);
        zombie.Life = 0;
        world.Tick(Array.Empty<Command>());
        var dead = world.Tick(new Command[] { new AttackCommand(zombie.Id) }).OfType<CommandRejected>().Single();
        Assert.Equal("target is already dead", dead.Reason);
    }

    [Fact]
    public void DeadPlayer_CannotAct()
    {
        var world = Arena();
        world.Player.Life = 0;
        var died = world.Tick(Array.Empty<Command>()).OfType<ActorDied>().Single();
        Assert.Equal(Actor.PlayerId, died.ActorId);
        Assert.True(world.IsGameOver);
        var rejected = world.Tick(new Command[] { new MoveCommand(new Cell(3, 3)) }).OfType<CommandRejected>().Single();
        Assert.Equal("the player is dead", rejected.Reason);
    }

    [Fact]
    public void DistantTarget_IsApproachedThenAttacked()
    {
        var world = Arena();
        var zombie = TestWorlds.Spawn(world, "zombie", 6, 1);
        var events = TestWorlds.Run(world, 30, new AttackCommand(zombie.Id));
        Assert.Contains(events, e => e is ActorMoved moved && moved.ActorId == Actor.PlayerId);
        var swing = events.OfType<AttackStarted>().First(e => e.AttackerId == Actor.PlayerId);
        Assert.Equal(zombie.Id, swing.TargetId);
    }

    [Theory]
    [InlineData(70, 4, 1, 1, 66)]
    [InlineData(70, 4, 5, 1, 74)]
    [InlineData(10, 40, 1, 1, 5)]
    [InlineData(200, 0, 1, 1, 95)]
    public void HitChance_IsClampedToTheRuleLimits(int toHit, int armor, int attackerLevel, int defenderLevel, int expected)
    {
        var timings = TestGameContent.Timings();
        var attacker = new CombatProfile(attackerLevel, 10, 0, 0, toHit, new IntRange(1, 2), 0, timings);
        var defender = new CombatProfile(defenderLevel, 10, 0, armor, 0, new IntRange(1, 2), 0, timings);
        Assert.Equal(expected, HitChance.Compute(attacker, defender, TestGameContent.Rules()));
    }
}
```

`tests/Mortar.Tests/Gameplay/ProgressionTests.cs`:

```csharp
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class ProgressionTests
{
    [Fact]
    public void KillThatCrossesAThreshold_LevelsUpAndRestoresLife()
    {
        var rules = TestGameContent.Rules(minHit: 100, maxHit: 100, thresholds: new long[] { 40, 300, 600 });
        var world = TestWorlds.Create(TestGameContent.Valid(rules: rules));
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        world.Player.Life = 30;
        var events = TestWorlds.Run(world, 40, new AttackCommand(zombie.Id));
        Assert.Equal(2, events.OfType<LeveledUp>().Single().Level);
        Assert.Equal(5, world.Progress.UnspentPoints);
        Assert.Equal(52, world.Player.Profile.MaxLife);
        Assert.Equal(52, world.Player.Life);
    }

    [Fact]
    public void SpendStatPoint_WithoutPoints_IsRejected()
    {
        var world = TestWorlds.Create();
        var rejected = world.Tick(new Command[] { new SpendStatPointCommand(StatKind.Strength) }).OfType<CommandRejected>().Single();
        Assert.Equal("no unspent stat points", rejected.Reason);
    }

    [Fact]
    public void SpendStatPoint_RaisesVitalityAndLife()
    {
        var world = TestWorlds.Create();
        world.Progress.UnspentPoints = 1;
        var life = world.Player.Life;
        var maxLife = world.Player.Profile.MaxLife;
        var events = world.Tick(new Command[] { new SpendStatPointCommand(StatKind.Vitality) });
        var increased = events.OfType<StatIncreased>().Single();
        Assert.Equal(StatKind.Vitality, increased.Stat);
        Assert.Equal(21, increased.Value);
        Assert.Equal(maxLife + 2, world.Player.Profile.MaxLife);
        Assert.Equal(life + 2, world.Player.Life);
    }
}
```

HitChance rows use `Rules()` (base 5/95, level difference 2): `70 - 4 + 0 = 66`; `70 - 4 + 4 * 2 = 74`; `10 - 40 = -30 -> 5`; `200 -> 95`.

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay.CombatTests|FullyQualifiedName~Gameplay.ProgressionTests"`
Expected: build FAILS (`AttackCommand`, `AttackStarted`, `HitChance` do not exist).

- [ ] **Step 4: Implement commands and events**

`src/Mortar.Game/Commands/AttackCommand.cs`:

```csharp
namespace Mortar.Game.Commands;

public sealed class AttackCommand : Command
{
    public AttackCommand(int targetId)
    {
        TargetId = targetId;
    }

    public int TargetId { get; }

    public override string Describe() => $"attack #{TargetId}";
}
```

`src/Mortar.Game/Commands/SpendStatPointCommand.cs`:

```csharp
using Mortar.Game.Content;

namespace Mortar.Game.Commands;

public sealed class SpendStatPointCommand : Command
{
    public SpendStatPointCommand(StatKind stat)
    {
        Stat = stat;
    }

    public StatKind Stat { get; }

    public override string Describe() => $"spend a point on {Stat.ToString().ToLowerInvariant()}";
}
```

`src/Mortar.Game/Events/CombatEvents.cs`:

```csharp
namespace Mortar.Game.Events;

public enum AttackOutcome
{
    Miss,
    Blocked,
    Hit,
}

public sealed class AttackStarted : GameEvent
{
    public AttackStarted(long tick, int attackerId, int targetId, int durationTicks)
        : base(tick)
    {
        AttackerId = attackerId;
        TargetId = targetId;
        DurationTicks = durationTicks;
    }

    public int AttackerId { get; }

    public int TargetId { get; }

    public int DurationTicks { get; }

    protected override string Describe() => $"#{AttackerId} swings at #{TargetId} for {DurationTicks}";
}

public sealed class AttackResolved : GameEvent
{
    public AttackResolved(long tick, int attackerId, int targetId, AttackOutcome outcome, int damage)
        : base(tick)
    {
        AttackerId = attackerId;
        TargetId = targetId;
        Outcome = outcome;
        Damage = damage;
    }

    public int AttackerId { get; }

    public int TargetId { get; }

    public AttackOutcome Outcome { get; }

    public int Damage { get; }

    protected override string Describe() => Outcome switch
    {
        AttackOutcome.Hit => $"#{AttackerId} hit #{TargetId} for {Damage}",
        AttackOutcome.Blocked => $"#{TargetId} blocked #{AttackerId}",
        _ => $"#{AttackerId} missed #{TargetId}",
    };
}

public sealed class ActorStaggered : GameEvent
{
    public ActorStaggered(long tick, int actorId, int durationTicks)
        : base(tick)
    {
        ActorId = actorId;
        DurationTicks = durationTicks;
    }

    public int ActorId { get; }

    public int DurationTicks { get; }

    protected override string Describe() => $"#{ActorId} staggered for {DurationTicks}";
}

public sealed class ActorDied : GameEvent
{
    public ActorDied(long tick, int actorId, int killerId)
        : base(tick)
    {
        ActorId = actorId;
        KillerId = killerId;
    }

    public int ActorId { get; }

    /// <summary>The last actor to hit the one that died, or 0 if none.</summary>
    public int KillerId { get; }

    protected override string Describe() =>
        KillerId == 0 ? $"#{ActorId} died" : $"#{ActorId} died (killed by #{KillerId})";
}
```

`src/Mortar.Game/Events/ProgressEvents.cs`:

```csharp
using Mortar.Game.Content;

namespace Mortar.Game.Events;

public sealed class ExperienceGained : GameEvent
{
    public ExperienceGained(long tick, long amount, long total)
        : base(tick)
    {
        Amount = amount;
        Total = total;
    }

    public long Amount { get; }

    public long Total { get; }

    protected override string Describe() => $"gained {Amount} xp (total {Total})";
}

public sealed class LeveledUp : GameEvent
{
    public LeveledUp(long tick, int level)
        : base(tick)
    {
        Level = level;
    }

    public int Level { get; }

    protected override string Describe() => $"reached level {Level}";
}

public sealed class StatIncreased : GameEvent
{
    public StatIncreased(long tick, StatKind stat, int value)
        : base(tick)
    {
        Stat = stat;
        Value = value;
    }

    public StatKind Stat { get; }

    public int Value { get; }

    protected override string Describe() => $"{Stat.ToString().ToLowerInvariant()} is now {Value}";
}
```

- [ ] **Step 5: Implement combat, death, and growth**

`src/Mortar.Game/Simulation/HitChance.cs`:

```csharp
using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public static class HitChance
{
    public static int Compute(CombatProfile attacker, CombatProfile defender, GameRules rules)
    {
        var chance = attacker.ToHit - defender.ArmorClass + (attacker.Level - defender.Level) * rules.ToHitPerLevelDifference;
        return Math.Max(rules.MinHitChance, Math.Min(rules.MaxHitChance, chance));
    }
}
```

`src/Mortar.Game/Simulation/CombatSystem.cs`:

```csharp
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class CombatSystem
{
    public static void StartAttack(Actor attacker, Actor target, EventSink events)
    {
        var duration = attacker.Profile.Timings.AttackTicks;
        attacker.Facing = Directions.Toward(attacker.Cell, target.Cell);
        attacker.StartAction(ActionKind.Attacking, duration, target.Id);
        events.Add(new AttackStarted(events.Tick, attacker.Id, target.Id, duration));
    }

    public static void Resolve(World world, IReadOnlyList<Actor> due, EventSink events)
    {
        foreach (var attacker in due)
        {
            if (!attacker.IsAlive || attacker.Action != ActionKind.Attacking) continue;
            var target = world.Level.Find(attacker.ActionTargetId);
            if (target is null || !target.IsAlive) continue;
            if (!attacker.Cell.IsAdjacentTo(target.Cell))
            {
                events.Add(new AttackResolved(events.Tick, attacker.Id, target.Id, AttackOutcome.Miss, 0));
                continue;
            }
            Strike(world, attacker, target, events);
        }
    }

    private static void Strike(World world, Actor attacker, Actor target, EventSink events)
    {
        var outcome = Roll(world, attacker, target);
        var damageRange = attacker.Profile.Damage;
        var damage = outcome == AttackOutcome.Hit ? world.Rng.NextInt(damageRange.Min, damageRange.Max + 1) : 0;
        events.Add(new AttackResolved(events.Tick, attacker.Id, target.Id, outcome, damage));
        if (outcome != AttackOutcome.Hit) return;
        target.Life -= damage;
        target.LastAttackerId = attacker.Id;
        if (target.Life > 0 && damage * 100 >= target.Profile.MaxLife * world.Content.Rules.HitRecoveryPercent)
        {
            var recovery = target.Profile.Timings.RecoveryTicks;
            target.StartAction(ActionKind.Recovering, recovery);
            events.Add(new ActorStaggered(events.Tick, target.Id, recovery));
        }
    }

    private static AttackOutcome Roll(World world, Actor attacker, Actor target)
    {
        var chance = HitChance.Compute(attacker.Profile, target.Profile, world.Content.Rules);
        if (world.Rng.NextInt(100) >= chance) return AttackOutcome.Miss;
        var block = target.Profile.BlockChance;
        if (block > 0 && world.Rng.NextInt(100) < block) return AttackOutcome.Blocked;
        return AttackOutcome.Hit;
    }
}
```

`src/Mortar.Game/Simulation/DeathSystem.cs`:

```csharp
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class DeathSystem
{
    public static void Process(World world, EventSink events)
    {
        var level = world.Level;
        foreach (var actor in level.Actors)
        {
            if (actor.IsDead || actor.Life > 0) continue;
            actor.MarkDead();
            level.Vacate(actor);
            events.Add(new ActorDied(events.Tick, actor.Id, actor.LastAttackerId));
            if (actor.Kind == ActorKind.Monster && actor.LastAttackerId == Actor.PlayerId)
            {
                PlayerGrowth.RewardKill(world, actor, events);
            }
        }
    }
}
```

`src/Mortar.Game/Simulation/PlayerGrowth.cs`:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class PlayerGrowth
{
    public static void RewardKill(World world, Actor monster, EventSink events)
    {
        var definition = world.Content.Monster(monster.DefinitionId);
        var amount = Leveling.KillExperience(definition.Experience, definition.Level, world.Progress.Level);
        if (amount == 0) return;
        var gained = Leveling.AddExperience(world.Progress, amount, world.PlayerClass, world.Content.Rules);
        events.Add(new ExperienceGained(events.Tick, amount, world.Progress.Experience));
        if (gained == 0) return;
        Refresh(world);
        world.Player.RestoreVitals();
        events.Add(new LeveledUp(events.Tick, world.Progress.Level));
    }

    public static string? SpendPoint(World world, StatKind stat, EventSink events)
    {
        var reason = Leveling.SpendPoint(world.Progress, stat, world.PlayerClass);
        if (reason is not null) return reason;
        Refresh(world);
        events.Add(new StatIncreased(events.Tick, stat, world.Progress.BaseStats.Get(stat)));
        return null;
    }

    private static void Refresh(World world) =>
        world.Player.ApplyProfile(PlayerStats.Compute(world.PlayerClass, world.Progress, world.Content.Rules));
}
```

- [ ] **Step 6: Wire combat into the commands, actions, and tick**

In `src/Mortar.Game/Simulation/CommandSystem.cs`, replace `Execute` and add `Attack`:

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
            default:
                return "unsupported command";
        }
    }

    private static string? Attack(World world, AttackCommand attack)
    {
        var target = world.Level.Find(attack.TargetId);
        if (target is null || target.Kind != ActorKind.Monster)
        {
            return "no such monster on this level";
        }
        if (!target.IsAlive)
        {
            return "target is already dead";
        }
        world.Player.SetAttackIntent(target.Id);
        return null;
    }
```

In `src/Mortar.Game/Simulation/ActionSystem.cs`, add `using Mortar.Game.Pathing;` and replace `StartFromIntent` with:

```csharp
    private static void StartFromIntent(LevelState level, Actor actor, EventSink events)
    {
        switch (actor.Intent)
        {
            case IntentKind.MoveTo:
                Movement.ContinueMoveTo(level, actor, events);
                break;
            case IntentKind.Attack:
                ContinueAttack(level, actor, events);
                break;
        }
    }

    private static void ContinueAttack(LevelState level, Actor actor, EventSink events)
    {
        var target = level.Find(actor.IntentTargetId);
        if (target is null || !target.IsAlive)
        {
            actor.ClearIntent();
            return;
        }
        if (actor.Cell.IsAdjacentTo(target.Cell))
        {
            CombatSystem.StartAttack(actor, target, events);
            return;
        }
        var path = Pathfinder.FindPathNextTo(actor.Cell, target.Cell, level.IsWalkable, level.IsFree);
        if (path is { Count: > 0 }) Movement.Step(level, actor, path[0], events);
    }
```

When no path exists the attack intent stays, so a blocked attacker waits and retries next tick.

In `src/Mortar.Game/Simulation/TickRunner.cs`, replace the two system lines with:

```csharp
        CommandSystem.Apply(world, commands, events);
        var due = ActionSystem.Advance(world, events);
        CombatSystem.Resolve(world, due, events);
        DeathSystem.Process(world, events);
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay"`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add src/Mortar.Game tests/Mortar.Tests/Gameplay
git commit -m "add melee combat, death, and experience"
```

---

### Task 8: Monster spawning and AI

**Files:**
- Create: `src/Mortar.Game/Simulation/MonsterSpawner.cs`, `src/Mortar.Game/Simulation/AiSystem.cs`, `src/Mortar.Game/Events/MonsterAwoke.cs`
- Modify: `src/Mortar.Game/Simulation/LevelEntry.cs`, `src/Mortar.Game/Simulation/TickRunner.cs`
- Test: `tests/Mortar.Tests/Gameplay/SpawnerTests.cs`, `tests/Mortar.Tests/Gameplay/AiTests.cs`

**Interfaces:**
- Consumes: Tasks 1–7; `Mortar.Core.Model.MarkerKinds`.
- Produces: internal `MonsterSpawner.EntranceSafeRadius = 5`, `MonsterSpawner.PlacementRadius = 3`, `MonsterSpawner.Populate(World, LevelState, LevelDefinition)`; internal `AiSystem.Update(World, EventSink)`; event `MonsterAwoke(long tick, int actorId)`.

- [ ] **Step 1: Write the failing tests**

`tests/Mortar.Tests/Gameplay/SpawnerTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Content;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class SpawnerTests
{
    private static readonly string[] SpawnRoom =
    {
        "###########",
        "#E.S.....S#",
        "#.........#",
        "#........S#",
        "###########",
    };

    private static readonly string[] OneSpawn =
    {
        "###########",
        "#E.......S#",
        "#.........#",
        "###########",
    };

    private static readonly string[] BossRoom =
    {
        "###########",
        "#E........#",
        "#........B#",
        "###########",
    };

    [Fact]
    public void Populate_SpawnsAtMarkersAwayFromTheEntrance()
    {
        var world = TestWorlds.Create(levels: new[] { SpawnRoom, TestWorlds.Room });
        var monsters = world.Level.Actors.Where(a => a.Kind == ActorKind.Monster).ToList();
        Assert.Equal(new[] { new Cell(9, 1), new Cell(9, 3) }, monsters.Select(m => m.Cell));
        Assert.Equal(new[] { 2, 3 }, monsters.Select(m => m.Id));
    }

    [Fact]
    public void PackLeader_SpawnsWithMinions()
    {
        var monsters = new[]
        {
            TestGameContent.Monster("imp"),
            TestGameContent.Monster("chief", AiKinds.PackLeader, new MinionSpec("imp", new IntRange(2, 2))),
        };
        var levels = new[] { TestGameContent.Level(1, "chief"), TestGameContent.Level(2, "imp") };
        var world = TestWorlds.Create(TestGameContent.Valid(monsters, levels), levels: new[] { OneSpawn, TestWorlds.Room });
        var chief = world.Level.Actors.Single(a => a.DefinitionId == "chief");
        var imps = world.Level.Actors.Where(a => a.DefinitionId == "imp").ToList();
        Assert.Equal(2, imps.Count);
        Assert.All(imps, imp => Assert.Equal(chief.Id, imp.LeaderId));
        Assert.All(imps, imp => Assert.True(imp.Cell.DistanceTo(chief.Cell) <= MonsterSpawner.PlacementRadius));
    }

    [Fact]
    public void Boss_SpawnsAtTheBossMarker()
    {
        var monsters = new[] { TestGameContent.Monster("zombie"), TestGameContent.Monster("warden") };
        var levels = new[] { TestGameContent.Level(1, bossId: "warden"), TestGameContent.Level(2) };
        var world = TestWorlds.Create(TestGameContent.Valid(monsters, levels), levels: new[] { BossRoom, TestWorlds.Room });
        Assert.Equal(new Cell(9, 2), world.Level.Actors.Single(a => a.DefinitionId == "warden").Cell);
    }

    [Fact]
    public void SameSeed_SpawnsTheSameMonsters()
    {
        Assert.Equal(Describe(MixedWorld(5)), Describe(MixedWorld(5)));
    }

    private static World MixedWorld(uint seed)
    {
        var monsters = new[] { TestGameContent.Monster("zombie"), TestGameContent.Monster("imp", life: 4) };
        var mixed = new LevelDefinition(1, "test", new[] { new SpawnEntry("zombie", 1), new SpawnEntry("imp", 1) }, null);
        var content = TestGameContent.Valid(monsters, new[] { mixed, TestGameContent.Level(2) });
        return TestWorlds.Create(content, seed, new[] { SpawnRoom, TestWorlds.Room });
    }

    private static IEnumerable<string> Describe(World world) =>
        world.Level.Actors.Select(a => $"{a.Id} {a.DefinitionId} {a.Cell} {a.Life}");
}
```

`tests/Mortar.Tests/Gameplay/AiTests.cs`:

```csharp
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class AiTests
{
    private static readonly string[] Hall =
    {
        "####################",
        "#E.................#",
        "####################",
    };

    private static World HallWorld(params MonsterDefinition[] monsters)
    {
        var spawnable = monsters[0].Id;
        var levels = new[] { TestGameContent.Level(1, spawnable), TestGameContent.Level(2, spawnable) };
        return TestWorlds.Create(TestGameContent.Valid(monsters, levels), levels: new[] { Hall, Hall });
    }

    [Fact]
    public void MonsterOutOfSight_StaysAsleep()
    {
        var world = HallWorld(TestGameContent.Monster("zombie", sight: 3));
        var zombie = TestWorlds.Spawn(world, "zombie", 15, 1);
        var events = TestWorlds.Run(world, 20);
        Assert.False(zombie.IsAwake);
        Assert.DoesNotContain(events, e => e is ActorMoved moved && moved.ActorId == zombie.Id);
    }

    [Fact]
    public void MonsterInSight_WakesChasesAndAttacks()
    {
        var world = HallWorld(TestGameContent.Monster("zombie", sight: 10));
        var zombie = TestWorlds.Spawn(world, "zombie", 8, 1);
        var events = TestWorlds.Run(world, 60);
        var awoke = events.OfType<MonsterAwoke>().First();
        Assert.Equal(zombie.Id, awoke.ActorId);
        Assert.Equal(1, awoke.Tick);
        Assert.Contains(events, e => e is ActorMoved moved && moved.ActorId == zombie.Id);
        Assert.Contains(events, e => e is AttackStarted swing && swing.AttackerId == zombie.Id && swing.TargetId == Actor.PlayerId);
    }

    [Fact]
    public void BeingHit_WakesAMonster()
    {
        var world = HallWorld(TestGameContent.Monster("zombie", sight: 1));
        var zombie = TestWorlds.Spawn(world, "zombie", 15, 1);
        zombie.LastAttackerId = Actor.PlayerId;
        var awoke = Assert.Single(world.Tick(Array.Empty<Command>()).OfType<MonsterAwoke>());
        Assert.Equal(zombie.Id, awoke.ActorId);
        Assert.True(zombie.IsAwake);
    }

    [Fact]
    public void Pack_WakesTogether()
    {
        var world = HallWorld(
            TestGameContent.Monster("imp", sight: 1),
            TestGameContent.Monster("chief", AiKinds.PackLeader, new MinionSpec("imp", new IntRange(2, 2)), sight: 1));
        var chief = TestWorlds.Spawn(world, "chief", 15, 1);
        var first = TestWorlds.Spawn(world, "imp", 16, 1);
        var second = TestWorlds.Spawn(world, "imp", 17, 1);
        first.LeaderId = chief.Id;
        second.LeaderId = chief.Id;
        second.LastAttackerId = Actor.PlayerId;
        var awoke = world.Tick(Array.Empty<Command>()).OfType<MonsterAwoke>().Select(e => e.ActorId);
        Assert.Equal(new[] { chief.Id, first.Id, second.Id }, awoke.OrderBy(id => id));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay.SpawnerTests|FullyQualifiedName~Gameplay.AiTests"`
Expected: build FAILS (`MonsterSpawner` and `MonsterAwoke` do not exist).

- [ ] **Step 3: Implement**

`src/Mortar.Game/Events/MonsterAwoke.cs`:

```csharp
namespace Mortar.Game.Events;

public sealed class MonsterAwoke : GameEvent
{
    public MonsterAwoke(long tick, int actorId)
        : base(tick)
    {
        ActorId = actorId;
    }

    public int ActorId { get; }

    protected override string Describe() => $"#{ActorId} woke up";
}
```

`src/Mortar.Game/Simulation/MonsterSpawner.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

internal static class MonsterSpawner
{
    public const int EntranceSafeRadius = 5;
    public const int PlacementRadius = 3;

    public static void Populate(World world, LevelState level, LevelDefinition definition)
    {
        var weights = definition.Spawns.Select(spawn => spawn.Weight).ToList();
        var entrance = level.FindMarker(MarkerKinds.Entrance);
        foreach (var marker in level.Layout.Markers)
        {
            if (marker.Kind != MarkerKinds.Spawn) continue;
            var origin = new Cell(marker.X, marker.Y);
            if (entrance is not null && origin.DistanceTo(entrance.Value) <= EntranceSafeRadius) continue;
            var spawn = definition.Spawns[world.Rng.PickWeighted(weights)];
            SpawnGroup(world, level, world.Content.Monster(spawn.MonsterId), origin);
        }
        var boss = level.FindMarker(MarkerKinds.Boss);
        if (definition.BossId is not null && boss is not null)
        {
            SpawnGroup(world, level, world.Content.Monster(definition.BossId), boss.Value);
        }
    }

    private static void SpawnGroup(World world, LevelState level, MonsterDefinition monster, Cell origin)
    {
        var cell = level.NearestFree(origin, PlacementRadius);
        if (cell is null) return;
        var leader = world.SpawnMonster(level, monster, cell.Value);
        if (monster.Minions is null) return;
        var minion = world.Content.Monster(monster.Minions.MonsterId);
        var count = world.Rng.NextInt(monster.Minions.Count.Min, monster.Minions.Count.Max + 1);
        for (var i = 0; i < count; i++)
        {
            var spot = level.NearestFree(cell.Value, PlacementRadius);
            if (spot is null) return;
            world.SpawnMonster(level, minion, spot.Value).LeaderId = leader.Id;
        }
    }
}
```

`src/Mortar.Game/Simulation/AiSystem.cs`:

```csharp
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class AiSystem
{
    public static void Update(World world, EventSink events)
    {
        var level = world.Level;
        foreach (var monster in level.Actors)
        {
            if (IsActiveMonster(monster) && !monster.IsAwake && ShouldWake(world, monster))
            {
                WakeGroup(level, monster, events);
            }
        }
        foreach (var monster in level.Actors)
        {
            if (IsActiveMonster(monster) && monster.IsAwake && monster.Intent != IntentKind.Attack)
            {
                monster.SetAttackIntent(world.Player.Id);
            }
        }
    }

    private static bool IsActiveMonster(Actor actor) => actor.Kind == ActorKind.Monster && actor.IsAlive;

    private static bool ShouldWake(World world, Actor monster)
    {
        if (monster.LastAttackerId != 0) return true;
        var sight = world.Content.Monster(monster.DefinitionId).SightRadius;
        return monster.Cell.DistanceTo(world.Player.Cell) <= sight;
    }

    private static void WakeGroup(LevelState level, Actor monster, EventSink events)
    {
        var groupId = monster.LeaderId != 0 ? monster.LeaderId : monster.Id;
        foreach (var member in level.Actors)
        {
            if (!IsActiveMonster(member) || member.IsAwake) continue;
            if (member.Id == monster.Id || member.Id == groupId || member.LeaderId == groupId)
            {
                member.IsAwake = true;
                events.Add(new MonsterAwoke(events.Tick, member.Id));
            }
        }
    }
}
```

The tick stops running systems once the player is dead (`TickRunner`), so the AI never targets a dead player.

In `src/Mortar.Game/Simulation/LevelEntry.cs`, in `Build`, insert this line after `world.AddLevel(level);`:

```csharp
        MonsterSpawner.Populate(world, level, definition);
```

In `src/Mortar.Game/Simulation/TickRunner.cs`, insert after `CommandSystem.Apply(world, commands, events);`:

```csharp
        AiSystem.Update(world, events);
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay"`
Expected: PASS, including the Task 6 and Task 7 tests (their rooms have no spawn markers, and their assertions filter by event type).

- [ ] **Step 5: Commit**

```bash
git add src/Mortar.Game tests/Mortar.Tests/Gameplay
git commit -m "add monster spawning and ai"
```

---

### Task 9: Stairs between levels

**Files:**
- Create: `src/Mortar.Game/Commands/UseStairsCommand.cs`, `src/Mortar.Game/Events/LevelChanged.cs`, `src/Mortar.Game/Simulation/Stairs.cs`
- Modify: `src/Mortar.Game/Simulation/CommandSystem.cs`
- Test: `tests/Mortar.Tests/Gameplay/StairsTests.cs`

**Interfaces:**
- Consumes: Tasks 1–8; `LevelEntry.Enter`.
- Produces: `UseStairsCommand()`; event `LevelChanged(long tick, int fromDepth, int toDepth, Cell cell)`; internal `Stairs.Use(World, EventSink) -> string?`.

Standing on or next to the `exit` marker goes down and arrives at the next level's entrance; standing on or next to the `entrance` marker goes up and arrives at the previous level's exit. A visited level is reused exactly as the player left it.

- [ ] **Step 1: Write the failing tests**

`tests/Mortar.Tests/Gameplay/StairsTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class StairsTests
{
    private static readonly Command[] Stairs = { new UseStairsCommand() };

    [Fact]
    public void UsingTheExit_DescendsToTheNextLevelEntrance()
    {
        var world = TestWorlds.Create();
        var firstLevel = world.Level;
        firstLevel.Move(world.Player, new Cell(6, 4));
        var changed = world.Tick(Stairs).OfType<LevelChanged>().Single();
        Assert.Equal(1, changed.FromDepth);
        Assert.Equal(2, changed.ToDepth);
        Assert.Equal(new Cell(1, 1), changed.Cell);
        Assert.Equal(2, world.Depth);
        Assert.Equal(new Cell(1, 1), world.Player.Cell);
        Assert.Equal(new[] { 1, 2 }, world.VisitedDepths);
        Assert.Null(firstLevel.Find(Actor.PlayerId));
        Assert.True(firstLevel.IsFree(new Cell(6, 4)));
    }

    [Fact]
    public void UsingTheEntrance_ReturnsToTheSameLevelAtItsExit()
    {
        var world = TestWorlds.Create();
        var firstLevel = world.Level;
        firstLevel.Move(world.Player, new Cell(6, 4));
        world.Tick(Stairs);
        var changed = world.Tick(Stairs).OfType<LevelChanged>().Single();
        Assert.Equal(1, changed.ToDepth);
        Assert.Same(firstLevel, world.Level);
        Assert.Equal(new Cell(7, 4), world.Player.Cell);
    }

    [Fact]
    public void AwayFromStairs_IsRejected()
    {
        var world = TestWorlds.Create();
        world.Level.Move(world.Player, new Cell(4, 2));
        Assert.Equal("no stairs within reach", world.Tick(Stairs).OfType<CommandRejected>().Single().Reason);
    }

    [Fact]
    public void LevelOneEntrance_IsRejected()
    {
        var world = TestWorlds.Create();
        Assert.Equal("there is no level above", world.Tick(Stairs).OfType<CommandRejected>().Single().Reason);
    }

    [Fact]
    public void DeepestExit_IsRejected()
    {
        var world = TestWorlds.Create();
        world.Level.Move(world.Player, new Cell(6, 4));
        world.Tick(Stairs);
        world.Level.Move(world.Player, new Cell(6, 4));
        Assert.Equal("there is no deeper level", world.Tick(Stairs).OfType<CommandRejected>().Single().Reason);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay.StairsTests"`
Expected: build FAILS (`UseStairsCommand` does not exist).

- [ ] **Step 3: Implement**

`src/Mortar.Game/Commands/UseStairsCommand.cs`:

```csharp
namespace Mortar.Game.Commands;

public sealed class UseStairsCommand : Command
{
    public override string Describe() => "use stairs";
}
```

`src/Mortar.Game/Events/LevelChanged.cs`:

```csharp
namespace Mortar.Game.Events;

public sealed class LevelChanged : GameEvent
{
    public LevelChanged(long tick, int fromDepth, int toDepth, Cell cell)
        : base(tick)
    {
        FromDepth = fromDepth;
        ToDepth = toDepth;
        Cell = cell;
    }

    public int FromDepth { get; }

    public int ToDepth { get; }

    public Cell Cell { get; }

    protected override string Describe() => $"level {FromDepth} -> {ToDepth} at {Cell}";
}
```

`src/Mortar.Game/Simulation/Stairs.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class Stairs
{
    public static string? Use(World world, EventSink events)
    {
        var level = world.Level;
        var cell = world.Player.Cell;
        if (IsNear(level, cell, MarkerKinds.Exit))
        {
            return Travel(world, world.Depth + 1, MarkerKinds.Entrance, events);
        }
        if (IsNear(level, cell, MarkerKinds.Entrance))
        {
            return Travel(world, world.Depth - 1, MarkerKinds.Exit, events);
        }
        return "no stairs within reach";
    }

    private static bool IsNear(LevelState level, Cell cell, string kind)
    {
        foreach (var marker in level.Layout.Markers)
        {
            if (marker.Kind == kind && cell.DistanceTo(new Cell(marker.X, marker.Y)) <= 1) return true;
        }
        return false;
    }

    private static string? Travel(World world, int depth, string arrivalMarker, EventSink events)
    {
        if (depth < 1)
        {
            return "there is no level above";
        }
        if (depth > world.Content.DeepestLevel)
        {
            return "there is no deeper level";
        }
        var from = world.Depth;
        var error = LevelEntry.Enter(world, depth, arrivalMarker);
        if (error is not null)
        {
            return error;
        }
        events.Add(new LevelChanged(events.Tick, from, depth, world.Player.Cell));
        return null;
    }
}
```

In `src/Mortar.Game/Simulation/CommandSystem.cs`, add this case to the `switch` in `Execute`, before `default`:

```csharp
            case UseStairsCommand _:
                return Stairs.Use(world, events);
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Mortar.Game tests/Mortar.Tests/Gameplay
git commit -m "add stairs between levels"
```

---

### Task 10: Content files and loader

**Files:**
- Modify: `src/Mortar.Json/Mortar.Json.csproj`
- Create: `src/Mortar.Json/Content/ContentDtos.cs`, `Fields.cs`, `ContentMapper.cs`, `GameContent.cs`, `GameContentLoader.cs`, `LevelFileLoader.cs`
- Create: `src/Mortar.Game/Simulation/AreaLevelGenerator.cs`
- Create: `content/game.json`, `content/rules.json`, `content/levels.json`, `content/classes/warrior.json`, `content/monsters/zombie.json`, `skeleton.json`, `carrion-bat.json`, `imp.json`, `imp-chieftain.json`, `gravewarden.json`
- Create: `docs/content.md`
- Test: `tests/Mortar.Tests/Support/ContentFixture.cs`, `tests/Mortar.Tests/Json/GameContentLoaderTests.cs`

**Interfaces:**
- Consumes: `MortarLoader.LoadArea`, internal `JsonFile.Read<T>`, `SafePath.Resolve` (Mortar.Json); `Generator.Generate` (Mortar.Core); Task 2 definitions; `ILevelGenerator`, `World` (Task 5).
- Produces:
  - `Mortar.Game.Simulation.AreaLevelGenerator(IReadOnlyDictionary<string, CompiledArea> areas) : ILevelGenerator`.
  - `Mortar.Json.Content.GameContent(ContentSet content, IReadOnlyDictionary<string, CompiledArea> areas)` with `Content`, `Areas`, `CreateLevelGenerator()`.
  - `Mortar.Json.Content.GameContentLoader.Load(string manifestPath) -> AuthoringResult<GameContent>`.

- [ ] **Step 1: Write the content files**

`content/game.json`:

```json
{
  "rules": "rules.json",
  "classes": "classes",
  "monsters": "monsters",
  "levels": "levels.json"
}
```

`content/rules.json`:

```json
{
  "minHitChance": 5,
  "maxHitChance": 95,
  "playerBaseToHit": 45,
  "toHitPerDexterity": 1,
  "toHitPerLevel": 1,
  "toHitPerLevelDifference": 2,
  "dexterityPerArmor": 5,
  "strengthPerDamage": 10,
  "hitRecoveryPercent": 15,
  "experienceThresholds": [200, 600, 1300, 2400, 4000, 6200, 9100, 12800, 17500]
}
```

`content/classes/warrior.json`:

```json
{
  "id": "warrior",
  "name": "Warrior",
  "spriteSet": "warrior",
  "baseStats": { "strength": 28, "dexterity": 18, "vitality": 26, "magic": 12 },
  "statCaps": { "strength": 240, "dexterity": 70, "vitality": 110, "magic": 60 },
  "growth": {
    "lifeBase": 18,
    "lifePerVitality": 2,
    "lifePerLevel": 2,
    "manaBase": 0,
    "manaPerMagic": 1,
    "manaPerLevel": 1,
    "statPointsPerLevel": 5
  },
  "unarmedDamage": { "min": 1, "max": 3 },
  "timings": { "moveTicks": 7, "attackTicks": 14, "hitFrame": 9, "recoveryTicks": 6 }
}
```

`content/monsters/zombie.json`:

```json
{
  "id": "zombie",
  "name": "Rotting Corpse",
  "spriteSet": "zombie",
  "level": 1,
  "life": { "min": 8, "max": 14 },
  "armorClass": 5,
  "toHit": 25,
  "damage": { "min": 2, "max": 5 },
  "timings": { "moveTicks": 14, "attackTicks": 16, "hitFrame": 10, "recoveryTicks": 8 },
  "sightRadius": 5,
  "experience": 48,
  "ai": "melee-chase"
}
```

`content/monsters/skeleton.json`:

```json
{
  "id": "skeleton",
  "name": "Restless Bones",
  "spriteSet": "skeleton",
  "level": 2,
  "life": { "min": 6, "max": 12 },
  "armorClass": 10,
  "toHit": 30,
  "damage": { "min": 1, "max": 4 },
  "timings": { "moveTicks": 9, "attackTicks": 12, "hitFrame": 7, "recoveryTicks": 6 },
  "sightRadius": 7,
  "experience": 64,
  "ai": "melee-chase"
}
```

`content/monsters/carrion-bat.json`:

```json
{
  "id": "carrion-bat",
  "name": "Carrion Bat",
  "spriteSet": "carrion-bat",
  "level": 2,
  "life": { "min": 4, "max": 8 },
  "armorClass": 15,
  "toHit": 35,
  "damage": { "min": 1, "max": 3 },
  "timings": { "moveTicks": 5, "attackTicks": 10, "hitFrame": 5, "recoveryTicks": 5 },
  "sightRadius": 9,
  "experience": 70,
  "ai": "melee-chase"
}
```

`content/monsters/imp.json`:

```json
{
  "id": "imp",
  "name": "Cave Imp",
  "spriteSet": "imp",
  "level": 1,
  "life": { "min": 2, "max": 5 },
  "armorClass": 8,
  "toHit": 25,
  "damage": { "min": 1, "max": 3 },
  "timings": { "moveTicks": 8, "attackTicks": 12, "hitFrame": 7, "recoveryTicks": 6 },
  "sightRadius": 6,
  "experience": 36,
  "ai": "melee-chase"
}
```

`content/monsters/imp-chieftain.json`:

```json
{
  "id": "imp-chieftain",
  "name": "Imp Chieftain",
  "spriteSet": "imp-chieftain",
  "level": 3,
  "life": { "min": 14, "max": 20 },
  "armorClass": 15,
  "toHit": 40,
  "damage": { "min": 3, "max": 6 },
  "timings": { "moveTicks": 8, "attackTicks": 12, "hitFrame": 7, "recoveryTicks": 6 },
  "sightRadius": 7,
  "experience": 140,
  "ai": "pack-leader",
  "minions": { "monster": "imp", "count": { "min": 2, "max": 4 } }
}
```

`content/monsters/gravewarden.json`:

```json
{
  "id": "gravewarden",
  "name": "The Gravewarden",
  "spriteSet": "gravewarden",
  "level": 6,
  "life": { "min": 120, "max": 120 },
  "armorClass": 30,
  "toHit": 60,
  "damage": { "min": 6, "max": 14 },
  "blockChance": 20,
  "timings": { "moveTicks": 10, "attackTicks": 14, "hitFrame": 9, "recoveryTicks": 10 },
  "sightRadius": 10,
  "experience": 900,
  "ai": "melee-chase"
}
```

`content/levels.json`:

```json
{
  "levels": [
    {
      "depth": 1,
      "area": "../samples/areas/crypt.json",
      "spawns": [
        { "monster": "zombie", "weight": 3 },
        { "monster": "imp-chieftain", "weight": 1 }
      ]
    },
    {
      "depth": 2,
      "area": "../samples/areas/crypt.json",
      "spawns": [
        { "monster": "zombie", "weight": 2 },
        { "monster": "skeleton", "weight": 3 },
        { "monster": "imp-chieftain", "weight": 1 }
      ]
    },
    {
      "depth": 3,
      "area": "../samples/areas/crypt.json",
      "spawns": [
        { "monster": "skeleton", "weight": 3 },
        { "monster": "carrion-bat", "weight": 2 },
        { "monster": "imp-chieftain", "weight": 1 }
      ]
    },
    {
      "depth": 4,
      "area": "../samples/areas/crypt.json",
      "spawns": [
        { "monster": "skeleton", "weight": 2 },
        { "monster": "carrion-bat", "weight": 2 },
        { "monster": "zombie", "weight": 1 }
      ],
      "boss": "gravewarden"
    }
  ]
}
```

- [ ] **Step 2: Write the test fixture and failing tests**

`tests/Mortar.Tests/Support/ContentFixture.cs`:

```csharp
namespace Mortar.Tests.Support;

internal static class ContentFixture
{
    public static string Manifest => Path.Combine(TestPaths.Root, "content", "game.json");

    /// <summary>Copies the repo's content folder into <paramref name="temp"/> and returns the copied manifest path.</summary>
    public static string Copy(TempDir temp)
    {
        var source = Path.Combine(TestPaths.Root, "content");
        foreach (var file in Directory.GetFiles(source, "*.json", SearchOption.AllDirectories))
        {
            temp.Write(Path.GetRelativePath(source, file), File.ReadAllText(file));
        }
        var levels = Path.Combine(temp.Root, "levels.json");
        var crypt = TestPaths.CryptArea.Replace('\\', '/');
        File.WriteAllText(levels, File.ReadAllText(levels).Replace("../samples/areas/crypt.json", crypt));
        return Path.Combine(temp.Root, "game.json");
    }
}
```

`tests/Mortar.Tests/Json/GameContentLoaderTests.cs`:

```csharp
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using Mortar.Tests.Support;

namespace Mortar.Tests.Json;

public class GameContentLoaderTests
{
    [Fact]
    public void RepoContent_Loads()
    {
        var result = GameContentLoader.Load(ContentFixture.Manifest);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        var content = result.Value!.Content;
        Assert.True(content.TryGetClass("warrior", out _));
        Assert.Equal(6, content.Monsters.Count);
        Assert.Equal(4, content.DeepestLevel);
        Assert.Equal("gravewarden", content.Level(4).BossId);
        Assert.True(result.Value.Areas.ContainsKey("crypt"));
    }

    [Fact]
    public void RepoContent_CreatesAPlayableWorld()
    {
        var content = GameContentLoader.Load(ContentFixture.Manifest).Value!;
        var world = World.Create(content.Content, content.CreateLevelGenerator(), "warrior", 1);
        Assert.True(world.Succeeded, world.Error);
        Assert.Contains(world.World!.Level.Actors, actor => actor.Kind == ActorKind.Monster);
    }

    [Fact]
    public void MissingField_NamesTheFile()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var zombie = Path.Combine(temp.Root, "monsters", "zombie.json");
        File.WriteAllText(zombie, File.ReadAllText(zombie).Replace("\"level\": 1,", ""));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == zombie && e.Message == "'level' is required");
    }

    [Fact]
    public void UnknownProperty_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var zombie = Path.Combine(temp.Root, "monsters", "zombie.json");
        File.WriteAllText(zombie, File.ReadAllText(zombie).Replace("\"level\": 1,", "\"level\": 1, \"speed\": 3,"));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == zombie && e.Message.StartsWith("invalid JSON") && e.Message.Contains("speed"));
    }

    [Fact]
    public void UnknownSpawnMonster_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var levels = Path.Combine(temp.Root, "levels.json");
        File.WriteAllText(levels, File.ReadAllText(levels).Replace("\"monster\": \"zombie\"", "\"monster\": \"ghoul\""));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == "level 1" && e.Message == "spawn references unknown monster 'ghoul'");
    }

    [Fact]
    public void MissingMonstersFolder_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        Directory.Delete(Path.Combine(temp.Root, "monsters"), recursive: true);
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == manifest && e.Message == "'monsters' must name an existing folder");
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~Json.GameContentLoaderTests"`
Expected: build FAILS (`Mortar.Json.Content` does not exist).

- [ ] **Step 4: Implement the level generator adapter**

`src/Mortar.Game/Simulation/AreaLevelGenerator.cs`:

```csharp
using Mortar.Core.Generation;
using Mortar.Core.Model;
using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public sealed class AreaLevelGenerator : ILevelGenerator
{
    private readonly IReadOnlyDictionary<string, CompiledArea> _areas;

    public AreaLevelGenerator(IReadOnlyDictionary<string, CompiledArea> areas)
    {
        _areas = areas;
    }

    public LevelBuildResult Generate(LevelDefinition level, uint seed)
    {
        if (!_areas.TryGetValue(level.AreaId, out var area))
        {
            return LevelBuildResult.Failure($"unknown area '{level.AreaId}'");
        }
        var result = Generator.Generate(area, seed);
        return result.Succeeded
            ? LevelBuildResult.Success(result.Layout!)
            : LevelBuildResult.Failure(result.FailureSummary);
    }
}
```

- [ ] **Step 5: Implement the loader**

In `src/Mortar.Json/Mortar.Json.csproj`, add to the `ProjectReference` item group:

```xml
    <ProjectReference Include="..\Mortar.Game\Mortar.Game.csproj" />
```

`src/Mortar.Json/Content/ContentDtos.cs`:

```csharp
namespace Mortar.Json.Content;

internal sealed class ManifestDto
{
    public string? Rules { get; set; }

    public string? Classes { get; set; }

    public string? Monsters { get; set; }

    public string? Levels { get; set; }
}

internal sealed class RulesDto
{
    public int? MinHitChance { get; set; }

    public int? MaxHitChance { get; set; }

    public int? PlayerBaseToHit { get; set; }

    public int? ToHitPerDexterity { get; set; }

    public int? ToHitPerLevel { get; set; }

    public int? ToHitPerLevelDifference { get; set; }

    public int? DexterityPerArmor { get; set; }

    public int? StrengthPerDamage { get; set; }

    public int? HitRecoveryPercent { get; set; }

    public List<long>? ExperienceThresholds { get; set; }
}

internal sealed class StatsDto
{
    public int? Strength { get; set; }

    public int? Dexterity { get; set; }

    public int? Vitality { get; set; }

    public int? Magic { get; set; }
}

internal sealed class RangeDto
{
    public int? Min { get; set; }

    public int? Max { get; set; }
}

internal sealed class TimingsDto
{
    public int? MoveTicks { get; set; }

    public int? AttackTicks { get; set; }

    public int? HitFrame { get; set; }

    public int? RecoveryTicks { get; set; }
}

internal sealed class GrowthDto
{
    public int? LifeBase { get; set; }

    public int? LifePerVitality { get; set; }

    public int? LifePerLevel { get; set; }

    public int? ManaBase { get; set; }

    public int? ManaPerMagic { get; set; }

    public int? ManaPerLevel { get; set; }

    public int? StatPointsPerLevel { get; set; }
}

internal sealed class ClassDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? SpriteSet { get; set; }

    public StatsDto? BaseStats { get; set; }

    public StatsDto? StatCaps { get; set; }

    public GrowthDto? Growth { get; set; }

    public RangeDto? UnarmedDamage { get; set; }

    public TimingsDto? Timings { get; set; }
}

internal sealed class MinionsDto
{
    public string? Monster { get; set; }

    public RangeDto? Count { get; set; }
}

internal sealed class MonsterDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? SpriteSet { get; set; }

    public int? Level { get; set; }

    public RangeDto? Life { get; set; }

    public int? ArmorClass { get; set; }

    public int? ToHit { get; set; }

    public RangeDto? Damage { get; set; }

    public int? BlockChance { get; set; }

    public TimingsDto? Timings { get; set; }

    public int? SightRadius { get; set; }

    public int? Experience { get; set; }

    public string? Ai { get; set; }

    public MinionsDto? Minions { get; set; }
}

internal sealed class LevelsDto
{
    public List<LevelDto?>? Levels { get; set; }
}

internal sealed class LevelDto
{
    public int? Depth { get; set; }

    public string? Area { get; set; }

    public List<SpawnDto?>? Spawns { get; set; }

    public string? Boss { get; set; }
}

internal sealed class SpawnDto
{
    public string? Monster { get; set; }

    public int? Weight { get; set; }
}
```

`src/Mortar.Json/Content/Fields.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Game.Content;

namespace Mortar.Json.Content;

internal sealed class Fields
{
    private readonly string _source;
    private readonly string _prefix;
    private readonly List<AuthoringError> _errors;

    public Fields(string source, List<AuthoringError> errors, string context = "")
    {
        _source = source;
        _errors = errors;
        _prefix = context.Length == 0 ? "" : context + " ";
    }

    public bool Present(object? value, string name)
    {
        if (value is null) Missing(name);
        return value is not null;
    }

    public int Int(int? value, string name)
    {
        Present(value, name);
        return value ?? 0;
    }

    public string Text(string? value, string name)
    {
        if (!string.IsNullOrWhiteSpace(value)) return value!;
        Missing(name);
        return "";
    }

    public IntRange ReadRange(RangeDto? dto, string name) =>
        Present(dto, name) ? new IntRange(Int(dto!.Min, name + ".min"), Int(dto.Max, name + ".max")) : default;

    public Stats ReadStats(StatsDto? dto, string name) =>
        Present(dto, name)
            ? new Stats(
                Int(dto!.Strength, name + ".strength"),
                Int(dto.Dexterity, name + ".dexterity"),
                Int(dto.Vitality, name + ".vitality"),
                Int(dto.Magic, name + ".magic"))
            : default;

    public ActionTimings ReadTimings(TimingsDto? dto) =>
        Present(dto, "timings")
            ? new ActionTimings(
                Int(dto!.MoveTicks, "timings.moveTicks"),
                Int(dto.AttackTicks, "timings.attackTicks"),
                Int(dto.HitFrame, "timings.hitFrame"),
                Int(dto.RecoveryTicks, "timings.recoveryTicks"))
            : new ActionTimings(0, 0, 0, 0);

    private void Missing(string name) => _errors.Add(new AuthoringError(_source, $"{_prefix}'{name}' is required"));
}
```

`src/Mortar.Json/Content/ContentMapper.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Game.Content;

namespace Mortar.Json.Content;

internal static class ContentMapper
{
    public static GameRules ToRules(string source, RulesDto dto, List<AuthoringError> errors)
    {
        var fields = new Fields(source, errors);
        fields.Present(dto.ExperienceThresholds, "experienceThresholds");
        return new GameRules(
            fields.Int(dto.MinHitChance, "minHitChance"),
            fields.Int(dto.MaxHitChance, "maxHitChance"),
            fields.Int(dto.PlayerBaseToHit, "playerBaseToHit"),
            fields.Int(dto.ToHitPerDexterity, "toHitPerDexterity"),
            fields.Int(dto.ToHitPerLevel, "toHitPerLevel"),
            fields.Int(dto.ToHitPerLevelDifference, "toHitPerLevelDifference"),
            fields.Int(dto.DexterityPerArmor, "dexterityPerArmor"),
            fields.Int(dto.StrengthPerDamage, "strengthPerDamage"),
            fields.Int(dto.HitRecoveryPercent, "hitRecoveryPercent"),
            dto.ExperienceThresholds ?? new List<long>());
    }

    public static ClassDefinition ToClass(string source, ClassDto dto, List<AuthoringError> errors)
    {
        var fields = new Fields(source, errors);
        return new ClassDefinition(
            fields.Text(dto.Id, "id"),
            fields.Text(dto.Name, "name"),
            fields.Text(dto.SpriteSet, "spriteSet"),
            fields.ReadStats(dto.BaseStats, "baseStats"),
            fields.ReadStats(dto.StatCaps, "statCaps"),
            ToGrowth(fields, dto.Growth),
            fields.ReadRange(dto.UnarmedDamage, "unarmedDamage"),
            fields.ReadTimings(dto.Timings));
    }

    public static MonsterDefinition ToMonster(string source, MonsterDto dto, List<AuthoringError> errors)
    {
        var fields = new Fields(source, errors);
        return new MonsterDefinition(
            fields.Text(dto.Id, "id"),
            fields.Text(dto.Name, "name"),
            fields.Text(dto.SpriteSet, "spriteSet"),
            fields.Int(dto.Level, "level"),
            fields.ReadRange(dto.Life, "life"),
            fields.Int(dto.ArmorClass, "armorClass"),
            fields.Int(dto.ToHit, "toHit"),
            fields.ReadRange(dto.Damage, "damage"),
            dto.BlockChance ?? 0,
            fields.ReadTimings(dto.Timings),
            fields.Int(dto.SightRadius, "sightRadius"),
            fields.Int(dto.Experience, "experience"),
            fields.Text(dto.Ai, "ai"),
            dto.Minions is null ? null : new MinionSpec(
                fields.Text(dto.Minions.Monster, "minions.monster"),
                fields.ReadRange(dto.Minions.Count, "minions.count")));
    }

    public static LevelDefinition ToLevel(string source, int index, LevelDto dto, string areaId, List<AuthoringError> errors)
    {
        var fields = new Fields(source, errors, $"levels entry {index}");
        var spawns = new List<SpawnEntry>();
        if (fields.Present(dto.Spawns, "spawns"))
        {
            for (var i = 0; i < dto.Spawns!.Count; i++)
            {
                var spawn = dto.Spawns[i];
                if (!fields.Present(spawn, $"spawns[{i}]")) continue;
                spawns.Add(new SpawnEntry(
                    fields.Text(spawn!.Monster, $"spawns[{i}].monster"),
                    fields.Int(spawn.Weight, $"spawns[{i}].weight")));
            }
        }
        var boss = string.IsNullOrWhiteSpace(dto.Boss) ? null : dto.Boss;
        return new LevelDefinition(fields.Int(dto.Depth, "depth"), areaId, spawns, boss);
    }

    private static ClassGrowth ToGrowth(Fields fields, GrowthDto? dto)
    {
        if (!fields.Present(dto, "growth")) return new ClassGrowth(0, 0, 0, 0, 0, 0, 0);
        return new ClassGrowth(
            fields.Int(dto!.LifeBase, "growth.lifeBase"),
            fields.Int(dto.LifePerVitality, "growth.lifePerVitality"),
            fields.Int(dto.LifePerLevel, "growth.lifePerLevel"),
            fields.Int(dto.ManaBase, "growth.manaBase"),
            fields.Int(dto.ManaPerMagic, "growth.manaPerMagic"),
            fields.Int(dto.ManaPerLevel, "growth.manaPerLevel"),
            fields.Int(dto.StatPointsPerLevel, "growth.statPointsPerLevel"));
    }
}
```

`src/Mortar.Json/Content/GameContent.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Game.Content;
using Mortar.Game.Simulation;

namespace Mortar.Json.Content;

public sealed class GameContent
{
    public GameContent(ContentSet content, IReadOnlyDictionary<string, CompiledArea> areas)
    {
        Content = content;
        Areas = areas;
    }

    public ContentSet Content { get; }

    public IReadOnlyDictionary<string, CompiledArea> Areas { get; }

    public ILevelGenerator CreateLevelGenerator() => new AreaLevelGenerator(Areas);
}
```

`src/Mortar.Json/Content/GameContentLoader.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Core.Model;
using Mortar.Game.Content;

namespace Mortar.Json.Content;

public static class GameContentLoader
{
    public static AuthoringResult<GameContent> Load(string manifestPath)
    {
        var source = SafePath.Resolve(null, manifestPath);
        if (source is null)
        {
            return AuthoringResult<GameContent>.Failure(manifestPath, "invalid path");
        }
        var manifest = JsonFile.Read<ManifestDto>(source, out var readError);
        if (manifest is null)
        {
            return AuthoringResult<GameContent>.Failure(new[] { readError! });
        }
        var directory = Path.GetDirectoryName(source)!;
        var errors = new List<AuthoringError>();
        var rules = LoadRules(source, directory, manifest.Rules, errors);
        var classes = LoadFolder<ClassDto, ClassDefinition>(source, directory, manifest.Classes, "classes", ContentMapper.ToClass, errors);
        var monsters = LoadFolder<MonsterDto, MonsterDefinition>(source, directory, manifest.Monsters, "monsters", ContentMapper.ToMonster, errors);
        var areas = new Dictionary<string, CompiledArea>(StringComparer.Ordinal);
        var levels = LevelFileLoader.Load(source, directory, manifest.Levels, areas, errors);
        if (errors.Count > 0 || rules is null)
        {
            return AuthoringResult<GameContent>.Failure(errors);
        }
        var content = ContentSet.Create(rules, classes, monsters, levels);
        return content.Succeeded
            ? AuthoringResult<GameContent>.Success(new GameContent(content.Value!, areas))
            : AuthoringResult<GameContent>.Failure(content.Errors);
    }

    private static GameRules? LoadRules(string manifest, string directory, string? entry, List<AuthoringError> errors)
    {
        var path = SafePath.Resolve(directory, entry);
        if (path is null)
        {
            errors.Add(new AuthoringError(manifest, "'rules' is empty or an invalid path"));
            return null;
        }
        var dto = JsonFile.Read<RulesDto>(path, out var error);
        if (dto is null)
        {
            errors.Add(error!);
            return null;
        }
        return ContentMapper.ToRules(path, dto, errors);
    }

    private static List<TDefinition> LoadFolder<TDto, TDefinition>(
        string manifest,
        string directory,
        string? entry,
        string field,
        Func<string, TDto, List<AuthoringError>, TDefinition> map,
        List<AuthoringError> errors)
        where TDto : class
    {
        var definitions = new List<TDefinition>();
        var path = SafePath.Resolve(directory, entry);
        if (path is null || !Directory.Exists(path))
        {
            errors.Add(new AuthoringError(manifest, $"'{field}' must name an existing folder"));
            return definitions;
        }
        var files = Directory.GetFiles(path)
            .Where(file => string.Equals(Path.GetExtension(file), ".json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => Path.GetFileName(file), StringComparer.Ordinal);
        foreach (var file in files)
        {
            var dto = JsonFile.Read<TDto>(file, out var error);
            if (dto is null) errors.Add(error!);
            else definitions.Add(map(file, dto, errors));
        }
        return definitions;
    }
}
```

`src/Mortar.Json/Content/LevelFileLoader.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Core.Model;
using Mortar.Game.Content;

namespace Mortar.Json.Content;

internal static class LevelFileLoader
{
    public static List<LevelDefinition> Load(
        string manifest,
        string directory,
        string? entry,
        Dictionary<string, CompiledArea> areas,
        List<AuthoringError> errors)
    {
        var levels = new List<LevelDefinition>();
        var path = SafePath.Resolve(directory, entry);
        if (path is null)
        {
            errors.Add(new AuthoringError(manifest, "'levels' is empty or an invalid path"));
            return levels;
        }
        var dto = JsonFile.Read<LevelsDto>(path, out var error);
        if (dto is null)
        {
            errors.Add(error!);
            return levels;
        }
        if (dto.Levels is null)
        {
            errors.Add(new AuthoringError(path, "'levels' is required"));
            return levels;
        }
        var areaIdsByFile = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < dto.Levels.Count; i++)
        {
            var level = dto.Levels[i];
            if (level is null)
            {
                errors.Add(new AuthoringError(path, $"levels entry {i} is null"));
                continue;
            }
            var areaId = LoadArea(path, i, level.Area, areas, areaIdsByFile, errors);
            if (areaId is not null) levels.Add(ContentMapper.ToLevel(path, i, level, areaId, errors));
        }
        return levels;
    }

    private static string? LoadArea(
        string source,
        int index,
        string? entry,
        Dictionary<string, CompiledArea> areas,
        Dictionary<string, string> areaIdsByFile,
        List<AuthoringError> errors)
    {
        var file = SafePath.Resolve(Path.GetDirectoryName(source)!, entry);
        if (file is null)
        {
            errors.Add(new AuthoringError(source, $"levels entry {index} has an empty or invalid area path"));
            return null;
        }
        if (areaIdsByFile.TryGetValue(file, out var known)) return known;
        var result = MortarLoader.LoadArea(file);
        if (!result.Succeeded)
        {
            errors.AddRange(result.Errors);
            return null;
        }
        var id = result.Value!.Definition.Id;
        if (areas.ContainsKey(id))
        {
            errors.Add(new AuthoringError(file, $"area id '{id}' is already used by another area file"));
            return null;
        }
        areas.Add(id, result.Value);
        areaIdsByFile.Add(file, id);
        return id;
    }
}
```

- [ ] **Step 6: Document the content format**

`docs/content.md`:

````markdown
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
````

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~Json.GameContentLoaderTests"`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add src/Mortar.Json src/Mortar.Game/Simulation/AreaLevelGenerator.cs content docs/content.md tests/Mortar.Tests/Support/ContentFixture.cs tests/Mortar.Tests/Json/GameContentLoaderTests.cs
git commit -m "add game content files and loader"
```

---

### Task 11: Determinism, scripted run snapshot, and docs

**Files:**
- Create: `src/Mortar.Game/Simulation/StateHasher.cs`, `src/Mortar.Game/Simulation/WorldHash.cs`
- Create: `tests/Mortar.Tests/Gameplay/Support/ScriptedPlayer.cs`, `tests/Mortar.Tests/Gameplay/DeterminismTests.cs`
- Create (by the first test run): `tests/Mortar.Tests/Snapshots/game-seed-7.txt`
- Modify: `README.md`

**Interfaces:**
- Consumes: everything above; `Hashing.Fnv1a64`, `Hashing.FnvOffset`.
- Produces: `WorldHash.Compute(World) -> string` (16 hex digits covering seed, tick, depth, random generator state, player progress, and every actor on every visited level).

- [ ] **Step 1: Write the scripted player**

`tests/Mortar.Tests/Gameplay/Support/ScriptedPlayer.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Simulation;
using Mortar.Json.Content;

namespace Mortar.Tests.Gameplay.Support;

internal sealed class ScriptedRun
{
    public ScriptedRun(IReadOnlyList<string> log, string hash)
    {
        Log = log;
        Hash = hash;
    }

    public IReadOnlyList<string> Log { get; }

    public string Hash { get; }
}

/// <summary>A simple bot: fight awake monsters nearby, spend points on strength, otherwise head for the exit and descend.</summary>
internal static class ScriptedPlayer
{
    private const int EngageDistance = 6;

    public static ScriptedRun Run(GameContent content, uint seed, int ticks)
    {
        var created = World.Create(content.Content, content.CreateLevelGenerator(), "warrior", seed);
        var world = created.World ?? throw new InvalidOperationException(created.Error);
        var log = new List<string>();
        for (var i = 0; i < ticks && !world.IsGameOver; i++)
        {
            log.AddRange(world.Tick(Decide(world)).Select(e => e.ToString()));
        }
        return new ScriptedRun(log, WorldHash.Compute(world));
    }

    private static IReadOnlyList<Command> Decide(World world)
    {
        var player = world.Player;
        if (player.Intent == IntentKind.Attack) return Array.Empty<Command>();
        var target = NearestAwakeMonster(world);
        if (target is not null) return new Command[] { new AttackCommand(target.Id) };
        if (player.Intent != IntentKind.None) return Array.Empty<Command>();
        if (world.Progress.UnspentPoints > 0 && world.Progress.BaseStats.Strength < world.PlayerClass.StatCaps.Strength)
        {
            return new Command[] { new SpendStatPointCommand(StatKind.Strength) };
        }
        var exit = world.Level.FindMarker(MarkerKinds.Exit);
        if (exit is null) return Array.Empty<Command>();
        return player.Cell.DistanceTo(exit.Value) <= 1
            ? new Command[] { new UseStairsCommand() }
            : new Command[] { new MoveCommand(exit.Value) };
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

- [ ] **Step 2: Write the failing tests**

`tests/Mortar.Tests/Gameplay/DeterminismTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using Mortar.Tests.Gameplay.Support;
using Mortar.Tests.Support;

namespace Mortar.Tests.Gameplay;

public class DeterminismTests
{
    private const int RunTicks = 3000;

    private static GameContent Content()
    {
        var result = GameContentLoader.Load(ContentFixture.Manifest);
        return result.Value ?? throw new InvalidOperationException(string.Join("; ", result.Errors));
    }

    [Fact]
    public void SameSeedAndCommands_GiveTheSameLogAndHash()
    {
        var content = Content();
        var first = ScriptedPlayer.Run(content, 7, RunTicks);
        var second = ScriptedPlayer.Run(content, 7, RunTicks);
        Assert.Equal(first.Log, second.Log);
        Assert.Equal(first.Hash, second.Hash);
    }

    [Fact]
    public void DifferentSeeds_GiveDifferentRuns()
    {
        var content = Content();
        Assert.NotEqual(ScriptedPlayer.Run(content, 7, 600).Hash, ScriptedPlayer.Run(content, 8, 600).Hash);
    }

    [Fact]
    public void ScriptedRun_KillsMonsters()
    {
        var run = ScriptedPlayer.Run(Content(), 7, RunTicks);
        Assert.Contains(run.Log, line => line.Contains("(killed by #1)"));
    }

    [Fact]
    public void WorldHash_ChangesWhenStateChanges()
    {
        var world = TestWorlds.Create();
        var before = WorldHash.Compute(world);
        world.Tick(new Command[] { new MoveCommand(new Cell(3, 1)) });
        Assert.NotEqual(before, WorldHash.Compute(world));
    }

    [Fact]
    public void ScriptedRun_MatchesSnapshot()
    {
        var run = ScriptedPlayer.Run(Content(), 7, RunTicks);
        var actual = string.Join("\n", run.Log.Take(300)) + $"\nhash: {run.Hash}\n";
        var path = Path.Combine(TestPaths.Root, "tests", "Mortar.Tests", "Snapshots", "game-seed-7.txt");
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual);
            Assert.Fail($"Snapshot created at {path}; review it, then run the tests again.");
        }
        Assert.Equal(File.ReadAllText(path).Replace("\r\n", "\n"), actual);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay.DeterminismTests"`
Expected: build FAILS (`WorldHash` does not exist).

- [ ] **Step 4: Implement the state hash**

`src/Mortar.Game/Simulation/StateHasher.cs`:

```csharp
using System.Buffers.Binary;
using System.Text;
using Mortar.Core.Rng;

namespace Mortar.Game.Simulation;

internal sealed class StateHasher
{
    public ulong Value { get; private set; } = Hashing.FnvOffset;

    public StateHasher Add(long value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
        Value = Hashing.Fnv1a64(bytes, Value);
        return this;
    }

    public StateHasher Add(ulong value) => Add(unchecked((long)value));

    public StateHasher Add(uint value) => Add((long)value);

    public StateHasher Add(bool value) => Add(value ? 1L : 0L);

    public StateHasher Add(string value)
    {
        Add((long)value.Length);
        Value = Hashing.Fnv1a64(Encoding.UTF8.GetBytes(value), Value);
        return this;
    }
}
```

`src/Mortar.Game/Simulation/WorldHash.cs`:

```csharp
namespace Mortar.Game.Simulation;

public static class WorldHash
{
    public static string Compute(World world)
    {
        var hasher = new StateHasher();
        hasher.Add(world.Seed).Add(world.TickCount).Add((long)world.Depth);
        hasher.Add(world.Rng.State).Add(world.Rng.Increment);
        AddProgress(hasher, world.Progress);
        foreach (var depth in world.VisitedDepths)
        {
            var level = world.GetLevel(depth);
            hasher.Add((long)level.Depth).Add(level.Seed).Add(level.Layout.Fingerprint);
            foreach (var actor in level.Actors) AddActor(hasher, actor);
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
}
```

- [ ] **Step 5: Run the tests and create the snapshot**

Run: `dotnet test --filter "FullyQualifiedName~Gameplay.DeterminismTests"`
Expected: `ScriptedRun_MatchesSnapshot` FAILS with "Snapshot created at ..."; the other four PASS.

Open `tests/Mortar.Tests/Snapshots/game-seed-7.txt` and check it reads like a plausible fight: the player moves, monsters wake (`woke up`), swings and hits alternate, at least one line ends `(killed by #1)`, and the last line is `hash: ` followed by 16 hex digits.

Run: `dotnet test --filter "FullyQualifiedName~Gameplay.DeterminismTests"`
Expected: PASS (all five).

- [ ] **Step 6: Update the README**

In `README.md`, replace the `## Status` paragraph with:

```markdown
## Status

Version 0.1 generates **indoor** levels by stitching hand-made rooms along a landmark graph. Outdoor generation and the Unity package are planned (see `docs/design.md`).

Mortar is growing into a full action-RPG framework for Unity (see `docs/superpowers/specs/2026-10-14-arpg-framework-design.md`). `Mortar.Game` already runs a deterministic, engine-independent game simulation: a warrior, melee combat, monsters with AI, and stairs between four dungeon levels, all driven by the data in `content/` (see `docs/content.md`). Items, inventory, and save/load come next.
```

and add these rows to the repository layout table, after the `src/Mortar.Json` row:

```markdown
| `src/Mortar.Game` | The game simulation: world, actors, combat, AI, and stairs. No engine dependencies. |
| `content/` | Game data: rules, classes, monsters, and dungeon levels. |
```

- [ ] **Step 7: Run the full suite and a Release build**

Run: `dotnet test`
Expected: PASS (all tests, including the existing generator tests).

Run: `dotnet build -c Release`
Expected: build succeeds with 0 warnings.

- [ ] **Step 8: Commit**

```bash
git add src/Mortar.Game/Simulation/StateHasher.cs src/Mortar.Game/Simulation/WorldHash.cs tests/Mortar.Tests/Gameplay tests/Mortar.Tests/Snapshots/game-seed-7.txt README.md
git commit -m "add world hash, scripted run snapshot, and game docs"
```
