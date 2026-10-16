# Art pipeline

Mortar draws everything as coloured monospace glyphs. The scene is rendered normally from
painted tiles and baked 3D actors, then `AsciiFilter` on the game camera turns every 6×10 px
cell into one glyph. The interface is drawn on top in DejaVu Sans Mono and is never filtered.

## The filter

Each frame the filter:

1. averages every cell into a cell texture, multiplied by the light (full in town; in dungeons a
   radius of about 6 tiles around the player fades to black);
2. renders a mask with a hidden camera: grass and water (stroke sprites) and loot (glow sprites)
   are marked, anything drawn in front clears the mark;
3. blurs bright and glowing cells for bloom;
4. draws one glyph per cell in the cell's colour on near-black: blank below the dark threshold,
   a rippling dash on stroke cells, `- / | \` across strong brightness edges, otherwise a ramp
   glyph by brightness.

F9 toggles the filter to show the raw scene; it is a debug key. Tuning values (thresholds,
light radius, stroke speed, bloom) live in `AsciiSettings`. If a shader or the atlas fails to
load, the filter logs once and shows the raw scene.

## Glyph atlas and icons

From `tools/art`:

    .venv/Scripts/python glyphs.py atlas    # 16-slot atlas into Runtime/Resources/Mortar
    .venv/Scripts/python glyphs.py icons    # Assets/Art/icons -> Assets/Art/glyph-icons

The atlas ramp is chosen by measured ink coverage. Glyph icons use the same slot rules as the
filter (`mortar_art/glyphs.py` and `GlyphRules.cs` must match), 6×4 characters per inventory
cell the icon covers, and a contact sheet goes to `tools/art/out/sheets/glyph-icons.png`. The
interface prefers a glyph icon and falls back to the painted one; ground items always use the
painted icon so the filter draws them.

## Blender

Install the pinned LTS:

    winget install --id BlenderFoundation.Blender.LTS.4.5 --exact

`bake.py` looks for `C:\Program Files\Blender Foundation\Blender 4.5\blender.exe`. Set `BLENDER`
to a `blender.exe` to use another install.

## Baking

From `tools/art`:

    .venv/Scripts/python bake.py --sheet                  # bake actors without sheets, write a review sheet
    .venv/Scripts/python bake.py --only zombie --force    # re-bake one actor
    .venv/Scripts/python bake.py --list-clips quaternius/Zombie.glb

Each actor renders five animations (idle, walk, attack, hit, death) in eight directions, or idle
facing south for town NPCs. Frames are 104×104 (rendered at 208×208 and halved), 64 pixels per
tile, with the foot pivot at (52, 24) from the bottom-left; death is baked in place. Sheets and
their JSON sidecars go to `unity/Mortar.Sample/Assets/Art/actors/<actor>/` and import as
compressed (DXT5) readable textures. Frames and Blender logs go to `tools/art/out/bake/<actor>/`.
Then run `Mortar/Rebuild Sprite Catalog` in Unity.

## Adding an actor

1. Copy the CC0 model into `tools/art/models/<pack>/` with a `LICENSE.txt` naming the source.
2. Run `bake.py --rehash` to record its sha256 in `actors.json`.
3. Run `bake.py --list-clips <file>` and add an entry to `actors.json`: `model`, `clips` (our
   name to the model's clip name), `attackHitSeconds`, and optional `hide`, `scale`, `facing`
   (degrees) and `tint`.
4. Bake with `--only <id> --sheet`, check the review sheet, and rebuild the catalog.

The bake checks each file against its recorded hash, so a changed model fails loudly.

## Licences

Every model under `tools/art/models` is CC0 (public domain dedication); each pack folder's
`LICENSE.txt` names its source: KayKit Adventurers and KayKit Skeletons by Kay Lousberg, and
models by Quaternius. DejaVu Sans Mono ships with its own licence in
`unity/com.backwoods.mortar/Runtime/Resources/Mortar/DejaVuSansMono-LICENSE.txt`.
