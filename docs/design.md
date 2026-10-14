# Mortar — Design

Mortar is a free, open-source (MIT) library that generates replayable, seed-based levels for action RPGs from hand-made pieces. Designers draw tiles and rooms as text, describe each area's overall structure, and Mortar turns a seed into a finished layout. The same seed always produces the same level, on every platform.

Mortar targets Unity, which runs the generator directly. The generator itself has no engine dependencies, so it also runs from the command line and in tests.

## 1. Goals and non-goals

**Goals**

- Indoor levels built by stitching hand-made rooms along a landmark graph.
- Outdoor levels built by tracing rivers, cliffs, roads, and forest edges between landmarks, then filling the space between.
- Deterministic output: identical layouts for the same inputs and seed on Windows, Linux, and macOS, in .NET and in Unity.
- Every successful layout is guaranteed walkable from entrance to every landmark and marker.
- Plain-text authoring that is easy to review in git.
- A thin Unity integration.

**Non-goals (v1)**

- Visual room editors inside Unity (text authoring only).
- Spawning monsters, loot, or gameplay objects. Mortar reports marker positions; games decide what spawns.
- Multi-floor or vertical levels.
- Engines other than Unity.

## 2. Repository layout

| Path | Purpose |
| --- | --- |
| `src/Mortar.Core` | The generator. C#, .NET Standard 2.1, no engine or file-format dependencies. |
| `src/Mortar.Json` | Reads tileset, room, and area files; writes layout files. |
| `src/Mortar.Cli` | Command-line tool: `generate`, `preview`, `stats`. |
| `tests/` | Unit, snapshot, and success-rate tests. |
| `samples/` | A small sample tileset, one indoor area, and one outdoor area. |
| `unity/` | Unity package (installed by Git URL through the Package Manager). |
| `docs/` | Design and authoring guides. |

## 3. Determinism

- Mortar uses its own seeded pseudo-random number generator (PCG32). It never uses `System.Random`, whose output is not guaranteed stable across .NET versions or platforms.
- All generation uses integer or fixed-order arithmetic for decisions. Floating point is allowed only where results are rounded to cells in a deterministic order.
- Collections that drive choices are iterated in a defined order (no hash-set or dictionary enumeration order in decision paths).
- Each layout includes a **fingerprint**: a hash over the area id, seed, successful attempt, dimensions, and every cell's tile type and variant. Servers and clients compare fingerprints to confirm they built the same level.

## 4. Input files

### 4.1 Tileset (`tileset.json`)

Defines **ground types**. Each ground type has:

- `id` (for example `floor`, `wall`, `water`, `cliff_high`, `void`)
- `char`: the character used to draw it in room grids
- `walkable`: whether units can stand on it
- `blocksSight`: whether it blocks projectiles and line of sight (walls, tall trees)
- `markers` (optional): extra marker characters, each with `char` and `kind` (for example `{ "char": "H", "kind": "healer" }`). They cannot reuse a ground char or a built-in marker char or kind.

It may also list **variant weights** per tile type, so engines can favor some art variants over others.

### 4.2 Tile types

Designers never place tile types directly. After generation, Mortar derives each cell's **tile type** from the ground types at its four corners (for example, water on the north corners and floor on the south corners produces a north-facing shoreline). Engines map tile types to meshes or prefabs and pick among variants using the seed. This removes hand-placed edge and corner pieces and lets repeated tiles vary.

### 4.3 Rooms (`rooms/*.json`)

A room is a small hand-drawn grid with tags:

```json
{
  "id": "crypt_hall_a",
  "tags": ["filler"],
  "grid": [
    "##D##",
    "#...#",
    "D.S.D",
    "#...#",
    "##D##"
  ]
}
```

- Ground characters come from the tileset (`#` wall, `.` floor in the samples).
- `D` is a possible door. It becomes floor if the generator connects something to it, and wall otherwise. Doors must lie on the room's outer edge.
- Marker characters sit on floor cells: `E` entrance, `X` exit, `B` boss, `W` waypoint, `S` monster spawn, `C` chest.
- Rooms may set `weight` (1–1000, default 1); a heavier room is picked more often among rooms that fit.
- Standard tags: `entrance`, `exit`, `boss`, `waypoint`, `filler`, `corridor`, `dead_end`. Areas may define custom tags.
- Indoor room width and height must each be `(slotSize - 1) × n + 1` for a whole number `n` (with `slotSize` 9: 9, 17, 25…), so neighboring rooms share exactly one cell.
- Rooms 17×17 and up span several slots. Landmark, filler, and dead-end rooms may all span several slots; doors on a multi-slot room belong to the slot side they sit on.

### 4.4 Areas (`areas/*.json`)

An area fixes the overall shape that stays recognizable between visits:

- `mode`: `indoor`, `outdoor`, or `fixed`
- `tileset`: tileset file
- `targetSize`: width and height in cells, with a tolerance (for example 60×60 ± 15%)
- `slotSize` (indoor): room slot size in cells (for example 9, giving an 8-cell stride with overlap)
- `rooms`: which room files or tags the area may use
- `maxSideBranches` (indoor): limit on optional branches
- `maxAttempts`: retry limit, default 20
- `graph`:
  - `nodes`: landmarks (`entrance`, `exit`, `boss`, `waypoint`, or custom), each with a **home region** expressed as a rectangle in fractions of the area (for example entrance at x 0.0–0.2, y 0.3–0.7)
  - `edges`: links between nodes. Indoor edges are `path` (required) or `branch` (optional). Outdoor edges have a feature type (`river`, `cliff`, `road`, `forest_edge`) with a width and the ground types for each side.

A **fixed** area is one hand-drawn map stamped as-is: it has only `id`, `mode`, `tileset`, and `map` (a file with a `grid`, like a room). The map needs exactly one `E`; every marker must be reachable from it; unreachable walkable pockets become `void`. The same map is produced for every seed (tile variants still follow the seed).

## 5. Generation pipeline

Fixed areas skip steps 2–6: the map is stamped, its tiles are resolved, and the result is attempt 0.

1. **Seed.** Create the generator from the area id and seed.
2. **Place landmarks.** Each node is placed at a random position within its home region.
3. **Build the level.**
   - **Indoor:**
     1. Map landmarks onto a coarse grid of slots.
     2. Pathfind along each `path` edge across the slot grid and record which sides of each slot need an opening.
     3. Place landmark rooms (by tag) on landmark slots.
     4. Fill every other used slot with a room whose doors cover the required openings. Unused doors stay wall.
     5. Add optional side branches with `dead_end` or `filler` rooms, up to `maxSideBranches`.
     6. Place rooms so neighbors overlap by one cell. Merge overlapping cells: wall + wall = wall; connected door + anything = floor; unconnected door = wall; floor + floor = floor.
   - **Outdoor:**
     1. For each edge, pathfind across a seeded, randomly weighted grid to get a winding route.
     2. Smooth the route into a curve and draw it into cells with the edge's feature (water band with shore, high/low ground step, floor strip).
     3. Stamp landmark rooms at landmark positions.
     4. Fill remaining walkable space with base ground plus `filler` rooms where they fit; everything outside the boundary becomes `void`.
4. **Resolve tiles.** Derive each cell's tile type from its corners and pick a variant using the seed and variant weights.
5. **Validate.**
   - A flood fill from the entrance must reach the exit, the boss, the waypoint, and every marker.
   - Walkable pockets the flood fill cannot reach are converted to `void`, so nothing can drop where the player cannot go.
   - The final walkable bounds must fall within `targetSize` and its tolerance.
6. **Retry.** If validation fails, derive a new seed from (original seed, attempt number) and repeat from step 2, up to `maxAttempts`. The output records the successful attempt so the result reproduces exactly.

## 6. Output (`layout.json`)

- `areaId`, `seed`, `attempt`
- `width`, `height`
- `cells`: per cell, ground type, tile type, and variant index
- `walkable`: walkability grid (row-major, 0/1)
- `markers`: type and cell position of every marker and landmark
- `fingerprint`
- `mortarVersion` and `formatVersion`

The layout file is the only thing engines and servers need.

## 7. Errors

- **Authoring errors** fail immediately, with no retries, and name the file and problem. Examples: invalid JSON, unknown ground character, a door not on a room's edge, a room with no doors, a room size that does not fit the slot grid, a tag no room provides, or a tile type with no matching piece.
- **Generation failures** retry automatically. If every attempt fails, Mortar returns a failure result that counts reasons, for example `boss unreachable: 14, too small: 6`.
- The core library returns results rather than throwing for both cases; the CLI prints them and exits non-zero.

## 8. Command-line tool

- `mortar generate --area <file> --seed <n> --out <layout.json>`
- `mortar preview --layout <layout.json> --out <map.png>`: renders ground types and markers as a top-down image
- `mortar stats --area <file> --runs <n>`: success rate, failure reasons, and size range across seeds

## 9. Unity package

- Includes `Mortar.Core` and `Mortar.Json` as DLLs; generation runs in the editor or at runtime.
- **Tile Set Mapping** asset: maps each tile type to one or more prefabs.
- **Mortar Level** component: takes an area file and seed, generates, and builds the level under a parent object. An editor button regenerates with a new seed.
- Markers become tagged empty game objects for games to spawn content on.

## 10. Testing

- **Determinism snapshots:** stored layouts for known seeds; any change in output fails the test.
- **Validation property tests:** across many seeds, every successful layout passes the walkability checks.
- **Authoring error tests:** each invalid-input case produces the expected error.
- **Success-rate tests:** sample areas must succeed on at least 80% of seeds; the shipped cathedral areas must succeed on all of seeds 1–500.
- **CI:** GitHub Actions runs the test suite on Windows, Linux, and macOS and confirms identical snapshots on all three.

## 11. Build order

1. **Core and CLI (first release):** file formats, PCG32, tile type resolution, validation, retries, indoor room stitching, `generate` and `preview`.
2. **Outdoor generation:** feature tracing, filler, and `stats`.
3. **Unity package** with a sample scene.

## 12. Repository conventions

- MIT license; README with quick start; contribution notes.
- Samples use plain placeholder geometry (cubes and planes) so the repo contains no licensed art.
- Commit messages are short, one line, present tense.
