# Cathedral Levels and Town Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate cathedral dungeon levels 1–4 and a hand-drawn town with `Mortar.Core`, and make the town playable as depth 0 in `Mortar.Game`.

**Architecture:** Four small Core features (room weights, multi-slot filler rooms, custom tileset markers, a `fixed` area mode) let the existing indoor room stitching build cathedral levels from new content files and stamp a hand-drawn town map. `Mortar.Json` reads the new fields and fixed areas. `Mortar.Game` treats an optional town as depth 0: new games start there and its exit leads to depth 1.

**Tech Stack:** C# 12, .NET Standard 2.1 (`Mortar.Core`, `Mortar.Json`, `Mortar.Game`), .NET 10 (CLI, tests), System.Text.Json, xUnit.

**Spec:** `docs/superpowers/specs/2026-10-14-cathedral-and-town-design.md` (approved). Builds on milestone 1b (`docs/superpowers/plans/2026-10-14-items-and-saves.md`, complete).

## Global Constraints

- `Mortar.Core` has no engine or file-format dependencies. `Mortar.Game` references only `Mortar.Core` and does no file IO.
- Never use `System.Random`. Generation randomness comes from `Pcg32`.
- No floating point in decisions. No dictionary or hash-set enumeration in decision paths.
- Existing sample layouts must not change: `tests/Mortar.Tests/Snapshots/crypt-seed-*.txt` stay byte-identical through every task.
- Invalid content produces `AuthoringResult` errors that name the file; nothing throws.
- Clean-room: no code, formulas, data, or names from devilutionX or the original game. No Blizzard or Diablo names anywhere.
- No file over 300 lines; no function over 50 lines.
- `Nullable` enabled and warnings treated as errors (already set in `Directory.Build.props`).
- Commit messages are short, one line, present tense, no prefixes.
- Stage files by explicit path. Never stage `.env` files, anything under `.superpowers/`, or `bin/`/`obj/` output.

## Design Interpretations

The spec leaves these choices open; this plan fixes them. Review them before execution.

1. **The town is optional in `ContentSet`.** Content with a depth-0 level has a town; content without one starts on depth 1 as today, so existing gameplay tests keep working unchanged. A new `ContentSet.ShallowestLevel` (0 or 1) drives `Level(depth)` indexing, the stairs' upper bound, save restore, and where `World.Create` starts.
2. **Target sizes are `8n + 1`** (49, 57, 65, 73) so the stride-8 slot grid covers the whole target. With 48 the grid is one slot short and every attempt fails as too small. The spec table was updated to match.
3. **A fixed map file is a room-format file with only `grid` used.** `id`, `tags`, and `weight` in it are ignored. Fixed-map markers have no node id.
4. **"A marker not on a walkable cell" cannot happen**: marker cells always become `floor`, which every tileset requires to be walkable. There is no separate check.
5. **Dead-end preference per candidate:** dead-end rooms that fit and have exactly one opening along their outer edge are picked first; otherwise any fitting filler or corridor room. For single-slot rooms this is exactly today's rule.
6. **Door matching is checked per opening** (the door in that side segment must meet a door of the placed neighbour). For single-slot rooms this gives the same answers as today's any-door check, so sample layouts are unchanged.
7. **`MonsterSpawner` skips spawn markers on a level with no spawn entries**, so a town map with an `S` cannot crash the spawner.
8. **The stairs message becomes "there is nothing above"** for every top level (the town, or depth 1 without a town).
9. **`LevelEntry` removes the player from the level it was standing on before entering**, captured before the new level is built. This replaces the `world.Depth != 0` sentinel.
10. **Town ground characters:** `,` grass, `=` road, `~` water, `T` tree. Markers `F` smith, `H` healer, `G` sage are recorded for later milestones and do nothing yet.
11. **The `levels.json` town entry is its own DTO with only `area`**, so `spawns` or `boss` there fail with the standard unknown-property error.
12. **Cathedral room weights:** single-slot fillers 3, corridors 2, multi-slot fillers 1, dead ends and landmarks 1.
13. **`MapRenderer.GroundColor` becomes `internal`** so the hashed fallback colour can be tested.

## Review Focus

1. **Determinism of existing output:** the crypt sample snapshots stay identical after the weight, multi-slot, and marker changes. Pinned by `SnapshotTests` in Tasks 1–3.
2. **Multi-slot fit rules:** every covered slot used and empty, a door on each outer opening that meets placed neighbours, inside openings ignored. Pinned in Task 2.
3. **Fixed areas always generate:** every map problem is a load-time error. Pinned in Tasks 4 and 5.
4. **Cathedral reliability:** each area generates on 500 consecutive seeds. Pinned in Task 6.
5. **Town wiring:** start in town, stairs both ways, nothing above the town, saves in and below the town round-trip. Pinned in Tasks 8 and 9.

## File Structure

```
src/Mortar.Core/Model/
  RoomDefinition.cs, Room.cs           + Weight (Room.MaxWeight = 1000)
  MarkerType.cs                        new: marker char + kind
  MarkerChars.cs                       built-in markers as a MarkerType list
  Tileset.cs                           + custom markers, TryGetMarkerKind, Markers
  AreaDefinition.cs                    + AreaMode.Fixed, AreaDefinition.Fixed(...)
  FixedMap.cs                          new: stamped map grid + markers
  CompiledArea.cs                      + Map
src/Mortar.Core/Authoring/
  RoomCompiler.cs                      weight bounds
  RoomGridParser.cs                    marker lookup through the tileset
  FixedMapCompiler.cs                  new: parse and check a fixed map
  AreaCompiler.cs                      + CompileFixed
  AreaValidator.cs                     rejects fixed definitions in the indoor path
src/Mortar.Core/Indoor/
  RoomPicker.cs                        new: weighted room pick
  LandmarkPlacer.cs                    uses RoomPicker
  RoomFiller.cs                        multi-slot fillers
  DoorGeometry.cs                      DoorsMeetOn replaces DoorsMeet
src/Mortar.Core/Validation/LayoutValidator.cs   FloodFill/VoidUnreached internal
src/Mortar.Core/Generation/Generator.cs         fixed path
src/Mortar.Json/
  Dtos.cs                              RoomDto.Weight, TilesetDto.Markers, MarkerDto, AreaDto.Map, nullable MaxSideBranches
  DefinitionMapper.cs                  weights, markers
  AreaMapper.cs                        map only in fixed areas, mode message
  FixedAreaLoader.cs                   new: fixed area loading
  MortarLoader.cs                      dispatches fixed areas
  Content/ContentDtos.cs               LevelsDto.Town, TownDto
  Content/LevelFileLoader.cs           town entry
src/Mortar.Game/
  Content/ContentSet.cs                ShallowestLevel, depth-0 town
  Content/ContentValidator.cs          town rules
  Simulation/LevelEntry.cs, Stairs.cs, World.cs, MonsterSpawner.cs
  Saves/LevelRestorer.cs
src/Mortar.Cli/Preview/MapRenderer.cs  GroundColor internal
content/
  tilesets/cathedral.json, tilesets/town.json
  rooms/cathedral/*.json (22 rooms)
  areas/cathedral-1.json … cathedral-4.json, areas/town.json
  maps/town.json
  levels.json                          town + cathedral areas
tests/Mortar.Tests/
  Indoor/RoomPickerTests.cs, Authoring/FixedMapTests.cs, Json/FixedAreaLoaderTests.cs,
  Generation/ContentAreaTests.cs, Gameplay/TownTests.cs, Cli/MapRendererTests.cs,
  Support/Snapshots.cs, Snapshots/cathedral-*-seed-1.txt, Snapshots/town-seed-1.txt
docs/design.md, docs/authoring.md, docs/content.md, README.md
```

---

### Task 1: Room weights

**Files:**
- Modify: `src/Mortar.Core/Model/RoomDefinition.cs`, `src/Mortar.Core/Model/Room.cs`, `src/Mortar.Core/Authoring/RoomCompiler.cs`, `src/Mortar.Core/Authoring/RoomGridParser.cs`, `src/Mortar.Core/Indoor/LandmarkPlacer.cs`, `src/Mortar.Core/Indoor/RoomFiller.cs`
- Create: `src/Mortar.Core/Indoor/RoomPicker.cs`
- Modify: `src/Mortar.Json/Dtos.cs`, `src/Mortar.Json/DefinitionMapper.cs`
- Modify: `tests/Mortar.Tests/Support/TestContent.cs`
- Test: `tests/Mortar.Tests/Indoor/RoomPickerTests.cs` (create), `tests/Mortar.Tests/Authoring/RoomCompilerTests.cs`, `tests/Mortar.Tests/Json/MortarLoaderTests.cs`

**Interfaces:**
- Produces: `RoomDefinition(string source, string id, IReadOnlyList<string> tags, IReadOnlyList<string> grid, int weight = 1)` with `Weight`; `Room.Weight`, `Room.MaxWeight = 1000`; internal `RoomPicker.Pick(IReadOnlyList<Room> rooms, Pcg32 rng)`; `RoomDto.Weight` (`int?`).
- Test helpers: `TestContent.RoomDef(id, tags, grid, int weight = 1)`, `TestContent.CompileRoom(id, tags, grid, int weight = 1)`.

- [ ] **Step 1: Extend the test helpers**

In `tests/Mortar.Tests/Support/TestContent.cs`, replace `RoomDef` and `CompileRoom` with:

```csharp
    public static RoomDefinition RoomDef(string id, string[] tags, string[] grid, int weight = 1) =>
        new RoomDefinition($"{id}.json", id, tags, grid, weight);

    public static Room CompileRoom(string id, string[] tags, string[] grid, int weight = 1)
    {
        var result = RoomCompiler.Compile(RoomDef(id, tags, grid, weight), CreateTileset(), 9);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        return result.Value!;
    }
```

- [ ] **Step 2: Write the failing tests**

Create `tests/Mortar.Tests/Indoor/RoomPickerTests.cs`:

```csharp
using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class RoomPickerTests
{
    private static Room Hall(string id, int weight) =>
        TestContent.CompileRoom(id, new[] { "filler" }, TestContent.Box(9, 9, new[] { 4 }), weight);

    [Fact]
    public void Pick_AllWeightsOne_MatchesUniformPick()
    {
        var rooms = new[] { Hall("a", 1), Hall("b", 1), Hall("c", 1) };
        var picker = new Pcg32(7, 7);
        var uniform = new Pcg32(7, 7);
        for (var i = 0; i < 50; i++)
        {
            Assert.Same(uniform.Pick(rooms), RoomPicker.Pick(rooms, picker));
        }
    }

    [Fact]
    public void Pick_FollowsWeights()
    {
        var rooms = new[] { Hall("light", 1), Hall("heavy", 99) };
        var rng = new Pcg32(3, 3);
        var heavy = Enumerable.Range(0, 1000).Count(_ => RoomPicker.Pick(rooms, rng).Id == "heavy");
        Assert.InRange(heavy, 950, 1000);
    }

    [Fact]
    public void Pick_IsDeterministic()
    {
        var rooms = new[] { Hall("a", 2), Hall("b", 5), Hall("c", 1) };
        var first = new Pcg32(11, 4);
        var second = new Pcg32(11, 4);
        for (var i = 0; i < 50; i++)
        {
            Assert.Same(RoomPicker.Pick(rooms, first), RoomPicker.Pick(rooms, second));
        }
    }
}
```

Add to `tests/Mortar.Tests/Authoring/RoomCompilerTests.cs` (inside the class):

```csharp
    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void Compile_WeightOutOfRange_IsAnError(int weight) =>
        AssertError(
            RoomCompiler.Compile(TestContent.RoomDef("room_a", Array.Empty<string>(), TestContent.Box(9, 9, new[] { 4 }), weight), Tiles, 9),
            "weight must be between 1 and 1000");

    [Fact]
    public void Compile_KeepsTheWeight()
    {
        var definition = TestContent.RoomDef("room_a", Array.Empty<string>(), TestContent.Box(9, 9, new[] { 4 }), 1000);
        Assert.Equal(1000, RoomCompiler.Compile(definition, Tiles, 9).Value!.Weight);
    }
```

Add to `tests/Mortar.Tests/Json/MortarLoaderTests.cs` (inside the class):

```csharp
    [Fact]
    public void LoadArea_ReadsRoomWeights()
    {
        using var temp = new TempDir();
        CopySampleRooms(temp);
        WriteHall(temp, "\"weight\": 5, \"tags\"");
        var area = temp.Write("area.json", SampleArea.AreaJson(SampleArea.Tileset, "rooms"));
        var loaded = MortarLoader.LoadArea(area);
        Assert.True(loaded.Succeeded, string.Join("; ", loaded.Errors));
        Assert.Equal(5, loaded.Value!.Rooms.Single(r => r.Id == "filler_hall").Weight);
    }

    [Fact]
    public void LoadArea_RoomWeightOutOfRange_NamesTheRoomFile()
    {
        using var temp = new TempDir();
        CopySampleRooms(temp);
        var hall = WriteHall(temp, "\"weight\": 0, \"tags\"");
        var area = temp.Write("area.json", SampleArea.AreaJson(SampleArea.Tileset, "rooms"));
        Assert.Contains(MortarLoader.LoadArea(area).Errors, e => e.Source == hall && e.Message.Contains("weight must be between 1 and 1000"));
    }

    private static void CopySampleRooms(TempDir temp)
    {
        foreach (var file in Directory.GetFiles(Path.Combine(TestPaths.Samples, "rooms")))
        {
            temp.Write(Path.Combine("rooms", Path.GetFileName(file)), File.ReadAllText(file));
        }
    }

    private static string WriteHall(TempDir temp, string tagsReplacement)
    {
        var text = File.ReadAllText(Path.Combine(TestPaths.Samples, "rooms", "filler_hall.json"));
        return temp.Write(Path.Combine("rooms", "filler_hall.json"), text.Replace("\"tags\"", tagsReplacement));
    }
```

Also replace the copy loop in the existing `LoadArea_RoomFolder_MatchesJsonExtensionIgnoringCase` with a call to `CopySampleRooms(temp);`.

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~RoomPickerTests|FullyQualifiedName~RoomCompilerTests|FullyQualifiedName~MortarLoaderTests"`
Expected: build FAIL (`RoomPicker`, `Room.Weight`, and the `weight` argument do not exist).

- [ ] **Step 4: Implement weights**

`src/Mortar.Core/Model/RoomDefinition.cs`:

```csharp
namespace Mortar.Core.Model;

public sealed class RoomDefinition
{
    public RoomDefinition(string source, string id, IReadOnlyList<string> tags, IReadOnlyList<string> grid, int weight = 1)
    {
        Source = source;
        Id = id;
        Tags = tags;
        Grid = grid;
        Weight = weight;
    }

    public string Source { get; }

    public string Id { get; }

    public IReadOnlyList<string> Tags { get; }

    public IReadOnlyList<string> Grid { get; }

    public int Weight { get; }
}
```

In `src/Mortar.Core/Model/Room.cs`, add `public const int MaxWeight = 1000;` at the top of the class, add an `int weight` constructor parameter right after `tags` (assign it to a new `public int Weight { get; }` placed after `Tags`):

```csharp
    internal Room(
        string id,
        IReadOnlyList<string> tags,
        int weight,
        int width,
        int height,
        int stride,
        int[] ground,
        IReadOnlyList<Door> doors,
        IReadOnlyList<RoomMarker> markers)
    {
        Id = id;
        Tags = tags;
        Weight = weight;
        Width = width;
        Height = height;
        Stride = stride;
        _ground = ground;
        Doors = doors;
        Markers = markers;
    }
```

In `src/Mortar.Core/Authoring/RoomGridParser.cs`, change the `return` in `Parse()` to:

```csharp
        return new Room(_definition.Id, _definition.Tags, _definition.Weight, _width, _height, _stride, _ground, _doors, _markers);
```

In `src/Mortar.Core/Authoring/RoomCompiler.cs`, add right after the "room has no id" check:

```csharp
        if (definition.Weight < 1 || definition.Weight > Room.MaxWeight)
        {
            errors.Add(new AuthoringError(definition.Source, $"room '{definition.Id}' weight must be between 1 and {Room.MaxWeight}"));
        }
```

Create `src/Mortar.Core/Indoor/RoomPicker.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Indoor;

internal static class RoomPicker
{
    /// <summary>Uses the plain uniform pick when every weight is 1, so layouts from unweighted rooms never change.</summary>
    public static Room Pick(IReadOnlyList<Room> rooms, Pcg32 rng) =>
        rooms.All(room => room.Weight == 1)
            ? rng.Pick(rooms)
            : rooms[rng.PickWeighted(rooms.Select(room => room.Weight).ToList())];
}
```

In `src/Mortar.Core/Indoor/LandmarkPlacer.cs`, replace `var room = rng.Pick(area.RoomsTagged(node.Kind));` with:

```csharp
        var room = RoomPicker.Pick(area.RoomsTagged(node.Kind), rng);
```

In `src/Mortar.Core/Indoor/RoomFiller.cs`, replace `return rng.Pick(endings);` with `return RoomPicker.Pick(endings, rng);` and `rng.Pick(candidates)` with `RoomPicker.Pick(candidates, rng)`.

In `src/Mortar.Json/Dtos.cs`, add to `RoomDto` after `Tags`:

```csharp
    public int? Weight { get; set; }
```

In `src/Mortar.Json/DefinitionMapper.cs`, replace `ToRoom` with:

```csharp
    public static RoomDefinition ToRoom(string source, RoomDto dto) => new RoomDefinition(
        source,
        dto.Id ?? "",
        dto.Tags ?? new List<string>(),
        (dto.Grid ?? new List<string?>()).Select(row => row ?? "").ToList(),
        dto.Weight ?? 1);
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~RoomPickerTests|FullyQualifiedName~RoomCompilerTests|FullyQualifiedName~MortarLoaderTests"`
Expected: PASS. Then run `dotnet test`; expected: all tests pass and the crypt snapshots are unchanged (no snapshot file modified in `git status`).

- [ ] **Step 6: Commit**

```bash
git add src/Mortar.Core/Model/RoomDefinition.cs src/Mortar.Core/Model/Room.cs src/Mortar.Core/Authoring/RoomCompiler.cs src/Mortar.Core/Authoring/RoomGridParser.cs src/Mortar.Core/Indoor/RoomPicker.cs src/Mortar.Core/Indoor/LandmarkPlacer.cs src/Mortar.Core/Indoor/RoomFiller.cs src/Mortar.Json/Dtos.cs src/Mortar.Json/DefinitionMapper.cs tests/Mortar.Tests/Support/TestContent.cs tests/Mortar.Tests/Indoor/RoomPickerTests.cs tests/Mortar.Tests/Authoring/RoomCompilerTests.cs tests/Mortar.Tests/Json/MortarLoaderTests.cs
git commit -m "add room weights"
```

---

### Task 2: Multi-slot filler rooms

**Files:**
- Modify: `src/Mortar.Core/Indoor/RoomFiller.cs`, `src/Mortar.Core/Indoor/DoorGeometry.cs`
- Test: `tests/Mortar.Tests/Indoor/RoomFillerTests.cs`

**Interfaces:**
- Consumes: `RoomPicker.Pick` (Task 1), `SlotGrid`, `Placement`, `Sides`.
- Produces: `RoomFiller.Fits(SlotGrid grid, Room room, int x, int y)` (replaces the overload that took an `open` list); `DoorGeometry.DoorsMeetOn(Placement placement, Side side, int segment, Placement neighbor, int stride)` (replaces `DoorsMeet`).

- [ ] **Step 1: Write the failing tests**

Replace `tests/Mortar.Tests/Indoor/RoomFillerTests.cs` with:

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
    private static readonly Room Nave = TestContent.CompileRoom("nave", new[] { "filler" }, TestContent.Box(17, 9, new[] { 4, 12 }));
    private static readonly Room HalfNave = TestContent.CompileRoom("half", new[] { "filler" }, TestContent.Box(17, 9, new[] { 4 }));
    private static readonly Room SealedNave = TestContent.CompileRoom("sealed", new[] { "filler" }, TestContent.Box(17, 9, Array.Empty<int>(), (4, 0, 'D')));

    private static SlotGrid UsedGrid(int columns, int rows)
    {
        var grid = new SlotGrid(columns, rows, 8);
        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                grid.MarkUsed(x, y);
            }
        }
        return grid;
    }

    [Fact]
    public void Fits_CrossRoomCoversAnyOpenings()
    {
        var grid = UsedGrid(3, 3);
        foreach (var side in Sides.All) grid.Open(1, 1, side);
        Assert.True(RoomFiller.Fits(grid, Cross, 1, 1));
    }

    [Fact]
    public void Fits_RejectsRoomWithoutDoorOnOpenSide()
    {
        var grid = UsedGrid(3, 3);
        grid.Open(1, 1, Side.East);
        Assert.False(RoomFiller.Fits(grid, NorthOnly, 1, 1));
    }

    [Fact]
    public void Fits_RequiresDoorsToLineUpWithPlacedNeighbors()
    {
        var grid = UsedGrid(2, 1);
        grid.Place(new Placement(OffCenter, 1, 0, null));
        grid.Open(0, 0, Side.East);
        Assert.False(RoomFiller.Fits(grid, Cross, 0, 0));
        Assert.True(RoomFiller.Fits(grid, OffCenter, 0, 0));
    }

    [Fact]
    public void Fits_MultiSlotRoomNeedsEveryCoveredSlotUsedAndEmpty()
    {
        var grid = new SlotGrid(3, 1, 8);
        grid.MarkUsed(0, 0);
        Assert.False(RoomFiller.Fits(grid, Nave, 0, 0));
        grid.MarkUsed(1, 0);
        Assert.True(RoomFiller.Fits(grid, Nave, 0, 0));
        grid.Place(new Placement(Cross, 1, 0, null));
        Assert.False(RoomFiller.Fits(grid, Nave, 0, 0));
        Assert.False(RoomFiller.Fits(grid, Nave, 2, 0));
    }

    [Fact]
    public void Fits_MultiSlotRoomNeedsADoorOnEachOuterOpening()
    {
        var grid = UsedGrid(2, 2);
        grid.Open(1, 0, Side.South);
        Assert.False(RoomFiller.Fits(grid, HalfNave, 0, 0));
        Assert.True(RoomFiller.Fits(grid, Nave, 0, 0));
    }

    [Fact]
    public void Fits_IgnoresOpeningsInsideTheRoom()
    {
        var grid = UsedGrid(2, 1);
        grid.Open(0, 0, Side.East);
        Assert.True(RoomFiller.Fits(grid, SealedNave, 0, 0));
    }

    [Fact]
    public void Fits_MultiSlotDoorsMustMeetPlacedNeighbors()
    {
        var misaligned = UsedGrid(3, 1);
        misaligned.Place(new Placement(OffCenter, 2, 0, null));
        misaligned.Open(1, 0, Side.East);
        Assert.False(RoomFiller.Fits(misaligned, Nave, 0, 0));

        var aligned = UsedGrid(3, 1);
        aligned.Place(new Placement(Cross, 2, 0, null));
        aligned.Open(1, 0, Side.East);
        Assert.True(RoomFiller.Fits(aligned, Nave, 0, 0));
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

    [Fact]
    public void FillAll_PlacesAMultiSlotFillerAcrossBothSlots()
    {
        var rooms = TestContent.CryptRooms().Where(r => r.Id != "hall" && r.Id != "alcove").ToList();
        rooms.Add(TestContent.RoomDef("stub", new[] { "filler" }, TestContent.Box(9, 9, Array.Empty<int>(), (4, 0, 'D'))));
        rooms.Add(TestContent.RoomDef("nave", new[] { "filler" }, TestContent.Box(17, 9, new[] { 4, 12 })));
        var area = TestContent.CompileCrypt(rooms: rooms);
        var grid = UsedGrid(2, 1);
        grid.Open(0, 0, Side.East);
        Assert.Null(RoomFiller.FillAll(area, grid, new Pcg32(1, 1)));
        var placement = Assert.Single(grid.Placements);
        Assert.Equal(("nave", 0, 0), (placement.Room.Id, placement.SlotX, placement.SlotY));
        Assert.Equal(0, grid.BlockAt(1, 0));
    }

    [Fact]
    public void FillAll_MultiSlotDeadEndCountsItsOuterOpenings()
    {
        var rooms = TestContent.CryptRooms().Where(r => r.Id != "alcove").ToList();
        rooms.Add(TestContent.RoomDef("long-end", new[] { "dead_end" }, TestContent.Box(17, 9, new[] { 4, 12 })));
        var area = TestContent.CompileCrypt(rooms: rooms);
        var grid = UsedGrid(3, 1);
        grid.Place(new Placement(area.RoomsTagged("entrance")[0], 0, 0, "entrance"));
        grid.Open(0, 0, Side.East);
        Assert.Null(RoomFiller.FillAll(area, grid, new Pcg32(1, 1)));
        Assert.Equal("long-end", grid.Placements[1].Room.Id);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~RoomFillerTests"`
Expected: build FAIL (no `Fits` overload with four arguments).

- [ ] **Step 3: Implement multi-slot fillers**

Replace `src/Mortar.Core/Indoor/DoorGeometry.cs` with:

```csharp
using Mortar.Core.Model;

namespace Mortar.Core.Indoor;

internal static class DoorGeometry
{
    public static IEnumerable<(int X, int Y)> WorldDoors(Placement placement, int stride) =>
        placement.Room.Doors.Select(d => (placement.SlotX * stride + d.X, placement.SlotY * stride + d.Y));

    public static bool DoorsMeetOn(Placement placement, Side side, int segment, Placement neighbor, int stride)
    {
        var neighborDoors = new HashSet<(int X, int Y)>(WorldDoors(neighbor, stride));
        return placement.Room.Doors
            .Where(door => door.Side == side && door.Segment == segment)
            .Any(door => neighborDoors.Contains((placement.SlotX * stride + door.X, placement.SlotY * stride + door.Y)));
    }
}
```

Replace `src/Mortar.Core/Indoor/RoomFiller.cs` with:

```csharp
using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Indoor;

internal static class RoomFiller
{
    public static string? FillAll(CompiledArea area, SlotGrid grid, Pcg32 rng)
    {
        var fillers = area.Rooms.Where(r => r.HasTag(StandardTags.Filler) || r.HasTag(StandardTags.Corridor)).ToList();
        var deadEnds = area.Rooms.Where(r => r.HasTag(StandardTags.DeadEnd)).ToList();
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

    public static bool Fits(SlotGrid grid, Room room, int x, int y) =>
        CoversEmptyUsedSlots(grid, room, x, y)
        && OuterOpenings(grid, room, x, y).All(opening => HasMatchingDoor(grid, new Placement(room, x, y, null), opening));

    private static Room? Choose(SlotGrid grid, int x, int y, List<Room> fillers, List<Room> deadEnds, Pcg32 rng)
    {
        var endings = deadEnds.Where(r => Fits(grid, r, x, y) && OuterOpenings(grid, r, x, y).Count == 1).ToList();
        if (endings.Count > 0) return RoomPicker.Pick(endings, rng);
        var candidates = fillers.Where(r => Fits(grid, r, x, y)).ToList();
        return candidates.Count == 0 ? null : RoomPicker.Pick(candidates, rng);
    }

    private static bool CoversEmptyUsedSlots(SlotGrid grid, Room room, int originX, int originY)
    {
        for (var y = originY; y < originY + room.SlotsHigh; y++)
        {
            for (var x = originX; x < originX + room.SlotsWide; x++)
            {
                if (!grid.InBounds(x, y) || !grid.IsUsed(x, y) || grid.BlockAt(x, y) != -1) return false;
            }
        }
        return true;
    }

    private static List<(int X, int Y, Side Side)> OuterOpenings(SlotGrid grid, Room room, int originX, int originY)
    {
        var footprint = new Placement(room, originX, originY, null);
        var openings = new List<(int X, int Y, Side Side)>();
        for (var y = originY; y < originY + room.SlotsHigh; y++)
        {
            for (var x = originX; x < originX + room.SlotsWide; x++)
            {
                foreach (var side in Sides.All)
                {
                    var (dx, dy) = Sides.Offset(side);
                    if (!footprint.Contains(x + dx, y + dy) && grid.IsOpen(x, y, side)) openings.Add((x, y, side));
                }
            }
        }
        return openings;
    }

    private static bool HasMatchingDoor(SlotGrid grid, Placement candidate, (int X, int Y, Side Side) opening)
    {
        var segment = opening.Side == Side.North || opening.Side == Side.South
            ? opening.X - candidate.SlotX
            : opening.Y - candidate.SlotY;
        if (!candidate.Room.HasDoorInSegment(opening.Side, segment)) return false;
        var (dx, dy) = Sides.Offset(opening.Side);
        var neighbor = grid.BlockAt(opening.X + dx, opening.Y + dy);
        return neighbor == -1
            || DoorGeometry.DoorsMeetOn(candidate, opening.Side, segment, grid.Placements[neighbor], grid.Stride);
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~RoomFillerTests"`
Expected: PASS. Then run `dotnet test`; expected: all tests pass and the crypt snapshots are unchanged.

- [ ] **Step 5: Commit**

```bash
git add src/Mortar.Core/Indoor/RoomFiller.cs src/Mortar.Core/Indoor/DoorGeometry.cs tests/Mortar.Tests/Indoor/RoomFillerTests.cs
git commit -m "allow multi-slot filler rooms"
```

---

### Task 3: Custom tileset markers

**Files:**
- Create: `src/Mortar.Core/Model/MarkerType.cs`
- Modify: `src/Mortar.Core/Model/MarkerChars.cs`, `src/Mortar.Core/Model/Tileset.cs`, `src/Mortar.Core/Authoring/RoomGridParser.cs`
- Modify: `src/Mortar.Json/Dtos.cs`, `src/Mortar.Json/DefinitionMapper.cs`
- Modify: `tests/Mortar.Tests/Support/TestContent.cs`, `tests/Mortar.Tests/Support/AsciiMap.cs`
- Test: `tests/Mortar.Tests/Authoring/TilesetTests.cs`, `tests/Mortar.Tests/Authoring/RoomCompilerTests.cs`, `tests/Mortar.Tests/Json/MortarLoaderTests.cs`

**Interfaces:**
- Produces: `MarkerType(char symbol, string kind)` with `Symbol`, `Kind`; `MarkerChars.BuiltIn` (`IReadOnlyList<MarkerType>`), `MarkerChars.IsBuiltInKind(string)`; `Tileset.Create(source, grounds, variantWeights, IReadOnlyList<MarkerType>? markers = null)`, `Tileset.Markers` (built-in six, then custom), `Tileset.TryGetMarkerKind(char, out string? kind)`; `TilesetDto.Markers`, `MarkerDto` (`Char`, `Kind`).
- Test helper: `TestContent.CreateTileset(weights = null, IReadOnlyList<MarkerType>? markers = null)`.

- [ ] **Step 1: Write the failing tests**

In `tests/Mortar.Tests/Support/TestContent.cs`, replace `CreateTileset` with:

```csharp
    public static Tileset CreateTileset(
        IReadOnlyDictionary<string, IReadOnlyList<int>>? weights = null,
        IReadOnlyList<MarkerType>? markers = null)
    {
        var grounds = new[]
        {
            new GroundType("void", ' ', false, false),
            new GroundType("floor", '.', true, false),
            new GroundType("wall", '#', false, true),
            new GroundType("water", '~', false, false),
        };
        var result = Tileset.Create("test-tileset.json", grounds, weights ?? new Dictionary<string, IReadOnlyList<int>>(), markers);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        return result.Value!;
    }
```

Add to `tests/Mortar.Tests/Authoring/TilesetTests.cs` (inside the class):

```csharp
    private static readonly MarkerType Healer = new MarkerType('H', "healer");

    private static AuthoringResult<Tileset> WithMarkers(params MarkerType[] markers) =>
        Tileset.Create("tileset.json", new[] { Void, Floor, Wall }, new Dictionary<string, IReadOnlyList<int>>(), markers);

    [Fact]
    public void Create_CustomMarkers_JoinTheBuiltInOnes()
    {
        var tileset = WithMarkers(Healer).Value!;
        Assert.True(tileset.TryGetMarkerKind('H', out var healer));
        Assert.Equal("healer", healer);
        Assert.True(tileset.TryGetMarkerKind('E', out var entrance));
        Assert.Equal(MarkerKinds.Entrance, entrance);
        Assert.False(tileset.TryGetMarkerKind('.', out _));
        Assert.Equal(7, tileset.Markers.Count);
    }

    [Theory]
    [InlineData('.', "healer", "marker char '.' is already a ground char")]
    [InlineData('D', "healer", "marker char 'D' is reserved")]
    [InlineData('E', "healer", "marker char 'E' is reserved")]
    [InlineData('H', "", "marker 'H' has no kind")]
    [InlineData('H', "exit", "marker kind 'exit' is built in")]
    public void Create_BadCustomMarker_IsAnError(char symbol, string kind, string expected) =>
        AssertError(WithMarkers(new MarkerType(symbol, kind)), expected);

    [Fact]
    public void Create_DuplicateMarkerChar_IsAnError() =>
        AssertError(WithMarkers(Healer, new MarkerType('H', "herbalist")), "duplicate marker char 'H'");

    [Fact]
    public void Create_DuplicateMarkerKind_IsAnError() =>
        AssertError(WithMarkers(Healer, new MarkerType('J', "healer")), "duplicate marker kind 'healer'");
```

Add to `tests/Mortar.Tests/Authoring/RoomCompilerTests.cs` (inside the class):

```csharp
    [Fact]
    public void Compile_CustomMarker_UsesTheTilesetKind()
    {
        var tiles = TestContent.CreateTileset(markers: new[] { new MarkerType('H', "healer") });
        var definition = TestContent.RoomDef("room_a", Array.Empty<string>(), TestContent.Box(9, 9, new[] { 4 }, (4, 4, 'H')));
        var room = RoomCompiler.Compile(definition, tiles, 9).Value!;
        var marker = Assert.Single(room.Markers);
        Assert.Equal(("healer", 4, 4), (marker.Kind, marker.X, marker.Y));
        Assert.Equal(tiles.FloorIndex, room.GroundAt(4, 4));
    }
```

Add to `tests/Mortar.Tests/Json/MortarLoaderTests.cs` (inside the class):

```csharp
    [Fact]
    public void LoadTileset_ReadsCustomMarkers()
    {
        using var temp = new TempDir();
        var path = temp.Write("tileset.json", TilesetWithMarker("""{ "char": "H", "kind": "healer" }"""));
        var tileset = MortarLoader.LoadTileset(path);
        Assert.True(tileset.Succeeded, string.Join("; ", tileset.Errors));
        Assert.True(tileset.Value!.TryGetMarkerKind('H', out var kind));
        Assert.Equal("healer", kind);
    }

    [Theory]
    [InlineData("null", "marker 0 is null")]
    [InlineData("""{ "char": "HH", "kind": "healer" }""", "marker 0 must have a single-character char")]
    [InlineData("""{ "char": "E", "kind": "healer" }""", "marker char 'E' is reserved")]
    public void LoadTileset_BadMarker_IsAnError(string marker, string expected)
    {
        using var temp = new TempDir();
        var path = temp.Write("tileset.json", TilesetWithMarker(marker));
        Assert.Contains(MortarLoader.LoadTileset(path).Errors, e => e.Source == path && e.Message.Contains(expected));
    }

    private static string TilesetWithMarker(string marker) => $$"""
        {
          "groundTypes": [
            { "id": "void", "char": " ", "walkable": false, "blocksSight": false },
            { "id": "floor", "char": ".", "walkable": true, "blocksSight": false },
            { "id": "wall", "char": "#", "walkable": false, "blocksSight": true }
          ],
          "markers": [{{marker}}]
        }
        """;
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~TilesetTests|FullyQualifiedName~RoomCompilerTests|FullyQualifiedName~MortarLoaderTests"`
Expected: build FAIL (`MarkerType` and `TryGetMarkerKind` do not exist).

- [ ] **Step 3: Implement custom markers**

Create `src/Mortar.Core/Model/MarkerType.cs`:

```csharp
namespace Mortar.Core.Model;

public sealed class MarkerType
{
    public MarkerType(char symbol, string kind)
    {
        Symbol = symbol;
        Kind = kind;
    }

    public char Symbol { get; }

    public string Kind { get; }
}
```

Replace `src/Mortar.Core/Model/MarkerChars.cs` with:

```csharp
using System.Diagnostics.CodeAnalysis;

namespace Mortar.Core.Model;

public static class MarkerChars
{
    public const char Door = 'D';

    public static readonly IReadOnlyList<MarkerType> BuiltIn = new[]
    {
        new MarkerType('E', MarkerKinds.Entrance),
        new MarkerType('X', MarkerKinds.Exit),
        new MarkerType('B', MarkerKinds.Boss),
        new MarkerType('W', MarkerKinds.Waypoint),
        new MarkerType('S', MarkerKinds.Spawn),
        new MarkerType('C', MarkerKinds.Chest),
    };

    public static bool TryGetKind(char symbol, [NotNullWhen(true)] out string? kind)
    {
        kind = BuiltIn.FirstOrDefault(marker => marker.Symbol == symbol)?.Kind;
        return kind is not null;
    }

    public static bool IsReserved(char symbol) => symbol == Door || BuiltIn.Any(marker => marker.Symbol == symbol);

    public static bool IsBuiltInKind(string kind) => BuiltIn.Any(marker => marker.Kind == kind);
}
```

In `src/Mortar.Core/Model/Tileset.cs`:

1. Add `using System.Diagnostics.CodeAnalysis;` at the top.
2. Add a field `private readonly Dictionary<char, string> _markerKinds = new Dictionary<char, string>();`.
3. Give the constructor a third parameter `IReadOnlyList<MarkerType> markers` and add at its end:

```csharp
        Markers = MarkerChars.BuiltIn.Concat(markers).ToArray();
        foreach (var marker in Markers)
        {
            _markerKinds[marker.Symbol] = marker.Kind;
        }
```

4. Add after `VoidIndex`:

```csharp
    public IReadOnlyList<MarkerType> Markers { get; }

    public bool TryGetMarkerKind(char symbol, [NotNullWhen(true)] out string? kind) =>
        _markerKinds.TryGetValue(symbol, out kind);
```

5. Replace `Create` with:

```csharp
    public static AuthoringResult<Tileset> Create(
        string source,
        IReadOnlyList<GroundType> grounds,
        IReadOnlyDictionary<string, IReadOnlyList<int>> variantWeights,
        IReadOnlyList<MarkerType>? markers = null)
    {
        var custom = markers ?? Array.Empty<MarkerType>();
        var errors = new List<AuthoringError>();
        CheckGrounds(source, grounds, errors);
        CheckWeights(source, variantWeights, errors);
        CheckMarkers(source, grounds, custom, errors);
        return errors.Count > 0
            ? AuthoringResult<Tileset>.Failure(errors)
            : AuthoringResult<Tileset>.Success(new Tileset(grounds, variantWeights, custom));
    }
```

6. Add at the end of the class:

```csharp
    private static void CheckMarkers(
        string source,
        IReadOnlyList<GroundType> grounds,
        IReadOnlyList<MarkerType> markers,
        List<AuthoringError> errors)
    {
        var symbols = new HashSet<char>();
        var kinds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var marker in markers)
        {
            if (grounds.Any(ground => ground.Symbol == marker.Symbol))
                errors.Add(new AuthoringError(source, $"marker char '{marker.Symbol}' is already a ground char"));
            else if (MarkerChars.IsReserved(marker.Symbol))
                errors.Add(new AuthoringError(source, $"marker char '{marker.Symbol}' is reserved"));
            else if (!symbols.Add(marker.Symbol))
                errors.Add(new AuthoringError(source, $"duplicate marker char '{marker.Symbol}'"));
            if (string.IsNullOrWhiteSpace(marker.Kind))
                errors.Add(new AuthoringError(source, $"marker '{marker.Symbol}' has no kind"));
            else if (MarkerChars.IsBuiltInKind(marker.Kind))
                errors.Add(new AuthoringError(source, $"marker kind '{marker.Kind}' is built in"));
            else if (!kinds.Add(marker.Kind))
                errors.Add(new AuthoringError(source, $"duplicate marker kind '{marker.Kind}'"));
        }
    }
```

In `src/Mortar.Core/Authoring/RoomGridParser.cs`, replace `if (MarkerChars.TryGetKind(symbol, out var kind))` with:

```csharp
        if (_tileset.TryGetMarkerKind(symbol, out var kind))
```

In `src/Mortar.Json/Dtos.cs`, add to `TilesetDto` after `VariantWeights`:

```csharp
    public List<MarkerDto?>? Markers { get; set; }
```

and add a new class after `GroundTypeDto`:

```csharp
internal sealed class MarkerDto
{
    public string? Char { get; set; }

    public string? Kind { get; set; }
}
```

In `src/Mortar.Json/DefinitionMapper.cs`, in `ToTileset` add `var markers = ToMarkers(source, dto.Markers, errors);` immediately before `if (errors.Count > 0)`, change the final line to `return Tileset.Create(source, grounds, weights, markers);`, and add:

```csharp
    private static List<MarkerType> ToMarkers(string source, List<MarkerDto?>? dtos, List<AuthoringError> errors)
    {
        var markers = new List<MarkerType>();
        if (dtos is null) return markers;
        for (var i = 0; i < dtos.Count; i++)
        {
            var marker = dtos[i];
            if (marker is null) errors.Add(new AuthoringError(source, $"marker {i} is null"));
            else if (marker.Char is not { Length: 1 }) errors.Add(new AuthoringError(source, $"marker {i} must have a single-character char"));
            else markers.Add(new MarkerType(marker.Char[0], marker.Kind ?? ""));
        }
        return markers;
    }
```

In `tests/Mortar.Tests/Support/AsciiMap.cs`, delete the `MarkerSymbols` dictionary and replace the marker loop with:

```csharp
        foreach (var marker in layout.Markers)
        {
            rows[marker.Y][marker.X] = tileset.Markers.FirstOrDefault(type => type.Kind == marker.Kind)?.Symbol ?? '?';
        }
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~TilesetTests|FullyQualifiedName~RoomCompilerTests|FullyQualifiedName~MortarLoaderTests"`
Expected: PASS. Then run `dotnet test`; expected: all tests pass and the crypt snapshots are unchanged.

- [ ] **Step 5: Commit**

```bash
git add src/Mortar.Core/Model/MarkerType.cs src/Mortar.Core/Model/MarkerChars.cs src/Mortar.Core/Model/Tileset.cs src/Mortar.Core/Authoring/RoomGridParser.cs src/Mortar.Json/Dtos.cs src/Mortar.Json/DefinitionMapper.cs tests/Mortar.Tests/Support/TestContent.cs tests/Mortar.Tests/Support/AsciiMap.cs tests/Mortar.Tests/Authoring/TilesetTests.cs tests/Mortar.Tests/Authoring/RoomCompilerTests.cs tests/Mortar.Tests/Json/MortarLoaderTests.cs
git commit -m "add custom tileset markers"
```

---

### Task 4: Fixed area mode in Core

**Files:**
- Modify: `src/Mortar.Core/Model/AreaDefinition.cs`, `src/Mortar.Core/Model/CompiledArea.cs`, `src/Mortar.Core/Authoring/AreaCompiler.cs`, `src/Mortar.Core/Authoring/AreaValidator.cs`, `src/Mortar.Core/Validation/LayoutValidator.cs`, `src/Mortar.Core/Generation/Generator.cs`
- Create: `src/Mortar.Core/Model/FixedMap.cs`, `src/Mortar.Core/Authoring/FixedMapCompiler.cs`
- Test: `tests/Mortar.Tests/Authoring/FixedMapTests.cs` (create)

**Interfaces:**
- Consumes: `Tileset.TryGetMarkerKind` (Task 3), `LayoutFactory.Create`, `Pcg32.ForAttempt`.
- Produces: `AreaMode.Fixed`; `AreaDefinition.Fixed(string source, string id)`; `FixedMap` (`Width`, `Height`, `Markers`; internal `Grid`); `CompiledArea.Map` (`FixedMap?`, null for indoor areas); `AreaCompiler.CompileFixed(AreaDefinition area, Tileset tileset, RoomDefinition map)`; `Generator.Generate` stamps fixed areas on attempt 0.

- [ ] **Step 1: Write the failing tests**

Create `tests/Mortar.Tests/Authoring/FixedMapTests.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Core.Generation;
using Mortar.Core.Layouts;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Authoring;

public class FixedMapTests
{
    private static readonly string[] Square =
    {
        "#######",
        "#E...X#",
        "#.~~..#",
        "#######",
    };

    private static AuthoringResult<CompiledArea> Compile(params string[] grid) =>
        CompileWith(TestContent.CreateTileset(), grid);

    private static AuthoringResult<CompiledArea> CompileWith(Tileset tileset, params string[] grid) =>
        AreaCompiler.CompileFixed(
            AreaDefinition.Fixed("town.json", "town"),
            tileset,
            new RoomDefinition("map.json", "", Array.Empty<string>(), grid));

    private static void AssertError(AuthoringResult<CompiledArea> result, string expected)
    {
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Source == "map.json" && e.Message.Contains(expected));
    }

    [Fact]
    public void Compile_RecordsSizeAndMarkersInRowOrder()
    {
        var area = Compile(Square).Value!;
        var map = area.Map!;
        Assert.Equal(AreaMode.Fixed, area.Definition.Mode);
        Assert.Equal((7, 4), (map.Width, map.Height));
        Assert.Equal(
            new[] { (MarkerKinds.Entrance, 1, 1), (MarkerKinds.Exit, 5, 1) },
            map.Markers.Select(m => (m.Kind, m.X, m.Y)));
    }

    [Fact]
    public void Generate_StampsTheMapAsDrawn()
    {
        var area = Compile(Square).Value!;
        var result = Generator.Generate(area, 5);
        Assert.True(result.Succeeded);
        var layout = result.Layout!;
        Assert.Equal(0, layout.Attempt);
        Assert.Equal(1, result.Attempts);
        Assert.Equal("water", layout.Cells.GroundAt(2, 2));
        Assert.Equal("floor", layout.Cells.GroundAt(1, 1));
        Assert.Equal("wall", layout.Cells.GroundAt(0, 0));
        Assert.Equal(area.Map!.Markers, layout.Markers);
    }

    [Fact]
    public void Generate_SameSeedGivesTheSameFingerprint()
    {
        var area = Compile(Square).Value!;
        Assert.Equal(Generator.Generate(area, 9).Layout!.Fingerprint, Generator.Generate(area, 9).Layout!.Fingerprint);
    }

    [Fact]
    public void Compile_VoidsUnreachablePockets()
    {
        var area = Compile("#####", "#E#.#", "#####").Value!;
        Assert.Equal("void", Generator.Generate(area, 1).Layout!.Cells.GroundAt(3, 1));
    }

    [Fact]
    public void Compile_CustomMarker_IsRecorded()
    {
        var tileset = TestContent.CreateTileset(markers: new[] { new MarkerType('H', "healer") });
        var area = CompileWith(tileset, "E.H").Value!;
        Assert.Contains(area.Map!.Markers, m => m.Kind == "healer" && m.X == 2 && m.Y == 0);
    }

    [Fact]
    public void Compile_EmptyGrid_IsAnError() => AssertError(Compile(), "map has an empty grid");

    [Fact]
    public void Compile_RaggedRows_IsAnError() => AssertError(Compile("###", "#E"), "map row 1 has length 2, expected 3");

    [Fact]
    public void Compile_UnknownChar_IsAnError() => AssertError(Compile("#E?#"), "unknown char '?' at (2, 0)");

    [Fact]
    public void Compile_Door_IsAnError() => AssertError(Compile("#ED#"), "door at (2, 0)");

    [Fact]
    public void Compile_NoEntrance_IsAnError() => AssertError(Compile("#..#"), "exactly one entrance (E), found 0");

    [Fact]
    public void Compile_TwoEntrances_IsAnError() => AssertError(Compile("#EE#"), "exactly one entrance (E), found 2");

    [Fact]
    public void Compile_UnreachableMarker_IsAnError() =>
        AssertError(Compile("#E#X#"), "marker 'exit' at (3, 0) cannot be reached from the entrance");

    [Fact]
    public void Compile_TooWide_IsAnError() =>
        AssertError(Compile("E" + new string('.', Layout.MaxDimension)), "width and height must be at most 4096");

    [Fact]
    public void CompileFixed_IndoorDefinition_IsAnError()
    {
        var result = AreaCompiler.CompileFixed(
            TestContent.CryptDefinition(), TestContent.CreateTileset(), new RoomDefinition("map.json", "", Array.Empty<string>(), Square));
        Assert.Contains(result.Errors, e => e.Message == "only fixed areas can be compiled from a map");
    }

    [Fact]
    public void Compile_FixedDefinitionWithRooms_IsAnError()
    {
        var result = AreaCompiler.Compile(AreaDefinition.Fixed("town.json", "town"), TestContent.CreateTileset(), TestContent.CryptRooms());
        Assert.Contains(result.Errors, e => e.Message.Contains("fixed areas are built from a map"));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~FixedMapTests"`
Expected: build FAIL (`AreaDefinition.Fixed`, `CompileFixed`, and `CompiledArea.Map` do not exist).

- [ ] **Step 3: Implement fixed areas**

In `src/Mortar.Core/Model/AreaDefinition.cs`, add `Fixed,` to `AreaMode` after `Outdoor`, and add to `AreaDefinition` after `DefaultMaxAttempts`:

```csharp
    /// <summary>A fixed area is one stamped map, so the indoor settings are unused placeholders.</summary>
    public static AreaDefinition Fixed(string source, string id) => new AreaDefinition(
        source,
        id,
        AreaMode.Fixed,
        new TargetSize(0, 0, 0),
        0,
        0,
        1,
        Array.Empty<string>(),
        Array.Empty<GraphNode>(),
        Array.Empty<GraphEdge>());
```

Create `src/Mortar.Core/Model/FixedMap.cs`:

```csharp
using Mortar.Core.Layouts;

namespace Mortar.Core.Model;

public sealed class FixedMap
{
    internal FixedMap(CellGrid grid, IReadOnlyList<Marker> markers)
    {
        Grid = grid;
        Markers = markers;
    }

    public int Width => Grid.Width;

    public int Height => Grid.Height;

    public IReadOnlyList<Marker> Markers { get; }

    internal CellGrid Grid { get; }
}
```

Replace `src/Mortar.Core/Model/CompiledArea.cs` with:

```csharp
namespace Mortar.Core.Model;

public sealed class CompiledArea
{
    internal CompiledArea(AreaDefinition definition, Tileset tileset, IReadOnlyList<Room> rooms)
        : this(definition, tileset, rooms, null)
    {
    }

    internal CompiledArea(AreaDefinition definition, Tileset tileset, FixedMap map)
        : this(definition, tileset, Array.Empty<Room>(), map)
    {
    }

    private CompiledArea(AreaDefinition definition, Tileset tileset, IReadOnlyList<Room> rooms, FixedMap? map)
    {
        Definition = definition;
        Tileset = tileset;
        Rooms = rooms;
        Map = map;
    }

    public AreaDefinition Definition { get; }

    public Tileset Tileset { get; }

    public IReadOnlyList<Room> Rooms { get; }

    public FixedMap? Map { get; }

    public IReadOnlyList<Room> RoomsTagged(string tag) => Rooms.Where(r => r.HasTag(tag)).ToList();

    public GraphNode Node(string id) => Definition.Nodes.First(n => n.Id == id);
}
```

In `src/Mortar.Core/Validation/LayoutValidator.cs`, change `private static bool[] FloodFill` to `internal static bool[] FloodFill` and `private static void VoidUnreached` to `internal static void VoidUnreached`.

Create `src/Mortar.Core/Authoring/FixedMapCompiler.cs`:

```csharp
using Mortar.Core.Layouts;
using Mortar.Core.Model;
using Mortar.Core.Validation;

namespace Mortar.Core.Authoring;

internal static class FixedMapCompiler
{
    public static AuthoringResult<FixedMap> Compile(RoomDefinition map, Tileset tileset)
    {
        var errors = new List<AuthoringError>();
        if (!CheckShape(map, errors))
        {
            return AuthoringResult<FixedMap>.Failure(errors);
        }
        var grid = new CellGrid(map.Grid[0].Length, map.Grid.Count, tileset.VoidIndex);
        var markers = new List<Marker>();
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                ParseCell(map, tileset, grid, markers, x, y, errors);
            }
        }
        if (errors.Count == 0)
        {
            CheckReachability(map, tileset, grid, markers, errors);
        }
        return errors.Count > 0
            ? AuthoringResult<FixedMap>.Failure(errors)
            : AuthoringResult<FixedMap>.Success(new FixedMap(grid, markers));
    }

    private static bool CheckShape(RoomDefinition map, List<AuthoringError> errors)
    {
        var rows = map.Grid;
        if (rows.Count == 0 || rows[0].Length == 0)
        {
            errors.Add(new AuthoringError(map.Source, "map has an empty grid"));
            return false;
        }
        for (var y = 1; y < rows.Count; y++)
        {
            if (rows[y].Length != rows[0].Length)
            {
                errors.Add(new AuthoringError(map.Source, $"map row {y} has length {rows[y].Length}, expected {rows[0].Length}"));
                return false;
            }
        }
        if (rows[0].Length > Layout.MaxDimension || rows.Count > Layout.MaxDimension)
        {
            errors.Add(new AuthoringError(
                map.Source,
                $"map is {rows[0].Length}x{rows.Count}; width and height must be at most {Layout.MaxDimension}"));
            return false;
        }
        return true;
    }

    private static void ParseCell(
        RoomDefinition map, Tileset tileset, CellGrid grid, List<Marker> markers, int x, int y, List<AuthoringError> errors)
    {
        var symbol = map.Grid[y][x];
        if (symbol == MarkerChars.Door)
        {
            errors.Add(new AuthoringError(map.Source, $"map has a door at ({x}, {y}); fixed maps have no doors"));
            return;
        }
        if (tileset.TryGetMarkerKind(symbol, out var kind))
        {
            grid[x, y] = tileset.FloorIndex;
            markers.Add(new Marker(kind, null, x, y));
            return;
        }
        var ground = tileset.IndexOfSymbol(symbol);
        if (ground < 0) errors.Add(new AuthoringError(map.Source, $"map uses unknown char '{symbol}' at ({x}, {y})"));
        else grid[x, y] = ground;
    }

    private static void CheckReachability(
        RoomDefinition map, Tileset tileset, CellGrid grid, List<Marker> markers, List<AuthoringError> errors)
    {
        var entrances = markers.Where(m => m.Kind == MarkerKinds.Entrance).ToList();
        if (entrances.Count != 1)
        {
            errors.Add(new AuthoringError(map.Source, $"map must have exactly one entrance (E), found {entrances.Count}"));
            return;
        }
        var reached = LayoutValidator.FloodFill(grid, tileset, entrances[0].X, entrances[0].Y);
        foreach (var marker in markers.Where(m => !reached[m.Y * grid.Width + m.X]))
        {
            errors.Add(new AuthoringError(
                map.Source,
                $"map marker '{marker.Kind}' at ({marker.X}, {marker.Y}) cannot be reached from the entrance"));
        }
        LayoutValidator.VoidUnreached(grid, tileset, reached);
    }
}
```

In `src/Mortar.Core/Authoring/AreaCompiler.cs`, add after `Compile`:

```csharp
    public static AuthoringResult<CompiledArea> CompileFixed(AreaDefinition area, Tileset tileset, RoomDefinition map)
    {
        if (area.Mode != AreaMode.Fixed)
        {
            return AuthoringResult<CompiledArea>.Failure(area.Source, "only fixed areas can be compiled from a map");
        }
        if (string.IsNullOrWhiteSpace(area.Id))
        {
            return AuthoringResult<CompiledArea>.Failure(area.Source, "area has no id");
        }
        var compiled = FixedMapCompiler.Compile(map, tileset);
        return compiled.Succeeded
            ? AuthoringResult<CompiledArea>.Success(new CompiledArea(area, tileset, compiled.Value!))
            : AuthoringResult<CompiledArea>.Failure(compiled.Errors);
    }
```

In `src/Mortar.Core/Authoring/AreaValidator.cs`, add in `CheckSettings` after the outdoor check:

```csharp
        if (area.Mode == AreaMode.Fixed) Error("fixed areas are built from a map, not from rooms and a graph");
```

In `src/Mortar.Core/Generation/Generator.cs`, add as the first line of `Generate`:

```csharp
        if (area.Map is not null) return GenerateFixed(area, area.Map, seed);
```

and add the method:

```csharp
    private static GenerationResult GenerateFixed(CompiledArea area, FixedMap map, uint seed)
    {
        var rng = Pcg32.ForAttempt(area.Definition.Id, seed, 0);
        var layout = LayoutFactory.Create(area, seed, 0, map.Grid, map.Markers, rng);
        return GenerationResult.Success(layout, Array.Empty<FailureCount>());
    }
```

(`LayoutFactory.Create` copies the ground array, so the compiled map is never modified.)

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~FixedMapTests"`
Expected: PASS. Then run `dotnet test`; expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Mortar.Core/Model/AreaDefinition.cs src/Mortar.Core/Model/FixedMap.cs src/Mortar.Core/Model/CompiledArea.cs src/Mortar.Core/Authoring/FixedMapCompiler.cs src/Mortar.Core/Authoring/AreaCompiler.cs src/Mortar.Core/Authoring/AreaValidator.cs src/Mortar.Core/Validation/LayoutValidator.cs src/Mortar.Core/Generation/Generator.cs tests/Mortar.Tests/Authoring/FixedMapTests.cs
git commit -m "add fixed area mode"
```

---

### Task 5: Load fixed areas from JSON

**Files:**
- Modify: `src/Mortar.Json/Dtos.cs`, `src/Mortar.Json/AreaMapper.cs`, `src/Mortar.Json/MortarLoader.cs`
- Create: `src/Mortar.Json/FixedAreaLoader.cs`
- Test: `tests/Mortar.Tests/Json/FixedAreaLoaderTests.cs` (create), `tests/Mortar.Tests/Json/MortarLoaderTests.cs`

**Interfaces:**
- Consumes: `AreaDefinition.Fixed`, `AreaCompiler.CompileFixed` (Task 4), `MortarLoader.LoadTileset`, `DefinitionMapper.ToRoom`.
- Produces: `AreaDto.Map` (`string?`), `AreaDto.MaxSideBranches` becomes `int?`; `MortarLoader.LoadArea` loads `mode: "fixed"` areas through internal `FixedAreaLoader.Load(string source, AreaDto dto)`.

- [ ] **Step 1: Write the failing tests**

Create `tests/Mortar.Tests/Json/FixedAreaLoaderTests.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Json;
using Mortar.Tests.Support;

namespace Mortar.Tests.Json;

public class FixedAreaLoaderTests
{
    private const string Map = """{ "grid": ["#####", "#E.X#", "#####"] }""";

    private static string AreaJson(string extra = "", string map = "\"map\": \"map.json\",") => $$"""
        {
          "id": "town",
          "mode": "fixed",{{map}}{{extra}}
          "tileset": "{{SampleArea.Tileset}}"
        }
        """;

    private static string WriteArea(TempDir temp, string json)
    {
        temp.Write("map.json", Map);
        return temp.Write("area.json", json);
    }

    [Fact]
    public void LoadArea_FixedArea_Succeeds()
    {
        using var temp = new TempDir();
        var result = MortarLoader.LoadArea(WriteArea(temp, AreaJson()));
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        Assert.Equal(AreaMode.Fixed, result.Value!.Definition.Mode);
        Assert.Equal("town", result.Value.Definition.Id);
        Assert.Equal(5, result.Value.Map!.Width);
    }

    [Theory]
    [InlineData("\n  \"graph\": { \"nodes\": [] },", "graph")]
    [InlineData("\n  \"slotSize\": 9,", "slotSize")]
    [InlineData("\n  \"rooms\": [],", "rooms")]
    [InlineData("\n  \"roomTags\": [],", "roomTags")]
    [InlineData("\n  \"maxSideBranches\": 0,", "maxSideBranches")]
    [InlineData("\n  \"targetSize\": { \"width\": 5, \"height\": 3, \"tolerance\": 0 },", "targetSize")]
    [InlineData("\n  \"maxAttempts\": 1,", "maxAttempts")]
    public void LoadArea_FixedAreaWithIndoorField_IsAnError(string extra, string field)
    {
        using var temp = new TempDir();
        var path = WriteArea(temp, AreaJson(extra));
        Assert.Contains(MortarLoader.LoadArea(path).Errors, e => e.Source == path && e.Message == $"'{field}' is not allowed in a fixed area");
    }

    [Fact]
    public void LoadArea_FixedAreaWithoutMap_IsAnError()
    {
        using var temp = new TempDir();
        var path = WriteArea(temp, AreaJson(map: ""));
        Assert.Contains(MortarLoader.LoadArea(path).Errors, e => e.Source == path && e.Message == "fixed area has no map");
    }

    [Fact]
    public void LoadArea_MissingMapFile_IsAnError()
    {
        using var temp = new TempDir();
        var path = temp.Write("area.json", AreaJson());
        Assert.Contains(MortarLoader.LoadArea(path).Errors, e => e.Source == path && e.Message == "map 'map.json' does not exist");
    }

    [Fact]
    public void LoadArea_BadMap_NamesTheMapFile()
    {
        using var temp = new TempDir();
        var map = temp.Write("map.json", """{ "grid": ["#E?#"] }""");
        var area = temp.Write("area.json", AreaJson());
        Assert.Contains(MortarLoader.LoadArea(area).Errors, e => e.Source == map && e.Message.Contains("unknown char '?'"));
    }

    [Fact]
    public void LoadArea_IndoorAreaWithMap_IsAnError()
    {
        using var temp = new TempDir();
        var json = SampleArea.AreaJson(SampleArea.Tileset, SampleArea.Rooms, extra: "\n  \"map\": \"map.json\",");
        var path = temp.Write("area.json", json);
        Assert.Contains(MortarLoader.LoadArea(path).Errors, e => e.Message == "'map' is only allowed in fixed areas");
    }
}
```

In `tests/Mortar.Tests/Json/MortarLoaderTests.cs`, change the expected message in `LoadArea_UnknownMode_IsAnError` to `"mode must be 'indoor', 'outdoor', or 'fixed'"`.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~FixedAreaLoaderTests|FullyQualifiedName~MortarLoaderTests"`
Expected: FAIL (fixed areas are rejected as an unknown mode; `map` is an unknown property).

- [ ] **Step 3: Implement fixed area loading**

In `src/Mortar.Json/Dtos.cs`, in `AreaDto` change `public int MaxSideBranches { get; set; }` to `public int? MaxSideBranches { get; set; }` and add after `Graph`:

```csharp
    public string? Map { get; set; }
```

In `src/Mortar.Json/AreaMapper.cs`:

1. Add after the `area has no graph nodes` check: `if (dto.Map is not null) Error("'map' is only allowed in fixed areas");`
2. Replace `dto.MaxSideBranches,` in the `AreaDefinition` constructor call with `dto.MaxSideBranches ?? 0,`.
3. Change the `MapMode` default message to `$"mode must be 'indoor', 'outdoor', or 'fixed', got '{mode}'"`.

Create `src/Mortar.Json/FixedAreaLoader.cs`:

```csharp
using Mortar.Core.Authoring;
using Mortar.Core.Model;

namespace Mortar.Json;

internal static class FixedAreaLoader
{
    public const string Mode = "fixed";

    public static AuthoringResult<CompiledArea> Load(string source, AreaDto dto)
    {
        var errors = CheckFields(source, dto);
        if (errors.Count > 0)
        {
            return AuthoringResult<CompiledArea>.Failure(errors);
        }
        var directory = Path.GetDirectoryName(source)!;
        var tilesetPath = SafePath.Resolve(directory, dto.Tileset);
        var tileset = tilesetPath is null
            ? AuthoringResult<Tileset>.Failure(source, $"tileset '{dto.Tileset}' is an invalid path")
            : MortarLoader.LoadTileset(tilesetPath);
        errors.AddRange(tileset.Errors);
        var map = LoadMap(source, directory, dto.Map, errors);
        if (errors.Count > 0)
        {
            return AuthoringResult<CompiledArea>.Failure(errors);
        }
        return AreaCompiler.CompileFixed(AreaDefinition.Fixed(source, dto.Id ?? ""), tileset.Value!, map!);
    }

    private static List<AuthoringError> CheckFields(string source, AreaDto dto)
    {
        var errors = new List<AuthoringError>();
        void NotAllowed(bool present, string field)
        {
            if (present) errors.Add(new AuthoringError(source, $"'{field}' is not allowed in a fixed area"));
        }
        NotAllowed(dto.Graph is not null, "graph");
        NotAllowed(dto.SlotSize is not null, "slotSize");
        NotAllowed(dto.Rooms is not null, "rooms");
        NotAllowed(dto.RoomTags is not null, "roomTags");
        NotAllowed(dto.MaxSideBranches is not null, "maxSideBranches");
        NotAllowed(dto.TargetSize is not null, "targetSize");
        NotAllowed(dto.MaxAttempts is not null, "maxAttempts");
        if (string.IsNullOrWhiteSpace(dto.Tileset)) errors.Add(new AuthoringError(source, "area has no tileset"));
        if (string.IsNullOrWhiteSpace(dto.Map)) errors.Add(new AuthoringError(source, "fixed area has no map"));
        return errors;
    }

    private static RoomDefinition? LoadMap(string source, string directory, string? entry, List<AuthoringError> errors)
    {
        var path = SafePath.Resolve(directory, entry);
        if (path is null)
        {
            errors.Add(new AuthoringError(source, $"map '{entry}' is an invalid path"));
            return null;
        }
        if (!File.Exists(path))
        {
            errors.Add(new AuthoringError(source, $"map '{entry}' does not exist"));
            return null;
        }
        var dto = JsonFile.Read<RoomDto>(path, out var error);
        if (dto is null)
        {
            errors.Add(error!);
            return null;
        }
        return DefinitionMapper.ToRoom(path, dto);
    }
}
```

In `src/Mortar.Json/MortarLoader.cs`, in `LoadArea` add right after the `if (dto is null) { ... }` block:

```csharp
        if (dto.Mode == FixedAreaLoader.Mode)
        {
            return FixedAreaLoader.Load(source, dto);
        }
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~FixedAreaLoaderTests|FullyQualifiedName~MortarLoaderTests"`
Expected: PASS. Then run `dotnet test`; expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Mortar.Json/Dtos.cs src/Mortar.Json/AreaMapper.cs src/Mortar.Json/FixedAreaLoader.cs src/Mortar.Json/MortarLoader.cs tests/Mortar.Tests/Json/FixedAreaLoaderTests.cs tests/Mortar.Tests/Json/MortarLoaderTests.cs
git commit -m "load fixed areas"
```

---

### Task 6: Cathedral content

**Files:**
- Create: `content/tilesets/cathedral.json`, `content/rooms/cathedral/*.json` (22 rooms), `content/areas/cathedral-1.json` … `cathedral-4.json`
- Create: `tests/Mortar.Tests/Support/Snapshots.cs`, `tests/Mortar.Tests/Generation/ContentAreaTests.cs`
- Modify: `tests/Mortar.Tests/Support/TestPaths.cs`, `tests/Mortar.Tests/Generation/SnapshotTests.cs`, `tests/Mortar.Tests/Gameplay/DeterminismTests.cs`
- Snapshots (generated, then reviewed): `tests/Mortar.Tests/Snapshots/cathedral-1-seed-1.txt` … `cathedral-4-seed-1.txt`

**Interfaces:**
- Consumes: room weights (Task 1), multi-slot fillers (Task 2), `MortarLoader.LoadArea`, `Generator.Generate`.
- Produces: `TestPaths.Content`, `TestPaths.ContentArea(string name)`; `Snapshots.AssertMatches(string fileName, string actual)`; the four cathedral areas that Task 9 wires into `levels.json`.

- [ ] **Step 1: Add the snapshot helper and content paths**

Create `tests/Mortar.Tests/Support/Snapshots.cs`:

```csharp
namespace Mortar.Tests.Support;

internal static class Snapshots
{
    /// <summary>A missing snapshot is written and the test fails, so every new snapshot gets reviewed once.</summary>
    public static void AssertMatches(string fileName, string actual)
    {
        var path = Path.Combine(TestPaths.Root, "tests", "Mortar.Tests", "Snapshots", fileName);
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

In `tests/Mortar.Tests/Support/TestPaths.cs`, add after `CryptArea`:

```csharp
    public static string Content => Path.Combine(Root, "content");

    public static string ContentArea(string name) => Path.Combine(Content, "areas", $"{name}.json");
```

In `tests/Mortar.Tests/Generation/SnapshotTests.cs`, replace everything after `var actual = ...;` in the test body with:

```csharp
        Snapshots.AssertMatches($"crypt-seed-{seed}.txt", actual);
```

In `tests/Mortar.Tests/Gameplay/DeterminismTests.cs`, replace everything after `var actual = ...;` in `ScriptedRun_MatchesSnapshot` with:

```csharp
        Snapshots.AssertMatches("game-seed-7.txt", actual);
```

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~SnapshotTests|FullyQualifiedName~DeterminismTests"`
Expected: PASS (the existing snapshots are unchanged).

- [ ] **Step 2: Write the failing content tests**

Create `tests/Mortar.Tests/Generation/ContentAreaTests.cs`:

```csharp
using Mortar.Core.Generation;
using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Json;
using Mortar.Tests.Support;

namespace Mortar.Tests.Generation;

public class ContentAreaTests
{
    private const uint SeedCount = 500;

    public static TheoryData<string> CathedralAreas => new() { "cathedral-1", "cathedral-2", "cathedral-3", "cathedral-4" };

    private static CompiledArea Load(string name)
    {
        var result = MortarLoader.LoadArea(TestPaths.ContentArea(name));
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        return result.Value!;
    }

    [Theory]
    [MemberData(nameof(CathedralAreas))]
    public void Cathedral_GeneratesOnEverySeed(string name)
    {
        var area = Load(name);
        var failures = new List<string>();
        for (var seed = 1u; seed <= SeedCount; seed++)
        {
            var result = Generator.Generate(area, seed);
            if (!result.Succeeded) failures.Add($"seed {seed}: {result.FailureSummary}");
        }
        Assert.Empty(failures);
    }

    [Theory]
    [MemberData(nameof(CathedralAreas))]
    public void Cathedral_MatchesSnapshot(string name)
    {
        var area = Load(name);
        var result = Generator.Generate(area, 1);
        Assert.True(result.Succeeded, result.FailureSummary);
        Snapshots.AssertMatches($"{name}-seed-1.txt", AsciiMap.Render(result.Layout!, area.Tileset));
    }

    [Fact]
    public void Cathedral_UsesMultiSlotFillers()
    {
        var area = Load("cathedral-3");
        var found = false;
        for (var seed = 1u; seed <= 20 && !found; seed++)
        {
            var grid = SlotGrid.ForArea(area.Definition);
            var rng = Pcg32.ForAttempt(area.Definition.Id, seed, 0);
            var failure = LandmarkPlacer.PlaceAll(area, grid, rng) ?? IndoorRouter.Route(area, grid, rng) ?? RoomFiller.FillAll(area, grid, rng);
            found = failure is null && grid.Placements.Any(p => p.NodeId is null && !p.Room.IsSingleSlot);
        }
        Assert.True(found, "no multi-slot filler was placed in 20 seeds");
    }
}
```

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~ContentAreaTests"`
Expected: FAIL (`content/areas/cathedral-1.json` does not exist).

- [ ] **Step 3: Add the cathedral content**

Create each file below with exactly the content shown.

`content/tilesets/cathedral.json`:

```json
{
  "groundTypes": [
    { "id": "void", "char": " ", "walkable": false, "blocksSight": false },
    { "id": "floor", "char": ".", "walkable": true, "blocksSight": false },
    { "id": "wall", "char": "#", "walkable": false, "blocksSight": true }
  ],
  "variantWeights": {
    "floor": [8, 2, 1],
    "wall": [6, 1, 1]
  }
}
```

`content/rooms/cathedral/alcove.json`:

```json
{
  "id": "alcove",
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

`content/rooms/cathedral/boss-chamber.json`:

```json
{
  "id": "boss-chamber",
  "tags": ["boss"],
  "grid": [
    "####D#######D####",
    "#...............#",
    "#.##.........##.#",
    "#.#...........#.#",
    "D...S.......S...D",
    "#...............#",
    "#.....#...#.....#",
    "#...............#",
    "#.......B.......#",
    "#...............#",
    "#.....#...#.....#",
    "#...............#",
    "D...S.......S...D",
    "#.#...........#.#",
    "#.##.........##.#",
    "#...............#",
    "####D#######D####"
  ]
}
```

`content/rooms/cathedral/cell-block.json`:

```json
{
  "id": "cell-block",
  "tags": ["filler"],
  "weight": 3,
  "grid": [
    "####D####",
    "#.#...#.#",
    "#.#...#.#",
    "#.......#",
    "D...S...D",
    "#.......#",
    "#.#...#.#",
    "#.#...#.#",
    "####D####"
  ]
}
```

`content/rooms/cathedral/chapel.json`:

```json
{
  "id": "chapel",
  "tags": ["filler"],
  "weight": 3,
  "grid": [
    "####D####",
    "#.......#",
    "#.#.#.#.#",
    "#.......#",
    "D...S...D",
    "#.......#",
    "#.#.#.#.#",
    "#.......#",
    "####D####"
  ]
}
```

`content/rooms/cathedral/collapsed-room.json`:

```json
{
  "id": "collapsed-room",
  "tags": ["filler"],
  "weight": 3,
  "grid": [
    "####D####",
    "#.##....#",
    "#..#....#",
    "#.......#",
    "D.......D",
    "#....#..#",
    "#.S.##..#",
    "#.......#",
    "####D####"
  ]
}
```

`content/rooms/cathedral/corridor-ew.json`:

```json
{
  "id": "corridor-ew",
  "tags": ["corridor"],
  "weight": 2,
  "grid": [
    "#########",
    "#########",
    "#########",
    "#.......#",
    "D.......D",
    "#.......#",
    "#########",
    "#########",
    "#########"
  ]
}
```

`content/rooms/cathedral/corridor-ns.json`:

```json
{
  "id": "corridor-ns",
  "tags": ["corridor"],
  "weight": 2,
  "grid": [
    "####D####",
    "###...###",
    "###...###",
    "###...###",
    "###...###",
    "###...###",
    "###...###",
    "###...###",
    "####D####"
  ]
}
```

`content/rooms/cathedral/crossroads.json`:

```json
{
  "id": "crossroads",
  "tags": ["filler"],
  "weight": 3,
  "grid": [
    "####D####",
    "###...###",
    "###...###",
    "#.......#",
    "D.......D",
    "#.......#",
    "###...###",
    "###...###",
    "####D####"
  ]
}
```

`content/rooms/cathedral/crypt-niche.json`:

```json
{
  "id": "crypt-niche",
  "tags": ["dead_end"],
  "grid": [
    "####D####",
    "#.......#",
    "#.......#",
    "#.##.##.#",
    "D.#...#.D",
    "#.#.S.#.#",
    "#.#####.#",
    "#.......#",
    "####D####"
  ]
}
```

`content/rooms/cathedral/entrance.json`:

```json
{
  "id": "entrance",
  "tags": ["entrance"],
  "grid": [
    "####D####",
    "#.......#",
    "#.#...#.#",
    "#.......#",
    "D...E...D",
    "#.......#",
    "#.#...#.#",
    "#.......#",
    "####D####"
  ]
}
```

`content/rooms/cathedral/exit.json`:

```json
{
  "id": "exit",
  "tags": ["exit"],
  "grid": [
    "####D####",
    "#.......#",
    "#.......#",
    "#..#.#..#",
    "D...X...D",
    "#..#.#..#",
    "#.......#",
    "#.......#",
    "####D####"
  ]
}
```

`content/rooms/cathedral/great-hall.json`:

```json
{
  "id": "great-hall",
  "tags": ["filler"],
  "grid": [
    "####D#######D####",
    "#...............#",
    "#.#...#...#...#.#",
    "#...............#",
    "D...S.......S...D",
    "#...............#",
    "#.#...#...#...#.#",
    "#...............#",
    "#.......C.......#",
    "#...............#",
    "#.#...#...#...#.#",
    "#...............#",
    "D...S.......S...D",
    "#...............#",
    "#.#...#...#...#.#",
    "#...............#",
    "####D#######D####"
  ]
}
```

`content/rooms/cathedral/guard-room.json`:

```json
{
  "id": "guard-room",
  "tags": ["filler"],
  "weight": 3,
  "grid": [
    "####D####",
    "#.......#",
    "#.S.....#",
    "#..#.#..#",
    "D.......D",
    "#..#.#..#",
    "#.......#",
    "#.......#",
    "####D####"
  ]
}
```

`content/rooms/cathedral/library.json`:

```json
{
  "id": "library",
  "tags": ["filler"],
  "weight": 3,
  "grid": [
    "####D####",
    "#.......#",
    "#.##.##.#",
    "#.......#",
    "D.##.##.D",
    "#.......#",
    "#.##.##.#",
    "#...S...#",
    "####D####"
  ]
}
```

`content/rooms/cathedral/nave-ew.json`:

```json
{
  "id": "nave-ew",
  "tags": ["filler"],
  "grid": [
    "####D#######D####",
    "#...............#",
    "#.#.#.#.#.#.#.#.#",
    "#...............#",
    "D.......S.......D",
    "#...............#",
    "#.#.#.#.#.#.#.#.#",
    "#...............#",
    "####D#######D####"
  ]
}
```

`content/rooms/cathedral/nave-ns.json`:

```json
{
  "id": "nave-ns",
  "tags": ["filler"],
  "grid": [
    "####D####",
    "#.......#",
    "#.#...#.#",
    "#.......#",
    "D.......D",
    "#.#...#.#",
    "#.......#",
    "#.#...#.#",
    "#...S...#",
    "#.#...#.#",
    "#.......#",
    "#.#...#.#",
    "D.......D",
    "#.......#",
    "#.#...#.#",
    "#.......#",
    "####D####"
  ]
}
```

`content/rooms/cathedral/ossuary.json`:

```json
{
  "id": "ossuary",
  "tags": ["filler"],
  "weight": 3,
  "grid": [
    "####D####",
    "#.......#",
    "#.##.##.#",
    "#.#...#.#",
    "D...C...D",
    "#.#...#.#",
    "#.##.##.#",
    "#.......#",
    "####D####"
  ]
}
```

`content/rooms/cathedral/pillared-room.json`:

```json
{
  "id": "pillared-room",
  "tags": ["filler"],
  "weight": 3,
  "grid": [
    "####D####",
    "#.......#",
    "#.#...#.#",
    "#.......#",
    "D.......D",
    "#...S...#",
    "#.#...#.#",
    "#.......#",
    "####D####"
  ]
}
```

`content/rooms/cathedral/refectory.json`:

```json
{
  "id": "refectory",
  "tags": ["filler"],
  "weight": 3,
  "grid": [
    "####D####",
    "#.......#",
    "#.#####.#",
    "#.......#",
    "D.......D",
    "#.......#",
    "#.#####.#",
    "#...S...#",
    "####D####"
  ]
}
```

`content/rooms/cathedral/shrine.json`:

```json
{
  "id": "shrine",
  "tags": ["waypoint"],
  "grid": [
    "####D####",
    "#.......#",
    "#.#...#.#",
    "#...W...#",
    "D.......D",
    "#.......#",
    "#.#...#.#",
    "#.......#",
    "####D####"
  ]
}
```

`content/rooms/cathedral/side-chapel.json`:

```json
{
  "id": "side-chapel",
  "tags": ["filler"],
  "weight": 3,
  "grid": [
    "####D####",
    "#.......#",
    "#...#...#",
    "#..###..#",
    "D.......D",
    "#...S...#",
    "#.......#",
    "#.......#",
    "####D####"
  ]
}
```

`content/rooms/cathedral/small-shrine.json`:

```json
{
  "id": "small-shrine",
  "tags": ["dead_end"],
  "grid": [
    "####D####",
    "#.......#",
    "#..#.#..#",
    "#.......#",
    "D...C...D",
    "#.......#",
    "#..#.#..#",
    "#.......#",
    "####D####"
  ]
}
```

`content/areas/cathedral-1.json`:

```json
{
  "id": "cathedral-1",
  "mode": "indoor",
  "tileset": "../tilesets/cathedral.json",
  "targetSize": { "width": 49, "height": 49, "tolerance": 15 },
  "slotSize": 9,
  "rooms": ["../rooms/cathedral"],
  "maxSideBranches": 3,
  "maxAttempts": 50,
  "graph": {
    "nodes": [
      { "id": "entrance", "kind": "entrance", "region": { "x": [0.0, 0.1], "y": [0.3, 0.7] } },
      { "id": "shrine", "kind": "waypoint", "region": { "x": [0.3, 0.7], "y": [0.0, 0.1] } },
      { "id": "exit", "kind": "exit", "region": { "x": [0.9, 1.0], "y": [0.9, 1.0] } }
    ],
    "edges": [
      { "from": "entrance", "to": "shrine", "kind": "path" },
      { "from": "shrine", "to": "exit", "kind": "path" },
      { "from": "entrance", "to": "exit", "kind": "branch" }
    ]
  }
}
```

`content/areas/cathedral-2.json`:

```json
{
  "id": "cathedral-2",
  "mode": "indoor",
  "tileset": "../tilesets/cathedral.json",
  "targetSize": { "width": 57, "height": 57, "tolerance": 15 },
  "slotSize": 9,
  "rooms": ["../rooms/cathedral"],
  "maxSideBranches": 4,
  "maxAttempts": 50,
  "graph": {
    "nodes": [
      { "id": "entrance", "kind": "entrance", "region": { "x": [0.0, 0.1], "y": [0.3, 0.7] } },
      { "id": "shrine", "kind": "waypoint", "region": { "x": [0.3, 0.7], "y": [0.0, 0.1] } },
      { "id": "exit", "kind": "exit", "region": { "x": [0.9, 1.0], "y": [0.9, 1.0] } }
    ],
    "edges": [
      { "from": "entrance", "to": "shrine", "kind": "path" },
      { "from": "shrine", "to": "exit", "kind": "path" },
      { "from": "entrance", "to": "exit", "kind": "branch" }
    ]
  }
}
```

`content/areas/cathedral-3.json`:

```json
{
  "id": "cathedral-3",
  "mode": "indoor",
  "tileset": "../tilesets/cathedral.json",
  "targetSize": { "width": 65, "height": 65, "tolerance": 15 },
  "slotSize": 9,
  "rooms": ["../rooms/cathedral"],
  "maxSideBranches": 6,
  "maxAttempts": 50,
  "graph": {
    "nodes": [
      { "id": "entrance", "kind": "entrance", "region": { "x": [0.0, 0.1], "y": [0.3, 0.7] } },
      { "id": "shrine", "kind": "waypoint", "region": { "x": [0.3, 0.7], "y": [0.0, 0.1] } },
      { "id": "exit", "kind": "exit", "region": { "x": [0.9, 1.0], "y": [0.9, 1.0] } }
    ],
    "edges": [
      { "from": "entrance", "to": "shrine", "kind": "path" },
      { "from": "shrine", "to": "exit", "kind": "path" },
      { "from": "entrance", "to": "exit", "kind": "branch" }
    ]
  }
}
```

`content/areas/cathedral-4.json`:

```json
{
  "id": "cathedral-4",
  "mode": "indoor",
  "tileset": "../tilesets/cathedral.json",
  "targetSize": { "width": 73, "height": 73, "tolerance": 15 },
  "slotSize": 9,
  "rooms": ["../rooms/cathedral"],
  "maxSideBranches": 8,
  "maxAttempts": 50,
  "graph": {
    "nodes": [
      { "id": "entrance", "kind": "entrance", "region": { "x": [0.0, 0.1], "y": [0.3, 0.7] } },
      { "id": "shrine", "kind": "waypoint", "region": { "x": [0.3, 0.7], "y": [0.0, 0.1] } },
      { "id": "boss", "kind": "boss", "region": { "x": [0.9, 1.0], "y": [0.3, 0.7] } },
      { "id": "exit", "kind": "exit", "region": { "x": [0.3, 0.7], "y": [0.9, 1.0] } }
    ],
    "edges": [
      { "from": "entrance", "to": "shrine", "kind": "path" },
      { "from": "shrine", "to": "boss", "kind": "path" },
      { "from": "boss", "to": "exit", "kind": "path" },
      { "from": "entrance", "to": "exit", "kind": "branch" }
    ]
  }
}
```

- [ ] **Step 4: Run the content tests and review the new snapshots**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~ContentAreaTests"`
Expected: the four `Cathedral_MatchesSnapshot` cases FAIL with "Snapshot created at …"; every other case PASSES.

Open each `tests/Mortar.Tests/Snapshots/cathedral-N-seed-1.txt` and check: one `E` and one `X`, one `W`; a `B` only in `cathedral-4`; walls close every room; no floor touches void. Then run the same command again.
Expected: PASS.

- [ ] **Step 5: Run the full suite**

Run: `dotnet test`
Expected: all tests pass; `crypt-seed-*.txt` and `game-seed-7.txt` are unchanged (`git status` shows no change under `tests/Mortar.Tests/Snapshots/` other than the four new files).

- [ ] **Step 6: Commit**

```bash
git add content/tilesets/cathedral.json content/rooms/cathedral content/areas/cathedral-1.json content/areas/cathedral-2.json content/areas/cathedral-3.json content/areas/cathedral-4.json tests/Mortar.Tests/Support/Snapshots.cs tests/Mortar.Tests/Support/TestPaths.cs tests/Mortar.Tests/Generation/SnapshotTests.cs tests/Mortar.Tests/Gameplay/DeterminismTests.cs tests/Mortar.Tests/Generation/ContentAreaTests.cs tests/Mortar.Tests/Snapshots/cathedral-1-seed-1.txt tests/Mortar.Tests/Snapshots/cathedral-2-seed-1.txt tests/Mortar.Tests/Snapshots/cathedral-3-seed-1.txt tests/Mortar.Tests/Snapshots/cathedral-4-seed-1.txt
git commit -m "add cathedral content"
```

---

### Task 7: Town content

**Files:**
- Create: `content/tilesets/town.json`, `content/areas/town.json`, `content/maps/town.json`
- Create: `tests/Mortar.Tests/Cli/MapRendererTests.cs`
- Modify: `tests/Mortar.Tests/Generation/ContentAreaTests.cs`, `tests/Mortar.Tests/Cli/CliTests.cs`, `src/Mortar.Cli/Preview/MapRenderer.cs`
- Snapshot (generated, then reviewed): `tests/Mortar.Tests/Snapshots/town-seed-1.txt`

**Interfaces:**
- Consumes: custom markers (Task 3), fixed areas (Tasks 4–5), `Snapshots.AssertMatches` and `TestPaths.ContentArea` (Task 6).
- Produces: the `town` area that Task 9 wires into `levels.json`; `MapRenderer.GroundColor` becomes `internal` so tests can check colours for new ground types.

- [ ] **Step 1: Write the failing tests**

Add to `ContentAreaTests`:

```csharp
    [Fact]
    public void Town_MatchesSnapshot()
    {
        var area = Load("town");
        var result = Generator.Generate(area, 1);
        Assert.True(result.Succeeded, result.FailureSummary);
        Snapshots.AssertMatches("town-seed-1.txt", AsciiMap.Render(result.Layout!, area.Tileset));
    }

    [Fact]
    public void Town_GroundIsTheSameForEverySeed()
    {
        var area = Load("town");
        var first = Generator.Generate(area, 1).Layout!.Cells;
        var second = Generator.Generate(area, 2).Layout!.Cells;
        for (var y = 0; y < first.Height; y++)
        {
            for (var x = 0; x < first.Width; x++)
            {
                Assert.Equal(first.GroundAt(x, y), second.GroundAt(x, y));
            }
        }
    }

    [Fact]
    public void Town_HasItsMarkers()
    {
        var layout = Generator.Generate(Load("town"), 1).Layout!;
        Assert.Equal(
            new[] { "entrance", "exit", "healer", "sage", "smith" },
            layout.Markers.Select(m => m.Kind).OrderBy(k => k, StringComparer.Ordinal));
    }
```

Add to `CliTests`:

```csharp
    [Theory]
    [InlineData("town")]
    [InlineData("cathedral-1")]
    [InlineData("cathedral-2")]
    [InlineData("cathedral-3")]
    [InlineData("cathedral-4")]
    public void GenerateAndPreview_WorkOnShippedAreas(string name)
    {
        using var temp = new TempDir();
        var layoutPath = Path.Combine(temp.Root, "layout.json");
        var pngPath = Path.Combine(temp.Root, "map.png");
        Assert.Equal(ExitCodes.Success, Run("generate", "--area", TestPaths.ContentArea(name), "--seed", "1", "--out", layoutPath).Code);
        Assert.Equal(ExitCodes.Success, Run("preview", "--layout", layoutPath, "--out", pngPath).Code);
        Assert.True(File.Exists(pngPath));
    }
```

Create `tests/Mortar.Tests/Cli/MapRendererTests.cs`:

```csharp
using Mortar.Cli.Preview;

namespace Mortar.Tests.Cli;

public class MapRendererTests
{
    [Fact]
    public void GroundColor_UnknownGroundIsStable() =>
        Assert.Equal(MapRenderer.GroundColor("grass"), MapRenderer.GroundColor("grass"));

    [Fact]
    public void GroundColor_DifferentGroundsDiffer() =>
        Assert.NotEqual(MapRenderer.GroundColor("grass"), MapRenderer.GroundColor("road"));

    [Fact]
    public void GroundColor_KnownGroundUsesItsColor() =>
        Assert.Equal(new Rgb(196, 186, 166), MapRenderer.GroundColor("floor"));
}
```

In `src/Mortar.Cli/Preview/MapRenderer.cs`, change `private static Rgb GroundColor` to `internal static Rgb GroundColor`. (`Rgb` is a `readonly record struct`, so `Assert.Equal` compares values.)

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~ContentAreaTests|FullyQualifiedName~CliTests|FullyQualifiedName~MapRendererTests"`
Expected: the town tests and the `town` CLI case FAIL (`content/areas/town.json` does not exist); the rest PASS.

- [ ] **Step 2: Add the town content**

Create each file below with exactly the content shown.

`content/tilesets/town.json`:

```json
{
  "groundTypes": [
    { "id": "void", "char": " ", "walkable": false, "blocksSight": false },
    { "id": "floor", "char": ".", "walkable": true, "blocksSight": false },
    { "id": "wall", "char": "#", "walkable": false, "blocksSight": true },
    { "id": "grass", "char": ",", "walkable": true, "blocksSight": false },
    { "id": "road", "char": "=", "walkable": true, "blocksSight": false },
    { "id": "water", "char": "~", "walkable": false, "blocksSight": false },
    { "id": "tree", "char": "T", "walkable": false, "blocksSight": true }
  ],
  "variantWeights": {
    "grass": [6, 2, 1],
    "floor": [4, 1]
  },
  "markers": [
    { "char": "F", "kind": "smith" },
    { "char": "H", "kind": "healer" },
    { "char": "G", "kind": "sage" }
  ]
}
```

`content/areas/town.json`:

```json
{
  "id": "town",
  "mode": "fixed",
  "tileset": "../tilesets/town.json",
  "map": "../maps/town.json"
}
```

`content/maps/town.json`:

```json
{
  "grid": [
    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
    "TTTT,,,,TT,,,~~,,,,,T,,,,,,,,,,TT,,,,,,,,,,,TTTT",
    "TTT,,,,,,,,,,~~,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,TTT",
    "TT,,,,,,,,,,,~~,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,TT",
    "TT,,,,,,,,,,,~~,,TT,,,,,,,,,,,,,,,,,,,,,,,,,,,TT",
    "TT,,,,,,,,,,,~~,,T,,,,,,,,,,,,,,,,TT,,,,,,,,,,TT",
    "TT,,,,,,,,,,,~~,,,,,#######,,,,,,,,T,,,,,,,,,,TT",
    "TT,,,,,,,,,,,~~,,,,,#######,,,,,,,,,,,,,TT,,,,TT",
    "TT,,######,,,~~,,,,,#######,,,,,,,,,,,,,,,,,,,TT",
    "TT,,######,,,~~,,,,,#######,,,,,,,,,,,,,,,,,,TTT",
    "TT,,######,,,~~,,,,,#######,,,,,,,,,,,,,,,,,,,TT",
    "TT,,######,,,~~,,,,,#######,,,,,,,,,,,,,,,,,,,TT",
    "TT,,######,,,~~,,,,,,,,=,,,,,,,,,,,,,,,,,,,,,,TT",
    "TT,,######,,,~~,,,,,,,,F,,,,,,,,,,,,,,,,,,,,,,TT",
    "TT,,,,=,,,,,,~~~,,,,,,,=,,,,,,,,,,,,,,########TT",
    "TT,,,,G,,,,,,~~~,,,,,,,=,,,,,,,,,,,,,,########TT",
    "TT,,,,=,,,,,,,~~,,,,,,,=,,,,,,,,,,,,,,########TT",
    "TT,,,,=,,,,,,,~~,,,,............,,,,,,########TT",
    "TT,,,,=,,,,,,,~~,,,,............,,,,,,########TT",
    "TT,,,,=,,,,,,,~~,,,,............,,,,,,########TT",
    "TT,,,,=,,,,,,,~~,,,,............,,,...########TT",
    "TT,,,,=,,,,,,,~~,,,,............,,,...########TT",
    "TT==================.....E......===.X.########TT",
    "TT==================............===...########TT",
    "TT,,,,,,,,,,,,~~,,,,............,,,...########TT",
    "TT,,,,,,,,,,,,~~,,,,............,,,...########TT",
    "TT,,,,,,,,,,,,~~,,,,............,,,,,,########TT",
    "TT,,,,,,,,,,,,~~,,,,............,,,,,,########TT",
    "TT,,,,,,,,,,,,~~,,,,............,,,,,,########TT",
    "TTT,,,,,,TT,,,~~,,,,,,,,,,,,,=,,,,,,,,########TT",
    "TTT,,,,,,,T,,,~~,,,,,,,,,,,,,=,,,,,,,,########TT",
    "TT,,,,,,,,,,,~~,,,,,,,,,,,,,,=,,,,,,,,########TT",
    "TT,,,,,,,,,,,~~,,,,,,,,,,,,,,H,,,,,,,,,,,,,,,,TT",
    "TT,,,,,,,,,,,~~,,,,,,,,,,,,,,=,,,,,,,,,,,,,,,,TT",
    "TT,,,,,,,,,,,~~,,,,,,,,,,,#######,,,,,,,,,,,,,TT",
    "TT,,,,,,,,,,,~~,,,,,,,,,,,#######,,,,,,,,,,,,,TT",
    "TT,,,,,,,,,,,~~,,,,,,,,,,,#######,,,,,,,,,,,,,TT",
    "TT,,,TT,,,,,,~~,,,,,TT,,,,#######,,,,,,,,,,,,TTT",
    "TT,,,,,,,,,,,~~,,,,,,T,,,,#######,,,,,,,,,,,,TTT",
    "TT,,,,,,,,,,,~~,,,,,,,,,,,#######,,,TT,,,,,,,,TT",
    "TT,,,,,,,,,,,~~,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,TT",
    "TT,,,,,,,,,,,~~,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,TT",
    "TT,,,,,,,,,,,~~,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,TT",
    "TTT,,,,,,,,,,~~,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,TTT",
    "TTTT,,,,,,,,T~~,,,,,,,,,,,,T,,,,,,,,,,TT,,,,TTTT",
    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT"
  ]
}
```

- [ ] **Step 3: Run the tests and review the snapshot**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~ContentAreaTests|FullyQualifiedName~CliTests|FullyQualifiedName~MapRendererTests"`
Expected: `Town_MatchesSnapshot` FAILS with "Snapshot created at …"; everything else PASSES.

Open `tests/Mortar.Tests/Snapshots/town-seed-1.txt` and check it matches `content/maps/town.json` cell for cell, with `F`, `G`, and `H` where the map has them. Run the command again.
Expected: PASS.

- [ ] **Step 4: Run the full suite**

Run: `dotnet test`
Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add content/tilesets/town.json content/areas/town.json content/maps/town.json src/Mortar.Cli/Preview/MapRenderer.cs tests/Mortar.Tests/Generation/ContentAreaTests.cs tests/Mortar.Tests/Cli/CliTests.cs tests/Mortar.Tests/Cli/MapRendererTests.cs tests/Mortar.Tests/Snapshots/town-seed-1.txt
git commit -m "add town content"
```

---

### Task 8: Town support in the game simulation

**Files:**
- Modify: `src/Mortar.Game/Content/ContentSet.cs`, `src/Mortar.Game/Content/ContentValidator.cs`, `src/Mortar.Game/Simulation/LevelEntry.cs`, `src/Mortar.Game/Simulation/Stairs.cs`, `src/Mortar.Game/Simulation/World.cs`, `src/Mortar.Game/Simulation/MonsterSpawner.cs`, `src/Mortar.Game/Saves/LevelRestorer.cs`
- Modify (test support): `tests/Mortar.Tests/Gameplay/Support/TestGameContent.cs`, `tests/Mortar.Tests/Gameplay/Support/TestLevels.cs`, `tests/Mortar.Tests/Gameplay/Support/TestWorlds.cs`
- Test: `tests/Mortar.Tests/Gameplay/TownTests.cs` (create), `tests/Mortar.Tests/Gameplay/ContentValidatorTests.cs`, `tests/Mortar.Tests/Gameplay/StairsTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks (the game sees only `LevelDefinition`s and `ILevelGenerator`).
- Produces: `ContentSet.ShallowestLevel` (0 with a town, else 1); `ContentSet.DeepestLevel` is now the largest depth; `ContentSet.Level(depth)` accepts depth 0; a level with depth 0 is the town. Test helpers `TestGameContent.Town()`, `TestGameContent.WithTown()`, `FixedLevelGenerator.WithTown(string[] town, params string[][] levels)`, `TestWorlds.Town`, `TestWorlds.CreateWithTown(ContentSet? content = null)`.

- [ ] **Step 1: Add the test helpers**

In `tests/Mortar.Tests/Gameplay/Support/TestGameContent.cs`, add after `Level`:

```csharp
    public static LevelDefinition Town(string areaId = "town") =>
        new LevelDefinition(0, areaId, Array.Empty<SpawnEntry>(), null);

    public static LevelDefinition[] WithTown() => new[] { Town(), Level(1), Level(2) };
```

In `tests/Mortar.Tests/Gameplay/Support/TestLevels.cs`, replace `FixedLevelGenerator` with:

```csharp
internal sealed class FixedLevelGenerator : ILevelGenerator
{
    private readonly string[]? _town;
    private readonly string[][] _levels;

    public FixedLevelGenerator(params string[][] levels)
        : this(null, levels)
    {
    }

    private FixedLevelGenerator(string[]? town, string[][] levels)
    {
        _town = town;
        _levels = levels;
    }

    /// <summary>Depth 0 is built from <paramref name="town"/>; depth n from the nth entry of <paramref name="levels"/>.</summary>
    public static FixedLevelGenerator WithTown(string[] town, params string[][] levels) => new FixedLevelGenerator(town, levels);

    public LevelBuildResult Generate(LevelDefinition level, uint seed)
    {
        var rows = level.Depth == 0 ? _town : level.Depth <= _levels.Length ? _levels[level.Depth - 1] : null;
        return rows is null ? LevelBuildResult.Failure("no test layout") : LevelBuildResult.Success(TestLevels.FromAscii(rows));
    }
}
```

In `tests/Mortar.Tests/Gameplay/Support/TestWorlds.cs`, add after `Room`:

```csharp
    public static readonly string[] Town =
    {
        "#######",
        "#E.S.X#",
        "#.....#",
        "#######",
    };
```

and after `Create`:

```csharp
    public static World CreateWithTown(ContentSet? content = null)
    {
        var generator = FixedLevelGenerator.WithTown(Town, Room, Room);
        var result = World.Create(content ?? TestGameContent.Valid(levels: TestGameContent.WithTown()), generator, "warrior", 1);
        return result.World ?? throw new InvalidOperationException(result.Error);
    }
```

- [ ] **Step 2: Write the failing tests**

Create `tests/Mortar.Tests/Gameplay/TownTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Events;
using Mortar.Game.Saves;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class TownTests
{
    private static readonly Command[] Stairs = { new UseStairsCommand() };

    [Fact]
    public void NewGame_StartsInTownAtItsEntrance()
    {
        var world = TestWorlds.CreateWithTown();
        Assert.Equal(0, world.Depth);
        Assert.Equal(new Cell(1, 1), world.Player.Cell);
        Assert.Equal(Actor.PlayerId, Assert.Single(world.Level.Actors).Id);
    }

    [Fact]
    public void TownExit_LeadsToLevelOne()
    {
        var world = TestWorlds.CreateWithTown();
        var town = world.Level;
        town.Move(world.Player, new Cell(4, 1));
        var changed = world.Tick(Stairs).OfType<LevelChanged>().Single();
        Assert.Equal((0, 1), (changed.FromDepth, changed.ToDepth));
        Assert.Equal(new Cell(1, 1), world.Player.Cell);
        Assert.Null(town.Find(Actor.PlayerId));
    }

    [Fact]
    public void LevelOneEntrance_LeadsBackToTheTownExit()
    {
        var world = TestWorlds.CreateWithTown();
        var town = world.Level;
        town.Move(world.Player, new Cell(4, 1));
        world.Tick(Stairs);
        world.Tick(Stairs);
        Assert.Equal(0, world.Depth);
        Assert.Same(town, world.Level);
        Assert.Equal(new Cell(5, 1), world.Player.Cell);
    }

    [Fact]
    public void TownEntrance_IsRejected()
    {
        var world = TestWorlds.CreateWithTown();
        Assert.Equal("there is nothing above", world.Tick(Stairs).OfType<CommandRejected>().Single().Reason);
    }

    [Fact]
    public void Save_RoundTripsInTown() => AssertRoundTrip(TestWorlds.CreateWithTown());

    [Fact]
    public void Save_RoundTripsAfterLeavingTown()
    {
        var world = TestWorlds.CreateWithTown();
        world.Level.Move(world.Player, new Cell(4, 1));
        world.Tick(Stairs);
        AssertRoundTrip(world);
    }

    private static void AssertRoundTrip(World world)
    {
        var generator = FixedLevelGenerator.WithTown(TestWorlds.Town, TestWorlds.Room, TestWorlds.Room);
        var restored = World.Restore(WorldSnapshot.Capture(world), world.Content, generator);
        Assert.True(restored.Succeeded, restored.Error);
        Assert.Equal(WorldHash.Compute(world), WorldHash.Compute(restored.World!));
    }
}
```

Add to `ContentValidatorTests`:

```csharp
    [Fact]
    public void Town_IsDepthZero()
    {
        var content = TestGameContent.Valid(levels: TestGameContent.WithTown());
        Assert.Equal(0, content.ShallowestLevel);
        Assert.Equal(2, content.DeepestLevel);
        Assert.Equal("town", content.Level(0).AreaId);
        Assert.Equal(2, content.Level(2).Depth);
    }

    [Fact]
    public void WithoutTown_ShallowestLevelIsOne() => Assert.Equal(1, TestGameContent.Valid().ShallowestLevel);

    [Fact]
    public void TownOnly_IsRejected() =>
        AssertError(TestGameContent.Create(levels: new[] { TestGameContent.Town() }), "levels", "at least one dungeon level is required");

    [Fact]
    public void TwoTowns_AreRejected()
    {
        var levels = new[] { TestGameContent.Town(), TestGameContent.Town(), TestGameContent.Level(1) };
        AssertError(TestGameContent.Create(levels: levels), "levels", "there must be at most one town");
    }

    [Fact]
    public void TownWithSpawns_IsRejected()
    {
        var town = new LevelDefinition(0, "town", new[] { new SpawnEntry("zombie", 1) }, null);
        AssertError(TestGameContent.Create(levels: new[] { town, TestGameContent.Level(1) }), "town", "the town must not have spawns");
    }

    [Fact]
    public void TownWithBoss_IsRejected()
    {
        var town = new LevelDefinition(0, "town", Array.Empty<SpawnEntry>(), "zombie");
        AssertError(TestGameContent.Create(levels: new[] { town, TestGameContent.Level(1) }), "town", "the town must not have a boss");
    }

    [Fact]
    public void TownWithoutArea_IsRejected() =>
        AssertError(TestGameContent.Create(levels: new[] { TestGameContent.Town(""), TestGameContent.Level(1) }), "town", "area is required");
```

In `StairsTests.LevelOneEntrance_IsRejected`, change the expected reason to `"there is nothing above"`.

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~TownTests|FullyQualifiedName~ContentValidatorTests|FullyQualifiedName~StairsTests"`
Expected: build FAIL (`ContentSet.ShallowestLevel` does not exist).

- [ ] **Step 3: Implement the town in the simulation**

In `src/Mortar.Game/Content/ContentSet.cs`, replace `public int DeepestLevel => Levels.Count;` with:

```csharp
    /// <summary>0 when the content has a town, otherwise 1.</summary>
    public int ShallowestLevel => Levels[0].Depth;

    public int DeepestLevel => Levels[Levels.Count - 1].Depth;
```

and replace `Level` with:

```csharp
    public LevelDefinition Level(int depth) => Levels[depth - ShallowestLevel];
```

In `src/Mortar.Game/Content/ContentValidator.cs`, replace `CheckLevels` with:

```csharp
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
        var towns = levels.Where(level => level.Depth == 0).ToList();
        var dungeon = levels.Where(level => level.Depth != 0).ToList();
        if (towns.Count > 1) errors.Add("levels", "there must be at most one town");
        if (dungeon.Count == 0) errors.Add("levels", "at least one dungeon level is required");
        var depths = dungeon.Select(level => level.Depth).OrderBy(depth => depth).ToList();
        for (var i = 0; i < depths.Count; i++)
        {
            if (depths[i] != i + 1)
            {
                errors.Add("levels", "depths must be 1, 2, 3, ... with no gaps or repeats");
                break;
            }
        }
        foreach (var town in towns) CheckTown(town, errors);
        foreach (var level in dungeon) CheckLevel(level, find, errors);
    }

    private static void CheckTown(LevelDefinition town, List<AuthoringError> errors)
    {
        const string source = "town";
        if (string.IsNullOrWhiteSpace(town.AreaId)) errors.Add(source, "area is required");
        if (town.Spawns.Count > 0) errors.Add(source, "the town must not have spawns");
        if (town.BossId is not null) errors.Add(source, "the town must not have a boss");
    }
```

In `src/Mortar.Game/Simulation/LevelEntry.cs`, in `Enter`:

1. Add as the first line: `var current = world.FindLevel(world.Depth);` (it must run before `Build`, which may add the level at the current depth on the first entry).
2. Replace the `if (world.Depth != 0) { world.Level.Remove(world.Player); }` block with `current?.Remove(world.Player);`.

In `src/Mortar.Game/Simulation/Stairs.cs`, replace the `depth < 1` check in `Travel` with:

```csharp
        if (depth < world.Content.ShallowestLevel)
        {
            return "there is nothing above";
        }
```

In `src/Mortar.Game/Simulation/World.cs`, in `Create`, replace `LevelEntry.Enter(world, 1, MarkerKinds.Entrance)` with `LevelEntry.Enter(world, content.ShallowestLevel, MarkerKinds.Entrance)`.

In `src/Mortar.Game/Saves/LevelRestorer.cs`, replace `depth >= 1` with `depth >= world.Content.ShallowestLevel`.

In `src/Mortar.Game/Simulation/MonsterSpawner.cs`, change the first line of the marker loop to:

```csharp
            if (marker.Kind != MarkerKinds.Spawn || weights.Count == 0) continue;
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~TownTests|FullyQualifiedName~ContentValidatorTests|FullyQualifiedName~StairsTests"`
Expected: PASS. Then run `dotnet test`; expected: all tests pass and `game-seed-7.txt` is unchanged (the repo content has no town yet).

- [ ] **Step 5: Commit**

```bash
git add src/Mortar.Game/Content/ContentSet.cs src/Mortar.Game/Content/ContentValidator.cs src/Mortar.Game/Simulation/LevelEntry.cs src/Mortar.Game/Simulation/Stairs.cs src/Mortar.Game/Simulation/World.cs src/Mortar.Game/Simulation/MonsterSpawner.cs src/Mortar.Game/Saves/LevelRestorer.cs tests/Mortar.Tests/Gameplay/Support/TestGameContent.cs tests/Mortar.Tests/Gameplay/Support/TestLevels.cs tests/Mortar.Tests/Gameplay/Support/TestWorlds.cs tests/Mortar.Tests/Gameplay/TownTests.cs tests/Mortar.Tests/Gameplay/ContentValidatorTests.cs tests/Mortar.Tests/Gameplay/StairsTests.cs
git commit -m "start new games in an optional town"
```

---

### Task 9: Wire the town and cathedral into the game content

**Files:**
- Modify: `content/levels.json`, `src/Mortar.Json/Content/ContentDtos.cs`, `src/Mortar.Json/Content/LevelFileLoader.cs`
- Modify (tests): `tests/Mortar.Tests/Support/ContentFixture.cs`, `tests/Mortar.Tests/Json/GameContentLoaderTests.cs`
- Regenerate: `tests/Mortar.Tests/Snapshots/game-seed-7.txt`
- Docs: `docs/design.md`, `docs/authoring.md`, `docs/content.md`, `README.md`

**Interfaces:**
- Consumes: the cathedral and town areas (Tasks 6–7), `CompiledArea.Map` (Task 4), town support (Task 8).
- Produces: `levels.json` accepts an optional `"town": { "area": "…" }`; `LevelsDto.Town`, `TownDto` (`Area`).

- [ ] **Step 1: Write the failing tests**

In `tests/Mortar.Tests/Support/ContentFixture.cs`, delete the three lines that rewrite `levels.json` (from `var levels = …` to the `File.WriteAllText(levels, …)` call) so `Copy` only copies files.

In `tests/Mortar.Tests/Json/GameContentLoaderTests.cs`, add `using Mortar.Core.Model;`, `using Mortar.Game;`, and `using Mortar.Game.Commands;`, then replace `RepoContent_Loads` and `RepoContent_CreatesAPlayableWorld` with:

```csharp
    [Fact]
    public void RepoContent_Loads()
    {
        var result = GameContentLoader.Load(ContentFixture.Manifest);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        var content = result.Value!.Content;
        Assert.True(content.TryGetClass("warrior", out _));
        Assert.Equal(6, content.Monsters.Count);
        Assert.Equal(0, content.ShallowestLevel);
        Assert.Equal(4, content.DeepestLevel);
        Assert.Equal("town", content.Level(0).AreaId);
        Assert.Equal("gravewarden", content.Level(4).BossId);
        Assert.Equal(
            new[] { "cathedral-1", "cathedral-2", "cathedral-3", "cathedral-4", "town" },
            result.Value.Areas.Keys.OrderBy(id => id, StringComparer.Ordinal));
    }

    [Fact]
    public void RepoContent_StartsInAQuietTownAboveAPopulatedCathedral()
    {
        var content = GameContentLoader.Load(ContentFixture.Manifest).Value!;
        var created = World.Create(content.Content, content.CreateLevelGenerator(), "warrior", 1);
        Assert.True(created.Succeeded, created.Error);
        var world = created.World!;
        Assert.Equal(0, world.Depth);
        Assert.DoesNotContain(world.Level.Actors, actor => actor.Kind == ActorKind.Monster);
        world.Level.Move(world.Player, world.Level.FindMarker(MarkerKinds.Exit)!.Value);
        world.Tick(new Command[] { new UseStairsCommand() });
        Assert.Equal(1, world.Depth);
        Assert.Contains(world.Level.Actors, actor => actor.Kind == ActorKind.Monster);
    }

    [Fact]
    public void TownSpawns_AreAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var levels = Path.Combine(temp.Root, "levels.json");
        File.WriteAllText(levels, File.ReadAllText(levels).Replace("\"area\": \"areas/town.json\"", "\"area\": \"areas/town.json\", \"spawns\": []"));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == levels && e.Message.StartsWith("invalid JSON") && e.Message.Contains("spawns"));
    }

    [Fact]
    public void TownWithIndoorArea_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var levels = Path.Combine(temp.Root, "levels.json");
        File.WriteAllText(levels, File.ReadAllText(levels).Replace("\"area\": \"areas/town.json\"", "\"area\": \"areas/cathedral-1.json\""));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == levels && e.Message == "the town area must use mode 'fixed'");
    }

    [Fact]
    public void TownMapWithoutExit_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var map = Path.Combine(temp.Root, "maps", "town.json");
        File.WriteAllText(map, File.ReadAllText(map).Replace('X', '.'));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Message == "the town map must have exactly one exit (X), found 0");
    }
```

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~GameContentLoaderTests"`
Expected: FAIL (the repo content has no town and still uses the crypt).

- [ ] **Step 2: Load the town from `levels.json`**

In `src/Mortar.Json/Content/ContentDtos.cs`, add to `LevelsDto`:

```csharp
    public TownDto? Town { get; set; }
```

and add after `LevelsDto`:

```csharp
internal sealed class TownDto
{
    public string? Area { get; set; }
}
```

In `src/Mortar.Json/Content/LevelFileLoader.cs`:

1. Right after the `areaIdsByFile` declaration in `Load`, add:

```csharp
        if (dto.Town is not null)
        {
            var town = LoadTown(path, dto.Town, areas, areaIdsByFile, errors);
            if (town is not null) levels.Add(town);
        }
```

2. In the loop, change `LoadArea(path, i, level.Area, …)` to `LoadArea(path, $"levels entry {i}", level.Area, …)`.
3. In `LoadArea`, replace the `int index` parameter with `string label` and change its message to `$"{label} has an empty or invalid area path"`.
4. Add:

```csharp
    private static LevelDefinition? LoadTown(
        string source,
        TownDto town,
        Dictionary<string, CompiledArea> areas,
        Dictionary<string, string> areaIdsByFile,
        List<AuthoringError> errors)
    {
        var id = LoadArea(source, "town", town.Area, areas, areaIdsByFile, errors);
        if (id is null) return null;
        var map = areas[id].Map;
        if (map is null)
        {
            errors.Add(new AuthoringError(source, "the town area must use mode 'fixed'"));
            return null;
        }
        var exits = map.Markers.Count(marker => marker.Kind == MarkerKinds.Exit);
        if (exits != 1)
        {
            errors.Add(new AuthoringError(source, $"the town map must have exactly one exit (X), found {exits}"));
            return null;
        }
        return new LevelDefinition(0, id, Array.Empty<SpawnEntry>(), null);
    }
```

- [ ] **Step 3: Point `levels.json` at the new areas**

Replace `content/levels.json` with:

```json
{
  "town": { "area": "areas/town.json" },
  "levels": [
    {
      "depth": 1,
      "area": "areas/cathedral-1.json",
      "spawns": [
        { "monster": "zombie", "weight": 3 },
        { "monster": "imp-chieftain", "weight": 1 }
      ]
    },
    {
      "depth": 2,
      "area": "areas/cathedral-2.json",
      "spawns": [
        { "monster": "zombie", "weight": 2 },
        { "monster": "skeleton", "weight": 3 },
        { "monster": "imp-chieftain", "weight": 1 }
      ]
    },
    {
      "depth": 3,
      "area": "areas/cathedral-3.json",
      "spawns": [
        { "monster": "skeleton", "weight": 3 },
        { "monster": "carrion-bat", "weight": 2 },
        { "monster": "imp-chieftain", "weight": 1 }
      ]
    },
    {
      "depth": 4,
      "area": "areas/cathedral-4.json",
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

- [ ] **Step 4: Run the loader tests**

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~GameContentLoaderTests"`
Expected: PASS.

- [ ] **Step 5: Regenerate the game snapshot**

The scripted run now starts in town and walks to its exit, so `game-seed-7.txt` must change. Delete `tests/Mortar.Tests/Snapshots/game-seed-7.txt`, then run:

Run: `dotnet test tests/Mortar.Tests --filter "FullyQualifiedName~DeterminismTests"`
Expected: `ScriptedRun_MatchesSnapshot` FAILS with "Snapshot created at …"; the other determinism tests PASS (they still kill monsters and pick up loot).

Review the new file: the first lines are moves in town, then a `LevelChanged` from depth 0 to 1, then fighting on depth 1; the last line is a `hash:`. Run the same command again.
Expected: PASS.

- [ ] **Step 6: Update the docs**

`docs/design.md`:
- §4.1: add a bullet: "`markers` (optional): extra marker characters, each with `char` and `kind` (for example `{ "char": "H", "kind": "healer" }`). They cannot reuse a ground char or a built-in marker char or kind."
- §4.3: after the marker bullet add "Rooms may set `weight` (1–1000, default 1); a heavier room is picked more often among rooms that fit." and change the multi-slot sentence so it says filler and dead-end rooms may also span several slots; doors on a multi-slot room belong to the slot side they sit on.
- §4.4: change the mode bullet to "`mode`: `indoor`, `outdoor`, or `fixed`", and add a paragraph: "A **fixed** area is one hand-drawn map stamped as-is: it has only `id`, `mode`, `tileset`, and `map` (a file with a `grid`, like a room). The map needs exactly one `E`; every marker must be reachable from it; unreachable walkable pockets become `void`. The same map is produced for every seed (tile variants still follow the seed)."
- §5: add before step 1: "Fixed areas skip steps 2–6: the map is stamped, its tiles are resolved, and the result is attempt 0."
- §10: change the success-rate bullet to "sample areas must succeed on at least 80% of seeds; the shipped cathedral areas must succeed on all of seeds 1–500."

`docs/authoring.md`:
- Tileset section: after the `variantWeights` bullet add the `markers` bullet (same wording as design §4.1) and change the reserved-chars bullet to say custom marker chars are also reserved once declared.
- Rooms section: add "`weight` (optional, 1–1000, default 1) makes a room more likely to be picked among rooms that fit." and change the last bullet to "Multi-slot rooms (17×17 and up) can be landmark, filler, or dead-end rooms. Doors on a multi-slot room belong to the slot side they sit on."
- Areas section: change "`mode`: only `indoor` is supported in this version." to "`mode`: `indoor`, or `fixed` (see below). `outdoor` is not supported yet."
- Add a new section `## Fixed areas` before `## Layout files`:

````markdown
## Fixed areas

A fixed area stamps one hand-drawn map, for places that should look the same every visit such as a town.

```json
{ "id": "town", "mode": "fixed", "tileset": "../tilesets/town.json", "map": "../maps/town.json" }
```

- `map` is a file with a `grid` drawn like a room (any size up to 4096×4096, no doors). Other room fields are ignored.
- The grid needs exactly one `E`. Every marker must be reachable from it; walkable cells that are not become `void`.
- Graph, rooms, size, and attempt settings are not allowed in a fixed area.
````

`docs/content.md`, `levels.json` section: append "An optional `town` entry (`{ "area": "…" }`) adds a safe town at depth 0: its area must use mode `fixed` with exactly one exit (`X`), it has no spawns or boss, and new games start there. The shipped content has a town and four cathedral levels (`content/areas/`)."

`README.md`:
- Status: change "the town and more classes come next" to "a hand-drawn town above four generated cathedral levels starts each game; more classes come next".
- Layout table: change the `content/` row to "Game data: rules, classes, monsters, levels, and the town and cathedral areas."

- [ ] **Step 7: Run the full suite and a release build**

Run: `dotnet test`
Expected: all tests pass.

Run: `dotnet build -c Release`
Expected: build succeeded, 0 warnings, 0 errors.

- [ ] **Step 8: Commit**

```bash
git add content/levels.json src/Mortar.Json/Content/ContentDtos.cs src/Mortar.Json/Content/LevelFileLoader.cs tests/Mortar.Tests/Support/ContentFixture.cs tests/Mortar.Tests/Json/GameContentLoaderTests.cs tests/Mortar.Tests/Snapshots/game-seed-7.txt docs/design.md docs/authoring.md docs/content.md README.md
git commit -m "wire the town and cathedral into the game"
```

---

## Self-Review

Spec coverage, checked section by section against `docs/superpowers/specs/2026-10-14-cathedral-and-town-design.md`:

| Spec item | Task |
|---|---|
| Room weights, uniform pick when all weights are 1 | 1 |
| Multi-slot filler and dead-end rooms, per-opening door matching | 2 |
| Custom tileset markers (`smith`, `healer`, `sage`) | 3 |
| `fixed` area mode, map checks, reachability, voided pockets | 4, 5 |
| Cathedral tileset, 22 rooms, four depth areas with 8n+1 sizes | 6 |
| 500-seed success on every cathedral area, snapshots | 6 |
| Town tileset, area, and map; town snapshot; CLI generate and preview | 7 |
| Town is depth 0, safe, new games start there; Stairs and saves | 8 |
| `levels.json` town entry and loader checks; regenerated game snapshot; docs | 9 |

Placeholder scan: none. Every content file appears in full in Tasks 6 and 7, and each was validated before this plan was written (the cathedral areas generated 2000 times with no failures, and the town map has no unreachable walkable cells).

Type consistency: `Room` weight (Task 1) is used by `RoomPicker` (Tasks 1–2); `Tileset.TryGetMarkerKind` (Task 3) is used by `FixedMapCompiler` (Task 4); `CompiledArea.Map` (Task 4) is used by `FixedAreaLoader` (Task 5) and `LevelFileLoader.LoadTown` (Task 9); `ContentSet.ShallowestLevel` (Task 8) is used by `Stairs`, `World.Create`, `LevelRestorer`, and the loader tests (Task 9).
