# ASCII look — design

Date: 2026-10-15. Replaces the painted 2D look with an animated ASCII look: the
world is rendered as today, then every frame is converted to coloured glyphs.
Supersedes Tasks 9 and 10 of `2026-10-15-3d-art-pipeline-design.md`, which fold
into phase 3 here.

## Goal

Mortar has one look: coloured monospace glyphs on near-black, with light that
fades into darkness, rippling stroke grass and glowing loot, and a crisp
monospace interface. The simulation, input, picking and views do not change;
only what reaches the screen does.

## Scope

In: a GPU glyph filter on the game camera; a stroke mode for grass and water; a
player light radius in dungeons; bloom; the 3D actor bake moved to half
resolution with compressed sheets, and the remaining nine actors baked; a
monospace restyle of every interface panel; glyph versions of every item icon;
docs.

Out: a player-facing setting for the filter or cell size; extra light sources
beyond the player (the hook exists, nothing uses it); a terminal or text-mode
build; glyph-authored tiles or actors; new gameplay.

## Decisions

| Question | Choice |
| --- | --- |
| Where glyphs come from | Filter the rendered scene (3D-baked actors and painted tiles stay as the source) |
| Colour | Each glyph takes its cell's scene colour, slightly desaturated, on near-black; bright things glow |
| Cell size | About 6×10 px at 1080p, fixed |
| Interface | Restyled as crisp monospace panels drawn on top of the filter, never filtered |
| Item icons | Each painted icon converted once into a coloured glyph picture |
| Renderer | GPU filter on the camera, plus stroke-mode cells for grass and water |

## Glyph renderer

`AsciiFilter` is a component on the existing camera (`CameraFollow`); the
project uses Unity's built-in render pipeline, so it runs in `OnRenderImage`.

1. The camera renders the world at screen resolution, as today.
2. Downsample: a shader pass averages each 6×10 px cell into one pixel of a cell
   texture (about 320×108 at 1080p). The cell count follows the screen size;
   the cell size is a constant.
3. Glyph pass: one full-screen shader finds each pixel's cell, reads that
   cell's colour and its neighbours', and picks a glyph:
   - brightness glyph: brightness indexes a ramp of about 12 glyphs from blank
     and `.` to `@`;
   - edge glyph: where brightness changes strongly across neighbours, one of
     `- / | \` by edge direction;
   - stroke glyph: cells marked in the stroke mask draw a dash whose angle comes
     from a scrolling noise field, so grass and water ripple.
   The glyph is tinted with the cell colour, slightly desaturated, on
   near-black (0.04).
4. Bloom: bright cells are blurred and added back, so fire, spells and loot
   glow.

The brightness-to-glyph lookup, cell-grid maths, light falloff and edge
threshold are C# functions that build the shader's inputs (a lookup texture and
material values), so they can be unit-tested.

Glyph atlas: a monospace font texture generated once by `tools/art` from an
open-licence font (DejaVu Sans Mono by default) at 6×10 per glyph, committed
with the font's licence. The ramp is ordered by measured ink coverage.

Stroke mask: ground tiles whose type is grass or water are tagged. A second,
cheap camera pass renders tagged tiles into a one-channel mask texture the
glyph pass reads; all other cells use brightness and edge glyphs.

F9 toggles the filter off to show the raw scene, for art tuning. It is a debug
key, not a player option.

Click picking keeps working in world space on the original sprites; the filter
does not affect it.

## World source

Actors: the 3D bake continues and becomes the filter's source. Because detail
below one cell is lost, the bake moves to half resolution: 64 px per tile,
frames 104×104 with the foot pivot at (52, 24) from the bottom-left. Sheets
import compressed (DXT5) instead of uncompressed. Death stays baked in place,
8 directions and tick-synced playback stay as built. The plan first checks that
outline picking (tight sprite meshes) works on compressed readable sheets; if
it does not, picking uses a coarse per-frame alpha mask instead.

Tiles: the painted tiles stay as the ground's colour source. Grass, water and
similar floor types carry the stroke tag; walls and floors use brightness and
edge glyphs.

Light: before downsampling, the scene is multiplied by a light factor. In
dungeons a soft radius of about 6 tiles around the player fades to black;
cells below a brightness threshold draw nothing, so the edge of the light
dissolves glyph by glyph. Town is fully lit. The light factor accepts a list of
extra light points; only the player's light is built now.

Feedback: the existing hit flash and corpse tint carry through as colour.
Gold, loot and other highlights render brighter than the scene so bloom picks
them up.

The still-sprite fallback (mirrored still with a facing arrow) stays as a
safety net for missing art and is not polished further.

## Interface

- One monospace font, the same as the atlas, for all interface text, on
  near-black panels.
- Panels have thin light-grey box borders with the title set into the top edge
  (`┤ Inventory ├`). Borders are UI Toolkit styles, not box characters, so
  existing layouts keep working.
- HUD, belt, inventory and equipment, tooltips, message log, hover text, pause
  and death menus move to one shared style sheet. Behaviour does not change.
- HP and mana become text bars, e.g. `HP ██████████░░░░ 96/129`, red and blue
  fills, numbers always shown.
- Damage numbers and popups use the monospace font in their existing colours,
  still floating up, without outline effects.
- The interface is never filtered. Item rarity colours stay. Names and numbers
  are always plain text, never glyph art.

## Item icons

A new `tools/art` step converts each painted icon into a glyph picture whose
character grid matches its on-screen size: about 6×4 characters per inventory
cell (a 1×3 sword gets about 6×12). Each character uses the same atlas,
brightness ramp and edge rules as the world filter, keeps the icon's colour,
and sits on a transparent background. The result is a small PNG saved next to
its source icon; the sprite catalog prefers the glyph version when it exists,
so inventory code does not change. Ground items are drawn in the world and go
through the filter.

## Errors and fallbacks

- If the glyph shader or atlas fails to load, or the GPU does not support it,
  the filter disables itself, logs once and the raw scene is shown.
- A missing glyph icon falls back to the painted icon.
- A missing baked actor falls back to the still sprite.
- Performance budget: the whole filter (downsample, glyphs, bloom) at most
  about 1 ms GPU at 1080p, measured during the play check.

## Testing

- pytest: atlas generator (glyph count, cell size, ramp ordered by ink
  coverage); icon converter (grid size per inventory cell, colours kept,
  transparent background, deterministic output); bake constants at half
  resolution.
- Unity EditMode: brightness-to-glyph lookup, cell-grid maths, light falloff,
  edge threshold, catalog preferring glyph icons.
- Unity PlayMode: known scenes through the real filter with pixel read-back —
  black gives blank cells; a bright square gives glyph pixels in its colour; an
  edge gives a stroke glyph; a stroke-tagged tile appears in the mask; F9
  restores the raw image; a failed shader load shows the raw scene. The
  existing art preview capture writes filtered town and dungeon screenshots.
- Manual: the user plays before each visible phase is committed.

## Order of work

The game stays playable after every phase.

1. Filter core: atlas generator and committed atlas, downsample, brightness and
   edge glyphs, F9 toggle.
2. World: light radius, stroke mask for grass and water, bloom, loot glow; user
   review.
3. Actors: half-resolution bake with compressed sheets and the picking check;
   re-bake the warrior; bake the other nine actors; user review.
4. Interface: shared monospace style, panels, text bars, popups.
5. Icons: converter, generated glyph icons, catalog preference; user review.
6. Docs: `docs/art.md`, `docs/unity.md`, README; amend the 3D pipeline spec
   (half resolution, 104×104 frames, death baked in place, compressed sheets,
   superseded by this design).
