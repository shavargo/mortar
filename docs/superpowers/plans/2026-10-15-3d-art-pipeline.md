# 3D Art Pipeline (Actor Bake) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the single AI still per actor with animated 8-direction sprite sheets baked in Blender from committed CC0 models, and play them in Unity in sync with the simulation.

**Architecture:** `tools/art/bake.py` (venv Python) reads `tools/art/actors.json`, runs Blender headless once per actor with `tools/art/blender/bake_actor.py`, then packs the rendered frames into one PNG sheet plus a JSON sidecar per animation under `unity/Mortar.Sample/Assets/Art/actors/<actor>/`. The Unity catalog builder registers each sheet as an `ActorClip`; `ActorAnimator` picks the frame from simulation state so the attack's hit frame lands on the hit tick. Actors without sheets keep today's stills.

**Tech Stack:** Blender 4.5 LTS (headless, bundled Python, glTF importer, EEVEE); Python 3.14 with Pillow and pytest in `tools/art/.venv`; Unity 6000.3 (C# 9), Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-10-15-3d-art-pipeline-design.md` (approved).

## Global Constraints

- Assets are CC0 only. A file whose licence is not CC0 is never committed. No Blizzard or Diablo names anywhere.
- Unity scripts compile as C# 9: block-scoped namespaces, explicit `using` directives, no records, no `init`, no file-scoped namespaces, no collection expressions. Every assembly folder keeps its `csc.rsp` with `-nullable:enable -warnaserror+`; serialized fields need initializers (CS0649 is an error).
- Never use `?.` or `??` on `UnityEngine.Object` values; compare with `== null`.
- `src/` (engine-free simulation) is not modified by this plan.
- No file over 300 lines; no function over 50 lines (C# and Python).
- Comments only state constraints the code cannot show.
- Python: `from __future__ import annotations`, double quotes, type hints, module docstring, like the existing `tools/art` modules. `mortar_art/bake_geometry.py` is stdlib-only because Blender's bundled Python imports it.
- pytest runs from `tools/art` as `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider` and must stay warning-free.
- Unity tests: `pwsh -File tools/unity/test.ps1 -Platform EditMode|PlayMode [-Filter X]`. Only one Unity process may open `unity/Mortar.Sample` at a time; the user's editor must be closed for batch runs.
- Commit messages: short, one line, present tense, no prefixes. Stage by explicit path. Never stage `.env`, `.superpowers/`, `bin/`, `obj/`, `out/`, `tools/art/out/`, `tools/art/.venv/`, Unity `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `unity/com.mortar.unity/Plugins/`, or `Assets/StreamingAssets/`.
- Baked art is committed only after the user approves the review contact sheet.

## Design Interpretations

The spec leaves these open; this plan fixes them. Task 10 updates the spec to match.

1. **No weapon attachment step.** KayKit character files ship with their accessories already parented to the hand bones; the manifest has a `hide` list of object names to switch off instead of an `attachments` list.
2. **Sheets use bilinear filtering**, like every other sprite in the project. Point filtering shimmers on smooth renders at non-integer zoom.
3. **The hit clip also plays during the simulation's stagger** (`ActionKind.Recovering`), driven by stagger progress. A hit without a stagger plays it once on real time from `Flash`.
4. **Sprites are cut at runtime.** Sheets import as readable, uncompressed, non-power-of-two textures; `ActorClip.Frame` creates each frame sprite with `Sprite.Create` on first use and caches it. This avoids the deprecated `TextureImporter.spritesheet` API (a warning, so a build error here) and the 2D Sprite package.
5. **Baked actors render at scale 1.** The bake fixes their size (one tile = 128 frame pixels = 1 Unity unit at 128 pixels per unit). Stills keep `BodyScale` 0.8.
6. **Model facing.** glTF characters face +Z, which Blender imports as −Y, which is grid South in the bake. A per-actor `facing` offset (degrees) fixes models that face elsewhere.
7. **Hashes.** `actors.json` has one `files` map (path under `tools/art/models/` → sha256) covering every committed model file. A bake checks the files its actor references; a pytest test checks the whole map.

## Review Focus

1. **Bake geometry (Task 2):** one grid step projects to exactly (+64, +32) frame pixels for east and (−64, +32) for south, and the origin lands on the foot pivot. Everything visual depends on it.
2. **Attack sync (Task 7):** clip frame `hitFrame` shows on the tick where `ActionElapsed == HitFrame`, for several `attackTicks`.
3. **Sheet import (Task 6):** sheets are never downscaled (non-power-of-two off, max size 8192) and never registered as stills.
4. **Fallback:** an actor with no sheets behaves exactly as today.

## File Structure

```
tools/art/
  actors.json                      new: bake manifest (Task 5 warrior, Task 10 the rest)
  bake.py                          new: CLI (bake, --list-clips, --rehash, --sheet)
  blender/bake_actor.py            new: runs inside Blender; thin
  models/<pack>/                   new: committed CC0 model files + LICENSE.txt (Task 1)
  mortar_art/blender.py            new: find the Blender executable
  mortar_art/bake_geometry.py      new: camera, directions, sampling math (stdlib only)
  mortar_art/actors.py             new: manifest parsing, hashes, job building
  mortar_art/sheets.py             new: frame finishing, sheet packing, review sheet
  mortar_art/bake_runner.py        new: run Blender, collect errors
  tests/test_blender.py, test_bake_geometry.py, test_actors.py, test_sheets.py,
  tests/test_bake_runner.py, test_bake_cli.py, test_bake_smoke.py, test_models.py
unity/com.mortar.unity/
  Runtime/Art/ClipInfo.cs, ActorClip.cs, ClipNames.cs, ClipSet.cs     new
  Runtime/Art/SpriteKeys.cs, SpriteCatalog.cs                         modified
  Runtime/Views/ActorAnimator.cs, AnimMoment.cs, ClipFrame.cs,
                PropAnimator.cs                                       new
  Runtime/Views/ActorView.cs, ActorViews.cs, MarkerProps.cs           modified
  Editor/ArtImport.cs, SpriteCatalogBuilder.cs                        modified
  Tests/Editor/TestClipInfo.cs, ActorClipTests.cs, ClipSetTests.cs,
               ActorAnimatorTests.cs                                  new
  Tests/Editor/SpriteCatalogTests.cs, ArtImportTests.cs               modified
  Tests/Runtime/TestClips.cs, BakedActorTests.cs                      new
unity/Mortar.Sample/Assets/Art/actors/<actor>/<anim>.png + .json     baked art (Tasks 9, 10)
docs/art.md                                                           new (Task 10)
```

---

### Task 1: Install Blender and commit the CC0 models (controller and user)

This task needs the user: itch.io downloads require a person. The controller does every other step.

**Files:**
- Create: `tools/art/models/kaykit-adventurers/`, `tools/art/models/kaykit-skeletons/`, `tools/art/models/kaykit-animations/`, `tools/art/models/quaternius/` (model files and one `LICENSE.txt` each)

- [ ] **Step 1: Install Blender**

Run: `winget install --id BlenderFoundation.Blender.LTS.4.5 --exact --accept-package-agreements --accept-source-agreements`
Expected: installs to `C:\Program Files\Blender Foundation\Blender 4.5\blender.exe` (the installer may show a UAC prompt for the user). Verify with `& "C:\Program Files\Blender Foundation\Blender 4.5\blender.exe" --version` → `Blender 4.5.x`.

- [ ] **Step 2: Ask the user to download the packs**

Ask the user to download these free tiers and unzip each into `tools/art/out/packs/<folder>/` (gitignored):

| Folder | Page |
| --- | --- |
| `kaykit-adventurers` | https://kaylousberg.itch.io/kaykit-adventurers (free tier) |
| `kaykit-skeletons` | https://kaylousberg.itch.io/kaykit-skeletons (free tier) |
| `kaykit-animations` | https://kaylousberg.itch.io/kaykit-character-animations (free tier) |
| `quaternius` | glTF downloads of Demon and Blue Demon from https://poly.pizza/bundle/Ultimate-Monsters-Bundle-5oyGWAmOB6, Bat from https://poly.pizza/m/hNO9XvjlKa, and the Zombie from https://poly.pizza/bundle/Post-Apocolypse-Pack-jg0We8Clu0 |

- [ ] **Step 3: Check every licence**

For each pack, read its licence file or page and confirm CC0. On poly.pizza check each model's own page, because the Post Apocalypse bundle lists both CC0 and CC-BY: the zombie used must say "Public Domain (CC0)". If a model is not CC0, stop and ask the user for a replacement.

- [ ] **Step 4: Copy only the files in use**

Prefer `.glb`. If a pack ships only `.gltf`, copy the `.gltf` with its `.bin` and texture files, keeping their relative layout. Copy into `tools/art/models/<folder>/`:
- `kaykit-adventurers`: Knight, Barbarian, Rogue_Hooded, Mage.
- `kaykit-skeletons`: Skeleton_Minion, Skeleton_Warrior.
- `kaykit-animations`: the Rig_Medium sets that hold idle, walk, a one-handed melee attack, a hit reaction and a death (typically the General, MovementBasic and CombatMelee sets).
- `quaternius`: Demon, Blue Demon, Bat, Zombie.

Write `tools/art/models/<folder>/LICENSE.txt` for each:

```text
<Pack name> by <author>
Source: <URL>
Licence: CC0 1.0 Universal (public domain dedication)
https://creativecommons.org/publicdomain/zero/1.0/
Files here are unmodified copies used as bake inputs.
```

- [ ] **Step 5: Check the size and commit**

Run: `Get-ChildItem -Recurse tools/art/models | Measure-Object Length -Sum`
Expected: under 40 MB. If it is larger, drop unused animation sets.

```powershell
git add tools/art/models
git status --short
git commit -m "add CC0 actor models"
```

---

### Task 2: Blender discovery and bake geometry

**Files:**
- Create: `tools/art/mortar_art/blender.py`, `tools/art/mortar_art/bake_geometry.py`
- Test: `tools/art/tests/test_blender.py`, `tools/art/tests/test_bake_geometry.py`

**Interfaces:**
- Produces: `find_blender(environ: Mapping[str, str]) -> Path`, `BlenderError`, `PINNED_VERSION = "4.5"`.
- Produces in `bake_geometry`: `DIRECTION_DELTAS`, `ANIMATIONS = ("idle", "walk", "attack", "hit", "death")`, `LOOPING`, `FRAME_WIDTH = 128`, `FRAME_HEIGHT = 192`, `RENDER_SCALE = 2`, `TILE_PIXELS = 128`, `PIVOT_X = 64`, `PIVOT_FROM_BOTTOM = 16`, `SAMPLE_FPS = 10`, `CAMERA_DISTANCE`, `render_size()`, `ground_vector(dx, dy)`, `model_rotation_degrees(direction, facing_offset=0.0)`, `camera_location()`, `camera_rotation_radians()`, `ortho_scale()`, `camera_shift()`, `project(point)`, `sample_times(duration, loop, fps=SAMPLE_FPS)`, `hit_frame_index(hit_seconds, frame_count, fps=SAMPLE_FPS)`.

- [ ] **Step 1: Write the failing tests**

`tools/art/tests/test_blender.py`:

```python
from pathlib import Path

import pytest

from mortar_art.blender import PINNED_VERSION, BlenderError, find_blender


def _install(root: Path, version: str) -> Path:
    exe = root / "Blender Foundation" / f"Blender {version}" / "blender.exe"
    exe.parent.mkdir(parents=True)
    exe.write_bytes(b"")
    return exe


def test_the_blender_variable_wins(tmp_path):
    custom = tmp_path / "custom" / "blender.exe"
    custom.parent.mkdir()
    custom.write_bytes(b"")
    _install(tmp_path, PINNED_VERSION)

    assert find_blender({"BLENDER": str(custom), "ProgramFiles": str(tmp_path)}) == custom


def test_the_blender_variable_must_name_a_file(tmp_path):
    with pytest.raises(BlenderError, match="not a file"):
        find_blender({"BLENDER": str(tmp_path / "missing.exe")})


def test_the_pinned_install_is_found(tmp_path):
    exe = _install(tmp_path, PINNED_VERSION)

    assert find_blender({"ProgramFiles": str(tmp_path)}) == exe


def test_other_versions_are_named_but_not_used(tmp_path):
    _install(tmp_path, "5.2")

    with pytest.raises(BlenderError, match=r"found Blender 5\.2") as error:
        find_blender({"ProgramFiles": str(tmp_path)})
    assert f"BlenderFoundation.Blender.LTS.{PINNED_VERSION}" in str(error.value)


def test_a_missing_install_says_how_to_fix_it(tmp_path):
    with pytest.raises(BlenderError, match="set BLENDER"):
        find_blender({"ProgramFiles": str(tmp_path)})


def test_no_program_files_still_explains(tmp_path):
    with pytest.raises(BlenderError, match="set BLENDER"):
        find_blender({})
```

`tools/art/tests/test_bake_geometry.py`:

```python
import math

import pytest

from mortar_art import bake_geometry as geo


def test_the_origin_lands_on_the_foot_pivot():
    x, y = geo.project((0.0, 0.0, 0.0))

    assert x == pytest.approx(geo.PIVOT_X)
    assert y == pytest.approx(geo.FRAME_HEIGHT - geo.PIVOT_FROM_BOTTOM)


@pytest.mark.parametrize(
    ("dx", "dy", "expected"),
    [(1, 0, (64, 32)), (0, 1, (-64, 32)), (1, 1, (0, 64)), (-1, -1, (0, -64)), (-1, 0, (-64, -32))],
)
def test_one_grid_step_matches_the_unity_isometric_grid(dx, dy, expected):
    origin = geo.project((0.0, 0.0, 0.0))
    step = geo.project(geo.ground_vector(dx, dy))

    assert step[0] - origin[0] == pytest.approx(expected[0])
    assert step[1] - origin[1] == pytest.approx(expected[1])


@pytest.mark.parametrize("direction", range(8))
def test_a_turned_model_faces_its_grid_direction(direction):
    angle = math.radians(geo.model_rotation_degrees(direction))
    facing = (math.sin(angle), -math.cos(angle))
    gx, gy, _ = geo.ground_vector(*geo.DIRECTION_DELTAS[direction])
    length = math.hypot(gx, gy)

    assert facing[0] == pytest.approx(gx / length, abs=1e-9)
    assert facing[1] == pytest.approx(gy / length, abs=1e-9)


def test_the_facing_offset_turns_every_direction():
    assert geo.model_rotation_degrees(2, facing_offset=90.0) == pytest.approx(0.0)
    assert geo.model_rotation_degrees(0, facing_offset=-45.0) == pytest.approx(315.0)


def test_the_camera_rotation_looks_at_the_origin():
    rx, ry, rz = geo.camera_rotation_radians()
    forward = (-math.sin(rx) * math.sin(rz), math.sin(rx) * math.cos(rz), -math.cos(rx))
    location = geo.camera_location()
    distance = math.sqrt(sum(value * value for value in location))

    assert ry == 0.0
    assert distance == pytest.approx(geo.CAMERA_DISTANCE)
    for axis in range(3):
        assert forward[axis] == pytest.approx(-location[axis] / distance)


def test_renders_are_twice_the_frame_size():
    assert geo.render_size() == (256, 384)


def test_looping_clips_do_not_repeat_their_first_pose():
    assert geo.sample_times(1.0, loop=True) == pytest.approx([index / 10 for index in range(10)])


def test_held_clips_end_on_their_last_pose():
    times = geo.sample_times(0.92, loop=False)

    assert len(times) == 10
    assert times[-1] == pytest.approx(0.92)


def test_very_short_clips_still_get_a_frame():
    assert geo.sample_times(0.01, loop=True) == [0.0]


def test_the_hit_frame_snaps_to_the_nearest_frame():
    assert geo.hit_frame_index(0.42, 10) == 4


def test_the_hit_frame_is_never_the_first_or_past_the_last_frame():
    assert geo.hit_frame_index(0.0, 10) == 1
    assert geo.hit_frame_index(5.0, 10) == 9


def test_attacks_need_two_frames():
    with pytest.raises(ValueError, match="two frames"):
        geo.hit_frame_index(0.3, 1)
```

- [ ] **Step 2: Run the tests to verify they fail**

Run (from `tools/art`): `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider tests/test_blender.py tests/test_bake_geometry.py`
Expected: FAIL with `ModuleNotFoundError: No module named 'mortar_art.blender'`.

- [ ] **Step 3: Write `mortar_art/blender.py`**

```python
"""Locate the Blender executable used for the actor bake."""

from __future__ import annotations

from collections.abc import Mapping
from pathlib import Path

PINNED_VERSION = "4.5"
VENDOR_FOLDER = "Blender Foundation"


class BlenderError(Exception):
    pass


def find_blender(environ: Mapping[str, str]) -> Path:
    override = environ.get("BLENDER", "").strip()
    if override:
        path = Path(override)
        if not path.is_file():
            raise BlenderError(f"BLENDER is set to {path}, which is not a file")
        return path
    program_files = environ.get("ProgramFiles", "").strip()
    if not program_files:
        raise BlenderError(_not_found("no ProgramFiles folder"))
    vendor = Path(program_files) / VENDOR_FOLDER
    pinned = vendor / f"Blender {PINNED_VERSION}" / "blender.exe"
    if pinned.is_file():
        return pinned
    found = sorted(path.parent.name for path in vendor.glob("Blender */blender.exe"))
    raise BlenderError(_not_found(f"found {', '.join(found)}" if found else f"nothing under {vendor}"))


def _not_found(detail: str) -> str:
    return (
        f"Blender {PINNED_VERSION} not found ({detail}); install it with "
        f"'winget install BlenderFoundation.Blender.LTS.{PINNED_VERSION}' or set BLENDER to blender.exe"
    )
```

- [ ] **Step 4: Write `mortar_art/bake_geometry.py`**

```python
"""Camera, direction and timing math shared by the Blender bake script and the sheet packer.

Stdlib only: Blender's bundled Python imports this module. Bake ground axes: grid +x is Blender +X
and grid +y (south) is Blender -Y; one grid cell is one metre.
"""

from __future__ import annotations

import math

DIRECTION_DELTAS = ((0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1), (1, 0), (1, 1))
ANIMATIONS = ("idle", "walk", "attack", "hit", "death")
LOOPING = frozenset({"idle", "walk"})

FRAME_WIDTH = 128
FRAME_HEIGHT = 192
RENDER_SCALE = 2
TILE_PIXELS = 128
PIVOT_X = 64
PIVOT_FROM_BOTTOM = 16
SAMPLE_FPS = 10

CAMERA_PITCH_DEGREES = 30.0
CAMERA_YAW_DEGREES = -45.0
CAMERA_DISTANCE = 20.0

Vector = tuple[float, float, float]


def render_size() -> tuple[int, int]:
    return FRAME_WIDTH * RENDER_SCALE, FRAME_HEIGHT * RENDER_SCALE


def ground_vector(dx: int, dy: int) -> Vector:
    return float(dx), float(-dy), 0.0


def model_rotation_degrees(direction: int, facing_offset: float = 0.0) -> float:
    return (facing_offset - 45.0 * direction) % 360.0


def camera_location() -> Vector:
    pitch = math.radians(CAMERA_PITCH_DEGREES)
    yaw = math.radians(CAMERA_YAW_DEGREES)
    flat = CAMERA_DISTANCE * math.cos(pitch)
    return flat * math.cos(yaw), flat * math.sin(yaw), CAMERA_DISTANCE * math.sin(pitch)


def camera_rotation_radians() -> Vector:
    return math.radians(90.0 - CAMERA_PITCH_DEGREES), 0.0, math.radians(CAMERA_YAW_DEGREES + 90.0)


def ortho_scale() -> float:
    return FRAME_HEIGHT / TILE_PIXELS * math.sqrt(2.0)


def camera_shift() -> tuple[float, float]:
    pivot_row = FRAME_HEIGHT - PIVOT_FROM_BOTTOM
    return (FRAME_WIDTH / 2 - PIVOT_X) / FRAME_HEIGHT, (pivot_row - FRAME_HEIGHT / 2) / FRAME_HEIGHT


def project(point: Vector) -> tuple[float, float]:
    right, up = _camera_axes()
    pixels_per_metre = FRAME_HEIGHT / ortho_scale()
    shift_x, shift_y = camera_shift()
    x = FRAME_WIDTH / 2 + _dot(point, right) * pixels_per_metre - shift_x * FRAME_HEIGHT
    y = FRAME_HEIGHT / 2 - _dot(point, up) * pixels_per_metre + shift_y * FRAME_HEIGHT
    return x, y


def sample_times(duration: float, loop: bool, fps: int = SAMPLE_FPS) -> list[float]:
    count = max(1, round(duration * fps))
    times = [index / fps for index in range(count)]
    return times if loop else times + [duration]


def hit_frame_index(hit_seconds: float, frame_count: int, fps: int = SAMPLE_FPS) -> int:
    if frame_count < 2:
        raise ValueError("an attack needs at least two frames")
    return min(max(round(hit_seconds * fps), 1), frame_count - 1)


def _camera_axes() -> tuple[Vector, Vector]:
    x, y, z = camera_location()
    length = math.sqrt(x * x + y * y + z * z)
    forward = (-x / length, -y / length, -z / length)
    right = _normalize((forward[1], -forward[0], 0.0))
    return right, _cross(right, forward)


def _dot(a: Vector, b: Vector) -> float:
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def _cross(a: Vector, b: Vector) -> Vector:
    return a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]


def _normalize(vector: Vector) -> Vector:
    length = math.sqrt(_dot(vector, vector))
    return vector[0] / length, vector[1] / length, vector[2] / length
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider`
Expected: all pass (47 existing + the new ones).

- [ ] **Step 6: Commit**

```powershell
git add tools/art/mortar_art/blender.py tools/art/mortar_art/bake_geometry.py tools/art/tests/test_blender.py tools/art/tests/test_bake_geometry.py
git commit -m "add blender discovery and bake geometry"
```

---

### Task 3: The bake manifest

**Files:**
- Create: `tools/art/mortar_art/actors.py`
- Test: `tools/art/tests/test_actors.py`

**Interfaces:**
- Consumes: `bake_geometry.ANIMATIONS`; `config.REPO_ROOT`.
- Produces: `ManifestError`; `ActorSpec` (fields `id, model, animations, hide, clips, scale, facing, tint, attack_hit_seconds, npc`; properties `wanted`, `files`, `directions`); `Manifest(actors: dict[str, ActorSpec], files: dict[str, str])`; `load_manifest(path=ACTORS_PATH)`, `parse_manifest(data)`, `required_actor_ids(content_root=CONTENT_ROOT) -> set[str]`, `sha256_of(path)`, `check_files(manifest, actor, models_root=MODELS_ROOT)`, `hash_models(models_root=MODELS_ROOT) -> dict[str, str]`, `record_hashes(path=ACTORS_PATH, models_root=MODELS_ROOT) -> int`, `build_job(actor, out_dir, models_root=MODELS_ROOT, *, animations=None, directions=None, max_frames=None) -> dict`, `list_job(paths) -> dict`; constants `ACTORS_PATH`, `MODELS_ROOT`, `CONTENT_ROOT`, `NPC_IDS`.

`actors.json` shape (Task 5 writes the real one):

```json
{
  "actors": [
    {
      "id": "warrior",
      "model": "kaykit-adventurers/Knight.glb",
      "animations": ["kaykit-animations/Rig_Medium_General.glb"],
      "hide": ["2H_Sword"],
      "clips": { "idle": "Idle_A", "walk": "Walking_A", "attack": "1H_Melee_Attack_Chop", "hit": "Hit_A", "death": "Death_A" },
      "attackHitSeconds": 0.45,
      "scale": 1.0,
      "facing": 0,
      "tint": [1.0, 1.0, 1.0]
    },
    { "id": "smith", "model": "kaykit-adventurers/Barbarian.glb", "npc": true, "clips": { "idle": "Idle_A" } }
  ],
  "files": { "kaykit-adventurers/Knight.glb": "<sha256>" }
}
```

- [ ] **Step 1: Write the failing tests**

`tools/art/tests/test_actors.py`:

```python
import copy
import json

import pytest

from mortar_art import actors
from mortar_art.actors import ManifestError

WARRIOR = {
    "id": "warrior",
    "model": "pack/knight.glb",
    "animations": ["pack/moves.glb"],
    "hide": ["Cape"],
    "clips": {"idle": "Idle", "walk": "Walk", "attack": "Chop", "hit": "Hit", "death": "Death"},
    "attackHitSeconds": 0.4,
    "scale": 1.1,
    "facing": 90,
    "tint": [1.0, 0.9, 0.8],
}
SMITH = {"id": "smith", "model": "pack/smith.glb", "npc": True, "clips": {"idle": "Idle"}}


def manifest_data(*entries):
    files = {name: "0" * 64 for entry in entries for name in [entry["model"], *entry.get("animations", [])]}
    return {"actors": [copy.deepcopy(entry) for entry in entries], "files": files}


def test_an_actor_entry_is_parsed():
    warrior = actors.parse_manifest(manifest_data(WARRIOR)).actors["warrior"]

    assert warrior.model == "pack/knight.glb"
    assert warrior.files == ("pack/knight.glb", "pack/moves.glb")
    assert warrior.hide == ("Cape",)
    assert warrior.wanted == ("idle", "walk", "attack", "hit", "death")
    assert warrior.directions == tuple(range(8))
    assert (warrior.scale, warrior.facing, warrior.tint) == (1.1, 90.0, (1.0, 0.9, 0.8))
    assert warrior.attack_hit_seconds == 0.4


def test_npcs_need_only_idle_and_face_south():
    smith = actors.parse_manifest(manifest_data(SMITH)).actors["smith"]

    assert smith.wanted == ("idle",)
    assert smith.directions == (0,)
    assert smith.tint == (1.0, 1.0, 1.0)


def test_a_missing_clip_is_named():
    entry = copy.deepcopy(WARRIOR)
    del entry["clips"]["death"]

    with pytest.raises(ManifestError, match="warrior: clips are missing death"):
        actors.parse_manifest(manifest_data(entry))


def test_unknown_animation_names_are_rejected():
    entry = copy.deepcopy(WARRIOR)
    entry["clips"]["cast"] = "Spell"

    with pytest.raises(ManifestError, match="unknown animations cast"):
        actors.parse_manifest(manifest_data(entry))


def test_fighters_need_an_attack_hit_time():
    entry = copy.deepcopy(WARRIOR)
    del entry["attackHitSeconds"]

    with pytest.raises(ManifestError, match="attackHitSeconds"):
        actors.parse_manifest(manifest_data(entry))


def test_a_tint_needs_three_numbers():
    entry = copy.deepcopy(WARRIOR)
    entry["tint"] = [1.0, 1.0]

    with pytest.raises(ManifestError, match="tint"):
        actors.parse_manifest(manifest_data(entry))


def test_an_actor_cannot_be_listed_twice():
    with pytest.raises(ManifestError, match="listed twice"):
        actors.parse_manifest(manifest_data(SMITH, SMITH))


def test_every_referenced_file_needs_a_hash():
    data = manifest_data(WARRIOR)
    del data["files"]["pack/moves.glb"]

    with pytest.raises(ManifestError, match="no sha256 recorded for pack/moves.glb"):
        actors.parse_manifest(data)


def test_a_missing_manifest_is_reported(tmp_path):
    with pytest.raises(ManifestError, match="not found"):
        actors.load_manifest(tmp_path / "actors.json")


def test_required_ids_come_from_classes_monsters_and_npcs(tmp_path):
    for folder, name in (("classes", "warrior"), ("monsters", "zombie")):
        (tmp_path / folder).mkdir()
        (tmp_path / folder / f"{name}.json").write_text(json.dumps({"id": name, "spriteSet": name}), encoding="utf-8")

    assert actors.required_actor_ids(tmp_path) == {"warrior", "zombie", "smith", "healer", "sage"}


def _models(tmp_path, content=b"model"):
    (tmp_path / "pack").mkdir()
    (tmp_path / "pack" / "smith.glb").write_bytes(content)
    (tmp_path / "pack" / "LICENSE.txt").write_text("CC0", encoding="utf-8")
    return tmp_path


def test_files_matching_their_hash_pass(tmp_path):
    root = _models(tmp_path)
    data = manifest_data(SMITH)
    data["files"]["pack/smith.glb"] = actors.sha256_of(root / "pack" / "smith.glb")
    manifest = actors.parse_manifest(data)

    actors.check_files(manifest, manifest.actors["smith"], root)


def test_a_changed_file_is_rejected(tmp_path):
    root = _models(tmp_path)
    manifest = actors.parse_manifest(manifest_data(SMITH))

    with pytest.raises(ManifestError, match="smith: pack/smith.glb does not match its recorded sha256"):
        actors.check_files(manifest, manifest.actors["smith"], root)


def test_a_missing_file_is_rejected(tmp_path):
    manifest = actors.parse_manifest(manifest_data(SMITH))

    with pytest.raises(ManifestError, match="model file pack/smith.glb is missing"):
        actors.check_files(manifest, manifest.actors["smith"], tmp_path)


def test_hashing_skips_licences_and_uses_forward_slashes(tmp_path):
    root = _models(tmp_path)

    assert list(actors.hash_models(root)) == ["pack/smith.glb"]


def test_recording_hashes_keeps_the_actors(tmp_path):
    root = _models(tmp_path / "models")
    path = tmp_path / "actors.json"
    path.write_text(json.dumps({"actors": [SMITH], "files": {}}), encoding="utf-8")

    assert actors.record_hashes(path, root) == 1
    saved = json.loads(path.read_text(encoding="utf-8"))
    assert saved["actors"] == [SMITH]
    assert saved["files"]["pack/smith.glb"] == actors.sha256_of(root / "pack" / "smith.glb")


def test_a_bake_job_has_absolute_paths_and_the_wanted_clips(tmp_path):
    warrior = actors.parse_manifest(manifest_data(WARRIOR)).actors["warrior"]

    job = actors.build_job(warrior, tmp_path / "out", tmp_path)

    assert job["mode"] == "bake"
    assert job["model"] == str(tmp_path / "pack" / "knight.glb")
    assert job["animations"] == [str(tmp_path / "pack" / "moves.glb")]
    assert list(job["clips"]) == ["idle", "walk", "attack", "hit", "death"]
    assert job["directions"] == list(range(8))
    assert (job["scale"], job["facing"], job["attackHitSeconds"]) == (1.1, 90.0, 0.4)
    assert job["hide"] == ["Cape"]
    assert job["maxFrames"] is None
    assert job["out"] == str(tmp_path / "out")


def test_a_bake_job_can_be_narrowed(tmp_path):
    warrior = actors.parse_manifest(manifest_data(WARRIOR)).actors["warrior"]

    job = actors.build_job(warrior, tmp_path, tmp_path, animations=("idle",), directions=(0,), max_frames=1)

    assert job["clips"] == {"idle": "Idle"}
    assert job["directions"] == [0]
    assert job["maxFrames"] == 1


def test_a_list_job_names_the_files(tmp_path):
    assert actors.list_job([tmp_path / "a.glb"]) == {"mode": "list", "files": [str(tmp_path / "a.glb")]}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider tests/test_actors.py`
Expected: FAIL with `ImportError: cannot import name 'actors'`.

- [ ] **Step 3: Write `mortar_art/actors.py`**

```python
"""The actor bake manifest (actors.json): which model, clips and look each actor uses."""

from __future__ import annotations

import hashlib
import json
from collections.abc import Iterable
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from .bake_geometry import ANIMATIONS
from .config import REPO_ROOT

ART_TOOL_ROOT = REPO_ROOT / "tools" / "art"
ACTORS_PATH = ART_TOOL_ROOT / "actors.json"
MODELS_ROOT = ART_TOOL_ROOT / "models"
CONTENT_ROOT = REPO_ROOT / "content"
NPC_IDS = ("smith", "healer", "sage")
LICENCE_NAME = "LICENSE.txt"


class ManifestError(Exception):
    pass


@dataclass(frozen=True)
class ActorSpec:
    id: str
    model: str
    animations: tuple[str, ...]
    hide: tuple[str, ...]
    clips: dict[str, str]
    scale: float = 1.0
    facing: float = 0.0
    tint: tuple[float, float, float] = (1.0, 1.0, 1.0)
    attack_hit_seconds: float = 0.0
    npc: bool = False

    @property
    def wanted(self) -> tuple[str, ...]:
        return ("idle",) if self.npc else ANIMATIONS

    @property
    def files(self) -> tuple[str, ...]:
        return (self.model, *self.animations)

    @property
    def directions(self) -> tuple[int, ...]:
        return (0,) if self.npc else tuple(range(8))


@dataclass(frozen=True)
class Manifest:
    actors: dict[str, ActorSpec]
    files: dict[str, str]


def load_manifest(path: Path = ACTORS_PATH) -> Manifest:
    if not path.is_file():
        raise ManifestError(f"{path.name} not found at {path}")
    return parse_manifest(json.loads(path.read_text(encoding="utf-8")))


def parse_manifest(data: dict[str, Any]) -> Manifest:
    files = {str(name): str(digest) for name, digest in data.get("files", {}).items()}
    parsed: dict[str, ActorSpec] = {}
    for entry in data.get("actors", []):
        actor = _parse_actor(entry)
        if actor.id in parsed:
            raise ManifestError(f"actor '{actor.id}' is listed twice")
        unhashed = [name for name in actor.files if name not in files]
        if unhashed:
            raise ManifestError(f"{actor.id}: no sha256 recorded for {', '.join(unhashed)}")
        parsed[actor.id] = actor
    return Manifest(parsed, files)


def _parse_actor(entry: dict[str, Any]) -> ActorSpec:
    actor_id = str(_require(entry, "id", "an actor entry"))
    npc = bool(entry.get("npc", False))
    clips = {str(name): str(clip) for name, clip in _require(entry, "clips", actor_id).items()}
    missing = [name for name in (("idle",) if npc else ANIMATIONS) if name not in clips]
    if missing:
        raise ManifestError(f"{actor_id}: clips are missing {', '.join(missing)}")
    unknown = sorted(set(clips) - set(ANIMATIONS))
    if unknown:
        raise ManifestError(f"{actor_id}: unknown animations {', '.join(unknown)}")
    hit = float(entry.get("attackHitSeconds", 0.0))
    if not npc and hit <= 0:
        raise ManifestError(f"{actor_id}: attackHitSeconds must be greater than 0")
    tint = tuple(float(value) for value in entry.get("tint", (1.0, 1.0, 1.0)))
    if len(tint) != 3:
        raise ManifestError(f"{actor_id}: tint needs three numbers")
    return ActorSpec(
        id=actor_id,
        model=str(_require(entry, "model", actor_id)),
        animations=_strings(entry.get("animations", [])),
        hide=_strings(entry.get("hide", [])),
        clips=clips,
        scale=float(entry.get("scale", 1.0)),
        facing=float(entry.get("facing", 0.0)),
        tint=(tint[0], tint[1], tint[2]),
        attack_hit_seconds=hit,
        npc=npc,
    )


def _require(entry: dict[str, Any], name: str, owner: str) -> Any:
    if name not in entry:
        raise ManifestError(f"{owner} is missing '{name}'")
    return entry[name]


def _strings(values: Iterable[Any]) -> tuple[str, ...]:
    return tuple(str(value) for value in values)


def required_actor_ids(content_root: Path = CONTENT_ROOT) -> set[str]:
    ids = set(NPC_IDS)
    for folder in ("classes", "monsters"):
        for path in sorted((content_root / folder).glob("*.json")):
            sprite_set = json.loads(path.read_text(encoding="utf-8")).get("spriteSet")
            if sprite_set:
                ids.add(str(sprite_set))
    return ids


def sha256_of(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def check_files(manifest: Manifest, actor: ActorSpec, models_root: Path = MODELS_ROOT) -> None:
    for name in actor.files:
        path = models_root / name
        if not path.is_file():
            raise ManifestError(f"{actor.id}: model file {name} is missing")
        if sha256_of(path) != manifest.files[name]:
            raise ManifestError(f"{actor.id}: {name} does not match its recorded sha256")


def hash_models(models_root: Path = MODELS_ROOT) -> dict[str, str]:
    return {
        path.relative_to(models_root).as_posix(): sha256_of(path)
        for path in sorted(models_root.rglob("*"))
        if path.is_file() and path.name != LICENCE_NAME
    }


def record_hashes(path: Path = ACTORS_PATH, models_root: Path = MODELS_ROOT) -> int:
    data = json.loads(path.read_text(encoding="utf-8"))
    data["files"] = hash_models(models_root)
    path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
    return len(data["files"])


def build_job(
    actor: ActorSpec,
    out_dir: Path,
    models_root: Path = MODELS_ROOT,
    *,
    animations: tuple[str, ...] | None = None,
    directions: tuple[int, ...] | None = None,
    max_frames: int | None = None,
) -> dict[str, Any]:
    return {
        "mode": "bake",
        "actor": actor.id,
        "model": str(models_root / actor.model),
        "animations": [str(models_root / name) for name in actor.animations],
        "hide": list(actor.hide),
        "clips": {name: actor.clips[name] for name in (animations or actor.wanted)},
        "attackHitSeconds": actor.attack_hit_seconds,
        "scale": actor.scale,
        "facing": actor.facing,
        "directions": list(directions if directions is not None else actor.directions),
        "maxFrames": max_frames,
        "out": str(out_dir),
    }


def list_job(paths: list[Path]) -> dict[str, Any]:
    return {"mode": "list", "files": [str(path) for path in paths]}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider`
Expected: all pass.

- [ ] **Step 5: Commit**

```powershell
git add tools/art/mortar_art/actors.py tools/art/tests/test_actors.py
git commit -m "add actor bake manifest"
```

---

### Task 4: Sheet packing and the review sheet

**Files:**
- Create: `tools/art/mortar_art/sheets.py`
- Test: `tools/art/tests/test_sheets.py`

**Interfaces:**
- Consumes: `bake_geometry` frame constants, `LOOPING`, `render_size()`; `imaging.contact_sheet(entries, cell=128, columns=6)`.
- Produces: `finish_frame(render, tint=(1.0, 1.0, 1.0)) -> Image`, `apply_tint(frame, tint) -> Image`, `pack_sheet(rows) -> Image`, `sheet_info(frames, directions, loop, hit_frame) -> dict`, `write_sheet(sheet, info, png_path)`, `pack_actor(bake_dir, art_dir, tint) -> list[Path]`, `frame_at(sheet, row, index) -> Image`, `review_entries(actor_id, art_dir) -> list[tuple[str, Image]]`, `write_review(entries, path)`.
- The Blender script (Task 5) writes `<bake_dir>/frames.json`: `{"directions": [0, ...], "animations": {"<anim>": {"frames": N, "hitFrame": H}}}` and frames at `<bake_dir>/<anim>/<direction>/<NN>.png`.
- Sidecar JSON keys (Unity's `ClipInfo` in Task 6 reads these names): `frames, directions, fps, loop, hitFrame, frameWidth, frameHeight, pivotX, pivotY` (pivot in pixels from the frame's bottom-left).

- [ ] **Step 1: Write the failing tests**

`tools/art/tests/test_sheets.py`:

```python
import json

import pytest
from PIL import Image

from mortar_art import bake_geometry as geo
from mortar_art import sheets

CLEAR = (0, 0, 0, 0)


def render(color=CLEAR):
    return Image.new("RGBA", geo.render_size(), color)


def frame(color):
    return Image.new("RGBA", (geo.FRAME_WIDTH, geo.FRAME_HEIGHT), color)


def test_finishing_halves_the_render():
    finished = sheets.finish_frame(render((10, 20, 30, 255)))

    assert finished.size == (geo.FRAME_WIDTH, geo.FRAME_HEIGHT)
    assert finished.getpixel((5, 5)) == (10, 20, 30, 255)


def test_finishing_keeps_edge_colours_instead_of_darkening_them():
    source = render()
    source.putpixel((0, 0), (255, 0, 0, 255))
    source.putpixel((0, 1), (255, 0, 0, 255))

    red, green, blue, alpha = sheets.finish_frame(source).getpixel((0, 0))

    assert alpha == pytest.approx(128, abs=1)
    assert red >= 250 and green == 0 and blue == 0


def test_finishing_rejects_the_wrong_render_size():
    with pytest.raises(ValueError, match="256x384"):
        sheets.finish_frame(Image.new("RGBA", (100, 100)))


def test_a_tint_scales_colour_and_keeps_alpha():
    tinted = sheets.apply_tint(frame((200, 100, 50, 128)), (0.5, 2.0, 1.0))

    assert tinted.getpixel((0, 0)) == (100, 200, 50, 128)


def test_a_sheet_has_a_row_per_direction_and_a_column_per_frame():
    rows = [[frame((255, 0, 0, 255)), frame((0, 255, 0, 255))], [frame((0, 0, 255, 255)), frame((9, 9, 9, 255))]]

    sheet = sheets.pack_sheet(rows)

    assert sheet.size == (2 * geo.FRAME_WIDTH, 2 * geo.FRAME_HEIGHT)
    assert sheet.getpixel((geo.FRAME_WIDTH + 1, 1)) == (0, 255, 0, 255)
    assert sheet.getpixel((1, geo.FRAME_HEIGHT + 1)) == (0, 0, 255, 255)


def test_rows_must_have_the_same_length():
    with pytest.raises(ValueError, match="same number of frames"):
        sheets.pack_sheet([[frame(CLEAR)], [frame(CLEAR), frame(CLEAR)]])


def test_sheet_info_matches_the_unity_reader():
    assert sheets.sheet_info(6, 8, False, 3) == {
        "frames": 6,
        "directions": 8,
        "fps": geo.SAMPLE_FPS,
        "loop": False,
        "hitFrame": 3,
        "frameWidth": geo.FRAME_WIDTH,
        "frameHeight": geo.FRAME_HEIGHT,
        "pivotX": geo.PIVOT_X,
        "pivotY": geo.PIVOT_FROM_BOTTOM,
    }


def _fake_bake(bake_dir, directions, animations):
    bake_dir.mkdir(parents=True)
    summary = {"directions": directions, "animations": {}}
    for name, (count, hit) in animations.items():
        summary["animations"][name] = {"frames": count, "hitFrame": hit}
        for direction in directions:
            for index in range(count):
                path = bake_dir / name / str(direction) / f"{index:02d}.png"
                path.parent.mkdir(parents=True, exist_ok=True)
                render((direction * 40, index * 40, 0, 255)).save(path)
    (bake_dir / "frames.json").write_text(json.dumps(summary), encoding="utf-8")


def test_packing_an_actor_writes_a_sheet_and_sidecar_per_animation(tmp_path):
    _fake_bake(tmp_path / "bake", [0, 1], {"idle": (2, 0), "attack": (3, 1)})

    written = sheets.pack_actor(tmp_path / "bake", tmp_path / "art", (1.0, 1.0, 1.0))

    assert sorted(path.name for path in written) == ["attack.png", "idle.png"]
    with Image.open(tmp_path / "art" / "attack.png") as sheet:
        assert sheet.size == (3 * geo.FRAME_WIDTH, 2 * geo.FRAME_HEIGHT)
        assert sheet.getpixel((2 * geo.FRAME_WIDTH + 1, geo.FRAME_HEIGHT + 1)) == (40, 80, 0, 255)
    attack = json.loads((tmp_path / "art" / "attack.json").read_text(encoding="utf-8"))
    idle = json.loads((tmp_path / "art" / "idle.json").read_text(encoding="utf-8"))
    assert (attack["loop"], attack["hitFrame"], attack["directions"]) == (False, 1, 2)
    assert idle["loop"] is True


def test_review_shows_idle_and_the_attack_hit_frame_for_each_direction(tmp_path):
    _fake_bake(tmp_path / "bake", [0, 1], {"idle": (2, 0), "attack": (3, 2)})
    sheets.pack_actor(tmp_path / "bake", tmp_path / "art", (1.0, 1.0, 1.0))

    entries = sheets.review_entries("warrior", tmp_path / "art")

    assert [label for label, _ in entries] == [
        "warrior idle 0", "warrior idle 1", "warrior attack 0", "warrior attack 1",
    ]
    assert entries[3][1].getpixel((1, 1)) == (40, 80, 0, 255)


def test_writing_the_review_saves_a_contact_sheet(tmp_path):
    path = tmp_path / "review.png"

    sheets.write_review([("warrior idle 0", frame((255, 0, 0, 255)))], path)

    assert path.is_file()
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider tests/test_sheets.py`
Expected: FAIL with `ImportError: cannot import name 'sheets'`.

- [ ] **Step 3: Write `mortar_art/sheets.py`**

```python
"""Turn baked frames into per-animation sprite sheets with JSON metadata for Unity."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from PIL import Image

from . import bake_geometry as geo
from .imaging import contact_sheet

Tint = tuple[float, float, float]
NO_TINT: Tint = (1.0, 1.0, 1.0)
REVIEW_ANIMATIONS = ("idle", "attack")


def finish_frame(render: Image.Image, tint: Tint = NO_TINT) -> Image.Image:
    expected = geo.render_size()
    if render.size != expected:
        raise ValueError(f"expected a {expected[0]}x{expected[1]} render, got {render.width}x{render.height}")
    premultiplied = render.convert("RGBA").convert("RGBa")
    small = premultiplied.resize((geo.FRAME_WIDTH, geo.FRAME_HEIGHT), Image.Resampling.BOX)
    return apply_tint(small.convert("RGBA"), tint)


def apply_tint(frame: Image.Image, tint: Tint) -> Image.Image:
    if tint == NO_TINT:
        return frame
    red, green, blue, alpha = frame.convert("RGBA").split()
    scaled = [
        band.point(lambda value, factor=factor: min(255, round(value * factor)))
        for band, factor in zip((red, green, blue), tint)
    ]
    return Image.merge("RGBA", (*scaled, alpha))


def pack_sheet(rows: list[list[Image.Image]]) -> Image.Image:
    if not rows or not rows[0]:
        raise ValueError("a sheet needs at least one frame")
    count = len(rows[0])
    if any(len(row) != count for row in rows):
        raise ValueError("every direction needs the same number of frames")
    sheet = Image.new("RGBA", (count * geo.FRAME_WIDTH, len(rows) * geo.FRAME_HEIGHT), (0, 0, 0, 0))
    for row_index, row in enumerate(rows):
        for column, frame in enumerate(row):
            if frame.size != (geo.FRAME_WIDTH, geo.FRAME_HEIGHT):
                raise ValueError(f"frames must be {geo.FRAME_WIDTH}x{geo.FRAME_HEIGHT}")
            sheet.paste(frame, (column * geo.FRAME_WIDTH, row_index * geo.FRAME_HEIGHT))
    return sheet


def sheet_info(frames: int, directions: int, loop: bool, hit_frame: int) -> dict[str, Any]:
    return {
        "frames": frames,
        "directions": directions,
        "fps": geo.SAMPLE_FPS,
        "loop": loop,
        "hitFrame": hit_frame,
        "frameWidth": geo.FRAME_WIDTH,
        "frameHeight": geo.FRAME_HEIGHT,
        "pivotX": geo.PIVOT_X,
        "pivotY": geo.PIVOT_FROM_BOTTOM,
    }


def write_sheet(sheet: Image.Image, info: dict[str, Any], png_path: Path) -> None:
    png_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(png_path)
    png_path.with_suffix(".json").write_text(json.dumps(info, indent=2) + "\n", encoding="utf-8")


def pack_actor(bake_dir: Path, art_dir: Path, tint: Tint) -> list[Path]:
    summary = json.loads((bake_dir / "frames.json").read_text(encoding="utf-8"))
    written = []
    for animation, meta in summary["animations"].items():
        rows = [
            [finish_frame(_load(bake_dir / animation / str(direction) / f"{index:02d}.png"), tint)
             for index in range(int(meta["frames"]))]
            for direction in summary["directions"]
        ]
        path = art_dir / f"{animation}.png"
        info = sheet_info(int(meta["frames"]), len(rows), animation in geo.LOOPING, int(meta["hitFrame"]))
        write_sheet(pack_sheet(rows), info, path)
        written.append(path)
    return written


def frame_at(sheet: Image.Image, row: int, index: int) -> Image.Image:
    left, top = index * geo.FRAME_WIDTH, row * geo.FRAME_HEIGHT
    return sheet.crop((left, top, left + geo.FRAME_WIDTH, top + geo.FRAME_HEIGHT))


def review_entries(actor_id: str, art_dir: Path) -> list[tuple[str, Image.Image]]:
    entries = []
    for animation in REVIEW_ANIMATIONS:
        png = art_dir / f"{animation}.png"
        if not png.is_file():
            continue
        info = json.loads(png.with_suffix(".json").read_text(encoding="utf-8"))
        index = int(info["hitFrame"]) if animation == "attack" else 0
        sheet = _load(png)
        for row in range(int(info["directions"])):
            entries.append((f"{actor_id} {animation} {row}", frame_at(sheet, row, index)))
    return entries


def write_review(entries: list[tuple[str, Image.Image]], path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    contact_sheet(entries, cell=geo.FRAME_WIDTH, columns=8).save(path)


def _load(path: Path) -> Image.Image:
    with Image.open(path) as image:
        return image.convert("RGBA")
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider`
Expected: all pass. If `test_finishing_keeps_edge_colours_instead_of_darkening_them` fails with a red near 128, the premultiplied round trip is missing; do not loosen the test.

- [ ] **Step 5: Commit**

```powershell
git add tools/art/mortar_art/sheets.py tools/art/tests/test_sheets.py
git commit -m "add sprite sheet packing"
```

---

### Task 5: The Blender bake script, the bake CLI and the warrior entry

**Files:**
- Create: `tools/art/blender/bake_actor.py`, `tools/art/mortar_art/bake_runner.py`, `tools/art/bake.py`, `tools/art/actors.json`
- Test: `tools/art/tests/test_bake_runner.py`, `tools/art/tests/test_bake_cli.py`, `tools/art/tests/test_bake_smoke.py`

**Interfaces:**
- Consumes: everything from Tasks 2–4; models from Task 1.
- Produces: `bake_runner.BakeError`, `bake_runner.blender_command(blender, job_path) -> list[str]`, `bake_runner.run_job(blender, job, work_dir) -> str` (the Blender log text), `bake_runner.failure_message(log_text, code, log_path) -> str`, `bake_runner.prefixed_lines(log_text, prefix) -> list[str]`; CLI `bake.py [--only ID] [--force] [--sheet] [--list-clips FILE ...] [--rehash]`; the Blender script prints `[CLIP] <file>: <name> (<seconds>s)`, `[OBJECT] <file>: <name> (<type>)` and on failure `[BAKE-ERROR] <message>`, and writes `frames.json` as described in Task 4.

- [ ] **Step 1: Write the failing runner and CLI tests**

`tools/art/tests/test_bake_runner.py`:

```python
import json
import subprocess
from pathlib import Path

import pytest

from mortar_art import bake_runner
from mortar_art.bake_runner import BakeError


def test_blender_runs_headless_with_factory_settings_and_the_job():
    command = bake_runner.blender_command(Path("blender.exe"), Path("job.json"))

    assert command[:5] == ["blender.exe", "--background", "--factory-startup", "--python-exit-code", "2"]
    assert command[5:7] == ["--python", str(bake_runner.BAKE_SCRIPT)]
    assert command[7:] == ["--", "job.json"]


def _fake_blender(monkeypatch, output, code):
    def run(command, stdout, stderr, timeout, check):
        stdout.write(output)
        return subprocess.CompletedProcess(command, code)

    monkeypatch.setattr(bake_runner.subprocess, "run", run)


def test_a_successful_job_returns_the_log_and_writes_the_job(monkeypatch, tmp_path):
    _fake_blender(monkeypatch, "[CLIP] a.glb: Idle (1.00s)\n", 0)

    log = bake_runner.run_job(Path("blender.exe"), {"mode": "list", "files": []}, tmp_path)

    assert bake_runner.prefixed_lines(log, "[CLIP]") == ["[CLIP] a.glb: Idle (1.00s)"]
    assert json.loads((tmp_path / "job.json").read_text(encoding="utf-8")) == {"mode": "list", "files": []}


def test_bake_errors_are_reported_with_the_log_path(monkeypatch, tmp_path):
    _fake_blender(monkeypatch, "noise\n[BAKE-ERROR] no clip named 'Idle'\n", 2)

    with pytest.raises(BakeError, match="no clip named 'Idle'") as error:
        bake_runner.run_job(Path("blender.exe"), {"mode": "list", "files": []}, tmp_path)
    assert "blender.log" in str(error.value)


def test_a_crash_without_a_bake_error_reports_the_exit_code():
    message = bake_runner.failure_message("Segmentation fault\n", 11, Path("blender.log"))

    assert message.startswith("Blender exited with code 11")


def test_timeouts_are_reported(monkeypatch, tmp_path):
    def run(command, stdout, stderr, timeout, check):
        raise subprocess.TimeoutExpired(command, timeout)

    monkeypatch.setattr(bake_runner.subprocess, "run", run)

    with pytest.raises(BakeError, match="timed out"):
        bake_runner.run_job(Path("blender.exe"), {"mode": "list", "files": []}, tmp_path)
```

`tools/art/tests/test_bake_cli.py`:

```python
import pytest

import bake
from mortar_art import actors
from mortar_art.bake_runner import BakeError
from mortar_art.blender import BlenderError

DATA = {
    "actors": [
        {"id": "smith", "model": "p/smith.glb", "npc": True, "clips": {"idle": "Idle"}},
        {"id": "healer", "model": "p/healer.glb", "npc": True, "clips": {"idle": "Idle"}},
    ],
    "files": {"p/smith.glb": "0" * 64, "p/healer.glb": "0" * 64},
}


@pytest.fixture
def cli(monkeypatch):
    state = {"baked": [], "fail": set(), "baked_already": set()}
    monkeypatch.setattr(bake.actors, "load_manifest", lambda: actors.parse_manifest(DATA))
    monkeypatch.setattr(bake, "is_baked", lambda actor: actor.id in state["baked_already"])
    monkeypatch.setattr(bake, "find_blender", lambda environ: "blender.exe")

    def bake_actor(blender, manifest, actor):
        if actor.id in state["fail"]:
            raise BakeError("no clip named 'Idle'")
        state["baked"].append(actor.id)

    monkeypatch.setattr(bake, "bake_actor", bake_actor)
    return state


def test_every_actor_is_baked_by_default(cli):
    assert bake.main([]) == 0
    assert cli["baked"] == ["smith", "healer"]


def test_only_bakes_one_actor(cli):
    assert bake.main(["--only", "healer"]) == 0
    assert cli["baked"] == ["healer"]


def test_an_unknown_actor_is_an_error(cli, capsys):
    assert bake.main(["--only", "dragon"]) == 1
    assert "no actor 'dragon'" in capsys.readouterr().err


def test_baked_actors_are_skipped_without_needing_blender(cli, monkeypatch, capsys):
    cli["baked_already"].update({"smith", "healer"})

    def no_blender(environ):
        raise BlenderError("not installed")

    monkeypatch.setattr(bake, "find_blender", no_blender)

    assert bake.main([]) == 0
    assert "already baked" in capsys.readouterr().out


def test_force_rebakes_baked_actors(cli):
    cli["baked_already"].add("smith")

    assert bake.main(["--force"]) == 0
    assert cli["baked"] == ["smith", "healer"]


def test_one_failure_does_not_stop_the_others(cli, capsys):
    cli["fail"].add("smith")

    assert bake.main([]) == 1
    assert cli["baked"] == ["healer"]
    assert "smith: no clip named 'Idle'" in capsys.readouterr().err


def test_rehash_records_hashes_and_bakes_nothing(cli, monkeypatch):
    monkeypatch.setattr(bake.actors, "record_hashes", lambda: 3)

    assert bake.main(["--rehash"]) == 0
    assert cli["baked"] == []
```

- [ ] **Step 2: Run them to verify they fail**

Run: `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider tests/test_bake_runner.py tests/test_bake_cli.py`
Expected: FAIL with `ImportError` for `bake_runner` and `ModuleNotFoundError: No module named 'bake'`.

- [ ] **Step 3: Write `mortar_art/bake_runner.py`**

```python
"""Run Blender headless for one bake or clip-listing job and collect its errors."""

from __future__ import annotations

import json
import subprocess
from pathlib import Path
from typing import Any

from .config import REPO_ROOT

BAKE_SCRIPT = REPO_ROOT / "tools" / "art" / "blender" / "bake_actor.py"
TIMEOUT_SECONDS = 3600
ERROR_PREFIX = "[BAKE-ERROR] "


class BakeError(Exception):
    pass


def blender_command(blender: Path, job_path: Path) -> list[str]:
    return [
        str(blender), "--background", "--factory-startup", "--python-exit-code", "2",
        "--python", str(BAKE_SCRIPT), "--", str(job_path),
    ]


def run_job(blender: Path, job: dict[str, Any], work_dir: Path) -> str:
    work_dir.mkdir(parents=True, exist_ok=True)
    job_path = work_dir / "job.json"
    log_path = work_dir / "blender.log"
    job_path.write_text(json.dumps(job, indent=2) + "\n", encoding="utf-8")
    with log_path.open("w", encoding="utf-8", errors="replace") as log:
        try:
            result = subprocess.run(
                blender_command(blender, job_path), stdout=log, stderr=subprocess.STDOUT,
                timeout=TIMEOUT_SECONDS, check=False,
            )
        except subprocess.TimeoutExpired as error:
            raise BakeError(f"Blender timed out after {TIMEOUT_SECONDS}s (log: {log_path})") from error
    text = log_path.read_text(encoding="utf-8", errors="replace")
    if result.returncode != 0:
        raise BakeError(failure_message(text, result.returncode, log_path))
    return text


def failure_message(log_text: str, code: int, log_path: Path) -> str:
    errors = [line[len(ERROR_PREFIX):].strip() for line in prefixed_lines(log_text, ERROR_PREFIX)]
    detail = "; ".join(errors) if errors else f"Blender exited with code {code}"
    return f"{detail} (log: {log_path})"


def prefixed_lines(log_text: str, prefix: str) -> list[str]:
    return [line for line in log_text.splitlines() if line.startswith(prefix)]
```

- [ ] **Step 4: Write `tools/art/bake.py`**

```python
"""Bake animated 8-direction actor sprite sheets from CC0 3D models with Blender."""

from __future__ import annotations

import argparse
import os
import shutil
import sys
from pathlib import Path

from mortar_art import actors, bake_runner, sheets
from mortar_art.blender import BlenderError, find_blender
from mortar_art.config import REPO_ROOT
from mortar_art.jobs import ART_ROOT

BAKE_ROOT = REPO_ROOT / "tools" / "art" / "out" / "bake"
REVIEW_PATH = REPO_ROOT / "tools" / "art" / "out" / "sheets" / "actors-baked.png"


def parse_args(argv: list[str] | None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--only", help="bake one actor id, e.g. warrior")
    parser.add_argument("--force", action="store_true", help="re-bake actors whose sheets already exist")
    parser.add_argument("--sheet", action="store_true", help="write a review contact sheet to tools/art/out/sheets")
    parser.add_argument("--list-clips", nargs="+", metavar="FILE",
                        help="print the clips and objects in files under tools/art/models")
    parser.add_argument("--rehash", action="store_true",
                        help="record the sha256 of every file under tools/art/models in actors.json")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        if args.rehash:
            print(f"[BAKE] Recorded {actors.record_hashes()} file hashes")
            return 0
        if args.list_clips:
            list_clips(args.list_clips)
            return 0
        manifest = actors.load_manifest()
        selected = select(manifest, args.only)
        failures = bake_all(manifest, selected, args.force)
        if args.sheet:
            write_review(selected)
    except (actors.ManifestError, BlenderError, bake_runner.BakeError) as error:
        print(f"[ERROR] {error}", file=sys.stderr)
        return 1
    return 1 if failures else 0


def select(manifest: actors.Manifest, only: str | None) -> list[actors.ActorSpec]:
    if only is None:
        return list(manifest.actors.values())
    if only not in manifest.actors:
        raise actors.ManifestError(f"no actor '{only}' in actors.json")
    return [manifest.actors[only]]


def is_baked(actor: actors.ActorSpec) -> bool:
    return all((ART_ROOT / "actors" / actor.id / f"{name}.png").is_file() for name in actor.wanted)


def bake_all(manifest: actors.Manifest, selected: list[actors.ActorSpec], force: bool) -> list[str]:
    pending = [actor for actor in selected if force or not is_baked(actor)]
    if not pending:
        print("[BAKE] Everything selected is already baked")
        return []
    blender = find_blender(os.environ)
    failures = []
    for index, actor in enumerate(pending, 1):
        print(f"[BAKE] {index}/{len(pending)} {actor.id}")
        try:
            bake_actor(blender, manifest, actor)
        except (actors.ManifestError, bake_runner.BakeError, OSError, ValueError) as error:
            print(f"[ERROR] {actor.id}: {error}", file=sys.stderr)
            failures.append(actor.id)
    return failures


def bake_actor(blender: Path, manifest: actors.Manifest, actor: actors.ActorSpec) -> None:
    actors.check_files(manifest, actor)
    work = BAKE_ROOT / actor.id
    shutil.rmtree(work, ignore_errors=True)
    bake_runner.run_job(blender, actors.build_job(actor, work), work)
    for path in sheets.pack_actor(work, ART_ROOT / "actors" / actor.id, actor.tint):
        print(f"[BAKE] Wrote {path.relative_to(REPO_ROOT)}")


def list_clips(names: list[str]) -> None:
    paths = [actors.MODELS_ROOT / name for name in names]
    missing = [str(path) for path in paths if not path.is_file()]
    if missing:
        raise actors.ManifestError(f"not found: {', '.join(missing)}")
    log = bake_runner.run_job(find_blender(os.environ), actors.list_job(paths), BAKE_ROOT / "_list")
    for line in bake_runner.prefixed_lines(log, "[CLIP]") + bake_runner.prefixed_lines(log, "[OBJECT]"):
        print(line)


def write_review(selected: list[actors.ActorSpec]) -> None:
    entries = [
        entry for actor in selected for entry in sheets.review_entries(actor.id, ART_ROOT / "actors" / actor.id)
    ]
    if not entries:
        print("[BAKE] No baked sheets to review")
        return
    sheets.write_review(entries, REVIEW_PATH)
    print(f"[BAKE] Wrote {REVIEW_PATH.relative_to(REPO_ROOT)}")


if __name__ == "__main__":
    sys.exit(main())
```

- [ ] **Step 5: Run the runner and CLI tests**

Run: `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider tests/test_bake_runner.py tests/test_bake_cli.py`
Expected: PASS.

- [ ] **Step 6: Write `tools/art/blender/bake_actor.py`**

```python
"""Blender side of the actor bake: renders the frames a job file asks for.

bake.py runs: blender --background --factory-startup --python-exit-code 2 --python bake_actor.py -- job.json
"""

import json
import math
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from mortar_art import bake_geometry as geo  # noqa: E402

KEY_LIGHT = ("Key", 4.0, (math.radians(50.0), 0.0, math.radians(10.0)))
FILL_LIGHT = ("Fill", 1.0, (math.radians(70.0), 0.0, math.radians(200.0)))
AMBIENT = (0.32, 0.32, 0.36, 1.0)


class BakeFailure(Exception):
    pass


def main() -> None:
    job = json.loads(Path(sys.argv[sys.argv.index("--") + 1]).read_text(encoding="utf-8"))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    try:
        if job["mode"] == "list":
            list_files(job["files"])
        else:
            bake(job)
    except (BakeFailure, ValueError) as failure:
        print(f"[BAKE-ERROR] {failure}", flush=True)
        sys.exit(2)


def scene_fps() -> float:
    render = bpy.context.scene.render
    return render.fps / render.fps_base


def import_file(path: str) -> list:
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    for action in bpy.data.actions:
        action.use_fake_user = True
    return [obj for obj in bpy.data.objects if obj not in before]


def list_files(paths: list) -> None:
    for path in paths:
        before = set(bpy.data.actions)
        objects = import_file(path)
        name = Path(path).name
        for action in (action for action in bpy.data.actions if action not in before):
            print(f"[CLIP] {name}: {action.name} ({clip_seconds(action):.2f}s)")
        for obj in objects:
            print(f"[OBJECT] {name}: {obj.name} ({obj.type})")


def bake(job: dict) -> None:
    scene = bpy.context.scene
    setup_render(scene)
    pivot, armature = load_model(job)
    for path in job["animations"]:
        for obj in import_file(path):
            bpy.data.objects.remove(obj, do_unlink=True)
    out = Path(job["out"])
    summary = {"directions": job["directions"], "animations": {}}
    for animation, clip_name in job["clips"].items():
        action = find_action(clip_name)
        times = geo.sample_times(clip_seconds(action), animation in geo.LOOPING)
        if job["maxFrames"]:
            times = times[: job["maxFrames"]]
        play(armature, action)
        for direction in job["directions"]:
            pivot.rotation_euler = (0.0, 0.0, math.radians(geo.model_rotation_degrees(direction, job["facing"])))
            for index, seconds in enumerate(times):
                render_frame(scene, action, seconds, out / animation / str(direction) / f"{index:02d}.png")
        hit = geo.hit_frame_index(job["attackHitSeconds"], len(times)) if animation == "attack" else 0
        summary["animations"][animation] = {"frames": len(times), "hitFrame": hit}
    (out / "frames.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")


def load_model(job: dict) -> tuple:
    objects = import_file(job["model"])
    armatures = [obj for obj in objects if obj.type == "ARMATURE"]
    if not armatures:
        raise BakeFailure(f"{Path(job['model']).name} has no armature")
    hide(objects, job["hide"])
    pivot = bpy.data.objects.new("BakePivot", None)
    bpy.context.scene.collection.objects.link(pivot)
    for obj in objects:
        if obj.parent is None:
            obj.parent = pivot
    pivot.scale = (job["scale"], job["scale"], job["scale"])
    return pivot, armatures[0]


def hide(objects: list, names: list) -> None:
    by_name = {obj.name: obj for obj in objects}
    unknown = [name for name in names if name not in by_name]
    if unknown:
        raise BakeFailure(f"unknown objects to hide: {', '.join(unknown)}; the model has {', '.join(sorted(by_name))}")
    for name in names:
        for obj in [by_name[name], *by_name[name].children_recursive]:
            obj.hide_render = True


def find_action(name: str):
    action = bpy.data.actions.get(name)
    if action is None:
        known = ", ".join(sorted(action.name for action in bpy.data.actions))
        raise BakeFailure(f"no clip named '{name}'; available: {known}")
    return action


def clip_seconds(action) -> float:
    start, end = action.frame_range
    return max(0.0, end - start) / scene_fps()


def play(armature, action) -> None:
    data = armature.animation_data or armature.animation_data_create()
    for track in data.nla_tracks:
        track.mute = True
    data.action = action
    slot = next((slot for slot in action.slots if slot.target_id_type == "OBJECT"), None)
    if slot is not None:
        data.action_slot = slot


def render_frame(scene, action, seconds: float, path: Path) -> None:
    frame = action.frame_range[0] + seconds * scene_fps()
    whole = math.floor(frame)
    scene.frame_set(whole, subframe=frame - whole)
    path.parent.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def setup_render(scene) -> None:
    width, height = geo.render_size()
    render = scene.render
    render.engine = eevee_engine()
    render.resolution_x, render.resolution_y, render.resolution_percentage = width, height, 100
    render.film_transparent = True
    render.image_settings.file_format = "PNG"
    render.image_settings.color_mode = "RGBA"
    scene.view_settings.view_transform = "Standard"
    add_camera(scene)
    for name, energy, rotation in (KEY_LIGHT, FILL_LIGHT):
        light = bpy.data.lights.new(name, "SUN")
        light.energy = energy
        obj = bpy.data.objects.new(name, light)
        obj.rotation_euler = rotation
        scene.collection.objects.link(obj)
    add_world(scene)


def eevee_engine() -> str:
    engines = {item.identifier for item in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items}
    return "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"


def add_camera(scene) -> None:
    data = bpy.data.cameras.new("BakeCamera")
    data.type = "ORTHO"
    data.ortho_scale = geo.ortho_scale()
    data.shift_x, data.shift_y = geo.camera_shift()
    data.clip_end = geo.CAMERA_DISTANCE * 3
    camera = bpy.data.objects.new("BakeCamera", data)
    scene.collection.objects.link(camera)
    camera.location = geo.camera_location()
    camera.rotation_euler = geo.camera_rotation_radians()
    scene.camera = camera


def add_world(scene) -> None:
    world = bpy.data.worlds.new("BakeWorld")
    world.use_nodes = True
    background = world.node_tree.nodes["Background"]
    background.inputs["Color"].default_value = AMBIENT
    background.inputs["Strength"].default_value = 1.0
    scene.world = world


if __name__ == "__main__":
    main()
```

- [ ] **Step 7: Find the warrior's clip and object names**

Run (from `tools/art`), adjusting the file names to what Task 1 committed:

```powershell
.venv\Scripts\python bake.py --list-clips kaykit-adventurers/Knight.glb kaykit-animations/Rig_Medium_General.glb kaykit-animations/Rig_Medium_MovementBasic.glb kaykit-animations/Rig_Medium_CombatMelee.glb
```

Expected: `[CLIP]` lines with names and durations, `[OBJECT]` lines with the Knight's meshes. If the glTF importer names actions after the target object (for example `Idle_A_Rig`), use the exact printed names.

- [ ] **Step 8: Write `tools/art/actors.json` with the warrior and record hashes**

Use the printed names: an idle loop, a forward walk, a one-handed melee attack, a light hit reaction, and a death that ends lying down. Hide every accessory object except one one-handed sword and one shield. Set `attackHitSeconds` to the moment the blade is fully extended (open the attack clip in Blender, or start at 40% of its duration).

```json
{
  "actors": [
    {
      "id": "warrior",
      "model": "kaykit-adventurers/Knight.glb",
      "animations": [
        "kaykit-animations/Rig_Medium_General.glb",
        "kaykit-animations/Rig_Medium_MovementBasic.glb",
        "kaykit-animations/Rig_Medium_CombatMelee.glb"
      ],
      "hide": ["<accessory objects to hide, from Step 7>"],
      "clips": { "idle": "<name>", "walk": "<name>", "attack": "<name>", "hit": "<name>", "death": "<name>" },
      "attackHitSeconds": 0.4,
      "scale": 1.0,
      "facing": 0,
      "tint": [1.0, 1.0, 1.0]
    }
  ],
  "files": {}
}
```

Every `<...>` must be replaced with real names from Step 7 before continuing. Then run `.venv\Scripts\python bake.py --rehash` → `[BAKE] Recorded N file hashes`.

- [ ] **Step 9: Write the smoke test**

`tools/art/tests/test_bake_smoke.py`:

```python
import os

import pytest
from PIL import Image

from mortar_art import actors, bake_runner
from mortar_art import bake_geometry as geo
from mortar_art.blender import BlenderError, find_blender


def _blender():
    try:
        return find_blender(os.environ)
    except BlenderError:
        return None


BLENDER = _blender()


@pytest.mark.skipif(BLENDER is None, reason="Blender is not installed")
def test_the_warrior_idle_bakes_with_real_transparency_and_feet_on_the_pivot(tmp_path):
    manifest = actors.load_manifest()
    warrior = manifest.actors["warrior"]
    actors.check_files(manifest, warrior)
    job = actors.build_job(warrior, tmp_path, animations=("idle",), directions=(0,), max_frames=1)

    bake_runner.run_job(BLENDER, job, tmp_path)

    with Image.open(tmp_path / "idle" / "0" / "00.png") as image:
        frame = image.convert("RGBA")
    width, height = frame.size
    alpha = frame.getchannel("A")
    assert (width, height) == geo.render_size()
    for corner in ((0, 0), (width - 1, 0), (0, height - 1), (width - 1, height - 1)):
        assert alpha.getpixel(corner) == 0
    box = alpha.getbbox()
    assert box is not None
    feet_row = height - geo.PIVOT_FROM_BOTTOM * geo.RENDER_SCALE
    assert abs(box[3] - feet_row) <= 8 * geo.RENDER_SCALE
```

- [ ] **Step 10: Run the whole suite, including the smoke test**

Run: `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider`
Expected: all pass, smoke test included (it takes 10–60 seconds). If the smoke test fails, read `blender.log` in the test's tmp folder. Common causes: wrong clip or object names (fix `actors.json`); an armature nested in an empty (still fine, since the pivot parents top-level objects); an unexpected Blender API name. Fix `bake_actor.py` and keep the test unchanged. If the feet sit too high or low only because the model's origin is not at its feet, report it instead of changing `PIVOT_FROM_BOTTOM`.

- [ ] **Step 11: Commit**

```powershell
git add tools/art/bake.py tools/art/blender/bake_actor.py tools/art/mortar_art/bake_runner.py tools/art/actors.json tools/art/tests/test_bake_runner.py tools/art/tests/test_bake_cli.py tools/art/tests/test_bake_smoke.py
git commit -m "add blender actor bake"
```

---

### Task 6: Actor clips in the sprite catalog

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Art/ClipInfo.cs`, `Runtime/Art/ActorClip.cs`, `Runtime/Art/ClipNames.cs`, `Tests/Editor/TestClipInfo.cs`, `Tests/Editor/ActorClipTests.cs`
- Modify: `Runtime/Art/SpriteKeys.cs`, `Runtime/Art/SpriteCatalog.cs`, `Editor/ArtImport.cs`, `Editor/SpriteCatalogBuilder.cs`, `Tests/Editor/SpriteCatalogTests.cs`, `Tests/Editor/ArtImportTests.cs`

(All paths below are under `unity/com.mortar.unity/`.)

**Interfaces:**
- Produces: `ClipInfo` (`FromJson(string) -> ClipInfo`, properties `Frames, Directions, Fps, Loop, HitFrame, FrameWidth, FrameHeight, PivotX, PivotY, Seconds`); `ActorClip(string key, Texture2D? sheet, ClipInfo info)` with `Key, Frames, Fps, Loop, HitFrame, Seconds, Directions`, `Frame(Direction, int) -> Sprite`, `const float PixelsPerUnit = 128f`; `ClipNames.Idle|Walk|Attack|Hit|Death`; `SpriteKeys.Clip(string id, string clip) -> "actors/{id}/{clip}"`; `SpriteCatalog.SetClips(IEnumerable<ActorClip>)`, `SpriteCatalog.Clip(string spriteSet, string clipName) -> ActorClip?`, `SpriteCatalog.Clips`, `SpriteCatalog.ClipCount`; `ArtImport.IsSheet(string assetPath)` and `IsSheet(string assetPath, Func<string, bool> fileExists)`.

- [ ] **Step 1: Write the test helper and failing tests**

`Tests/Editor/TestClipInfo.cs`:

```csharp
namespace Mortar.Unity.Tests
{
    public static class TestClipInfo
    {
        public const int FrameWidth = 4;
        public const int FrameHeight = 6;

        public static ClipInfo Make(int frames, int directions, int hitFrame = 0, bool loop = false, float fps = 10f) =>
            ClipInfo.FromJson(
                $"{{\"frames\":{frames},\"directions\":{directions},\"fps\":{fps},\"loop\":{(loop ? "true" : "false")}," +
                $"\"hitFrame\":{hitFrame},\"frameWidth\":{FrameWidth},\"frameHeight\":{FrameHeight},\"pivotX\":2,\"pivotY\":1}}");
    }
}
```

`Tests/Editor/ActorClipTests.cs`:

```csharp
using System;
using Mortar.Game;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mortar.Unity.Tests
{
    public sealed class ActorClipTests
    {
        private Texture2D _sheet = null!;
        private ActorClip _clip = null!;

        [SetUp]
        public void SetUp()
        {
            _sheet = new Texture2D(3 * TestClipInfo.FrameWidth, 8 * TestClipInfo.FrameHeight);
            _clip = new ActorClip("actors/warrior/attack", _sheet, TestClipInfo.Make(3, 8, hitFrame: 1));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_sheet);

        [Test]
        public void SouthIsTheTopRow()
        {
            Assert.AreEqual(new Rect(0, 42, 4, 6), _clip.Frame(Direction.South, 0).rect);
        }

        [Test]
        public void SouthEastIsTheBottomRow()
        {
            Assert.AreEqual(new Rect(8, 0, 4, 6), _clip.Frame(Direction.SouthEast, 2).rect);
        }

        [Test]
        public void PivotIsMeasuredFromTheBottomLeftOfTheFrame()
        {
            var sprite = _clip.Frame(Direction.West, 1);

            Assert.AreEqual(new Vector2(2, 1), sprite.pivot);
            Assert.AreEqual(ActorClip.PixelsPerUnit, sprite.pixelsPerUnit);
        }

        [Test]
        public void FramesAreCreatedOnce()
        {
            Assert.AreSame(_clip.Frame(Direction.North, 1), _clip.Frame(Direction.North, 1));
        }

        [Test]
        public void IndexIsClampedToTheClip()
        {
            Assert.AreEqual(8f, _clip.Frame(Direction.South, 9).rect.x);
            Assert.AreEqual(0f, _clip.Frame(Direction.South, -1).rect.x);
        }

        [Test]
        public void SingleRowClipsUseTheirRowForEveryDirection()
        {
            var sheet = new Texture2D(2 * TestClipInfo.FrameWidth, TestClipInfo.FrameHeight);
            var clip = new ActorClip("actors/smith/idle", sheet, TestClipInfo.Make(2, 1, loop: true));

            Assert.AreEqual(new Rect(4, 0, 4, 6), clip.Frame(Direction.North, 1).rect);
            Object.DestroyImmediate(sheet);
        }

        [Test]
        public void MissingSheetFallsBackToTheActorPlaceholder()
        {
            var clip = new ActorClip("actors/warrior/idle", null, TestClipInfo.Make(2, 8));

            Assert.AreSame(GeneratedSprites.Actor, clip.Frame(Direction.South, 0));
        }

        [Test]
        public void MetadataComesFromTheSidecar()
        {
            Assert.AreEqual(3, _clip.Frames);
            Assert.AreEqual(1, _clip.HitFrame);
            Assert.AreEqual(0.3f, _clip.Seconds, 1e-5f);
            Assert.IsFalse(_clip.Loop);
        }

        [TestCase("{\"frames\":2,\"directions\":3,\"fps\":10,\"frameWidth\":4,\"frameHeight\":6}")]
        [TestCase("{\"frames\":0,\"directions\":8,\"fps\":10,\"frameWidth\":4,\"frameHeight\":6}")]
        [TestCase("{\"frames\":2,\"directions\":8,\"fps\":0,\"frameWidth\":4,\"frameHeight\":6}")]
        public void BadMetadataIsRejected(string json)
        {
            Assert.Throws<FormatException>(() => ClipInfo.FromJson(json));
        }
    }
}
```

Add to `Tests/Editor/SpriteCatalogTests.cs`:

```csharp
        [Test]
        public void ClipsResolveBySpriteSetAndName()
        {
            var idle = new ActorClip("actors/warrior/idle", null, TestClipInfo.Make(2, 8, loop: true));
            _catalog.SetClips(new[] { idle });

            Assert.AreSame(idle, _catalog.Clip("warrior", ClipNames.Idle));
            Assert.IsNull(_catalog.Clip("warrior", ClipNames.Walk));
            Assert.IsNull(_catalog.Clip("zombie", ClipNames.Idle));
            Assert.AreEqual(1, _catalog.ClipCount);
            LogAssert.NoUnexpectedReceived();
        }
```

Add to `Tests/Editor/ArtImportTests.cs`:

```csharp
        [Test]
        public void ActorPngsWithAJsonSidecarAreSheets()
        {
            Assert.IsTrue(ArtImport.IsSheet("Assets/Art/actors/warrior/idle.png", path => path == "Assets/Art/actors/warrior/idle.json"));
        }

        [Test]
        public void PngsWithoutASidecarAreNotSheets()
        {
            Assert.IsFalse(ArtImport.IsSheet("Assets/Art/actors/warrior.png", path => false));
        }

        [TestCase("Assets/Art/icons/gold.png")]
        [TestCase("Assets/Other/actors/warrior/idle.png")]
        public void OnlyActorArtCanBeASheet(string path)
        {
            Assert.IsFalse(ArtImport.IsSheet(path, candidate => true));
        }
```

- [ ] **Step 2: Run the EditMode tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: compile failure naming `ClipInfo`, `ActorClip`, `ClipNames`, `SetClips` or `IsSheet` (see `out/unity-tests/EditMode.log`).

- [ ] **Step 3: Write `Runtime/Art/ClipInfo.cs`**

```csharp
using System;
using UnityEngine;

namespace Mortar.Unity
{
    [Serializable]
    public sealed class ClipInfo
    {
        // Field names match the JSON keys written by tools/art/mortar_art/sheets.py.
        [SerializeField] private int frames = 0;
        [SerializeField] private int directions = 0;
        [SerializeField] private float fps = 0f;
        [SerializeField] private bool loop = false;
        [SerializeField] private int hitFrame = 0;
        [SerializeField] private int frameWidth = 0;
        [SerializeField] private int frameHeight = 0;
        [SerializeField] private int pivotX = 0;
        [SerializeField] private int pivotY = 0;

        public int Frames => frames;

        public int Directions => directions;

        public float Fps => fps;

        public bool Loop => loop;

        public int HitFrame => hitFrame;

        public int FrameWidth => frameWidth;

        public int FrameHeight => frameHeight;

        public int PivotX => pivotX;

        public int PivotY => pivotY;

        public float Seconds => fps > 0f ? frames / fps : 0f;

        public static ClipInfo FromJson(string json)
        {
            var info = JsonUtility.FromJson<ClipInfo>(json);
            if (info == null || info.frames < 1 || (info.directions != 1 && info.directions != 8)
                || info.fps <= 0f || info.frameWidth < 1 || info.frameHeight < 1)
            {
                throw new FormatException("sprite sheet metadata needs frames >= 1, 1 or 8 directions, fps > 0 and a frame size");
            }
            return info;
        }
    }
}
```

- [ ] **Step 4: Write `Runtime/Art/ClipNames.cs` and `Runtime/Art/ActorClip.cs`**

```csharp
namespace Mortar.Unity
{
    public static class ClipNames
    {
        public const string Idle = "idle";
        public const string Walk = "walk";
        public const string Attack = "attack";
        public const string Hit = "hit";
        public const string Death = "death";
    }
}
```

```csharp
using System;
using Mortar.Game;
using UnityEngine;

namespace Mortar.Unity
{
    [Serializable]
    public sealed class ActorClip
    {
        public const float PixelsPerUnit = 128f;

        [SerializeField] private string _key = "";
        [SerializeField] private Texture2D? _sheet;
        [SerializeField] private ClipInfo _info = new ClipInfo();

        [NonSerialized] private Sprite?[]? _sprites;

        public ActorClip(string key, Texture2D? sheet, ClipInfo info)
        {
            _key = key;
            _sheet = sheet;
            _info = info;
        }

        public string Key => _key;

        public int Frames => _info.Frames;

        public int Directions => _info.Directions;

        public float Fps => _info.Fps;

        public bool Loop => _info.Loop;

        public int HitFrame => _info.HitFrame;

        public float Seconds => _info.Seconds;

        public Sprite Frame(Direction direction, int index)
        {
            if (_sheet == null)
            {
                return GeneratedSprites.Actor;
            }
            var row = _info.Directions == 1 ? 0 : (int)direction;
            var column = Mathf.Clamp(index, 0, _info.Frames - 1);
            if (_sprites == null)
            {
                _sprites = new Sprite?[_info.Directions * _info.Frames];
            }
            var slot = row * _info.Frames + column;
            var sprite = _sprites[slot];
            if (sprite == null)
            {
                sprite = CreateSprite(_sheet, row, column);
                _sprites[slot] = sprite;
            }
            return sprite;
        }

        private Sprite CreateSprite(Texture2D sheet, int row, int column)
        {
            var width = _info.FrameWidth;
            var height = _info.FrameHeight;
            var rect = new Rect(column * width, sheet.height - (row + 1) * height, width, height);
            var pivot = new Vector2((float)_info.PivotX / width, (float)_info.PivotY / height);
            var sprite = Sprite.Create(sheet, rect, pivot, PixelsPerUnit);
            sprite.name = $"{_key}/{row}/{column}";
            return sprite;
        }
    }
}
```

- [ ] **Step 5: Add the key and catalog lookups**

In `Runtime/Art/SpriteKeys.cs` add:

```csharp
        public static string Clip(string id, string clip) => $"actors/{id}/{clip}";
```

In `Runtime/Art/SpriteCatalog.cs` add fields after `_entries`:

```csharp
        [SerializeField] private List<ActorClip> _clips = new List<ActorClip>();
```

and after `_lookup`:

```csharp
        private Dictionary<string, ActorClip>? _clipLookup;
```

Add members after `Entries`:

```csharp
        public int ClipCount => _clips.Count;

        public IReadOnlyList<ActorClip> Clips => _clips;

        public void SetClips(IEnumerable<ActorClip> clips)
        {
            _clips = clips.OrderBy(clip => clip.Key, StringComparer.Ordinal).ToList();
            _clipLookup = null;
        }

        public ActorClip? Clip(string spriteSet, string clipName)
        {
            if (_clipLookup == null)
            {
                _clipLookup = new Dictionary<string, ActorClip>(StringComparer.Ordinal);
                foreach (var clip in _clips)
                {
                    _clipLookup[clip.Key] = clip;
                }
            }
            return _clipLookup.TryGetValue(SpriteKeys.Clip(spriteSet, clipName), out var found) ? found : null;
        }
```

Replace `private void OnValidate() => _lookup = null;` with:

```csharp
        private void OnValidate()
        {
            _lookup = null;
            _clipLookup = null;
        }
```

- [ ] **Step 6: Import sheets as readable, full-size textures**

Replace `Editor/ArtImport.cs` with:

```csharp
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mortar.Unity.EditorTools
{
    public sealed class ArtImport : AssetPostprocessor
    {
        public const int MaxSheetSize = 8192;

        private void OnPreprocessTexture()
        {
            if (SpriteCatalogBuilder.KeyFor(assetPath, SpriteCatalogBuilder.ArtRoot) == null)
            {
                return;
            }
            var importer = (TextureImporter)assetImporter;
            if (IsSheet(assetPath))
            {
                ConfigureSheet(importer);
                return;
            }
            importer.GetSourceTextureWidthAndHeight(out var width, out var height);
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = PivotFor(assetPath, width, height);
            settings.spritePixelsPerUnit = GeneratedSprites.PixelsPerUnit;
            importer.SetTextureSettings(settings);
        }

        public static bool IsSheet(string assetPath) => IsSheet(assetPath, File.Exists);

        public static bool IsSheet(string assetPath, Func<string, bool> fileExists)
        {
            var key = SpriteCatalogBuilder.KeyFor(assetPath, SpriteCatalogBuilder.ArtRoot);
            return key != null && key.StartsWith("actors/", StringComparison.Ordinal)
                && fileExists(Path.ChangeExtension(assetPath, ".json"));
        }

        public static Vector2 PivotFor(string assetPath, int width, int height)
        {
            var key = SpriteCatalogBuilder.KeyFor(assetPath, SpriteCatalogBuilder.ArtRoot) ?? "";
            if (height <= 0)
            {
                return new Vector2(0.5f, 0.5f);
            }
            if (key.StartsWith("tiles/", StringComparison.Ordinal))
            {
                return new Vector2(0.5f, Mathf.Min(0.5f, 16f / height));
            }
            if (key.StartsWith("actors/", StringComparison.Ordinal))
            {
                return new Vector2(0.5f, 4f / height);
            }
            return new Vector2(0.5f, 0.5f);
        }

        private static void ConfigureSheet(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = MaxSheetSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
```

- [ ] **Step 7: Register clips in the catalog builder**

In `Editor/SpriteCatalogBuilder.cs` add `using System.IO;`, then replace the body of `Rebuild()` from `var entries = ...` through `catalog.SetEntries(entries);` with:

```csharp
            catalog.SetEntries(FindSprites());
            catalog.SetClips(FindClips());
```

and add:

```csharp
        private static List<SpriteEntry> FindSprites()
        {
            var entries = new List<SpriteEntry>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { ArtRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var key = KeyFor(path, ArtRoot);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (key != null && sprite != null)
                {
                    entries.Add(new SpriteEntry(key, sprite));
                }
            }
            return entries;
        }

        private static List<ActorClip> FindClips()
        {
            var clips = new List<ActorClip>();
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { ArtRoot }))
            {
                var jsonPath = AssetDatabase.GUIDToAssetPath(guid);
                var sheetPath = Path.ChangeExtension(jsonPath, ".png").Replace('\\', '/');
                if (!jsonPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || !ArtImport.IsSheet(sheetPath))
                {
                    continue;
                }
                var key = KeyFor(sheetPath, ArtRoot);
                var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
                if (key != null && sheet != null && text != null)
                {
                    clips.Add(new ActorClip(key, sheet, ClipInfo.FromJson(text.text)));
                }
            }
            return clips;
        }
```

Change `RebuildFromMenu`'s log line to:

```csharp
            Debug.Log($"Mortar: sprite catalog has {catalog.Count} sprites and {catalog.ClipCount} clips");
```

- [ ] **Step 8: Run the tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode` then `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: EditMode all pass (88 before + 16 new); PlayMode 44 passed + 1 skipped. Check `out/unity-tests/EditMode.log` has no `warning CS`.

- [ ] **Step 9: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Art unity/com.mortar.unity/Editor/ArtImport.cs unity/com.mortar.unity/Editor/SpriteCatalogBuilder.cs unity/com.mortar.unity/Tests/Editor
git status --short
git commit -m "add actor clips to the sprite catalog"
```

Stage the new `.cs.meta` files Unity generated next to new scripts as well (`git status` lists them).

---

### Task 7: Clip sets and the actor animator

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Art/ClipSet.cs`, `Runtime/Views/ActorAnimator.cs`, `Runtime/Views/AnimMoment.cs`, `Runtime/Views/ClipFrame.cs`, `Tests/Editor/ClipSetTests.cs`, `Tests/Editor/ActorAnimatorTests.cs`

**Interfaces:**
- Consumes: `ActorClip`, `ClipNames`, `SpriteCatalog.Clip`, `ActorPose.Progress(int elapsed, int duration, float alpha)`, `Mortar.Game.Simulation.ActionKind` (`Idle, Moving, Attacking, Recovering`).
- Produces: `ClipSet(IReadOnlyDictionary<string, ActorClip>)` (throws `ArgumentException` without idle), `ClipSet.For(SpriteCatalog, string spriteSet) -> ClipSet?`, `Has(string)`, `Get(string) -> ActorClip` (falls back to idle); `AnimMoment` struct with public fields `Action, Elapsed, Duration, HitTick, Alpha, Now, HitAt, DiedAt, IsDead`; `ClipFrame(string name, int index)` with `Name`, `Index`; `ActorAnimator.Pick(ClipSet, AnimMoment) -> ClipFrame`, `LoopFrame(float seconds, float fps, int frames)`, `OnceFrame(float seconds, float fps, int frames)`, `ProgressFrame(float progress, int frames)`, `AttackFrame(float ticks, int attackTicks, int hitTick, int frames, int hitFrame)`.

- [ ] **Step 1: Write the failing tests**

`Tests/Editor/ClipSetTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mortar.Unity.Tests
{
    public sealed class ClipSetTests
    {
        private SpriteCatalog _catalog = null!;

        [SetUp]
        public void SetUp() => _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_catalog);

        private static ActorClip Clip(string set, string name, int frames = 2) =>
            new ActorClip(SpriteKeys.Clip(set, name), null, TestClipInfo.Make(frames, 8));

        [Test]
        public void NoIdleClipMeansNoSet()
        {
            _catalog.SetClips(new[] { Clip("zombie", ClipNames.Walk) });

            Assert.IsNull(ClipSet.For(_catalog, "zombie"));
        }

        [Test]
        public void TheSetHoldsTheClipsFoundAndFallsBackToIdle()
        {
            var idle = Clip("zombie", ClipNames.Idle);
            var walk = Clip("zombie", ClipNames.Walk);
            _catalog.SetClips(new[] { idle, walk, Clip("skeleton", ClipNames.Attack) });

            var set = ClipSet.For(_catalog, "zombie")!;

            Assert.AreSame(walk, set.Get(ClipNames.Walk));
            Assert.IsTrue(set.Has(ClipNames.Walk));
            Assert.IsFalse(set.Has(ClipNames.Attack));
            Assert.AreSame(idle, set.Get(ClipNames.Attack));
        }

        [Test]
        public void ASetNeedsAnIdleClip()
        {
            var clips = new Dictionary<string, ActorClip> { { ClipNames.Walk, Clip("zombie", ClipNames.Walk) } };

            Assert.Throws<ArgumentException>(() => new ClipSet(clips));
        }
    }
}
```

`Tests/Editor/ActorAnimatorTests.cs`:

```csharp
using System.Collections.Generic;
using Mortar.Game.Simulation;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class ActorAnimatorTests
    {
        private static ClipSet Full() => new ClipSet(new Dictionary<string, ActorClip>
        {
            { ClipNames.Idle, Clip(ClipNames.Idle, 4, loop: true) },
            { ClipNames.Walk, Clip(ClipNames.Walk, 8, loop: true) },
            { ClipNames.Attack, Clip(ClipNames.Attack, 6, hitFrame: 3) },
            { ClipNames.Hit, Clip(ClipNames.Hit, 3) },
            { ClipNames.Death, Clip(ClipNames.Death, 5) },
        });

        private static ActorClip Clip(string name, int frames, int hitFrame = 0, bool loop = false) =>
            new ActorClip(SpriteKeys.Clip("warrior", name), null, TestClipInfo.Make(frames, 8, hitFrame, loop));

        private static AnimMoment Moment(ActionKind action, int elapsed = 0, int duration = 0, float alpha = 0f, float now = 0f) =>
            new AnimMoment
            {
                Action = action, Elapsed = elapsed, Duration = duration, HitTick = 4, Alpha = alpha, Now = now,
                HitAt = float.NegativeInfinity, DiedAt = -1f, IsDead = false,
            };

        private static void AssertFrame(ClipFrame frame, string name, int index)
        {
            Assert.AreEqual(name, frame.Name);
            Assert.AreEqual(index, frame.Index);
        }

        [TestCase(0.25f, 2)]
        [TestCase(0.45f, 0)]
        [TestCase(0.75f, 3)]
        public void IdleLoopsOnRealTime(float now, int expected)
        {
            AssertFrame(ActorAnimator.Pick(Full(), Moment(ActionKind.Idle, now: now)), ClipNames.Idle, expected);
        }

        [Test]
        public void WalkFollowsTheMoveAcrossOneCell()
        {
            AssertFrame(ActorAnimator.Pick(Full(), Moment(ActionKind.Moving, 2, 8)), ClipNames.Walk, 2);
            AssertFrame(ActorAnimator.Pick(Full(), Moment(ActionKind.Moving, 7, 8, alpha: 0.99f)), ClipNames.Walk, 7);
        }

        [TestCase(8, 4)]
        [TestCase(12, 5)]
        [TestCase(6, 1)]
        [TestCase(20, 15)]
        public void AttackShowsTheHitFrameExactlyOnTheHitTick(int attackTicks, int hitTick)
        {
            Assert.AreEqual(3, ActorAnimator.AttackFrame(hitTick, attackTicks, hitTick, 6, 3));
            Assert.Less(ActorAnimator.AttackFrame(hitTick - 0.01f, attackTicks, hitTick, 6, 3), 3);
            Assert.AreEqual(0, ActorAnimator.AttackFrame(0f, attackTicks, hitTick, 6, 3));
            Assert.AreEqual(5, ActorAnimator.AttackFrame(attackTicks, attackTicks, hitTick, 6, 3));
        }

        [Test]
        public void PickUsesTheAttackTimingOfTheActor()
        {
            var moment = Moment(ActionKind.Attacking, 4, 10);

            AssertFrame(ActorAnimator.Pick(Full(), moment), ClipNames.Attack, 3);
        }

        [Test]
        public void StaggerPlaysTheHitClipOverItsDuration()
        {
            AssertFrame(ActorAnimator.Pick(Full(), Moment(ActionKind.Recovering, 3, 6)), ClipNames.Hit, 1);
        }

        [Test]
        public void ARecentHitOverridesWalkingButNotAttacking()
        {
            var walking = Moment(ActionKind.Moving, 2, 8, now: 10.1f);
            walking.HitAt = 10f;
            var attacking = Moment(ActionKind.Attacking, 1, 10, now: 10.1f);
            attacking.HitAt = 10f;

            AssertFrame(ActorAnimator.Pick(Full(), walking), ClipNames.Hit, 1);
            Assert.AreEqual(ClipNames.Attack, ActorAnimator.Pick(Full(), attacking).Name);
        }

        [Test]
        public void AnOldHitNoLongerShows()
        {
            var moment = Moment(ActionKind.Idle, now: 10.5f);
            moment.HitAt = 10f;

            Assert.AreEqual(ClipNames.Idle, ActorAnimator.Pick(Full(), moment).Name);
        }

        [Test]
        public void WithoutAHitClipAHitKeepsTheCurrentClip()
        {
            var idleOnly = new ClipSet(new Dictionary<string, ActorClip> { { ClipNames.Idle, Clip(ClipNames.Idle, 4, loop: true) } });
            var moment = Moment(ActionKind.Idle, now: 10.1f);
            moment.HitAt = 10f;

            Assert.AreEqual(ClipNames.Idle, ActorAnimator.Pick(idleOnly, moment).Name);
        }

        [TestCase(0.2f, 2)]
        [TestCase(10f, 4)]
        public void DeathPlaysOnceAndHoldsTheLastFrame(float sinceDeath, int expected)
        {
            var moment = Moment(ActionKind.Attacking, 2, 10, now: 5f + sinceDeath);
            moment.IsDead = true;
            moment.DiedAt = 5f;

            AssertFrame(ActorAnimator.Pick(Full(), moment), ClipNames.Death, expected);
        }

        [Test]
        public void AMissingClipUsesIdleFrames()
        {
            var idleOnly = new ClipSet(new Dictionary<string, ActorClip> { { ClipNames.Idle, Clip(ClipNames.Idle, 4, loop: true) } });

            var frame = ActorAnimator.Pick(idleOnly, Moment(ActionKind.Moving, 7, 8));

            Assert.AreEqual(ClipNames.Walk, frame.Name);
            Assert.AreEqual(3, frame.Index);
        }
    }
}
```

- [ ] **Step 2: Run the EditMode tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: compile failure naming `ClipSet`, `AnimMoment`, `ClipFrame` or `ActorAnimator`.

- [ ] **Step 3: Write `Runtime/Art/ClipSet.cs`**

```csharp
using System;
using System.Collections.Generic;

namespace Mortar.Unity
{
    public sealed class ClipSet
    {
        private static readonly string[] Names =
        {
            ClipNames.Idle, ClipNames.Walk, ClipNames.Attack, ClipNames.Hit, ClipNames.Death,
        };

        private readonly Dictionary<string, ActorClip> _clips;

        public ClipSet(IReadOnlyDictionary<string, ActorClip> clips)
        {
            if (!clips.ContainsKey(ClipNames.Idle))
            {
                throw new ArgumentException("a clip set needs an idle clip", nameof(clips));
            }
            _clips = new Dictionary<string, ActorClip>(clips, StringComparer.Ordinal);
        }

        public static ClipSet? For(SpriteCatalog catalog, string spriteSet)
        {
            var found = new Dictionary<string, ActorClip>(StringComparer.Ordinal);
            foreach (var name in Names)
            {
                var clip = catalog.Clip(spriteSet, name);
                if (clip != null)
                {
                    found.Add(name, clip);
                }
            }
            return found.ContainsKey(ClipNames.Idle) ? new ClipSet(found) : null;
        }

        public bool Has(string name) => _clips.ContainsKey(name);

        public ActorClip Get(string name) => _clips.TryGetValue(name, out var clip) ? clip : _clips[ClipNames.Idle];
    }
}
```

- [ ] **Step 4: Write `Runtime/Views/AnimMoment.cs` and `Runtime/Views/ClipFrame.cs`**

```csharp
using Mortar.Game.Simulation;

namespace Mortar.Unity
{
    public struct AnimMoment
    {
        public ActionKind Action;
        public int Elapsed;
        public int Duration;
        public int HitTick;
        public float Alpha;
        public float Now;
        public float HitAt;
        public float DiedAt;
        public bool IsDead;
    }
}
```

```csharp
namespace Mortar.Unity
{
    public readonly struct ClipFrame
    {
        public ClipFrame(string name, int index)
        {
            Name = name;
            Index = index;
        }

        public string Name { get; }

        public int Index { get; }
    }
}
```

- [ ] **Step 5: Write `Runtime/Views/ActorAnimator.cs`**

```csharp
using Mortar.Game.Simulation;
using UnityEngine;

namespace Mortar.Unity
{
    public static class ActorAnimator
    {
        public static ClipFrame Pick(ClipSet clips, AnimMoment moment)
        {
            if (moment.IsDead)
            {
                var death = clips.Get(ClipNames.Death);
                return new ClipFrame(ClipNames.Death, OnceFrame(moment.Now - moment.DiedAt, death.Fps, death.Frames));
            }
            if (moment.Action == ActionKind.Attacking)
            {
                var attack = clips.Get(ClipNames.Attack);
                var ticks = moment.Elapsed + moment.Alpha;
                return new ClipFrame(ClipNames.Attack, AttackFrame(ticks, moment.Duration, moment.HitTick, attack.Frames, attack.HitFrame));
            }
            var progress = ActorPose.Progress(moment.Elapsed, moment.Duration, moment.Alpha);
            if (moment.Action == ActionKind.Recovering)
            {
                return new ClipFrame(ClipNames.Hit, ProgressFrame(progress, clips.Get(ClipNames.Hit).Frames));
            }
            var sinceHit = moment.Now - moment.HitAt;
            if (clips.Has(ClipNames.Hit) && sinceHit >= 0f && sinceHit < clips.Get(ClipNames.Hit).Seconds)
            {
                var hit = clips.Get(ClipNames.Hit);
                return new ClipFrame(ClipNames.Hit, OnceFrame(sinceHit, hit.Fps, hit.Frames));
            }
            if (moment.Action == ActionKind.Moving)
            {
                return new ClipFrame(ClipNames.Walk, ProgressFrame(progress, clips.Get(ClipNames.Walk).Frames));
            }
            var idle = clips.Get(ClipNames.Idle);
            return new ClipFrame(ClipNames.Idle, LoopFrame(moment.Now, idle.Fps, idle.Frames));
        }

        public static int LoopFrame(float seconds, float fps, int frames) =>
            frames <= 1 ? 0 : Mathf.FloorToInt(Mathf.Max(0f, seconds) * fps) % frames;

        public static int OnceFrame(float seconds, float fps, int frames) =>
            Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0f, seconds) * fps), 0, frames - 1);

        public static int ProgressFrame(float progress, int frames) =>
            Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(progress) * frames), 0, frames - 1);

        public static int AttackFrame(float ticks, int attackTicks, int hitTick, int frames, int hitFrame)
        {
            if (frames <= 1 || attackTicks <= 0)
            {
                return 0;
            }
            if (hitTick <= 0 || hitTick >= attackTicks)
            {
                return ProgressFrame(ticks / attackTicks, frames);
            }
            var hit = Mathf.Clamp(hitFrame, 0, frames - 1);
            var position = ticks <= hitTick
                ? ticks / hitTick * hit
                : hit + (ticks - hitTick) / (attackTicks - hitTick) * (frames - 1 - hit);
            return Mathf.Clamp(Mathf.FloorToInt(position + 1e-4f), 0, frames - 1);
        }
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: all pass, no `warning CS` in the log.

- [ ] **Step 7: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Art/ClipSet.cs unity/com.mortar.unity/Runtime/Art/ClipSet.cs.meta unity/com.mortar.unity/Runtime/Views unity/com.mortar.unity/Tests/Editor
git status --short
git commit -m "add actor animator"
```

---

### Task 8: Play clips on actors and town NPCs

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Views/PropAnimator.cs`, `Tests/Runtime/TestClips.cs`, `Tests/Runtime/BakedActorTests.cs`
- Modify: `Runtime/Views/ActorView.cs`, `Runtime/Views/ActorViews.cs`, `Runtime/Views/MarkerProps.cs`

**Interfaces:**
- Consumes: Tasks 6–7; `ActorPose`, `LevelView.WorldOrder`, `LevelView.FloorOrder`, `SpriteObjects.Create`.
- Produces: `ActorView.Create(Transform parent, Actor actor, Sprite sprite, ClipSet? clips, CellSpace space)`, `ActorView.IsBaked`; `PropAnimator.Attach(SpriteRenderer, ActorClip) -> PropAnimator`, `PropAnimator.Show(float now)`, `PropAnimator.Clip`.

- [ ] **Step 1: Write the PlayMode helper and failing tests**

`Tests/Runtime/TestClips.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mortar.Unity.Tests
{
    public sealed class TestClips : IDisposable
    {
        private const int FrameWidth = 4;
        private const int FrameHeight = 6;

        private readonly List<Texture2D> _textures = new List<Texture2D>();

        public void InstallFighter(SpriteCatalog catalog, string spriteSet)
        {
            var clips = new List<ActorClip>(catalog.Clips)
            {
                Make(spriteSet, ClipNames.Idle, 4, 8, true, 0),
                Make(spriteSet, ClipNames.Walk, 8, 8, true, 0),
                Make(spriteSet, ClipNames.Attack, 6, 8, false, 3),
                Make(spriteSet, ClipNames.Hit, 3, 8, false, 0),
                Make(spriteSet, ClipNames.Death, 5, 8, false, 0),
            };
            catalog.SetClips(clips);
        }

        public void InstallNpc(SpriteCatalog catalog, string spriteSet)
        {
            var clips = new List<ActorClip>(catalog.Clips) { Make(spriteSet, ClipNames.Idle, 4, 1, true, 0) };
            catalog.SetClips(clips);
        }

        public void Dispose()
        {
            foreach (var texture in _textures)
            {
                Object.Destroy(texture);
            }
            _textures.Clear();
        }

        private ActorClip Make(string spriteSet, string name, int frames, int directions, bool loop, int hitFrame)
        {
            var texture = new Texture2D(frames * FrameWidth, directions * FrameHeight);
            _textures.Add(texture);
            var json = $"{{\"frames\":{frames},\"directions\":{directions},\"fps\":10,\"loop\":{(loop ? "true" : "false")}," +
                $"\"hitFrame\":{hitFrame},\"frameWidth\":{FrameWidth},\"frameHeight\":{FrameHeight},\"pivotX\":2,\"pivotY\":1}}";
            return new ActorClip(SpriteKeys.Clip(spriteSet, name), texture, ClipInfo.FromJson(json));
        }
    }
}
```

`Tests/Runtime/BakedActorTests.cs`:

```csharp
using System.Collections.Generic;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class BakedActorTests
    {
        private GameObject _root = null!;
        private SpriteCatalog _catalog = null!;
        private TestClips _clips = null!;
        private MortarGame _game = null!;
        private CellSpace _space = null!;
        private ActorViews _actors = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("baked-actor-test");
            _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
            _clips = new TestClips();
            _clips.InstallFighter(_catalog, "warrior");
            _game = _root.AddComponent<MortarGame>();
            _space = new CellSpace(CellSpace.CreateGrid(_root.transform));
            _actors = ActorViews.Create(_root.transform, _game);
            _game.Begin(TestContent.NewSession());
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_catalog);
            _clips.Dispose();
        }

        private World World => _game.Session!.World;

        [Test]
        public void BakedActorsShowTheirIdleClipAtFullSize()
        {
            _actors.Sync(World, _space, _catalog);
            var view = _actors.Find(Actor.PlayerId)!;

            view.Render(0f, 0f);

            var idle = _catalog.Clip("warrior", ClipNames.Idle)!;
            Assert.IsTrue(view.IsBaked);
            Assert.AreSame(idle.Frame(World.Player.Facing, 0), view.Body.sprite);
            Assert.AreEqual(Vector3.one, view.Body.transform.localScale);
            Assert.AreEqual(Vector3.zero, view.Body.transform.localPosition);
            Assert.IsFalse(view.Body.flipX);
        }

        [Test]
        public void TheAttackHitFrameShowsOnTheHitTick()
        {
            Journeys.TakeStairsDown(_game);
            _actors.Clear();
            var player = World.Player;
            _game.Enqueue(new AttackCommand(NearestMonster().Id));
            for (var i = 0; i < Journeys.MaxTicks; i++)
            {
                _game.RunTick();
                _actors.Sync(World, _space, _catalog);
                if (player.Action == ActionKind.Attacking && player.ActionElapsed == player.Profile.Timings.HitFrame)
                {
                    var view = _actors.Find(Actor.PlayerId)!;
                    view.Render(0f, Time.time);
                    var attack = _catalog.Clip("warrior", ClipNames.Attack)!;
                    Assert.AreSame(attack.Frame(player.Facing, attack.HitFrame), view.Body.sprite);
                    return;
                }
            }
            Assert.Fail("the player never reached the hit tick of an attack");
        }

        [Test]
        public void TownNpcsWithAnIdleClipAreAnimated()
        {
            _clips.InstallNpc(_catalog, "smith");
            var props = new List<GameObject>();

            MarkerProps.Place(World.Level, _space, _catalog, _root.transform, false, props);

            var smith = props.Find(prop => prop.name == "Prop smith");
            Assert.IsNotNull(smith, "town should have a smith marker");
            var animator = smith!.GetComponent<PropAnimator>();
            Assert.IsTrue(animator != null, "the smith prop should have a PropAnimator");
            animator.Show(0.25f);
            var idle = _catalog.Clip("smith", ClipNames.Idle)!;
            Assert.AreSame(idle.Frame(Direction.South, 2), smith.GetComponent<SpriteRenderer>().sprite);
        }

        private Actor NearestMonster()
        {
            Actor? nearest = null;
            foreach (var actor in World.Level.Actors)
            {
                if (actor.Kind == ActorKind.Monster && actor.IsAlive
                    && (nearest == null || actor.Cell.DistanceTo(World.Player.Cell) < nearest.Cell.DistanceTo(World.Player.Cell)))
                {
                    nearest = actor;
                }
            }
            Assert.IsNotNull(nearest, "the level should have a living monster");
            return nearest!;
        }
    }
}
```

- [ ] **Step 2: Run the PlayMode tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode -Filter BakedActorTests`
Expected: compile failure naming `IsBaked` or `PropAnimator`.

- [ ] **Step 3: Write `Runtime/Views/PropAnimator.cs`**

```csharp
using Mortar.Game;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class PropAnimator : MonoBehaviour
    {
        private SpriteRenderer _renderer = null!;
        private ActorClip _clip = null!;

        public ActorClip Clip => _clip;

        public static PropAnimator Attach(SpriteRenderer renderer, ActorClip clip)
        {
            var animator = renderer.gameObject.AddComponent<PropAnimator>();
            animator._renderer = renderer;
            animator._clip = clip;
            animator.Show(Time.time);
            return animator;
        }

        public void Show(float now) =>
            _renderer.sprite = _clip.Frame(Direction.South, ActorAnimator.LoopFrame(now, _clip.Fps, _clip.Frames));

        private void Update() => Show(Time.time);
    }
}
```

- [ ] **Step 4: Animate NPC props**

In `Runtime/Views/MarkerProps.cs`, replace the two lines that create and position the prop inside `Place` with:

```csharp
                var idle = catalog.Clip(spriteId, ClipNames.Idle);
                var sprite = idle != null ? idle.Frame(Direction.South, 0) : catalog.Actor(spriteId);
                var prop = SpriteObjects.Create(parent, $"Prop {marker.Kind}", sprite, LevelView.WorldOrder);
                prop.transform.position = space.Center(new Cell(marker.X, marker.Y)) + Vector3.up * PropLift;
                if (idle != null)
                {
                    PropAnimator.Attach(prop, idle);
                }
```

(`Mortar.Game` is already imported for `Cell` and `Direction`.)

- [ ] **Step 5: Create views with their clip set**

In `Runtime/Views/ActorViews.cs`, replace the creation line inside `Sync`:

```csharp
                    view = ActorView.Create(transform, actor, catalog.Actor(SpriteSetOf(world, actor)), space);
```

with:

```csharp
                    view = CreateView(world, actor, space, catalog);
```

and add:

```csharp
        private ActorView CreateView(World world, Actor actor, CellSpace space, SpriteCatalog catalog)
        {
            var spriteSet = SpriteSetOf(world, actor);
            var clips = ClipSet.For(catalog, spriteSet);
            var sprite = clips != null ? clips.Get(ClipNames.Idle).Frame(actor.Facing, 0) : catalog.Actor(spriteSet);
            return ActorView.Create(transform, actor, sprite, clips, space);
        }
```

Search for other callers with `rg "ActorView.Create" unity` and update them the same way (there should be none).

- [ ] **Step 6: Render clips in `ActorView`**

Replace `Runtime/Views/ActorView.cs` with:

```csharp
using Mortar.Game;
using Mortar.Game.Simulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mortar.Unity
{
    public sealed class ActorView : MonoBehaviour
    {
        public const float FlashSeconds = 0.15f;
        public const float FallSeconds = 0.3f;
        public const float BodyScale = 0.8f;

        private static readonly Color HitColor = new Color(1f, 0.35f, 0.35f);
        private static readonly Color CorpseColor = new Color(0.45f, 0.45f, 0.45f);
        private static readonly Color ArrowColor = new Color(1f, 0.85f, 0.3f, 0.8f);

        private SortingGroup _sorting = null!;
        private SpriteRenderer _body = null!;
        private SpriteRenderer _arrow = null!;
        private ClipSet? _clips;
        private Vector3 _from;
        private Vector3 _to;
        private Vector3 _target;
        private Cell _cell;
        private ActionKind _action;
        private int _elapsed;
        private int _duration;
        private int _hitTick;
        private Direction _facing;
        private float _flashUntil = -1f;
        private float _hitAt = float.NegativeInfinity;
        private float _diedAt = -1f;

        public int ActorId { get; private set; }

        public bool IsCorpse { get; private set; }

        public bool IsBaked => _clips != null;

        public Vector3 GroundPosition { get; private set; }

        public Color Tint => _body.color;

        public SortingGroup Sorting => _sorting;

        public SpriteRenderer Body => _body;

        public static ActorView Create(Transform parent, Actor actor, Sprite sprite, ClipSet? clips, CellSpace space)
        {
            var go = new GameObject($"Actor {actor.Id}");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<ActorView>();
            view._sorting = go.AddComponent<SortingGroup>();
            view._sorting.sortingOrder = LevelView.WorldOrder;
            view._clips = clips;
            view._body = SpriteObjects.Create(go.transform, "Body", sprite, 0);
            view._body.transform.localScale = Vector3.one * (clips != null ? 1f : BodyScale);
            view._arrow = SpriteObjects.Create(go.transform, "Facing", GeneratedSprites.Arrow, -1);
            view._arrow.color = ArrowColor;
            view.ActorId = actor.Id;
            view._cell = actor.Cell;
            view._from = view._to = view._target = space.Center(actor.Cell);
            view.GroundPosition = view._to;
            go.transform.position = view._to;
            return view;
        }

        public void Sync(Actor actor, LevelState level, CellSpace space)
        {
            if (actor.Cell != _cell)
            {
                _from = space.Center(_cell);
                _cell = actor.Cell;
            }
            _to = space.Center(actor.Cell);
            if (actor.Action != ActionKind.Moving)
            {
                _from = _to;
            }
            _action = actor.Action;
            _elapsed = actor.ActionElapsed;
            _duration = actor.ActionDuration;
            _hitTick = actor.Profile.Timings.HitFrame;
            _facing = actor.Facing;
            var target = actor.Action == ActionKind.Attacking ? level.Find(actor.ActionTargetId) : null;
            _target = target != null ? space.Center(target.Cell) : _to;
            if (actor.IsDead && !IsCorpse)
            {
                IsCorpse = true;
                _diedAt = Time.time;
            }
        }

        public void Render(float alpha, float now)
        {
            var progress = ActorPose.Progress(_elapsed, _duration, alpha);
            var action = IsCorpse ? ActionKind.Idle : _action;
            GroundPosition = ActorPose.Ground(action, progress, _from, _to);
            transform.position = GroundPosition;
            _arrow.enabled = !IsCorpse;
            _arrow.transform.localRotation = Quaternion.Euler(0f, 0f, ActorPose.ArrowDegrees(_facing));
            if (IsCorpse)
            {
                _sorting.sortingOrder = LevelView.FloorOrder + 3;
            }
            if (_clips != null)
            {
                RenderClip(_clips, alpha, now);
                return;
            }
            RenderStill(action, progress, now);
        }

        public void Flash(float now)
        {
            _flashUntil = now + FlashSeconds;
            _hitAt = now;
        }

        public bool Covers(Vector3 point) => !IsCorpse && SpriteObjects.Covers(_body, point);

        private void RenderClip(ClipSet clips, float alpha, float now)
        {
            var moment = new AnimMoment
            {
                Action = _action, Elapsed = _elapsed, Duration = _duration, HitTick = _hitTick,
                Alpha = alpha, Now = now, HitAt = _hitAt, DiedAt = _diedAt, IsDead = IsCorpse,
            };
            var frame = ActorAnimator.Pick(clips, moment);
            _body.sprite = clips.Get(frame.Name).Frame(_facing, frame.Index);
            _body.transform.localPosition = Vector3.zero;
            _body.color = !IsCorpse && now < _flashUntil ? HitColor : Color.white;
        }

        private void RenderStill(ActionKind action, float progress, float now)
        {
            _body.transform.localPosition = ActorPose.Body(action, progress, _from, _to, _target) - GroundPosition;
            _body.flipX = ActorPose.FacesLeft(_facing);
            if (IsCorpse)
            {
                var fall = Mathf.Clamp01((now - _diedAt) / FallSeconds);
                _body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f * fall * (_body.flipX ? -1f : 1f));
                _body.color = CorpseColor;
                return;
            }
            _body.transform.localRotation = Quaternion.identity;
            _body.color = now < _flashUntil ? HitColor : Color.white;
        }
    }
}
```

- [ ] **Step 7: Run all Unity tests**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode` then `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: EditMode all pass; PlayMode 47 passed + 1 skipped (44 + 3 new). Existing `ActorViewsTests` still pass, which shows the still fallback is unchanged. No `warning CS` in either log.

- [ ] **Step 8: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Views unity/com.mortar.unity/Tests/Runtime
git status --short
git commit -m "play baked clips on actors and town npcs"
```

---

### Task 9: The warrior from bake to play (controller and user)

**Files:**
- Create: `unity/Mortar.Sample/Assets/Art/actors/warrior/{idle,walk,attack,hit,death}.png` and `.json`, with Unity `.meta` files
- Modify: `unity/Mortar.Sample/Assets/Art/SpriteCatalog.asset`; possibly `tools/art/actors.json` (scale, facing, hide, hit time)

- [ ] **Step 1: Bake the warrior**

Run (from `tools/art`): `.venv\Scripts\python bake.py --only warrior --force --sheet`
Expected: `[BAKE] 1/1 warrior`, five `Wrote unity/Mortar.Sample/Assets/Art/actors/warrior/<anim>.png` lines, and `Wrote tools/art/out/sheets/actors-baked.png`. It takes a few minutes.

- [ ] **Step 2: Check the review sheet**

Open `tools/art/out/sheets/actors-baked.png`. Check:
- direction 0 (South) faces screen down-left and direction 6 (East) faces screen down-right;
- the figure fills about two-thirds of the frame height;
- the feet sit near the bottom without being cut off;
- the attack hit frame shows the blade extended.

Fix problems in `actors.json` and bake again: `facing` for wrong directions, `scale` for size, `attackHitSeconds` for the hit pose, `hide` for stray accessories.

- [ ] **Step 3: Import into Unity and rebuild the catalog**

With the user's editor closed, run: `pwsh -File tools/unity/run-method.ps1 -Method Mortar.Unity.EditorTools.SpriteCatalogBuilder.RebuildFromCommandLine`
Expected: the log has `Mortar: sprite catalog has 43 sprites and 5 clips`.

- [ ] **Step 4: Run all checks**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`, `pwsh -File tools/unity/test.ps1 -Platform PlayMode`, and from `tools/art` `.venv\Scripts\python -m pytest -W error -q -p no:cacheprovider`
Expected: all green.

- [ ] **Step 5: Show the user and get approval**

Show the review sheet and an in-game capture: `pwsh -File tools/unity/test.ps1 -Platform PlayMode -Filter ArtPreviewCapture` writes `out/art-preview-*.png`. Ask the user to play a few minutes (Unity Play on `Assets/Scenes/Game.unity`): walk, attack, take hits, die. Iterate on Step 2 until the user approves.

- [ ] **Step 6: Commit the warrior art**

```powershell
git add unity/Mortar.Sample/Assets/Art/actors/warrior unity/Mortar.Sample/Assets/Art/actors/warrior.meta unity/Mortar.Sample/Assets/Art/SpriteCatalog.asset tools/art/actors.json
git status --short
git commit -m "add baked warrior sprites"
```

---

### Task 10: The other nine actors, coverage tests and docs

**Files:**
- Modify: `tools/art/actors.json`, `unity/Mortar.Sample/Assets/Art/SpriteCatalog.asset`, `docs/unity.md`, `README.md`, `docs/superpowers/specs/2026-10-15-3d-art-pipeline-design.md`
- Create: baked art under `unity/Mortar.Sample/Assets/Art/actors/<actor>/`, `tools/art/tests/test_models.py`, `docs/art.md`

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

- [ ] **Step 2: Add the nine entries**

For each actor run `bake.py --list-clips <files>` and add an entry following the spec's source table:
- `skeleton`: Skeleton_Minion with the Rig_Medium sets.
- `gravewarden`: Skeleton_Warrior, `scale` about 1.3, `tint` about `[0.75, 0.7, 0.8]`.
- `zombie`: Quaternius zombie (own clips).
- `imp`: Demon, `scale` about 0.8.
- `imp-chieftain`: Blue Demon, `scale` about 1.0.
- `carrion-bat`: Bat (own clips; use its flying loop for both idle and walk).
- `smith`, `healer`, `sage`: Barbarian, Rogue_Hooded and Mage with `"npc": true` and an idle clip.

Quaternius clip names are usually `Idle`, `Walk`, `Punch` or `Bite_Front`, `HitReact` and `Death`; use what `--list-clips` prints. Run `bake.py --rehash` if Task 1 added files. Check that `test_models.py` passes.

- [ ] **Step 3: Bake and review everything**

Run: `.venv\Scripts\python bake.py --sheet`
Expected: nine new actors baked (the warrior is skipped), and one review sheet. Apply the Task 9 Step 2 checks to every actor. In addition, check relative sizes: the bat and imp are smaller than the warrior, and the gravewarden is larger. Iterate per actor with `--only <id> --force`.

- [ ] **Step 4: Rebuild the catalog and run all checks**

Run the catalog rebuild from Task 9 Step 3 and every test suite from Task 9 Step 4. Expected log line: `Mortar: sprite catalog has 43 sprites and 38 clips` (five clips for each of the seven fighters, plus three NPC idles).

- [ ] **Step 5: Show the user and get approval**

Show the review sheet and fresh `ArtPreviewCapture` images; ask the user to play the town and cathedral 1–4. Iterate until approved.

- [ ] **Step 6: Write `docs/art.md`**

```markdown
# Art pipeline

Mortar's sample art comes from two tools in `tools/art`, sharing one Python environment
(see "Placeholder art" in `docs/unity.md` for setup):

- `generate.py`: tiles and item icons as AI stills (Cloudflare Workers AI).
- `bake.py`: animated actors baked in Blender from CC0 3D models.

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
facing south for town NPCs. Sheets and their JSON sidecars go to
`unity/Mortar.Sample/Assets/Art/actors/<actor>/`; frames and Blender logs go to
`tools/art/out/bake/<actor>/`. Then run `Mortar/Rebuild Sprite Catalog` in Unity.

## Adding an actor

1. Copy the CC0 model into `tools/art/models/<pack>/` with a `LICENSE.txt` naming the source.
2. Run `bake.py --rehash` to record its sha256 in `actors.json`.
3. Run `bake.py --list-clips <file>` and add an entry to `actors.json`: `model`, optional
   `animations` files, `clips` (our name to the model's clip name), `attackHitSeconds`, and
   optional `hide`, `scale`, `facing` (degrees) and `tint`.
4. Bake with `--only <id> --sheet`, check the review sheet, and rebuild the catalog.

The bake checks each file against its recorded hash, so a changed model fails loudly.

## Licences

Every model under `tools/art/models` is CC0 (public domain dedication); each pack folder's
`LICENSE.txt` names its source. Current packs: KayKit Adventurers, KayKit Skeletons and KayKit
Character Animations by Kay Lousberg, and models by Quaternius.
```

- [ ] **Step 7: Update the other docs**

- `docs/unity.md`: in "Placeholder art", replace the paragraph starting "Sprites are stills" with: "Tiles and icons are AI stills: tiles are seamless textures projected onto 64×32 diamonds, or 64×64 blocks for walls and trees. Actors are baked from 3D models; see `docs/art.md`. An actor without baked sheets falls back to its still, mirrored horizontally, with a facing arrow." Add a line pointing to `docs/art.md` for the bake.
- `README.md`: add `docs/art.md` to the layout or docs list, next to `docs/unity.md`.
- The spec: in "Bake manifest" replace the `attachments` bullet with "`hide`: object names to switch off (packs ship accessories already attached to the hands)"; in "Unity" replace "point filtering" with "bilinear filtering"; in the playback list, change the hit bullet to "hit plays over the simulation's stagger, or once when `Flash` fires without a stagger, overriding idle or walk but never an attack".

- [ ] **Step 8: Run every check and commit**

Run: `dotnet test Mortar.slnx`, both Unity suites, and pytest.
Expected: dotnet 474 passed; Unity and pytest all green.

```powershell
git add tools/art/actors.json tools/art/tests/test_models.py unity/Mortar.Sample/Assets/Art/actors unity/Mortar.Sample/Assets/Art/SpriteCatalog.asset docs/art.md docs/unity.md README.md docs/superpowers/specs/2026-10-15-3d-art-pipeline-design.md
git status --short
git commit -m "bake the remaining actors"
```

Check `git status --short` shows no `.env`, `out/` or `Library/` paths before committing.
