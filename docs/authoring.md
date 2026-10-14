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
- Each `char` must be a single character and unique. `D`, `E`, `X`, `B`, `W`, `S`, and `C` are reserved for doors and markers, and so is each custom marker char once declared.
- `variantWeights` maps a tile type (see below) to relative weights for its art variants. Tile types without weights always use variant `0`.
- `markers` (optional): extra marker characters, each with `char` and `kind` (for example `{ "char": "H", "kind": "healer" }`). They cannot reuse a ground char or a built-in marker char or kind.

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
- Standard tags: `entrance`, `exit`, `boss`, `waypoint` (the room must contain the matching marker), `filler` and `corridor` (fill the path between landmarks; at least one single-slot room must carry one of them), `dead_end` (preferred for side-branch tips). Any other tag can be used by a custom landmark kind; its marker is placed on the room's center cell, so that cell must be walkable.
- `weight` (optional, 1–1000, default 1) makes a room more likely to be picked among rooms that fit.
- Multi-slot rooms (17×17 and up) can be landmark, filler, or dead-end rooms. Doors on a multi-slot room belong to the slot side they sit on.

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

- `mode`: `indoor`, or `fixed` (see below). `outdoor` is not supported yet.
- `tileset` and `rooms` are paths relative to the area file. A `rooms` entry can be a file or a folder; a folder loads every file whose extension is `.json` in any letter case, in ordinal file-name order.
- `roomTags` (optional) keeps only rooms that carry at least one of these tags.
- `targetSize`: the level is built on a grid of `(width - 1) / (slotSize - 1)` slots per side. After generation, the walkable area must measure within `tolerance` percent of `width` and `height`, or the attempt is retried. Width and height are at most 4096.
- `maxSideBranches`: up to this many optional dead-end rooms are added per attempt (at most 1000).
- `maxAttempts`: retry limit (default 20, at most 1000).
- `nodes`: landmarks. `kind` defaults to the node `id`. `region` is the home rectangle in fractions of the area; the landmark's room is placed on a slot inside it. Exactly one node must be an `entrance`.
- `edges`: `path` edges are required connections; every node must reach the entrance through them. `branch` edges are each included with a 50% chance and dropped if no route exists.

## Fixed areas

A fixed area stamps one hand-drawn map, for places that should look the same every visit such as a town.

```json
{ "id": "town", "mode": "fixed", "tileset": "../tilesets/town.json", "map": "../maps/town.json" }
```

- `map` is a file with a `grid` drawn like a room (any size up to 4096×4096, no doors). Other room fields are ignored.
- The grid needs exactly one `E`. Every marker must be reachable from it; walkable cells that are not become `void`.
- Graph, rooms, size, and attempt settings are not allowed in a fixed area.

## Layout files

`mortar generate` writes compact JSON with these properties, in order. Readers reject files whose width or height exceeds 4096, whose arrays or markers fall outside the grid, or whose fingerprint does not match the contents.

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
