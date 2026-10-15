# Unity layer and first playable warrior — design

Date: 2026-10-14. Build-order step 3 of
`2026-10-14-arpg-framework-design.md`.

## Goal

A playable warrior in Unity 6 LTS on top of the existing engine-free
simulation: start in town, descend through cathedral levels 1–4, fight, loot,
manage gear and stats, die and reload, save and continue. Placeholder art comes
from the stills half of the art pipeline (Cloudflare Workers AI), so the
mapping from content IDs to art is exercised from day one.

## Scope

In: everything `Mortar.Game` supports today — movement, melee, stairs between
town and cathedral 1–4, ground loot, inventory grid, equipment, belt, stat
points, death, save/load — plus the HUD and screens for it, and the stills
half of `tools/art`.

Out: vendors, Town Portal, spells, other classes (later milestones); the 3D
sprite bake (step 6); audio beyond empty hook points; Unity tests in CI.

## Environment

- Unity 6000.3.26f1 (Unity 6.3 LTS), installed with the `unity` CLI at
  `Z:\Unity Editor\6000.3.26f1`, Personal license.
- Unity compiles C# 9, so engine-free code ships as prebuilt netstandard2.1
  DLLs, never as source.
- Python 3.12+ via the `py` launcher for `tools/art`.

## Layout and build pipeline

```
unity/
  com.backwoods.mortar/          UPM package
    package.json                 unity 6000.3
    Plugins/                     Mortar.*.dll + needed dependency DLLs (gitignored)
    Runtime/                     Mortar.Unity assembly (UI built in C#, one theme file)
    Editor/                      Mortar.Unity.Editor assembly (catalog builder)
    Tests/Editor/                edit-mode tests
    Tests/Runtime/               play-mode tests
  Mortar.Sample/                 Unity project
    Packages/manifest.json       "com.backwoods.mortar": "file:../../com.backwoods.mortar"
    Assets/Scenes/Game.unity
    Assets/Art/                  generated placeholders (committed after review)
    Assets/StreamingAssets/content/   copy of content/ (gitignored)
tools/unity/sync.ps1
tools/art/
```

- `tools/unity/sync.ps1` builds `Mortar.Json` (and with it Core and Game) in
  Release for netstandard2.1, copies the Mortar DLLs and the dependency DLLs
  Unity lacks into `Plugins/`, and copies `content/` into StreamingAssets. Run
  after cloning and after any engine-free change.
- `Mortar.Json` depends on `System.Text.Json` 10, whose netstandard
  dependencies may clash with assemblies Unity already ships. The first task is
  a spike that settles the exact DLL set: sync, open the sample in batch mode,
  and load content plus create a `World` from a play-mode test. Everything else
  builds on that result.
- CI keeps running `dotnet test` only.

## Runtime

`MortarGame` (MonoBehaviour) owns the `World`.

- Start: load content from StreamingAssets with `GameContentLoader.Load`, then
  either create a new warrior world with a random seed or read the save,
  depending on the main-menu choice. Load failures show the error on the menu,
  never a crash.
- Update: add `Time.deltaTime` to an accumulator; while it holds at least
  50 ms, drain queued commands, call `world.Tick`, and pass the events to
  consumers. After the loop, expose `Alpha` (0–1, the elapsed fraction of the
  next tick) for interpolation. The accumulator is capped so a long hitch runs
  a bounded number of ticks.
- The simulation pauses while the `Esc` menu or the death screen is open.
  Inventory and character sheet do not pause it.

Views sync to state after every tick:

- `LevelView` rebuilds the tilemap on `LevelChanged` (and on load).
- `ActorViews` pools one view per actor ID, lerps from previous to current
  cell by `Alpha`, applies facing and the action animation, and keeps corpses.
- `GroundItemViews` mirrors `Level.GroundItems`.

Event consumers handle one-off moments only: damage numbers, hit flashes,
death, level-up, and a message line for `CommandRejected` and other text.

Saves live at `persistentDataPath/saves/slot1.json` via `SaveFile.Write` and
`SaveFile.Read`. Auto-save on every `LevelChanged`; manual save from the `Esc`
menu; the death screen offers "load last save". Read errors are shown, never
thrown.

Plain C# helpers keep MonoBehaviours thin and testable: `IsoMath`
(cell ↔ world), `TickClock` (accumulator), `ClickResolver` (cell under cursor
→ command).

## Rendering and art mapping

- Levels: a `Grid` with isometric cell layout and one `Tilemap`. Floor-like
  tiles are 64×32 px diamonds; walls and trees are 64×64 raised blocks with a
  bottom pivot. Sorting uses a custom Y axis so actors pass in front of and
  behind walls correctly.
- `SpriteCatalog` (ScriptableObject) is the mapping asset: tiles by tileset,
  ground type and variant; actor stills by `spriteSet`; icons by `iconId`. The
  editor menu **Mortar → Rebuild Sprite Catalog** fills it by convention from
  `Assets/Art/tiles/<tileset>/<ground>-<variant>.png`,
  `Assets/Art/actors/<spriteSet>.png` and `Assets/Art/icons/<iconId>.png`. A
  missing entry yields a magenta fallback and one warning per ID.
- Actors: one still per sprite set. Facing flips the still horizontally for
  west-facing directions and rotates a small arrow under the feet. Actions
  animate procedurally from `Action` and `ActionElapsed / ActionDuration`:
  walk bobs, attack lunges toward the target, hit flashes red, death tips over
  and darkens and stays as a corpse, idle is static.
- Ground items: the icon at half scale on the floor, name on hover.
- Markers: stairs down at the exit, stairs up at the entrance only when a
  level exists above (the town entrance is not stairs); static NPC stills at the town
  smith, healer and sage markers (decorative until vendors exist).
- Camera: orthographic, follows the player's interpolated position at a fixed
  zoom.

## UI and input

Input System actions created in code (no actions asset):

- Left click resolves the cell under the cursor in priority order: living
  monster → `AttackCommand`; ground item → `PickUpCommand`; stairs → walk next
  to them, then `UseStairsCommand`; walkable floor → `MoveCommand`. Holding the
  button keeps moving toward the cursor.
- `1`–`8` → `UseBeltCommand`; `I` inventory; `C` character sheet; `Esc` menu.

UI Toolkit, built in C# with one theme file:

- HUD: life and mana orbs with circular fill, 8-slot belt with hotkey labels,
  experience bar, message line.
- Inventory: the bag grid and equipment slots. Left-click an item to hold it,
  then left-click a bag cell (`MoveItemCommand`), an equipment slot
  (`EquipCommand`), a belt slot (`PutInBeltCommand`), or the world
  (`DropItemCommand`). Right-click equips, unequips, or belts in one step.
  Tooltips show name, affixes and stats.
- Character sheet: level, experience, stats, and "+" buttons that send
  `SpendStatPointCommand` while points are available.
- Main menu: New Game, Continue (only when a save exists), Quit.
- `Esc` menu: Resume, Save, Load, Quit to menu.
- Death screen: Load last save, New Game.

## Art tool (`tools/art`, stills half)

- Python 3.12+ and Pillow only; HTTP through the standard library. Virtual
  environment at `tools/art/.venv` (gitignored).
- `prompts.json` (committed): a shared style prefix and entries for tiles
  (tileset, ground type, variant count), actors (`spriteSet`, including town
  NPCs and stairs props) and icons (`iconId`). Each entry's seed is only a
  reroll counter: FLUX-1 schnell on Workers AI rejects a `seed` input, so
  output is not reproducible. Prompts never name protected games or styles.
- `generate.py [--kind all|tiles|actors|icons] [--only <entry>] [--reroll]
  [--force] [--reprocess] [--sheet]` calls
  `@cf/black-forest-labs/flux-1-schnell`, saves raw images to
  `tools/art/out/raw/` (gitignored), then post-processes:
  - tiles: make seamless, project into 64×32 diamonds; walls and trees become
    blocks with shaded side faces;
  - actors: generated on a flat key colour, chroma-keyed, cropped, outlined,
    scaled to 64×96;
  - icons: cropped and scaled to 64×64.
- Output goes to `unity/Mortar.Sample/Assets/Art/`; a person reviews the diff
  before committing.
- Credentials: `CF_API_TOKEN` and `CF_ACCOUNT_ID` from the environment or a
  gitignored `mortar/.env`; `.env.example` lists the names only. Values never
  appear in output or errors.
- pytest covers the pure image steps (seamless blend, isometric projection,
  chroma key, crop).

## Testing

- Edit-mode: `IsoMath`, `TickClock`, `ClickResolver`, catalog fallback.
- Play-mode: content loads and a `World` is created; the runner advances ticks
  at 20 Hz; a move click moves the player; a level change rebuilds the tilemap
  and re-places the player; save then load yields an identical `WorldHash`.
- Run locally with the `unity test` CLI in batch mode.

## Docs

New `docs/unity.md` (editor install, sync, opening the sample, art tool,
catalog rebuild). README gains a Unity section and build-order status.

## Risks

- Dependency DLL clashes with Unity — settled by the first-task spike.
- AI placeholders may need rerolls; the magenta fallback keeps the game
  playable with gaps.
- The game code stays on its rules (no file over 300 lines, no function over
  50 lines) in Unity scripts too.
