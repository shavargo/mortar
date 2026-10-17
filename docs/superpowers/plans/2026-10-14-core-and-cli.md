# Mortar Core and CLI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship Mortar's first release: a deterministic indoor level generator (`Mortar.Core`), its file formats (`Mortar.Json`), and a `mortar` CLI with `generate` and `preview`.

**Architecture:** `Mortar.Core` turns validated in-memory definitions (tileset, rooms, area) plus a seed into a `Layout`, using PCG32 for every random decision. Indoor generation places landmark rooms on a coarse slot grid, routes paths between them with seeded Dijkstra, fills the remaining slots with rooms whose doors line up, stitches the rooms into one cell grid, resolves corner-based tile types, and validates walkability, retrying with derived seeds. `Mortar.Json` only maps files to and from those definitions; the CLI only wires files to the library.

**Tech Stack:** C# 12, .NET Standard 2.1 (Core, Json), .NET 10 (CLI, tests), System.Text.Json, xUnit, GitHub Actions.

**Spec:** `docs/design.md` (sections 1–8, 11–13 apply to this plan; sections 9–10 and outdoor generation are later plans).

## Global Constraints

- `Mortar.Core` and `Mortar.Json` target `netstandard2.1`. `Mortar.Core` has no engine or file-format dependencies (no System.Text.Json, no file IO).
- Never use `System.Random`. All randomness comes from `Pcg32`.
- No hash-set or dictionary enumeration order in decision paths. Candidate lists are built from ordered lists; rooms are sorted by id with `StringComparer.Ordinal`.
- Floating point is only used to turn home-region fractions into slot indices (`Math.Floor` / `Math.Ceiling`).
- The library returns results (`AuthoringResult<T>`, `GenerationResult`) for authoring errors and generation failures; it never throws for them.
- Authoring errors fail immediately with no retries and name the file and problem.
- Default `maxAttempts` is 20. Retry seeds derive from (area id, seed, attempt).
- No file over 300 lines; no function over 50 lines.
- `Nullable` enabled and warnings treated as errors in every project.
- CLI exit codes: `0` success, `1` generation failed, `2` invalid input (usage, authoring, or IO error).
- Commit messages are short, one line, present tense, no prefixes (for example `add pcg32 generator`).
- MIT license; samples contain no licensed art.

## Design Interpretations

The spec leaves these choices open; this plan fixes them. Review them before execution.

1. **Tile types use a dual grid.** Cell `(x, y)` gets its tile type from the ground at cells `(x, y)` NW, `(x+1, y)` NE, `(x, y+1)` SW, `(x+1, y+1)` SE; cells outside the grid count as `void`. Engines place each tile offset by half a cell toward +x/+y. Names: one ground → its id (`floor`); two grounds → `{base}-{other}:{mask}` where `base` is the ground with the lower tileset index and `mask` lists NW, NE, SW, SE as `1` where `other` sits (`floor-water:1100` is water on the north corners); three or more → the four corner ids joined by `|` in NW, NE, SW, SE order.
2. **Seeds are unsigned 32-bit integers** (`uint`), safe in JSON for every engine and JavaScript tooling.
3. **Doors connect only where both rooms have a door on the same cell** of a shared, opened edge. Authors put doors at the middle of each slot side (offset 4 with `slotSize` 9).
4. **`branch` edges are each included with a 50% chance**; if no route exists the branch is dropped without failing the attempt.
5. **Filling uses single-slot rooms only.** Multi-slot rooms (17×17 and up) are allowed as landmark rooms.
6. **`mode: "outdoor"` is an authoring error** until the outdoor plan lands.
7. **Area `rooms` lists room files or directories** (relative to the area file); an optional `roomTags` list keeps only rooms that carry at least one of those tags.
8. **Custom landmark kinds** (not entrance/exit/boss/waypoint) need rooms tagged with that kind; their marker sits at the room's center cell.

## Review Focus

1. **The same seed on Windows, Linux, and macOS** must produce byte-identical layouts. Pinned by the snapshot tests in Task 9 and the three-OS CI matrix in Task 12.
2. **Room files enumerated in a different order** (different OS or file system) must not change output. Pinned by `RoomOrder_DoesNotAffectOutput` in Task 9.
3. **Seed edge values and bad seeds**: `0` and `4294967295` must generate deterministically; `-1`, `abc`, or a missing seed must give a clear CLI error with exit code 2. Pinned in Task 9 and Task 11.
4. **An area that can never succeed** must report counted failure reasons after `maxAttempts`, exit 1 from the CLI, and write no output file. Pinned in Task 9 and Task 11.
5. **`--out` pointing into a folder that does not exist yet** must create the folder. Pinned in Task 11.

## File Structure

```
Mortar.slnx
Directory.Build.props            shared compiler settings
.editorconfig / .gitattributes   LF endings so snapshots match on every OS
src/Mortar.Core/
  MortarInfo.cs                  library version
  Rng/Hashing.cs                 FNV-1a 64 and SplitMix64
  Rng/Pcg32.cs                   seeded generator
  Authoring/AuthoringError.cs, AuthoringResult.cs
  Authoring/RoomCompiler.cs, RoomGridParser.cs
  Authoring/AreaValidator.cs, AreaCompiler.cs
  Model/GroundType.cs, Tileset.cs, MarkerChars.cs, MarkerKinds.cs, StandardTags.cs
  Model/Side.cs, Door.cs, RoomMarker.cs, RoomDefinition.cs, Room.cs
  Model/AreaDefinition.cs, AreaGraph.cs, CompiledArea.cs
  Layouts/CellGrid.cs, Marker.cs, LayoutCells.cs, Layout.cs, LayoutFingerprint.cs
  Tiles/TileResolver.cs, ResolvedTiles.cs
  Validation/LayoutValidator.cs
  Indoor/Sides.cs, Placement.cs, SlotGrid.cs, SlotSearch.cs, SlotPathfinder.cs
  Indoor/LandmarkPlacer.cs, IndoorRouter.cs, DoorGeometry.cs, RoomFiller.cs
  Indoor/RoomStitcher.cs, MarkerCollector.cs, BuildOutcome.cs, IndoorBuilder.cs
  Generation/FailureCount.cs, GenerationResult.cs, LayoutFactory.cs, Generator.cs
src/Mortar.Json/
  Dtos.cs, JsonFile.cs, TilesetMapper.cs, AreaMapper.cs, MortarLoader.cs
  LayoutWriter.cs, LayoutReader.cs
src/Mortar.Cli/
  Program.cs, CliApp.cs, CliOptions.cs, ExitCodes.cs
  GenerateCommand.cs, PreviewCommand.cs
  Preview/Crc32.cs, PngEncoder.cs, RgbImage.cs, MapRenderer.cs
samples/tileset.json, samples/rooms/*.json, samples/areas/crypt.json
tests/Mortar.Tests/              one folder per area of the code, plus Support/ helpers and Snapshots/
docs/authoring.md, README.md, CONTRIBUTING.md, LICENSE, .github/workflows/ci.yml
```

---

### Task 1: Solution scaffold, hashing, and PCG32

**Files:**
- Create: `Mortar.slnx`, `Directory.Build.props`, `.editorconfig`, `.gitattributes`
- Create: `src/Mortar.Core/Mortar.Core.csproj`, `src/Mortar.Core/MortarInfo.cs`
- Create: `src/Mortar.Core/Rng/Hashing.cs`, `src/Mortar.Core/Rng/Pcg32.cs`
- Create: `tests/Mortar.Tests/Mortar.Tests.csproj`
- Test: `tests/Mortar.Tests/Rng/HashingTests.cs`, `tests/Mortar.Tests/Rng/Pcg32Tests.cs`

**Interfaces:**
- Produces: `Hashing.FnvOffset` (`ulong`), `Hashing.Fnv1a64(ReadOnlySpan<byte> data, ulong hash = FnvOffset) : ulong`, `Hashing.Fnv1a64(string text) : ulong`, `Hashing.SplitMix64(ulong) : ulong`; `Pcg32(ulong initState, ulong initSequence)`, `Pcg32.ForAttempt(string areaId, uint seed, int attempt) : Pcg32`, `NextUInt() : uint`, `NextInt(int maxExclusive) : int`, `NextInt(int minInclusive, int maxExclusive) : int`, `Pick<T>(IReadOnlyList<T>) : T`, `PickWeighted(IReadOnlyList<int> weights) : int`; `MortarInfo.Version` (`"0.1.0"`).

- [ ] **Step 1: Create the solution and projects**

Run from the repo root:

```powershell
dotnet new sln -n Mortar --format slnx
dotnet new classlib -n Mortar.Core -o src/Mortar.Core -f netstandard2.1
dotnet new xunit -n Mortar.Tests -o tests/Mortar.Tests -f net10.0
dotnet sln Mortar.slnx add src/Mortar.Core/Mortar.Core.csproj tests/Mortar.Tests/Mortar.Tests.csproj
dotnet add tests/Mortar.Tests reference src/Mortar.Core
Remove-Item src/Mortar.Core/Class1.cs, tests/Mortar.Tests/UnitTest1.cs
```

Replace `src/Mortar.Core/Mortar.Core.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <RootNamespace>Mortar.Core</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Mortar.Tests" />
  </ItemGroup>

</Project>
```

Open `tests/Mortar.Tests/Mortar.Tests.csproj` and make sure it contains `<Using Include="Xunit" />` inside an `<ItemGroup>` (the template normally adds it; add it if missing). Leave the template's package references as they are.

- [ ] **Step 2: Add shared settings**

`Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Deterministic>true</Deterministic>
    <Version>0.1.0</Version>
  </PropertyGroup>
</Project>
```

The test project and CLI target .NET 10; override `LangVersion` there so they get the current language. Add to the `<PropertyGroup>` of `tests/Mortar.Tests/Mortar.Tests.csproj`:

```xml
    <LangVersion>latest</LangVersion>
```

`.editorconfig`:

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space
indent_size = 4

[*.{json,yml,yaml,props,csproj,slnx}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

`.gitattributes`:

```
* text=auto eol=lf
*.png binary
```

`src/Mortar.Core/MortarInfo.cs`:

```csharp
namespace Mortar.Core;

public static class MortarInfo
{
    public const string Version = "0.1.0";
}
```

- [ ] **Step 3: Write the failing tests**

`tests/Mortar.Tests/Rng/HashingTests.cs`:

```csharp
using Mortar.Core.Rng;

namespace Mortar.Tests.Rng;

public class HashingTests
{
    [Theory]
    [InlineData("", 0xcbf29ce484222325UL)]
    [InlineData("a", 0xaf63dc4c8601ec8cUL)]
    [InlineData("foobar", 0x85944171f73967e8UL)]
    public void Fnv1a64_MatchesReferenceVectors(string text, ulong expected)
    {
        Assert.Equal(expected, Hashing.Fnv1a64(text));
    }

    [Fact]
    public void Fnv1a64_ContinuesFromAPreviousHash()
    {
        var partial = Hashing.Fnv1a64("foo");
        var continued = Hashing.Fnv1a64(System.Text.Encoding.UTF8.GetBytes("bar"), partial);
        Assert.Equal(Hashing.Fnv1a64("foobar"), continued);
    }

    [Fact]
    public void SplitMix64_MatchesReferenceVector()
    {
        Assert.Equal(0xe220a8397b1dcdafUL, Hashing.SplitMix64(0));
    }
}
```

`tests/Mortar.Tests/Rng/Pcg32Tests.cs`:

```csharp
using Mortar.Core.Rng;

namespace Mortar.Tests.Rng;

public class Pcg32Tests
{
    [Fact]
    public void MatchesPcgReferenceOutput()
    {
        var rng = new Pcg32(42, 54);
        uint[] expected = { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };
        foreach (var value in expected)
        {
            Assert.Equal(value, rng.NextUInt());
        }
    }

    [Fact]
    public void NextInt_StaysInRange()
    {
        var rng = new Pcg32(1, 1);
        for (var i = 0; i < 10_000; i++)
        {
            Assert.InRange(rng.NextInt(3, 7), 3, 6);
        }
    }

    [Fact]
    public void NextInt_RejectsEmptyRange()
    {
        var rng = new Pcg32(1, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(0));
    }

    [Fact]
    public void ForAttempt_IsDeterministic()
    {
        var a = Pcg32.ForAttempt("crypt", 7, 0);
        var b = Pcg32.ForAttempt("crypt", 7, 0);
        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(a.NextUInt(), b.NextUInt());
        }
    }

    [Fact]
    public void ForAttempt_DiffersByAreaSeedAndAttempt()
    {
        var baseline = Pcg32.ForAttempt("crypt", 7, 0).NextUInt();
        Assert.NotEqual(baseline, Pcg32.ForAttempt("crypt", 7, 1).NextUInt());
        Assert.NotEqual(baseline, Pcg32.ForAttempt("crypt", 8, 0).NextUInt());
        Assert.NotEqual(baseline, Pcg32.ForAttempt("forest", 7, 0).NextUInt());
    }

    [Fact]
    public void PickWeighted_NeverPicksZeroWeights()
    {
        var rng = new Pcg32(3, 3);
        for (var i = 0; i < 1_000; i++)
        {
            Assert.Equal(1, rng.PickWeighted(new[] { 0, 5, 0 }));
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they fail**

Run: `dotnet test Mortar.slnx`
Expected: build FAILS with `The type or namespace name 'Rng' does not exist in the namespace 'Mortar.Core'`.

- [ ] **Step 5: Implement hashing and PCG32**

`src/Mortar.Core/Rng/Hashing.cs`:

```csharp
using System.Text;

namespace Mortar.Core.Rng;

public static class Hashing
{
    public const ulong FnvOffset = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    public static ulong Fnv1a64(ReadOnlySpan<byte> data, ulong hash = FnvOffset)
    {
        foreach (var value in data)
        {
            hash ^= value;
            hash *= FnvPrime;
        }
        return hash;
    }

    public static ulong Fnv1a64(string text) => Fnv1a64(Encoding.UTF8.GetBytes(text));

    public static ulong SplitMix64(ulong value)
    {
        value += 0x9E3779B97F4A7C15UL;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
```

`src/Mortar.Core/Rng/Pcg32.cs`:

```csharp
namespace Mortar.Core.Rng;

public sealed class Pcg32
{
    private const ulong Multiplier = 6364136223846793005UL;
    private readonly ulong _increment;
    private ulong _state;

    public Pcg32(ulong initState, ulong initSequence)
    {
        _increment = (initSequence << 1) | 1UL;
        _state = 0;
        NextUInt();
        _state += initState;
        NextUInt();
    }

    public static Pcg32 ForAttempt(string areaId, uint seed, int attempt)
    {
        var mixed = Hashing.Fnv1a64(areaId) ^ (((ulong)seed << 32) | (uint)attempt);
        var state = Hashing.SplitMix64(mixed);
        return new Pcg32(state, Hashing.SplitMix64(state));
    }

    public uint NextUInt()
    {
        var old = _state;
        _state = old * Multiplier + _increment;
        var xorShifted = (uint)(((old >> 18) ^ old) >> 27);
        var rotation = (int)(old >> 59);
        return (xorShifted >> rotation) | (xorShifted << (-rotation & 31));
    }

    public int NextInt(int maxExclusive)
    {
        if (maxExclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "must be positive");
        }
        var bound = (uint)maxExclusive;
        var threshold = (uint)((0x1_0000_0000UL - bound) % bound);
        while (true)
        {
            var value = NextUInt();
            if (value >= threshold)
            {
                return (int)(value % bound);
            }
        }
    }

    public int NextInt(int minInclusive, int maxExclusive) => minInclusive + NextInt(maxExclusive - minInclusive);

    public T Pick<T>(IReadOnlyList<T> items) => items[NextInt(items.Count)];

    public int PickWeighted(IReadOnlyList<int> weights)
    {
        var roll = NextInt(weights.Sum());
        for (var i = 0; i < weights.Count; i++)
        {
            roll -= weights[i];
            if (roll < 0)
            {
                return i;
            }
        }
        return weights.Count - 1;
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test Mortar.slnx`
Expected: PASS (11 tests).

- [ ] **Step 7: Commit**

```powershell
git add Mortar.slnx Directory.Build.props .editorconfig .gitattributes src tests
git commit -m "add solution scaffold, hashing, and pcg32"
```

---

### Task 2: Tileset and room compiler

**Files:**
- Create: `src/Mortar.Core/Authoring/AuthoringError.cs`, `AuthoringResult.cs`, `RoomCompiler.cs`, `RoomGridParser.cs`
- Create: `src/Mortar.Core/Model/GroundType.cs`, `Tileset.cs`, `MarkerKinds.cs`, `MarkerChars.cs`, `Side.cs`, `Door.cs`, `RoomMarker.cs`, `RoomDefinition.cs`, `Room.cs`
- Create: `tests/Mortar.Tests/Support/TestContent.cs`
- Test: `tests/Mortar.Tests/Authoring/TilesetTests.cs`, `tests/Mortar.Tests/Authoring/RoomCompilerTests.cs`

**Interfaces:**
- Consumes: nothing from Task 1 beyond the projects.
- Produces:
  - `AuthoringError(string source, string message)` with `Source`, `Message`, `ToString()` → `"{Source}: {Message}"`.
  - `AuthoringResult<T>` (`T : class`): `Value : T?`, `Errors : IReadOnlyList<AuthoringError>`, `Succeeded : bool`, static `Success(T)`, `Failure(IReadOnlyList<AuthoringError>)`, `Failure(string source, string message)`.
  - `GroundType(string id, char symbol, bool walkable, bool blocksSight)`.
  - `Tileset.Create(string source, IReadOnlyList<GroundType>, IReadOnlyDictionary<string, IReadOnlyList<int>> variantWeights) : AuthoringResult<Tileset>`; members `Grounds`, `FloorIndex`, `WallIndex`, `VoidIndex`, `IndexOf(string) : int`, `IndexOfSymbol(char) : int`, `IsWalkable(int) : bool`, `WeightsFor(string tileType) : IReadOnlyList<int>`; constants `FloorId`, `WallId`, `VoidId`.
  - `MarkerKinds` constants `Entrance`, `Exit`, `Boss`, `Waypoint`, `Spawn`, `Chest`, list `Landmarks`.
  - `MarkerChars.Door` (`'D'`), `MarkerChars.TryGetKind(char, out string? kind) : bool`, `MarkerChars.IsReserved(char) : bool`.
  - `enum Side { North, East, South, West }`; `Door(int x, int y, Side side, int segment)`; `RoomMarker(string kind, int x, int y)`.
  - `RoomDefinition(string source, string id, IReadOnlyList<string> tags, IReadOnlyList<string> grid)`.
  - `Room`: `Id`, `Tags`, `Width`, `Height`, `Stride`, `SlotsWide`, `SlotsHigh`, `IsSingleSlot`, `Doors`, `Markers`, `GroundAt(x, y)`, `HasTag(string)`, `HasDoorAt(x, y)`, `HasDoorInSegment(Side, int)`.
  - `RoomCompiler.Compile(RoomDefinition, Tileset, int slotSize) : AuthoringResult<Room>`.
  - Test helpers `TestContent.CreateTileset(weights?)`, `TestContent.Box(width, height, doorOffsets, params (int X, int Y, char C)[] cells)`, `TestContent.RoomDef(id, tags, grid)`, `TestContent.CompileRoom(id, tags, grid)`.

- [ ] **Step 1: Write the test helpers**

`tests/Mortar.Tests/Support/TestContent.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Core.Model;

namespace Mortar.Tests.Support;

internal static partial class TestContent
{
    public static Tileset CreateTileset(IReadOnlyDictionary<string, IReadOnlyList<int>>? weights = null)
    {
        var grounds = new[]
        {
            new GroundType("void", ' ', false, false),
            new GroundType("floor", '.', true, false),
            new GroundType("wall", '#', false, true),
            new GroundType("water", '~', false, false),
        };
        var result = Tileset.Create("test-tileset.json", grounds, weights ?? new Dictionary<string, IReadOnlyList<int>>());
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        return result.Value!;
    }

    public static string[] Box(int width, int height, int[] doorOffsets, params (int X, int Y, char C)[] cells)
    {
        var rows = new char[height][];
        for (var y = 0; y < height; y++)
        {
            rows[y] = new char[width];
            for (var x = 0; x < width; x++)
            {
                var edge = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                rows[y][x] = edge ? '#' : '.';
            }
        }
        foreach (var offset in doorOffsets)
        {
            if (offset < width)
            {
                rows[0][offset] = 'D';
                rows[height - 1][offset] = 'D';
            }
            if (offset < height)
            {
                rows[offset][0] = 'D';
                rows[offset][width - 1] = 'D';
            }
        }
        foreach (var cell in cells)
        {
            rows[cell.Y][cell.X] = cell.C;
        }
        return rows.Select(row => new string(row)).ToArray();
    }

    public static RoomDefinition RoomDef(string id, string[] tags, string[] grid) =>
        new RoomDefinition($"{id}.json", id, tags, grid);

    public static Room CompileRoom(string id, string[] tags, string[] grid)
    {
        var result = RoomCompiler.Compile(RoomDef(id, tags, grid), CreateTileset(), 9);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        return result.Value!;
    }
}
```

- [ ] **Step 2: Write the failing tests**

`tests/Mortar.Tests/Authoring/TilesetTests.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Core.Model;

namespace Mortar.Tests.Authoring;

public class TilesetTests
{
    private static readonly GroundType Void = new GroundType("void", ' ', false, false);
    private static readonly GroundType Floor = new GroundType("floor", '.', true, false);
    private static readonly GroundType Wall = new GroundType("wall", '#', false, true);

    private static AuthoringResult<Tileset> Create(params GroundType[] grounds) =>
        Tileset.Create("tileset.json", grounds, new Dictionary<string, IReadOnlyList<int>>());

    private static void AssertError(AuthoringResult<Tileset> result, string expected)
    {
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains(expected) && e.Source == "tileset.json");
    }

    [Fact]
    public void Create_ValidTileset_IndexesGrounds()
    {
        var tileset = Create(Void, Floor, Wall).Value!;
        Assert.Equal(0, tileset.VoidIndex);
        Assert.Equal(1, tileset.FloorIndex);
        Assert.Equal(2, tileset.WallIndex);
        Assert.Equal(tileset.FloorIndex, tileset.IndexOfSymbol('.'));
        Assert.Equal(-1, tileset.IndexOf("lava"));
        Assert.Equal(new[] { 1 }, tileset.WeightsFor("floor"));
    }

    [Fact]
    public void Create_MissingFloor_IsAnError() =>
        AssertError(Create(Void, Wall), "missing required ground type 'floor'");

    [Fact]
    public void Create_DuplicateChar_IsAnError() =>
        AssertError(Create(Void, Floor, Wall, new GroundType("sand", '.', true, false)), "duplicate ground char '.'");

    [Fact]
    public void Create_ReservedChar_IsAnError() =>
        AssertError(Create(Void, Floor, Wall, new GroundType("door", 'D', true, false)), "uses reserved char 'D'");

    [Fact]
    public void Create_WalkableWall_IsAnError() =>
        AssertError(Create(Void, Floor, new GroundType("wall", '#', true, true)), "'wall' must not be walkable");

    [Fact]
    public void Create_AllZeroWeights_IsAnError()
    {
        var weights = new Dictionary<string, IReadOnlyList<int>> { ["floor"] = new[] { 0, 0 } };
        AssertError(Tileset.Create("tileset.json", new[] { Void, Floor, Wall }, weights), "variant weights for 'floor'");
    }
}
```

`tests/Mortar.Tests/Authoring/RoomCompilerTests.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Authoring;

public class RoomCompilerTests
{
    private static readonly Tileset Tiles = TestContent.CreateTileset();

    private static AuthoringResult<Room> Compile(string[] grid, params string[] tags) =>
        RoomCompiler.Compile(TestContent.RoomDef("room_a", tags, grid), Tiles, 9);

    private static void AssertError(AuthoringResult<Room> result, string expected)
    {
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains(expected) && e.Source == "room_a.json");
    }

    [Fact]
    public void Compile_CrossRoom_FindsFourDoorsOnSegmentZero()
    {
        var room = Compile(TestContent.Box(9, 9, new[] { 4 })).Value!;
        Assert.Equal(new[] { Side.North, Side.West, Side.East, Side.South }, room.Doors.Select(d => d.Side));
        Assert.All(room.Doors, d => Assert.Equal(0, d.Segment));
        Assert.True(room.IsSingleSlot);
        Assert.Equal(Tiles.WallIndex, room.GroundAt(4, 0));
        Assert.Equal(Tiles.FloorIndex, room.GroundAt(4, 4));
    }

    [Fact]
    public void Compile_TwoSlotRoom_AssignsDoorSegments()
    {
        var room = Compile(TestContent.Box(17, 9, new[] { 4, 12 })).Value!;
        Assert.Equal(2, room.SlotsWide);
        Assert.Equal(1, room.SlotsHigh);
        Assert.True(room.HasDoorInSegment(Side.North, 0));
        Assert.True(room.HasDoorInSegment(Side.North, 1));
        Assert.True(room.HasDoorInSegment(Side.East, 0));
    }

    [Fact]
    public void Compile_RecordsMarkersOnFloor()
    {
        var room = Compile(TestContent.Box(9, 9, new[] { 4 }, (4, 4, 'E')), "entrance").Value!;
        var marker = Assert.Single(room.Markers);
        Assert.Equal((MarkerKinds.Entrance, 4, 4), (marker.Kind, marker.X, marker.Y));
        Assert.Equal(Tiles.FloorIndex, room.GroundAt(4, 4));
    }

    [Fact]
    public void Compile_SizeOffSlotGrid_IsAnError() =>
        AssertError(Compile(TestContent.Box(8, 8, new[] { 4 })), "(slotSize - 1) x n + 1");

    [Fact]
    public void Compile_UnevenRows_IsAnError() =>
        AssertError(Compile(new[] { "####D####", "#..#" }), "row 1 has length 4");

    [Fact]
    public void Compile_UnknownChar_IsAnError() =>
        AssertError(Compile(TestContent.Box(9, 9, new[] { 4 }, (2, 2, '?'))), "unknown ground char '?' at (2, 2)");

    [Fact]
    public void Compile_InteriorDoor_IsAnError() =>
        AssertError(Compile(TestContent.Box(9, 9, new[] { 4 }, (3, 3, 'D'))), "not on its outer edge");

    [Fact]
    public void Compile_DoorOnSlotCorner_IsAnError() =>
        AssertError(Compile(TestContent.Box(17, 9, new[] { 4 }, (8, 0, 'D'))), "on a slot corner");

    [Fact]
    public void Compile_NoDoors_IsAnError() =>
        AssertError(Compile(TestContent.Box(9, 9, Array.Empty<int>())), "has no doors");

    [Fact]
    public void Compile_EntranceTagWithoutMarker_IsAnError() =>
        AssertError(Compile(TestContent.Box(9, 9, new[] { 4 }), "entrance"), "tagged 'entrance' but has no entrance marker");

    [Fact]
    public void Compile_MarkerOnEdge_IsAnError() =>
        AssertError(Compile(TestContent.Box(9, 9, new[] { 4 }, (0, 2, 'S'))), "on its edge");

    [Fact]
    public void Compile_ReportsEveryProblem()
    {
        var result = Compile(TestContent.Box(9, 9, new[] { 4 }, (2, 2, '?'), (3, 3, 'D')));
        Assert.Equal(2, result.Errors.Count);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test Mortar.slnx`
Expected: build FAILS with `The type or namespace name 'Authoring' does not exist in the namespace 'Mortar.Core'`.

- [ ] **Step 4: Implement the authoring results**

`src/Mortar.Core/Authoring/AuthoringError.cs`:

```csharp
namespace Mortar.Core.Authoring;

public sealed class AuthoringError
{
    public AuthoringError(string source, string message)
    {
        Source = source;
        Message = message;
    }

    public string Source { get; }

    public string Message { get; }

    public override string ToString() => $"{Source}: {Message}";
}
```

`src/Mortar.Core/Authoring/AuthoringResult.cs`:

```csharp
namespace Mortar.Core.Authoring;

public sealed class AuthoringResult<T>
    where T : class
{
    private AuthoringResult(T? value, IReadOnlyList<AuthoringError> errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }

    public IReadOnlyList<AuthoringError> Errors { get; }

    public bool Succeeded => Value is not null;

    public static AuthoringResult<T> Success(T value) =>
        new AuthoringResult<T>(value, Array.Empty<AuthoringError>());

    public static AuthoringResult<T> Failure(IReadOnlyList<AuthoringError> errors) =>
        new AuthoringResult<T>(null, errors);

    public static AuthoringResult<T> Failure(string source, string message) =>
        Failure(new[] { new AuthoringError(source, message) });
}
```

- [ ] **Step 5: Implement grounds, markers, and the tileset**

`src/Mortar.Core/Model/GroundType.cs`:

```csharp
namespace Mortar.Core.Model;

public sealed class GroundType
{
    public GroundType(string id, char symbol, bool walkable, bool blocksSight)
    {
        Id = id;
        Symbol = symbol;
        Walkable = walkable;
        BlocksSight = blocksSight;
    }

    public string Id { get; }

    public char Symbol { get; }

    public bool Walkable { get; }

    public bool BlocksSight { get; }
}
```

`src/Mortar.Core/Model/MarkerKinds.cs`:

```csharp
namespace Mortar.Core.Model;

public static class MarkerKinds
{
    public const string Entrance = "entrance";
    public const string Exit = "exit";
    public const string Boss = "boss";
    public const string Waypoint = "waypoint";
    public const string Spawn = "spawn";
    public const string Chest = "chest";

    public static readonly IReadOnlyList<string> Landmarks = new[] { Entrance, Exit, Boss, Waypoint };
}
```

`src/Mortar.Core/Model/MarkerChars.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;

namespace Mortar.Core.Model;

public static class MarkerChars
{
    public const char Door = 'D';

    private static readonly Dictionary<char, string> KindsByChar = new Dictionary<char, string>
    {
        ['E'] = MarkerKinds.Entrance,
        ['X'] = MarkerKinds.Exit,
        ['B'] = MarkerKinds.Boss,
        ['W'] = MarkerKinds.Waypoint,
        ['S'] = MarkerKinds.Spawn,
        ['C'] = MarkerKinds.Chest,
    };

    public static bool TryGetKind(char symbol, [NotNullWhen(true)] out string? kind) =>
        KindsByChar.TryGetValue(symbol, out kind);

    public static bool IsReserved(char symbol) => symbol == Door || KindsByChar.ContainsKey(symbol);
}
```

`src/Mortar.Core/Model/Tileset.cs`:

```csharp
using Mortar.Core.Authoring;

namespace Mortar.Core.Model;

public sealed class Tileset
{
    public const string FloorId = "floor";
    public const string WallId = "wall";
    public const string VoidId = "void";

    private static readonly IReadOnlyList<int> DefaultWeights = new[] { 1 };
    private static readonly string[] RequiredIds = { VoidId, FloorId, WallId };

    private readonly Dictionary<string, int> _byId = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<char, int> _bySymbol = new Dictionary<char, int>();
    private readonly IReadOnlyDictionary<string, IReadOnlyList<int>> _variantWeights;

    private Tileset(IReadOnlyList<GroundType> grounds, IReadOnlyDictionary<string, IReadOnlyList<int>> variantWeights)
    {
        Grounds = grounds;
        _variantWeights = variantWeights;
        for (var i = 0; i < grounds.Count; i++)
        {
            _byId[grounds[i].Id] = i;
            _bySymbol[grounds[i].Symbol] = i;
        }
        FloorIndex = _byId[FloorId];
        WallIndex = _byId[WallId];
        VoidIndex = _byId[VoidId];
    }

    public IReadOnlyList<GroundType> Grounds { get; }

    public int FloorIndex { get; }

    public int WallIndex { get; }

    public int VoidIndex { get; }

    public int IndexOf(string id) => _byId.TryGetValue(id, out var index) ? index : -1;

    public int IndexOfSymbol(char symbol) => _bySymbol.TryGetValue(symbol, out var index) ? index : -1;

    public bool IsWalkable(int index) => Grounds[index].Walkable;

    public IReadOnlyList<int> WeightsFor(string tileType) =>
        _variantWeights.TryGetValue(tileType, out var weights) ? weights : DefaultWeights;

    public static AuthoringResult<Tileset> Create(
        string source,
        IReadOnlyList<GroundType> grounds,
        IReadOnlyDictionary<string, IReadOnlyList<int>> variantWeights)
    {
        var errors = new List<AuthoringError>();
        CheckGrounds(source, grounds, errors);
        CheckWeights(source, variantWeights, errors);
        return errors.Count > 0
            ? AuthoringResult<Tileset>.Failure(errors)
            : AuthoringResult<Tileset>.Success(new Tileset(grounds, variantWeights));
    }

    private static void CheckGrounds(string source, IReadOnlyList<GroundType> grounds, List<AuthoringError> errors)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var symbols = new HashSet<char>();
        foreach (var ground in grounds)
        {
            if (string.IsNullOrWhiteSpace(ground.Id))
                errors.Add(new AuthoringError(source, "a ground type has no id"));
            else if (!ids.Add(ground.Id))
                errors.Add(new AuthoringError(source, $"duplicate ground id '{ground.Id}'"));
            if (!symbols.Add(ground.Symbol))
                errors.Add(new AuthoringError(source, $"duplicate ground char '{ground.Symbol}'"));
            if (MarkerChars.IsReserved(ground.Symbol))
                errors.Add(new AuthoringError(source, $"ground '{ground.Id}' uses reserved char '{ground.Symbol}'"));
        }
        foreach (var required in RequiredIds)
        {
            if (!ids.Contains(required))
                errors.Add(new AuthoringError(source, $"missing required ground type '{required}'"));
        }
        CheckWalkability(source, grounds, errors);
    }

    private static void CheckWalkability(string source, IReadOnlyList<GroundType> grounds, List<AuthoringError> errors)
    {
        foreach (var ground in grounds)
        {
            if (ground.Id == FloorId && !ground.Walkable)
                errors.Add(new AuthoringError(source, "ground type 'floor' must be walkable"));
            if ((ground.Id == WallId || ground.Id == VoidId) && ground.Walkable)
                errors.Add(new AuthoringError(source, $"ground type '{ground.Id}' must not be walkable"));
        }
    }

    private static void CheckWeights(
        string source,
        IReadOnlyDictionary<string, IReadOnlyList<int>> variantWeights,
        List<AuthoringError> errors)
    {
        foreach (var tileType in variantWeights.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            var weights = variantWeights[tileType];
            if (weights.Count == 0 || weights.Any(w => w < 0) || weights.All(w => w == 0))
            {
                errors.Add(new AuthoringError(
                    source,
                    $"variant weights for '{tileType}' must be non-negative with at least one positive weight"));
            }
        }
    }
}
```

- [ ] **Step 6: Implement the room model**

`src/Mortar.Core/Model/Side.cs`:

```csharp
namespace Mortar.Core.Model;

public enum Side
{
    North,
    East,
    South,
    West,
}
```

`src/Mortar.Core/Model/Door.cs`:

```csharp
namespace Mortar.Core.Model;

public sealed class Door
{
    public Door(int x, int y, Side side, int segment)
    {
        X = x;
        Y = y;
        Side = side;
        Segment = segment;
    }

    public int X { get; }

    public int Y { get; }

    public Side Side { get; }

    public int Segment { get; }
}
```

`src/Mortar.Core/Model/RoomMarker.cs`:

```csharp
namespace Mortar.Core.Model;

public sealed class RoomMarker
{
    public RoomMarker(string kind, int x, int y)
    {
        Kind = kind;
        X = x;
        Y = y;
    }

    public string Kind { get; }

    public int X { get; }

    public int Y { get; }
}
```

`src/Mortar.Core/Model/RoomDefinition.cs`:

```csharp
namespace Mortar.Core.Model;

public sealed class RoomDefinition
{
    public RoomDefinition(string source, string id, IReadOnlyList<string> tags, IReadOnlyList<string> grid)
    {
        Source = source;
        Id = id;
        Tags = tags;
        Grid = grid;
    }

    public string Source { get; }

    public string Id { get; }

    public IReadOnlyList<string> Tags { get; }

    public IReadOnlyList<string> Grid { get; }
}
```

`src/Mortar.Core/Model/Room.cs`:

```csharp
namespace Mortar.Core.Model;

public sealed class Room
{
    private readonly int[] _ground;

    internal Room(
        string id,
        IReadOnlyList<string> tags,
        int width,
        int height,
        int stride,
        int[] ground,
        IReadOnlyList<Door> doors,
        IReadOnlyList<RoomMarker> markers)
    {
        Id = id;
        Tags = tags;
        Width = width;
        Height = height;
        Stride = stride;
        _ground = ground;
        Doors = doors;
        Markers = markers;
    }

    public string Id { get; }

    public IReadOnlyList<string> Tags { get; }

    public int Width { get; }

    public int Height { get; }

    public int Stride { get; }

    public int SlotsWide => (Width - 1) / Stride;

    public int SlotsHigh => (Height - 1) / Stride;

    public bool IsSingleSlot => SlotsWide == 1 && SlotsHigh == 1;

    public IReadOnlyList<Door> Doors { get; }

    public IReadOnlyList<RoomMarker> Markers { get; }

    public int GroundAt(int x, int y) => _ground[y * Width + x];

    public bool HasTag(string tag) => Tags.Contains(tag);

    public bool HasDoorAt(int x, int y) => Doors.Any(d => d.X == x && d.Y == y);

    public bool HasDoorInSegment(Side side, int segment) => Doors.Any(d => d.Side == side && d.Segment == segment);
}
```

- [ ] **Step 7: Implement the room compiler**

`src/Mortar.Core/Authoring/RoomCompiler.cs`:

```csharp
using Mortar.Core.Model;

namespace Mortar.Core.Authoring;

public static class RoomCompiler
{
    public static AuthoringResult<Room> Compile(RoomDefinition definition, Tileset tileset, int slotSize)
    {
        var errors = new List<AuthoringError>();
        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            errors.Add(new AuthoringError(definition.Source, "room has no id"));
        }
        if (!CheckShape(definition, slotSize, errors))
        {
            return AuthoringResult<Room>.Failure(errors);
        }

        var room = new RoomGridParser(definition, tileset, slotSize - 1, errors).Parse();
        return errors.Count > 0 ? AuthoringResult<Room>.Failure(errors) : AuthoringResult<Room>.Success(room);
    }

    private static bool CheckShape(RoomDefinition definition, int slotSize, List<AuthoringError> errors)
    {
        var grid = definition.Grid;
        if (grid.Count == 0 || grid[0].Length == 0)
        {
            errors.Add(new AuthoringError(definition.Source, $"room '{definition.Id}' has an empty grid"));
            return false;
        }
        var width = grid[0].Length;
        for (var y = 1; y < grid.Count; y++)
        {
            if (grid[y].Length != width)
            {
                errors.Add(new AuthoringError(
                    definition.Source,
                    $"room '{definition.Id}' row {y} has length {grid[y].Length}, expected {width}"));
                return false;
            }
        }
        var stride = slotSize - 1;
        if (FitsSlots(width, stride) && FitsSlots(grid.Count, stride))
        {
            return true;
        }
        errors.Add(new AuthoringError(
            definition.Source,
            $"room '{definition.Id}' is {width}x{grid.Count}; width and height must be (slotSize - 1) x n + 1 " +
            $"(for slotSize {slotSize}: {stride + 1}, {2 * stride + 1}, {3 * stride + 1}...)"));
        return false;
    }

    private static bool FitsSlots(int length, int stride) => length > 1 && (length - 1) % stride == 0;
}
```

`src/Mortar.Core/Authoring/RoomGridParser.cs`:

```csharp
using Mortar.Core.Model;

namespace Mortar.Core.Authoring;

internal sealed class RoomGridParser
{
    private readonly RoomDefinition _definition;
    private readonly Tileset _tileset;
    private readonly int _stride;
    private readonly List<AuthoringError> _errors;
    private readonly int _width;
    private readonly int _height;
    private readonly int[] _ground;
    private readonly List<Door> _doors = new List<Door>();
    private readonly List<RoomMarker> _markers = new List<RoomMarker>();

    public RoomGridParser(RoomDefinition definition, Tileset tileset, int stride, List<AuthoringError> errors)
    {
        _definition = definition;
        _tileset = tileset;
        _stride = stride;
        _errors = errors;
        _width = definition.Grid[0].Length;
        _height = definition.Grid.Count;
        _ground = new int[_width * _height];
    }

    public Room Parse()
    {
        for (var y = 0; y < _height; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                ParseCell(x, y);
            }
        }
        CheckDoorsAndTags();
        return new Room(_definition.Id, _definition.Tags, _width, _height, _stride, _ground, _doors, _markers);
    }

    private void ParseCell(int x, int y)
    {
        var symbol = _definition.Grid[y][x];
        var index = y * _width + x;
        if (symbol == MarkerChars.Door)
        {
            _ground[index] = _tileset.WallIndex;
            AddDoor(x, y);
            return;
        }
        if (MarkerChars.TryGetKind(symbol, out var kind))
        {
            _ground[index] = _tileset.FloorIndex;
            AddMarker(kind, symbol, x, y);
            return;
        }
        var ground = _tileset.IndexOfSymbol(symbol);
        if (ground < 0)
        {
            Error($"uses unknown ground char '{symbol}' at ({x}, {y})");
            ground = _tileset.VoidIndex;
        }
        _ground[index] = ground;
    }

    private void AddDoor(int x, int y)
    {
        if (!TryGetEdge(x, y, out var side, out var along))
        {
            Error($"has a door at ({x}, {y}) that is not on its outer edge");
            return;
        }
        if (along % _stride == 0)
        {
            Error($"has a door at ({x}, {y}) on a slot corner; doors must sit between slot corners");
            return;
        }
        _doors.Add(new Door(x, y, side, along / _stride));
    }

    private void AddMarker(string kind, char symbol, int x, int y)
    {
        if (x == 0 || y == 0 || x == _width - 1 || y == _height - 1)
        {
            Error($"has marker '{symbol}' on its edge at ({x}, {y})");
            return;
        }
        _markers.Add(new RoomMarker(kind, x, y));
    }

    private bool TryGetEdge(int x, int y, out Side side, out int along)
    {
        (side, along) = (Side.North, x);
        if (y == 0) return true;
        (side, along) = (Side.South, x);
        if (y == _height - 1) return true;
        (side, along) = (Side.West, y);
        if (x == 0) return true;
        (side, along) = (Side.East, y);
        return x == _width - 1;
    }

    private void CheckDoorsAndTags()
    {
        if (_doors.Count == 0)
        {
            Error("has no doors");
        }
        foreach (var landmark in MarkerKinds.Landmarks)
        {
            if (_definition.Tags.Contains(landmark) && !_markers.Any(m => m.Kind == landmark))
            {
                Error($"is tagged '{landmark}' but has no {landmark} marker");
            }
        }
    }

    private void Error(string message) =>
        _errors.Add(new AuthoringError(_definition.Source, $"room '{_definition.Id}' {message}"));
}
```

- [ ] **Step 8: Run tests to verify they pass**

Run: `dotnet test Mortar.slnx`
Expected: PASS (all tests, including 6 tileset and 12 room compiler tests).

- [ ] **Step 9: Commit**

```powershell
git add src/Mortar.Core tests/Mortar.Tests
git commit -m "add tileset and room compiler"
```

---

### Task 3: Area definitions and area compiler

**Files:**
- Create: `src/Mortar.Core/Model/AreaDefinition.cs`, `AreaGraph.cs`, `CompiledArea.cs`, `StandardTags.cs`
- Create: `src/Mortar.Core/Authoring/AreaValidator.cs`, `AreaCompiler.cs`
- Create: `tests/Mortar.Tests/Support/TestContent.Crypt.cs`
- Test: `tests/Mortar.Tests/Authoring/AreaCompilerTests.cs`

**Interfaces:**
- Consumes: `Tileset`, `RoomDefinition`, `Room`, `RoomCompiler.Compile`, `AuthoringResult<T>`, `MarkerKinds` (Task 2).
- Produces:
  - `enum AreaMode { Indoor, Outdoor }`; `TargetSize(int width, int height, int tolerancePercent)` with `MinWidth`, `MaxWidth`, `MinHeight`, `MaxHeight`.
  - `FractionRect(double x0, double x1, double y0, double y1)`; `GraphNode(string id, string kind, FractionRect region)`; `enum EdgeKind { Path, Branch }`; `GraphEdge(string from, string to, EdgeKind kind)`.
  - `AreaDefinition(string source, string id, AreaMode mode, TargetSize targetSize, int slotSize, int maxSideBranches, int maxAttempts, IReadOnlyList<string> roomTags, IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges)`; `AreaDefinition.DefaultMaxAttempts` = 20.
  - `CompiledArea`: `Definition`, `Tileset`, `Rooms` (sorted by id), `RoomsTagged(string) : IReadOnlyList<Room>`, `Node(string id) : GraphNode`.
  - `StandardTags.Filler`, `Corridor`, `DeadEnd`.
  - `AreaCompiler.Compile(AreaDefinition, Tileset, IReadOnlyList<RoomDefinition>) : AuthoringResult<CompiledArea>`.
  - Test helpers `TestContent.CryptNodes()`, `TestContent.CryptEdges()`, `TestContent.CryptDefinition(...)`, `TestContent.CryptRooms()`, `TestContent.CompileCrypt(definition?, rooms?)`.

- [ ] **Step 1: Write the crypt test content**

`tests/Mortar.Tests/Support/TestContent.Crypt.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Core.Model;

namespace Mortar.Tests.Support;

internal static partial class TestContent
{
    public static GraphNode[] CryptNodes() => new[]
    {
        new GraphNode("entrance", "entrance", new FractionRect(0.0, 0.1, 0.3, 0.7)),
        new GraphNode("waypoint", "waypoint", new FractionRect(0.3, 0.7, 0.0, 0.1)),
        new GraphNode("boss", "boss", new FractionRect(0.9, 1.0, 0.3, 0.7)),
        new GraphNode("exit", "exit", new FractionRect(0.3, 0.7, 0.9, 1.0)),
    };

    public static GraphEdge[] CryptEdges() => new[]
    {
        new GraphEdge("entrance", "waypoint", EdgeKind.Path),
        new GraphEdge("waypoint", "boss", EdgeKind.Path),
        new GraphEdge("boss", "exit", EdgeKind.Path),
        new GraphEdge("entrance", "exit", EdgeKind.Branch),
    };

    public static AreaDefinition CryptDefinition(
        AreaMode mode = AreaMode.Indoor,
        GraphNode[]? nodes = null,
        GraphEdge[]? edges = null,
        int size = 60,
        int tolerance = 15,
        int maxAttempts = 20,
        string[]? roomTags = null) =>
        new AreaDefinition(
            "crypt.json",
            "crypt",
            mode,
            new TargetSize(size, size, tolerance),
            9,
            4,
            maxAttempts,
            roomTags ?? Array.Empty<string>(),
            nodes ?? CryptNodes(),
            edges ?? CryptEdges());

    public static List<RoomDefinition> CryptRooms() => new List<RoomDefinition>
    {
        RoomDef("entrance", new[] { "entrance" }, Box(9, 9, new[] { 4 }, (4, 4, 'E'))),
        RoomDef("exit", new[] { "exit" }, Box(9, 9, new[] { 4 }, (4, 4, 'X'))),
        RoomDef("waypoint", new[] { "waypoint" }, Box(9, 9, new[] { 4 }, (4, 4, 'W'))),
        RoomDef("boss", new[] { "boss" }, Box(17, 17, new[] { 4, 12 }, (8, 8, 'B'))),
        RoomDef("hall", new[] { "filler" }, Box(9, 9, new[] { 4 }, (4, 4, 'S'))),
        RoomDef("alcove", new[] { "dead_end" }, Box(9, 9, new[] { 4 }, (4, 4, 'C'))),
    };

    public static CompiledArea CompileCrypt(AreaDefinition? definition = null, List<RoomDefinition>? rooms = null)
    {
        var result = AreaCompiler.Compile(definition ?? CryptDefinition(), CreateTileset(), rooms ?? CryptRooms());
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        return result.Value!;
    }
}
```

- [ ] **Step 2: Write the failing tests**

`tests/Mortar.Tests/Authoring/AreaCompilerTests.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Authoring;

public class AreaCompilerTests
{
    private static AuthoringResult<CompiledArea> Compile(AreaDefinition area, List<RoomDefinition>? rooms = null) =>
        AreaCompiler.Compile(area, TestContent.CreateTileset(), rooms ?? TestContent.CryptRooms());

    private static void AssertError(AuthoringResult<CompiledArea> result, string expected)
    {
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains(expected));
    }

    [Fact]
    public void Compile_CryptArea_SortsRoomsById()
    {
        var area = TestContent.CompileCrypt();
        Assert.Equal(
            new[] { "alcove", "boss", "entrance", "exit", "hall", "waypoint" },
            area.Rooms.Select(r => r.Id));
        Assert.Equal("boss", Assert.Single(area.RoomsTagged("boss")).Id);
    }

    [Fact]
    public void Compile_OutdoorMode_IsNotSupportedYet() =>
        AssertError(Compile(TestContent.CryptDefinition(mode: AreaMode.Outdoor)), "outdoor areas are not supported yet");

    [Fact]
    public void Compile_EdgeToUnknownNode_IsAnError()
    {
        var edges = TestContent.CryptEdges().Append(new GraphEdge("entrance", "ghost", EdgeKind.Path)).ToArray();
        AssertError(Compile(TestContent.CryptDefinition(edges: edges)), "unknown node 'ghost'");
    }

    [Fact]
    public void Compile_NoEntranceNode_IsAnError()
    {
        var nodes = new[] { new GraphNode("boss", "boss", new FractionRect(0, 1, 0, 1)) };
        var area = TestContent.CryptDefinition(nodes: nodes, edges: Array.Empty<GraphEdge>());
        AssertError(Compile(area), "exactly one entrance node, found 0");
    }

    [Fact]
    public void Compile_InvalidRegion_IsAnError()
    {
        var nodes = TestContent.CryptNodes();
        nodes[1] = new GraphNode("waypoint", "waypoint", new FractionRect(0.5, 0.2, 0, 1));
        AssertError(Compile(TestContent.CryptDefinition(nodes: nodes)), "node 'waypoint' region must lie within 0..1");
    }

    [Fact]
    public void Compile_NodeWithoutPathToEntrance_IsAnError()
    {
        var edges = new[] { new GraphEdge("entrance", "waypoint", EdgeKind.Path), new GraphEdge("entrance", "boss", EdgeKind.Branch) };
        AssertError(Compile(TestContent.CryptDefinition(edges: edges)), "node 'boss' is not connected to the entrance by path edges");
    }

    [Fact]
    public void Compile_TagNoRoomProvides_IsAnError()
    {
        var rooms = TestContent.CryptRooms().Where(r => r.Id != "boss").ToList();
        AssertError(Compile(TestContent.CryptDefinition(), rooms), "no room provides tag 'boss' needed by node 'boss'");
    }

    [Fact]
    public void Compile_RoomTagsFilterRemovesFillers_IsAnError()
    {
        var area = TestContent.CryptDefinition(roomTags: new[] { "entrance", "exit", "waypoint", "boss" });
        AssertError(Compile(area), "no single-slot room is tagged 'filler' or 'corridor'");
    }

    [Fact]
    public void Compile_DuplicateRoomIds_IsAnError()
    {
        var rooms = TestContent.CryptRooms();
        rooms.Add(TestContent.RoomDef("hall", new[] { "filler" }, TestContent.Box(9, 9, new[] { 4 })));
        AssertError(Compile(TestContent.CryptDefinition(), rooms), "more than one room has id 'hall'");
    }

    [Fact]
    public void Compile_RoomErrors_NameTheRoomFile()
    {
        var rooms = TestContent.CryptRooms();
        rooms.Add(TestContent.RoomDef("broken", new[] { "filler" }, TestContent.Box(8, 8, new[] { 4 })));
        var result = Compile(TestContent.CryptDefinition(), rooms);
        Assert.Contains(result.Errors, e => e.Source == "broken.json");
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test Mortar.slnx`
Expected: build FAILS with `The type or namespace name 'GraphNode' could not be found`.

- [ ] **Step 4: Implement the area model**

`src/Mortar.Core/Model/AreaDefinition.cs`:

```csharp
namespace Mortar.Core.Model;

public enum AreaMode
{
    Indoor,
    Outdoor,
}

public sealed class TargetSize
{
    public TargetSize(int width, int height, int tolerancePercent)
    {
        Width = width;
        Height = height;
        TolerancePercent = tolerancePercent;
    }

    public int Width { get; }

    public int Height { get; }

    public int TolerancePercent { get; }

    public int MinWidth => Width * (100 - TolerancePercent) / 100;

    public int MaxWidth => (Width * (100 + TolerancePercent) + 99) / 100;

    public int MinHeight => Height * (100 - TolerancePercent) / 100;

    public int MaxHeight => (Height * (100 + TolerancePercent) + 99) / 100;
}

public sealed class AreaDefinition
{
    public const int DefaultMaxAttempts = 20;

    public AreaDefinition(
        string source,
        string id,
        AreaMode mode,
        TargetSize targetSize,
        int slotSize,
        int maxSideBranches,
        int maxAttempts,
        IReadOnlyList<string> roomTags,
        IReadOnlyList<GraphNode> nodes,
        IReadOnlyList<GraphEdge> edges)
    {
        Source = source;
        Id = id;
        Mode = mode;
        TargetSize = targetSize;
        SlotSize = slotSize;
        MaxSideBranches = maxSideBranches;
        MaxAttempts = maxAttempts;
        RoomTags = roomTags;
        Nodes = nodes;
        Edges = edges;
    }

    public string Source { get; }

    public string Id { get; }

    public AreaMode Mode { get; }

    public TargetSize TargetSize { get; }

    public int SlotSize { get; }

    public int MaxSideBranches { get; }

    public int MaxAttempts { get; }

    public IReadOnlyList<string> RoomTags { get; }

    public IReadOnlyList<GraphNode> Nodes { get; }

    public IReadOnlyList<GraphEdge> Edges { get; }
}
```

`src/Mortar.Core/Model/AreaGraph.cs`:

```csharp
namespace Mortar.Core.Model;

public sealed class FractionRect
{
    public FractionRect(double x0, double x1, double y0, double y1)
    {
        X0 = x0;
        X1 = x1;
        Y0 = y0;
        Y1 = y1;
    }

    public double X0 { get; }

    public double X1 { get; }

    public double Y0 { get; }

    public double Y1 { get; }
}

public sealed class GraphNode
{
    public GraphNode(string id, string kind, FractionRect region)
    {
        Id = id;
        Kind = kind;
        Region = region;
    }

    public string Id { get; }

    public string Kind { get; }

    public FractionRect Region { get; }
}

public enum EdgeKind
{
    Path,
    Branch,
}

public sealed class GraphEdge
{
    public GraphEdge(string from, string to, EdgeKind kind)
    {
        From = from;
        To = to;
        Kind = kind;
    }

    public string From { get; }

    public string To { get; }

    public EdgeKind Kind { get; }
}
```

`src/Mortar.Core/Model/StandardTags.cs`:

```csharp
namespace Mortar.Core.Model;

public static class StandardTags
{
    public const string Filler = "filler";
    public const string Corridor = "corridor";
    public const string DeadEnd = "dead_end";
}
```

`src/Mortar.Core/Model/CompiledArea.cs`:

```csharp
namespace Mortar.Core.Model;

public sealed class CompiledArea
{
    internal CompiledArea(AreaDefinition definition, Tileset tileset, IReadOnlyList<Room> rooms)
    {
        Definition = definition;
        Tileset = tileset;
        Rooms = rooms;
    }

    public AreaDefinition Definition { get; }

    public Tileset Tileset { get; }

    public IReadOnlyList<Room> Rooms { get; }

    public IReadOnlyList<Room> RoomsTagged(string tag) => Rooms.Where(r => r.HasTag(tag)).ToList();

    public GraphNode Node(string id) => Definition.Nodes.First(n => n.Id == id);
}
```

- [ ] **Step 5: Implement the validator and compiler**

`src/Mortar.Core/Authoring/AreaValidator.cs`:

```csharp
using Mortar.Core.Model;

namespace Mortar.Core.Authoring;

internal static class AreaValidator
{
    public static List<AuthoringError> Validate(AreaDefinition area)
    {
        var errors = new List<AuthoringError>();
        CheckSettings(area, errors);
        CheckNodes(area, errors);
        CheckEdges(area, errors);
        if (errors.Count == 0)
        {
            CheckConnectivity(area, errors);
        }
        return errors;
    }

    private static void CheckSettings(AreaDefinition area, List<AuthoringError> errors)
    {
        void Error(string message) => errors.Add(new AuthoringError(area.Source, message));
        var size = area.TargetSize;
        if (string.IsNullOrWhiteSpace(area.Id)) Error("area has no id");
        if (area.Mode == AreaMode.Outdoor) Error("outdoor areas are not supported yet; use mode 'indoor'");
        if (area.SlotSize < 3) Error("slotSize must be at least 3");
        if (size.Width < area.SlotSize || size.Height < area.SlotSize) Error("targetSize width and height must be at least slotSize");
        if (size.TolerancePercent < 0 || size.TolerancePercent > 100) Error("targetSize tolerance must be between 0 and 100");
        if (area.MaxAttempts < 1) Error("maxAttempts must be at least 1");
        if (area.MaxSideBranches < 0) Error("maxSideBranches must not be negative");
    }

    private static void CheckNodes(AreaDefinition area, List<AuthoringError> errors)
    {
        void Error(string message) => errors.Add(new AuthoringError(area.Source, message));
        if (area.Nodes.Count == 0) Error("graph has no nodes");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in area.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id)) Error("a graph node has no id");
            else if (!ids.Add(node.Id)) Error($"duplicate node id '{node.Id}'");
            if (!IsValidRegion(node.Region)) Error($"node '{node.Id}' region must lie within 0..1 with from <= to");
        }
        var entrances = area.Nodes.Count(n => n.Kind == MarkerKinds.Entrance);
        if (entrances != 1) Error($"graph must have exactly one entrance node, found {entrances}");
    }

    private static bool IsValidRegion(FractionRect region) =>
        InUnit(region.X0) && InUnit(region.X1) && InUnit(region.Y0) && InUnit(region.Y1)
        && region.X0 <= region.X1 && region.Y0 <= region.Y1;

    private static bool InUnit(double value) => value >= 0 && value <= 1;

    private static void CheckEdges(AreaDefinition area, List<AuthoringError> errors)
    {
        var ids = area.Nodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var edge in area.Edges)
        {
            var label = $"edge {edge.From}->{edge.To}";
            if (!ids.Contains(edge.From)) errors.Add(new AuthoringError(area.Source, $"{label} references unknown node '{edge.From}'"));
            if (!ids.Contains(edge.To)) errors.Add(new AuthoringError(area.Source, $"{label} references unknown node '{edge.To}'"));
            if (edge.From == edge.To) errors.Add(new AuthoringError(area.Source, $"{label} connects a node to itself"));
        }
    }

    private static void CheckConnectivity(AreaDefinition area, List<AuthoringError> errors)
    {
        var entrance = area.Nodes.First(n => n.Kind == MarkerKinds.Entrance).Id;
        var reached = new HashSet<string>(StringComparer.Ordinal) { entrance };
        var queue = new Queue<string>();
        queue.Enqueue(entrance);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var edge in area.Edges.Where(e => e.Kind == EdgeKind.Path))
            {
                var next = edge.From == current ? edge.To : edge.To == current ? edge.From : null;
                if (next is not null && reached.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }
        foreach (var node in area.Nodes.Where(n => !reached.Contains(n.Id)))
        {
            errors.Add(new AuthoringError(area.Source, $"node '{node.Id}' is not connected to the entrance by path edges"));
        }
    }
}
```

`src/Mortar.Core/Authoring/AreaCompiler.cs`:

```csharp
using Mortar.Core.Model;

namespace Mortar.Core.Authoring;

public static class AreaCompiler
{
    public static AuthoringResult<CompiledArea> Compile(
        AreaDefinition area,
        Tileset tileset,
        IReadOnlyList<RoomDefinition> roomDefinitions)
    {
        var errors = AreaValidator.Validate(area);
        if (errors.Count > 0)
        {
            return AuthoringResult<CompiledArea>.Failure(errors);
        }
        var rooms = CompileRooms(area, tileset, roomDefinitions, errors);
        if (errors.Count == 0)
        {
            CheckTagCoverage(area, rooms, errors);
        }
        return errors.Count > 0
            ? AuthoringResult<CompiledArea>.Failure(errors)
            : AuthoringResult<CompiledArea>.Success(new CompiledArea(area, tileset, rooms));
    }

    private static List<Room> CompileRooms(
        AreaDefinition area,
        Tileset tileset,
        IReadOnlyList<RoomDefinition> roomDefinitions,
        List<AuthoringError> errors)
    {
        var rooms = new List<Room>();
        foreach (var definition in roomDefinitions)
        {
            if (area.RoomTags.Count > 0 && !definition.Tags.Any(tag => area.RoomTags.Contains(tag)))
            {
                continue;
            }
            var result = RoomCompiler.Compile(definition, tileset, area.SlotSize);
            if (result.Succeeded) rooms.Add(result.Value!);
            else errors.AddRange(result.Errors);
        }
        var duplicates = rooms
            .GroupBy(r => r.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(id => id, StringComparer.Ordinal);
        foreach (var id in duplicates)
        {
            errors.Add(new AuthoringError(area.Source, $"more than one room has id '{id}'"));
        }
        return rooms.OrderBy(r => r.Id, StringComparer.Ordinal).ToList();
    }

    private static void CheckTagCoverage(AreaDefinition area, List<Room> rooms, List<AuthoringError> errors)
    {
        foreach (var node in area.Nodes)
        {
            if (!rooms.Any(r => r.HasTag(node.Kind)))
            {
                errors.Add(new AuthoringError(area.Source, $"no room provides tag '{node.Kind}' needed by node '{node.Id}'"));
            }
        }
        if (!rooms.Any(r => r.IsSingleSlot && (r.HasTag(StandardTags.Filler) || r.HasTag(StandardTags.Corridor))))
        {
            errors.Add(new AuthoringError(area.Source, "no single-slot room is tagged 'filler' or 'corridor'"));
        }
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test Mortar.slnx`
Expected: PASS (all tests, including 10 area compiler tests).

- [ ] **Step 7: Commit**

```powershell
git add src/Mortar.Core tests/Mortar.Tests
git commit -m "add area definitions and compiler"
```

---

### Task 4: Layout model, tile resolution, and fingerprint

**Files:**
- Create: `src/Mortar.Core/Layouts/CellGrid.cs`, `Marker.cs`, `LayoutCells.cs`, `Layout.cs`, `LayoutFingerprint.cs`
- Create: `src/Mortar.Core/Tiles/TileResolver.cs`, `ResolvedTiles.cs`
- Create: `tests/Mortar.Tests/Support/TestContent.Cells.cs`
- Test: `tests/Mortar.Tests/Tiles/TileResolverTests.cs`, `tests/Mortar.Tests/Layouts/FingerprintTests.cs`

**Interfaces:**
- Consumes: `Tileset` (Task 2), `Pcg32`, `Hashing` (Task 1), `MortarInfo.Version` (Task 1).
- Produces:
  - `internal CellGrid(int width, int height, int fill)`: `Width`, `Height`, `Ground : int[]`, `InBounds(x, y)`, indexer `this[int x, int y]`.
  - `Marker(string kind, string? nodeId, int x, int y)`: `Kind`, `NodeId`, `X`, `Y`.
  - `LayoutCells(int width, int height, IReadOnlyList<string> groundTypes, int[] ground, IReadOnlyList<string> tileTypes, int[] tiles, int[] variants, bool[] walkable)`: `Width`, `Height`, `GroundTypes`, `TileTypes`, `Ground`, `Tiles`, `Variants` (`IReadOnlyList<int>`), `Walkable` (`IReadOnlyList<bool>`), `GroundAt(x, y) : string`, `TileAt(x, y) : string`, `VariantAt(x, y) : int`, `IsWalkable(x, y) : bool`.
  - `Layout(string areaId, uint seed, int attempt, LayoutCells cells, IReadOnlyList<Marker> markers, string mortarVersion, int formatVersion)`: `AreaId`, `Seed`, `Attempt`, `Width`, `Height`, `Cells`, `Markers`, `MortarVersion`, `FormatVersion`, `Fingerprint`; constant `Layout.CurrentFormatVersion` = 1.
  - `LayoutFingerprint.Compute(string areaId, uint seed, int attempt, LayoutCells cells) : string` (16 lowercase hex chars).
  - `internal TileResolver.Name(Tileset, int[] corners) : string`, `TileTypeAt(CellGrid, Tileset, x, y) : string`, `Resolve(CellGrid, Tileset, Pcg32) : ResolvedTiles`.
  - `internal ResolvedTiles`: `TileTypes : IReadOnlyList<string>`, `Tiles : int[]`, `Variants : int[]`.
  - Test helper `TestContent.Cells(Tileset, params string[] rows) : CellGrid`.

- [ ] **Step 1: Write the cells test helper**

`tests/Mortar.Tests/Support/TestContent.Cells.cs`:

```csharp
using Mortar.Core.Layouts;
using Mortar.Core.Model;

namespace Mortar.Tests.Support;

internal static partial class TestContent
{
    public static CellGrid Cells(Tileset tileset, params string[] rows)
    {
        var grid = new CellGrid(rows[0].Length, rows.Length, tileset.VoidIndex);
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                grid[x, y] = tileset.IndexOfSymbol(rows[y][x]);
            }
        }
        return grid;
    }
}
```

- [ ] **Step 2: Write the failing tests**

`tests/Mortar.Tests/Tiles/TileResolverTests.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Core.Tiles;
using Mortar.Tests.Support;

namespace Mortar.Tests.Tiles;

public class TileResolverTests
{
    private static readonly Tileset Tiles = TestContent.CreateTileset();
    private static readonly int Floor = Tiles.FloorIndex;
    private static readonly int Wall = Tiles.WallIndex;
    private static readonly int Water = Tiles.IndexOf("water");

    [Fact]
    public void Name_UniformCorners_UsesGroundId() =>
        Assert.Equal("floor", TileResolver.Name(Tiles, new[] { Floor, Floor, Floor, Floor }));

    [Fact]
    public void Name_WaterOnNorthCorners_IsNorthShoreline() =>
        Assert.Equal("floor-water:1100", TileResolver.Name(Tiles, new[] { Water, Water, Floor, Floor }));

    [Fact]
    public void Name_ThreeGrounds_ListsAllCorners() =>
        Assert.Equal("floor|water|wall|wall", TileResolver.Name(Tiles, new[] { Floor, Water, Wall, Wall }));

    [Fact]
    public void TileTypeAt_TreatsOutsideCellsAsVoid()
    {
        var grid = TestContent.Cells(Tiles, ".");
        Assert.Equal("void-floor:1000", TileResolver.TileTypeAt(grid, Tiles, 0, 0));
    }

    [Fact]
    public void Resolve_BuildsPaletteInFirstAppearanceOrder()
    {
        var resolved = TileResolver.Resolve(TestContent.Cells(Tiles, ".."), Tiles, new Pcg32(1, 1));
        Assert.Equal(new[] { "void-floor:1100", "void-floor:1000" }, resolved.TileTypes);
        Assert.Equal(new[] { 0, 1 }, resolved.Tiles);
        Assert.Equal(new[] { 0, 0 }, resolved.Variants);
    }

    [Fact]
    public void Resolve_PicksVariantsFromWeights()
    {
        var weights = new Dictionary<string, IReadOnlyList<int>> { ["floor"] = new[] { 0, 1 } };
        var tileset = TestContent.CreateTileset(weights);
        var resolved = TileResolver.Resolve(TestContent.Cells(tileset, "...", "...", "..."), tileset, new Pcg32(1, 1));
        Assert.Equal("floor", resolved.TileTypes[resolved.Tiles[0]]);
        Assert.Equal(1, resolved.Variants[0]);
    }
}
```

`tests/Mortar.Tests/Layouts/FingerprintTests.cs`:

```csharp
using Mortar.Core;
using Mortar.Core.Layouts;

namespace Mortar.Tests.Layouts;

public class FingerprintTests
{
    private static LayoutCells Cells(int variant) => new LayoutCells(
        2,
        1,
        new[] { "void", "floor" },
        new[] { 1, 1 },
        new[] { "floor" },
        new[] { 0, 0 },
        new[] { 0, variant },
        new[] { true, true });

    [Fact]
    public void Compute_IsStableAndHex()
    {
        var first = LayoutFingerprint.Compute("crypt", 1, 0, Cells(0));
        Assert.Equal(first, LayoutFingerprint.Compute("crypt", 1, 0, Cells(0)));
        Assert.Matches("^[0-9a-f]{16}$", first);
    }

    [Fact]
    public void Compute_ChangesWithEveryInput()
    {
        var baseline = LayoutFingerprint.Compute("crypt", 1, 0, Cells(0));
        Assert.NotEqual(baseline, LayoutFingerprint.Compute("crypt", 1, 0, Cells(1)));
        Assert.NotEqual(baseline, LayoutFingerprint.Compute("crypt", 2, 0, Cells(0)));
        Assert.NotEqual(baseline, LayoutFingerprint.Compute("crypt", 1, 1, Cells(0)));
        Assert.NotEqual(baseline, LayoutFingerprint.Compute("forest", 1, 0, Cells(0)));
    }

    [Fact]
    public void Layout_ComputesItsOwnFingerprint()
    {
        var layout = new Layout("crypt", 1, 0, Cells(0), Array.Empty<Marker>(), MortarInfo.Version, Layout.CurrentFormatVersion);
        Assert.Equal(LayoutFingerprint.Compute("crypt", 1, 0, Cells(0)), layout.Fingerprint);
        Assert.Equal(2, layout.Width);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test Mortar.slnx`
Expected: build FAILS with `The type or namespace name 'Layouts' does not exist in the namespace 'Mortar.Core'`.

- [ ] **Step 4: Implement the layout types**

`src/Mortar.Core/Layouts/CellGrid.cs`:

```csharp
namespace Mortar.Core.Layouts;

internal sealed class CellGrid
{
    public CellGrid(int width, int height, int fill)
    {
        Width = width;
        Height = height;
        Ground = new int[width * height];
        Array.Fill(Ground, fill);
    }

    public int Width { get; }

    public int Height { get; }

    public int[] Ground { get; }

    public int this[int x, int y]
    {
        get => Ground[y * Width + x];
        set => Ground[y * Width + x] = value;
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
}
```

`src/Mortar.Core/Layouts/Marker.cs`:

```csharp
namespace Mortar.Core.Layouts;

public sealed class Marker
{
    public Marker(string kind, string? nodeId, int x, int y)
    {
        Kind = kind;
        NodeId = nodeId;
        X = x;
        Y = y;
    }

    public string Kind { get; }

    public string? NodeId { get; }

    public int X { get; }

    public int Y { get; }
}
```

`src/Mortar.Core/Layouts/LayoutCells.cs`:

```csharp
namespace Mortar.Core.Layouts;

public sealed class LayoutCells
{
    private readonly int[] _ground;
    private readonly int[] _tiles;
    private readonly int[] _variants;
    private readonly bool[] _walkable;

    public LayoutCells(
        int width,
        int height,
        IReadOnlyList<string> groundTypes,
        int[] ground,
        IReadOnlyList<string> tileTypes,
        int[] tiles,
        int[] variants,
        bool[] walkable)
    {
        Width = width;
        Height = height;
        GroundTypes = groundTypes;
        TileTypes = tileTypes;
        _ground = ground;
        _tiles = tiles;
        _variants = variants;
        _walkable = walkable;
    }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<string> GroundTypes { get; }

    public IReadOnlyList<string> TileTypes { get; }

    public IReadOnlyList<int> Ground => _ground;

    public IReadOnlyList<int> Tiles => _tiles;

    public IReadOnlyList<int> Variants => _variants;

    public IReadOnlyList<bool> Walkable => _walkable;

    public string GroundAt(int x, int y) => GroundTypes[_ground[Index(x, y)]];

    public string TileAt(int x, int y) => TileTypes[_tiles[Index(x, y)]];

    public int VariantAt(int x, int y) => _variants[Index(x, y)];

    public bool IsWalkable(int x, int y) => _walkable[Index(x, y)];

    private int Index(int x, int y) => y * Width + x;
}
```

`src/Mortar.Core/Layouts/LayoutFingerprint.cs`:

```csharp
using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Mortar.Core.Rng;

namespace Mortar.Core.Layouts;

public static class LayoutFingerprint
{
    public static string Compute(string areaId, uint seed, int attempt, LayoutCells cells)
    {
        var hash = Hashing.Fnv1a64(TerminatedUtf8(areaId));
        hash = AddUInt32(hash, seed);
        hash = AddUInt32(hash, (uint)attempt);
        hash = AddUInt32(hash, (uint)cells.Width);
        hash = AddUInt32(hash, (uint)cells.Height);
        var tileBytes = cells.TileTypes.Select(TerminatedUtf8).ToArray();
        for (var i = 0; i < cells.Tiles.Count; i++)
        {
            hash = Hashing.Fnv1a64(tileBytes[cells.Tiles[i]], hash);
            hash = AddUInt32(hash, (uint)cells.Variants[i]);
        }
        return hash.ToString("x16", CultureInfo.InvariantCulture);
    }

    private static byte[] TerminatedUtf8(string text)
    {
        var bytes = new byte[Encoding.UTF8.GetByteCount(text) + 1];
        Encoding.UTF8.GetBytes(text, 0, text.Length, bytes, 0);
        return bytes;
    }

    private static ulong AddUInt32(ulong hash, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        return Hashing.Fnv1a64(bytes, hash);
    }
}
```

`src/Mortar.Core/Layouts/Layout.cs`:

```csharp
namespace Mortar.Core.Layouts;

public sealed class Layout
{
    public const int CurrentFormatVersion = 1;

    public Layout(
        string areaId,
        uint seed,
        int attempt,
        LayoutCells cells,
        IReadOnlyList<Marker> markers,
        string mortarVersion,
        int formatVersion)
    {
        AreaId = areaId;
        Seed = seed;
        Attempt = attempt;
        Cells = cells;
        Markers = markers;
        MortarVersion = mortarVersion;
        FormatVersion = formatVersion;
        Fingerprint = LayoutFingerprint.Compute(areaId, seed, attempt, cells);
    }

    public string AreaId { get; }

    public uint Seed { get; }

    public int Attempt { get; }

    public int Width => Cells.Width;

    public int Height => Cells.Height;

    public LayoutCells Cells { get; }

    public IReadOnlyList<Marker> Markers { get; }

    public string MortarVersion { get; }

    public int FormatVersion { get; }

    public string Fingerprint { get; }
}
```

- [ ] **Step 5: Implement tile resolution**

`src/Mortar.Core/Tiles/ResolvedTiles.cs`:

```csharp
namespace Mortar.Core.Tiles;

internal sealed class ResolvedTiles
{
    public ResolvedTiles(IReadOnlyList<string> tileTypes, int[] tiles, int[] variants)
    {
        TileTypes = tileTypes;
        Tiles = tiles;
        Variants = variants;
    }

    public IReadOnlyList<string> TileTypes { get; }

    public int[] Tiles { get; }

    public int[] Variants { get; }
}
```

`src/Mortar.Core/Tiles/TileResolver.cs`:

```csharp
using Mortar.Core.Layouts;
using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Tiles;

internal static class TileResolver
{
    public static ResolvedTiles Resolve(CellGrid grid, Tileset tileset, Pcg32 rng)
    {
        var palette = new List<string>();
        var paletteIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        var tiles = new int[grid.Width * grid.Height];
        var variants = new int[tiles.Length];
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var tileType = TileTypeAt(grid, tileset, x, y);
                if (!paletteIndex.TryGetValue(tileType, out var index))
                {
                    index = palette.Count;
                    palette.Add(tileType);
                    paletteIndex[tileType] = index;
                }
                var weights = tileset.WeightsFor(tileType);
                tiles[y * grid.Width + x] = index;
                variants[y * grid.Width + x] = weights.Count == 1 ? 0 : rng.PickWeighted(weights);
            }
        }
        return new ResolvedTiles(palette, tiles, variants);
    }

    public static string TileTypeAt(CellGrid grid, Tileset tileset, int x, int y)
    {
        var corners = new[]
        {
            Corner(grid, tileset, x, y),
            Corner(grid, tileset, x + 1, y),
            Corner(grid, tileset, x, y + 1),
            Corner(grid, tileset, x + 1, y + 1),
        };
        return Name(tileset, corners);
    }

    public static string Name(Tileset tileset, int[] corners)
    {
        var distinct = corners.Distinct().OrderBy(index => index).ToArray();
        if (distinct.Length == 1)
        {
            return tileset.Grounds[distinct[0]].Id;
        }
        if (distinct.Length == 2)
        {
            var mask = new string(corners.Select(c => c == distinct[1] ? '1' : '0').ToArray());
            return $"{tileset.Grounds[distinct[0]].Id}-{tileset.Grounds[distinct[1]].Id}:{mask}";
        }
        return string.Join("|", corners.Select(c => tileset.Grounds[c].Id));
    }

    private static int Corner(CellGrid grid, Tileset tileset, int x, int y) =>
        grid.InBounds(x, y) ? grid[x, y] : tileset.VoidIndex;
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test Mortar.slnx`
Expected: PASS (all tests, including 6 tile resolver and 3 fingerprint tests).

- [ ] **Step 7: Commit**

```powershell
git add src/Mortar.Core tests/Mortar.Tests
git commit -m "add layout model, tile resolution, and fingerprint"
```

---

### Task 5: Layout validation

**Files:**
- Create: `src/Mortar.Core/Validation/LayoutValidator.cs`
- Test: `tests/Mortar.Tests/Validation/LayoutValidatorTests.cs`

**Interfaces:**
- Consumes: `CellGrid`, `Marker` (Task 4); `Tileset`, `MarkerKinds` (Task 2); `TargetSize` (Task 3).
- Produces: `internal LayoutValidator.Validate(CellGrid grid, Tileset tileset, IReadOnlyList<Marker> markers, TargetSize target) : string?` — returns `null` on success or a failure reason: `"entrance missing"`, `"{kind} unreachable"`, `"too small"`, `"too large"`. Converts unreached walkable cells to `void` before the size check.

- [ ] **Step 1: Write the failing tests**

`tests/Mortar.Tests/Validation/LayoutValidatorTests.cs`:

```csharp
using Mortar.Core.Layouts;
using Mortar.Core.Model;
using Mortar.Core.Validation;
using Mortar.Tests.Support;

namespace Mortar.Tests.Validation;

public class LayoutValidatorTests
{
    private static readonly Tileset Tiles = TestContent.CreateTileset();
    private static readonly Marker Entrance = new Marker(MarkerKinds.Entrance, "entrance", 1, 1);
    private static readonly Marker Chest = new Marker(MarkerKinds.Chest, null, 3, 1);

    [Fact]
    public void Validate_EverythingReachable_Succeeds()
    {
        var grid = TestContent.Cells(Tiles, "#####", "#...#", "#####");
        Assert.Null(LayoutValidator.Validate(grid, Tiles, new[] { Entrance, Chest }, new TargetSize(3, 1, 0)));
    }

    [Fact]
    public void Validate_MarkerBehindWall_IsUnreachable()
    {
        var grid = TestContent.Cells(Tiles, "#####", "#.#.#", "#####");
        Assert.Equal("chest unreachable", LayoutValidator.Validate(grid, Tiles, new[] { Entrance, Chest }, new TargetSize(3, 1, 0)));
    }

    [Fact]
    public void Validate_UnreachedPocket_BecomesVoid()
    {
        var grid = TestContent.Cells(Tiles, "######", "#..#.#", "######");
        Assert.Null(LayoutValidator.Validate(grid, Tiles, new[] { Entrance }, new TargetSize(2, 1, 0)));
        Assert.Equal(Tiles.VoidIndex, grid[4, 1]);
        Assert.Equal(Tiles.FloorIndex, grid[2, 1]);
    }

    [Fact]
    public void Validate_BelowTolerance_IsTooSmall()
    {
        var grid = TestContent.Cells(Tiles, "#####", "#...#", "#####");
        Assert.Equal("too small", LayoutValidator.Validate(grid, Tiles, new[] { Entrance }, new TargetSize(10, 10, 10)));
    }

    [Fact]
    public void Validate_AboveTolerance_IsTooLarge()
    {
        var grid = TestContent.Cells(Tiles, "#####", "#...#", "#####");
        Assert.Equal("too large", LayoutValidator.Validate(grid, Tiles, new[] { Entrance }, new TargetSize(1, 1, 0)));
    }

    [Fact]
    public void Validate_NoEntranceMarker_Fails()
    {
        var grid = TestContent.Cells(Tiles, "#####", "#...#", "#####");
        Assert.Equal("entrance missing", LayoutValidator.Validate(grid, Tiles, new[] { Chest }, new TargetSize(3, 1, 0)));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Mortar.slnx`
Expected: build FAILS with `The type or namespace name 'Validation' does not exist in the namespace 'Mortar.Core'`.

- [ ] **Step 3: Implement the validator**

`src/Mortar.Core/Validation/LayoutValidator.cs`:

```csharp
using Mortar.Core.Layouts;
using Mortar.Core.Model;

namespace Mortar.Core.Validation;

internal static class LayoutValidator
{
    public static string? Validate(CellGrid grid, Tileset tileset, IReadOnlyList<Marker> markers, TargetSize target)
    {
        var entrance = markers.FirstOrDefault(m => m.Kind == MarkerKinds.Entrance);
        if (entrance is null)
        {
            return "entrance missing";
        }
        var reached = FloodFill(grid, tileset, entrance.X, entrance.Y);
        var unreachable = markers.FirstOrDefault(m => !reached[m.Y * grid.Width + m.X]);
        if (unreachable is not null)
        {
            return $"{unreachable.Kind} unreachable";
        }
        VoidUnreached(grid, tileset, reached);
        return CheckSize(grid, tileset, target);
    }

    private static bool[] FloodFill(CellGrid grid, Tileset tileset, int startX, int startY)
    {
        var reached = new bool[grid.Width * grid.Height];
        if (!tileset.IsWalkable(grid[startX, startY]))
        {
            return reached;
        }
        var queue = new Queue<(int X, int Y)>();
        reached[startY * grid.Width + startX] = true;
        queue.Enqueue((startX, startY));
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (nx, ny) in new[] { (x, y - 1), (x + 1, y), (x, y + 1), (x - 1, y) })
            {
                if (!grid.InBounds(nx, ny) || reached[ny * grid.Width + nx] || !tileset.IsWalkable(grid[nx, ny]))
                {
                    continue;
                }
                reached[ny * grid.Width + nx] = true;
                queue.Enqueue((nx, ny));
            }
        }
        return reached;
    }

    private static void VoidUnreached(CellGrid grid, Tileset tileset, bool[] reached)
    {
        for (var i = 0; i < grid.Ground.Length; i++)
        {
            if (!reached[i] && tileset.IsWalkable(grid.Ground[i]))
            {
                grid.Ground[i] = tileset.VoidIndex;
            }
        }
    }

    private static string? CheckSize(CellGrid grid, Tileset tileset, TargetSize target)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                if (!tileset.IsWalkable(grid[x, y])) continue;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }
        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        if (maxX < 0 || width < target.MinWidth || height < target.MinHeight) return "too small";
        if (width > target.MaxWidth || height > target.MaxHeight) return "too large";
        return null;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Mortar.slnx`
Expected: PASS (all tests, including 6 validator tests).

- [ ] **Step 5: Commit**

```powershell
git add src/Mortar.Core tests/Mortar.Tests
git commit -m "add layout walkability and size validation"
```

---

### Task 6: Slot grid, landmark placement, and path routing

**Files:**
- Create: `src/Mortar.Core/Indoor/Sides.cs`, `Placement.cs`, `SlotGrid.cs`, `SlotSearch.cs`, `SlotPathfinder.cs`, `LandmarkPlacer.cs`, `IndoorRouter.cs`
- Test: `tests/Mortar.Tests/Indoor/SlotGridTests.cs`, `LandmarkPlacerTests.cs`, `SlotPathfinderTests.cs`, `IndoorRouterTests.cs`

**Interfaces:**
- Consumes: `Room`, `Side`, `CompiledArea`, `AreaDefinition`, `GraphNode`, `EdgeKind` (Tasks 2–3); `Pcg32` (Task 1).
- Produces (all `internal`, namespace `Mortar.Core.Indoor`):
  - `Sides.All : IReadOnlyList<Side>` (N, E, S, W), `Sides.Offset(Side) : (int Dx, int Dy)`, `Sides.Opposite(Side) : Side`, `Sides.Toward(int dx, int dy) : Side`.
  - `Placement(Room room, int slotX, int slotY, string? nodeId)`: `Room`, `SlotX`, `SlotY`, `NodeId`, `Contains(int x, int y) : bool`.
  - `SlotGrid(int columns, int rows, int stride)`, `SlotGrid.ForArea(AreaDefinition)`: `Columns`, `Rows`, `Stride`, `CellWidth`, `CellHeight`, `Placements`, `Index(x, y)`, `Coords(int) : (int X, int Y)`, `InBounds`, `BlockAt(x, y) : int` (-1 when free), `IsUsed`, `MarkUsed`, `BlockOfNode(string) : int`, `CanPlace(Room, x, y)`, `Place(Placement)`, `Open(x, y, Side)`, `IsOpen(x, y, Side)`, `ConnectPath(IReadOnlyList<int> slots)`.
  - `SlotPathfinder(SlotGrid, Pcg32)`, `FindPath(int fromBlock, int toBlock) : IReadOnlyList<int>?`.
  - `LandmarkPlacer.PlaceAll(CompiledArea, SlotGrid, Pcg32) : string?` (failure `"landmark placement failed"`), `LandmarkPlacer.PickSlot(double from, double to, int count, Pcg32) : int`.
  - `IndoorRouter.Route(CompiledArea, SlotGrid, Pcg32) : string?` (failure `"no path: {from}->{to}"`).

- [ ] **Step 1: Write the failing tests**

`tests/Mortar.Tests/Indoor/SlotGridTests.cs`:

```csharp
using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class SlotGridTests
{
    private static readonly Room Cross = TestContent.CompileRoom("cross", new[] { "filler" }, TestContent.Box(9, 9, new[] { 4 }));
    private static readonly Room Boss = TestContent.CompileRoom("boss", new[] { "boss" }, TestContent.Box(17, 17, new[] { 4, 12 }, (8, 8, 'B')));

    [Fact]
    public void ForArea_DerivesColumnsFromTargetSize()
    {
        var grid = SlotGrid.ForArea(TestContent.CryptDefinition());
        Assert.Equal((7, 7, 8), (grid.Columns, grid.Rows, grid.Stride));
        Assert.Equal(57, grid.CellWidth);
    }

    [Fact]
    public void Open_IsVisibleFromBothSides()
    {
        var grid = new SlotGrid(3, 3, 8);
        grid.Open(1, 1, Side.East);
        Assert.True(grid.IsOpen(2, 1, Side.West));
        Assert.True(grid.IsOpen(1, 1, Side.East));
        Assert.False(grid.IsOpen(1, 1, Side.South));
    }

    [Fact]
    public void IsOpen_IsFalseAtGridEdge() => Assert.False(new SlotGrid(3, 3, 8).IsOpen(0, 0, Side.West));

    [Fact]
    public void CanPlace_RejectsOverlapAndOutOfBounds()
    {
        var grid = new SlotGrid(3, 3, 8);
        Assert.False(grid.CanPlace(Boss, 2, 0));
        grid.Place(new Placement(Boss, 0, 0, "boss"));
        Assert.False(grid.CanPlace(Boss, 1, 1));
        Assert.True(grid.CanPlace(Cross, 2, 2));
        Assert.Equal(0, grid.BlockAt(1, 1));
        Assert.Equal(0, grid.BlockOfNode("boss"));
    }

    [Fact]
    public void ConnectPath_OpensBetweenSlotsButNotInsideBlocks()
    {
        var grid = new SlotGrid(4, 2, 8);
        grid.Place(new Placement(Boss, 0, 0, "boss"));
        grid.ConnectPath(new[] { grid.Index(0, 0), grid.Index(1, 0), grid.Index(2, 0), grid.Index(3, 0) });
        Assert.False(grid.IsOpen(0, 0, Side.East));
        Assert.True(grid.IsOpen(1, 0, Side.East));
        Assert.True(grid.IsOpen(2, 0, Side.East));
        Assert.True(grid.IsUsed(3, 0));
    }
}
```

`tests/Mortar.Tests/Indoor/LandmarkPlacerTests.cs`:

```csharp
using Mortar.Core.Indoor;
using Mortar.Core.Rng;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class LandmarkPlacerTests
{
    [Theory]
    [InlineData(0.0, 0.1, 7, 0)]
    [InlineData(0.9, 1.0, 7, 6)]
    [InlineData(0.0, 1.0, 1, 0)]
    public void PickSlot_StaysInsideRegion(double from, double to, int count, int expected)
    {
        var rng = new Pcg32(1, 1);
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(expected, LandmarkPlacer.PickSlot(from, to, count, rng));
        }
    }

    [Fact]
    public void PlaceAll_PutsEachLandmarkInItsRegion()
    {
        var area = TestContent.CompileCrypt();
        var grid = SlotGrid.ForArea(area.Definition);
        Assert.Null(LandmarkPlacer.PlaceAll(area, grid, Pcg32.ForAttempt("crypt", 1, 0)));
        Assert.Equal(4, grid.Placements.Count);
        Assert.Equal(0, grid.Placements[grid.BlockOfNode("entrance")].SlotX);
        Assert.Equal(0, grid.Placements[grid.BlockOfNode("waypoint")].SlotY);
        Assert.Equal(5, grid.Placements[grid.BlockOfNode("boss")].SlotX);
        Assert.Equal(6, grid.Placements[grid.BlockOfNode("exit")].SlotY);
    }
}
```

`tests/Mortar.Tests/Indoor/SlotPathfinderTests.cs`:

```csharp
using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class SlotPathfinderTests
{
    private static readonly Room Cross = TestContent.CompileRoom("cross", new[] { "filler" }, TestContent.Box(9, 9, new[] { 4 }));
    private static readonly Room NorthOnly = TestContent.CompileRoom("north", new[] { "filler" }, TestContent.Box(9, 9, Array.Empty<int>(), (4, 0, 'D')));

    [Fact]
    public void FindPath_ConnectsBlocksWithAdjacentSteps()
    {
        var grid = new SlotGrid(5, 1, 8);
        grid.Place(new Placement(Cross, 0, 0, "a"));
        grid.Place(new Placement(Cross, 4, 0, "b"));
        var path = new SlotPathfinder(grid, new Pcg32(1, 1)).FindPath(0, 1);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, path);
    }

    [Fact]
    public void FindPath_DoesNotCrossOtherLandmarks()
    {
        var grid = new SlotGrid(3, 1, 8);
        grid.Place(new Placement(Cross, 0, 0, "a"));
        grid.Place(new Placement(Cross, 1, 0, "middle"));
        grid.Place(new Placement(Cross, 2, 0, "b"));
        Assert.Null(new SlotPathfinder(grid, new Pcg32(1, 1)).FindPath(0, 2));
    }

    [Fact]
    public void FindPath_LeavesBlocksOnlyThroughDoors()
    {
        var grid = new SlotGrid(2, 2, 8);
        grid.Place(new Placement(NorthOnly, 0, 1, "a"));
        grid.Place(new Placement(Cross, 1, 1, "b"));
        var path = new SlotPathfinder(grid, new Pcg32(1, 1)).FindPath(0, 1);
        Assert.Equal(new[] { grid.Index(0, 1), grid.Index(0, 0), grid.Index(1, 0), grid.Index(1, 1) }, path);
    }
}
```

`tests/Mortar.Tests/Indoor/IndoorRouterTests.cs`:

```csharp
using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class IndoorRouterTests
{
    [Fact]
    public void Route_OpensEveryLandmarkToTheLevel()
    {
        var area = TestContent.CompileCrypt();
        var grid = SlotGrid.ForArea(area.Definition);
        var rng = Pcg32.ForAttempt("crypt", 1, 0);
        Assert.Null(LandmarkPlacer.PlaceAll(area, grid, rng));
        Assert.Null(IndoorRouter.Route(area, grid, rng));
        Assert.All(grid.Placements, placement => Assert.True(HasOpening(grid, placement), placement.NodeId));
    }

    private static bool HasOpening(SlotGrid grid, Placement placement)
    {
        for (var y = placement.SlotY; y < placement.SlotY + placement.Room.SlotsHigh; y++)
        {
            for (var x = placement.SlotX; x < placement.SlotX + placement.Room.SlotsWide; x++)
            {
                foreach (var side in Sides.All)
                {
                    var (dx, dy) = Sides.Offset(side);
                    if (!placement.Contains(x + dx, y + dy) && grid.IsOpen(x, y, side))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Mortar.slnx`
Expected: build FAILS with `The type or namespace name 'Indoor' does not exist in the namespace 'Mortar.Core'`.

- [ ] **Step 3: Implement sides, placements, and the slot grid**

`src/Mortar.Core/Indoor/Sides.cs`:

```csharp
using Mortar.Core.Model;

namespace Mortar.Core.Indoor;

internal static class Sides
{
    public static readonly IReadOnlyList<Side> All = new[] { Side.North, Side.East, Side.South, Side.West };

    public static (int Dx, int Dy) Offset(Side side) => side switch
    {
        Side.North => (0, -1),
        Side.East => (1, 0),
        Side.South => (0, 1),
        _ => (-1, 0),
    };

    public static Side Opposite(Side side) => side switch
    {
        Side.North => Side.South,
        Side.East => Side.West,
        Side.South => Side.North,
        _ => Side.East,
    };

    public static Side Toward(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => Side.North,
        (1, 0) => Side.East,
        (0, 1) => Side.South,
        (-1, 0) => Side.West,
        _ => throw new ArgumentException("slots are not adjacent"),
    };
}
```

`src/Mortar.Core/Indoor/Placement.cs`:

```csharp
using Mortar.Core.Model;

namespace Mortar.Core.Indoor;

internal sealed class Placement
{
    public Placement(Room room, int slotX, int slotY, string? nodeId)
    {
        Room = room;
        SlotX = slotX;
        SlotY = slotY;
        NodeId = nodeId;
    }

    public Room Room { get; }

    public int SlotX { get; }

    public int SlotY { get; }

    public string? NodeId { get; }

    public bool Contains(int x, int y) =>
        x >= SlotX && x < SlotX + Room.SlotsWide && y >= SlotY && y < SlotY + Room.SlotsHigh;
}
```

`src/Mortar.Core/Indoor/SlotGrid.cs`:

```csharp
using Mortar.Core.Model;

namespace Mortar.Core.Indoor;

internal sealed class SlotGrid
{
    private readonly int[] _blockOf;
    private readonly bool[] _used;
    private readonly bool[] _openEast;
    private readonly bool[] _openSouth;
    private readonly List<Placement> _placements = new List<Placement>();

    public SlotGrid(int columns, int rows, int stride)
    {
        Columns = columns;
        Rows = rows;
        Stride = stride;
        _blockOf = new int[columns * rows];
        Array.Fill(_blockOf, -1);
        _used = new bool[columns * rows];
        _openEast = new bool[columns * rows];
        _openSouth = new bool[columns * rows];
    }

    public static SlotGrid ForArea(AreaDefinition area)
    {
        var stride = area.SlotSize - 1;
        var columns = Math.Max(1, (area.TargetSize.Width - 1) / stride);
        var rows = Math.Max(1, (area.TargetSize.Height - 1) / stride);
        return new SlotGrid(columns, rows, stride);
    }

    public int Columns { get; }

    public int Rows { get; }

    public int Stride { get; }

    public int CellWidth => Columns * Stride + 1;

    public int CellHeight => Rows * Stride + 1;

    public IReadOnlyList<Placement> Placements => _placements;

    public int Index(int x, int y) => y * Columns + x;

    public (int X, int Y) Coords(int index) => (index % Columns, index / Columns);

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Columns && y < Rows;

    public int BlockAt(int x, int y) => _blockOf[Index(x, y)];

    public bool IsUsed(int x, int y) => _used[Index(x, y)];

    public void MarkUsed(int x, int y) => _used[Index(x, y)] = true;

    public int BlockOfNode(string nodeId) => _placements.FindIndex(p => p.NodeId == nodeId);

    public bool CanPlace(Room room, int originX, int originY)
    {
        for (var y = originY; y < originY + room.SlotsHigh; y++)
        {
            for (var x = originX; x < originX + room.SlotsWide; x++)
            {
                if (!InBounds(x, y) || BlockAt(x, y) != -1) return false;
            }
        }
        return true;
    }

    public void Place(Placement placement)
    {
        var block = _placements.Count;
        _placements.Add(placement);
        for (var y = placement.SlotY; y < placement.SlotY + placement.Room.SlotsHigh; y++)
        {
            for (var x = placement.SlotX; x < placement.SlotX + placement.Room.SlotsWide; x++)
            {
                _blockOf[Index(x, y)] = block;
                _used[Index(x, y)] = true;
            }
        }
    }

    public void Open(int x, int y, Side side)
    {
        switch (side)
        {
            case Side.East: _openEast[Index(x, y)] = true; break;
            case Side.West: _openEast[Index(x - 1, y)] = true; break;
            case Side.South: _openSouth[Index(x, y)] = true; break;
            default: _openSouth[Index(x, y - 1)] = true; break;
        }
    }

    public bool IsOpen(int x, int y, Side side)
    {
        var (dx, dy) = Sides.Offset(side);
        if (!InBounds(x, y) || !InBounds(x + dx, y + dy)) return false;
        return side switch
        {
            Side.East => _openEast[Index(x, y)],
            Side.West => _openEast[Index(x - 1, y)],
            Side.South => _openSouth[Index(x, y)],
            _ => _openSouth[Index(x, y - 1)],
        };
    }

    public void ConnectPath(IReadOnlyList<int> slots)
    {
        for (var i = 0; i < slots.Count; i++)
        {
            var (x, y) = Coords(slots[i]);
            MarkUsed(x, y);
            if (i == 0) continue;
            var (px, py) = Coords(slots[i - 1]);
            var block = BlockAt(x, y);
            if (block != -1 && block == BlockAt(px, py)) continue;
            Open(px, py, Sides.Toward(x - px, y - py));
        }
    }
}
```

- [ ] **Step 4: Implement the pathfinder**

`src/Mortar.Core/Indoor/SlotSearch.cs`:

```csharp
namespace Mortar.Core.Indoor;

internal sealed class SlotSearch
{
    private readonly int[] _cost;
    private readonly int[] _previous;
    private readonly SortedSet<(int Cost, int Slot)> _open = new SortedSet<(int Cost, int Slot)>();

    public SlotSearch(int slotCount)
    {
        _cost = new int[slotCount];
        Array.Fill(_cost, int.MaxValue);
        _previous = new int[slotCount];
        Array.Fill(_previous, -1);
    }

    public int CostOf(int slot) => _cost[slot];

    public void Push(int slot, int cost, int previous)
    {
        if (cost >= _cost[slot]) return;
        if (_cost[slot] != int.MaxValue) _open.Remove((_cost[slot], slot));
        _cost[slot] = cost;
        _previous[slot] = previous;
        _open.Add((cost, slot));
    }

    public bool TryPop(out int slot)
    {
        if (_open.Count == 0)
        {
            slot = -1;
            return false;
        }
        var next = _open.Min;
        _open.Remove(next);
        slot = next.Slot;
        return true;
    }

    public List<int> PathTo(int slot)
    {
        var path = new List<int>();
        for (var current = slot; current != -1; current = _previous[current])
        {
            path.Add(current);
        }
        path.Reverse();
        return path;
    }
}
```

`src/Mortar.Core/Indoor/SlotPathfinder.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Indoor;

internal sealed class SlotPathfinder
{
    private const int MaxRandomWeight = 4;
    private readonly SlotGrid _grid;
    private readonly int[] _weights;

    public SlotPathfinder(SlotGrid grid, Pcg32 rng)
    {
        _grid = grid;
        _weights = new int[grid.Columns * grid.Rows];
        for (var i = 0; i < _weights.Length; i++)
        {
            _weights[i] = rng.NextInt(MaxRandomWeight);
        }
    }

    public IReadOnlyList<int>? FindPath(int fromBlock, int toBlock)
    {
        var search = new SlotSearch(_weights.Length);
        foreach (var slot in SlotsOf(fromBlock))
        {
            search.Push(slot, 0, -1);
        }
        while (search.TryPop(out var slot))
        {
            var (x, y) = _grid.Coords(slot);
            if (_grid.BlockAt(x, y) == toBlock)
            {
                return search.PathTo(slot);
            }
            foreach (var side in Sides.All)
            {
                Relax(search, slot, side, fromBlock, toBlock);
            }
        }
        return null;
    }

    private void Relax(SlotSearch search, int slot, Side side, int fromBlock, int toBlock)
    {
        var (x, y) = _grid.Coords(slot);
        var (dx, dy) = Sides.Offset(side);
        var nx = x + dx;
        var ny = y + dy;
        if (!_grid.InBounds(nx, ny) || !CanCross(x, y, nx, ny, side, fromBlock, toBlock))
        {
            return;
        }
        var next = _grid.Index(nx, ny);
        var step = _grid.IsUsed(nx, ny) ? 1 : 1 + _weights[next];
        search.Push(next, search.CostOf(slot) + step, slot);
    }

    private bool CanCross(int x, int y, int nx, int ny, Side side, int fromBlock, int toBlock)
    {
        var from = _grid.BlockAt(x, y);
        var to = _grid.BlockAt(nx, ny);
        if (from == to) return true;
        if (to != -1 && to != fromBlock && to != toBlock) return false;
        if (from != -1 && !HasDoorToward(from, x, y, side)) return false;
        return to == -1 || HasDoorToward(to, nx, ny, Sides.Opposite(side));
    }

    private bool HasDoorToward(int block, int x, int y, Side side)
    {
        var placement = _grid.Placements[block];
        var segment = side == Side.North || side == Side.South ? x - placement.SlotX : y - placement.SlotY;
        return placement.Room.HasDoorInSegment(side, segment);
    }

    private IEnumerable<int> SlotsOf(int block)
    {
        var placement = _grid.Placements[block];
        for (var y = placement.SlotY; y < placement.SlotY + placement.Room.SlotsHigh; y++)
        {
            for (var x = placement.SlotX; x < placement.SlotX + placement.Room.SlotsWide; x++)
            {
                yield return _grid.Index(x, y);
            }
        }
    }
}
```

- [ ] **Step 5: Implement landmark placement and routing**

`src/Mortar.Core/Indoor/LandmarkPlacer.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Indoor;

internal static class LandmarkPlacer
{
    public static string? PlaceAll(CompiledArea area, SlotGrid grid, Pcg32 rng)
    {
        foreach (var node in area.Definition.Nodes)
        {
            if (!Place(node, area, grid, rng))
            {
                return "landmark placement failed";
            }
        }
        return null;
    }

    public static int PickSlot(double from, double to, int count, Pcg32 rng)
    {
        var low = Math.Clamp((int)Math.Floor(from * count), 0, count - 1);
        var high = Math.Clamp((int)Math.Ceiling(to * count) - 1, low, count - 1);
        return rng.NextInt(low, high + 1);
    }

    private static bool Place(GraphNode node, CompiledArea area, SlotGrid grid, Pcg32 rng)
    {
        var slotX = PickSlot(node.Region.X0, node.Region.X1, grid.Columns, rng);
        var slotY = PickSlot(node.Region.Y0, node.Region.Y1, grid.Rows, rng);
        var room = rng.Pick(area.RoomsTagged(node.Kind));
        var origins = new List<(int X, int Y)>();
        for (var y = slotY - room.SlotsHigh + 1; y <= slotY; y++)
        {
            for (var x = slotX - room.SlotsWide + 1; x <= slotX; x++)
            {
                if (grid.CanPlace(room, x, y)) origins.Add((x, y));
            }
        }
        if (origins.Count == 0)
        {
            return false;
        }
        var origin = rng.Pick(origins);
        grid.Place(new Placement(room, origin.X, origin.Y, node.Id));
        return true;
    }
}
```

`src/Mortar.Core/Indoor/IndoorRouter.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Indoor;

internal static class IndoorRouter
{
    public static string? Route(CompiledArea area, SlotGrid grid, Pcg32 rng)
    {
        var finder = new SlotPathfinder(grid, rng);
        foreach (var edge in area.Definition.Edges.Where(e => e.Kind == EdgeKind.Path))
        {
            var path = finder.FindPath(grid.BlockOfNode(edge.From), grid.BlockOfNode(edge.To));
            if (path is null)
            {
                return $"no path: {edge.From}->{edge.To}";
            }
            grid.ConnectPath(path);
        }
        foreach (var edge in area.Definition.Edges.Where(e => e.Kind == EdgeKind.Branch))
        {
            if (rng.NextInt(2) == 0) continue;
            var path = finder.FindPath(grid.BlockOfNode(edge.From), grid.BlockOfNode(edge.To));
            if (path is not null) grid.ConnectPath(path);
        }
        AddSideBranches(area.Definition.MaxSideBranches, grid, rng);
        return null;
    }

    private static void AddSideBranches(int maxSideBranches, SlotGrid grid, Pcg32 rng)
    {
        var count = rng.NextInt(maxSideBranches + 1);
        for (var i = 0; i < count; i++)
        {
            var options = SideBranchOptions(grid);
            if (options.Count == 0) return;
            var (x, y, side) = rng.Pick(options);
            var (dx, dy) = Sides.Offset(side);
            grid.Open(x, y, side);
            grid.MarkUsed(x + dx, y + dy);
        }
    }

    private static List<(int X, int Y, Side Side)> SideBranchOptions(SlotGrid grid)
    {
        var options = new List<(int X, int Y, Side Side)>();
        for (var y = 0; y < grid.Rows; y++)
        {
            for (var x = 0; x < grid.Columns; x++)
            {
                if (!grid.IsUsed(x, y) || grid.BlockAt(x, y) != -1) continue;
                foreach (var side in Sides.All)
                {
                    var (dx, dy) = Sides.Offset(side);
                    if (grid.InBounds(x + dx, y + dy) && !grid.IsUsed(x + dx, y + dy))
                    {
                        options.Add((x, y, side));
                    }
                }
            }
        }
        return options;
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test Mortar.slnx`
Expected: PASS (all tests, including 5 slot grid, 4 landmark, 3 pathfinder, and 1 router test).

- [ ] **Step 7: Commit**

```powershell
git add src/Mortar.Core tests/Mortar.Tests
git commit -m "add slot grid, landmark placement, and path routing"
```

---

### Task 7: Room filling, stitching, and the indoor builder

**Files:**
- Create: `src/Mortar.Core/Indoor/DoorGeometry.cs`, `RoomFiller.cs`, `StitchCanvas.cs`, `RoomStitcher.cs`, `MarkerCollector.cs`, `BuildOutcome.cs`, `IndoorBuilder.cs`
- Test: `tests/Mortar.Tests/Indoor/RoomFillerTests.cs`, `RoomStitcherTests.cs`, `MarkerCollectorTests.cs`, `IndoorBuilderTests.cs`

**Interfaces:**
- Consumes: `SlotGrid`, `Placement`, `Sides`, `LandmarkPlacer`, `IndoorRouter` (Task 6); `CellGrid`, `Marker` (Task 4); `CompiledArea`, `StandardTags`, `GraphNode` (Task 3); `Pcg32` (Task 1).
- Produces (all `internal`, namespace `Mortar.Core.Indoor`):
  - `DoorGeometry.WorldDoors(Placement, int stride) : IEnumerable<(int X, int Y)>`, `DoorGeometry.DoorsMeet(Placement a, Placement b, int stride) : bool`.
  - `RoomFiller.FillAll(CompiledArea, SlotGrid, Pcg32) : string?` (failure `"no room fits"`), `RoomFiller.Fits(SlotGrid, Room, int x, int y, IReadOnlyList<Side> open) : bool`.
  - `RoomStitcher.Stitch(SlotGrid, Tileset) : CellGrid`.
  - `MarkerCollector.Collect(CompiledArea, SlotGrid) : List<Marker>`.
  - `BuildOutcome`: `Grid : CellGrid?`, `Markers : IReadOnlyList<Marker>`, `Failure : string?`, static `Built(CellGrid, IReadOnlyList<Marker>)`, `Failed(string)`.
  - `IndoorBuilder.Build(CompiledArea, Pcg32) : BuildOutcome`.

- [ ] **Step 1: Write the failing tests**

`tests/Mortar.Tests/Indoor/RoomFillerTests.cs`:

```csharp
using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class RoomFillerTests
{
    private static readonly Room Cross = TestContent.CompileRoom("cross", new[] { "filler" }, TestContent.Box(9, 9, new[] { 4 }));
    private static readonly Room NorthOnly = TestContent.CompileRoom("north", new[] { "filler" }, TestContent.Box(9, 9, Array.Empty<int>(), (4, 0, 'D')));
    private static readonly Room OffCenter = TestContent.CompileRoom("off", new[] { "filler" }, TestContent.Box(9, 9, Array.Empty<int>(), (0, 2, 'D'), (8, 2, 'D')));

    [Fact]
    public void Fits_CrossRoomCoversAnyOpenings() =>
        Assert.True(RoomFiller.Fits(new SlotGrid(3, 3, 8), Cross, 1, 1, Sides.All));

    [Fact]
    public void Fits_RejectsRoomWithoutDoorOnOpenSide() =>
        Assert.False(RoomFiller.Fits(new SlotGrid(3, 3, 8), NorthOnly, 1, 1, new[] { Side.East }));

    [Fact]
    public void Fits_RequiresDoorsToLineUpWithPlacedNeighbors()
    {
        var grid = new SlotGrid(2, 1, 8);
        grid.Place(new Placement(OffCenter, 1, 0, null));
        Assert.False(RoomFiller.Fits(grid, Cross, 0, 0, new[] { Side.East }));
        Assert.True(RoomFiller.Fits(grid, OffCenter, 0, 0, new[] { Side.East }));
    }

    [Fact]
    public void FillAll_FillsEveryUsedSlot()
    {
        var area = TestContent.CompileCrypt();
        var grid = SlotGrid.ForArea(area.Definition);
        var rng = Pcg32.ForAttempt("crypt", 1, 0);
        Assert.Null(LandmarkPlacer.PlaceAll(area, grid, rng));
        Assert.Null(IndoorRouter.Route(area, grid, rng));
        Assert.Null(RoomFiller.FillAll(area, grid, rng));
        for (var y = 0; y < grid.Rows; y++)
        {
            for (var x = 0; x < grid.Columns; x++)
            {
                Assert.True(!grid.IsUsed(x, y) || grid.BlockAt(x, y) != -1, $"slot ({x}, {y}) is used but empty");
            }
        }
    }

    [Fact]
    public void FillAll_UsesDeadEndsForSingleOpenings()
    {
        var area = TestContent.CompileCrypt();
        var grid = new SlotGrid(2, 1, 8);
        grid.Place(new Placement(area.RoomsTagged("entrance")[0], 0, 0, "entrance"));
        grid.MarkUsed(1, 0);
        grid.Open(0, 0, Side.East);
        Assert.Null(RoomFiller.FillAll(area, grid, new Pcg32(1, 1)));
        Assert.Equal("alcove", grid.Placements[1].Room.Id);
    }
}
```

`tests/Mortar.Tests/Indoor/RoomStitcherTests.cs`:

```csharp
using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class RoomStitcherTests
{
    private static readonly Tileset Tiles = TestContent.CreateTileset();
    private static readonly Room Cross = TestContent.CompileRoom("cross", new[] { "filler" }, TestContent.Box(9, 9, new[] { 4 }));

    private static SlotGrid TwoCrosses()
    {
        var grid = new SlotGrid(2, 1, 8);
        grid.Place(new Placement(Cross, 0, 0, null));
        grid.Place(new Placement(Cross, 1, 0, null));
        return grid;
    }

    [Fact]
    public void Stitch_OpensSharedDoorWhenConnected()
    {
        var grid = TwoCrosses();
        grid.Open(0, 0, Side.East);
        var cells = RoomStitcher.Stitch(grid, Tiles);
        Assert.Equal((17, 9), (cells.Width, cells.Height));
        Assert.Equal(Tiles.FloorIndex, cells[8, 4]);
        Assert.Equal(Tiles.WallIndex, cells[0, 4]);
        Assert.Equal(Tiles.WallIndex, cells[4, 0]);
        Assert.Equal(Tiles.FloorIndex, cells[4, 4]);
    }

    [Fact]
    public void Stitch_KeepsSharedDoorClosedWithoutOpening()
    {
        var cells = RoomStitcher.Stitch(TwoCrosses(), Tiles);
        Assert.Equal(Tiles.WallIndex, cells[8, 4]);
    }

    [Fact]
    public void Stitch_LeavesUnusedSlotsVoid()
    {
        var grid = new SlotGrid(2, 1, 8);
        grid.Place(new Placement(Cross, 0, 0, null));
        Assert.Equal(Tiles.VoidIndex, RoomStitcher.Stitch(grid, Tiles)[12, 4]);
    }
}
```

`tests/Mortar.Tests/Indoor/MarkerCollectorTests.cs`:

```csharp
using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class MarkerCollectorTests
{
    [Fact]
    public void Collect_TagsLandmarkMarkersWithNodeId()
    {
        var area = TestContent.CompileCrypt();
        var grid = SlotGrid.ForArea(area.Definition);
        grid.Place(new Placement(area.RoomsTagged("entrance")[0], 0, 3, "entrance"));
        grid.Place(new Placement(area.RoomsTagged("filler")[0], 1, 3, null));
        var markers = MarkerCollector.Collect(area, grid);
        Assert.Equal(2, markers.Count);
        Assert.Equal(("entrance", (string?)"entrance", 4, 28), (markers[0].Kind, markers[0].NodeId, markers[0].X, markers[0].Y));
        Assert.Equal(("spawn", (string?)null, 12, 28), (markers[1].Kind, markers[1].NodeId, markers[1].X, markers[1].Y));
    }

    [Fact]
    public void Collect_UsesRoomCenterForCustomLandmarks()
    {
        var nodes = TestContent.CryptNodes().Append(new GraphNode("shrine", "shrine", new FractionRect(0.4, 0.6, 0.4, 0.6))).ToArray();
        var edges = TestContent.CryptEdges().Append(new GraphEdge("entrance", "shrine", EdgeKind.Path)).ToArray();
        var rooms = TestContent.CryptRooms();
        rooms.Add(TestContent.RoomDef("shrine_room", new[] { "shrine" }, TestContent.Box(9, 9, new[] { 4 })));
        var area = TestContent.CompileCrypt(TestContent.CryptDefinition(nodes: nodes, edges: edges), rooms);
        var grid = SlotGrid.ForArea(area.Definition);
        grid.Place(new Placement(area.RoomsTagged("shrine")[0], 2, 2, "shrine"));
        var marker = Assert.Single(MarkerCollector.Collect(area, grid));
        Assert.Equal(("shrine", (string?)"shrine", 20, 20), (marker.Kind, marker.NodeId, marker.X, marker.Y));
    }
}
```

`tests/Mortar.Tests/Indoor/IndoorBuilderTests.cs`:

```csharp
using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class IndoorBuilderTests
{
    [Fact]
    public void Build_SucceedsForMostSeeds()
    {
        var area = TestContent.CompileCrypt();
        var outcomes = Enumerable.Range(0, 20)
            .Select(seed => IndoorBuilder.Build(area, Pcg32.ForAttempt("crypt", (uint)seed, 0)))
            .ToList();
        Assert.True(outcomes.Count(o => o.Failure is null) >= 16);
        Assert.All(outcomes.Where(o => o.Failure is null), outcome =>
        {
            Assert.Equal(57, outcome.Grid!.Width);
            Assert.Equal(MarkerKinds.Entrance, outcome.Markers[0].Kind);
        });
    }

    [Fact]
    public void Build_IsDeterministic()
    {
        var area = TestContent.CompileCrypt();
        var first = IndoorBuilder.Build(area, Pcg32.ForAttempt("crypt", 5, 0));
        var second = IndoorBuilder.Build(area, Pcg32.ForAttempt("crypt", 5, 0));
        Assert.Equal(first.Failure, second.Failure);
        Assert.Equal(first.Grid?.Ground, second.Grid?.Ground);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Mortar.slnx`
Expected: build FAILS with `The name 'RoomFiller' does not exist in the current context`.

- [ ] **Step 3: Implement door geometry and filling**

`src/Mortar.Core/Indoor/DoorGeometry.cs`:

```csharp
namespace Mortar.Core.Indoor;

internal static class DoorGeometry
{
    public static IEnumerable<(int X, int Y)> WorldDoors(Placement placement, int stride) =>
        placement.Room.Doors.Select(d => (placement.SlotX * stride + d.X, placement.SlotY * stride + d.Y));

    public static bool DoorsMeet(Placement a, Placement b, int stride)
    {
        var doorsOfB = new HashSet<(int X, int Y)>(WorldDoors(b, stride));
        return WorldDoors(a, stride).Any(doorsOfB.Contains);
    }
}
```

`src/Mortar.Core/Indoor/RoomFiller.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Indoor;

internal static class RoomFiller
{
    public static string? FillAll(CompiledArea area, SlotGrid grid, Pcg32 rng)
    {
        var fillers = area.Rooms
            .Where(r => r.IsSingleSlot && (r.HasTag(StandardTags.Filler) || r.HasTag(StandardTags.Corridor)))
            .ToList();
        var deadEnds = area.Rooms.Where(r => r.IsSingleSlot && r.HasTag(StandardTags.DeadEnd)).ToList();
        for (var y = 0; y < grid.Rows; y++)
        {
            for (var x = 0; x < grid.Columns; x++)
            {
                if (!grid.IsUsed(x, y) || grid.BlockAt(x, y) != -1) continue;
                var room = Choose(grid, x, y, fillers, deadEnds, rng);
                if (room is null) return "no room fits";
                grid.Place(new Placement(room, x, y, null));
            }
        }
        return null;
    }

    public static bool Fits(SlotGrid grid, Room room, int x, int y, IReadOnlyList<Side> open)
    {
        var candidate = new Placement(room, x, y, null);
        foreach (var side in open)
        {
            if (!room.HasDoorInSegment(side, 0)) return false;
            var (dx, dy) = Sides.Offset(side);
            var neighbor = grid.BlockAt(x + dx, y + dy);
            if (neighbor != -1 && !DoorGeometry.DoorsMeet(candidate, grid.Placements[neighbor], grid.Stride)) return false;
        }
        return true;
    }

    private static Room? Choose(SlotGrid grid, int x, int y, List<Room> fillers, List<Room> deadEnds, Pcg32 rng)
    {
        var open = Sides.All.Where(side => grid.IsOpen(x, y, side)).ToList();
        if (open.Count == 1)
        {
            var endings = deadEnds.Where(r => Fits(grid, r, x, y, open)).ToList();
            if (endings.Count > 0) return rng.Pick(endings);
        }
        var candidates = fillers.Where(r => Fits(grid, r, x, y, open)).ToList();
        return candidates.Count == 0 ? null : rng.Pick(candidates);
    }
}
```

- [ ] **Step 4: Implement stitching and marker collection**

`src/Mortar.Core/Indoor/StitchCanvas.cs`:

```csharp
using Mortar.Core.Layouts;
using Mortar.Core.Model;

namespace Mortar.Core.Indoor;

internal sealed class StitchCanvas
{
    private readonly Tileset _tileset;
    private readonly bool[] _written;
    private readonly bool[] _forced;

    public StitchCanvas(int width, int height, Tileset tileset)
    {
        _tileset = tileset;
        Cells = new CellGrid(width, height, tileset.VoidIndex);
        _written = new bool[width * height];
        _forced = new bool[width * height];
    }

    public CellGrid Cells { get; }

    public void ForceFloor(int x, int y)
    {
        var index = y * Cells.Width + x;
        Cells.Ground[index] = _tileset.FloorIndex;
        _written[index] = true;
        _forced[index] = true;
    }

    public void Merge(int x, int y, int ground)
    {
        var index = y * Cells.Width + x;
        if (_forced[index]) return;
        if (!_written[index])
        {
            Cells.Ground[index] = ground;
            _written[index] = true;
            return;
        }
        if (_tileset.IsWalkable(Cells.Ground[index]) && !_tileset.IsWalkable(ground))
        {
            Cells.Ground[index] = ground;
        }
    }
}
```

`src/Mortar.Core/Indoor/RoomStitcher.cs`:

```csharp
using Mortar.Core.Layouts;
using Mortar.Core.Model;

namespace Mortar.Core.Indoor;

internal static class RoomStitcher
{
    public static CellGrid Stitch(SlotGrid grid, Tileset tileset)
    {
        var connected = ConnectedDoors(grid);
        var canvas = new StitchCanvas(grid.CellWidth, grid.CellHeight, tileset);
        foreach (var placement in grid.Placements)
        {
            Draw(placement, grid.Stride, connected, canvas, tileset);
        }
        return canvas.Cells;
    }

    private static void Draw(
        Placement placement,
        int stride,
        HashSet<(int X, int Y)> connected,
        StitchCanvas canvas,
        Tileset tileset)
    {
        var room = placement.Room;
        for (var ly = 0; ly < room.Height; ly++)
        {
            for (var lx = 0; lx < room.Width; lx++)
            {
                var x = placement.SlotX * stride + lx;
                var y = placement.SlotY * stride + ly;
                if (!room.HasDoorAt(lx, ly)) canvas.Merge(x, y, room.GroundAt(lx, ly));
                else if (connected.Contains((x, y))) canvas.ForceFloor(x, y);
                else canvas.Merge(x, y, tileset.WallIndex);
            }
        }
    }

    private static HashSet<(int X, int Y)> ConnectedDoors(SlotGrid grid)
    {
        var seen = new HashSet<(int X, int Y)>();
        var connected = new HashSet<(int X, int Y)>();
        foreach (var placement in grid.Placements)
        {
            foreach (var door in DoorGeometry.WorldDoors(placement, grid.Stride))
            {
                if (!seen.Add(door) && IsOpenBoundary(grid, door.X, door.Y))
                {
                    connected.Add(door);
                }
            }
        }
        return connected;
    }

    private static bool IsOpenBoundary(SlotGrid grid, int x, int y)
    {
        var stride = grid.Stride;
        if (x % stride == 0 && y % stride != 0)
        {
            var column = x / stride;
            return column > 0 && column < grid.Columns && grid.IsOpen(column - 1, y / stride, Side.East);
        }
        if (y % stride == 0 && x % stride != 0)
        {
            var row = y / stride;
            return row > 0 && row < grid.Rows && grid.IsOpen(x / stride, row - 1, Side.South);
        }
        return false;
    }
}
```

`src/Mortar.Core/Indoor/MarkerCollector.cs`:

```csharp
using Mortar.Core.Layouts;
using Mortar.Core.Model;

namespace Mortar.Core.Indoor;

internal static class MarkerCollector
{
    public static List<Marker> Collect(CompiledArea area, SlotGrid grid)
    {
        var markers = new List<Marker>();
        foreach (var placement in grid.Placements)
        {
            var node = placement.NodeId is null ? null : area.Node(placement.NodeId);
            AddRoomMarkers(placement, node, grid.Stride, markers);
        }
        return markers;
    }

    private static void AddRoomMarkers(Placement placement, GraphNode? node, int stride, List<Marker> markers)
    {
        var originX = placement.SlotX * stride;
        var originY = placement.SlotY * stride;
        var landmarkFound = false;
        foreach (var marker in placement.Room.Markers)
        {
            var isLandmark = node is not null && !landmarkFound && marker.Kind == node.Kind;
            landmarkFound |= isLandmark;
            markers.Add(new Marker(marker.Kind, isLandmark ? node!.Id : null, originX + marker.X, originY + marker.Y));
        }
        if (node is not null && !landmarkFound)
        {
            var room = placement.Room;
            markers.Add(new Marker(node.Kind, node.Id, originX + room.Width / 2, originY + room.Height / 2));
        }
    }
}
```

- [ ] **Step 5: Implement the indoor builder**

`src/Mortar.Core/Indoor/BuildOutcome.cs`:

```csharp
using Mortar.Core.Layouts;

namespace Mortar.Core.Indoor;

internal sealed class BuildOutcome
{
    private BuildOutcome(CellGrid? grid, IReadOnlyList<Marker> markers, string? failure)
    {
        Grid = grid;
        Markers = markers;
        Failure = failure;
    }

    public CellGrid? Grid { get; }

    public IReadOnlyList<Marker> Markers { get; }

    public string? Failure { get; }

    public static BuildOutcome Built(CellGrid grid, IReadOnlyList<Marker> markers) =>
        new BuildOutcome(grid, markers, null);

    public static BuildOutcome Failed(string failure) =>
        new BuildOutcome(null, Array.Empty<Marker>(), failure);
}
```

`src/Mortar.Core/Indoor/IndoorBuilder.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Indoor;

internal static class IndoorBuilder
{
    public static BuildOutcome Build(CompiledArea area, Pcg32 rng)
    {
        var grid = SlotGrid.ForArea(area.Definition);
        var failure = LandmarkPlacer.PlaceAll(area, grid, rng)
            ?? IndoorRouter.Route(area, grid, rng)
            ?? RoomFiller.FillAll(area, grid, rng);
        if (failure is not null)
        {
            return BuildOutcome.Failed(failure);
        }
        return BuildOutcome.Built(RoomStitcher.Stitch(grid, area.Tileset), MarkerCollector.Collect(area, grid));
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test Mortar.slnx`
Expected: PASS (all tests, including 5 filler, 3 stitcher, 2 marker, and 2 builder tests).

- [ ] **Step 7: Commit**

```powershell
git add src/Mortar.Core tests/Mortar.Tests
git commit -m "add room filling, stitching, and indoor builder"
```

---

### Task 8: JSON loader and samples

**Files:**
- Create: `src/Mortar.Json/Mortar.Json.csproj`, `Dtos.cs`, `JsonFile.cs`, `DefinitionMapper.cs`, `AreaMapper.cs`, `MortarLoader.cs`
- Create: `samples/tileset.json`, `samples/areas/crypt.json`, `samples/rooms/entrance.json`, `exit.json`, `waypoint.json`, `boss.json`, `filler_hall.json`, `filler_pillars.json`, `corridor_ns.json`, `corridor_ew.json`, `dead_end_alcove.json`
- Create: `tests/Mortar.Tests/Support/TestPaths.cs`, `TempDir.cs`, `SampleArea.cs`
- Test: `tests/Mortar.Tests/Json/MortarLoaderTests.cs`

**Interfaces:**
- Consumes: `Tileset.Create`, `GroundType`, `RoomDefinition` (Task 2); `AreaDefinition`, `TargetSize`, `GraphNode`, `GraphEdge`, `FractionRect`, `AreaMode`, `EdgeKind`, `AreaCompiler.Compile`, `CompiledArea` (Task 3).
- Produces:
  - `MortarLoader.LoadArea(string areaPath) : AuthoringResult<CompiledArea>`, `MortarLoader.LoadTileset(string path) : AuthoringResult<Tileset>`.
  - Test helpers `TestPaths.Root`, `TestPaths.Samples`, `TestPaths.CryptArea`; `TempDir` (`Root`, `Write(relative, contents) : string` full path, `IDisposable`); `SampleArea.LoadCrypt() : CompiledArea`, `SampleArea.Tileset`, `SampleArea.Rooms` (absolute forward-slash paths), `SampleArea.AreaJson(tileset, rooms, mode = "indoor", tolerance = 15, extra = "") : string`.

**File formats** (documented again in Task 12's authoring guide):

- Tileset: `{ "groundTypes": [ { "id", "char", "walkable", "blocksSight" } ], "variantWeights": { "<tileType>": [int, ...] } }`
- Room: `{ "id", "tags": [...], "grid": [ "row", ... ] }`
- Area: `{ "id", "mode", "tileset", "targetSize": { "width", "height", "tolerance" }, "slotSize", "rooms": [paths], "roomTags"?: [...], "maxSideBranches", "maxAttempts"?, "graph": { "nodes": [ { "id", "kind"?, "region": { "x": [from, to], "y": [from, to] } } ], "edges": [ { "from", "to", "kind" } ] } }`. A node's `kind` defaults to its `id`. Unknown properties are errors.

- [ ] **Step 1: Create the project**

```powershell
dotnet new classlib -n Mortar.Json -o src/Mortar.Json -f netstandard2.1
Remove-Item src/Mortar.Json/Class1.cs
dotnet add src/Mortar.Json package System.Text.Json
dotnet add src/Mortar.Json reference src/Mortar.Core
dotnet sln Mortar.slnx add src/Mortar.Json/Mortar.Json.csproj
dotnet add tests/Mortar.Tests reference src/Mortar.Json
```

Make sure `src/Mortar.Json/Mortar.Json.csproj` has `<TargetFramework>netstandard2.1</TargetFramework>` and `<RootNamespace>Mortar.Json</RootNamespace>` in its `<PropertyGroup>`.

- [ ] **Step 2: Add the samples**

`samples/tileset.json`:

```json
{
  "groundTypes": [
    { "id": "void", "char": " ", "walkable": false, "blocksSight": false },
    { "id": "floor", "char": ".", "walkable": true, "blocksSight": false },
    { "id": "wall", "char": "#", "walkable": false, "blocksSight": true }
  ],
  "variantWeights": {
    "floor": [6, 1, 1],
    "wall": [4, 1]
  }
}
```

`samples/areas/crypt.json`:

```json
{
  "id": "crypt",
  "mode": "indoor",
  "tileset": "../tileset.json",
  "targetSize": { "width": 60, "height": 60, "tolerance": 15 },
  "slotSize": 9,
  "rooms": ["../rooms"],
  "maxSideBranches": 4,
  "maxAttempts": 20,
  "graph": {
    "nodes": [
      { "id": "entrance", "kind": "entrance", "region": { "x": [0.0, 0.1], "y": [0.3, 0.7] } },
      { "id": "waypoint", "kind": "waypoint", "region": { "x": [0.3, 0.7], "y": [0.0, 0.1] } },
      { "id": "boss", "kind": "boss", "region": { "x": [0.9, 1.0], "y": [0.3, 0.7] } },
      { "id": "exit", "kind": "exit", "region": { "x": [0.3, 0.7], "y": [0.9, 1.0] } }
    ],
    "edges": [
      { "from": "entrance", "to": "waypoint", "kind": "path" },
      { "from": "waypoint", "to": "boss", "kind": "path" },
      { "from": "boss", "to": "exit", "kind": "path" },
      { "from": "entrance", "to": "exit", "kind": "branch" }
    ]
  }
}
```

`samples/rooms/entrance.json`:

```json
{
  "id": "entrance",
  "tags": ["entrance"],
  "grid": [
    "####D####",
    "#.......#",
    "#.......#",
    "#.......#",
    "D...E...D",
    "#.......#",
    "#.......#",
    "#.......#",
    "####D####"
  ]
}
```

`samples/rooms/exit.json`:

```json
{
  "id": "exit",
  "tags": ["exit"],
  "grid": [
    "####D####",
    "#.......#",
    "#.......#",
    "#.......#",
    "D...X...D",
    "#.......#",
    "#.......#",
    "#.......#",
    "####D####"
  ]
}
```

`samples/rooms/waypoint.json`:

```json
{
  "id": "waypoint",
  "tags": ["waypoint"],
  "grid": [
    "####D####",
    "#.......#",
    "#.#...#.#",
    "#.......#",
    "D...W...D",
    "#.......#",
    "#.#...#.#",
    "#.......#",
    "####D####"
  ]
}
```

`samples/rooms/boss.json`:

```json
{
  "id": "boss",
  "tags": ["boss"],
  "grid": [
    "####D#######D####",
    "#...............#",
    "#...............#",
    "#...............#",
    "D...S.......S...D",
    "#...............#",
    "#...............#",
    "#...............#",
    "#.......B.......#",
    "#...............#",
    "#...............#",
    "#...............#",
    "D...S.......S...D",
    "#...............#",
    "#...............#",
    "#...............#",
    "####D#######D####"
  ]
}
```

`samples/rooms/filler_hall.json`:

```json
{
  "id": "filler_hall",
  "tags": ["filler"],
  "grid": [
    "####D####",
    "#.......#",
    "#.......#",
    "#.......#",
    "D...S...D",
    "#.......#",
    "#.......#",
    "#.......#",
    "####D####"
  ]
}
```

`samples/rooms/filler_pillars.json`:

```json
{
  "id": "filler_pillars",
  "tags": ["filler"],
  "grid": [
    "####D####",
    "#.......#",
    "#.#...#.#",
    "#...C...#",
    "D.......D",
    "#.......#",
    "#.#...#.#",
    "#.......#",
    "####D####"
  ]
}
```

`samples/rooms/corridor_ns.json`:

```json
{
  "id": "corridor_ns",
  "tags": ["corridor"],
  "grid": [
    "####D####",
    "###...###",
    "###...###",
    "###...###",
    "###.S.###",
    "###...###",
    "###...###",
    "###...###",
    "####D####"
  ]
}
```

`samples/rooms/corridor_ew.json`:

```json
{
  "id": "corridor_ew",
  "tags": ["corridor"],
  "grid": [
    "#########",
    "#########",
    "#########",
    "#.......#",
    "D...S...D",
    "#.......#",
    "#########",
    "#########",
    "#########"
  ]
}
```

`samples/rooms/dead_end_alcove.json`:

```json
{
  "id": "dead_end_alcove",
  "tags": ["dead_end"],
  "grid": [
    "####D####",
    "#.......#",
    "#.#####.#",
    "#.#.C.#.#",
    "D.#...#.D",
    "#.#...#.#",
    "#.##.##.#",
    "#.......#",
    "####D####"
  ]
}
```

- [ ] **Step 3: Write the test helpers**

`tests/Mortar.Tests/Support/TestPaths.cs`:

```csharp
namespace Mortar.Tests.Support;

internal static class TestPaths
{
    public static string Root { get; } = FindRoot();

    public static string Samples => Path.Combine(Root, "samples");

    public static string CryptArea => Path.Combine(Samples, "areas", "crypt.json");

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Mortar.slnx")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException("Mortar.slnx not found above the test output folder");
    }
}
```

`tests/Mortar.Tests/Support/TempDir.cs`:

```csharp
namespace Mortar.Tests.Support;

internal sealed class TempDir : IDisposable
{
    public TempDir()
    {
        Root = Path.Combine(Path.GetTempPath(), "mortar-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string Write(string relativePath, string contents)
    {
        var fullPath = Path.GetFullPath(Path.Combine(Root, relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, contents);
        return fullPath;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
```

`tests/Mortar.Tests/Support/SampleArea.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Json;

namespace Mortar.Tests.Support;

internal static class SampleArea
{
    public static string Tileset => JsonPath(Path.Combine(TestPaths.Samples, "tileset.json"));

    public static string Rooms => JsonPath(Path.Combine(TestPaths.Samples, "rooms"));

    public static CompiledArea LoadCrypt()
    {
        var result = MortarLoader.LoadArea(TestPaths.CryptArea);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        return result.Value!;
    }

    public static string AreaJson(string tileset, string rooms, string mode = "indoor", int tolerance = 15, string extra = "") => $$"""
        {
          "id": "temp",
          "mode": "{{mode}}",
          "tileset": "{{tileset}}",
          "targetSize": { "width": 60, "height": 60, "tolerance": {{tolerance}} },
          "slotSize": 9,
          "rooms": ["{{rooms}}"],
          "maxSideBranches": 4,
          "maxAttempts": 5,{{extra}}
          "graph": {
            "nodes": [
              { "id": "entrance", "region": { "x": [0.0, 0.1], "y": [0.3, 0.7] } },
              { "id": "waypoint", "region": { "x": [0.3, 0.7], "y": [0.0, 0.1] } },
              { "id": "boss", "region": { "x": [0.9, 1.0], "y": [0.3, 0.7] } },
              { "id": "exit", "region": { "x": [0.3, 0.7], "y": [0.9, 1.0] } }
            ],
            "edges": [
              { "from": "entrance", "to": "waypoint", "kind": "path" },
              { "from": "waypoint", "to": "boss", "kind": "path" },
              { "from": "boss", "to": "exit", "kind": "path" }
            ]
          }
        }
        """;

    private static string JsonPath(string path) => path.Replace('\\', '/');
}
```

- [ ] **Step 4: Write the failing tests**

`tests/Mortar.Tests/Json/MortarLoaderTests.cs`:

```csharp
using Mortar.Json;
using Mortar.Tests.Support;

namespace Mortar.Tests.Json;

public class MortarLoaderTests
{
    [Fact]
    public void LoadArea_Sample_Succeeds()
    {
        var area = SampleArea.LoadCrypt();
        Assert.Equal("crypt", area.Definition.Id);
        Assert.Equal(9, area.Rooms.Count);
        Assert.Equal(new[] { "void", "floor", "wall" }, area.Tileset.Grounds.Select(g => g.Id));
        Assert.Equal(new[] { 6, 1, 1 }, area.Tileset.WeightsFor("floor"));
    }

    [Fact]
    public void LoadArea_InvalidJson_NamesTheFile()
    {
        using var temp = new TempDir();
        var path = temp.Write("area.json", "{ not json");
        var error = Assert.Single(MortarLoader.LoadArea(path).Errors);
        Assert.Equal(path, error.Source);
        Assert.StartsWith("invalid JSON", error.Message);
    }

    [Fact]
    public void LoadArea_UnknownProperty_IsAnError()
    {
        using var temp = new TempDir();
        var json = SampleArea.AreaJson(SampleArea.Tileset, SampleArea.Rooms, extra: "\n  \"maxSideBranch\": 2,");
        var path = temp.Write("area.json", json);
        Assert.Contains(MortarLoader.LoadArea(path).Errors, e => e.Message.Contains("maxSideBranch"));
    }

    [Fact]
    public void LoadArea_MissingRoomPath_IsAnError()
    {
        using var temp = new TempDir();
        var path = temp.Write("area.json", SampleArea.AreaJson(SampleArea.Tileset, "missing"));
        Assert.Contains(MortarLoader.LoadArea(path).Errors, e => e.Message == "room path 'missing' does not exist");
    }

    [Fact]
    public void LoadArea_UnknownMode_IsAnError()
    {
        using var temp = new TempDir();
        var path = temp.Write("area.json", SampleArea.AreaJson(SampleArea.Tileset, SampleArea.Rooms, mode: "underwater"));
        Assert.Contains(MortarLoader.LoadArea(path).Errors, e => e.Message.Contains("mode must be 'indoor' or 'outdoor'"));
    }

    [Fact]
    public void LoadArea_BadRoom_NamesTheRoomFile()
    {
        using var temp = new TempDir();
        var room = temp.Write("rooms/bad.json", """{ "id": "bad", "tags": ["filler"], "grid": ["####", "#..#"] }""");
        var area = temp.Write("area.json", SampleArea.AreaJson(SampleArea.Tileset, "rooms"));
        Assert.Contains(MortarLoader.LoadArea(area).Errors, e => e.Source == room && e.Message.Contains("width and height"));
    }
}
```

- [ ] **Step 5: Run tests to verify they fail**

Run: `dotnet test Mortar.slnx`
Expected: build FAILS with `The type or namespace name 'MortarLoader' could not be found`.

- [ ] **Step 6: Implement the DTOs and file reading**

`src/Mortar.Json/Dtos.cs`:

```csharp
namespace Mortar.Json;

internal sealed class TilesetDto
{
    public List<GroundTypeDto>? GroundTypes { get; set; }

    public Dictionary<string, List<int>?>? VariantWeights { get; set; }
}

internal sealed class GroundTypeDto
{
    public string? Id { get; set; }

    public string? Char { get; set; }

    public bool Walkable { get; set; }

    public bool BlocksSight { get; set; }
}

internal sealed class RoomDto
{
    public string? Id { get; set; }

    public List<string>? Tags { get; set; }

    public List<string?>? Grid { get; set; }
}

internal sealed class AreaDto
{
    public string? Id { get; set; }

    public string? Mode { get; set; }

    public string? Tileset { get; set; }

    public TargetSizeDto? TargetSize { get; set; }

    public int? SlotSize { get; set; }

    public List<string>? Rooms { get; set; }

    public List<string>? RoomTags { get; set; }

    public int MaxSideBranches { get; set; }

    public int? MaxAttempts { get; set; }

    public GraphDto? Graph { get; set; }
}

internal sealed class TargetSizeDto
{
    public int Width { get; set; }

    public int Height { get; set; }

    public int Tolerance { get; set; }
}

internal sealed class GraphDto
{
    public List<NodeDto>? Nodes { get; set; }

    public List<EdgeDto>? Edges { get; set; }
}

internal sealed class NodeDto
{
    public string? Id { get; set; }

    public string? Kind { get; set; }

    public RegionDto? Region { get; set; }
}

internal sealed class RegionDto
{
    public List<double>? X { get; set; }

    public List<double>? Y { get; set; }
}

internal sealed class EdgeDto
{
    public string? From { get; set; }

    public string? To { get; set; }

    public string? Kind { get; set; }
}
```

`src/Mortar.Json/JsonFile.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Mortar.Core.Authoring;

namespace Mortar.Json;

internal static class JsonFile
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static T? Read<T>(string path, out AuthoringError? error)
        where T : class
    {
        error = null;
        try
        {
            var value = JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
            if (value is null) error = new AuthoringError(path, "file contains null");
            return value;
        }
        catch (JsonException ex)
        {
            error = new AuthoringError(path, $"invalid JSON: {ex.Message}");
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            error = new AuthoringError(path, $"cannot read file: {ex.Message}");
        }
        return null;
    }
}
```

- [ ] **Step 7: Implement the mappers and loader**

`src/Mortar.Json/DefinitionMapper.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Core.Model;

namespace Mortar.Json;

internal static class DefinitionMapper
{
    public static AuthoringResult<Tileset> ToTileset(string source, TilesetDto dto)
    {
        var errors = new List<AuthoringError>();
        var grounds = new List<GroundType>();
        foreach (var ground in dto.GroundTypes ?? new List<GroundTypeDto>())
        {
            if (ground.Char is not { Length: 1 })
            {
                errors.Add(new AuthoringError(source, $"ground '{ground.Id}' must have a single-character char"));
                continue;
            }
            grounds.Add(new GroundType(ground.Id ?? "", ground.Char[0], ground.Walkable, ground.BlocksSight));
        }
        if (errors.Count > 0)
        {
            return AuthoringResult<Tileset>.Failure(errors);
        }
        var weights = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal);
        foreach (var pair in dto.VariantWeights ?? new Dictionary<string, List<int>?>())
        {
            weights[pair.Key] = pair.Value ?? new List<int>();
        }
        return Tileset.Create(source, grounds, weights);
    }

    public static RoomDefinition ToRoom(string source, RoomDto dto) => new RoomDefinition(
        source,
        dto.Id ?? "",
        dto.Tags ?? new List<string>(),
        (dto.Grid ?? new List<string?>()).Select(row => row ?? "").ToList());
}
```

`src/Mortar.Json/AreaMapper.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Core.Model;

namespace Mortar.Json;

internal static class AreaMapper
{
    public static AuthoringResult<AreaDefinition> ToArea(string source, AreaDto dto)
    {
        var errors = new List<AuthoringError>();
        void Error(string message) => errors.Add(new AuthoringError(source, message));
        var mode = MapMode(dto.Mode, Error);
        if (string.IsNullOrWhiteSpace(dto.Tileset)) Error("area has no tileset");
        if (dto.Rooms is null || dto.Rooms.Count == 0) Error("area lists no rooms");
        if (dto.TargetSize is null) Error("area has no targetSize");
        if (dto.SlotSize is null) Error("area has no slotSize");
        if (dto.Graph?.Nodes is null) Error("area has no graph nodes");
        var nodes = (dto.Graph?.Nodes ?? new List<NodeDto>()).Select(n => MapNode(n, Error)).ToList();
        var edges = (dto.Graph?.Edges ?? new List<EdgeDto>()).Select(e => MapEdge(e, Error)).ToList();
        if (errors.Count > 0)
        {
            return AuthoringResult<AreaDefinition>.Failure(errors);
        }
        var size = dto.TargetSize!;
        return AuthoringResult<AreaDefinition>.Success(new AreaDefinition(
            source,
            dto.Id ?? "",
            mode,
            new TargetSize(size.Width, size.Height, size.Tolerance),
            dto.SlotSize!.Value,
            dto.MaxSideBranches,
            dto.MaxAttempts ?? AreaDefinition.DefaultMaxAttempts,
            dto.RoomTags ?? new List<string>(),
            nodes,
            edges));
    }

    private static AreaMode MapMode(string? mode, Action<string> error)
    {
        switch (mode)
        {
            case "indoor": return AreaMode.Indoor;
            case "outdoor": return AreaMode.Outdoor;
            default:
                error($"mode must be 'indoor' or 'outdoor', got '{mode}'");
                return AreaMode.Indoor;
        }
    }

    private static GraphNode MapNode(NodeDto dto, Action<string> error)
    {
        var id = dto.Id ?? "";
        var kind = dto.Kind ?? id;
        var x = dto.Region?.X;
        var y = dto.Region?.Y;
        if (x is not { Count: 2 } || y is not { Count: 2 })
        {
            error($"node '{id}' region needs x and y as [from, to] pairs");
            return new GraphNode(id, kind, new FractionRect(0, 1, 0, 1));
        }
        return new GraphNode(id, kind, new FractionRect(x[0], x[1], y[0], y[1]));
    }

    private static GraphEdge MapEdge(EdgeDto dto, Action<string> error)
    {
        EdgeKind? kind = dto.Kind switch
        {
            "path" => EdgeKind.Path,
            "branch" => EdgeKind.Branch,
            _ => null,
        };
        if (kind is null)
        {
            error($"edge {dto.From}->{dto.To} kind must be 'path' or 'branch', got '{dto.Kind}'");
        }
        return new GraphEdge(dto.From ?? "", dto.To ?? "", kind ?? EdgeKind.Path);
    }
}
```

`src/Mortar.Json/MortarLoader.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Core.Model;

namespace Mortar.Json;

public static class MortarLoader
{
    public static AuthoringResult<CompiledArea> LoadArea(string areaPath)
    {
        var source = Path.GetFullPath(areaPath);
        var dto = JsonFile.Read<AreaDto>(source, out var readError);
        if (dto is null)
        {
            return AuthoringResult<CompiledArea>.Failure(new[] { readError! });
        }
        var area = AreaMapper.ToArea(source, dto);
        if (!area.Succeeded)
        {
            return AuthoringResult<CompiledArea>.Failure(area.Errors);
        }
        var directory = Path.GetDirectoryName(source)!;
        var tileset = LoadTileset(Path.Combine(directory, dto.Tileset!));
        var errors = new List<AuthoringError>(tileset.Errors);
        var rooms = LoadRooms(source, directory, dto.Rooms!, errors);
        if (errors.Count > 0)
        {
            return AuthoringResult<CompiledArea>.Failure(errors);
        }
        return AreaCompiler.Compile(area.Value!, tileset.Value!, rooms);
    }

    public static AuthoringResult<Tileset> LoadTileset(string path)
    {
        var source = Path.GetFullPath(path);
        var dto = JsonFile.Read<TilesetDto>(source, out var error);
        return dto is null
            ? AuthoringResult<Tileset>.Failure(new[] { error! })
            : DefinitionMapper.ToTileset(source, dto);
    }

    private static List<RoomDefinition> LoadRooms(
        string areaSource,
        string directory,
        IReadOnlyList<string> entries,
        List<AuthoringError> errors)
    {
        var rooms = new List<RoomDefinition>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            foreach (var file in ExpandRoomEntry(areaSource, directory, entry, errors))
            {
                if (!seen.Add(file)) continue;
                var dto = JsonFile.Read<RoomDto>(file, out var error);
                if (dto is null) errors.Add(error!);
                else rooms.Add(DefinitionMapper.ToRoom(file, dto));
            }
        }
        return rooms;
    }

    private static IEnumerable<string> ExpandRoomEntry(
        string areaSource,
        string directory,
        string entry,
        List<AuthoringError> errors)
    {
        var path = Path.GetFullPath(Path.Combine(directory, entry));
        if (Directory.Exists(path))
        {
            return Directory.GetFiles(path, "*.json").OrderBy(file => Path.GetFileName(file), StringComparer.Ordinal);
        }
        if (File.Exists(path))
        {
            return new[] { path };
        }
        errors.Add(new AuthoringError(areaSource, $"room path '{entry}' does not exist"));
        return Array.Empty<string>();
    }
}
```

- [ ] **Step 8: Run tests to verify they pass**

Run: `dotnet test Mortar.slnx`
Expected: PASS (all tests, including 6 loader tests).

- [ ] **Step 9: Commit**

```powershell
git add src/Mortar.Json samples tests/Mortar.Tests Mortar.slnx
git commit -m "add json loader and sample crypt area"
```

---

### Task 9: Generator with retries, determinism, and snapshots

**Files:**
- Create: `src/Mortar.Core/Generation/FailureCount.cs`, `GenerationResult.cs`, `LayoutFactory.cs`, `Generator.cs`
- Create: `tests/Mortar.Tests/Support/AsciiMap.cs`
- Test: `tests/Mortar.Tests/Generation/GeneratorTests.cs`, `WalkabilityTests.cs`, `SnapshotTests.cs`
- Create (generated by the snapshot test, then reviewed and committed): `tests/Mortar.Tests/Snapshots/crypt-seed-1.txt`, `crypt-seed-2.txt`, `crypt-seed-3.txt`

**Interfaces:**
- Consumes: `IndoorBuilder.Build`, `BuildOutcome` (Task 7); `LayoutValidator.Validate` (Task 5); `TileResolver.Resolve`, `Layout`, `LayoutCells`, `CellGrid`, `Marker` (Task 4); `CompiledArea` (Task 3); `Pcg32.ForAttempt` (Task 1); `SampleArea.LoadCrypt`, `TestPaths` (Task 8).
- Produces:
  - `FailureCount(string reason, int count)`: `Reason`, `Count`.
  - `GenerationResult`: `Layout : Layout?`, `Failures : IReadOnlyList<FailureCount>` (most frequent first, then by reason), `Attempts : int`, `Succeeded : bool`, `FailureSummary : string` (for example `"boss unreachable: 14, too small: 6"`).
  - `Generator.Generate(CompiledArea area, uint seed) : GenerationResult`.

- [ ] **Step 1: Write the ASCII snapshot helper**

`tests/Mortar.Tests/Support/AsciiMap.cs`:

```csharp
using System.Text;
using Mortar.Core.Layouts;
using Mortar.Core.Model;

namespace Mortar.Tests.Support;

internal static class AsciiMap
{
    private static readonly Dictionary<string, char> MarkerSymbols = new Dictionary<string, char>
    {
        [MarkerKinds.Entrance] = 'E',
        [MarkerKinds.Exit] = 'X',
        [MarkerKinds.Boss] = 'B',
        [MarkerKinds.Waypoint] = 'W',
        [MarkerKinds.Spawn] = 'S',
        [MarkerKinds.Chest] = 'C',
    };

    public static string Render(Layout layout, Tileset tileset)
    {
        var rows = new char[layout.Height][];
        for (var y = 0; y < layout.Height; y++)
        {
            rows[y] = new char[layout.Width];
            for (var x = 0; x < layout.Width; x++)
            {
                var symbol = tileset.Grounds[tileset.IndexOf(layout.Cells.GroundAt(x, y))].Symbol;
                rows[y][x] = symbol == ' ' ? '_' : symbol;
            }
        }
        foreach (var marker in layout.Markers)
        {
            rows[marker.Y][marker.X] = MarkerSymbols.TryGetValue(marker.Kind, out var symbol) ? symbol : '?';
        }
        var text = new StringBuilder();
        text.Append("fingerprint: ").Append(layout.Fingerprint).Append('\n');
        foreach (var row in rows)
        {
            text.Append(row).Append('\n');
        }
        return text.ToString();
    }
}
```

- [ ] **Step 2: Write the failing tests**

`tests/Mortar.Tests/Generation/GeneratorTests.cs`:

```csharp
using Mortar.Core.Generation;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Generation;

public class GeneratorTests
{
    private static readonly CompiledArea Crypt = SampleArea.LoadCrypt();

    [Fact]
    public void Generate_Sample_Succeeds()
    {
        var result = Generator.Generate(Crypt, 1);
        Assert.True(result.Succeeded, result.FailureSummary);
        var layout = result.Layout!;
        Assert.Equal(("crypt", 1u, 57, 57), (layout.AreaId, layout.Seed, layout.Width, layout.Height));
        Assert.Equal(result.Attempts, layout.Attempt + 1);
    }

    [Fact]
    public void Generate_SameSeed_SameFingerprint() =>
        Assert.Equal(Generator.Generate(Crypt, 42).Layout?.Fingerprint, Generator.Generate(Crypt, 42).Layout?.Fingerprint);

    [Fact]
    public void Generate_DifferentSeeds_DifferentFingerprints() =>
        Assert.NotEqual(Generator.Generate(Crypt, 1).Layout?.Fingerprint, Generator.Generate(Crypt, 2).Layout?.Fingerprint);

    [Theory]
    [InlineData(0u)]
    [InlineData(uint.MaxValue)]
    public void Generate_SeedEdgeValues_AreDeterministic(uint seed)
    {
        var first = Generator.Generate(Crypt, seed);
        var second = Generator.Generate(Crypt, seed);
        Assert.Equal(first.Succeeded, second.Succeeded);
        Assert.Equal(first.Layout?.Fingerprint, second.Layout?.Fingerprint);
    }

    [Fact]
    public void RoomOrder_DoesNotAffectOutput()
    {
        var rooms = TestContent.CryptRooms();
        var forward = Generator.Generate(TestContent.CompileCrypt(rooms: rooms), 9);
        var reversed = Generator.Generate(TestContent.CompileCrypt(rooms: Enumerable.Reverse(rooms).ToList()), 9);
        Assert.NotNull(forward.Layout);
        Assert.Equal(forward.Layout!.Fingerprint, reversed.Layout?.Fingerprint);
    }

    [Fact]
    public void Generate_ImpossibleArea_ReportsCountedFailures()
    {
        var area = TestContent.CompileCrypt(TestContent.CryptDefinition(tolerance: 0, maxAttempts: 5));
        var result = Generator.Generate(area, 1);
        Assert.False(result.Succeeded);
        Assert.Equal(5, result.Attempts);
        Assert.Equal(5, result.Failures.Sum(f => f.Count));
        Assert.Equal("too small", result.Failures[0].Reason);
        Assert.StartsWith("too small: ", result.FailureSummary);
    }
}
```

`tests/Mortar.Tests/Generation/WalkabilityTests.cs`:

```csharp
using Mortar.Core.Generation;
using Mortar.Core.Layouts;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Generation;

public class WalkabilityTests
{
    private static readonly CompiledArea Crypt = SampleArea.LoadCrypt();

    [Fact]
    public void SuccessfulLayouts_ReachEveryMarkerAndWalkableCell()
    {
        for (uint seed = 0; seed < 50; seed++)
        {
            var result = Generator.Generate(Crypt, seed);
            if (!result.Succeeded) continue;
            var layout = result.Layout!;
            var entrance = layout.Markers.First(m => m.Kind == MarkerKinds.Entrance);
            var reached = Reach(layout.Cells, entrance.X, entrance.Y);
            foreach (var marker in layout.Markers)
            {
                Assert.True(reached[marker.Y * layout.Width + marker.X], $"seed {seed}: {marker.Kind} unreachable");
            }
            Assert.Equal(layout.Cells.Walkable, reached);
        }
    }

    [Fact]
    public void SampleArea_SucceedsOnAtLeast80PercentOfSeeds()
    {
        var successes = Enumerable.Range(0, 100).Count(seed => Generator.Generate(Crypt, (uint)seed).Succeeded);
        Assert.True(successes >= 80, $"only {successes}/100 seeds succeeded");
    }

    private static bool[] Reach(LayoutCells cells, int startX, int startY)
    {
        var reached = new bool[cells.Width * cells.Height];
        var queue = new Queue<(int X, int Y)>();
        reached[startY * cells.Width + startX] = true;
        queue.Enqueue((startX, startY));
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (nx, ny) in new[] { (x, y - 1), (x + 1, y), (x, y + 1), (x - 1, y) })
            {
                if (nx < 0 || ny < 0 || nx >= cells.Width || ny >= cells.Height) continue;
                if (reached[ny * cells.Width + nx] || !cells.IsWalkable(nx, ny)) continue;
                reached[ny * cells.Width + nx] = true;
                queue.Enqueue((nx, ny));
            }
        }
        return reached;
    }
}
```

`tests/Mortar.Tests/Generation/SnapshotTests.cs`:

```csharp
using Mortar.Core.Generation;
using Mortar.Tests.Support;

namespace Mortar.Tests.Generation;

public class SnapshotTests
{
    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(3u)]
    public void SampleCrypt_MatchesSnapshot(uint seed)
    {
        var area = SampleArea.LoadCrypt();
        var result = Generator.Generate(area, seed);
        Assert.True(result.Succeeded, result.FailureSummary);
        var actual = AsciiMap.Render(result.Layout!, area.Tileset);
        var path = Path.Combine(TestPaths.Root, "tests", "Mortar.Tests", "Snapshots", $"crypt-seed-{seed}.txt");
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

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test Mortar.slnx`
Expected: build FAILS with `The type or namespace name 'Generation' does not exist in the namespace 'Mortar.Core'`.

- [ ] **Step 4: Implement the generator**

`src/Mortar.Core/Generation/FailureCount.cs`:

```csharp
namespace Mortar.Core.Generation;

public sealed class FailureCount
{
    public FailureCount(string reason, int count)
    {
        Reason = reason;
        Count = count;
    }

    public string Reason { get; }

    public int Count { get; }
}
```

`src/Mortar.Core/Generation/GenerationResult.cs`:

```csharp
using Mortar.Core.Layouts;

namespace Mortar.Core.Generation;

public sealed class GenerationResult
{
    private GenerationResult(Layout? layout, IReadOnlyList<FailureCount> failures, int attempts)
    {
        Layout = layout;
        Failures = failures;
        Attempts = attempts;
    }

    public Layout? Layout { get; }

    public IReadOnlyList<FailureCount> Failures { get; }

    public int Attempts { get; }

    public bool Succeeded => Layout is not null;

    public string FailureSummary => string.Join(", ", Failures.Select(f => $"{f.Reason}: {f.Count}"));

    internal static GenerationResult Success(Layout layout, IReadOnlyList<FailureCount> earlierFailures) =>
        new GenerationResult(layout, earlierFailures, layout.Attempt + 1);

    internal static GenerationResult Failure(IReadOnlyList<FailureCount> failures, int attempts) =>
        new GenerationResult(null, failures, attempts);
}
```

`src/Mortar.Core/Generation/LayoutFactory.cs`:

```csharp
using Mortar.Core.Layouts;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Core.Tiles;

namespace Mortar.Core.Generation;

internal static class LayoutFactory
{
    public static Layout Create(
        CompiledArea area,
        uint seed,
        int attempt,
        CellGrid grid,
        IReadOnlyList<Marker> markers,
        Pcg32 rng)
    {
        var tileset = area.Tileset;
        var tiles = TileResolver.Resolve(grid, tileset, rng);
        var cells = new LayoutCells(
            grid.Width,
            grid.Height,
            tileset.Grounds.Select(g => g.Id).ToArray(),
            (int[])grid.Ground.Clone(),
            tiles.TileTypes,
            tiles.Tiles,
            tiles.Variants,
            grid.Ground.Select(tileset.IsWalkable).ToArray());
        return new Layout(area.Definition.Id, seed, attempt, cells, markers, MortarInfo.Version, Layout.CurrentFormatVersion);
    }
}
```

`src/Mortar.Core/Generation/Generator.cs`:

```csharp
using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Core.Validation;

namespace Mortar.Core.Generation;

public static class Generator
{
    public static GenerationResult Generate(CompiledArea area, uint seed)
    {
        var definition = area.Definition;
        var failures = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var attempt = 0; attempt < definition.MaxAttempts; attempt++)
        {
            var rng = Pcg32.ForAttempt(definition.Id, seed, attempt);
            var outcome = IndoorBuilder.Build(area, rng);
            var failure = outcome.Failure
                ?? LayoutValidator.Validate(outcome.Grid!, area.Tileset, outcome.Markers, definition.TargetSize);
            if (failure is null)
            {
                var layout = LayoutFactory.Create(area, seed, attempt, outcome.Grid!, outcome.Markers, rng);
                return GenerationResult.Success(layout, Summarize(failures));
            }
            failures[failure] = failures.TryGetValue(failure, out var count) ? count + 1 : 1;
        }
        return GenerationResult.Failure(Summarize(failures), definition.MaxAttempts);
    }

    private static IReadOnlyList<FailureCount> Summarize(Dictionary<string, int> failures) =>
        failures
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new FailureCount(pair.Key, pair.Value))
            .ToList();
}
```

- [ ] **Step 5: Run tests and create snapshots**

Run: `dotnet test Mortar.slnx`
Expected: every test PASSES except the three `SampleCrypt_MatchesSnapshot` cases, which FAIL with `Snapshot created at ...`.

Open each of `tests/Mortar.Tests/Snapshots/crypt-seed-1.txt`, `-2.txt`, `-3.txt` and check by eye: a `fingerprint:` line, then 57 rows of 57 characters; walls (`#`) enclose floor (`.`); `_` (void) fills unused slots; one `E`, one `X`, one `B`, and one `W` are present; the three seeds look different from each other.

Run: `dotnet test Mortar.slnx`
Expected: PASS (all tests).

- [ ] **Step 6: Commit**

```powershell
git add src/Mortar.Core tests/Mortar.Tests
git commit -m "add generator with retries and determinism snapshots"
```

---

### Task 10: Layout file writer and reader

**Files:**
- Create: `src/Mortar.Json/LayoutWriter.cs`, `src/Mortar.Json/LayoutReader.cs`
- Test: `tests/Mortar.Tests/Json/LayoutJsonTests.cs`

**Interfaces:**
- Consumes: `Layout`, `LayoutCells`, `Marker` (Task 4); `AuthoringResult<T>` (Task 2); `Generator.Generate` (Task 9).
- Produces:
  - `LayoutWriter.ToJson(Layout) : string` — compact JSON, properties in this order: `formatVersion`, `mortarVersion`, `areaId`, `seed`, `attempt`, `width`, `height`, `groundTypes`, `tileTypes`, `cells` (`ground`, `tile`, `variant`), `walkable` (0/1), `markers` (`kind`, optional `node`, `x`, `y`), `fingerprint`.
  - `LayoutReader.FromJson(string json, string source = "layout") : AuthoringResult<Layout>`, `LayoutReader.FromFile(string path) : AuthoringResult<Layout>`. Rejects unsupported `formatVersion`, wrong array lengths, out-of-range palette indices, and fingerprint mismatches.

- [ ] **Step 1: Write the failing tests**

`tests/Mortar.Tests/Json/LayoutJsonTests.cs`:

```csharp
using System.Text.Json.Nodes;
using Mortar.Core.Generation;
using Mortar.Core.Layouts;
using Mortar.Json;
using Mortar.Tests.Support;

namespace Mortar.Tests.Json;

public class LayoutJsonTests
{
    private static readonly Layout Sample = GenerateSample();

    private static Layout GenerateSample()
    {
        var result = Generator.Generate(SampleArea.LoadCrypt(), 1);
        Assert.True(result.Succeeded, result.FailureSummary);
        return result.Layout!;
    }

    private static JsonNode SampleNode() => JsonNode.Parse(LayoutWriter.ToJson(Sample))!;

    [Fact]
    public void RoundTrip_PreservesLayout()
    {
        var read = LayoutReader.FromJson(LayoutWriter.ToJson(Sample));
        Assert.True(read.Succeeded, string.Join("; ", read.Errors));
        var layout = read.Value!;
        Assert.Equal(Sample.Fingerprint, layout.Fingerprint);
        Assert.Equal(Sample.Cells.Ground, layout.Cells.Ground);
        Assert.Equal(Sample.Cells.Walkable, layout.Cells.Walkable);
        Assert.Equal(
            Sample.Markers.Select(m => (m.Kind, m.NodeId, m.X, m.Y)),
            layout.Markers.Select(m => (m.Kind, m.NodeId, m.X, m.Y)));
    }

    [Fact]
    public void ToJson_IsDeterministic()
    {
        var json = LayoutWriter.ToJson(Sample);
        Assert.Equal(json, LayoutWriter.ToJson(Sample));
        Assert.StartsWith("{\"formatVersion\":1,\"mortarVersion\":\"0.1.0\",\"areaId\":\"crypt\"", json);
    }

    [Fact]
    public void FromJson_TamperedCell_ReportsFingerprintMismatch()
    {
        var node = SampleNode();
        node["cells"]!["variant"]![0] = 7;
        var error = Assert.Single(LayoutReader.FromJson(node.ToJsonString()).Errors);
        Assert.StartsWith("fingerprint mismatch", error.Message);
    }

    [Fact]
    public void FromJson_UnsupportedFormatVersion_IsAnError()
    {
        var node = SampleNode();
        node["formatVersion"] = 99;
        var error = Assert.Single(LayoutReader.FromJson(node.ToJsonString()).Errors);
        Assert.StartsWith("unsupported formatVersion 99", error.Message);
    }

    [Fact]
    public void FromJson_Truncated_IsInvalidJson() =>
        Assert.StartsWith("invalid JSON", Assert.Single(LayoutReader.FromJson("{\"formatVersion\":").Errors).Message);

    [Fact]
    public void FromJson_MissingProperties_IsMalformed() =>
        Assert.StartsWith("malformed layout", Assert.Single(LayoutReader.FromJson("{\"formatVersion\":1}").Errors).Message);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Mortar.slnx`
Expected: build FAILS with `The name 'LayoutWriter' does not exist in the current context`.

- [ ] **Step 3: Implement the writer**

`src/Mortar.Json/LayoutWriter.cs`:

```csharp
using System.Text;
using System.Text.Json;
using Mortar.Core.Layouts;

namespace Mortar.Json;

public static class LayoutWriter
{
    public static string ToJson(Layout layout)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            WriteHeader(writer, layout);
            WriteCells(writer, layout.Cells);
            WriteMarkers(writer, layout.Markers);
            writer.WriteString("fingerprint", layout.Fingerprint);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteHeader(Utf8JsonWriter writer, Layout layout)
    {
        writer.WriteNumber("formatVersion", layout.FormatVersion);
        writer.WriteString("mortarVersion", layout.MortarVersion);
        writer.WriteString("areaId", layout.AreaId);
        writer.WriteNumber("seed", layout.Seed);
        writer.WriteNumber("attempt", layout.Attempt);
        writer.WriteNumber("width", layout.Width);
        writer.WriteNumber("height", layout.Height);
    }

    private static void WriteCells(Utf8JsonWriter writer, LayoutCells cells)
    {
        WriteStrings(writer, "groundTypes", cells.GroundTypes);
        WriteStrings(writer, "tileTypes", cells.TileTypes);
        writer.WriteStartObject("cells");
        WriteInts(writer, "ground", cells.Ground);
        WriteInts(writer, "tile", cells.Tiles);
        WriteInts(writer, "variant", cells.Variants);
        writer.WriteEndObject();
        WriteInts(writer, "walkable", cells.Walkable.Select(walkable => walkable ? 1 : 0));
    }

    private static void WriteMarkers(Utf8JsonWriter writer, IReadOnlyList<Marker> markers)
    {
        writer.WriteStartArray("markers");
        foreach (var marker in markers)
        {
            writer.WriteStartObject();
            writer.WriteString("kind", marker.Kind);
            if (marker.NodeId is not null) writer.WriteString("node", marker.NodeId);
            writer.WriteNumber("x", marker.X);
            writer.WriteNumber("y", marker.Y);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteStrings(Utf8JsonWriter writer, string name, IEnumerable<string> values)
    {
        writer.WriteStartArray(name);
        foreach (var value in values) writer.WriteStringValue(value);
        writer.WriteEndArray();
    }

    private static void WriteInts(Utf8JsonWriter writer, string name, IEnumerable<int> values)
    {
        writer.WriteStartArray(name);
        foreach (var value in values) writer.WriteNumberValue(value);
        writer.WriteEndArray();
    }
}
```

- [ ] **Step 4: Implement the reader**

`src/Mortar.Json/LayoutReader.cs`:

```csharp
using System.Text.Json;
using Mortar.Core.Authoring;
using Mortar.Core.Layouts;

namespace Mortar.Json;

public static class LayoutReader
{
    public static AuthoringResult<Layout> FromFile(string path)
    {
        var source = Path.GetFullPath(path);
        try
        {
            return FromJson(File.ReadAllText(source), source);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return AuthoringResult<Layout>.Failure(source, $"cannot read file: {ex.Message}");
        }
    }

    public static AuthoringResult<Layout> FromJson(string json, string source = "layout")
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return Map(document.RootElement, source);
        }
        catch (JsonException ex)
        {
            return AuthoringResult<Layout>.Failure(source, $"invalid JSON: {ex.Message}");
        }
        catch (Exception ex) when (ex is KeyNotFoundException || ex is InvalidOperationException || ex is FormatException)
        {
            return AuthoringResult<Layout>.Failure(source, $"malformed layout: {ex.Message}");
        }
    }

    private static AuthoringResult<Layout> Map(JsonElement root, string source)
    {
        var formatVersion = root.GetProperty("formatVersion").GetInt32();
        if (formatVersion != Layout.CurrentFormatVersion)
        {
            return AuthoringResult<Layout>.Failure(
                source,
                $"unsupported formatVersion {formatVersion}; this Mortar reads {Layout.CurrentFormatVersion}");
        }
        var cells = ReadCells(root);
        var problem = CheckCells(cells);
        if (problem is not null)
        {
            return AuthoringResult<Layout>.Failure(source, $"malformed layout: {problem}");
        }
        var layout = new Layout(
            root.GetProperty("areaId").GetString() ?? "",
            root.GetProperty("seed").GetUInt32(),
            root.GetProperty("attempt").GetInt32(),
            cells,
            ReadMarkers(root),
            root.GetProperty("mortarVersion").GetString() ?? "",
            formatVersion);
        var stored = root.GetProperty("fingerprint").GetString();
        return stored == layout.Fingerprint
            ? AuthoringResult<Layout>.Success(layout)
            : AuthoringResult<Layout>.Failure(source, $"fingerprint mismatch: file has {stored}, contents hash to {layout.Fingerprint}");
    }

    private static LayoutCells ReadCells(JsonElement root)
    {
        var cells = root.GetProperty("cells");
        return new LayoutCells(
            root.GetProperty("width").GetInt32(),
            root.GetProperty("height").GetInt32(),
            Strings(root.GetProperty("groundTypes")),
            Ints(cells.GetProperty("ground")),
            Strings(root.GetProperty("tileTypes")),
            Ints(cells.GetProperty("tile")),
            Ints(cells.GetProperty("variant")),
            Ints(root.GetProperty("walkable")).Select(value => value != 0).ToArray());
    }

    private static string? CheckCells(LayoutCells cells)
    {
        if (cells.Width <= 0 || cells.Height <= 0) return "width and height must be positive";
        var count = cells.Width * cells.Height;
        if (cells.Ground.Count != count || cells.Tiles.Count != count || cells.Variants.Count != count || cells.Walkable.Count != count)
            return $"cell arrays must each hold width x height = {count} entries";
        if (cells.Ground.Any(g => g < 0 || g >= cells.GroundTypes.Count)) return "ground index out of range";
        if (cells.Tiles.Any(t => t < 0 || t >= cells.TileTypes.Count)) return "tile index out of range";
        return null;
    }

    private static List<Marker> ReadMarkers(JsonElement root) =>
        root.GetProperty("markers").EnumerateArray()
            .Select(m => new Marker(
                m.GetProperty("kind").GetString() ?? "",
                m.TryGetProperty("node", out var node) ? node.GetString() : null,
                m.GetProperty("x").GetInt32(),
                m.GetProperty("y").GetInt32()))
            .ToList();

    private static string[] Strings(JsonElement array) =>
        array.EnumerateArray().Select(element => element.GetString() ?? "").ToArray();

    private static int[] Ints(JsonElement array) =>
        array.EnumerateArray().Select(element => element.GetInt32()).ToArray();
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Mortar.slnx`
Expected: PASS (all tests, including 6 layout JSON tests).

- [ ] **Step 6: Commit**

```powershell
git add src/Mortar.Json tests/Mortar.Tests
git commit -m "add layout file writer and reader"
```

---

### Task 11: `mortar` CLI with generate and preview

**Files:**
- Create: `src/Mortar.Cli/Mortar.Cli.csproj`, `Program.cs`, `CliApp.cs`, `CliOptions.cs`, `ExitCodes.cs`, `OutputFile.cs`, `GenerateCommand.cs`, `PreviewCommand.cs`
- Create: `src/Mortar.Cli/Preview/Crc32.cs`, `PngEncoder.cs`, `RgbImage.cs`, `MapRenderer.cs`
- Test: `tests/Mortar.Tests/Cli/PngEncoderTests.cs`, `tests/Mortar.Tests/Cli/CliTests.cs`

**Interfaces:**
- Consumes: `MortarLoader.LoadArea` (Task 8); `Generator.Generate` (Task 9); `LayoutWriter.ToJson`, `LayoutReader.FromFile` (Task 10); `Layout`, `Marker` (Task 4); `Hashing.Fnv1a64` (Task 1).
- Produces:
  - `CliApp.Run(string[] args, TextWriter output, TextWriter error) : int`.
  - `ExitCodes.Success` (0), `ExitCodes.GenerationFailed` (1), `ExitCodes.InvalidInput` (2).
  - `internal PngEncoder.EncodeRgb(int width, int height, byte[] rgb) : byte[]`; `internal Crc32.Compute(params byte[][] parts) : uint`.
  - Commands: `mortar generate --area <file> --seed <n> --out <layout.json>`, `mortar preview --layout <layout.json> --out <map.png> [--scale <1-64>]` (default scale 8 pixels per cell).

- [ ] **Step 1: Create the project**

```powershell
dotnet new console -n Mortar.Cli -o src/Mortar.Cli -f net10.0
dotnet add src/Mortar.Cli reference src/Mortar.Core src/Mortar.Json
dotnet sln Mortar.slnx add src/Mortar.Cli/Mortar.Cli.csproj
dotnet add tests/Mortar.Tests reference src/Mortar.Cli
```

Replace `src/Mortar.Cli/Mortar.Cli.csproj` with (keep the `ProjectReference` items the command added):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <AssemblyName>mortar</AssemblyName>
    <RootNamespace>Mortar.Cli</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Mortar.Tests" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Mortar.Core\Mortar.Core.csproj" />
    <ProjectReference Include="..\Mortar.Json\Mortar.Json.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Write the failing tests**

`tests/Mortar.Tests/Cli/PngEncoderTests.cs`:

```csharp
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Mortar.Cli.Preview;

namespace Mortar.Tests.Cli;

public class PngEncoderTests
{
    [Fact]
    public void Crc32_MatchesReferenceVector() =>
        Assert.Equal(0xCBF43926u, Crc32.Compute(Encoding.ASCII.GetBytes("123456789")));

    [Fact]
    public void EncodeRgb_ProducesDecodableImage()
    {
        var png = PngEncoder.EncodeRgb(2, 1, new byte[] { 255, 0, 0, 0, 0, 255 });
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png.Take(8));
        Assert.Equal("IHDR", Encoding.ASCII.GetString(png, 12, 4));
        Assert.Equal(2, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)));
        Assert.Equal(1, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20)));
        Assert.Equal("IDAT", Encoding.ASCII.GetString(png, 37, 4));
        var idatLength = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(33));
        using var inflater = new ZLibStream(new MemoryStream(png, 41, idatLength), CompressionMode.Decompress);
        using var raw = new MemoryStream();
        inflater.CopyTo(raw);
        Assert.Equal(new byte[] { 0, 255, 0, 0, 0, 0, 255 }, raw.ToArray());
    }
}
```

`tests/Mortar.Tests/Cli/CliTests.cs`:

```csharp
using System.Buffers.Binary;
using Mortar.Cli;
using Mortar.Json;
using Mortar.Tests.Support;

namespace Mortar.Tests.Cli;

public class CliTests
{
    private static (int Code, string Output, string Error) Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var code = CliApp.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }

    [Fact]
    public void Generate_WritesReadableLayout()
    {
        using var temp = new TempDir();
        var outPath = Path.Combine(temp.Root, "layout.json");
        var (code, output, _) = Run("generate", "--area", TestPaths.CryptArea, "--seed", "1", "--out", outPath);
        Assert.Equal(ExitCodes.Success, code);
        Assert.Contains("fingerprint", output);
        Assert.True(LayoutReader.FromFile(outPath).Succeeded);
    }

    [Fact]
    public void Generate_CreatesMissingOutputFolder()
    {
        using var temp = new TempDir();
        var outPath = Path.Combine(temp.Root, "nested", "deeper", "layout.json");
        Assert.Equal(ExitCodes.Success, Run("generate", "--area", TestPaths.CryptArea, "--seed", "1", "--out", outPath).Code);
        Assert.True(File.Exists(outPath));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("4294967296")]
    public void Generate_RejectsBadSeeds(string seed)
    {
        var (code, _, error) = Run("generate", "--area", TestPaths.CryptArea, "--seed", seed, "--out", "unused.json");
        Assert.Equal(ExitCodes.InvalidInput, code);
        Assert.Contains("--seed must be a whole number", error);
    }

    [Fact]
    public void Generate_RequiresOut()
    {
        var (code, _, error) = Run("generate", "--area", TestPaths.CryptArea, "--seed", "1");
        Assert.Equal(ExitCodes.InvalidInput, code);
        Assert.Contains("missing --out", error);
    }

    [Fact]
    public void Generate_ReportsAuthoringErrors()
    {
        using var temp = new TempDir();
        var missing = Path.Combine(temp.Root, "missing.json");
        var (code, _, error) = Run("generate", "--area", missing, "--seed", "1", "--out", Path.Combine(temp.Root, "out.json"));
        Assert.Equal(ExitCodes.InvalidInput, code);
        Assert.Contains("missing.json", error);
    }

    [Fact]
    public void Generate_ImpossibleArea_ExitsOneWithoutOutput()
    {
        using var temp = new TempDir();
        var area = temp.Write("area.json", SampleArea.AreaJson(SampleArea.Tileset, SampleArea.Rooms, tolerance: 0));
        var outPath = Path.Combine(temp.Root, "layout.json");
        var (code, _, error) = Run("generate", "--area", area, "--seed", "1", "--out", outPath);
        Assert.Equal(ExitCodes.GenerationFailed, code);
        Assert.Contains("generation failed after 5 attempts: too small: ", error);
        Assert.False(File.Exists(outPath));
    }

    [Fact]
    public void Preview_WritesPngAtScale()
    {
        using var temp = new TempDir();
        var layoutPath = Path.Combine(temp.Root, "layout.json");
        var pngPath = Path.Combine(temp.Root, "map.png");
        Run("generate", "--area", TestPaths.CryptArea, "--seed", "1", "--out", layoutPath);
        Assert.Equal(ExitCodes.Success, Run("preview", "--layout", layoutPath, "--out", pngPath).Code);
        var png = File.ReadAllBytes(pngPath);
        Assert.Equal(57 * 8, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)));
    }

    [Fact]
    public void Preview_RejectsBadScale()
    {
        var (code, _, error) = Run("preview", "--layout", "layout.json", "--out", "map.png", "--scale", "0");
        Assert.Equal(ExitCodes.InvalidInput, code);
        Assert.Contains("--scale must be a whole number", error);
    }

    [Fact]
    public void UnknownCommand_ExitsTwo()
    {
        var (code, _, error) = Run("stats");
        Assert.Equal(ExitCodes.InvalidInput, code);
        Assert.Contains("unknown command 'stats'", error);
    }

    [Fact]
    public void NoArguments_PrintsUsage()
    {
        var (code, _, error) = Run();
        Assert.Equal(ExitCodes.InvalidInput, code);
        Assert.Contains("Usage:", error);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test Mortar.slnx`
Expected: build FAILS with `The type or namespace name 'CliApp' could not be found`.

- [ ] **Step 4: Implement the PNG preview pieces**

`src/Mortar.Cli/Preview/Crc32.cs`:

```csharp
namespace Mortar.Cli.Preview;

internal static class Crc32
{
    private static readonly uint[] Table = BuildTable();

    public static uint Compute(params byte[][] parts)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var part in parts)
        {
            foreach (var value in part)
            {
                crc = Table[(crc ^ value) & 0xFF] ^ (crc >> 8);
            }
        }
        return crc ^ 0xFFFFFFFFu;
    }

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }
            table[n] = c;
        }
        return table;
    }
}
```

`src/Mortar.Cli/Preview/PngEncoder.cs`:

```csharp
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Mortar.Cli.Preview;

internal static class PngEncoder
{
    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    public static byte[] EncodeRgb(int width, int height, byte[] rgb)
    {
        using var output = new MemoryStream();
        output.Write(Signature);
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0), width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;
        header[9] = 2;
        WriteChunk(output, "IHDR", header);
        WriteChunk(output, "IDAT", Compress(width, height, rgb));
        WriteChunk(output, "IEND", Array.Empty<byte>());
        return output.ToArray();
    }

    private static byte[] Compress(int width, int height, byte[] rgb)
    {
        using var buffer = new MemoryStream();
        using (var zlib = new ZLibStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
        {
            var stride = width * 3;
            for (var y = 0; y < height; y++)
            {
                zlib.WriteByte(0);
                zlib.Write(rgb, y * stride, stride);
            }
        }
        return buffer.ToArray();
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        output.Write(number);
        var typeBytes = Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes);
        output.Write(data);
        BinaryPrimitives.WriteUInt32BigEndian(number, Crc32.Compute(typeBytes, data));
        output.Write(number);
    }
}
```

`src/Mortar.Cli/Preview/RgbImage.cs`:

```csharp
namespace Mortar.Cli.Preview;

internal readonly record struct Rgb(byte R, byte G, byte B);

internal sealed class RgbImage
{
    public RgbImage(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new byte[width * height * 3];
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] Pixels { get; }

    public void FillRect(int x, int y, int width, int height, Rgb color)
    {
        for (var py = Math.Max(0, y); py < Math.Min(Height, y + height); py++)
        {
            for (var px = Math.Max(0, x); px < Math.Min(Width, x + width); px++)
            {
                var index = (py * Width + px) * 3;
                Pixels[index] = color.R;
                Pixels[index + 1] = color.G;
                Pixels[index + 2] = color.B;
            }
        }
    }
}
```

`src/Mortar.Cli/Preview/MapRenderer.cs`:

```csharp
using Mortar.Core.Layouts;
using Mortar.Core.Rng;

namespace Mortar.Cli.Preview;

internal static class MapRenderer
{
    private static readonly Dictionary<string, Rgb> GroundColors = new()
    {
        ["void"] = new Rgb(18, 18, 18),
        ["floor"] = new Rgb(196, 186, 166),
        ["wall"] = new Rgb(72, 66, 60),
        ["water"] = new Rgb(58, 108, 196),
        ["cliff_high"] = new Rgb(122, 100, 78),
    };

    private static readonly Dictionary<string, Rgb> MarkerColors = new()
    {
        ["entrance"] = new Rgb(46, 204, 64),
        ["exit"] = new Rgb(0, 200, 220),
        ["boss"] = new Rgb(230, 40, 40),
        ["waypoint"] = new Rgb(255, 214, 0),
        ["spawn"] = new Rgb(255, 133, 27),
        ["chest"] = new Rgb(177, 13, 201),
    };

    private static readonly Rgb OtherMarker = new Rgb(255, 255, 255);

    public static RgbImage Render(Layout layout, int scale)
    {
        var image = new RgbImage(layout.Width * scale, layout.Height * scale);
        for (var y = 0; y < layout.Height; y++)
        {
            for (var x = 0; x < layout.Width; x++)
            {
                image.FillRect(x * scale, y * scale, scale, scale, GroundColor(layout.Cells.GroundAt(x, y)));
            }
        }
        var inset = scale / 4;
        var size = scale - 2 * inset;
        foreach (var marker in layout.Markers)
        {
            var color = MarkerColors.TryGetValue(marker.Kind, out var known) ? known : OtherMarker;
            image.FillRect(marker.X * scale + inset, marker.Y * scale + inset, size, size, color);
        }
        return image;
    }

    private static Rgb GroundColor(string groundId)
    {
        if (GroundColors.TryGetValue(groundId, out var color))
        {
            return color;
        }
        var hash = Hashing.Fnv1a64(groundId);
        return new Rgb((byte)hash, (byte)(hash >> 8), (byte)(hash >> 16));
    }
}
```

- [ ] **Step 5: Implement the CLI**

`src/Mortar.Cli/ExitCodes.cs`:

```csharp
namespace Mortar.Cli;

public static class ExitCodes
{
    public const int Success = 0;
    public const int GenerationFailed = 1;
    public const int InvalidInput = 2;
}
```

`src/Mortar.Cli/CliOptions.cs`:

```csharp
namespace Mortar.Cli;

internal sealed class CliOptions
{
    private readonly Dictionary<string, string> _values;

    private CliOptions(Dictionary<string, string> values)
    {
        _values = values;
    }

    public static CliOptions? Parse(IReadOnlyList<string> args, int start, TextWriter error)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = start; i < args.Count; i += 2)
        {
            var name = args[i];
            if (!name.StartsWith("--", StringComparison.Ordinal) || i + 1 >= args.Count)
            {
                error.WriteLine($"error: expected '--option value' but got '{name}'");
                return null;
            }
            values[name[2..]] = args[i + 1];
        }
        return new CliOptions(values);
    }

    public string? Get(string name) => _values.TryGetValue(name, out var value) ? value : null;

    public string? Require(string name, TextWriter error)
    {
        var value = Get(name);
        if (value is null)
        {
            error.WriteLine($"error: missing --{name}");
        }
        return value;
    }
}
```

`src/Mortar.Cli/OutputFile.cs`:

```csharp
namespace Mortar.Cli;

internal static class OutputFile
{
    public static bool TryWrite(string path, byte[] contents, TextWriter error)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllBytes(fullPath, contents);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"error: cannot write {path}: {ex.Message}");
            return false;
        }
    }
}
```

`src/Mortar.Cli/GenerateCommand.cs`:

```csharp
using System.Globalization;
using System.Text;
using Mortar.Core.Generation;
using Mortar.Json;

namespace Mortar.Cli;

internal static class GenerateCommand
{
    public static int Run(CliOptions options, TextWriter output, TextWriter error)
    {
        var areaPath = options.Require("area", error);
        var seedText = options.Require("seed", error);
        var outPath = options.Require("out", error);
        if (areaPath is null || seedText is null || outPath is null) return ExitCodes.InvalidInput;
        if (!uint.TryParse(seedText, NumberStyles.None, CultureInfo.InvariantCulture, out var seed))
        {
            error.WriteLine($"error: --seed must be a whole number from 0 to {uint.MaxValue}");
            return ExitCodes.InvalidInput;
        }
        var area = MortarLoader.LoadArea(areaPath);
        if (!area.Succeeded)
        {
            foreach (var problem in area.Errors) error.WriteLine($"error: {problem}");
            return ExitCodes.InvalidInput;
        }
        var result = Generator.Generate(area.Value!, seed);
        if (!result.Succeeded)
        {
            error.WriteLine($"generation failed after {result.Attempts} attempts: {result.FailureSummary}");
            return ExitCodes.GenerationFailed;
        }
        var layout = result.Layout!;
        if (!OutputFile.TryWrite(outPath, Encoding.UTF8.GetBytes(LayoutWriter.ToJson(layout)), error))
        {
            return ExitCodes.InvalidInput;
        }
        output.WriteLine($"wrote {outPath} (attempt {layout.Attempt}, fingerprint {layout.Fingerprint})");
        return ExitCodes.Success;
    }
}
```

`src/Mortar.Cli/PreviewCommand.cs`:

```csharp
using System.Globalization;
using Mortar.Cli.Preview;
using Mortar.Json;

namespace Mortar.Cli;

internal static class PreviewCommand
{
    private const int DefaultScale = 8;
    private const int MaxScale = 64;

    public static int Run(CliOptions options, TextWriter output, TextWriter error)
    {
        var layoutPath = options.Require("layout", error);
        var outPath = options.Require("out", error);
        if (layoutPath is null || outPath is null) return ExitCodes.InvalidInput;
        var scale = ParseScale(options.Get("scale"), error);
        if (scale is null) return ExitCodes.InvalidInput;
        var layout = LayoutReader.FromFile(layoutPath);
        if (!layout.Succeeded)
        {
            foreach (var problem in layout.Errors) error.WriteLine($"error: {problem}");
            return ExitCodes.InvalidInput;
        }
        var image = MapRenderer.Render(layout.Value!, scale.Value);
        if (!OutputFile.TryWrite(outPath, PngEncoder.EncodeRgb(image.Width, image.Height, image.Pixels), error))
        {
            return ExitCodes.InvalidInput;
        }
        output.WriteLine($"wrote {outPath} ({image.Width}x{image.Height} px)");
        return ExitCodes.Success;
    }

    private static int? ParseScale(string? text, TextWriter error)
    {
        if (text is null) return DefaultScale;
        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var scale) && scale >= 1 && scale <= MaxScale)
        {
            return scale;
        }
        error.WriteLine($"error: --scale must be a whole number from 1 to {MaxScale}");
        return null;
    }
}
```

`src/Mortar.Cli/CliApp.cs`:

```csharp
namespace Mortar.Cli;

public static class CliApp
{
    private const string Usage = """
        Usage:
          mortar generate --area <area.json> --seed <n> --out <layout.json>
          mortar preview --layout <layout.json> --out <map.png> [--scale <pixels per cell>]
        """;

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0)
        {
            error.WriteLine(Usage);
            return ExitCodes.InvalidInput;
        }
        if (args[0] is "help" or "--help" or "-h")
        {
            output.WriteLine(Usage);
            return ExitCodes.Success;
        }
        var options = CliOptions.Parse(args, 1, error);
        if (options is null) return ExitCodes.InvalidInput;
        return args[0] switch
        {
            "generate" => GenerateCommand.Run(options, output, error),
            "preview" => PreviewCommand.Run(options, output, error),
            _ => UnknownCommand(args[0], error),
        };
    }

    private static int UnknownCommand(string name, TextWriter error)
    {
        error.WriteLine($"error: unknown command '{name}'");
        error.WriteLine(Usage);
        return ExitCodes.InvalidInput;
    }
}
```

Replace `src/Mortar.Cli/Program.cs` with:

```csharp
return Mortar.Cli.CliApp.Run(args, Console.Out, Console.Error);
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test Mortar.slnx`
Expected: PASS (all tests, including 2 PNG and 12 CLI tests).

- [ ] **Step 7: Try the CLI by hand**

```powershell
dotnet run --project src/Mortar.Cli -- generate --area samples/areas/crypt.json --seed 1 --out out/crypt-1.json
dotnet run --project src/Mortar.Cli -- preview --layout out/crypt-1.json --out out/crypt-1.png
```

Expected: both print `wrote ...`. Open `out/crypt-1.png` and confirm it shows rooms connected by doors with colored markers. Then delete the `out` folder (it is not committed).

- [ ] **Step 8: Commit**

```powershell
git add src/Mortar.Cli tests/Mortar.Tests Mortar.slnx
git commit -m "add mortar cli with generate and preview"
```

---

### Task 12: Docs, license, and cross-platform CI

**Files:**
- Create: `LICENSE`, `README.md`, `CONTRIBUTING.md`, `docs/authoring.md`, `.github/workflows/ci.yml`
- Modify: `.gitignore` (add `out/`)

**Interfaces:**
- Consumes: everything above (documents the formats from Tasks 2–4, 8, and 10).
- Produces: nothing used by code.

- [ ] **Step 1: Add the license**

`LICENSE`:

```
MIT License

Copyright (c) 2026 Mortar contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

- [ ] **Step 2: Add the README**

`README.md`:

````markdown
# Mortar

Mortar generates replayable, seed-based levels for action RPGs from hand-made pieces. You draw rooms as text, describe each area's overall structure, and Mortar turns a seed into a finished layout. The same seed always produces the same level on Windows, Linux, and macOS.

Mortar is free and open source under the MIT license.

## Status

Version 0.1 generates **indoor** levels by stitching hand-made rooms along a landmark graph. Outdoor generation, the Unity package, and the Unreal plugin are planned (see `docs/design.md`).

## Quick start

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build
dotnet run --project src/Mortar.Cli -- generate --area samples/areas/crypt.json --seed 1 --out crypt-1.json
dotnet run --project src/Mortar.Cli -- preview --layout crypt-1.json --out crypt-1.png
```

`generate` exits with `0` on success, `1` if every attempt failed (it prints counted reasons such as `too small: 6`), and `2` for bad input such as an invalid room file.

## Using the library

```csharp
using Mortar.Core.Generation;
using Mortar.Json;

var area = MortarLoader.LoadArea("samples/areas/crypt.json");
if (!area.Succeeded)
{
    foreach (var error in area.Errors) Console.Error.WriteLine(error);
    return;
}

var result = Generator.Generate(area.Value!, seed: 1);
if (result.Succeeded)
{
    File.WriteAllText("crypt-1.json", LayoutWriter.ToJson(result.Layout!));
}
```

`Mortar.Core` and `Mortar.Json` target .NET Standard 2.1, so they also run inside Unity.

## Repository layout

| Path | Purpose |
| --- | --- |
| `src/Mortar.Core` | The generator. No engine or file-format dependencies. |
| `src/Mortar.Json` | Reads tileset, room, and area files; writes and reads layout files. |
| `src/Mortar.Cli` | The `mortar` command-line tool. |
| `samples/` | A sample tileset, rooms, and the `crypt` indoor area. |
| `tests/` | Unit, determinism snapshot, walkability, and success-rate tests. |
| `docs/` | Design and the authoring guide. |

See `docs/authoring.md` to write your own tilesets, rooms, and areas.

## License

MIT. See `LICENSE`.
````

- [ ] **Step 3: Add contribution notes**

`CONTRIBUTING.md`:

```markdown
# Contributing to Mortar

## Build and test

    dotnet build
    dotnet test

CI runs the tests on Windows, Linux, and macOS. All three must produce identical snapshots.

## Determinism rules

Mortar promises the same layout for the same inputs and seed on every platform. Changes to `Mortar.Core` must keep these rules:

- Use `Pcg32` for every random decision. Never use `System.Random`.
- Never let hash-set or dictionary enumeration order drive a decision. Build candidate lists from ordered lists, and sort by id with `StringComparer.Ordinal`.
- Use integer arithmetic for decisions. Floating point is only for turning home-region fractions into slot indices.
- Pass the same `Pcg32` through generation in a fixed order; adding or removing a random call changes every layout after it.

## Snapshots

`tests/Mortar.Tests/Snapshots` holds text renderings of the sample crypt for fixed seeds. If you change generation on purpose, delete the affected snapshot files, run `dotnet test` once to recreate them, review the new maps, and commit them with your change.

## Style

- Keep files under 300 lines and functions under 50 lines.
- The library returns results for authoring errors and generation failures; it does not throw for them.
- Commit messages are short, one line, and present tense, for example `add corridor rooms to sample`.
```

- [ ] **Step 4: Add the authoring guide**

`docs/authoring.md`:

````markdown
# Authoring Mortar content

Mortar reads three kinds of files: a **tileset**, **rooms**, and **areas**. It writes **layout** files. All are JSON and are easy to review in git. Comments (`//`) and trailing commas are allowed; unknown properties are errors so typos are caught.

## Tileset

```json
{
  "groundTypes": [
    { "id": "void", "char": " ", "walkable": false, "blocksSight": false },
    { "id": "floor", "char": ".", "walkable": true, "blocksSight": false },
    { "id": "wall", "char": "#", "walkable": false, "blocksSight": true }
  ],
  "variantWeights": { "floor": [6, 1, 1] }
}
```

- Every tileset needs `void`, `floor` (walkable), and `wall` (not walkable).
- Each `char` must be a single character and unique. `D`, `E`, `X`, `B`, `W`, `S`, and `C` are reserved for doors and markers.
- `variantWeights` maps a tile type (see below) to relative weights for its art variants. Tile types without weights always use variant `0`.

## Rooms

```json
{
  "id": "filler_hall",
  "tags": ["filler"],
  "grid": [
    "####D####",
    "#.......#",
    "#.......#",
    "#.......#",
    "D...S...D",
    "#.......#",
    "#.......#",
    "#.......#",
    "####D####"
  ]
}
```

- Grid characters are ground types from the tileset, plus doors and markers.
- `D` is a possible door. It must sit on the room's outer edge, not on a corner. A door becomes floor only when a neighboring room has a door on the same cell and the generator connects the two rooms; otherwise it becomes wall.
- Put doors at the middle of each slot side (offset 4 with `slotSize` 9) so rooms line up with each other.
- Markers sit on floor cells and never on the edge: `E` entrance, `X` exit, `B` boss, `W` waypoint, `S` monster spawn, `C` chest.
- Width and height must each be `(slotSize - 1) × n + 1`: 9, 17, 25… for `slotSize` 9. Neighboring rooms share their edge cells.
- Standard tags: `entrance`, `exit`, `boss`, `waypoint` (the room must contain the matching marker), `filler` and `corridor` (fill the path between landmarks; must be one slot), `dead_end` (preferred for side-branch tips). Any other tag can be used by a custom landmark kind.
- Multi-slot rooms (17×17 and up) can be landmark rooms. Doors on a multi-slot room belong to the slot side they sit on.

## Areas

```json
{
  "id": "crypt",
  "mode": "indoor",
  "tileset": "../tileset.json",
  "targetSize": { "width": 60, "height": 60, "tolerance": 15 },
  "slotSize": 9,
  "rooms": ["../rooms"],
  "roomTags": ["crypt"],
  "maxSideBranches": 4,
  "maxAttempts": 20,
  "graph": {
    "nodes": [
      { "id": "entrance", "kind": "entrance", "region": { "x": [0.0, 0.1], "y": [0.3, 0.7] } }
    ],
    "edges": [
      { "from": "entrance", "to": "boss", "kind": "path" }
    ]
  }
}
```

- `mode`: only `indoor` is supported in this version.
- `tileset` and `rooms` are paths relative to the area file. A `rooms` entry can be a file or a folder of `.json` files.
- `roomTags` (optional) keeps only rooms that carry at least one of these tags.
- `targetSize`: the level is built on a grid of `(width - 1) / (slotSize - 1)` slots per side. After generation, the walkable area must measure within `tolerance` percent of `width` and `height`, or the attempt is retried.
- `maxSideBranches`: up to this many optional dead-end rooms are added per attempt.
- `maxAttempts`: retry limit (default 20).
- `nodes`: landmarks. `kind` defaults to the node `id`. `region` is the home rectangle in fractions of the area; the landmark's room is placed on a slot inside it. Exactly one node must be an `entrance`.
- `edges`: `path` edges are required connections; every node must reach the entrance through them. `branch` edges are each included with a 50% chance and dropped if no route exists.

## Layout files

`mortar generate` writes compact JSON with these properties, in order:

| Property | Meaning |
| --- | --- |
| `formatVersion` | `1` for this version. |
| `mortarVersion` | Library version that wrote the file. |
| `areaId`, `seed`, `attempt` | Inputs that reproduce the layout. `seed` is 0–4294967295. |
| `width`, `height` | Size in cells. |
| `groundTypes`, `tileTypes` | Palettes; cells store indices into them. |
| `cells.ground`, `cells.tile`, `cells.variant` | One entry per cell, row-major from the top-left. |
| `walkable` | One `0`/`1` per cell, row-major. |
| `markers` | `kind`, `node` (for landmarks), `x`, `y`. |
| `fingerprint` | Hash that confirms two machines built the same level. |

### Tile types

Each cell's tile type is derived from the ground at four corner points: the cell itself (NW), the cell to its east (NE), the cell to its south (SW), and the cell to its south-east (SE). Cells outside the grid count as `void`. Engines should place each tile offset by half a cell toward +x and +y so it sits between those four points.

- All four corners the same: the ground id, for example `floor`.
- Two ground types: `{base}-{other}:{mask}`, where `base` is the ground listed first in the tileset and `mask` lists NW, NE, SW, SE with `1` where `other` sits. `floor-water:1100` is water along the north edge.
- Three or more: the four corner ids joined by `|` in NW, NE, SW, SE order.

### Fingerprint

The fingerprint is 64-bit FNV-1a (offset `14695981039346656037`, prime `1099511628211`) over, in order: the area id as UTF-8 followed by a zero byte; `seed`, `attempt`, `width`, and `height` as 4-byte little-endian unsigned integers; then for every cell in row-major order, its tile type name as UTF-8 followed by a zero byte and its variant as a 4-byte little-endian unsigned integer. It is written as 16 lowercase hex digits.
````

- [ ] **Step 5: Add CI and ignore CLI output**

`.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [main, dev]
  pull_request:

jobs:
  test:
    strategy:
      fail-fast: false
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet test Mortar.slnx --configuration Release
```

Append to `.gitignore`:

```
# CLI output
out/
```

- [ ] **Step 6: Run the full suite in Release**

Run: `dotnet test Mortar.slnx --configuration Release`
Expected: PASS (all tests), and no build warnings.

- [ ] **Step 7: Commit**

```powershell
git add LICENSE README.md CONTRIBUTING.md docs/authoring.md .github .gitignore
git commit -m "add docs, license, and cross-platform ci"
```
