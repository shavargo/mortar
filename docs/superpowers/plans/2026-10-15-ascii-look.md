# ASCII Look Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give Mortar one look: every camera frame converted to coloured monospace glyphs on near-black, with a player light in dungeons, rippling stroke grass and water, glowing loot, half-resolution baked actors, a monospace interface and glyph item icons.

**Architecture:** `AsciiFilter` sits on the game camera (built-in render pipeline, `OnRenderImage`). It averages each 6×10 px cell into a cell texture (lit by the player light), blurs bright cells for bloom, and draws one glyph per cell from a committed glyph atlas, choosing ramp, edge or stroke glyphs with lookup textures built from tested C# rules. A hidden mask camera renders the scene with a replacement shader so grass, water and loot can be told apart. `tools/art/glyphs.py` builds the atlas and the glyph icons from DejaVu Sans Mono with the same rules.

**Tech Stack:** Unity 6000.3 (C# 9, built-in render pipeline, CG/HLSL shaders, UI Toolkit, Input System), Unity Test Framework; Python 3.14 with Pillow and pytest in `tools/art/.venv`; Blender 4.5 LTS for the actor bake.

**Spec:** `docs/superpowers/specs/2026-10-15-ascii-look-design.md` (approved, commit 1d0b9e3).

## Global Constraints

- No Blizzard or Diablo names anywhere. Models stay CC0; the only non-CC0 asset is DejaVu Sans Mono, committed with its licence file.
- Unity scripts compile as C# 9: block-scoped namespaces, explicit `using` directives, no records, no `init`, no file-scoped namespaces, no collection expressions. Every assembly folder keeps its `csc.rsp` with `-nullable:enable -warnaserror+`; serialized fields need initializers (CS0649 is an error).
- Never use `?.` or `??` on `UnityEngine.Object` values; compare with `== null`.
- Built-in render pipeline, gamma colour space. Shaders live in `unity/com.mortar.unity/Runtime/Resources/Mortar/` and load with `Resources.Load`, so builds include them. Full-screen passes sample with `tex2Dlod` (no gradients inside branches or loops).
- Cell size is fixed at 6×10 screen pixels; the glyph atlas has 16 slots: 0 is blank, 1–11 the brightness ramp by ink coverage, 12–15 are `- / | \`.
- The glyph slot rules in `mortar_art/glyphs.py` and `GlyphRules.cs` must match; a pytest test checks the shared constants.
- The interface is never filtered. Names and numbers stay plain text.
- `src/` (engine-free simulation) is not modified by this plan.
- No file over 300 lines; no function over 50 lines (C#, HLSL and Python).
- Comments only state constraints the code cannot show.
- Python: `from __future__ import annotations`, double quotes, type hints, module docstring, like the existing `tools/art` modules. Pillow 12 APIs (`get_flattened_data`, not `getdata`).
- pytest runs from `tools/art` as `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider` and must stay warning-free.
- Unity tests: `pwsh -File tools/unity/test.ps1 -Platform EditMode|PlayMode [-Filter X]`. Only one Unity process may open `unity/Mortar.Sample` at a time; the user's editor must be closed for batch runs.
- Unity writes `.meta` files for new package and asset files on its next run (any test run). Commit each `.meta` with its file.
- Commit messages: short, one line, present tense, no prefixes. Stage by explicit path. Never stage `.env`, `.superpowers/`, `bin/`, `obj/`, `out/`, `tools/art/out/`, `tools/art/.venv/`, Unity `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `unity/com.mortar.unity/Plugins/`, or `Assets/StreamingAssets/`.
- Baked art and glyph icons are committed only after the user approves them.

## Design Interpretations

The spec leaves these open; this plan fixes them. Task 13 records them in the docs.

1. **The shared style sheet is the `Ui` class.** The interface is built in code, so the shared style is `Ui`'s palette and helpers plus one root font set by `UiRoot`. `MortarTheme.tss` is unchanged.
2. **The camera background becomes black.** The old 0.07 grey sits above the dark threshold and would draw dots across the whole void.
3. **Edges come from cell brightness.** Central differences of cell luminance give a gradient; above a threshold the cell draws the edge glyph running across the gradient. Cells below the dark threshold are always blank, so edges never outline darkness.
4. **The light is an isometric ellipse.** A radius of 6 grid tiles is an ellipse 6/√2 world units wide and half as tall on screen; the shader doubles the vertical offset.
5. **The mask camera renders straight into a cell-sized texture**, one sample per cell, with a replacement shader keyed on `RenderType`: `MortarStroke` writes red, `MortarGlow` writes green, every other sprite (`Transparent`) writes black so whatever stands in front covers the mask.
6. **Ground items keep the painted icon** (`SpriteCatalog.WorldIcon`) so the filter never glyphs a glyph picture; the interface uses `SpriteCatalog.Icon`, which prefers the glyph version.
7. **Glyph icons cover the item's smaller side.** The inventory draws an icon `min(width, height)` cells wide, so the converter uses 6×4 characters per covered cell, rendered at 2× (12×20 px per character) to stay crisp after panel scaling.
8. **HP and MP bars are 14 cells.** Filled blocks round up, so any life shows one block. The experience bar stays a thin track in the new palette.
9. **F9 is an `InputAction` owned by the filter**, like the other bindings in `PlayerControls`.

## Review Focus

1. **Filter pixels (Task 3):** black stays background, a bright area gives glyphs in its colour, a vertical boundary gives `|`, F9 and a missing shader show the raw scene.
2. **Mask occlusion (Task 5):** a plain sprite drawn over a stroke sprite clears the mask there.
3. **Compressed picking (Task 7):** outline picking still works on DXT5 sheets; the fallback is only used if it does not.
4. **Rule parity (Tasks 1, 2, 11):** Python and C# give the same slots for the same inputs.

## File Structure

```
tools/art/
  glyphs.py                         new: CLI (atlas, icons)
  mortar_art/glyphs.py              new: font rendering, ramp choice, slot rules, atlas
  mortar_art/glyph_icons.py         new: icon to glyph picture (Task 11)
  mortar_art/bake_geometry.py       modified: half resolution (Task 7)
  tests/test_glyphs.py, test_glyphs_cli.py, test_glyph_icons.py      new
  tests/test_bake_geometry.py, test_sheets.py                         modified (Task 7)
  tests/test_models.py              new (Task 8)
unity/com.mortar.unity/
  Runtime/Resources/Mortar/DejaVuSansMono.ttf, DejaVuSansMono-LICENSE.txt, GlyphAtlas.png      new
  Runtime/Resources/Mortar/Ascii.shader, Downsample.cginc, Glyph.cginc, Bloom.cginc            new
  Runtime/Resources/Mortar/AsciiMask.shader, Mask.cginc, StrokeSprite.shader, GlowSprite.shader new
  Runtime/Ascii/AsciiSettings.cs, CellGrid.cs, GlyphRules.cs, LightFalloff.cs, GlyphLookups.cs  new
  Runtime/Ascii/AsciiResources.cs, AsciiMaterial.cs, AsciiFilter.cs, AsciiMask.cs,
                AsciiLight.cs, PlayerLight.cs, MaskMaterials.cs                                new
  Runtime/Views/CameraFollow.cs, LevelView.cs, GroundItemViews.cs                              modified
  Runtime/App/MortarApp.cs                                                                     modified
  Runtime/Art/ActorClip.cs, SpriteKeys.cs, SpriteCatalog.cs                                    modified
  Runtime/UI/Ui.cs, UiRoot.cs, HudView.cs, FloatingTexts.cs, InventoryScreen.cs,
            CharacterSheet.cs                                                                  modified
  Runtime/UI/TextBar.cs                                                                        new
  Runtime/UI/Orb.cs                                                                            deleted
  Editor/ArtImport.cs                                                                          modified
  Tests/Editor/GlyphRulesTests.cs, CellGridTests.cs, GlyphLookupsTests.cs, UiTests.cs,
               TextBarTests.cs                                                                 new
  Tests/Editor/ArtImportTests.cs, SpriteCatalogTests.cs                                        modified
  Tests/Runtime/AsciiScene.cs, AsciiFilterTests.cs                                             new
  Tests/Runtime/ArtPreviewCapture.cs, LevelViewTests.cs, MortarAppTests.cs, HudTests.cs,
                BakedActorTests.cs, TestClips.cs                                               modified
unity/Mortar.Sample/Assets/Art/actors/<actor>/   re-baked half-resolution sheets (Task 8)
unity/Mortar.Sample/Assets/Art/glyph-icons/      generated glyph icons (Task 12)
docs/art.md (new), docs/unity.md, README.md, docs/superpowers/specs/2026-10-15-3d-art-pipeline-design.md (Task 13)
```

---

## Phase 1: Filter core

### Task 1: The font and the glyph atlas

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Resources/Mortar/DejaVuSansMono.ttf`, `DejaVuSansMono-LICENSE.txt`, `GlyphAtlas.png` (with Unity `.meta` files, and `Runtime/Resources.meta`, `Runtime/Resources/Mortar.meta`)
- Create: `tools/art/mortar_art/glyphs.py`, `tools/art/glyphs.py`
- Test: `tools/art/tests/test_glyphs.py`, `tools/art/tests/test_glyphs_cli.py`

**Interfaces:**
- Produces (Python, `mortar_art.glyphs`): `RESOURCES_ROOT`, `FONT_PATH`, `ATLAS_PATH`, `CELL_WIDTH = 6`, `CELL_HEIGHT = 10`, `SLOTS = 16`, `RAMP_SIZE = 12`, `FIRST_EDGE_SLOT = 12`, `EDGE_CHARS`, `LUMA`; `load_font(scale: int = 1, path: Path | None = None) -> ImageFont.FreeTypeFont`; `render_glyph(char: str, font, scale: int = 1) -> Image.Image` (mode `L`, `6*scale × 10*scale`); `coverage(glyph) -> float`; `pick_ramp(coverages: dict[str, float], size: int = RAMP_SIZE) -> list[str]`; `glyph_chars(font) -> list[str]` (16 chars, slot order); `build_atlas(chars: list[str], font, scale: int = 1) -> Image.Image` (RGBA, white, alpha = ink); `ramp_slot(luminance: float, dark_threshold: float) -> int`; `edge_slot(gx: float, gy: float, threshold: float) -> int`.
- Produces (CLI): `glyphs.main(argv) -> int`, `glyphs.write_atlas(path: Path) -> None`.
- Produces (Unity): `Resources/Mortar/GlyphAtlas.png` (96×10) and `Resources/Mortar/DejaVuSansMono.ttf`.

- [ ] **Step 1: Download DejaVu Sans Mono**

From the repo root:

```powershell
New-Item -ItemType Directory -Force tools/art/out/fonts | Out-Null
Invoke-WebRequest https://github.com/dejavu-fonts/dejavu-fonts/releases/download/version_2_37/dejavu-fonts-ttf-2.37.zip -OutFile tools/art/out/fonts/dejavu.zip
Expand-Archive tools/art/out/fonts/dejavu.zip tools/art/out/fonts -Force
New-Item -ItemType Directory -Force unity/com.mortar.unity/Runtime/Resources/Mortar | Out-Null
Copy-Item tools/art/out/fonts/dejavu-fonts-ttf-2.37/ttf/DejaVuSansMono.ttf unity/com.mortar.unity/Runtime/Resources/Mortar/
Copy-Item tools/art/out/fonts/dejavu-fonts-ttf-2.37/LICENSE unity/com.mortar.unity/Runtime/Resources/Mortar/DejaVuSansMono-LICENSE.txt
```

Open the copied licence and confirm it is the DejaVu licence (Bitstream Vera terms plus public-domain changes), which allows bundling. If the download fails or the licence differs, stop and report.

- [ ] **Step 2: Write the failing glyph tests**

`tools/art/tests/test_glyphs.py`:

```python
import re

import pytest

from mortar_art import glyphs
from mortar_art.config import REPO_ROOT

SETTINGS_PATH = REPO_ROOT / "unity" / "com.mortar.unity" / "Runtime" / "Ascii" / "AsciiSettings.cs"


@pytest.fixture(scope="module")
def font():
    return glyphs.load_font()


def test_a_glyph_fills_one_cell(font):
    assert glyphs.render_glyph("@", font).size == (glyphs.CELL_WIDTH, glyphs.CELL_HEIGHT)
    assert glyphs.render_glyph("@", glyphs.load_font(2), 2).size == (2 * glyphs.CELL_WIDTH, 2 * glyphs.CELL_HEIGHT)


def test_space_has_no_ink_and_at_sign_has_plenty(font):
    assert glyphs.coverage(glyphs.render_glyph(" ", font)) == 0.0
    assert glyphs.coverage(glyphs.render_glyph("@", font)) > 0.3


def test_the_ramp_is_ordered_by_ink_coverage(font):
    chars = glyphs.glyph_chars(font)
    ramp = chars[: glyphs.RAMP_SIZE]
    inks = [glyphs.coverage(glyphs.render_glyph(char, font)) for char in ramp]

    assert ramp[0] == " "
    assert len(set(ramp)) == glyphs.RAMP_SIZE
    assert inks == sorted(inks)
    assert inks[-1] > 0.3


def test_the_last_four_slots_are_the_edge_glyphs(font):
    chars = glyphs.glyph_chars(font)

    assert len(chars) == glyphs.SLOTS
    assert chars[glyphs.FIRST_EDGE_SLOT :] == ["-", "/", "|", "\\"]


def test_pick_ramp_spreads_choices_across_coverage():
    coverages = {" ": 0.0, **{chr(97 + index): index / 20 for index in range(1, 21)}}

    ramp = glyphs.pick_ramp(coverages, size=5)

    assert ramp == [" ", "e", "j", "o", "t"]


def test_pick_ramp_needs_a_space_and_enough_candidates():
    with pytest.raises(ValueError, match="space"):
        glyphs.pick_ramp({"a": 0.1, "b": 0.2}, size=2)


def test_the_atlas_is_sixteen_cells_wide_with_a_blank_first_slot(font):
    atlas = glyphs.build_atlas(glyphs.glyph_chars(font), font)
    alpha = atlas.getchannel("A")

    assert atlas.size == (glyphs.CELL_WIDTH * glyphs.SLOTS, glyphs.CELL_HEIGHT)
    assert alpha.crop((0, 0, glyphs.CELL_WIDTH, glyphs.CELL_HEIGHT)).getbbox() is None
    assert atlas.getpixel((glyphs.CELL_WIDTH * 11 + 3, 5))[:3] == (255, 255, 255)


def test_the_bar_glyph_is_a_vertical_line(font):
    atlas = glyphs.build_atlas(glyphs.glyph_chars(font), font)
    left = glyphs.CELL_WIDTH * (glyphs.FIRST_EDGE_SLOT + 2)
    cell = atlas.getchannel("A").crop((left, 0, left + glyphs.CELL_WIDTH, glyphs.CELL_HEIGHT))
    columns = [sum(cell.getpixel((x, y)) for y in range(glyphs.CELL_HEIGHT)) for x in range(glyphs.CELL_WIDTH)]

    assert (columns[2] + columns[3]) > 0.6 * sum(columns)


def test_the_atlas_is_deterministic(font):
    chars = glyphs.glyph_chars(font)

    assert glyphs.build_atlas(chars, font).tobytes() == glyphs.build_atlas(chars, font).tobytes()


def test_an_atlas_needs_sixteen_glyphs(font):
    with pytest.raises(ValueError, match="16 glyphs"):
        glyphs.build_atlas([" ", "@"], font)


def test_a_missing_font_is_a_clear_error(tmp_path):
    with pytest.raises(FileNotFoundError, match="font not found"):
        glyphs.load_font(path=tmp_path / "missing.ttf")


@pytest.mark.parametrize(
    ("luminance", "expected"),
    [(0.0, 0), (0.059, 0), (0.06, 1), (0.5, 6), (0.99, 11), (1.0, 11)],
)
def test_ramp_slots_follow_brightness_above_the_dark_threshold(luminance, expected):
    assert glyphs.ramp_slot(luminance, 0.06) == expected


@pytest.mark.parametrize(
    ("gx", "gy", "expected"),
    [(-0.5, 0.0, 14), (0.5, 0.0, 14), (0.0, -0.5, 12), (-0.4, -0.4, 15), (0.4, -0.4, 13), (0.1, 0.1, 0)],
)
def test_edge_slots_run_across_the_gradient(gx, gy, expected):
    assert glyphs.edge_slot(gx, gy, 0.25) == expected


def test_slot_constants_match_the_unity_settings():
    if not SETTINGS_PATH.is_file():
        pytest.skip("AsciiSettings.cs arrives in Task 2")
    text = SETTINGS_PATH.read_text(encoding="utf-8")

    def constant(name: str) -> int:
        match = re.search(rf"public const int {name} = (\d+);", text)
        assert match, f"{name} not found in AsciiSettings.cs"
        return int(match.group(1))

    assert constant("CellWidth") == glyphs.CELL_WIDTH
    assert constant("CellHeight") == glyphs.CELL_HEIGHT
    assert constant("Slots") == glyphs.SLOTS
    assert constant("RampSize") == glyphs.RAMP_SIZE
    assert constant("FirstEdgeSlot") == glyphs.FIRST_EDGE_SLOT
```

`tools/art/tests/test_glyphs_cli.py`:

```python
from PIL import Image

import glyphs as cli
from mortar_art import glyphs


def test_atlas_writes_the_atlas(tmp_path, monkeypatch, capsys):
    target = tmp_path / "GlyphAtlas.png"
    monkeypatch.setattr(glyphs, "ATLAS_PATH", target)
    monkeypatch.setattr(cli, "REPO_ROOT", tmp_path)

    assert cli.main(["atlas"]) == 0

    with Image.open(target) as atlas:
        assert atlas.size == (glyphs.CELL_WIDTH * glyphs.SLOTS, glyphs.CELL_HEIGHT)
    assert "[GLYPHS] Wrote GlyphAtlas.png" in capsys.readouterr().out


def test_a_missing_font_fails_with_an_error(tmp_path, monkeypatch, capsys):
    monkeypatch.setattr(glyphs, "FONT_PATH", tmp_path / "missing.ttf")
    monkeypatch.setattr(glyphs, "ATLAS_PATH", tmp_path / "GlyphAtlas.png")

    assert cli.main(["atlas"]) == 1
    assert "[ERROR] font not found" in capsys.readouterr().err
```

- [ ] **Step 3: Run the tests to verify they fail**

Run (from `tools/art`): `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider tests/test_glyphs.py tests/test_glyphs_cli.py`
Expected: FAIL with `ImportError` (no `mortar_art.glyphs`, no `glyphs`).

- [ ] **Step 4: Write `mortar_art/glyphs.py`**

```python
"""Monospace glyph rendering shared by the world filter's atlas and the glyph icons.

The slot rules must match `GlyphRules` in the Unity package: Unity draws the world with them and
this module draws the item icons with them.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from .config import REPO_ROOT

RESOURCES_ROOT = REPO_ROOT / "unity" / "com.mortar.unity" / "Runtime" / "Resources" / "Mortar"
FONT_PATH = RESOURCES_ROOT / "DejaVuSansMono.ttf"
ATLAS_PATH = RESOURCES_ROOT / "GlyphAtlas.png"

CELL_WIDTH = 6
CELL_HEIGHT = 10
SLOTS = 16
RAMP_SIZE = 12
FIRST_EDGE_SLOT = 12
EDGE_CHARS = ("-", "/", "|", "\\")
RAMP_CANDIDATES = " .`',:;-~^\"=+<>*!ilcoxe%#&$@MW"
LUMA = (0.299, 0.587, 0.114)
SUPERSAMPLE = 4
FONT_SIZE = 9


def load_font(scale: int = 1, path: Path | None = None) -> ImageFont.FreeTypeFont:
    font_path = path or FONT_PATH
    if not font_path.is_file():
        raise FileNotFoundError(f"font not found: {font_path}")
    return ImageFont.truetype(str(font_path), FONT_SIZE * SUPERSAMPLE * scale)


def render_glyph(char: str, font: ImageFont.FreeTypeFont, scale: int = 1) -> Image.Image:
    width, height = CELL_WIDTH * scale, CELL_HEIGHT * scale
    big = Image.new("L", (width * SUPERSAMPLE, height * SUPERSAMPLE), 0)
    ImageDraw.Draw(big).text((big.width / 2, big.height / 2), char, fill=255, font=font, anchor="mm")
    return big.resize((width, height), Image.Resampling.BOX)


def coverage(glyph: Image.Image) -> float:
    pixels = list(glyph.get_flattened_data())
    return sum(pixels) / (255 * len(pixels))


def pick_ramp(coverages: dict[str, float], size: int = RAMP_SIZE) -> list[str]:
    if " " not in coverages or len(coverages) < size:
        raise ValueError(f"a ramp needs a space and at least {size} candidates")
    remaining = sorted((value, char) for char, value in coverages.items() if char != " ")
    densest = remaining[-1][0]
    ramp = [" "]
    for step in range(1, size):
        target = densest * step / (size - 1)
        best = min(remaining, key=lambda item: (abs(item[0] - target), item[1]))
        remaining.remove(best)
        ramp.append(best[1])
    return sorted(ramp, key=lambda char: (coverages[char], char))


def glyph_chars(font: ImageFont.FreeTypeFont) -> list[str]:
    candidates = dict.fromkeys(RAMP_CANDIDATES)
    coverages = {char: coverage(render_glyph(char, font)) for char in candidates}
    return pick_ramp(coverages) + list(EDGE_CHARS)


def build_atlas(chars: list[str], font: ImageFont.FreeTypeFont, scale: int = 1) -> Image.Image:
    if len(chars) != SLOTS:
        raise ValueError(f"an atlas needs {SLOTS} glyphs, got {len(chars)}")
    width = CELL_WIDTH * scale
    atlas = Image.new("RGBA", (width * SLOTS, CELL_HEIGHT * scale), (255, 255, 255, 0))
    for slot, char in enumerate(chars):
        glyph = render_glyph(char, font, scale)
        white = Image.new("RGBA", glyph.size, (255, 255, 255, 0))
        white.putalpha(glyph)
        atlas.paste(white, (slot * width, 0))
    return atlas


def ramp_slot(luminance: float, dark_threshold: float) -> int:
    if luminance < dark_threshold:
        return 0
    span = (luminance - dark_threshold) / max(1e-6, 1.0 - dark_threshold)
    return min(RAMP_SIZE - 1, 1 + int(span * (RAMP_SIZE - 1)))


def edge_slot(gx: float, gy: float, threshold: float) -> int:
    if math.hypot(gx, gy) < threshold:
        return 0
    angle = (math.degrees(math.atan2(gy, gx)) + 90.0) % 180.0
    return FIRST_EDGE_SLOT + round(angle / 45.0) % 4
```

- [ ] **Step 5: Write the CLI `tools/art/glyphs.py`**

```python
"""Build the glyph atlas for Mortar's ASCII world filter."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

from mortar_art import glyphs
from mortar_art.config import REPO_ROOT


def parse_args(argv: list[str] | None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("atlas", help="render the 16-slot glyph atlas into the Unity package")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        if args.command == "atlas":
            write_atlas(glyphs.ATLAS_PATH)
    except (OSError, ValueError) as error:
        print(f"[ERROR] {error}", file=sys.stderr)
        return 1
    return 0


def write_atlas(path: Path) -> None:
    font = glyphs.load_font()
    chars = glyphs.glyph_chars(font)
    path.parent.mkdir(parents=True, exist_ok=True)
    glyphs.build_atlas(chars, font).save(path)
    print(f"[GLYPHS] Ramp {''.join(chars[: glyphs.RAMP_SIZE])!r}")
    print(f"[GLYPHS] Wrote {path.relative_to(REPO_ROOT).as_posix()}")


if __name__ == "__main__":
    sys.exit(main())
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider tests/test_glyphs.py tests/test_glyphs_cli.py`
Expected: PASS, with `test_slot_constants_match_the_unity_settings` skipped.

- [ ] **Step 7: Generate the atlas and let Unity import it**

Run (from `tools/art`): `.venv\Scripts\python glyphs.py atlas`
Expected: `[GLYPHS] Ramp ' ...'` (12 characters) and `[GLYPHS] Wrote unity/com.mortar.unity/Runtime/Resources/Mortar/GlyphAtlas.png`.

Then run `pwsh -File tools/unity/test.ps1 -Platform EditMode` (editor closed) so Unity imports the new files and writes their `.meta` files.
Expected: all EditMode tests still pass, and `Runtime/Resources.meta`, `Runtime/Resources/Mortar.meta`, and a `.meta` beside each new file exist.

- [ ] **Step 8: Run the full pytest suite and commit**

Run: `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider`
Expected: all pass.

```powershell
git add tools/art/glyphs.py tools/art/mortar_art/glyphs.py tools/art/tests/test_glyphs.py tools/art/tests/test_glyphs_cli.py unity/com.mortar.unity/Runtime/Resources.meta unity/com.mortar.unity/Runtime/Resources/Mortar.meta unity/com.mortar.unity/Runtime/Resources/Mortar/DejaVuSansMono.ttf unity/com.mortar.unity/Runtime/Resources/Mortar/DejaVuSansMono.ttf.meta unity/com.mortar.unity/Runtime/Resources/Mortar/DejaVuSansMono-LICENSE.txt unity/com.mortar.unity/Runtime/Resources/Mortar/DejaVuSansMono-LICENSE.txt.meta unity/com.mortar.unity/Runtime/Resources/Mortar/GlyphAtlas.png unity/com.mortar.unity/Runtime/Resources/Mortar/GlyphAtlas.png.meta
git status --short
git commit -m "add glyph atlas generator and dejavu sans mono"
```

---

### Task 2: Glyph rules, lookup textures and atlas import

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Ascii/AsciiSettings.cs`, `CellGrid.cs`, `GlyphRules.cs`, `GlyphLookups.cs`
- Modify: `unity/com.mortar.unity/Editor/ArtImport.cs`
- Test: `unity/com.mortar.unity/Tests/Editor/GlyphRulesTests.cs`, `CellGridTests.cs`, `GlyphLookupsTests.cs`; modify `Tests/Editor/ArtImportTests.cs`

**Interfaces:**
- Consumes: `GlyphAtlas.png` from Task 1; the Python rules (`ramp_slot`, `edge_slot`) that these must match.
- Produces: `AsciiSettings` constants (`CellWidth`, `CellHeight`, `Slots`, `RampSize`, `FirstEdgeSlot`, `DarkThreshold`, `EdgeThreshold`, `Background`, `Normalize`, `Saturation`); `readonly struct CellGrid(int width, int height)` with `Columns`, `Rows`; `GlyphRules.Blank`, `GlyphRules.RampSlot(float luminance, float darkThreshold) -> int`, `GlyphRules.EdgeSlot(float gx, float gy, float threshold) -> int`; `GlyphLookups.RampWidth`, `EdgeSize`, `SlotStep`, `Ramp(float darkThreshold) -> Texture2D`, `Edges(float threshold) -> Texture2D`, `Gradient(int texel) -> float`, `Create(int width, int height, byte[] values, FilterMode filter, string name) -> Texture2D`; `ArtImport.GlyphAtlasPath`.

- [ ] **Step 1: Write the failing tests**

`Tests/Editor/GlyphRulesTests.cs`:

```csharp
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class GlyphRulesTests
    {
        [TestCase(0f, 0)]
        [TestCase(0.059f, 0)]
        [TestCase(0.06f, 1)]
        [TestCase(0.5f, 6)]
        [TestCase(0.99f, 11)]
        [TestCase(1f, 11)]
        public void RampSlotsFollowBrightnessAboveTheDarkThreshold(float luminance, int expected)
        {
            Assert.AreEqual(expected, GlyphRules.RampSlot(luminance, 0.06f));
        }

        [TestCase(-0.5f, 0f, 14)]
        [TestCase(0.5f, 0f, 14)]
        [TestCase(0f, -0.5f, 12)]
        [TestCase(-0.4f, -0.4f, 15)]
        [TestCase(0.4f, -0.4f, 13)]
        [TestCase(0.1f, 0.1f, 0)]
        public void EdgeSlotsRunAcrossTheGradient(float gx, float gy, int expected)
        {
            Assert.AreEqual(expected, GlyphRules.EdgeSlot(gx, gy, 0.25f));
        }
    }
}
```

`Tests/Editor/CellGridTests.cs`:

```csharp
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class CellGridTests
    {
        [TestCase(1920, 1080, 320, 108)]
        [TestCase(1280, 720, 214, 72)]
        [TestCase(1200, 600, 200, 60)]
        [TestCase(0, 0, 1, 1)]
        public void CellsCoverTheWholeImage(int width, int height, int columns, int rows)
        {
            var grid = new CellGrid(width, height);

            Assert.AreEqual(columns, grid.Columns);
            Assert.AreEqual(rows, grid.Rows);
        }
    }
}
```

`Tests/Editor/GlyphLookupsTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class GlyphLookupsTests
    {
        [Test]
        public void TheRampLookupIsBlankInTheDarkAndDensestAtFullBrightness()
        {
            var ramp = GlyphLookups.Ramp(0.06f);
            var values = ramp.GetRawTextureData();

            Assert.AreEqual(GlyphLookups.RampWidth, ramp.width);
            Assert.AreEqual(FilterMode.Point, ramp.filterMode);
            Assert.AreEqual(GlyphRules.Blank, values[0] / GlyphLookups.SlotStep);
            Assert.AreEqual(AsciiSettings.RampSize - 1, values[GlyphLookups.RampWidth - 1] / GlyphLookups.SlotStep);
            Object.DestroyImmediate(ramp);
        }

        [Test]
        public void TheEdgeLookupMatchesTheRuleAtEachTexel()
        {
            var edges = GlyphLookups.Edges(0.25f);
            var values = edges.GetRawTextureData();

            for (var y = 0; y < GlyphLookups.EdgeSize; y += 7)
            {
                for (var x = 0; x < GlyphLookups.EdgeSize; x += 7)
                {
                    var expected = GlyphRules.EdgeSlot(GlyphLookups.Gradient(x), GlyphLookups.Gradient(y), 0.25f);
                    Assert.AreEqual(expected, values[y * GlyphLookups.EdgeSize + x] / GlyphLookups.SlotStep, $"texel {x},{y}");
                }
            }
            Object.DestroyImmediate(edges);
        }

        [Test]
        public void AVerticalBoundaryLooksUpTheBarGlyph()
        {
            var edges = GlyphLookups.Edges(0.25f);

            Assert.AreEqual(AsciiSettings.FirstEdgeSlot + 2, edges.GetRawTextureData()[31 * GlyphLookups.EdgeSize + 16] / GlyphLookups.SlotStep);
            Object.DestroyImmediate(edges);
        }
    }
}
```

Add to `Tests/Editor/ArtImportTests.cs` (inside the class; add `using UnityEditor;` at the top):

```csharp
        [Test]
        public void TheGlyphAtlasImportsUncompressedWithPointFiltering()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtImport.GlyphAtlasPath);

            Assert.IsNotNull(atlas, "run glyphs.py atlas first");
            Assert.AreEqual(AsciiSettings.CellWidth * AsciiSettings.Slots, atlas.width);
            Assert.AreEqual(AsciiSettings.CellHeight, atlas.height);
            Assert.AreEqual(FilterMode.Point, atlas.filterMode);
            Assert.AreEqual(TextureFormat.RGBA32, atlas.format);
            Assert.AreEqual(1, atlas.mipmapCount);
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: FAIL to compile (`GlyphRules`, `CellGrid`, `GlyphLookups`, `AsciiSettings`, `ArtImport.GlyphAtlasPath` missing).

- [ ] **Step 3: Write the settings, grid and rules**

`Runtime/Ascii/AsciiSettings.cs`:

```csharp
namespace Mortar.Unity
{
    public static class AsciiSettings
    {
        public const int CellWidth = 6;
        public const int CellHeight = 10;
        public const int Slots = 16;
        public const int RampSize = 12;
        public const int FirstEdgeSlot = 12;
        public const float DarkThreshold = 0.06f;
        public const float EdgeThreshold = 0.25f;
        public const float Background = 0.04f;
        public const float Normalize = 0.5f;
        public const float Saturation = 0.85f;
    }
}
```

`Runtime/Ascii/CellGrid.cs`:

```csharp
namespace Mortar.Unity
{
    public readonly struct CellGrid
    {
        public CellGrid(int width, int height)
        {
            Columns = Count(width, AsciiSettings.CellWidth);
            Rows = Count(height, AsciiSettings.CellHeight);
        }

        public int Columns { get; }

        public int Rows { get; }

        private static int Count(int pixels, int cell) => pixels <= 0 ? 1 : (pixels + cell - 1) / cell;
    }
}
```

`Runtime/Ascii/GlyphRules.cs`:

```csharp
using UnityEngine;

namespace Mortar.Unity
{
    public static class GlyphRules
    {
        public const int Blank = 0;

        public static int RampSlot(float luminance, float darkThreshold)
        {
            if (luminance < darkThreshold)
            {
                return Blank;
            }
            var span = (luminance - darkThreshold) / Mathf.Max(1e-6f, 1f - darkThreshold);
            return Mathf.Min(AsciiSettings.RampSize - 1, 1 + (int)(span * (AsciiSettings.RampSize - 1)));
        }

        public static int EdgeSlot(float gx, float gy, float threshold)
        {
            if (Mathf.Sqrt(gx * gx + gy * gy) < threshold)
            {
                return Blank;
            }
            var angle = Mathf.Repeat(Mathf.Atan2(gy, gx) * Mathf.Rad2Deg + 90f, 180f);
            return AsciiSettings.FirstEdgeSlot + Mathf.RoundToInt(angle / 45f) % 4;
        }
    }
}
```

- [ ] **Step 4: Write the lookups**

`Runtime/Ascii/GlyphLookups.cs`:

```csharp
using UnityEngine;

namespace Mortar.Unity
{
    public static class GlyphLookups
    {
        public const int RampWidth = 256;
        public const int EdgeSize = 64;
        public const int SlotStep = 255 / (AsciiSettings.Slots - 1);

        public static Texture2D Ramp(float darkThreshold)
        {
            var values = new byte[RampWidth];
            for (var i = 0; i < RampWidth; i++)
            {
                values[i] = Encode(GlyphRules.RampSlot((i + 0.5f) / RampWidth, darkThreshold));
            }
            return Create(RampWidth, 1, values, FilterMode.Point, "Mortar Ramp Lookup");
        }

        public static Texture2D Edges(float threshold)
        {
            var values = new byte[EdgeSize * EdgeSize];
            for (var y = 0; y < EdgeSize; y++)
            {
                for (var x = 0; x < EdgeSize; x++)
                {
                    values[y * EdgeSize + x] = Encode(GlyphRules.EdgeSlot(Gradient(x), Gradient(y), threshold));
                }
            }
            return Create(EdgeSize, EdgeSize, values, FilterMode.Point, "Mortar Edge Lookup");
        }

        public static float Gradient(int texel) => (texel + 0.5f) / EdgeSize * 2f - 1f;

        public static Texture2D Create(int width, int height, byte[] values, FilterMode filter, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.R8, false, true)
            {
                name = name,
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            texture.SetPixelData(values, 0);
            texture.Apply(false, false);
            return texture;
        }

        private static byte Encode(int slot) => (byte)(slot * SlotStep);
    }
}
```

- [ ] **Step 5: Import the atlas with point filtering**

In `Editor/ArtImport.cs`, add the constant and version below `MaxSheetSize`, the early branch at the top of `OnPreprocessTexture`, and the helper after `ConfigureSheet`:

```csharp
        public const string GlyphAtlasPath = "Packages/com.mortar.unity/Runtime/Resources/Mortar/GlyphAtlas.png";

        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            if (assetPath == GlyphAtlasPath)
            {
                ConfigureAtlas((TextureImporter)assetImporter);
                return;
            }
            if (SpriteCatalogBuilder.KeyFor(assetPath, SpriteCatalogBuilder.ArtRoot) == null)
            {
                return;
            }
            // ... existing body unchanged ...
        }

        private static void ConfigureAtlas(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
```

`GetVersion` makes Unity re-import every texture this postprocessor handles, so the atlas imported in Task 1 picks up these settings.

- [ ] **Step 6: Run the tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: PASS, all EditMode tests green.

Run (from `tools/art`): `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider tests/test_glyphs.py`
Expected: PASS, now including `test_slot_constants_match_the_unity_settings`.

- [ ] **Step 7: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Ascii.meta unity/com.mortar.unity/Runtime/Ascii unity/com.mortar.unity/Editor/ArtImport.cs unity/com.mortar.unity/Tests/Editor/GlyphRulesTests.cs unity/com.mortar.unity/Tests/Editor/GlyphRulesTests.cs.meta unity/com.mortar.unity/Tests/Editor/CellGridTests.cs unity/com.mortar.unity/Tests/Editor/CellGridTests.cs.meta unity/com.mortar.unity/Tests/Editor/GlyphLookupsTests.cs unity/com.mortar.unity/Tests/Editor/GlyphLookupsTests.cs.meta unity/com.mortar.unity/Tests/Editor/ArtImportTests.cs
git status --short
git commit -m "add glyph rules and lookup textures"
```

---

### Task 3: The glyph filter on the camera

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Resources/Mortar/Ascii.shader`, `Downsample.cginc`, `Glyph.cginc`
- Create: `unity/com.mortar.unity/Runtime/Ascii/AsciiResources.cs`, `AsciiMaterial.cs`, `AsciiFilter.cs`
- Modify: `unity/com.mortar.unity/Runtime/Views/CameraFollow.cs`, `unity/com.mortar.unity/Tests/Runtime/ArtPreviewCapture.cs`
- Test: `unity/com.mortar.unity/Tests/Runtime/AsciiScene.cs`, `AsciiFilterTests.cs`

**Interfaces:**
- Consumes: `AsciiSettings`, `CellGrid`, `GlyphLookups.Ramp/Edges` (Task 2); `Resources/Mortar/GlyphAtlas.png` (Task 1).
- Produces: `AsciiResources(Shader? shader, Texture2D? atlas)` with `Shader`, `Atlas`, `Problem` (string or null), `static Load()`; `AsciiMaterial.Create(Shader shader, Texture2D atlas, List<Object> owned) -> Material`; `AsciiFilter` component with `static Attach(Camera camera, AsciiResources resources) -> AsciiFilter`, `IsUsable`, `IsOn`, `Problem`, `Toggle()`; `CameraFollow.Filter`; test helper `AsciiScene` (`Width = 1200`, `Height = 600`, `Camera`, `AddCells(int column, int row, int columns, int rows, Color color, Material? material = null, int order = 0)`, `Render()`, `Pixel(int x, int y)`, `LitPixels(int column, int row)`, `InkSpan(int column, int row)`, `Read(RenderTexture texture)`).

- [ ] **Step 1: Write the test scene helper**

`Tests/Runtime/AsciiScene.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mortar.Unity.Tests
{
    public sealed class AsciiScene : IDisposable
    {
        public const int Width = 1200;
        public const int Height = 600;
        public const float PixelsPerUnit = 100f;
        public const int LitLevel = 60;

        private readonly GameObject _root = new GameObject("ascii-scene");
        private readonly List<Object> _owned = new List<Object>();
        private readonly RenderTexture _target = new RenderTexture(Width, Height, 24);
        private readonly Texture2D _white;
        private Color32[] _pixels = new Color32[0];

        public AsciiScene()
        {
            var go = new GameObject("ascii-camera");
            go.transform.SetParent(_root.transform, false);
            go.transform.position = new Vector3(0f, 0f, -10f);
            Camera = go.AddComponent<Camera>();
            Camera.orthographic = true;
            Camera.orthographicSize = Height / PixelsPerUnit / 2f;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = Color.black;
            Camera.targetTexture = _target;
            Camera.enabled = false;
            _white = new Texture2D(4, 4) { filterMode = FilterMode.Point };
            var pixels = new Color32[16];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }
            _white.SetPixels32(pixels);
            _white.Apply();
        }

        public Camera Camera { get; }

        public SpriteRenderer AddCells(int column, int row, int columns, int rows, Color color, Material? material = null, int order = 0)
        {
            var sprite = Sprite.Create(_white, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            _owned.Add(sprite);
            var size = new Vector2(columns * AsciiSettings.CellWidth, rows * AsciiSettings.CellHeight);
            var left = column * AsciiSettings.CellWidth - Width / 2f;
            var bottom = row * AsciiSettings.CellHeight - Height / 2f;
            var go = new GameObject($"cells {column},{row}");
            go.transform.SetParent(_root.transform, false);
            go.transform.position = new Vector3(left + size.x / 2f, bottom + size.y / 2f, 0f) / PixelsPerUnit;
            go.transform.localScale = new Vector3(size.x / PixelsPerUnit, size.y / PixelsPerUnit, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
            return renderer;
        }

        public void Render()
        {
            Camera.Render();
            _pixels = Read(_target);
        }

        public Color32 Pixel(int x, int y) => _pixels[y * Width + x];

        public int LitPixels(int column, int row)
        {
            var count = 0;
            ForEachPixel(column, row, (x, y, pixel) => count += IsLit(pixel) ? 1 : 0);
            return count;
        }

        public Vector2Int InkSpan(int column, int row)
        {
            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            var max = new Vector2Int(int.MinValue, int.MinValue);
            ForEachPixel(column, row, (x, y, pixel) =>
            {
                if (!IsLit(pixel)) return;
                min = Vector2Int.Min(min, new Vector2Int(x, y));
                max = Vector2Int.Max(max, new Vector2Int(x, y));
            });
            return max.x < min.x ? Vector2Int.zero : max - min + Vector2Int.one;
        }

        public Color32[] Read(RenderTexture texture)
        {
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            var pixels = image.GetPixels32();
            Object.Destroy(image);
            return pixels;
        }

        public void Dispose()
        {
            Camera.targetTexture = null;
            Object.Destroy(_root);
            _target.Release();
            Object.Destroy(_target);
            Object.Destroy(_white);
            foreach (var item in _owned)
            {
                Object.Destroy(item);
            }
            _owned.Clear();
        }

        private static bool IsLit(Color32 pixel) => Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)) > LitLevel;

        private void ForEachPixel(int column, int row, Action<int, int, Color32> visit)
        {
            for (var y = row * AsciiSettings.CellHeight; y < (row + 1) * AsciiSettings.CellHeight; y++)
            {
                for (var x = column * AsciiSettings.CellWidth; x < (column + 1) * AsciiSettings.CellWidth; x++)
                {
                    visit(x, y, Pixel(x, y));
                }
            }
        }
    }
}
```

- [ ] **Step 2: Write the failing filter tests**

`Tests/Runtime/AsciiFilterTests.cs`:

```csharp
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mortar.Unity.Tests
{
    public sealed class AsciiFilterTests
    {
        private AsciiScene _scene = null!;
        private AsciiFilter _filter = null!;

        [SetUp]
        public void SetUp()
        {
            _scene = new AsciiScene();
            _filter = AsciiFilter.Attach(_scene.Camera, AsciiResources.Load());
        }

        [TearDown]
        public void TearDown() => _scene.Dispose();

        private static int BackgroundLevel => Mathf.RoundToInt(AsciiSettings.Background * 255f);

        [Test]
        public void TheFilterLoadsItsResources()
        {
            Assert.IsNull(_filter.Problem);
            Assert.IsTrue(_filter.IsUsable);
        }

        [Test]
        public void ABlackSceneShowsOnlyTheBackground()
        {
            _scene.Render();

            for (var y = 0; y < AsciiScene.Height; y += 7)
            {
                for (var x = 0; x < AsciiScene.Width; x += 5)
                {
                    Assert.That(_scene.Pixel(x, y).r, Is.InRange(BackgroundLevel - 1, BackgroundLevel + 1), $"pixel {x},{y}");
                }
            }
        }

        [Test]
        public void ABrightAreaShowsGlyphsInItsColour()
        {
            _scene.AddCells(80, 20, 40, 20, new Color(1f, 0.5f, 0.2f));

            _scene.Render();

            var brightest = new Color32(0, 0, 0, 255);
            var background = 0;
            for (var y = 300; y < 310; y++)
            {
                for (var x = 600; x < 606; x++)
                {
                    var pixel = _scene.Pixel(x, y);
                    if (pixel.r > brightest.r) brightest = pixel;
                    if (pixel.r <= BackgroundLevel + 1) background++;
                }
            }
            Assert.Greater(brightest.r, 150, "the glyph should be inked in the area's colour");
            Assert.Greater(brightest.r, brightest.g);
            Assert.Greater(brightest.g, brightest.b);
            Assert.Greater(background, 0, "a glyph is not a solid block");
        }

        [Test]
        public void AVerticalBoundaryShowsABarGlyph()
        {
            _scene.AddCells(0, 0, 100, 60, Color.white);

            _scene.Render();

            var edge = _scene.InkSpan(99, 30);
            var inside = _scene.InkSpan(98, 30);
            Assert.LessOrEqual(edge.x, 2, "the boundary cell should be a thin vertical stroke");
            Assert.GreaterOrEqual(edge.y, 6);
            Assert.Greater(inside.x, 2, "a bright interior cell should use a dense ramp glyph");
        }

        [Test]
        public void TogglingShowsTheRawScene()
        {
            _scene.AddCells(0, 0, 100, 60, Color.white);
            _filter.Toggle();

            _scene.Render();

            Assert.IsFalse(_filter.IsOn);
            Assert.AreEqual(255, _scene.Pixel(300, 300).r);
            Assert.AreEqual(0, _scene.Pixel(900, 300).r);
        }

        [Test]
        public void AMissingShaderShowsTheRawSceneAndSaysWhy()
        {
            LogAssert.Expect(LogType.Warning, new Regex("ASCII filter off.*did not load"));
            Object.DestroyImmediate(_filter);
            var broken = AsciiFilter.Attach(_scene.Camera, new AsciiResources(null, null));
            _scene.AddCells(0, 0, 100, 60, Color.white);

            _scene.Render();

            Assert.IsFalse(broken.IsUsable);
            StringAssert.Contains("did not load", broken.Problem);
            Assert.AreEqual(255, _scene.Pixel(300, 300).r);
        }
    }
}
```

The last test removes the working filter with `DestroyImmediate`: image effects on one camera chain, so a filter left alive until the end of the frame would feed glyphs into the broken one.

- [ ] **Step 3: Run the tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode -Filter AsciiFilterTests`
Expected: FAIL to compile (`AsciiFilter`, `AsciiResources` missing).

- [ ] **Step 4: Write the shader**

`Runtime/Resources/Mortar/Ascii.shader`:

```hlsl
Shader "Hidden/Mortar/Ascii"
{
    Properties
    {
        _MainTex ("Source", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "Downsample"
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragDownsample
            #pragma target 3.0
            #include "Downsample.cginc"
            ENDCG
        }

        Pass
        {
            Name "Glyph"
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragGlyph
            #pragma target 3.0
            #include "Glyph.cginc"
            ENDCG
        }
    }
    Fallback Off
}
```

`Runtime/Resources/Mortar/Downsample.cginc`:

```hlsl
#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_TexelSize;
float4 _CellCount;

// Fifteen bilinear taps between pixel pairs average a 6x10 cell (AsciiSettings.CellWidth/CellHeight).
fixed4 FragDownsample(v2f_img i) : SV_Target
{
    float2 origin = floor(i.uv * _CellCount.xy) * float2(6.0, 10.0);
    float3 sum = 0.0;
    for (int y = 0; y < 5; y++)
    {
        for (int x = 0; x < 3; x++)
        {
            float2 pixel = origin + float2(1.0 + 2.0 * x, 1.0 + 2.0 * y);
            sum += tex2Dlod(_MainTex, float4(pixel * _MainTex_TexelSize.xy, 0.0, 0.0)).rgb;
        }
    }
    return fixed4(sum / 15.0, 1.0);
}
```

`Runtime/Resources/Mortar/Glyph.cginc`:

```hlsl
#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_TexelSize;
sampler2D _CellTex;
sampler2D _GlyphAtlas;
sampler2D _RampLut;
sampler2D _EdgeLut;
float4 _CellCount;
float _Background;
float _Normalize;
float _Saturation;
float _DarkThreshold;

static const float2 CellPixels = float2(6.0, 10.0);
static const float Slots = 16.0;
static const float3 Luma = float3(0.299, 0.587, 0.114);

float4 Fetch(sampler2D lookup, float2 uv)
{
    return tex2Dlod(lookup, float4(uv, 0.0, 0.0));
}

float2 CellUv(float2 cell)
{
    return (cell + 0.5) / _CellCount.xy;
}

float CellLuma(float2 cell)
{
    return dot(Fetch(_CellTex, CellUv(cell)).rgb, Luma);
}

float Slot(float encoded)
{
    return round(encoded * (Slots - 1.0));
}

float ChooseSlot(float2 cell, float luma)
{
    if (luma < _DarkThreshold)
    {
        return 0.0;
    }
    float gx = (CellLuma(cell + float2(1.0, 0.0)) - CellLuma(cell - float2(1.0, 0.0))) * 0.5;
    float gy = (CellLuma(cell + float2(0.0, 1.0)) - CellLuma(cell - float2(0.0, 1.0))) * 0.5;
    float edge = Slot(Fetch(_EdgeLut, float2(gx, gy) * 0.5 + 0.5).r);
    return edge > 0.0 ? edge : Slot(Fetch(_RampLut, float2(luma, 0.5)).r);
}

float3 Tint(float3 color)
{
    float peak = max(max(color.r, color.g), max(color.b, 0.0001));
    float3 tint = lerp(color, color / peak, _Normalize);
    return lerp(dot(tint, Luma).xxx, tint, _Saturation);
}

float Ink(float slot, float2 local)
{
    float2 uv = float2((slot * CellPixels.x + local.x + 0.5) / (Slots * CellPixels.x), (local.y + 0.5) / CellPixels.y);
    return Fetch(_GlyphAtlas, uv).a;
}

fixed4 FragGlyph(v2f_img i) : SV_Target
{
    float2 uv = i.uv;
#if UNITY_UV_STARTS_AT_TOP
    if (_MainTex_TexelSize.y < 0.0)
    {
        uv.y = 1.0 - uv.y;
    }
#endif
    float2 pixel = floor(uv * abs(_MainTex_TexelSize.zw));
    float2 cell = floor(pixel / CellPixels);
    float3 color = Fetch(_CellTex, CellUv(cell)).rgb;
    float slot = ChooseSlot(cell, dot(color, Luma));
    float ink = Ink(slot, pixel - cell * CellPixels);
    return fixed4(lerp(_Background.xxx, Tint(color), ink), 1.0);
}
```

- [ ] **Step 5: Write the resources, material and filter**

`Runtime/Ascii/AsciiResources.cs`:

```csharp
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class AsciiResources
    {
        public const string ShaderPath = "Mortar/Ascii";
        public const string AtlasPath = "Mortar/GlyphAtlas";

        public AsciiResources(Shader? shader, Texture2D? atlas)
        {
            Shader = shader;
            Atlas = atlas;
        }

        public Shader? Shader { get; }

        public Texture2D? Atlas { get; }

        public string? Problem => Missing(Shader, ShaderPath) ?? (Atlas == null ? $"glyph atlas '{AtlasPath}' did not load" : null);

        public static AsciiResources Load() =>
            new AsciiResources(Resources.Load<Shader>(ShaderPath), Resources.Load<Texture2D>(AtlasPath));

        private static string? Missing(Shader? shader, string path)
        {
            if (shader == null)
            {
                return $"shader '{path}' did not load";
            }
            return shader.isSupported ? null : $"shader '{shader.name}' is not supported on this GPU";
        }
    }
}
```

`Runtime/Ascii/AsciiMaterial.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Mortar.Unity
{
    public static class AsciiMaterial
    {
        public static Material Create(Shader shader, Texture2D atlas, List<Object> owned)
        {
            var material = new Material(shader) { name = "Mortar Ascii", hideFlags = HideFlags.DontSave };
            owned.Add(material);
            material.SetTexture("_GlyphAtlas", atlas);
            material.SetTexture("_RampLut", Own(GlyphLookups.Ramp(AsciiSettings.DarkThreshold), owned));
            material.SetTexture("_EdgeLut", Own(GlyphLookups.Edges(AsciiSettings.EdgeThreshold), owned));
            material.SetFloat("_Background", AsciiSettings.Background);
            material.SetFloat("_Normalize", AsciiSettings.Normalize);
            material.SetFloat("_Saturation", AsciiSettings.Saturation);
            material.SetFloat("_DarkThreshold", AsciiSettings.DarkThreshold);
            return material;
        }

        private static Texture2D Own(Texture2D texture, List<Object> owned)
        {
            owned.Add(texture);
            return texture;
        }
    }
}
```

`Runtime/Ascii/AsciiFilter.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mortar.Unity
{
    [RequireComponent(typeof(Camera))]
    public sealed class AsciiFilter : MonoBehaviour
    {
        private const int DownsamplePass = 0;
        private const int GlyphPass = 1;

        private static readonly int CellCountId = Shader.PropertyToID("_CellCount");
        private static readonly int CellTexId = Shader.PropertyToID("_CellTex");

        private readonly List<Object> _owned = new List<Object>();
        private InputAction _toggle = null!;
        private Material? _material;

        public bool IsUsable => _material != null;

        public bool IsOn { get; private set; } = true;

        public string? Problem { get; private set; }

        public static AsciiFilter Attach(Camera camera, AsciiResources resources)
        {
            var filter = camera.gameObject.AddComponent<AsciiFilter>();
            filter.Use(resources);
            return filter;
        }

        public void Toggle() => IsOn = !IsOn;

        private void Use(AsciiResources resources)
        {
            Problem = resources.Problem;
            if (Problem != null)
            {
                Debug.LogWarning($"Mortar: ASCII filter off, showing the raw scene: {Problem}");
                return;
            }
            _material = AsciiMaterial.Create(resources.Shader!, resources.Atlas!, _owned);
        }

        private void Awake()
        {
            _toggle = new InputAction("ascii-toggle", InputActionType.Button, "<Keyboard>/f9");
            _toggle.performed += _ => Toggle();
        }

        private void OnEnable() => _toggle.Enable();

        private void OnDisable() => _toggle.Disable();

        private void OnDestroy()
        {
            _toggle.Dispose();
            foreach (var item in _owned)
            {
                if (item != null) Destroy(item);
            }
            _owned.Clear();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_material == null || !IsOn)
            {
                Graphics.Blit(source, destination);
                return;
            }
            var grid = new CellGrid(source.width, source.height);
            _material.SetVector(CellCountId, new Vector4(grid.Columns, grid.Rows, 0f, 0f));
            var cells = CellTexture(grid);
            Graphics.Blit(source, cells, _material, DownsamplePass);
            _material.SetTexture(CellTexId, cells);
            Graphics.Blit(source, destination, _material, GlyphPass);
            RenderTexture.ReleaseTemporary(cells);
        }

        private static RenderTexture CellTexture(CellGrid grid)
        {
            var texture = RenderTexture.GetTemporary(grid.Columns, grid.Rows, 0, RenderTextureFormat.ARGB32);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }
    }
}
```

- [ ] **Step 6: Put the filter on the game camera**

In `Runtime/Views/CameraFollow.cs`, add the property after `Camera`, make the background black and attach the filter in `Create`:

```csharp
        public AsciiFilter Filter { get; private set; } = null!;
```

```csharp
            camera.backgroundColor = Color.black;
```

```csharp
            go.AddComponent<AudioListener>();
            var follow = go.AddComponent<CameraFollow>();
            follow.Camera = camera;
            follow.Filter = AsciiFilter.Attach(camera, AsciiResources.Load());
            return follow;
```

- [ ] **Step 7: Capture raw and filtered previews**

Replace `Capture` in `Tests/Runtime/ArtPreviewCapture.cs` and its two call sites:

```csharp
            app.StartNewGame();
            yield return null;
            Capture(app, "town");
            Journeys.TakeStairsDown(app.Game);
            yield return null;
            Capture(app, "cathedral");
```

```csharp
        private static void Capture(MortarApp app, string name)
        {
            CaptureCamera(app.CameraRig.Camera, name);
            app.CameraRig.Filter.Toggle();
            CaptureCamera(app.CameraRig.Camera, name + "-raw");
            app.CameraRig.Filter.Toggle();
        }

        private static void CaptureCamera(Camera camera, string name)
        {
            // ... the old Capture body, unchanged ...
        }
```

- [ ] **Step 8: Run the tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode -Filter AsciiFilterTests`
Expected: PASS (6 tests).

Then run both full suites: `pwsh -File tools/unity/test.ps1 -Platform PlayMode` and `-Platform EditMode`.
Expected: all green, and no `ASCII filter off` warning in `out/unity-tests/PlayMode.log` except from `AMissingShaderShowsTheRawSceneAndSaysWhy`.

If `AVerticalBoundaryShowsABarGlyph` fails, inspect the frame: `pwsh -File tools/unity/test.ps1 -Platform PlayMode -Filter ArtPreviewCapture` writes `out/art-preview-*.png`. A filtered image that is upside down or shifted by one cell points at the `UNITY_UV_STARTS_AT_TOP` flip in `Glyph.cginc`.

- [ ] **Step 9: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Resources/Mortar unity/com.mortar.unity/Runtime/Ascii unity/com.mortar.unity/Runtime/Views/CameraFollow.cs unity/com.mortar.unity/Tests/Runtime/AsciiScene.cs unity/com.mortar.unity/Tests/Runtime/AsciiScene.cs.meta unity/com.mortar.unity/Tests/Runtime/AsciiFilterTests.cs unity/com.mortar.unity/Tests/Runtime/AsciiFilterTests.cs.meta unity/com.mortar.unity/Tests/Runtime/ArtPreviewCapture.cs
git status --short
git commit -m "draw the world as glyphs"
```

---

## Phase 2: World

### Task 4: The player light

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Ascii/LightFalloff.cs`, `AsciiLight.cs`, `PlayerLight.cs`
- Modify: `Runtime/Ascii/AsciiSettings.cs`, `GlyphLookups.cs`, `AsciiMaterial.cs`, `AsciiFilter.cs`; `Runtime/Resources/Mortar/Downsample.cginc`; `Runtime/App/MortarApp.cs`
- Test: `Tests/Editor/GlyphLookupsTests.cs` (add), `Tests/Runtime/AsciiFilterTests.cs` (add), `Tests/Runtime/MortarAppTests.cs` (add)

**Interfaces:**
- Consumes: Task 3's `AsciiFilter`, `AsciiMaterial`, `AsciiScene`.
- Produces: `AsciiSettings.LightRadiusTiles = 6f`, `AsciiSettings.LightInner = 0.55f`; `LightFalloff.At(float distance) -> float`; `GlyphLookups.FalloffWidth = 64`, `GlyphLookups.Falloff() -> Texture2D`; `readonly struct AsciiLight(Vector3 position, float radius, float intensity)` with `Position`, `Radius` (world units, horizontal semi-axis), `Intensity`; `AsciiFilter.MaxLights = 4`, `AsciiFilter.Ambient` (float, default 1), `AsciiFilter.Lights` (`List<AsciiLight>`); `PlayerLight.RadiusUnits`, `PlayerLight.Apply(AsciiFilter filter, int depth, Vector3? player)`.

- [ ] **Step 1: Write the failing tests**

Add to `Tests/Editor/GlyphLookupsTests.cs`:

```csharp
        [TestCase(0f, 1f)]
        [TestCase(0.55f, 1f)]
        [TestCase(1f, 0f)]
        [TestCase(2f, 0f)]
        public void LightFallsOffFromTheInnerRadiusToTheEdge(float distance, float expected)
        {
            Assert.AreEqual(expected, LightFalloff.At(distance), 1e-5f);
        }

        [Test]
        public void LightFallsOffSmoothly()
        {
            var previous = 1f;
            for (var d = 0f; d <= 1f; d += 0.05f)
            {
                var value = LightFalloff.At(d);
                Assert.LessOrEqual(value, previous + 1e-6f);
                previous = value;
            }
            Assert.That(LightFalloff.At(0.8f), Is.InRange(0.1f, 0.9f));
        }

        [Test]
        public void TheFalloffLookupStartsBrightAndEndsDark()
        {
            var falloff = GlyphLookups.Falloff();
            var values = falloff.GetRawTextureData();

            Assert.AreEqual(GlyphLookups.FalloffWidth, falloff.width);
            Assert.AreEqual(FilterMode.Bilinear, falloff.filterMode);
            Assert.AreEqual(255, values[0]);
            Assert.LessOrEqual(values[GlyphLookups.FalloffWidth - 1], 1);
            Object.DestroyImmediate(falloff);
        }
```

Add to `Tests/Runtime/AsciiFilterTests.cs`:

```csharp
        [Test]
        public void InTheDarkOnlyTheLightRadiusShowsGlyphs()
        {
            _scene.AddCells(0, 0, 200, 60, Color.white);
            _filter.Ambient = 0f;
            _filter.Lights.Add(new AsciiLight(Vector3.zero, 2f, 1f));

            _scene.Render();

            Assert.Greater(_scene.LitPixels(100, 30), 0, "the lit centre should draw glyphs");
            Assert.AreEqual(0, _scene.LitPixels(100, 52), "the light is half as tall as it is wide");
            Assert.AreEqual(0, _scene.LitPixels(0, 0), "outside the light is blank");
        }

        [Test]
        public void FullAmbientLightsEverything()
        {
            _scene.AddCells(0, 0, 200, 60, Color.white);
            _filter.Ambient = 1f;

            _scene.Render();

            Assert.Greater(_scene.LitPixels(1, 1), 0);
        }
```

Add to `Tests/Runtime/MortarAppTests.cs`:

```csharp
        [Test]
        public void TownIsLitAndDungeonsOnlyAroundThePlayer()
        {
            _app.StartNewGame();
            _app.RefreshFrame();

            Assert.AreEqual(1f, _app.CameraRig.Filter.Ambient);
            Assert.AreEqual(1, _app.CameraRig.Filter.Lights.Count);

            Journeys.TakeStairsDown(_app.Game);
            _app.RefreshFrame();

            Assert.AreEqual(0f, _app.CameraRig.Filter.Ambient);
            Assert.AreEqual(1, _app.CameraRig.Filter.Lights.Count);
            Assert.AreEqual(PlayerLight.RadiusUnits, _app.CameraRig.Filter.Lights[0].Radius);
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode -Filter GlyphLookupsTests`
Expected: FAIL to compile (`LightFalloff`, `GlyphLookups.Falloff` missing).

- [ ] **Step 3: Write the falloff, the light and its lookup**

Add to `AsciiSettings` after `Saturation`:

```csharp
        public const float LightRadiusTiles = 6f;
        public const float LightInner = 0.55f;
```

`Runtime/Ascii/LightFalloff.cs`:

```csharp
using UnityEngine;

namespace Mortar.Unity
{
    public static class LightFalloff
    {
        public static float At(float distance)
        {
            var t = Mathf.Clamp01((distance - AsciiSettings.LightInner) / (1f - AsciiSettings.LightInner));
            return 1f - t * t * (3f - 2f * t);
        }
    }
}
```

`Runtime/Ascii/AsciiLight.cs`:

```csharp
using UnityEngine;

namespace Mortar.Unity
{
    public readonly struct AsciiLight
    {
        public AsciiLight(Vector3 position, float radius, float intensity)
        {
            Position = position;
            Radius = radius;
            Intensity = intensity;
        }

        public Vector3 Position { get; }

        public float Radius { get; }

        public float Intensity { get; }
    }
}
```

`Runtime/Ascii/PlayerLight.cs`:

```csharp
using UnityEngine;

namespace Mortar.Unity
{
    public static class PlayerLight
    {
        public static readonly float RadiusUnits = AsciiSettings.LightRadiusTiles / Mathf.Sqrt(2f);

        public static void Apply(AsciiFilter filter, int depth, Vector3? player)
        {
            filter.Ambient = depth > 0 ? 0f : 1f;
            filter.Lights.Clear();
            if (player.HasValue)
            {
                filter.Lights.Add(new AsciiLight(player.Value, RadiusUnits, 1f));
            }
        }
    }
}
```

Add to `GlyphLookups` after `EdgeSize`, and the method after `Edges`:

```csharp
        public const int FalloffWidth = 64;
```

```csharp
        public static Texture2D Falloff()
        {
            var values = new byte[FalloffWidth];
            for (var i = 0; i < FalloffWidth; i++)
            {
                values[i] = (byte)Mathf.RoundToInt(LightFalloff.At((float)i / (FalloffWidth - 1)) * 255f);
            }
            return Create(FalloffWidth, 1, values, FilterMode.Bilinear, "Mortar Light Falloff");
        }
```

Add to `AsciiMaterial.Create` after the edge lookup:

```csharp
            material.SetTexture("_FalloffLut", Own(GlyphLookups.Falloff(), owned));
```

- [ ] **Step 4: Light the downsample pass**

Replace `Runtime/Resources/Mortar/Downsample.cginc`:

```hlsl
#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_TexelSize;
float4 _CellCount;
sampler2D _FalloffLut;
float4 _Lights[4];
float _LightCount;
float _Ambient;

// Each light is (x pixels, y pixels, horizontal radius in pixels, intensity); the ellipse is half as tall.
float LightAt(float2 pixel)
{
    float light = _Ambient;
    for (int k = 0; k < 4; k++)
    {
        if (k < _LightCount)
        {
            float2 offset = (pixel - _Lights[k].xy) / max(_Lights[k].z, 1.0);
            offset.y *= 2.0;
            float falloff = tex2Dlod(_FalloffLut, float4(length(offset), 0.5, 0.0, 0.0)).r;
            light = max(light, falloff * _Lights[k].w);
        }
    }
    return light;
}

// Fifteen bilinear taps between pixel pairs average a 6x10 cell (AsciiSettings.CellWidth/CellHeight).
fixed4 FragDownsample(v2f_img i) : SV_Target
{
    float2 origin = floor(i.uv * _CellCount.xy) * float2(6.0, 10.0);
    float3 sum = 0.0;
    for (int y = 0; y < 5; y++)
    {
        for (int x = 0; x < 3; x++)
        {
            float2 pixel = origin + float2(1.0 + 2.0 * x, 1.0 + 2.0 * y);
            sum += tex2Dlod(_MainTex, float4(pixel * _MainTex_TexelSize.xy, 0.0, 0.0)).rgb;
        }
    }
    return fixed4(sum / 15.0 * LightAt(origin + float2(3.0, 5.0)), 1.0);
}
```

- [ ] **Step 5: Feed lights to the material**

Replace `Runtime/Ascii/AsciiFilter.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mortar.Unity
{
    [RequireComponent(typeof(Camera))]
    public sealed class AsciiFilter : MonoBehaviour
    {
        public const int MaxLights = 4;

        private const int DownsamplePass = 0;
        private const int GlyphPass = 1;

        private static readonly int CellCountId = Shader.PropertyToID("_CellCount");
        private static readonly int CellTexId = Shader.PropertyToID("_CellTex");
        private static readonly int AmbientId = Shader.PropertyToID("_Ambient");
        private static readonly int LightCountId = Shader.PropertyToID("_LightCount");
        private static readonly int LightsId = Shader.PropertyToID("_Lights");

        private readonly List<Object> _owned = new List<Object>();
        private readonly Vector4[] _lightData = new Vector4[MaxLights];
        private InputAction _toggle = null!;
        private Camera _camera = null!;
        private Material? _material;

        public bool IsUsable => _material != null;

        public bool IsOn { get; private set; } = true;

        public string? Problem { get; private set; }

        public float Ambient { get; set; } = 1f;

        public List<AsciiLight> Lights { get; } = new List<AsciiLight>();

        public static AsciiFilter Attach(Camera camera, AsciiResources resources)
        {
            var filter = camera.gameObject.AddComponent<AsciiFilter>();
            filter.Use(resources);
            return filter;
        }

        public void Toggle() => IsOn = !IsOn;

        private void Use(AsciiResources resources)
        {
            Problem = resources.Problem;
            if (Problem != null)
            {
                Debug.LogWarning($"Mortar: ASCII filter off, showing the raw scene: {Problem}");
                return;
            }
            _material = AsciiMaterial.Create(resources.Shader!, resources.Atlas!, _owned);
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _toggle = new InputAction("ascii-toggle", InputActionType.Button, "<Keyboard>/f9");
            _toggle.performed += _ => Toggle();
        }

        private void OnEnable() => _toggle.Enable();

        private void OnDisable() => _toggle.Disable();

        private void OnDestroy()
        {
            _toggle.Dispose();
            foreach (var item in _owned)
            {
                if (item != null) Destroy(item);
            }
            _owned.Clear();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_material == null || !IsOn)
            {
                Graphics.Blit(source, destination);
                return;
            }
            var grid = new CellGrid(source.width, source.height);
            _material.SetVector(CellCountId, new Vector4(grid.Columns, grid.Rows, 0f, 0f));
            ApplyLights(_material, source.width, source.height);
            var cells = CellTexture(grid);
            Graphics.Blit(source, cells, _material, DownsamplePass);
            _material.SetTexture(CellTexId, cells);
            Graphics.Blit(source, destination, _material, GlyphPass);
            RenderTexture.ReleaseTemporary(cells);
        }

        private void ApplyLights(Material material, int width, int height)
        {
            var count = Mathf.Min(Lights.Count, MaxLights);
            var pixelsPerUnit = height / (2f * _camera.orthographicSize);
            for (var i = 0; i < count; i++)
            {
                var light = Lights[i];
                var viewport = _camera.WorldToViewportPoint(light.Position);
                _lightData[i] = new Vector4(viewport.x * width, viewport.y * height, light.Radius * pixelsPerUnit, light.Intensity);
            }
            material.SetFloat(AmbientId, Ambient);
            material.SetFloat(LightCountId, count);
            material.SetVectorArray(LightsId, _lightData);
        }

        private static RenderTexture CellTexture(CellGrid grid)
        {
            var texture = RenderTexture.GetTemporary(grid.Columns, grid.Rows, 0, RenderTextureFormat.ARGB32);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }
    }
}
```

- [ ] **Step 6: Light the player each frame**

In `Runtime/App/MortarApp.cs`, `RefreshFrame`, add after `var world = session.World;`:

```csharp
            PlayerLight.Apply(CameraRig.Filter, world.Depth, PlayerPosition());
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode` and `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: all green, including the five new tests.

In `InTheDarkOnlyTheLightRadiusShowsGlyphs` the light sits at pixel (600, 300) with a 200 px horizontal radius, so it reaches 100 px up; cell row 52 starts at y = 520, past the ellipse.

- [ ] **Step 8: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Ascii unity/com.mortar.unity/Runtime/Resources/Mortar/Downsample.cginc unity/com.mortar.unity/Runtime/App/MortarApp.cs unity/com.mortar.unity/Tests/Editor/GlyphLookupsTests.cs unity/com.mortar.unity/Tests/Runtime/AsciiFilterTests.cs unity/com.mortar.unity/Tests/Runtime/MortarAppTests.cs
git status --short
git commit -m "light dungeons around the player"
```

---

### Task 5: The stroke mask for grass and water

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Resources/Mortar/AsciiMask.shader`, `Mask.cginc`, `StrokeSprite.shader`
- Create: `unity/com.mortar.unity/Runtime/Ascii/AsciiMask.cs`, `MaskMaterials.cs`
- Modify: `Runtime/Ascii/AsciiSettings.cs`, `AsciiResources.cs`, `AsciiMaterial.cs`, `AsciiFilter.cs`; `Runtime/Resources/Mortar/Glyph.cginc`; `Runtime/Views/LevelView.cs`
- Test: `Tests/Runtime/AsciiFilterTests.cs` (modify and add), `Tests/Runtime/LevelViewTests.cs` (add)

**Interfaces:**
- Consumes: Task 4's `AsciiFilter`, `AsciiScene`.
- Produces: `AsciiSettings.StrokeScale = 0.15f`, `AsciiSettings.StrokeSpeed = 0.6f`; `AsciiResources(Shader? shader, Shader? maskShader, Texture2D? atlas)` with `MaskShader` and `MaskShaderPath = "Mortar/AsciiMask"`; `AsciiMask(Camera main, Shader shader)` with `Texture`, `Render(Camera main, CellGrid grid) -> RenderTexture`, `Dispose()`; `AsciiFilter.MaskTexture`; `MaskMaterials.StrokeShaderPath`, `MaskMaterials.Stroke` (Material or null); `LevelView.StrokeTileCount`, `LevelView.IsStroke(string ground) -> bool`. Shader tags: stroke sprites use `RenderType = MortarStroke`.

- [ ] **Step 1: Write the failing tests**

In `Tests/Runtime/AsciiFilterTests.cs`, change the broken-resources line in `AMissingShaderShowsTheRawSceneAndSaysWhy` to `new AsciiResources(null, null, null)`, then add:

```csharp
        [Test]
        public void StrokeSpritesMarkTheMaskUnlessSomethingCoversThem()
        {
            _scene.AddCells(0, 0, 100, 60, Color.white, MaskMaterials.Stroke);
            _scene.AddCells(20, 0, 20, 60, Color.white, null, 1);
            _scene.AddCells(100, 0, 100, 60, Color.white);

            _scene.Render();

            var mask = _filter.MaskTexture!;
            var pixels = _scene.Read(mask);
            Assert.AreEqual(new CellGrid(AsciiScene.Width, AsciiScene.Height).Columns, mask.width);
            Assert.AreEqual(255, pixels[30 * mask.width + 50].r, "the stroke sprite marks its cells");
            Assert.AreEqual(0, pixels[30 * mask.width + 30].r, "a sprite in front clears the mask");
            Assert.AreEqual(0, pixels[30 * mask.width + 150].r, "plain sprites never mark the mask");
        }

        [Test]
        public void StrokeCellsDrawThinDashesInsteadOfDenseGlyphs()
        {
            _scene.AddCells(0, 0, 100, 60, Color.white, MaskMaterials.Stroke);
            _scene.AddCells(100, 0, 100, 60, Color.white);

            _scene.Render();

            Assert.Greater(_scene.LitPixels(50, 30), 0);
            Assert.Less(_scene.LitPixels(50, 30) * 2, _scene.LitPixels(150, 30));
        }
```

Add to `Tests/Runtime/LevelViewTests.cs`:

```csharp
        [Test]
        public void TownGrassAndWaterUseTheStrokeTilemap()
        {
            var world = TestContent.NewSession().World;

            Show(world);

            var cells = world.Level.Layout.Cells;
            var expected = 0;
            for (var y = 0; y < cells.Height; y++)
            {
                for (var x = 0; x < cells.Width; x++)
                {
                    if (LevelView.IsStroke(cells.GroundAt(x, y))) expected++;
                }
            }
            Assert.Greater(expected, 0, "the town should have grass or water");
            Assert.AreEqual(expected, _view.StrokeTileCount);
            Assert.AreEqual(NonVoidCells(world.Level), _view.TileCount);
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode -Filter "AsciiFilterTests|LevelViewTests"`
Expected: FAIL to compile (`MaskMaterials`, `MaskTexture`, the three-argument `AsciiResources`, `LevelView.IsStroke` missing).

- [ ] **Step 3: Write the mask shaders**

`Runtime/Resources/Mortar/Mask.cginc`:

```hlsl
#include "UnitySprites.cginc"

fixed4 Mask(v2f i, fixed4 value)
{
    clip(SampleSpriteTexture(i.texcoord).a * i.color.a - 0.5);
    return value;
}

fixed4 FragStroke(v2f i) : SV_Target { return Mask(i, fixed4(1.0, 0.0, 0.0, 1.0)); }

fixed4 FragGlow(v2f i) : SV_Target { return Mask(i, fixed4(0.0, 1.0, 0.0, 1.0)); }

fixed4 FragOccluder(v2f i) : SV_Target { return Mask(i, fixed4(0.0, 0.0, 0.0, 1.0)); }
```

`Runtime/Resources/Mortar/AsciiMask.shader`:

```hlsl
Shader "Hidden/Mortar/AsciiMask"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="MortarStroke" "Queue"="Transparent" }
        Cull Off ZWrite Off Blend Off
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment FragStroke
            #pragma target 2.0
            #include "Mask.cginc"
            ENDCG
        }
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Cull Off ZWrite Off Blend Off
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment FragOccluder
            #pragma target 2.0
            #include "Mask.cginc"
            ENDCG
        }
    }
    Fallback Off
}
```

`Runtime/Resources/Mortar/StrokeSprite.shader`:

```hlsl
Shader "Mortar/StrokeSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="MortarStroke" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            ENDCG
        }
    }
}
```

- [ ] **Step 4: Draw stroke glyphs from the mask**

Replace `Runtime/Resources/Mortar/Glyph.cginc`:

```hlsl
#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_TexelSize;
sampler2D _CellTex;
sampler2D _MaskTex;
sampler2D _GlyphAtlas;
sampler2D _RampLut;
sampler2D _EdgeLut;
float4 _CellCount;
float _Background;
float _Normalize;
float _Saturation;
float _DarkThreshold;
float _StrokeScale;
float _StrokeSpeed;

static const float2 CellPixels = float2(6.0, 10.0);
static const float Slots = 16.0;
static const float FirstEdgeSlot = 12.0;
static const float3 Luma = float3(0.299, 0.587, 0.114);

float4 Fetch(sampler2D lookup, float2 uv)
{
    return tex2Dlod(lookup, float4(uv, 0.0, 0.0));
}

float2 CellUv(float2 cell)
{
    return (cell + 0.5) / _CellCount.xy;
}

float CellLuma(float2 cell)
{
    return dot(Fetch(_CellTex, CellUv(cell)).rgb, Luma);
}

float Slot(float encoded)
{
    return round(encoded * (Slots - 1.0));
}

float Hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float Noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float bottom = lerp(Hash(i), Hash(i + float2(1.0, 0.0)), u.x);
    float top = lerp(Hash(i + float2(0.0, 1.0)), Hash(i + float2(1.0, 1.0)), u.x);
    return lerp(bottom, top, u.y);
}

float StrokeSlot(float2 cell)
{
    float wave = Noise(cell * _StrokeScale + float2(_Time.y * _StrokeSpeed, 0.0));
    return FirstEdgeSlot + floor(frac(wave * 2.0) * 4.0);
}

float ChooseSlot(float2 cell, float luma)
{
    if (luma < _DarkThreshold)
    {
        return 0.0;
    }
    if (Fetch(_MaskTex, CellUv(cell)).r > 0.5)
    {
        return StrokeSlot(cell);
    }
    float gx = (CellLuma(cell + float2(1.0, 0.0)) - CellLuma(cell - float2(1.0, 0.0))) * 0.5;
    float gy = (CellLuma(cell + float2(0.0, 1.0)) - CellLuma(cell - float2(0.0, 1.0))) * 0.5;
    float edge = Slot(Fetch(_EdgeLut, float2(gx, gy) * 0.5 + 0.5).r);
    return edge > 0.0 ? edge : Slot(Fetch(_RampLut, float2(luma, 0.5)).r);
}

float3 Tint(float3 color)
{
    float peak = max(max(color.r, color.g), max(color.b, 0.0001));
    float3 tint = lerp(color, color / peak, _Normalize);
    return lerp(dot(tint, Luma).xxx, tint, _Saturation);
}

float Ink(float slot, float2 local)
{
    float2 uv = float2((slot * CellPixels.x + local.x + 0.5) / (Slots * CellPixels.x), (local.y + 0.5) / CellPixels.y);
    return Fetch(_GlyphAtlas, uv).a;
}

fixed4 FragGlyph(v2f_img i) : SV_Target
{
    float2 uv = i.uv;
#if UNITY_UV_STARTS_AT_TOP
    if (_MainTex_TexelSize.y < 0.0)
    {
        uv.y = 1.0 - uv.y;
    }
#endif
    float2 pixel = floor(uv * abs(_MainTex_TexelSize.zw));
    float2 cell = floor(pixel / CellPixels);
    float3 color = Fetch(_CellTex, CellUv(cell)).rgb;
    float slot = ChooseSlot(cell, dot(color, Luma));
    float ink = Ink(slot, pixel - cell * CellPixels);
    return fixed4(lerp(_Background.xxx, Tint(color), ink), 1.0);
}
```

- [ ] **Step 5: Write the mask camera and stroke material**

Add to `AsciiSettings` after `LightInner`:

```csharp
        public const float StrokeScale = 0.15f;
        public const float StrokeSpeed = 0.6f;
```

Add to `AsciiMaterial.Create` after `_DarkThreshold`:

```csharp
            material.SetFloat("_StrokeScale", AsciiSettings.StrokeScale);
            material.SetFloat("_StrokeSpeed", AsciiSettings.StrokeSpeed);
```

Replace `Runtime/Ascii/AsciiResources.cs`:

```csharp
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class AsciiResources
    {
        public const string ShaderPath = "Mortar/Ascii";
        public const string MaskShaderPath = "Mortar/AsciiMask";
        public const string AtlasPath = "Mortar/GlyphAtlas";

        public AsciiResources(Shader? shader, Shader? maskShader, Texture2D? atlas)
        {
            Shader = shader;
            MaskShader = maskShader;
            Atlas = atlas;
        }

        public Shader? Shader { get; }

        public Shader? MaskShader { get; }

        public Texture2D? Atlas { get; }

        public string? Problem =>
            Missing(Shader, ShaderPath)
            ?? Missing(MaskShader, MaskShaderPath)
            ?? (Atlas == null ? $"glyph atlas '{AtlasPath}' did not load" : null);

        public static AsciiResources Load() =>
            new AsciiResources(
                Resources.Load<Shader>(ShaderPath), Resources.Load<Shader>(MaskShaderPath), Resources.Load<Texture2D>(AtlasPath));

        private static string? Missing(Shader? shader, string path)
        {
            if (shader == null)
            {
                return $"shader '{path}' did not load";
            }
            return shader.isSupported ? null : $"shader '{shader.name}' is not supported on this GPU";
        }
    }
}
```

`Runtime/Ascii/AsciiMask.cs`:

```csharp
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mortar.Unity
{
    public sealed class AsciiMask : IDisposable
    {
        private readonly Camera _camera;
        private readonly Shader _shader;

        public AsciiMask(Camera main, Shader shader)
        {
            _shader = shader;
            var go = new GameObject("Ascii Mask Camera");
            go.transform.SetParent(main.transform, false);
            _camera = go.AddComponent<Camera>();
            _camera.enabled = false;
        }

        public RenderTexture? Texture { get; private set; }

        public RenderTexture Render(Camera main, CellGrid grid)
        {
            if (Texture == null || Texture.width != grid.Columns || Texture.height != grid.Rows)
            {
                Release();
                Texture = new RenderTexture(grid.Columns, grid.Rows, 0, RenderTextureFormat.ARGB32)
                {
                    name = "Mortar Ascii Mask",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
            }
            _camera.CopyFrom(main);
            _camera.targetTexture = Texture;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.allowMSAA = false;
            _camera.allowHDR = false;
            _camera.RenderWithShader(_shader, "RenderType");
            return Texture;
        }

        public void Dispose()
        {
            Release();
            if (_camera != null)
            {
                Object.Destroy(_camera.gameObject);
            }
        }

        private void Release()
        {
            if (Texture == null)
            {
                return;
            }
            Texture.Release();
            Object.Destroy(Texture);
            Texture = null;
        }
    }
}
```

`Runtime/Ascii/MaskMaterials.cs`:

```csharp
using UnityEngine;

namespace Mortar.Unity
{
    public static class MaskMaterials
    {
        public const string StrokeShaderPath = "Mortar/StrokeSprite";

        private static Material? _stroke;

        public static Material? Stroke
        {
            get
            {
                if (_stroke == null)
                {
                    _stroke = Create(StrokeShaderPath);
                }
                return _stroke;
            }
        }

        private static Material? Create(string path)
        {
            var shader = Resources.Load<Shader>(path);
            if (shader == null || !shader.isSupported)
            {
                return null;
            }
            return new Material(shader) { name = shader.name, hideFlags = HideFlags.DontSave };
        }
    }
}
```

- [ ] **Step 6: Render the mask in the filter**

Replace `Runtime/Ascii/AsciiFilter.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mortar.Unity
{
    [RequireComponent(typeof(Camera))]
    public sealed class AsciiFilter : MonoBehaviour
    {
        public const int MaxLights = 4;

        private const int DownsamplePass = 0;
        private const int GlyphPass = 1;

        private static readonly int CellCountId = Shader.PropertyToID("_CellCount");
        private static readonly int CellTexId = Shader.PropertyToID("_CellTex");
        private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
        private static readonly int AmbientId = Shader.PropertyToID("_Ambient");
        private static readonly int LightCountId = Shader.PropertyToID("_LightCount");
        private static readonly int LightsId = Shader.PropertyToID("_Lights");

        private readonly List<Object> _owned = new List<Object>();
        private readonly Vector4[] _lightData = new Vector4[MaxLights];
        private InputAction _toggle = null!;
        private Camera _camera = null!;
        private Material? _material;
        private AsciiMask? _mask;

        public bool IsUsable => _material != null;

        public bool IsOn { get; private set; } = true;

        public string? Problem { get; private set; }

        public float Ambient { get; set; } = 1f;

        public List<AsciiLight> Lights { get; } = new List<AsciiLight>();

        public RenderTexture? MaskTexture => _mask?.Texture;

        public static AsciiFilter Attach(Camera camera, AsciiResources resources)
        {
            var filter = camera.gameObject.AddComponent<AsciiFilter>();
            filter.Use(resources);
            return filter;
        }

        public void Toggle() => IsOn = !IsOn;

        private void Use(AsciiResources resources)
        {
            Problem = resources.Problem;
            if (Problem != null)
            {
                Debug.LogWarning($"Mortar: ASCII filter off, showing the raw scene: {Problem}");
                return;
            }
            _material = AsciiMaterial.Create(resources.Shader!, resources.Atlas!, _owned);
            _mask = new AsciiMask(_camera, resources.MaskShader!);
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _toggle = new InputAction("ascii-toggle", InputActionType.Button, "<Keyboard>/f9");
            _toggle.performed += _ => Toggle();
        }

        private void OnEnable() => _toggle.Enable();

        private void OnDisable() => _toggle.Disable();

        private void OnDestroy()
        {
            _toggle.Dispose();
            _mask?.Dispose();
            foreach (var item in _owned)
            {
                if (item != null) Destroy(item);
            }
            _owned.Clear();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_material == null || _mask == null || !IsOn)
            {
                Graphics.Blit(source, destination);
                return;
            }
            var grid = new CellGrid(source.width, source.height);
            _material.SetVector(CellCountId, new Vector4(grid.Columns, grid.Rows, 0f, 0f));
            _material.SetTexture(MaskTexId, _mask.Render(_camera, grid));
            ApplyLights(_material, source.width, source.height);
            var cells = CellTexture(grid);
            Graphics.Blit(source, cells, _material, DownsamplePass);
            _material.SetTexture(CellTexId, cells);
            Graphics.Blit(source, destination, _material, GlyphPass);
            RenderTexture.ReleaseTemporary(cells);
        }

        private void ApplyLights(Material material, int width, int height)
        {
            var count = Mathf.Min(Lights.Count, MaxLights);
            var pixelsPerUnit = height / (2f * _camera.orthographicSize);
            for (var i = 0; i < count; i++)
            {
                var light = Lights[i];
                var viewport = _camera.WorldToViewportPoint(light.Position);
                _lightData[i] = new Vector4(viewport.x * width, viewport.y * height, light.Radius * pixelsPerUnit, light.Intensity);
            }
            material.SetFloat(AmbientId, Ambient);
            material.SetFloat(LightCountId, count);
            material.SetVectorArray(LightsId, _lightData);
        }

        private static RenderTexture CellTexture(CellGrid grid)
        {
            var texture = RenderTexture.GetTemporary(grid.Columns, grid.Rows, 0, RenderTextureFormat.ARGB32);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }
    }
}
```

- [ ] **Step 7: Put grass and water on a stroke tilemap**

In `Runtime/Views/LevelView.cs` (it already has `using System.Collections.Generic;`), add the set and field:

```csharp
        private static readonly HashSet<string> StrokeGrounds = new HashSet<string> { "grass", "water" };
```

```csharp
        private Tilemap _strokes = null!;
```

Add and change the counts:

```csharp
        public int StrokeTileCount => CountTiles(_strokes);

        public int TileCount => FloorTileCount + RaisedTileCount + StrokeTileCount;
```

```csharp
        public static bool IsStroke(string ground) => StrokeGrounds.Contains(ground);
```

In `Create`, create the stroke tilemap after the raised one:

```csharp
            view._floor = CreateTilemap(grid.transform, "Floor", FloorOrder, TilemapRenderer.Mode.Chunk, null);
            view._strokes = CreateTilemap(grid.transform, "Strokes", FloorOrder, TilemapRenderer.Mode.Chunk, MaskMaterials.Stroke);
            view._raised = CreateTilemap(grid.transform, "Raised", WorldOrder, TilemapRenderer.Mode.Individual, null);
```

In `Show`, add a third batch and route stroke grounds to it:

```csharp
            var floor = new TileBatch();
            var strokes = new TileBatch();
            var raised = new TileBatch();
```

```csharp
                    var tile = TileFor(_catalog.Tile(theme, ground, variant, isRaised));
                    var batch = isRaised ? raised : IsStroke(ground) ? strokes : floor;
                    batch.Add(IsoMath.ToTileCell(new Cell(x, y)), tile);
```

```csharp
            floor.ApplyTo(_floor);
            strokes.ApplyTo(_strokes);
            raised.ApplyTo(_raised);
```

In `Clear`, add `_strokes.ClearAllTiles();` after `_floor.ClearAllTiles();`. Replace `CreateTilemap`:

```csharp
        private static Tilemap CreateTilemap(Transform parent, string name, int order, TilemapRenderer.Mode mode, Material? material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            renderer.mode = mode;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
            return tilemap;
        }
```

- [ ] **Step 8: Run the tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode` and `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: all green.

If `StrokeSpritesMarkTheMaskUnlessSomethingCoversThem` reads red under the covering sprite, the replacement pass is not keeping sprite sorting order. Report it rather than working around it: the fallback is a per-pixel depth or stencil approach, which needs a decision.

- [ ] **Step 9: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Resources/Mortar unity/com.mortar.unity/Runtime/Ascii unity/com.mortar.unity/Runtime/Views/LevelView.cs unity/com.mortar.unity/Tests/Runtime/AsciiFilterTests.cs unity/com.mortar.unity/Tests/Runtime/LevelViewTests.cs
git status --short
git commit -m "ripple grass and water with stroke glyphs"
```

---

### Task 6: Bloom and glowing loot, then the world review

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Resources/Mortar/Bloom.cginc`, `GlowSprite.shader`
- Modify: `Runtime/Resources/Mortar/Ascii.shader`, `Glyph.cginc`, `AsciiMask.shader` (`Mask.cginc` already has `FragGlow`); `Runtime/Ascii/AsciiSettings.cs`, `AsciiMaterial.cs`, `AsciiFilter.cs`, `MaskMaterials.cs`; `Runtime/Views/GroundItemViews.cs`
- Test: `Tests/Runtime/AsciiFilterTests.cs` (add)

**Interfaces:**
- Consumes: Task 5's mask (`_MaskTex`), `MaskMaterials`, `AsciiFilter`.
- Produces: `AsciiSettings.BloomThreshold = 0.6f`, `BloomStrength = 0.8f`, `GlowBoost = 1.5f`; `MaskMaterials.GlowShaderPath`, `MaskMaterials.Glow`; shader tag `RenderType = MortarGlow` writes mask green; ground items use the glow material.

- [ ] **Step 1: Write the failing tests**

Add to `Tests/Runtime/AsciiFilterTests.cs`:

```csharp
        [Test]
        public void BrightCellsBleedIntoTheirNeighbours()
        {
            _scene.AddCells(99, 29, 2, 2, Color.white);

            _scene.Render();

            Assert.Greater(_scene.LitPixels(101, 30), 0, "bloom should light the cell beside a bright one");
        }

        [Test]
        public void DimCellsDoNotBloom()
        {
            _scene.AddCells(99, 29, 2, 2, new Color(0.5f, 0.5f, 0.5f));

            _scene.Render();

            Assert.AreEqual(0, _scene.LitPixels(101, 30));
        }

        [Test]
        public void GlowingSpritesBloomEvenWhenDim()
        {
            _scene.AddCells(99, 29, 2, 2, new Color(0.4f, 0.4f, 0.4f), MaskMaterials.Glow);

            _scene.Render();

            Assert.AreEqual(255, _scene.Read(_filter.MaskTexture!)[30 * 200 + 99].g, "glow sprites mark the mask green");
            Assert.Greater(_scene.LitPixels(101, 30), 0, "glow should light the neighbouring cell");
        }
```

The numbers: a white 2×2 block bright-passes at 1.0; the 5-tap blur (0.4, 0.2, 0.1) gives the next cell 0.3 across and 0.18 after the vertical pass, times strength 0.8 is 0.144, above the 0.06 dark threshold. A 0.5 grey block is under the 0.6 bloom threshold, so nothing bleeds. A 0.4 grey glow block (0.5 after the sprite's 1.25 boost) bright-passes at 0.5 × 1.5 = 0.75, giving 0.108 beside it.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode -Filter AsciiFilterTests`
Expected: FAIL to compile (`MaskMaterials.Glow` missing).

- [ ] **Step 3: Write the bloom passes**

`Runtime/Resources/Mortar/Bloom.cginc`:

```hlsl
#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_TexelSize;
sampler2D _MaskTex;
float _BloomThreshold;
float _GlowBoost;

static const float3 Luma = float3(0.299, 0.587, 0.114);
static const float Weights[3] = { 0.4, 0.2, 0.1 };

float3 Fetch(float2 uv)
{
    return tex2Dlod(_MainTex, float4(uv, 0.0, 0.0)).rgb;
}

float3 Bright(float2 uv)
{
    float3 color = Fetch(uv);
    float over = saturate((dot(color, Luma) - _BloomThreshold) / (1.0 - _BloomThreshold));
    float glow = tex2Dlod(_MaskTex, float4(uv, 0.0, 0.0)).g;
    return color * (over + glow * _GlowBoost);
}

fixed4 FragBloomAcross(v2f_img i) : SV_Target
{
    float2 step = float2(_MainTex_TexelSize.x, 0.0);
    float3 sum = Bright(i.uv) * Weights[0];
    for (int k = 1; k <= 2; k++)
    {
        sum += (Bright(i.uv + step * k) + Bright(i.uv - step * k)) * Weights[k];
    }
    return fixed4(sum, 1.0);
}

fixed4 FragBloomDown(v2f_img i) : SV_Target
{
    float2 step = float2(0.0, _MainTex_TexelSize.y);
    float3 sum = Fetch(i.uv) * Weights[0];
    for (int k = 1; k <= 2; k++)
    {
        sum += (Fetch(i.uv + step * k) + Fetch(i.uv - step * k)) * Weights[k];
    }
    return fixed4(sum, 1.0);
}
```

Replace `Runtime/Resources/Mortar/Ascii.shader`:

```hlsl
Shader "Hidden/Mortar/Ascii"
{
    Properties
    {
        _MainTex ("Source", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "Downsample"
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragDownsample
            #pragma target 3.0
            #include "Downsample.cginc"
            ENDCG
        }

        Pass
        {
            Name "Glyph"
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragGlyph
            #pragma target 3.0
            #include "Glyph.cginc"
            ENDCG
        }

        Pass
        {
            Name "BloomAcross"
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragBloomAcross
            #pragma target 3.0
            #include "Bloom.cginc"
            ENDCG
        }

        Pass
        {
            Name "BloomDown"
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragBloomDown
            #pragma target 3.0
            #include "Bloom.cginc"
            ENDCG
        }
    }
    Fallback Off
}
```

In `Runtime/Resources/Mortar/Glyph.cginc`, add the two declarations after `float _StrokeSpeed;`:

```hlsl
sampler2D _BloomTex;
float _BloomStrength;
```

and in `FragGlyph` replace the colour line:

```hlsl
    float2 cellUv = CellUv(cell);
    float3 color = saturate(Fetch(_CellTex, cellUv).rgb + Fetch(_BloomTex, cellUv).rgb * _BloomStrength);
```

- [ ] **Step 4: Write the glow sprite and its mask**

`Runtime/Resources/Mortar/GlowSprite.shader`:

```hlsl
Shader "Mortar/GlowSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Boost ("Boost", Float) = 1.25
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="MortarGlow" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment GlowFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float _Boost;

            fixed4 GlowFrag(v2f i) : SV_Target
            {
                fixed4 color = SpriteFrag(i);
                color.rgb *= _Boost;
                return color;
            }
            ENDCG
        }
    }
}
```

In `Runtime/Resources/Mortar/AsciiMask.shader`, add this SubShader between the stroke and the transparent ones:

```hlsl
    SubShader
    {
        Tags { "RenderType"="MortarGlow" "Queue"="Transparent" }
        Cull Off ZWrite Off Blend Off
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment FragGlow
            #pragma target 2.0
            #include "Mask.cginc"
            ENDCG
        }
    }
```

In `Runtime/Ascii/MaskMaterials.cs`, add after the stroke members:

```csharp
        public const string GlowShaderPath = "Mortar/GlowSprite";

        private static Material? _glow;

        public static Material? Glow
        {
            get
            {
                if (_glow == null)
                {
                    _glow = Create(GlowShaderPath);
                }
                return _glow;
            }
        }
```

- [ ] **Step 5: Run bloom in the filter**

Add to `AsciiSettings` after `StrokeSpeed`:

```csharp
        public const float BloomThreshold = 0.6f;
        public const float BloomStrength = 0.8f;
        public const float GlowBoost = 1.5f;
```

Add to `AsciiMaterial.Create` after `_StrokeSpeed`:

```csharp
            material.SetFloat("_BloomThreshold", AsciiSettings.BloomThreshold);
            material.SetFloat("_BloomStrength", AsciiSettings.BloomStrength);
            material.SetFloat("_GlowBoost", AsciiSettings.GlowBoost);
```

In `Runtime/Ascii/AsciiFilter.cs`, add the pass constants and id:

```csharp
        private const int BloomAcrossPass = 2;
        private const int BloomDownPass = 3;
```

```csharp
        private static readonly int BloomTexId = Shader.PropertyToID("_BloomTex");
```

Replace `OnRenderImage` and add `Bloom`:

```csharp
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_material == null || _mask == null || !IsOn)
            {
                Graphics.Blit(source, destination);
                return;
            }
            var grid = new CellGrid(source.width, source.height);
            _material.SetVector(CellCountId, new Vector4(grid.Columns, grid.Rows, 0f, 0f));
            _material.SetTexture(MaskTexId, _mask.Render(_camera, grid));
            ApplyLights(_material, source.width, source.height);
            var cells = CellTexture(grid);
            Graphics.Blit(source, cells, _material, DownsamplePass);
            var bloom = Bloom(_material, cells, grid);
            _material.SetTexture(CellTexId, cells);
            _material.SetTexture(BloomTexId, bloom);
            Graphics.Blit(source, destination, _material, GlyphPass);
            RenderTexture.ReleaseTemporary(bloom);
            RenderTexture.ReleaseTemporary(cells);
        }

        private static RenderTexture Bloom(Material material, RenderTexture cells, CellGrid grid)
        {
            var across = CellTexture(grid);
            var down = CellTexture(grid);
            Graphics.Blit(cells, across, material, BloomAcrossPass);
            Graphics.Blit(across, down, material, BloomDownPass);
            RenderTexture.ReleaseTemporary(across);
            return down;
        }
```

- [ ] **Step 6: Make ground items glow**

In `Runtime/Views/GroundItemViews.cs`, `Sync`, after `view.transform.localScale = ...`:

```csharp
                var glow = MaskMaterials.Glow;
                if (glow != null)
                {
                    view.sharedMaterial = glow;
                }
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode` and `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: all green.

- [ ] **Step 8: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Resources/Mortar unity/com.mortar.unity/Runtime/Ascii unity/com.mortar.unity/Runtime/Views/GroundItemViews.cs unity/com.mortar.unity/Tests/Runtime/AsciiFilterTests.cs
git status --short
git commit -m "bloom bright cells and glowing loot"
```

- [ ] **Step 9: World review with the user (controller)**

Run `pwsh -File tools/unity/test.ps1 -Platform PlayMode -Filter ArtPreviewCapture` and show the user `out/art-preview-town.png`, `out/art-preview-cathedral.png` and their `-raw` versions. Ask the user to play (Unity Play on `Assets/Scenes/Game.unity`, Game view at 1920×1080): walk the town grass, take the stairs, fight by the light, pick up loot, press F9 to compare.

While playing, ask the user to open Window > Analysis > Profiler with the GPU Usage module and note the image-effect time for the camera; the budget is about 1 ms at 1080p. Record it in the ledger.

Tune only `AsciiSettings` values (thresholds, light radius, stroke speed, bloom) from the user's feedback; re-run both Unity suites after any change and commit as `tune the ascii look`. Move on when the user approves.

### Task 6b: Trampled stroke grass (controller follow-up)

**Files:**
- Modify: `docs/superpowers/specs/2026-10-15-ascii-look-design.md` (already amended)
- Create: `Runtime/Ascii/AsciiTrample.cs`, `Runtime/Ascii/GrassTrampers.cs`, `Runtime/Resources/Mortar/AsciiTrample.shader`, `Trample.cginc`
- Modify: `AsciiFilter.cs`, `AsciiSettings.cs`, `AsciiMaterial.cs`, `Glyph.cginc`, `Ascii.shader` (if sharing material), `MortarApp.cs`
- Test: `Tests/Runtime/AsciiFilterTests.cs`

**Interfaces:**
- Produces: persistent cell-grid trample RT; `AsciiFilter.Tramplers`; decay half-life ~5s; grass `WindSlot` lerps to flat `-` by trample; water unchanged; living actors + town NPC props stamp each frame.

Steps: settings + shader decay/stamp; wire filter; `GrassTrampers.Collect` from actors + NPC props; MortarApp fills list in `RefreshLights`; PlayMode tests (stamp marks field, flatten thins grass ink vs windy, decay clears after long step); commit `trample grass under living walkers`.

---

## Phase 3: Actors

### Task 7: Half-resolution bake, compressed sheets and the picking check

**Files:**
- Modify: `tools/art/mortar_art/bake_geometry.py`, `tools/art/tests/test_bake_geometry.py`, `tools/art/tests/test_sheets.py`
- Modify: `unity/com.mortar.unity/Runtime/Art/ActorClip.cs`, `unity/com.mortar.unity/Editor/ArtImport.cs`
- Test: `unity/com.mortar.unity/Tests/Runtime/TestClips.cs`, `BakedActorTests.cs`; `Tests/Editor/ArtImportTests.cs`

**Interfaces:**
- Consumes: the 3D bake modules and `ActorClip` as committed (c787fb9).
- Produces: `bake_geometry` constants `FRAME_WIDTH = 104`, `FRAME_HEIGHT = 104`, `TILE_PIXELS = 64`, `PIVOT_X = 52`, `PIVOT_FROM_BOTTOM = 24` (`RENDER_SCALE` stays 2); `ActorClip.PixelsPerUnit = 64f`; sheets import as DXT5, readable; `TestClips.InstallOutlined(SpriteCatalog catalog, string spriteSet, bool compressed = false) -> Texture2D`.

- [ ] **Step 1: Change the geometry tests to half resolution**

In `tools/art/tests/test_bake_geometry.py`, replace the grid-step parameters, the camera test and the render-size test:

```python
@pytest.mark.parametrize(
    ("dx", "dy", "expected"),
    [(1, 0, (32, 16)), (0, 1, (-32, 16)), (1, 1, (0, 32)), (-1, -1, (0, -32)), (-1, 0, (-32, -16))],
)
```

```python
def test_the_camera_keeps_64_pixels_per_tile_with_the_pivot_shift():
    assert geo.ortho_scale() == pytest.approx(104 / 64 * math.sqrt(2))
    assert geo.camera_shift() == pytest.approx((0.0, 28 / 104))


def test_renders_are_twice_the_frame_size():
    assert geo.render_size() == (208, 208)
```

In `tools/art/tests/test_sheets.py`, change `match="416x416"` to `match="208x208"`.

- [ ] **Step 2: Run them to verify they fail**

Run (from `tools/art`): `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider tests/test_bake_geometry.py tests/test_sheets.py`
Expected: FAIL (grid steps are (64, 32), render size (416, 416)).

- [ ] **Step 3: Halve the bake constants**

In `tools/art/mortar_art/bake_geometry.py`:

```python
FRAME_WIDTH = 104
FRAME_HEIGHT = 104
RENDER_SCALE = 2
TILE_PIXELS = 64
PIVOT_X = 52
PIVOT_FROM_BOTTOM = 24
```

- [ ] **Step 4: Run the full pytest suite**

Run: `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider`
Expected: all pass, including the Blender smoke test (it reads the constants).

- [ ] **Step 5: Write the failing compressed picking test**

In `Tests/Runtime/TestClips.cs`, replace `InstallOutlined`:

```csharp
        public Texture2D InstallOutlined(SpriteCatalog catalog, string spriteSet, bool compressed = false)
        {
            var texture = NewTexture(OutlinedWidth, OutlinedHeight * 8);
            var pixels = texture.GetPixels32();
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var inside = x >= OutlinedWidth / 4 && x < OutlinedWidth * 3 / 4
                        && y % OutlinedHeight >= OutlinedHeight / 4 && y % OutlinedHeight < OutlinedHeight * 3 / 4;
                    pixels[y * texture.width + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            if (compressed)
            {
                texture.Compress(false);
            }
            var json = Json(1, 8, true, 0, OutlinedWidth, OutlinedHeight, OutlinedPivotX, OutlinedPivotY);
            var idle = new ActorClip(SpriteKeys.Clip(spriteSet, ClipNames.Idle), texture, ClipInfo.FromJson(json));
            catalog.SetClips(new List<ActorClip>(catalog.Clips) { idle });
            return texture;
        }
```

Add to `Tests/Runtime/BakedActorTests.cs`:

```csharp
        [Test]
        public void CompressedSheetsKeepOutlinePicking()
        {
            Object.Destroy(_catalog);
            _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
            var sheet = _clips.InstallOutlined(_catalog, "warrior", compressed: true);
            _actors.Sync(World, _space, _catalog);
            var view = _actors.Find(Actor.PlayerId)!;
            view.Render(0f, Time.time);

            Assert.AreEqual(TextureFormat.DXT5, sheet.format);
            Assert.IsFalse(view.Covers(PixelPoint(view, 2, 2)), "a transparent frame corner should not pick the actor");
            Assert.IsTrue(
                view.Covers(PixelPoint(view, TestClips.OutlinedWidth / 2, TestClips.OutlinedHeight / 2)),
                "the opaque centre should pick the actor");
        }
```

Add to `Tests/Editor/ArtImportTests.cs` (add `using System.Linq;`):

```csharp
        [Test]
        public void BakedSheetsImportCompressedAndReadable()
        {
            var sheets = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/actors" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => ArtImport.IsSheet(path))
                .ToList();
            if (sheets.Count == 0)
            {
                Assert.Ignore("no baked sheets imported");
            }
            foreach (var path in sheets)
            {
                var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.AreEqual(TextureFormat.DXT5, sheet.format, path);
                Assert.IsTrue(sheet.isReadable, path);
            }
        }
```

- [ ] **Step 6: Run the Unity tests to see where they stand**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode -Filter BakedActorTests` and `pwsh -File tools/unity/test.ps1 -Platform EditMode -Filter ArtImportTests`
Expected: `BakedSheetsImportCompressedAndReadable` FAILS on the uncommitted warrior sheets (format `RGBA32`). `CompressedSheetsKeepOutlinePicking` may already pass: it tests whether tight sprite meshes work on DXT5. Note the result.

- [ ] **Step 7: Import sheets compressed at 64 pixels per unit**

In `Runtime/Art/ActorClip.cs`: `public const float PixelsPerUnit = 64f;`

In `Editor/ArtImport.cs`: change `GetVersion` to return `2`, and in `ConfigureSheet` replace the compression line:

```csharp
            importer.textureCompression = TextureImporterCompression.Compressed;
```

- [ ] **Step 8: Run the Unity suites**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode` and `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: all green. `BakedSheetsImportCompressedAndReadable` passes (the old 208 px warrior sheets re-import as DXT5; they are re-baked in Task 8).

- [ ] **Step 9 (only if `CompressedSheetsKeepOutlinePicking` fails): pick by alpha on readable sheets**

In `Runtime/Art/ActorClip.cs`, `CreateSprite`, use `SpriteMeshType.FullRect`. In `Runtime/Views/SpriteObjects.cs`, in `Covers`, replace the triangle loop and its `return false;` with an alpha check for readable textures, keeping triangles for the rest:

```csharp
            if (sprite.texture != null && sprite.texture.isReadable)
            {
                return AlphaAt(sprite, spot) >= 0.5f;
            }
            var corners = CornersOf(sprite);
            for (var i = 0; i + 2 < corners.Length; i += 3)
            {
                if (InTriangle(spot, corners[i], corners[i + 1], corners[i + 2]))
                {
                    return true;
                }
            }
            return false;
```

```csharp
        private static float AlphaAt(Sprite sprite, Vector2 local)
        {
            var rect = sprite.textureRect;
            var pixel = local * sprite.pixelsPerUnit + sprite.pivot;
            if (pixel.x < 0f || pixel.y < 0f || pixel.x > rect.width || pixel.y > rect.height)
            {
                return 0f;
            }
            var texture = sprite.texture;
            return texture.GetPixelBilinear((rect.x + pixel.x) / texture.width, (rect.y + pixel.y) / texture.height).a;
        }
```

Re-run both suites; all must pass, including `BakedActorsArePickedByTheirOutlineNotTheirFrame` and `MonsterPickingTests`. Skip this step when Step 6 or Step 8 showed the compressed test passing.

- [ ] **Step 10: Commit**

```powershell
git add tools/art/mortar_art/bake_geometry.py tools/art/tests/test_bake_geometry.py tools/art/tests/test_sheets.py unity/com.mortar.unity/Runtime/Art/ActorClip.cs unity/com.mortar.unity/Editor/ArtImport.cs unity/com.mortar.unity/Tests/Runtime/TestClips.cs unity/com.mortar.unity/Tests/Runtime/BakedActorTests.cs unity/com.mortar.unity/Tests/Editor/ArtImportTests.cs
git status --short
git commit -m "bake actors at half resolution into compressed sheets"
```

Add `unity/com.mortar.unity/Runtime/Views/SpriteObjects.cs` to the `git add` if Step 9 ran. Do not stage the uncommitted warrior art.

---

### Task 8: Re-bake the warrior and bake the other nine actors (controller and user)

**Files:**
- Modify: `tools/art/actors.json`, `unity/Mortar.Sample/Assets/Art/SpriteCatalog.asset`
- Create: `unity/Mortar.Sample/Assets/Art/actors/<actor>/{idle,walk,attack,hit,death}.png` and `.json` with `.meta` files (NPCs: `idle` only), `tools/art/tests/test_models.py`

**Interfaces:**
- Consumes: Task 7's half-resolution bake; `actors.required_actor_ids()`, `actors.hash_models()`, `actors.MODELS_ROOT`, `actors.LICENCE_NAME` (existing).
- Produces: baked sheets for all ten actors; catalog with 43 sprites and 38 clips.

- [ ] **Step 1: Write the coverage tests**

`tools/art/tests/test_models.py`:

```python
from mortar_art import actors


def test_every_actor_in_content_has_a_bake_entry():
    manifest = actors.load_manifest()

    assert sorted(actors.required_actor_ids() - set(manifest.actors)) == []


def test_committed_models_match_their_recorded_hashes():
    manifest = actors.load_manifest()

    assert actors.hash_models() == manifest.files


def test_every_model_pack_has_a_licence():
    packs = [path for path in actors.MODELS_ROOT.iterdir() if path.is_dir()]

    assert packs
    assert [pack.name for pack in packs if not (pack / actors.LICENCE_NAME).is_file()] == []
```

Run: `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider tests/test_models.py`
Expected: FAIL on the first test, listing the nine missing ids.

- [ ] **Step 2: Re-bake the warrior and check it**

Run (from `tools/art`): `.venv\Scripts\python bake.py --only warrior --force --sheet`
Expected: `[BAKE] 1/1 warrior`, five `Wrote unity/Mortar.Sample/Assets/Art/actors/warrior/<anim>.png` lines, and `Wrote tools/art/out/sheets/actors-baked.png`.

Open the review sheet. Check that direction 0 (South) faces screen down-left and direction 6 (East) faces down-right, that the figure fills about two-thirds of the 104 px frame height, that the feet are not cut off, and that the attack hit frame shows the blade extended.

- [ ] **Step 3: Add the nine entries**

For each actor run `.venv\Scripts\python bake.py --list-clips <files>` and add an entry to `actors.json`:
- `skeleton`: `kaykit-skeletons/Skeleton_Minion.glb`.
- `gravewarden`: `kaykit-skeletons/Skeleton_Warrior.glb`, `scale` about 0.78 (1.3 × the warrior's 0.6), `tint` about `[0.75, 0.7, 0.8]`.
- `zombie`: `quaternius/Zombie.glb`.
- `imp`: `quaternius/Demon.glb`, smaller than the warrior.
- `imp-chieftain`: `quaternius/BlueDemon.glb`, about the warrior's size.
- `carrion-bat`: `quaternius/Bat.glb`; use `BatArmature|Bat_Flying` for both idle and walk.
- `smith`, `healer`, `sage`: `kaykit-adventurers/Barbarian.glb`, `Rogue_Hooded.glb` and `Mage.glb`, each with `"npc": true` and an idle clip.

KayKit characters embed their clips (`Idle`, `Walking_A`, `1H_Melee_Attack_Chop`, `Hit_A`, `Death_A`). Quaternius clips are prefixed, such as `CharacterArmature|Idle`; use exactly what `--list-clips` prints. Pick `attackHitSeconds` where the strike lands, and `hide` any stray weapon meshes. Run `test_models.py` again; it must pass.

- [ ] **Step 4: Bake and review everything**

Run: `.venv\Scripts\python bake.py --sheet`
Expected: nine actors baked (the warrior is skipped) and one review sheet. Apply the Step 2 checks to every actor, and check relative sizes: the bat and imp are smaller than the warrior, the gravewarden larger. Iterate per actor with `--only <id> --force`.

- [ ] **Step 5: Rebuild the catalog and run every check**

With the user's editor closed:

Run: `pwsh -File tools/unity/run-method.ps1 -Method Mortar.Unity.EditorTools.SpriteCatalogBuilder.RebuildFromCommandLine`
Expected: the log under `out/` has `Mortar: sprite catalog has 43 sprites and 38 clips` (five clips for each of the seven fighters plus three NPC idles).

Run both Unity suites and pytest. Expected: all green; `BakedSheetsImportCompressedAndReadable` now checks 38 sheets.

- [ ] **Step 6: Show the user and get approval**

Run `pwsh -File tools/unity/test.ps1 -Platform PlayMode -Filter ArtPreviewCapture`. Show the review sheet and the filtered previews, then ask the user to play the town and cathedral levels 1–4: walk, attack, take hits, die. Iterate on Steps 3–4 until the user approves.

- [ ] **Step 7: Commit the baked actors**

```powershell
git add tools/art/actors.json tools/art/tests/test_models.py unity/Mortar.Sample/Assets/Art/actors unity/Mortar.Sample/Assets/Art/SpriteCatalog.asset
git status --short
git commit -m "bake all actors at half resolution"
```

Check that `git status --short` shows no `.env`, `out/` or `Library/` paths before committing.

---

## Phase 4: Interface

### Task 9: Monospace panels

**Files:**
- Modify: `unity/com.mortar.unity/Runtime/UI/Ui.cs`, `UiRoot.cs`, `FloatingTexts.cs`, `InventoryScreen.cs`, `CharacterSheet.cs`
- Test: `unity/com.mortar.unity/Tests/Editor/UiTests.cs` (new), `Tests/Runtime/HudTests.cs` (add)

**Interfaces:**
- Consumes: `Resources/Mortar/DejaVuSansMono.ttf` (Task 1).
- Produces: `Ui.FontPath = "Mortar/DejaVuSansMono"`, `Ui.ApplyFont(VisualElement root) -> bool`, `Ui.Box(string name, string? title = null)` (title label named `title`, text `┤ {title} ├`), `Ui.Flat(Button button)`, palette `Ui.PanelColor`, `Ui.TitleBackground`, `Ui.BorderColor`, `Ui.TextColor`, `Ui.SlotColor`, `Ui.EmptyBarColor`.

- [ ] **Step 1: Write the failing tests**

`Tests/Editor/UiTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Mortar.Unity.Tests
{
    public sealed class UiTests
    {
        [Test]
        public void TitledBoxesSetTheTitleIntoTheTopBorder()
        {
            var box = Ui.Box("inventory", "Inventory");

            var title = box.Q<Label>("title");
            Assert.IsNotNull(title);
            Assert.AreEqual("┤ Inventory ├", title.text);
            Assert.AreEqual(1f, box.style.borderTopWidth.value);
        }

        [Test]
        public void UntitledBoxesHaveNoTitle()
        {
            Assert.IsNull(Ui.Box("menu-box").Q<Label>("title"));
        }

        [Test]
        public void TheMonospaceFontApplies()
        {
            var root = new VisualElement();

            Assert.IsTrue(Ui.ApplyFont(root));
            Assert.IsNotNull(root.style.unityFontDefinition.value.font);
        }
    }
}
```

Add to `Tests/Runtime/HudTests.cs`:

```csharp
        [Test]
        public void TheInterfaceUsesTheMonospaceFont()
        {
            Assert.IsNotNull(_ui.Root.style.unityFontDefinition.value.font);
        }

        [Test]
        public void FloatingTextsAreNotBold()
        {
            var texts = new FloatingTexts(_ui.Overlay);
            texts.Spawn("12", Vector3.zero, Color.white, 0f);

            Assert.AreNotEqual(FontStyle.Bold, _ui.Overlay.Q<Label>().style.unityFontStyleAndWeight.value);
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode -Filter UiTests`
Expected: FAIL to compile (`Ui.Box` has no title parameter, `Ui.ApplyFont` missing).

- [ ] **Step 3: Restyle `Ui`**

Replace `Runtime/UI/Ui.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public static class Ui
    {
        public const string FontPath = "Mortar/DejaVuSansMono";

        public static readonly Color PanelColor = new Color(0.02f, 0.02f, 0.02f, 0.92f);
        public static readonly Color TitleBackground = new Color(0.02f, 0.02f, 0.02f, 1f);
        public static readonly Color BorderColor = new Color(0.62f, 0.62f, 0.6f);
        public static readonly Color TextColor = new Color(0.86f, 0.86f, 0.82f);
        public static readonly Color MagicColor = new Color(0.45f, 0.6f, 1f);
        public static readonly Color HeldColor = new Color(0.95f, 0.8f, 0.3f);
        public static readonly Color SlotColor = new Color(0.06f, 0.06f, 0.06f, 0.95f);
        public static readonly Color EmptyBarColor = new Color(0.3f, 0.3f, 0.3f);

        public static bool ApplyFont(VisualElement root)
        {
            var font = Resources.Load<Font>(FontPath);
            if (font == null)
            {
                Debug.LogWarning($"Mortar: interface font '{FontPath}' did not load; using the default font");
                return false;
            }
            root.style.unityFontDefinition = FontDefinition.FromFont(font);
            return true;
        }

        public static VisualElement Box(string name, string? title = null)
        {
            var box = new VisualElement { name = name };
            box.style.backgroundColor = PanelColor;
            Border(box, 1f, BorderColor);
            Padding(box, 10f);
            if (title != null)
            {
                var label = Text($"┤ {title} ├", 14);
                label.name = "title";
                label.style.backgroundColor = TitleBackground;
                Place(label, 12f, -10f, null, null);
                box.style.paddingTop = 16f;
                box.Add(label);
            }
            return box;
        }

        public static Label Text(string text, int size = 14)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.style.color = TextColor;
            label.style.fontSize = size;
            return label;
        }

        public static Button MenuButton(string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.style.minHeight = 44f;
            button.style.minWidth = 220f;
            button.style.fontSize = 16;
            button.style.marginTop = 4f;
            button.style.marginBottom = 4f;
            Flat(button);
            return button;
        }

        public static void Flat(Button button)
        {
            button.style.color = TextColor;
            button.style.backgroundColor = SlotColor;
            Border(button, 1f, BorderColor);
            Radius(button, 0f);
        }

        public static Image Icon(Sprite? sprite, float size)
        {
            var image = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.style.width = size;
            image.style.height = size;
            return image;
        }

        public static void Border(VisualElement element, float width, Color color)
        {
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
        }

        public static void Padding(VisualElement element, float amount)
        {
            element.style.paddingTop = amount;
            element.style.paddingBottom = amount;
            element.style.paddingLeft = amount;
            element.style.paddingRight = amount;
        }

        public static void Radius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }

        public static void Fill(VisualElement element) => Place(element, 0f, 0f, 0f, 0f);

        public static void Place(VisualElement element, float? left, float? top, float? right, float? bottom)
        {
            element.style.position = Position.Absolute;
            if (left.HasValue) element.style.left = left.Value;
            if (top.HasValue) element.style.top = top.Value;
            if (right.HasValue) element.style.right = right.Value;
            if (bottom.HasValue) element.style.bottom = bottom.Value;
        }

        public static void Show(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        public static bool IsShown(VisualElement element) => element.style.display.value != DisplayStyle.None;
    }
}
```

- [ ] **Step 4: Apply the font, titles and plain popups**

In `Runtime/UI/UiRoot.cs`, `BuildLayers`, add first:

```csharp
            Ui.ApplyFont(Root);
```

In `Runtime/UI/InventoryScreen.cs`: `_panel = Ui.Box("inventory", "Inventory");`

In `Runtime/UI/CharacterSheet.cs`: `_panel = Ui.Box("character", "Character");`, and in `StatRow` call `Ui.Flat(plus);` after `plus.style.minHeight = 44f;`.

In `Runtime/UI/FloatingTexts.cs`, `Spawn`: delete the line `label.style.unityFontStyleAndWeight = FontStyle.Bold;`.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode` and `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: all green.

- [ ] **Step 6: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/UI/Ui.cs unity/com.mortar.unity/Runtime/UI/UiRoot.cs unity/com.mortar.unity/Runtime/UI/FloatingTexts.cs unity/com.mortar.unity/Runtime/UI/InventoryScreen.cs unity/com.mortar.unity/Runtime/UI/CharacterSheet.cs unity/com.mortar.unity/Tests/Editor/UiTests.cs unity/com.mortar.unity/Tests/Editor/UiTests.cs.meta unity/com.mortar.unity/Tests/Runtime/HudTests.cs
git status --short
git commit -m "restyle the interface as monospace panels"
```

---

### Task 10: Text bars for life and mana

**Files:**
- Create: `unity/com.mortar.unity/Runtime/UI/TextBar.cs`
- Modify: `unity/com.mortar.unity/Runtime/UI/HudView.cs`
- Delete: `unity/com.mortar.unity/Runtime/UI/Orb.cs` and `Orb.cs.meta`
- Test: `unity/com.mortar.unity/Tests/Editor/TextBarTests.cs` (new), `Tests/Runtime/HudTests.cs` (modify)

**Interfaces:**
- Consumes: `Ui` palette and `Ui.Text` (Task 9).
- Produces: `TextBar(string name, string caption, Color fill)` with `Cells = 14`, `Element`, `Text` (`"cur/max"`), `BarText` (14 block characters), `Set(int current, int max)`, `static Filled(int current, int max) -> int`; `HudView.Life` and `HudView.Mana` are `TextBar`.

- [ ] **Step 1: Write the failing tests**

`Tests/Editor/TextBarTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class TextBarTests
    {
        [TestCase(0, 100, 0)]
        [TestCase(1, 100, 1)]
        [TestCase(50, 100, 7)]
        [TestCase(96, 129, 11)]
        [TestCase(100, 100, 14)]
        [TestCase(150, 100, 14)]
        [TestCase(-5, 100, 0)]
        [TestCase(5, 0, 0)]
        public void FilledCellsRoundUpSoAnyLifeShows(int current, int max, int expected)
        {
            Assert.AreEqual(expected, TextBar.Filled(current, max));
        }

        [Test]
        public void BarsShowBlocksAndNumbers()
        {
            var bar = new TextBar("life", "HP", Color.red);

            bar.Set(96, 129);

            Assert.AreEqual("96/129", bar.Text);
            Assert.AreEqual(new string('█', 11) + new string('░', 3), bar.BarText);
        }

        [Test]
        public void NegativeLifeShowsAsZero()
        {
            var bar = new TextBar("life", "HP", Color.red);

            bar.Set(-3, 50);

            Assert.AreEqual("0/50", bar.Text);
        }
    }
}
```

In `Tests/Runtime/HudTests.cs`, replace `OrbsShowLifeAndMana`:

```csharp
        [Test]
        public void BarsShowLifeAndMana()
        {
            _hud.Refresh(_world, new string[0], null);

            var player = _world.Player;
            Assert.AreEqual($"{player.Life}/{player.Profile.MaxLife}", _hud.Life.Text);
            Assert.AreEqual($"{player.Mana}/{player.Profile.MaxMana}", _hud.Mana.Text);
            Assert.AreEqual(new string('█', TextBar.Cells), _hud.Life.BarText, "a new character starts at full life");
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode -Filter TextBarTests`
Expected: FAIL to compile (`TextBar` missing).

- [ ] **Step 3: Write `TextBar`**

`Runtime/UI/TextBar.cs`:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class TextBar
    {
        public const int Cells = 14;
        public const int FontSize = 15;

        private readonly Label _filled;
        private readonly Label _empty;
        private readonly Label _numbers;
        private int _current = int.MinValue;
        private int _max = int.MinValue;

        public TextBar(string name, string caption, Color fill)
        {
            Element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            Element.style.flexDirection = FlexDirection.Row;
            Element.style.backgroundColor = Ui.PanelColor;
            Ui.Border(Element, 1f, Ui.BorderColor);
            Ui.Padding(Element, 4f);
            Element.Add(Ui.Text(caption + " ", FontSize));
            _filled = Tight(Ui.Text("", FontSize), fill);
            _empty = Tight(Ui.Text("", FontSize), Ui.EmptyBarColor);
            _numbers = Ui.Text("", FontSize);
            _numbers.style.marginLeft = 8f;
            Element.Add(_filled);
            Element.Add(_empty);
            Element.Add(_numbers);
        }

        public VisualElement Element { get; }

        public string Text => _numbers.text;

        public string BarText => _filled.text + _empty.text;

        public void Set(int current, int max)
        {
            var shown = Mathf.Max(0, current);
            if (shown == _current && max == _max)
            {
                return;
            }
            _current = shown;
            _max = max;
            var filled = Filled(shown, max);
            _filled.text = new string('█', filled);
            _empty.text = new string('░', Cells - filled);
            _numbers.text = $"{shown}/{max}";
        }

        public static int Filled(int current, int max) =>
            max <= 0 ? 0 : Mathf.Clamp(Mathf.CeilToInt((float)current * Cells / max), 0, Cells);

        private static Label Tight(Label label, Color color)
        {
            label.style.color = color;
            label.style.marginLeft = 0f;
            label.style.marginRight = 0f;
            label.style.paddingLeft = 0f;
            label.style.paddingRight = 0f;
            return label;
        }
    }
}
```

- [ ] **Step 4: Use text bars in the HUD and remove `Orb`**

In `Runtime/UI/HudView.cs`:

```csharp
            Life = new TextBar("life", "HP", new Color(0.85f, 0.15f, 0.15f));
            Mana = new TextBar("mana", "MP", new Color(0.3f, 0.45f, 1f));
```

```csharp
        public TextBar Life { get; }

        public TextBar Mana { get; }
```

In `BuildCenter`, change the experience track colours to the new palette:

```csharp
            track.style.backgroundColor = Ui.SlotColor;
            _experienceFill.style.height = Length.Percent(100f);
            _experienceFill.style.backgroundColor = new Color(0.8f, 0.7f, 0.3f);
```

`BuildBottomBar` keeps adding `Life.Element` and `Mana.Element`. Then delete `Runtime/UI/Orb.cs` and `Runtime/UI/Orb.cs.meta`.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode` and `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: all green, including `MortarAppTests.NewGameShowsTheTownAndHud` (it reads `Hud.Life.Text`).

- [ ] **Step 6: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/UI/TextBar.cs unity/com.mortar.unity/Runtime/UI/TextBar.cs.meta unity/com.mortar.unity/Runtime/UI/HudView.cs unity/com.mortar.unity/Runtime/UI/Orb.cs unity/com.mortar.unity/Runtime/UI/Orb.cs.meta unity/com.mortar.unity/Tests/Editor/TextBarTests.cs unity/com.mortar.unity/Tests/Editor/TextBarTests.cs.meta unity/com.mortar.unity/Tests/Runtime/HudTests.cs
git status --short
git commit -m "show life and mana as text bars"
```

(`git add` on the deleted `Orb.cs` paths stages their removal.)

---

## Phase 5: Icons

### Task 11: The glyph icon converter

**Files:**
- Create: `tools/art/mortar_art/glyph_icons.py`
- Modify: `tools/art/glyphs.py`
- Test: `tools/art/tests/test_glyph_icons.py` (new), `tools/art/tests/test_glyphs_cli.py` (add)

**Interfaces:**
- Consumes: `mortar_art.glyphs` (`load_font`, `render_glyph`, `glyph_chars`, `ramp_slot`, `edge_slot`, `LUMA`) from Task 1; `imaging.contact_sheet`; `jobs.ART_ROOT`.
- Produces: `glyph_icons.COLUMNS_PER_CELL = 6`, `ROWS_PER_CELL = 4`, `ICON_SCALE = 2`; `item_cells(items_root: Path = CONTENT_ITEMS) -> dict[str, int]`; `grid_for(cells: int) -> tuple[int, int]`; `cell_colours(icon, columns, rows) -> list[list[Colour]]`; `choose_slots(colours) -> list[list[int]]`; `tint(colour) -> tuple[int, int, int]`; `convert_icon(icon, cells, glyph_images) -> Image.Image`; `convert_all(source: Path, target: Path, cells: dict[str, int]) -> list[Path]`. CLI: `glyphs.py icons` writes `Assets/Art/glyph-icons/<id>.png` and `tools/art/out/sheets/glyph-icons.png`.

- [ ] **Step 1: Write the failing tests**

`tools/art/tests/test_glyph_icons.py`:

```python
import json

import pytest
from PIL import Image

from mortar_art import glyph_icons, glyphs

WHITE = (1.0, 1.0, 1.0, 1.0)
CLEAR = (0.0, 0.0, 0.0, 0.0)


@pytest.fixture(scope="module")
def glyph_images():
    chars = glyphs.glyph_chars(glyphs.load_font())
    font = glyphs.load_font(glyph_icons.ICON_SCALE)
    return [glyphs.render_glyph(char, font, glyph_icons.ICON_SCALE) for char in chars]


def test_each_covered_cell_gets_six_by_four_characters():
    assert glyph_icons.grid_for(1) == (6, 4)
    assert glyph_icons.grid_for(2) == (12, 8)


def test_an_icon_covers_at_least_one_cell():
    with pytest.raises(ValueError, match="at least one cell"):
        glyph_icons.grid_for(0)


def test_icons_cover_the_smaller_side_of_their_item(tmp_path):
    (tmp_path / "blade.json").write_text(json.dumps({"icon": "blade", "size": {"width": 1, "height": 3}}))
    (tmp_path / "vest.json").write_text(json.dumps({"icon": "vest", "size": {"width": 2, "height": 3}}))

    assert glyph_icons.item_cells(tmp_path) == {"blade": 1, "vest": 2}


def test_the_real_items_all_have_cells():
    cells = glyph_icons.item_cells()

    assert cells["broad-blade"] == 1
    assert all(value >= 1 for value in cells.values())


def test_a_converted_icon_matches_its_grid_size(glyph_images):
    icon = Image.new("RGBA", (64, 64), (200, 40, 40, 255))

    picture = glyph_icons.convert_icon(icon, 1, glyph_images)

    assert picture.size == (6 * 12, 4 * 20)


def test_a_transparent_icon_converts_to_nothing(glyph_images):
    picture = glyph_icons.convert_icon(Image.new("RGBA", (64, 64), (0, 0, 0, 0)), 1, glyph_images)

    assert picture.getchannel("A").getbbox() is None


def test_glyphs_keep_the_icon_colour_on_a_clear_background(glyph_images):
    picture = glyph_icons.convert_icon(Image.new("RGBA", (64, 64), (220, 60, 30, 255)), 1, glyph_images)
    inked = [pixel for pixel in picture.get_flattened_data() if pixel[3] > 200]

    assert inked
    assert all(red > green > blue for red, green, blue, _ in inked)
    assert any(pixel[3] == 0 for pixel in picture.get_flattened_data())


def test_conversion_is_deterministic(glyph_images):
    icon = Image.new("RGBA", (64, 64), (90, 160, 220, 255))

    first = glyph_icons.convert_icon(icon, 1, glyph_images).tobytes()

    assert glyph_icons.convert_icon(icon, 1, glyph_images).tobytes() == first


def test_an_opaque_edge_beside_clear_space_draws_a_bar():
    colours = [[WHITE, WHITE, WHITE, CLEAR, CLEAR, CLEAR] for _ in range(4)]

    slots = glyph_icons.choose_slots(colours)

    assert slots[1][2] == glyphs.FIRST_EDGE_SLOT + 2
    assert slots[1][1] == glyphs.RAMP_SIZE - 1
    assert slots[1][3] == 0


def test_tint_keeps_hue_and_lifts_dim_colours():
    red, green, blue = glyph_icons.tint((0.4, 0.2, 0.1, 1.0))

    assert red > green > blue
    assert red > round(0.4 * 255)
```

Add to `tools/art/tests/test_glyphs_cli.py`:

```python
def test_icons_writes_glyph_icons_and_a_sheet(tmp_path, monkeypatch):
    art = tmp_path / "Art"
    (art / "icons").mkdir(parents=True)
    Image.new("RGBA", (64, 64), (200, 200, 40, 255)).save(art / "icons" / "lamp.png")
    monkeypatch.setattr(cli, "ART_ROOT", art)
    monkeypatch.setattr(cli, "SHEET_PATH", tmp_path / "sheets" / "glyph-icons.png")
    monkeypatch.setattr(cli, "REPO_ROOT", tmp_path)

    assert cli.main(["icons"]) == 0

    with Image.open(art / "glyph-icons" / "lamp.png") as picture:
        assert picture.size == (72, 80)
    assert (tmp_path / "sheets" / "glyph-icons.png").is_file()
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider tests/test_glyph_icons.py tests/test_glyphs_cli.py`
Expected: FAIL with `ImportError` (no `glyph_icons`) and the CLI rejecting `icons`.

- [ ] **Step 3: Write `mortar_art/glyph_icons.py`**

```python
"""Turn painted item icons into coloured glyph pictures with the world filter's rules."""

from __future__ import annotations

import json
from pathlib import Path

from PIL import Image

from . import glyphs
from .config import REPO_ROOT

CONTENT_ITEMS = REPO_ROOT / "content" / "items"
COLUMNS_PER_CELL = 6
ROWS_PER_CELL = 4
ICON_SCALE = 2
SOLID_ALPHA = 0.35
EDGE_THRESHOLD = 0.25
NORMALIZE = 0.5
SATURATION = 0.85

Colour = tuple[float, float, float, float]


def item_cells(items_root: Path = CONTENT_ITEMS) -> dict[str, int]:
    cells: dict[str, int] = {}
    for path in sorted(items_root.glob("*.json")):
        item = json.loads(path.read_text(encoding="utf-8"))
        size = item.get("size", {})
        cells[item["icon"]] = min(int(size.get("width", 1)), int(size.get("height", 1)))
    return cells


def grid_for(cells: int) -> tuple[int, int]:
    if cells < 1:
        raise ValueError("an icon covers at least one cell")
    return COLUMNS_PER_CELL * cells, ROWS_PER_CELL * cells


def cell_colours(icon: Image.Image, columns: int, rows: int) -> list[list[Colour]]:
    small = icon.convert("RGBA").resize((columns, rows), Image.Resampling.BOX)
    pixels = [tuple(channel / 255 for channel in pixel) for pixel in small.get_flattened_data()]
    return [pixels[row * columns : (row + 1) * columns] for row in range(rows)]


def luminance(colour: Colour) -> float:
    return sum(channel * weight for channel, weight in zip(colour[:3], glyphs.LUMA))


def choose_slots(colours: list[list[Colour]]) -> list[list[int]]:
    rows, columns = len(colours), len(colours[0])

    def ink(row: int, column: int) -> float:
        if not (0 <= row < rows and 0 <= column < columns):
            return 0.0
        return luminance(colours[row][column]) * colours[row][column][3]

    slots = []
    for row in range(rows):
        line = []
        for column in range(columns):
            colour = colours[row][column]
            if colour[3] < SOLID_ALPHA:
                line.append(0)
                continue
            gx = (ink(row, column + 1) - ink(row, column - 1)) / 2
            gy = (ink(row - 1, column) - ink(row + 1, column)) / 2
            edge = glyphs.edge_slot(gx, gy, EDGE_THRESHOLD)
            line.append(edge or glyphs.ramp_slot(luminance(colour), 0.0))
        slots.append(line)
    return slots


def tint(colour: Colour) -> tuple[int, int, int]:
    peak = max(colour[0], colour[1], colour[2], 1e-4)
    mixed = [channel + (channel / peak - channel) * NORMALIZE for channel in colour[:3]]
    grey = sum(channel * weight for channel, weight in zip(mixed, glyphs.LUMA))
    red, green, blue = (round(255 * min(1.0, grey + (channel - grey) * SATURATION)) for channel in mixed)
    return red, green, blue


def convert_icon(icon: Image.Image, cells: int, glyph_images: list[Image.Image]) -> Image.Image:
    columns, rows = grid_for(cells)
    colours = cell_colours(icon, columns, rows)
    slots = choose_slots(colours)
    width, height = glyph_images[0].size
    picture = Image.new("RGBA", (columns * width, rows * height), (0, 0, 0, 0))
    for row in range(rows):
        for column in range(columns):
            slot = slots[row][column]
            if slot == 0:
                continue
            glyph = Image.new("RGBA", (width, height), (*tint(colours[row][column]), 255))
            glyph.putalpha(glyph_images[slot])
            picture.paste(glyph, (column * width, row * height))
    return picture


def convert_all(source: Path, target: Path, cells: dict[str, int]) -> list[Path]:
    chars = glyphs.glyph_chars(glyphs.load_font())
    font = glyphs.load_font(ICON_SCALE)
    glyph_images = [glyphs.render_glyph(char, font, ICON_SCALE) for char in chars]
    target.mkdir(parents=True, exist_ok=True)
    written = []
    for path in sorted(source.glob("*.png")):
        with Image.open(path) as icon:
            picture = convert_icon(icon, cells.get(path.stem, 1), glyph_images)
        output = target / path.name
        picture.save(output)
        written.append(output)
    return written
```

- [ ] **Step 4: Add the `icons` command**

Replace `tools/art/glyphs.py`:

```python
"""Build the glyph atlas for Mortar's ASCII world filter and glyph versions of the item icons."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image

from mortar_art import glyph_icons, glyphs
from mortar_art.config import REPO_ROOT
from mortar_art.imaging import contact_sheet
from mortar_art.jobs import ART_ROOT

SHEET_PATH = REPO_ROOT / "tools" / "art" / "out" / "sheets" / "glyph-icons.png"


def parse_args(argv: list[str] | None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("atlas", help="render the 16-slot glyph atlas into the Unity package")
    commands.add_parser("icons", help="convert Assets/Art/icons into Assets/Art/glyph-icons")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        if args.command == "atlas":
            write_atlas(glyphs.ATLAS_PATH)
        else:
            write_icons(ART_ROOT, SHEET_PATH)
    except (OSError, ValueError) as error:
        print(f"[ERROR] {error}", file=sys.stderr)
        return 1
    return 0


def write_atlas(path: Path) -> None:
    font = glyphs.load_font()
    chars = glyphs.glyph_chars(font)
    path.parent.mkdir(parents=True, exist_ok=True)
    glyphs.build_atlas(chars, font).save(path)
    print(f"[GLYPHS] Ramp {''.join(chars[: glyphs.RAMP_SIZE])!r}")
    print(f"[GLYPHS] Wrote {path.relative_to(REPO_ROOT).as_posix()}")


def write_icons(art_root: Path, sheet_path: Path) -> None:
    written = glyph_icons.convert_all(art_root / "icons", art_root / "glyph-icons", glyph_icons.item_cells())
    for path in written:
        print(f"[GLYPHS] Wrote {path.relative_to(REPO_ROOT).as_posix()}")
    if not written:
        print("[GLYPHS] No icons to convert")
        return
    entries = []
    for path in written:
        with Image.open(path) as picture:
            entries.append((path.stem, picture.copy()))
    sheet_path.parent.mkdir(parents=True, exist_ok=True)
    contact_sheet(entries).save(sheet_path)
    print(f"[GLYPHS] Wrote {sheet_path.relative_to(REPO_ROOT).as_posix()}")


if __name__ == "__main__":
    sys.exit(main())
```

`main` passes the module attributes `ART_ROOT` and `SHEET_PATH` at call time, so the CLI test's monkeypatching takes effect.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider`
Expected: all pass.

- [ ] **Step 6: Commit**

```powershell
git add tools/art/glyphs.py tools/art/mortar_art/glyph_icons.py tools/art/tests/test_glyph_icons.py tools/art/tests/test_glyphs_cli.py
git status --short
git commit -m "add the glyph icon converter"
```

---

### Task 12: Glyph icons in the interface, then the icon review

**Files:**
- Modify: `unity/com.mortar.unity/Runtime/Art/SpriteKeys.cs`, `SpriteCatalog.cs`, `Runtime/Views/GroundItemViews.cs`
- Create: `unity/Mortar.Sample/Assets/Art/glyph-icons/*.png` with `.meta` files and `glyph-icons.meta` (controller, after approval)
- Modify: `unity/Mortar.Sample/Assets/Art/SpriteCatalog.asset`
- Test: `unity/com.mortar.unity/Tests/Editor/SpriteCatalogTests.cs` (add)

**Interfaces:**
- Consumes: Task 11's `glyphs.py icons`.
- Produces: `SpriteKeys.GlyphIcon(string id) -> string` (`glyph-icons/{id}`); `SpriteCatalog.Icon(string iconId)` prefers the glyph icon; `SpriteCatalog.WorldIcon(string iconId)` returns the painted icon or the placeholder.

- [ ] **Step 1: Write the failing tests**

Add to `Tests/Editor/SpriteCatalogTests.cs`:

```csharp
        [Test]
        public void IconsPreferTheGlyphVersion()
        {
            var painted = MakeSprite();
            var glyph = MakeSprite();
            _catalog.SetEntries(new[] { new SpriteEntry("icons/iron-knife", painted), new SpriteEntry("glyph-icons/iron-knife", glyph) });

            Assert.AreSame(glyph, _catalog.Icon("iron-knife"));
        }

        [Test]
        public void WorldIconsKeepThePaintedIcon()
        {
            var painted = MakeSprite();
            var glyph = MakeSprite();
            _catalog.SetEntries(new[] { new SpriteEntry("icons/iron-knife", painted), new SpriteEntry("glyph-icons/iron-knife", glyph) });

            Assert.AreSame(painted, _catalog.WorldIcon("iron-knife"));
        }

        [Test]
        public void AMissingGlyphIconFallsBackToThePaintedIconWithoutWarning()
        {
            var painted = MakeSprite();
            _catalog.SetEntries(new[] { new SpriteEntry("icons/iron-knife", painted) });

            Assert.AreSame(painted, _catalog.Icon("iron-knife"));
            LogAssert.NoUnexpectedReceived();
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode -Filter SpriteCatalogTests`
Expected: FAIL to compile (`WorldIcon` missing).

- [ ] **Step 3: Prefer glyph icons**

In `Runtime/Art/SpriteKeys.cs`, add:

```csharp
        public static string GlyphIcon(string id) => $"glyph-icons/{id}";
```

In `Runtime/Art/SpriteCatalog.cs`, replace the `Icon` line with:

```csharp
        public Sprite Icon(string iconId)
        {
            var glyph = Find(SpriteKeys.GlyphIcon(iconId));
            return glyph != null ? glyph : WorldIcon(iconId);
        }

        public Sprite WorldIcon(string iconId) => Resolve(SpriteKeys.Icon(iconId), GeneratedSprites.Icon);
```

In `Runtime/Views/GroundItemViews.cs`, change `catalog.Icon(icon)` to `catalog.WorldIcon(icon)`.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode` and `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: all green.

- [ ] **Step 5: Commit the code**

```powershell
git add unity/com.mortar.unity/Runtime/Art/SpriteKeys.cs unity/com.mortar.unity/Runtime/Art/SpriteCatalog.cs unity/com.mortar.unity/Runtime/Views/GroundItemViews.cs unity/com.mortar.unity/Tests/Editor/SpriteCatalogTests.cs
git status --short
git commit -m "prefer glyph icons in the interface"
```

- [ ] **Step 6: Generate the glyph icons (controller)**

Run (from `tools/art`): `.venv\Scripts\python glyphs.py icons`
Expected: 16 `Wrote unity/Mortar.Sample/Assets/Art/glyph-icons/<id>.png` lines and `Wrote tools/art/out/sheets/glyph-icons.png`.

With the user's editor closed, rebuild the catalog: `pwsh -File tools/unity/run-method.ps1 -Method Mortar.Unity.EditorTools.SpriteCatalogBuilder.RebuildFromCommandLine`
Expected: `Mortar: sprite catalog has 59 sprites and 38 clips`.

- [ ] **Step 7: Show the user and get approval**

Show `tools/art/out/sheets/glyph-icons.png`, then ask the user to play and open the inventory (I), the belt and the character sheet (C) to judge the icons and panels together. Tune `glyph_icons.py` constants (`SOLID_ALPHA`, `EDGE_THRESHOLD`, `NORMALIZE`, `SATURATION`) from feedback, re-run Step 6, and re-run pytest. Move on when the user approves.

- [ ] **Step 8: Commit the icons**

```powershell
git add unity/Mortar.Sample/Assets/Art/glyph-icons.meta unity/Mortar.Sample/Assets/Art/glyph-icons unity/Mortar.Sample/Assets/Art/SpriteCatalog.asset tools/art/mortar_art/glyph_icons.py
git status --short
git commit -m "add glyph item icons"
```

---

## Phase 6: Docs

### Task 13: Document the look and amend the 3D spec

**Files:**
- Create: `docs/art.md`
- Modify: `docs/unity.md`, `README.md`, `docs/superpowers/specs/2026-10-15-3d-art-pipeline-design.md`

- [ ] **Step 1: Write `docs/art.md`**

```markdown
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
`unity/com.mortar.unity/Runtime/Resources/Mortar/DejaVuSansMono-LICENSE.txt`.
```

- [ ] **Step 2: Update `docs/unity.md` and `README.md`**

- `docs/unity.md`, Controls table: add the row `| F9 | Toggle the ASCII filter (debug) |`.
- `docs/unity.md`, "Placeholder art": replace the paragraph starting "Sprites are stills" with: "The game is drawn through an ASCII filter; see `docs/art.md`. Tiles and icons are AI stills: tiles are seamless textures projected onto 64×32 diamonds, or 64×64 blocks for walls and trees, and each icon also has a generated glyph version. Actors are baked from 3D models; an actor without baked sheets falls back to its still, mirrored horizontally, with a facing arrow."
- `README.md`, "Unity" section: append "The look, the glyph tools and the actor bake are described in `docs/art.md`."

- [ ] **Step 3: Amend the 3D pipeline spec**

In `docs/superpowers/specs/2026-10-15-3d-art-pipeline-design.md`:
- Under the title, add: "Amended 2026-10-15: actors are now the source for the ASCII filter (`2026-10-15-ascii-look-design.md`), which supersedes this spec's Tasks 9 and 10 and the values below."
- In "Bake", replace "Frames render at 256×384 and Pillow downsamples 2× to 128×192." with "Frames render at 208×208 and Pillow downsamples 2× to 104×104 (64 pixels per tile, foot pivot at (52, 24) from the bottom-left). Death is baked in place: the root is held at its first-frame ground position."
- In "Bake manifest", replace the `attachments` bullet with "`hide`: object names to switch off (packs ship accessories already attached to the hands)".
- In "Unity", replace "128 pixels per unit, point filtering" with "64 pixels per unit, bilinear filtering, compressed (DXT5) readable sheets".
- In the playback list, change the hit bullet to "hit plays over the simulation's stagger, or once when `Flash` fires without a stagger, overriding idle or walk but never an attack".

- [ ] **Step 4: Run every check and commit**

Run: `dotnet test Mortar.slnx`, both Unity suites, and pytest.
Expected: dotnet 474 passed; Unity and pytest all green.

```powershell
git add docs/art.md docs/unity.md README.md docs/superpowers/specs/2026-10-15-3d-art-pipeline-design.md
git status --short
git commit -m "document the ascii look"
```
