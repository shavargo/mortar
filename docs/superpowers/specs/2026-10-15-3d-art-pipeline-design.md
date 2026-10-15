# 3D art pipeline (actor bake) — design

Date: 2026-10-15. The 3D-to-sprite half of the art pipeline described in
`2026-10-14-arpg-framework-design.md`, brought forward because the AI stills
are not good enough for actors.

## Goal

Replace the single still per actor with baked, animated, 8-direction sprite
sheets rendered in Blender from CC0 3D models, while keeping the 2D isometric
renderer. Anyone with Blender can re-bake every actor from files in the repo.

## Scope

In: the warrior, six monsters (zombie, skeleton, imp, imp-chieftain,
carrion-bat, gravewarden) and three town NPCs (smith, healer, sage); the
animations `idle`, `walk`, `attack`, `hit`, `death` in 8 directions (NPCs:
`idle` only, facing South); Unity import and tick-synced playback.

Out: tiles and icons (stay on the AI stills pipeline); a `cast` animation
(arrives with spells); equipment shown on the body; retargeting animations
between rigs; paid asset tiers.

## Environment

- Blender LTS installed with `winget install BlenderFoundation.Blender.LTS.4.5`
  (exact version pinned in `docs/art.md`), run headless.
- Python venv at `tools/art/.venv` (existing), Pillow for packing.
- Unity 6000.3 as today.

## Asset sources

All CC0. Each actor uses animations from its own pack, so no retargeting.

| Actor | Source |
| --- | --- |
| warrior | KayKit Adventurers: Knight, sword and shield |
| smith, healer, sage | KayKit Adventurers: Barbarian, Rogue (hooded), Mage |
| skeleton | KayKit Skeletons: Minion |
| gravewarden | KayKit Skeletons: Warrior, scaled up and tinted darker |
| zombie | Quaternius Zombie Apocalypse kit: zombie |
| imp, imp-chieftain | Quaternius Ultimate Monsters: Demon; chieftain larger and tinted |
| carrion-bat | Quaternius bat |

KayKit animations come from the free KayKit Character Animations library,
which shares one rig with the KayKit characters. Quaternius models carry their
own clips. Each file's licence is confirmed as CC0 when it is downloaded; a
file that is not CC0 is replaced, never used.

Only the `.glb` files in use (models, animation libraries, weapons) are
committed, under `tools/art/models/<pack>/`, each pack folder with a
`LICENSE.txt` (CC0 notice and source URL). itch.io downloads need a human, so
packs are fetched once and committed; `actors.json` records a SHA-256 per file
and the bake refuses a mismatch.

## Bake manifest

`tools/art/actors.json`, the 3D counterpart of `prompts.json`. Per actor id:

- `model`: path under `tools/art/models/`, plus optional `animations` file
  when clips live in a separate library;
- `attachments`: model file and bone name, e.g. a sword on the right hand;
- `scale` and optional `tint` (RGB multiplier);
- `clips`: our animation name to the source clip name;
- `attackHitSeconds`: the clip time where the blow lands;
- `sha256`: per referenced file.

A pytest check loads the content files and requires an entry for every
`spriteSet` in `content/classes` and `content/monsters`, plus `smith`,
`healer`, `sage`. NPC entries need only `idle`.

## Bake

`tools/art/bake.py [--only ID] [--force] [--sheet]`, run with the venv Python.

- Blender discovery: the `BLENDER` environment variable, else the standard
  winget install folder under `Program Files\Blender Foundation\`. Not found
  is a clear error naming both.
- One run per actor:
  `blender --background --factory-startup --python tools/art/blender/bake_actor.py -- <job.json>`.
  `--factory-startup` keeps user preferences out of the output.
- Scene: orthographic camera at a fixed scale, yawed 45° and pitched 30° down
  so the ground plane projects as 2:1 isometric; one sun key light plus a
  fill, identical for all actors; EEVEE with a transparent film.
- Frames render at 256×384 and Pillow downsamples 2× to 128×192.
- Directions: the camera stays put and the model turns in 45° steps. Grid
  directions are 45° apart on the ground, so each `Direction` lands where
  `IsoMath.ScreenDirection` points. Row order is the `Direction` enum order
  (South, SouthWest, West, NorthWest, North, NorthEast, East, SouthEast).
- Sampling: 10 fps. `attackHitSeconds` snaps to a frame index.
- Raw frames go to `tools/art/out/bake/<actor>/<anim>/<dir>/NN.png`
  (gitignored). Blender output goes to `tools/art/out/bake/<actor>/blender.log`;
  the console gets a summary.
- The Blender-side script stays thin. Job building, rotation mapping, frame
  sampling and hit-frame snapping live in plain Python under `mortar_art/`.
- A missing clip, unknown bone or hash mismatch fails that actor with a named
  error; other actors still bake.

## Sheets

One PNG per actor and animation at
`unity/Mortar.Sample/Assets/Art/actors/<actor>/<anim>.png`: a row per
direction, a column per frame. Beside it, `<anim>.json`: `frames`, `fps`,
`loop` (true for idle and walk), `hitFrame` (attack only) and the foot `pivot`
in pixels. NPC sheets have a single row.

`bake.py --sheet` writes a contact sheet of every actor's idle and attack in
all directions for review before baked art is committed.

## Unity

- `SpriteCatalogBuilder` slices each sheet into frame sprites (foot pivot,
  128 pixels per unit, point filtering) and stores an `ActorClip` per actor
  and animation: frames by direction, fps, loop, hit frame.
- `SpriteCatalog.Clip(spriteSet, anim)` returns the clip or null. Actors
  without clips keep today's still plus bob and lunge, so migration can be
  partial.
- `ActorAnimator` (pure, tested) picks the frame:
  - idle loops on real time;
  - walk follows move progress, one cycle per cell;
  - attack is time-warped piecewise so clip frame `hitFrame` shows on the
    tick where `ActionElapsed == HitFrame`, whatever `attackTicks` is;
  - hit plays once when `Flash` fires, overriding idle or walk but never an
    attack;
  - death plays once and holds the last frame, replacing the tip-over and
    grey tint; corpse sorting is unchanged.
- `ActorView` uses `ActorAnimator` when clips exist: real directions replace
  `flipX`, and the bob and lunge offsets are skipped.
- `MarkerProps` NPCs loop their `idle` clip facing South when one exists.
- Click picking keeps testing the outline of the frame on screen.

## Testing

- pytest (no Blender): manifest against content, rotation mapping, frame
  sampling and hit-frame snapping, sheet packing and JSON against synthetic
  frames, job building, Blender discovery errors.
- pytest Blender smoke test, skipped when Blender is not found: bake one
  direction of the warrior's idle; check frame size, transparent corners, and
  feet at the pivot.
- EditMode: `ActorAnimator` for each animation (attack hit frame across
  several `attackTicks`, death holds), catalog clip lookup and fallback,
  importer slicing.
- PlayMode: a baked actor walks, attacks and dies without errors and shows
  the hit frame on the hit tick.

## Docs

New `docs/art.md`: installing Blender, bake commands, adding an actor, pack
licences. `README.md` and `docs/unity.md` link to it.

## Order of work

1. Pipeline and pytest coverage.
2. The warrior end to end: bake, import, play.
3. The remaining nine actors.
