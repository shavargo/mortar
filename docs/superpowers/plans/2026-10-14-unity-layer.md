# Unity Layer and First Playable Warrior Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the warrior playable in Unity 6 LTS on top of the engine-free simulation — town and cathedral 1–4, combat, loot, gear, stats, death, saves — with AI-generated placeholder art.

**Architecture:** A UPM package (`unity/com.mortar.unity`) ships the prebuilt netstandard2.1 Mortar DLLs plus a `Mortar.Unity` runtime assembly. One `MortarGame` MonoBehaviour runs the simulation at 20 Hz; views re-read world state after every tick and interpolate between ticks; events drive only one-off effects. UI is UI Toolkit built in C#. A sample Unity project (`unity/Mortar.Sample`) holds the scene and art. `tools/art` (Python + Pillow) generates the placeholder stills with Cloudflare Workers AI.

**Tech Stack:** Unity 6000.3.26f1 (C# 9 for Unity scripts), Input System package, UI Toolkit, Tilemap, Unity Test Framework (NUnit); existing C# 12 / netstandard2.1 Mortar assemblies and System.Text.Json; Python 3.12+ with Pillow and pytest.

**Spec:** `docs/superpowers/specs/2026-10-14-unity-layer-design.md` (approved). Builds on the cathedral-and-town plan (complete).

## Global Constraints

- Unity scripts compile as C# 9: block-scoped namespaces, explicit `using` directives (no implicit or global usings), no records, no `init`, no file-scoped namespaces, no collection expressions.
- Every Unity assembly folder has a `csc.rsp` with `-nullable:enable` and `-warnaserror+`. Serialized Unity references use `= null!` or nullable types.
- Never use `?.` or `??` on `UnityEngine.Object` values (they bypass Unity's destroyed-object check); compare with `== null` explicitly.
- Engine-free projects (`src/`) are not modified by this plan except where a task says so. `Mortar.Game` stays free of Unity.
- No file over 300 lines; no function over 50 lines (C# and Python).
- Clean-room: no code, formulas, data, or names from devilutionX or the original game. No Blizzard or Diablo names. Art prompts never name protected games or styles.
- Credentials come only from `CF_API_TOKEN` and `CF_ACCOUNT_ID` in the environment or the gitignored `mortar/.env`. Never print, log, or include their values in errors or commits.
- Commit messages are short, one line, present tense, no prefixes.
- Stage files by explicit path. Never stage `.env`, anything under `.superpowers/`, `bin/`/`obj/`, `out/`, Unity `Library/`/`Temp/`/`Logs/`/`UserSettings/`, `unity/com.mortar.unity/Plugins/`, or `Assets/StreamingAssets/`.
- Only one Unity editor process may open `unity/Mortar.Sample` at a time; batch runs must finish before the next starts.
- CI keeps running `dotnet test Mortar.slnx` only.

## Design Interpretations

The spec leaves these choices open; this plan fixes them. Review them before execution.

1. **The belt has 8 slots**, matching `Belt.Slots`, so the hotkeys are `1`–`8` (the spec said 4). Task 14 corrects the spec.
2. **Inventory uses click-to-pick-up, click-to-place** instead of dragging: left-click an item to hold it, left-click a bag cell to move it there (`MoveItemCommand`), a belt slot to belt it (`PutInBeltCommand`), an equipment slot to equip it, or the world to drop it (`DropItemCommand`). Right-click equips, unequips, or belts in one step. Held items are highlighted in place; there is no cursor-following icon.
3. **UI is built in C# with UI Toolkit elements** and a small style helper, not UXML/USS. The only UI asset is a theme file (`MortarTheme.tss`) that imports Unity's default runtime theme. This keeps every screen reviewable and testable without scene wiring.
4. **Input actions are created in code** with Input System bindings rather than an `.inputactions` asset.
5. **Tile art is keyed by theme**, where the theme is the area id with a trailing `-<digits>` removed (`cathedral-3` → `cathedral`, `town` → `town`). Keys are `tiles/<theme>/<ground>-<variant>`. A missing variant falls back to variant 0, then to a generated magenta placeholder.
6. **Raised tiles** are ground types with `BlocksSight` (walls, trees); they render as 64×64 blocks on a separate Y-sorted tilemap. All other non-void grounds render as flat 64×32 diamonds. `void` renders nothing.
7. **Tile variants:** a cell uses `VariantAt` only when its resolved tile type equals its ground id (pure tiles); transition tiles use variant 0.
8. **Markers:** `exit` shows a `stairs-down` prop; `entrance` shows `stairs-up` only when a level exists above; town `smith`, `healer`, and `sage` markers show static NPC props. Other markers show nothing.
9. **Corpses** stay as views tipped 90° and greyed, drawn under the living.
10. **Seeds for FLUX-1 schnell are best effort.** The generator sends and records the seed, but Workers AI may ignore it for this model; `--reroll` bumps the recorded seed so a reroll is always a new, recorded request.
11. **The shared stills pipeline writes straight into `unity/Mortar.Sample/Assets/Art/`.** The implementer generates art and contact sheets but does not commit art; the controller shows the sheets to the user and commits only after approval.
12. **Death screen is not covered by an automated test** (the simulation has no public way to force death); it is verified in the manual play check.
13. **New-game seeds** come from `Environment.TickCount` cast to `uint`. Determinism matters inside the simulation, not for picking a fresh seed.
14. **Unity test commands** run through `tools/unity/test.ps1`, which calls the editor in batch mode with `-runTests` and prints a summary from the NUnit XML.

## Review Focus

1. **The DLL spike (Task 1):** content loads, a `World` is created, and a save round-trips inside Unity. Everything else depends on it.
2. **Tick timing and interpolation:** 20 ticks per simulated second, capped catch-up, views continuous between ticks. Pinned in Tasks 2, 4, and 6.
3. **Cell mapping:** Mortar east is screen down-right, south is screen down-left, and screen-to-cell round-trips. Pinned in Task 2.
4. **Click resolution priority** (monster, item, stairs, floor) and the walk-then-use stairs flow. Pinned in Task 7.
5. **Save safety:** auto-save on level change, manual save, continue, and identical `WorldHash` after load. Pinned in Tasks 4 and 11.
6. **Credential hygiene in `tools/art`:** values never appear in output or exceptions. Pinned in Task 13.

## File Structure

```
.gitignore                                      + Unity package plugins, StreamingAssets, art tool output
.env.example                                    new: CF_API_TOKEN=, CF_ACCOUNT_ID=
tools/unity/sync.ps1                            builds DLLs, writes Plugins/ + metas, copies content
tools/unity/test.ps1                            runs Unity EditMode/PlayMode tests in batch mode
tools/unity/run-method.ps1                      runs a static editor method in batch mode
unity/com.mortar.unity/
  package.json
  Runtime/Mortar.Unity.asmdef, csc.rsp
  Runtime/Core/       ContentPaths, IsoMath, CellSpace, TickClock, TileTheme
  Runtime/Art/        SpriteKeys, GeneratedSprites, SpriteCatalog
  Runtime/Session/    ContentLoading, GameSession, SaveSlot, MortarGame
  Runtime/Views/      SpriteObjects, LevelView, MarkerProps, CameraFollow,
                      ActorPose, ActorView, ActorViews, GroundItemViews
  Runtime/Input/      ClickAction, ClickResolver, WorldCellQuery, StairsWalk, PlayerControls
  Runtime/Feedback/   EventText, CombatFeedback, MessageLog, ExperienceBar, HoverText,
                      ItemText, InventoryCursor
  Runtime/UI/         Ui, UiRoot, Orb, BeltBar, FloatingTexts, HudView,
                      ItemTooltip, EquipmentPanel, InventoryScreen,
                      CharacterSheet, MenuScreen, Menus, MortarTheme.tss
  Runtime/App/        MortarApp, SessionPresenter
  Editor/Mortar.Unity.Editor.asmdef, csc.rsp
  Editor/             ArtImport, SpriteCatalogBuilder, SampleSceneBuilder
  Tests/Editor/       Mortar.Unity.EditorTests.asmdef + edit-mode tests
  Tests/Runtime/      Mortar.Unity.Tests.asmdef + play-mode tests and helpers
unity/Mortar.Sample/                             Unity project (Assets/, Packages/, ProjectSettings/)
  Assets/Scenes/Game.unity, Assets/Settings/MortarPanelSettings.asset, Assets/link.xml
  Assets/Art/{tiles,actors,icons}/..., Assets/Art/SpriteCatalog.asset
tools/art/
  requirements.txt, pytest.ini, prompts.json, generate.py
  mortar_art/__init__.py, imaging.py, config.py, workers_ai.py, jobs.py, process.py
  tests/test_imaging.py, test_config.py, test_workers_ai.py, test_jobs.py, test_process.py
docs/unity.md                                   new: setup, sync, tests, art tool, catalog
README.md                                       + Unity section, build-order status
```

---

### Task 1: Unity scaffold and DLL spike

Create the package and sample project, a sync script that builds and installs the Mortar DLLs, a batch test runner, and a play-mode test proving that content loads, a world is created, and a save round-trips inside Unity.

**Files:**
- Modify: `.gitignore`
- Create: `tools/unity/sync.ps1`, `tools/unity/test.ps1`
- Create: `unity/com.mortar.unity/package.json`
- Create: `unity/com.mortar.unity/Runtime/Mortar.Unity.asmdef`, `Runtime/csc.rsp`, `Runtime/Core/ContentPaths.cs`
- Create: `unity/com.mortar.unity/Tests/Runtime/Mortar.Unity.Tests.asmdef`, `Tests/Runtime/ContentSpikeTests.cs`
- Create (generated by Unity, then edited): `unity/Mortar.Sample/Packages/manifest.json`, `unity/Mortar.Sample/ProjectSettings/*`

**Interfaces:**
- Produces: `Mortar.Unity.ContentPaths.Manifest` (string path to `StreamingAssets/content/game.json`); `tools/unity/sync.ps1`; `tools/unity/test.ps1 -Platform EditMode|PlayMode [-Filter <name>]`; assemblies `Mortar.Unity` and `Mortar.Unity.Tests`.

- [ ] **Step 1: Extend `.gitignore`**

Append after the existing `# Unity` block:

```gitignore
unity/**/*.csproj
unity/**/*.sln
unity/**/.vsconfig
unity/com.mortar.unity/Plugins/
unity/com.mortar.unity/Plugins.meta
unity/Mortar.Sample/Assets/StreamingAssets/
unity/Mortar.Sample/Assets/StreamingAssets.meta

# Art tool
tools/art/.venv/
tools/art/out/
__pycache__/
.pytest_cache/
```

- [ ] **Step 2: Write `tools/unity/sync.ps1`**

```powershell
param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$package = Join-Path $root 'unity\com.mortar.unity'
$plugins = Join-Path $package 'Plugins'
$streaming = Join-Path $root 'unity\Mortar.Sample\Assets\StreamingAssets'
$publish = Join-Path $root 'out\unity-publish'

# Assemblies Unity already provides; copying them causes duplicate-assembly errors.
$skip = @()

function Get-StableGuid([string]$seed) {
	$md5 = [System.Security.Cryptography.MD5]::Create()
	$bytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($seed))
	return -join ($bytes | ForEach-Object { $_.ToString('x2') })
}

function Write-FolderMeta([string]$path) {
	$guid = Get-StableGuid "mortar-folder:$([IO.Path]::GetFileName($path))"
	$text = "fileFormatVersion: 2`nguid: $guid`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
	[IO.File]::WriteAllText("$path.meta", $text)
}

function Write-PluginMeta([string]$path) {
	$guid = Get-StableGuid "mortar-plugin:$([IO.Path]::GetFileName($path))"
	$text = @"
fileFormatVersion: 2
guid: $guid
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 0
  platformData:
  - first:
      Any: 
    second:
      enabled: 1
      settings: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
	[IO.File]::WriteAllText("$path.meta", ($text -replace "`r`n", "`n"))
}

dotnet publish (Join-Path $root 'src\Mortar.Json\Mortar.Json.csproj') -c $Configuration -f netstandard2.1 -o $publish --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

if (Test-Path $plugins) { Remove-Item $plugins -Recurse -Force }
New-Item -ItemType Directory -Force $plugins | Out-Null
Write-FolderMeta $plugins
$copied = 0
Get-ChildItem $publish -Filter *.dll | Where-Object { $skip -notcontains $_.BaseName } | ForEach-Object {
	$target = Join-Path $plugins $_.Name
	Copy-Item $_.FullName $target
	Write-PluginMeta $target
	$copied++
}

if (Test-Path $streaming) { Remove-Item $streaming -Recurse -Force }
New-Item -ItemType Directory -Force $streaming | Out-Null
Copy-Item (Join-Path $root 'content') (Join-Path $streaming 'content') -Recurse

Write-Host "[SYNC] $copied assemblies -> $plugins"
Write-Host "[SYNC] content -> $streaming"
```

- [ ] **Step 3: Write `tools/unity/test.ps1`**

```powershell
param(
	[ValidateSet('EditMode', 'PlayMode')][string]$Platform = 'EditMode',
	[string]$Filter = '',
	[string]$Unity = $env:MORTAR_UNITY
)
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $root 'unity\Mortar.Sample'
. (Join-Path $PSScriptRoot 'unity-path.ps1')
$Unity = Resolve-UnityPath $project $Unity

$outDir = Join-Path $root 'out\unity-tests'
New-Item -ItemType Directory -Force $outDir | Out-Null
$results = Join-Path $outDir "$Platform.xml"
$log = Join-Path $outDir "$Platform.log"
if (Test-Path $results) { Remove-Item $results }

$arguments = @('-batchmode', '-projectPath', "`"$project`"", '-runTests', '-testPlatform', $Platform,
	'-testResults', "`"$results`"", '-logFile', "`"$log`"")
if ($Filter) { $arguments += @('-testFilter', $Filter) }
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -Wait -PassThru -NoNewWindow

if (-not (Test-Path $results)) {
	Write-Host "[ERROR] no test results; see $log"
	exit 1
}
[xml]$xml = Get-Content $results
$run = $xml.'test-run'
Write-Host "[$Platform] total $($run.total), passed $($run.passed), failed $($run.failed), skipped $($run.skipped)"
foreach ($case in $xml.SelectNodes("//test-case[@result='Failed']")) {
	Write-Host "FAILED $($case.fullname)"
	Write-Host $case.failure.message.InnerText
}
exit $process.ExitCode
```

Create `tools/unity/unity-path.ps1` (shared with the batch method runner in Task 3):

```powershell
function Resolve-UnityPath([string]$project, [string]$override) {
	if ($override) { return $override }
	$versionFile = Join-Path $project 'ProjectSettings\ProjectVersion.txt'
	$version = (Select-String -Path $versionFile -Pattern '^m_EditorVersion: (.+)$').Matches[0].Groups[1].Value.Trim()
	$env:UNITY_NO_BANNER = '1'
	$editorRoot = (& unity editors path $version).Trim()
	if (-not $editorRoot) { throw "Unity $version is not installed; set MORTAR_UNITY to Unity.exe" }
	return Join-Path $editorRoot 'Editor\Unity.exe'
}
```

Add `tools/unity/unity-path.ps1` to the Files list of this task when staging.

- [ ] **Step 4: Create the package manifest and runtime assembly**

Look up the current Input System version for the manifest:

Run: `(Invoke-RestMethod https://packages.unity.com/com.unity.inputsystem).'dist-tags'.latest`
Expected: a version such as `1.14.2`. Use the printed value as `<INPUT_VERSION>` below.

`unity/com.mortar.unity/package.json`:

```json
{
  "name": "com.mortar.unity",
  "version": "0.1.0",
  "displayName": "Mortar",
  "description": "Unity layer for the Mortar ARPG framework: rendering, input, HUD and screens on top of the engine-free simulation.",
  "unity": "6000.3",
  "license": "MIT",
  "author": { "name": "Mortar contributors" },
  "dependencies": {
    "com.unity.inputsystem": "<INPUT_VERSION>"
  }
}
```

`unity/com.mortar.unity/Runtime/Mortar.Unity.asmdef`:

```json
{
  "name": "Mortar.Unity",
  "rootNamespace": "Mortar.Unity",
  "references": ["Unity.InputSystem"],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

`unity/com.mortar.unity/Runtime/csc.rsp`:

```
-nullable:enable
-warnaserror+
```

`unity/com.mortar.unity/Runtime/Core/ContentPaths.cs`:

```csharp
using System.IO;
using UnityEngine;

namespace Mortar.Unity
{
    public static class ContentPaths
    {
        public static string Manifest => Path.Combine(Application.streamingAssetsPath, "content", "game.json");
    }
}
```

- [ ] **Step 5: Create the play-mode test assembly and the spike test**

`unity/com.mortar.unity/Tests/Runtime/Mortar.Unity.Tests.asmdef`:

```json
{
  "name": "Mortar.Unity.Tests",
  "rootNamespace": "Mortar.Unity.Tests",
  "references": ["Mortar.Unity", "Unity.InputSystem", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll", "Mortar.Core.dll", "Mortar.Game.dll", "Mortar.Json.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "versionDefines": [],
  "noEngineReferences": false
}
```

`unity/com.mortar.unity/Tests/Runtime/ContentSpikeTests.cs`:

```csharp
using System.Linq;
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using Mortar.Json.Saves;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class ContentSpikeTests
    {
        [Test]
        public void ShippedContentLoadsAndCreatesAWorldInTown()
        {
            var loaded = GameContentLoader.Load(ContentPaths.Manifest);
            Assert.IsTrue(loaded.Succeeded, string.Join("\n", loaded.Errors.Select(error => error.ToString())));

            var result = World.Create(loaded.Value!.Content, loaded.Value.CreateLevelGenerator(), "warrior", 7);

            Assert.IsTrue(result.Succeeded, result.Error);
            Assert.AreEqual(0, result.World!.Depth);
        }

        [Test]
        public void SaveJsonRoundTripsToTheSameWorldHash()
        {
            var content = GameContentLoader.Load(ContentPaths.Manifest).Value!;
            var world = World.Create(content.Content, content.CreateLevelGenerator(), "warrior", 7).World!;

            var restored = SaveFile.FromJson(SaveFile.ToJson(world), content.Content, content.CreateLevelGenerator());

            Assert.IsTrue(restored.Succeeded, restored.Error);
            Assert.AreEqual(WorldHash.Compute(world), WorldHash.Compute(restored.World!));
        }
    }
}
```

- [ ] **Step 6: Create the sample project**

Run (one line; resolves the editor through the Unity CLI):

```powershell
$env:UNITY_NO_BANNER='1'; $u = Join-Path ((unity editors path 6000.3.26f1).Trim()) 'Editor\Unity.exe'; New-Item -ItemType Directory -Force out | Out-Null; Start-Process $u -ArgumentList @('-batchmode','-quit','-createProject','"unity\Mortar.Sample"','-logFile','"out\unity-create.log"') -Wait -NoNewWindow
```

Expected: `unity/Mortar.Sample/` exists with `Assets/`, `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt` containing `m_EditorVersion: 6000.3.26f1`.

Edit `unity/Mortar.Sample/Packages/manifest.json`:
- add `"com.mortar.unity": "file:../../com.mortar.unity"` to `dependencies`;
- add `"com.unity.inputsystem": "<INPUT_VERSION>"` to `dependencies`;
- make sure `"com.unity.test-framework"` is present (keep the version Unity wrote; if absent, look it up the same way as Step 4 with `com.unity.test-framework` and add it);
- add a top-level `"testables": ["com.mortar.unity"]`.

In `unity/Mortar.Sample/ProjectSettings/ProjectSettings.asset`, set `activeInputHandler: 1` (Input System only).

- [ ] **Step 7: Sync and run the spike**

Run: `pwsh -File tools/unity/sync.ps1`
Expected: `[SYNC] N assemblies -> ...Plugins` with `Mortar.Core.dll`, `Mortar.Game.dll`, `Mortar.Json.dll`, `System.Text.Json.dll` among the files in `Plugins/`.

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: `[PlayMode] total 2, passed 2, failed 0`.

If compilation fails, read `out/unity-tests/PlayMode.log` for `error CS` lines and assembly messages:
- "Multiple precompiled assemblies with the same name X" or "X is already provided by Unity": add `X` (base name, no `.dll`) to `$skip` in `sync.ps1`, re-sync, re-run.
- Missing-reference errors for a `System.*` assembly: it must stay in `Plugins/` (remove it from `$skip` if present).
- Errors raised by `-warnaserror+` that come from Unity-generated code (not from files in this package): remove `-warnaserror+` from `csc.rsp` in every Unity assembly folder of this plan and note it in the progress log. Keep `-nullable:enable`.

Repeat until both tests pass. Record the final `$skip` list in the commit.

- [ ] **Step 8: Verify ignore rules and commit**

Run: `git status --short unity tools .gitignore`
Expected: no `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Plugins/`, or `StreamingAssets/` paths.

```powershell
git add .gitignore tools/unity/sync.ps1 tools/unity/test.ps1 tools/unity/unity-path.ps1 unity/com.mortar.unity/package.json unity/com.mortar.unity/Runtime unity/com.mortar.unity/Tests unity/Mortar.Sample/Assets unity/Mortar.Sample/Packages/manifest.json unity/Mortar.Sample/Packages/packages-lock.json unity/Mortar.Sample/ProjectSettings
git commit -m "add unity package scaffold and dll spike"
```

Also stage the `.meta` files Unity created next to package folders and files (`unity/com.mortar.unity/**/*.meta`, excluding `Plugins.meta`); the paths above include them.

---

### Task 2: Cell space, tick clock, and tile themes

Pure helpers that the views and runner build on, with the first edit-mode tests.

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Core/IsoMath.cs`, `CellSpace.cs`, `TickClock.cs`, `TileTheme.cs`
- Create: `unity/com.mortar.unity/Tests/Editor/Mortar.Unity.EditorTests.asmdef`
- Test: `unity/com.mortar.unity/Tests/Editor/CellSpaceTests.cs`, `TickClockTests.cs`, `TileThemeTests.cs`

**Interfaces:**
- Produces:
  - `IsoMath.ToTileCell(Cell) : Vector3Int`, `IsoMath.FromTileCell(Vector3Int) : Cell`, `IsoMath.ScreenDirection(Direction) : Vector2` (unit length).
  - `CellSpace(Grid)`, `CellSpace.Center(Cell) : Vector3` (z = 0), `CellSpace.CellAt(Vector3) : Cell`, `static CellSpace.CreateGrid(Transform parent) : Grid`.
  - `TickClock` with `const float TickSeconds`, `const int MaxTicksPerFrame = 5`, `float Alpha`, `int Advance(float deltaSeconds)`, `void Reset()`.
  - `TileTheme.For(string areaId) : string`.
  - Assembly `Mortar.Unity.EditorTests` (edit-mode tests).

- [ ] **Step 1: Create the edit-mode test assembly**

`unity/com.mortar.unity/Tests/Editor/Mortar.Unity.EditorTests.asmdef`:

```json
{
  "name": "Mortar.Unity.EditorTests",
  "rootNamespace": "Mortar.Unity.Tests",
  "references": ["Mortar.Unity", "Mortar.Unity.Tests", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll", "Mortar.Core.dll", "Mortar.Game.dll", "Mortar.Json.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "versionDefines": [],
  "noEngineReferences": false
}
```

- [ ] **Step 2: Write the failing tests**

`Tests/Editor/CellSpaceTests.cs`:

```csharp
using Mortar.Game;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class CellSpaceTests
    {
        private GameObject _root = null!;
        private CellSpace _space = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("cell-space-test");
            _space = new CellSpace(CellSpace.CreateGrid(_root.transform));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void CellCentersRoundTrip()
        {
            foreach (var cell in new[] { new Cell(0, 0), new Cell(5, 3), new Cell(12, 40), new Cell(63, 1) })
            {
                Assert.AreEqual(cell, _space.CellAt(_space.Center(cell)));
            }
        }

        [Test]
        public void EastIsDownRightAndSouthIsDownLeft()
        {
            var origin = _space.Center(new Cell(4, 4));
            var east = _space.Center(new Cell(5, 4));
            var south = _space.Center(new Cell(4, 5));

            Assert.AreEqual(0.5f, east.x - origin.x, 1e-4f);
            Assert.AreEqual(-0.25f, east.y - origin.y, 1e-4f);
            Assert.AreEqual(-0.5f, south.x - origin.x, 1e-4f);
            Assert.AreEqual(-0.25f, south.y - origin.y, 1e-4f);
        }

        [Test]
        public void CentersLieOnTheZeroPlane()
        {
            Assert.AreEqual(0f, _space.Center(new Cell(7, 9)).z);
        }

        [Test]
        public void ScreenDirectionOfEastPointsDownRightWithUnitLength()
        {
            var east = IsoMath.ScreenDirection(Direction.East);

            Assert.Greater(east.x, 0f);
            Assert.Less(east.y, 0f);
            Assert.AreEqual(1f, east.magnitude, 1e-4f);
        }
    }
}
```

`Tests/Editor/TickClockTests.cs`:

```csharp
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class TickClockTests
    {
        [Test]
        public void FiftyMillisecondsIsOneTick()
        {
            var clock = new TickClock();

            Assert.AreEqual(1, clock.Advance(0.05f));
            Assert.AreEqual(0f, clock.Alpha, 1e-4f);
        }

        [Test]
        public void PartialTicksAccumulate()
        {
            var clock = new TickClock();

            Assert.AreEqual(0, clock.Advance(0.025f));
            Assert.AreEqual(0.5f, clock.Alpha, 1e-4f);
            Assert.AreEqual(1, clock.Advance(0.025f));
        }

        [Test]
        public void LongHitchIsCappedAndAlphaStaysBelowOne()
        {
            var clock = new TickClock();

            Assert.AreEqual(TickClock.MaxTicksPerFrame, clock.Advance(10f));
            Assert.That(clock.Alpha, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
        }

        [Test]
        public void NegativeTimeIsIgnored()
        {
            var clock = new TickClock();

            Assert.AreEqual(0, clock.Advance(-1f));
            Assert.AreEqual(0f, clock.Alpha);
        }

        [Test]
        public void ResetClearsTheRemainder()
        {
            var clock = new TickClock();
            clock.Advance(0.03f);

            clock.Reset();

            Assert.AreEqual(0f, clock.Alpha);
        }
    }
}
```

`Tests/Editor/TileThemeTests.cs`:

```csharp
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class TileThemeTests
    {
        [TestCase("cathedral-3", "cathedral")]
        [TestCase("crypt-12", "crypt")]
        [TestCase("town", "town")]
        [TestCase("old-town", "old-town")]
        [TestCase("level-", "level-")]
        [TestCase("-4", "-4")]
        public void StripsATrailingNumber(string areaId, string expected)
        {
            Assert.AreEqual(expected, TileTheme.For(areaId));
        }
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: no results or compile errors in `out/unity-tests/EditMode.log` naming `CellSpace`, `IsoMath`, `TickClock`, `TileTheme`.

- [ ] **Step 4: Implement the helpers**

`Runtime/Core/IsoMath.cs`:

```csharp
using Mortar.Game;
using UnityEngine;

namespace Mortar.Unity
{
    /// <summary>Maps Mortar cells (x east, y south) onto Unity isometric grid cells.</summary>
    public static class IsoMath
    {
        public static Vector3Int ToTileCell(Cell cell) => new Vector3Int(-cell.Y, -cell.X, 0);

        public static Cell FromTileCell(Vector3Int tile) => new Cell(-tile.y, -tile.x);

        public static Vector2 ScreenDirection(Direction direction)
        {
            var dx = direction.DeltaX();
            var dy = direction.DeltaY();
            return new Vector2((dx - dy) * 0.5f, -(dx + dy) * 0.25f).normalized;
        }
    }
}
```

`Runtime/Core/CellSpace.cs`:

```csharp
using Mortar.Game;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class CellSpace
    {
        private readonly Grid _grid;

        public CellSpace(Grid grid)
        {
            _grid = grid;
        }

        public Vector3 Center(Cell cell)
        {
            var world = _grid.GetCellCenterWorld(IsoMath.ToTileCell(cell));
            return new Vector3(world.x, world.y, 0f);
        }

        public Cell CellAt(Vector3 world) => IsoMath.FromTileCell(_grid.WorldToCell(new Vector3(world.x, world.y, 0f)));

        public static Grid CreateGrid(Transform parent)
        {
            var go = new GameObject("Grid");
            go.transform.SetParent(parent, false);
            var grid = go.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = new Vector3(1f, 0.5f, 1f);
            return grid;
        }
    }
}
```

`Runtime/Core/TickClock.cs`:

```csharp
using Mortar.Game;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class TickClock
    {
        public const float TickSeconds = 1f / GameClock.TicksPerSecond;
        public const int MaxTicksPerFrame = 5;

        private float _accumulator;

        public float Alpha => _accumulator / TickSeconds;

        public int Advance(float deltaSeconds)
        {
            _accumulator += Mathf.Max(0f, deltaSeconds);
            var ticks = 0;
            while (_accumulator >= TickSeconds && ticks < MaxTicksPerFrame)
            {
                _accumulator -= TickSeconds;
                ticks++;
            }
            if (_accumulator >= TickSeconds)
            {
                _accumulator %= TickSeconds;
            }
            return ticks;
        }

        public void Reset() => _accumulator = 0f;
    }
}
```

`Runtime/Core/TileTheme.cs`:

```csharp
namespace Mortar.Unity
{
    public static class TileTheme
    {
        public static string For(string areaId)
        {
            var dash = areaId.LastIndexOf('-');
            if (dash <= 0 || dash == areaId.Length - 1)
            {
                return areaId;
            }
            for (var i = dash + 1; i < areaId.Length; i++)
            {
                if (areaId[i] < '0' || areaId[i] > '9')
                {
                    return areaId;
                }
            }
            return areaId.Substring(0, dash);
        }
    }
}
```

If `EastIsDownRightAndSouthIsDownLeft` fails with mirrored signs, Unity's isometric axes differ from the assumption `local = ((cx - cy) * w/2, (cx + cy) * h/2)`; swap or negate the components in `ToTileCell`/`FromTileCell` (keeping them inverse to each other) until the test passes. Do not change the test.

- [ ] **Step 5: Run tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: `[EditMode] total 15, passed 15, failed 0`.

- [ ] **Step 6: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Core unity/com.mortar.unity/Tests/Editor unity/com.mortar.unity/Tests/Editor.meta
git commit -m "add cell space, tick clock and tile themes"
```

---

### Task 3: Sprite catalog and art import

The mapping asset from content ids to sprites, generated magenta placeholders, the import rules for `Assets/Art`, the catalog builder menu, and a batch method runner.

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Art/SpriteKeys.cs`, `GeneratedSprites.cs`, `SpriteCatalog.cs`
- Create: `unity/com.mortar.unity/Editor/Mortar.Unity.Editor.asmdef`, `Editor/csc.rsp`, `Editor/ArtImport.cs`, `Editor/SpriteCatalogBuilder.cs`
- Create: `tools/unity/run-method.ps1`
- Modify: `unity/com.mortar.unity/Tests/Editor/Mortar.Unity.EditorTests.asmdef` (reference the editor assembly)
- Test: `Tests/Editor/SpriteCatalogTests.cs`, `Tests/Editor/ArtImportTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks besides the assemblies.
- Produces:
  - `SpriteKeys.Tile(string theme, string ground, int variant)`, `SpriteKeys.Actor(string id)`, `SpriteKeys.Icon(string id)` returning `tiles/<theme>/<ground>-<variant>`, `actors/<id>`, `icons/<id>`.
  - `GeneratedSprites.PixelsPerUnit = 64`, static `Sprite` properties `Flat`, `Raised`, `Actor`, `Icon`, `Arrow`.
  - `SpriteEntry(string key, Sprite sprite)` with `Key`, `Sprite`.
  - `SpriteCatalog : ScriptableObject` with `int Count`, `IReadOnlyList<SpriteEntry> Entries`, `void SetEntries(IEnumerable<SpriteEntry>)`, `Sprite Tile(string theme, string ground, int variant, bool raised)`, `Sprite Actor(string spriteSet)`, `Sprite Icon(string iconId)`, `bool Has(string key)`.
  - Editor: `SpriteCatalogBuilder.ArtRoot = "Assets/Art"`, `CatalogPath = "Assets/Art/SpriteCatalog.asset"`, `static SpriteCatalog Rebuild()`, `static void RebuildFromCommandLine()`, `static string? KeyFor(string assetPath, string root)`; `ArtImport.PivotFor(string assetPath, int width, int height) : Vector2`.
  - `tools/unity/run-method.ps1 -Method <Namespace.Type.Method>`.

- [ ] **Step 1: Create the editor assembly and reference it from edit-mode tests**

`Editor/Mortar.Unity.Editor.asmdef`:

```json
{
  "name": "Mortar.Unity.Editor",
  "rootNamespace": "Mortar.Unity.EditorTools",
  "references": ["Mortar.Unity"],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

`Editor/csc.rsp`:

```
-nullable:enable
-warnaserror+
```

In `Tests/Editor/Mortar.Unity.EditorTests.asmdef`, change `references` to:

```json
"references": ["Mortar.Unity", "Mortar.Unity.Editor", "Mortar.Unity.Tests", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
```

- [ ] **Step 2: Write the failing tests**

`Tests/Editor/SpriteCatalogTests.cs`:

```csharp
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mortar.Unity.Tests
{
    public sealed class SpriteCatalogTests
    {
        private SpriteCatalog _catalog = null!;

        [SetUp]
        public void SetUp() => _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_catalog);

        [Test]
        public void ReturnsTheMappedTileVariant()
        {
            var sprite = MakeSprite();
            _catalog.SetEntries(new[] { new SpriteEntry("tiles/cathedral/floor-1", sprite) });

            Assert.AreSame(sprite, _catalog.Tile("cathedral", "floor", 1, false));
        }

        [Test]
        public void MissingVariantFallsBackToVariantZero()
        {
            var sprite = MakeSprite();
            _catalog.SetEntries(new[] { new SpriteEntry("tiles/cathedral/floor-0", sprite) });

            Assert.AreSame(sprite, _catalog.Tile("cathedral", "floor", 2, false));
        }

        [Test]
        public void MissingTileUsesThePlaceholderForItsShape()
        {
            LogAssert.Expect(LogType.Warning, new Regex("tiles/town/wall-0"));
            LogAssert.Expect(LogType.Warning, new Regex("tiles/town/grass-0"));

            Assert.AreSame(GeneratedSprites.Raised, _catalog.Tile("town", "wall", 0, true));
            Assert.AreSame(GeneratedSprites.Flat, _catalog.Tile("town", "grass", 0, false));
        }

        [Test]
        public void WarnsOncePerMissingKey()
        {
            LogAssert.Expect(LogType.Warning, new Regex("actors/zombie"));

            _catalog.Actor("zombie");
            _catalog.Actor("zombie");

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ActorsAndIconsResolveByKey()
        {
            var actor = MakeSprite();
            var icon = MakeSprite();
            _catalog.SetEntries(new[] { new SpriteEntry("actors/warrior", actor), new SpriteEntry("icons/iron-knife", icon) });

            Assert.AreSame(actor, _catalog.Actor("warrior"));
            Assert.AreSame(icon, _catalog.Icon("iron-knife"));
            Assert.IsTrue(_catalog.Has("icons/iron-knife"));
            Assert.AreEqual(2, _catalog.Count);
        }

        [Test]
        public void MissingIconUsesTheIconPlaceholder()
        {
            LogAssert.Expect(LogType.Warning, new Regex("icons/gold"));

            Assert.AreSame(GeneratedSprites.Icon, _catalog.Icon("gold"));
        }

        private static Sprite MakeSprite()
        {
            var texture = new Texture2D(4, 4);
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), GeneratedSprites.PixelsPerUnit);
        }
    }
}
```

`Tests/Editor/ArtImportTests.cs`:

```csharp
using Mortar.Unity.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class ArtImportTests
    {
        [TestCase("Assets/Art/tiles/cathedral/floor-0.png", "tiles/cathedral/floor-0")]
        [TestCase("Assets/Art/actors/warrior.png", "actors/warrior")]
        [TestCase("Assets\\Art\\icons\\gold.PNG", "icons/gold")]
        public void KeysAreArtRelativePathsWithoutExtension(string path, string expected)
        {
            Assert.AreEqual(expected, SpriteCatalogBuilder.KeyFor(path, SpriteCatalogBuilder.ArtRoot));
        }

        [TestCase("Assets/Other/actors/warrior.png")]
        [TestCase("Assets/Art/SpriteCatalog.asset")]
        [TestCase("Assets/Artwork/actors/warrior.png")]
        public void PathsOutsideTheArtFolderHaveNoKey(string path)
        {
            Assert.IsNull(SpriteCatalogBuilder.KeyFor(path, SpriteCatalogBuilder.ArtRoot));
        }

        [Test]
        public void FlatTilesPivotOnTheirCenter()
        {
            Assert.AreEqual(new Vector2(0.5f, 0.5f), ArtImport.PivotFor("Assets/Art/tiles/town/grass-0.png", 64, 32));
        }

        [Test]
        public void RaisedTilesPivotOnTheCenterOfTheirBase()
        {
            Assert.AreEqual(new Vector2(0.5f, 0.25f), ArtImport.PivotFor("Assets/Art/tiles/town/wall-0.png", 64, 64));
        }

        [Test]
        public void ActorsPivotAtTheirFeet()
        {
            Assert.AreEqual(new Vector2(0.5f, 4f / 96f), ArtImport.PivotFor("Assets/Art/actors/warrior.png", 64, 96));
        }

        [Test]
        public void IconsPivotOnTheirCenter()
        {
            Assert.AreEqual(new Vector2(0.5f, 0.5f), ArtImport.PivotFor("Assets/Art/icons/gold.png", 64, 64));
        }
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: compile errors naming `SpriteCatalog`, `SpriteEntry`, `GeneratedSprites`, `SpriteCatalogBuilder`, `ArtImport`.

- [ ] **Step 4: Implement keys and generated sprites**

`Runtime/Art/SpriteKeys.cs`:

```csharp
namespace Mortar.Unity
{
    public static class SpriteKeys
    {
        public static string Tile(string theme, string ground, int variant) => $"tiles/{theme}/{ground}-{variant}";

        public static string Actor(string id) => $"actors/{id}";

        public static string Icon(string id) => $"icons/{id}";
    }
}
```

`Runtime/Art/GeneratedSprites.cs`:

```csharp
using System;
using UnityEngine;

namespace Mortar.Unity
{
    public static class GeneratedSprites
    {
        public const int PixelsPerUnit = 64;

        private static readonly Color32 Magenta = new Color32(255, 0, 255, 255);
        private static readonly Color32 DarkMagenta = new Color32(150, 0, 150, 255);
        private static Sprite? _flat;
        private static Sprite? _raised;
        private static Sprite? _actor;
        private static Sprite? _icon;
        private static Sprite? _arrow;

        public static Sprite Flat => Cached(ref _flat, () => Make(64, 32, new Vector2(0.5f, 0.5f), FlatPixel));

        public static Sprite Raised => Cached(ref _raised, () => Make(64, 64, new Vector2(0.5f, 0.25f), RaisedPixel));

        public static Sprite Actor => Cached(ref _actor, () => Make(64, 96, new Vector2(0.5f, 4f / 96f), ActorPixel));

        public static Sprite Icon => Cached(ref _icon, () => Make(64, 64, new Vector2(0.5f, 0.5f), IconPixel));

        public static Sprite Arrow => Cached(ref _arrow, () => Make(16, 16, new Vector2(0.5f, 0.5f), ArrowPixel));

        private static Sprite Cached(ref Sprite? field, Func<Sprite> create)
        {
            if (field == null)
            {
                field = create();
            }
            return field;
        }

        private static Sprite Make(int width, int height, Vector2 pivot, Func<int, int, Color32?> paint)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    pixels[y * width + x] = paint(x, y) ?? new Color32(0, 0, 0, 0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), pivot, PixelsPerUnit);
        }

        private static Color32? FlatPixel(int x, int y) =>
            Mathf.Abs(x - 31.5f) / 32f + Mathf.Abs(y - 15.5f) / 16f <= 1f ? Magenta : (Color32?)null;

        private static Color32? RaisedPixel(int x, int y)
        {
            var across = Mathf.Abs(x - 31.5f) / 32f;
            if (across + Mathf.Max(0f, Mathf.Abs(y - 31.5f) - 16f) / 16f > 1f)
            {
                return null;
            }
            return across + Mathf.Abs(y - 47.5f) / 16f <= 1f ? Magenta : DarkMagenta;
        }

        private static Color32? ActorPixel(int x, int y) => x >= 16 && x < 48 && y >= 4 && y < 80 ? Magenta : (Color32?)null;

        private static Color32? IconPixel(int x, int y) =>
            x < 2 || y < 2 || x > 61 || y > 61 ? DarkMagenta : Magenta;

        private static Color32? ArrowPixel(int x, int y) =>
            x >= 2 && Mathf.Abs(y - 7.5f) <= (14 - x) * 0.5f ? new Color32(255, 255, 255, 255) : (Color32?)null;
    }
}
```

- [ ] **Step 5: Implement the catalog**

`Runtime/Art/SpriteCatalog.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mortar.Unity
{
    [Serializable]
    public sealed class SpriteEntry
    {
        [SerializeField] private string _key = "";
        [SerializeField] private Sprite? _sprite;

        public SpriteEntry(string key, Sprite sprite)
        {
            _key = key;
            _sprite = sprite;
        }

        public string Key => _key;

        public Sprite? Sprite => _sprite;
    }

    [CreateAssetMenu(menuName = "Mortar/Sprite Catalog", fileName = "SpriteCatalog")]
    public sealed class SpriteCatalog : ScriptableObject
    {
        [SerializeField] private List<SpriteEntry> _entries = new List<SpriteEntry>();

        private readonly HashSet<string> _warned = new HashSet<string>();
        private Dictionary<string, Sprite>? _lookup;

        public int Count => _entries.Count;

        public IReadOnlyList<SpriteEntry> Entries => _entries;

        public void SetEntries(IEnumerable<SpriteEntry> entries)
        {
            _entries = entries.OrderBy(entry => entry.Key, StringComparer.Ordinal).ToList();
            _lookup = null;
            _warned.Clear();
        }

        public Sprite Tile(string theme, string ground, int variant, bool raised)
        {
            var key = SpriteKeys.Tile(theme, ground, variant);
            var sprite = Find(key);
            if (sprite == null)
            {
                sprite = Find(SpriteKeys.Tile(theme, ground, 0));
            }
            return sprite != null ? sprite : Missing(key, raised ? GeneratedSprites.Raised : GeneratedSprites.Flat);
        }

        public Sprite Actor(string spriteSet) => Resolve(SpriteKeys.Actor(spriteSet), GeneratedSprites.Actor);

        public Sprite Icon(string iconId) => Resolve(SpriteKeys.Icon(iconId), GeneratedSprites.Icon);

        public bool Has(string key) => Find(key) != null;

        private Sprite Resolve(string key, Sprite placeholder)
        {
            var sprite = Find(key);
            return sprite != null ? sprite : Missing(key, placeholder);
        }

        private Sprite? Find(string key)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
                foreach (var entry in _entries)
                {
                    if (entry.Sprite != null && entry.Key.Length > 0)
                    {
                        _lookup[entry.Key] = entry.Sprite;
                    }
                }
            }
            return _lookup.TryGetValue(key, out var sprite) ? sprite : null;
        }

        private Sprite Missing(string key, Sprite placeholder)
        {
            if (_warned.Add(key))
            {
                Debug.LogWarning($"Mortar: no sprite for '{key}', using a placeholder");
            }
            return placeholder;
        }

        private void OnValidate() => _lookup = null;
    }
}
```

- [ ] **Step 6: Implement import rules and the builder**

`Editor/SpriteCatalogBuilder.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mortar.Unity.EditorTools
{
    public static class SpriteCatalogBuilder
    {
        public const string ArtRoot = "Assets/Art";
        public const string CatalogPath = "Assets/Art/SpriteCatalog.asset";

        [MenuItem("Mortar/Rebuild Sprite Catalog")]
        public static void RebuildFromMenu()
        {
            var catalog = Rebuild();
            Debug.Log($"Mortar: sprite catalog has {catalog.Count} entries");
        }

        public static void RebuildFromCommandLine()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            RebuildFromMenu();
        }

        public static SpriteCatalog Rebuild()
        {
            EnsureFolder(ArtRoot);
            var catalog = AssetDatabase.LoadAssetAtPath<SpriteCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
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
            catalog.SetEntries(entries);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        public static string? KeyFor(string assetPath, string root)
        {
            var normalized = assetPath.Replace('\\', '/');
            var prefix = root.TrimEnd('/') + "/";
            if (!normalized.StartsWith(prefix, StringComparison.Ordinal)
                || !normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return normalized.Substring(prefix.Length, normalized.Length - prefix.Length - ".png".Length);
        }

        public static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
```

`Editor/ArtImport.cs`:

```csharp
using System;
using UnityEditor;
using UnityEngine;

namespace Mortar.Unity.EditorTools
{
    public sealed class ArtImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (SpriteCatalogBuilder.KeyFor(assetPath, SpriteCatalogBuilder.ArtRoot) == null)
            {
                return;
            }
            var importer = (TextureImporter)assetImporter;
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
    }
}
```

- [ ] **Step 7: Write `tools/unity/run-method.ps1`**

```powershell
param(
	[Parameter(Mandatory = $true)][string]$Method,
	[string]$Unity = $env:MORTAR_UNITY
)
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $root 'unity\Mortar.Sample'
. (Join-Path $PSScriptRoot 'unity-path.ps1')
$Unity = Resolve-UnityPath $project $Unity

$outDir = Join-Path $root 'out\unity-methods'
New-Item -ItemType Directory -Force $outDir | Out-Null
$log = Join-Path $outDir "$($Method -replace '[^A-Za-z0-9]', '-').log"
$arguments = @('-batchmode', '-quit', '-projectPath', "`"$project`"", '-executeMethod', $Method, '-logFile', "`"$log`"")
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -Wait -PassThru -NoNewWindow
if ($process.ExitCode -ne 0) {
	Write-Host "[ERROR] $Method failed (exit $($process.ExitCode)); see $log"
	exit $process.ExitCode
}
Write-Host "[OK] $Method"
```

- [ ] **Step 8: Run tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: `[EditMode] total 31, passed 31, failed 0`.

Run: `pwsh -File tools/unity/run-method.ps1 -Method Mortar.Unity.EditorTools.SpriteCatalogBuilder.RebuildFromCommandLine`
Expected: `[OK] ...`; the log contains `Mortar: sprite catalog has 0 entries`; `unity/Mortar.Sample/Assets/Art/SpriteCatalog.asset` exists.

- [ ] **Step 9: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Art unity/com.mortar.unity/Runtime/Art.meta unity/com.mortar.unity/Editor unity/com.mortar.unity/Editor.meta unity/com.mortar.unity/Tests/Editor tools/unity/run-method.ps1 unity/Mortar.Sample/Assets/Art
git commit -m "add sprite catalog and art import rules"
```

---

### Task 4: Game session, saves, and the 20 Hz runner

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Session/ContentLoading.cs`, `GameSession.cs`, `SaveSlot.cs`, `MortarGame.cs`
- Create: `unity/com.mortar.unity/Tests/Runtime/TestContent.cs`, `Tests/Runtime/Journeys.cs`
- Test: `unity/com.mortar.unity/Tests/Runtime/MortarGameTests.cs`

**Interfaces:**
- Consumes: `TickClock` (Task 2), `ContentPaths.Manifest` (Task 1).
- Produces:
  - `ContentLoading.Load(string manifest, out string? error) : GameContent?`; `ContentLoading.Describe(IReadOnlyList<AuthoringError>) : string`.
  - `GameSession` with `GameContent Content`, `World World`, `static GameSession? NewGame(GameContent, string classId, uint seed, out string? error)`, `static GameSession? Load(GameContent, SaveSlot, out string? error)`, `void Enqueue(Command)`, `IReadOnlyList<GameEvent> Tick()`.
  - `SaveSlot(string folder, string name = "slot1")` with `string Folder`, `string FilePath`, `bool Exists`, `string? Write(World)`, `WorldResult Read(GameContent)`, `static SaveSlot InPersistentData()`.
  - `MortarGame : MonoBehaviour` with `GameSession? Session`, `SaveSlot? Slot`, `bool Paused`, `float Alpha`, events `SessionStarted(GameSession)`, `Ticked(IReadOnlyList<GameEvent>)`, `Notice(string)`; methods `Begin(GameSession)`, `End()`, `Enqueue(Command)`, `Step(float deltaSeconds)`, `RunTick()`, `Save() : string?`.
  - Test helpers: `TestContent.Content : GameContent`, `TestContent.NewSession(uint seed = 7) : GameSession`, `TestPaths.FreshSaveFolder() : string`, `TestPaths.Delete(string)`, `Journeys.MarkerCell(World, string kind) : Cell`, `Journeys.WalkNextTo(MortarGame, string markerKind)`, `Journeys.TakeStairsDown(MortarGame)`, `Journeys.MaxTicks = 4000`.

- [ ] **Step 1: Write the test helpers**

`Tests/Runtime/TestContent.cs`:

```csharp
using System;
using System.IO;
using Mortar.Json.Content;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public static class TestContent
    {
        private static GameContent? _content;

        public static GameContent Content
        {
            get
            {
                if (_content == null)
                {
                    _content = ContentLoading.Load(ContentPaths.Manifest, out var error);
                    if (_content == null)
                    {
                        Assert.Fail(error);
                    }
                }
                return _content!;
            }
        }

        public static GameSession NewSession(uint seed = 7)
        {
            var session = GameSession.NewGame(Content, "warrior", seed, out var error);
            Assert.IsNotNull(session, error);
            return session!;
        }
    }

    public static class TestPaths
    {
        public static string FreshSaveFolder() =>
            Path.Combine(Application.temporaryCachePath, "mortar-tests", Guid.NewGuid().ToString("N"));

        public static void Delete(string folder)
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
    }
}
```

`Tests/Runtime/Journeys.cs`:

```csharp
using System;
using Mortar.Core.Model;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Simulation;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public static class Journeys
    {
        public const int MaxTicks = 4000;

        public static Cell MarkerCell(World world, string kind) =>
            world.Level.FindMarker(kind) ?? throw new InvalidOperationException($"depth {world.Depth} has no '{kind}' marker");

        public static void WalkNextTo(MortarGame game, string markerKind)
        {
            var world = game.Session!.World;
            var target = MarkerCell(world, markerKind);
            game.Enqueue(new MoveCommand(target));
            for (var i = 0; i < MaxTicks; i++)
            {
                game.RunTick();
                var player = world.Player;
                if (player.Cell.DistanceTo(target) <= 1 && player.Action == ActionKind.Idle && player.Intent == IntentKind.None)
                {
                    return;
                }
            }
            Assert.Fail($"player did not reach the '{markerKind}' marker within {MaxTicks} ticks");
        }

        public static void TakeStairsDown(MortarGame game)
        {
            var world = game.Session!.World;
            var depth = world.Depth;
            WalkNextTo(game, MarkerKinds.Exit);
            game.Enqueue(new UseStairsCommand());
            game.RunTick();
            Assert.AreEqual(depth + 1, world.Depth, "the stairs did not lead down");
        }
    }
}
```

- [ ] **Step 2: Write the failing tests**

`Tests/Runtime/MortarGameTests.cs`:

```csharp
using System.Collections;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mortar.Unity.Tests
{
    public sealed class MortarGameTests
    {
        private GameObject _host = null!;
        private MortarGame _game = null!;
        private string _folder = "";

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("mortar-game-test");
            _game = _host.AddComponent<MortarGame>();
            _folder = TestPaths.FreshSaveFolder();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_host);
            TestPaths.Delete(_folder);
        }

        [Test]
        public void TwentyFiftyMillisecondStepsRunTwentyTicks()
        {
            _game.Begin(TestContent.NewSession());

            for (var i = 0; i < 20; i++)
            {
                _game.Step(0.05f);
            }

            Assert.AreEqual(20, _game.Session!.World.TickCount);
        }

        [Test]
        public void PausedGameDoesNotTick()
        {
            _game.Begin(TestContent.NewSession());
            _game.Paused = true;

            _game.Step(1f);

            Assert.AreEqual(0, _game.Session!.World.TickCount);
        }

        [Test]
        public void TickedFiresOncePerTick()
        {
            var count = 0;
            _game.Ticked += events => count++;
            _game.Begin(TestContent.NewSession());

            _game.Step(0.15f);

            Assert.AreEqual(3, count);
        }

        [Test]
        public void SaveThenLoadRestoresIdenticalState()
        {
            _game.Slot = new SaveSlot(_folder);
            _game.Begin(TestContent.NewSession());
            var player = _game.Session!.World.Player.Cell;
            _game.Enqueue(new MoveCommand(new Cell(player.X + 1, player.Y)));
            for (var i = 0; i < 40; i++)
            {
                _game.RunTick();
            }

            Assert.IsNull(_game.Save());
            var loaded = GameSession.Load(TestContent.Content, _game.Slot, out var error);

            Assert.IsNotNull(loaded, error);
            Assert.AreEqual(WorldHash.Compute(_game.Session.World), WorldHash.Compute(loaded!.World));
        }

        [Test]
        public void ChangingLevelAutoSaves()
        {
            string? notice = null;
            _game.Notice += text => notice = text;
            _game.Slot = new SaveSlot(_folder);
            _game.Begin(TestContent.NewSession());

            Journeys.TakeStairsDown(_game);

            Assert.IsTrue(_game.Slot.Exists);
            Assert.AreEqual("Game saved", notice);
            var loaded = GameSession.Load(TestContent.Content, _game.Slot, out var error);
            Assert.AreEqual(1, loaded!.World.Depth, error);
        }

        [UnityTest]
        public IEnumerator UpdateAdvancesTicksInRealTime()
        {
            _game.Begin(TestContent.NewSession());

            yield return new WaitForSeconds(0.3f);

            Assert.Greater(_game.Session!.World.TickCount, 0);
        }
    }
}
```

`TickedFiresOncePerTick` uses 0.15 s; if float rounding yields 2 ticks, change the step to three calls of `Step(0.05f)` rather than altering the clock.

- [ ] **Step 3: Run tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: compile errors naming `MortarGame`, `GameSession`, `SaveSlot`, `ContentLoading`.

- [ ] **Step 4: Implement content loading, session, and save slot**

`Runtime/Session/ContentLoading.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Mortar.Core.Authoring;
using Mortar.Json.Content;

namespace Mortar.Unity
{
    public static class ContentLoading
    {
        public const int ShownErrors = 5;

        public static GameContent? Load(string manifest, out string? error)
        {
            var result = GameContentLoader.Load(manifest);
            error = result.Succeeded ? null : Describe(result.Errors);
            return result.Value;
        }

        public static string Describe(IReadOnlyList<AuthoringError> errors)
        {
            var lines = errors.Take(ShownErrors).Select(error => error.ToString()).ToList();
            if (errors.Count > ShownErrors)
            {
                lines.Add($"... and {errors.Count - ShownErrors} more");
            }
            return "Content failed to load:\n" + string.Join("\n", lines);
        }
    }
}
```

`Runtime/Session/GameSession.cs`:

```csharp
using System.Collections.Generic;
using Mortar.Game.Commands;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Json.Content;

namespace Mortar.Unity
{
    public sealed class GameSession
    {
        private readonly List<Command> _pending = new List<Command>();

        private GameSession(GameContent content, World world)
        {
            Content = content;
            World = world;
        }

        public GameContent Content { get; }

        public World World { get; }

        public static GameSession? NewGame(GameContent content, string classId, uint seed, out string? error) =>
            From(content, World.Create(content.Content, content.CreateLevelGenerator(), classId, seed), out error);

        public static GameSession? Load(GameContent content, SaveSlot slot, out string? error) =>
            From(content, slot.Read(content), out error);

        public void Enqueue(Command command) => _pending.Add(command);

        public IReadOnlyList<GameEvent> Tick()
        {
            var commands = _pending.ToArray();
            _pending.Clear();
            return World.Tick(commands);
        }

        private static GameSession? From(GameContent content, WorldResult result, out string? error)
        {
            error = result.Error;
            return result.World == null ? null : new GameSession(content, result.World);
        }
    }
}
```

`Runtime/Session/SaveSlot.cs`:

```csharp
using System;
using System.IO;
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using Mortar.Json.Saves;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class SaveSlot
    {
        public SaveSlot(string folder, string name = "slot1")
        {
            Folder = folder;
            FilePath = Path.Combine(folder, name + ".json");
        }

        public string Folder { get; }

        public string FilePath { get; }

        public bool Exists => File.Exists(FilePath);

        public string? Write(World world)
        {
            try
            {
                Directory.CreateDirectory(Folder);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return $"{Folder}: cannot create the save folder: {ex.Message}";
            }
            return SaveFile.Write(FilePath, world);
        }

        public WorldResult Read(GameContent content) => SaveFile.Read(FilePath, content.Content, content.CreateLevelGenerator());

        public static SaveSlot InPersistentData() => new SaveSlot(Path.Combine(Application.persistentDataPath, "saves"));
    }
}
```

- [ ] **Step 5: Implement the runner**

`Runtime/Session/MortarGame.cs`:

```csharp
using System;
using System.Collections.Generic;
using Mortar.Game.Commands;
using Mortar.Game.Events;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class MortarGame : MonoBehaviour
    {
        private readonly TickClock _clock = new TickClock();

        public event Action<GameSession>? SessionStarted;

        public event Action<IReadOnlyList<GameEvent>>? Ticked;

        public event Action<string>? Notice;

        public GameSession? Session { get; private set; }

        public SaveSlot? Slot { get; set; }

        public bool Paused { get; set; }

        public float Alpha => _clock.Alpha;

        public void Begin(GameSession session)
        {
            Session = session;
            Paused = false;
            _clock.Reset();
            SessionStarted?.Invoke(session);
        }

        public void End()
        {
            Session = null;
            _clock.Reset();
        }

        public void Enqueue(Command command)
        {
            if (Session != null && !Paused)
            {
                Session.Enqueue(command);
            }
        }

        public void Step(float deltaSeconds)
        {
            if (Session == null || Paused)
            {
                return;
            }
            var ticks = _clock.Advance(deltaSeconds);
            for (var i = 0; i < ticks && Session != null && !Paused; i++)
            {
                RunTick();
            }
        }

        public void RunTick()
        {
            if (Session == null)
            {
                return;
            }
            var events = Session.Tick();
            if (Slot != null && ContainsLevelChange(events))
            {
                Notice?.Invoke(Save() ?? "Game saved");
            }
            Ticked?.Invoke(events);
        }

        public string? Save()
        {
            if (Session == null)
            {
                return "no game in progress";
            }
            return Slot == null ? "no save slot" : Slot.Write(Session.World);
        }

        private static bool ContainsLevelChange(IReadOnlyList<GameEvent> events)
        {
            foreach (var gameEvent in events)
            {
                if (gameEvent is LevelChanged)
                {
                    return true;
                }
            }
            return false;
        }

        private void Update() => Step(Time.deltaTime);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: `[PlayMode] total 8, passed 8, failed 0`.

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: `[EditMode] total 31, passed 31, failed 0`.

- [ ] **Step 7: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Session unity/com.mortar.unity/Runtime/Session.meta unity/com.mortar.unity/Tests/Runtime
git commit -m "add game session, save slot and tick runner"
```

---

### Task 5: Level view and camera

Render the current level on two isometric tilemaps, place stairs and NPC props, and follow the player with an orthographic camera.

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Views/SpriteObjects.cs`, `LevelView.cs`, `MarkerProps.cs`, `CameraFollow.cs`
- Test: `unity/com.mortar.unity/Tests/Runtime/LevelViewTests.cs`

**Interfaces:**
- Consumes: `CellSpace`, `IsoMath`, `TileTheme` (Task 2); `SpriteCatalog`, `GeneratedSprites` (Task 3); `MortarGame`, `TestContent`, `Journeys` (Task 4).
- Produces:
  - `SpriteObjects.Create(Transform parent, string name, Sprite sprite, int sortingOrder) : SpriteRenderer`.
  - `LevelView : MonoBehaviour` with `const int FloorOrder = -10`, `const int WorldOrder = 0`, `CellSpace Space`, `int TileCount`, `IReadOnlyList<GameObject> Props`, `static LevelView Create(Transform parent, SpriteCatalog catalog)`, `void Show(LevelState level, Tileset tileset, bool hasLevelAbove)`, `void Clear()`, `static Tileset TilesetFor(GameContent content, LevelState level)`.
  - `MarkerProps.SpriteFor(string markerKind, bool hasLevelAbove) : string?`; `MarkerProps.Place(LevelState, CellSpace, SpriteCatalog, Transform, bool hasLevelAbove, List<GameObject> into)`.
  - `CameraFollow : MonoBehaviour` with `const float Size = 5.5f`, `Camera Camera`, `static CameraFollow Create(Transform parent)`, `void Follow(Func<Vector3?> target)`, `void Snap()`.

- [ ] **Step 1: Write the failing tests**

`Tests/Runtime/LevelViewTests.cs`:

```csharp
using System.Linq;
using Mortar.Core.Model;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class LevelViewTests
    {
        private GameObject _root = null!;
        private SpriteCatalog _catalog = null!;
        private LevelView _view = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("level-view-test");
            _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
            _view = LevelView.Create(_root.transform, _catalog);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_catalog);
        }

        [Test]
        public void TownShowsATileForEveryNonVoidCell()
        {
            var world = TestContent.NewSession().World;

            Show(world);

            Assert.AreEqual(NonVoidCells(world.Level), _view.TileCount);
        }

        [Test]
        public void TownShowsStairsDownAndNpcsButNoStairsUp()
        {
            var world = TestContent.NewSession().World;

            Show(world);

            var names = _view.Props.Select(prop => prop.name).ToList();
            Assert.Contains("Prop exit", names);
            Assert.Contains("Prop smith", names);
            Assert.Contains("Prop healer", names);
            Assert.Contains("Prop sage", names);
            Assert.IsFalse(names.Contains("Prop entrance"));
        }

        [Test]
        public void ShowingTheNextLevelReplacesTheTown()
        {
            var host = new GameObject("game");
            host.transform.SetParent(_root.transform);
            var game = host.AddComponent<MortarGame>();
            game.Begin(TestContent.NewSession());
            Journeys.TakeStairsDown(game);
            var world = game.Session!.World;

            Show(world);

            Assert.AreEqual(NonVoidCells(world.Level), _view.TileCount);
            Assert.Contains("Prop entrance", _view.Props.Select(prop => prop.name).ToList());
        }

        [Test]
        public void PropsSitOnTheirMarkerCells()
        {
            var world = TestContent.NewSession().World;

            Show(world);

            var exit = _view.Props.First(prop => prop.name == "Prop exit");
            var expected = _view.Space.Center(Journeys.MarkerCell(world, MarkerKinds.Exit));
            Assert.AreEqual(expected, exit.transform.position);
        }

        private void Show(World world) =>
            _view.Show(world.Level, LevelView.TilesetFor(TestContent.Content, world.Level), world.Depth > world.Content.ShallowestLevel);

        private static int NonVoidCells(LevelState level)
        {
            var cells = level.Layout.Cells;
            var count = 0;
            for (var y = 0; y < cells.Height; y++)
            {
                for (var x = 0; x < cells.Width; x++)
                {
                    if (cells.GroundAt(x, y) != Tileset.VoidId)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: compile errors naming `LevelView`.

- [ ] **Step 3: Implement sprite objects and marker props**

`Runtime/Views/SpriteObjects.cs`:

```csharp
using UnityEngine;

namespace Mortar.Unity
{
    public static class SpriteObjects
    {
        public static SpriteRenderer Create(Transform parent, string name, Sprite sprite, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
```

`Runtime/Views/MarkerProps.cs`:

```csharp
using System.Collections.Generic;
using Mortar.Core.Model;
using Mortar.Game;
using Mortar.Game.Simulation;
using UnityEngine;

namespace Mortar.Unity
{
    public static class MarkerProps
    {
        public static string? SpriteFor(string markerKind, bool hasLevelAbove)
        {
            switch (markerKind)
            {
                case MarkerKinds.Exit:
                    return "stairs-down";
                case MarkerKinds.Entrance:
                    return hasLevelAbove ? "stairs-up" : null;
                case "smith":
                case "healer":
                case "sage":
                    return markerKind;
                default:
                    return null;
            }
        }

        public static void Place(
            LevelState level, CellSpace space, SpriteCatalog catalog, Transform parent, bool hasLevelAbove, List<GameObject> into)
        {
            foreach (var marker in level.Layout.Markers)
            {
                var spriteId = SpriteFor(marker.Kind, hasLevelAbove);
                if (spriteId == null)
                {
                    continue;
                }
                var prop = SpriteObjects.Create(parent, $"Prop {marker.Kind}", catalog.Actor(spriteId), LevelView.WorldOrder);
                prop.transform.position = space.Center(new Cell(marker.X, marker.Y));
                into.Add(prop.gameObject);
            }
        }
    }
}
```

- [ ] **Step 4: Implement the level view**

`Runtime/Views/LevelView.cs`:

```csharp
using System.Collections.Generic;
using Mortar.Core.Model;
using Mortar.Game;
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Mortar.Unity
{
    public sealed class LevelView : MonoBehaviour
    {
        public const int FloorOrder = -10;
        public const int WorldOrder = 0;

        private readonly Dictionary<Sprite, Tile> _tiles = new Dictionary<Sprite, Tile>();
        private readonly List<GameObject> _props = new List<GameObject>();
        private SpriteCatalog _catalog = null!;
        private Tilemap _floor = null!;
        private Tilemap _raised = null!;

        public CellSpace Space { get; private set; } = null!;

        public int TileCount { get; private set; }

        public IReadOnlyList<GameObject> Props => _props;

        public static LevelView Create(Transform parent, SpriteCatalog catalog)
        {
            var go = new GameObject("Level");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<LevelView>();
            view._catalog = catalog;
            var grid = CellSpace.CreateGrid(go.transform);
            view.Space = new CellSpace(grid);
            view._floor = CreateTilemap(grid.transform, "Floor", FloorOrder, TilemapRenderer.Mode.Chunk);
            view._raised = CreateTilemap(grid.transform, "Raised", WorldOrder, TilemapRenderer.Mode.Individual);
            return view;
        }

        public static Tileset TilesetFor(GameContent content, LevelState level) => content.Areas[level.Layout.AreaId].Tileset;

        public void Show(LevelState level, Tileset tileset, bool hasLevelAbove)
        {
            Clear();
            var cells = level.Layout.Cells;
            var theme = TileTheme.For(level.Layout.AreaId);
            var floor = new TileBatch();
            var raised = new TileBatch();
            for (var y = 0; y < cells.Height; y++)
            {
                for (var x = 0; x < cells.Width; x++)
                {
                    var ground = cells.GroundAt(x, y);
                    if (ground == Tileset.VoidId)
                    {
                        continue;
                    }
                    var index = tileset.IndexOf(ground);
                    var isRaised = index >= 0 && tileset.Grounds[index].BlocksSight;
                    var variant = cells.TileAt(x, y) == ground ? cells.VariantAt(x, y) : 0;
                    var tile = TileFor(_catalog.Tile(theme, ground, variant, isRaised));
                    (isRaised ? raised : floor).Add(IsoMath.ToTileCell(new Cell(x, y)), tile);
                }
            }
            floor.ApplyTo(_floor);
            raised.ApplyTo(_raised);
            TileCount = floor.Count + raised.Count;
            MarkerProps.Place(level, Space, _catalog, transform, hasLevelAbove, _props);
        }

        public void Clear()
        {
            _floor.ClearAllTiles();
            _raised.ClearAllTiles();
            foreach (var prop in _props)
            {
                Destroy(prop);
            }
            _props.Clear();
            TileCount = 0;
        }

        private Tile TileFor(Sprite sprite)
        {
            if (!_tiles.TryGetValue(sprite, out var tile))
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
                _tiles.Add(sprite, tile);
            }
            return tile;
        }

        private void OnDestroy()
        {
            foreach (var tile in _tiles.Values)
            {
                Destroy(tile);
            }
            _tiles.Clear();
        }

        private static Tilemap CreateTilemap(Transform parent, string name, int order, TilemapRenderer.Mode mode)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            renderer.mode = mode;
            return tilemap;
        }

        private sealed class TileBatch
        {
            private readonly List<Vector3Int> _positions = new List<Vector3Int>();
            private readonly List<TileBase> _tiles = new List<TileBase>();

            public int Count => _positions.Count;

            public void Add(Vector3Int position, TileBase tile)
            {
                _positions.Add(position);
                _tiles.Add(tile);
            }

            public void ApplyTo(Tilemap tilemap) => tilemap.SetTiles(_positions.ToArray(), _tiles.ToArray());
        }
    }
}
```

- [ ] **Step 5: Implement the camera**

`Runtime/Views/CameraFollow.cs`:

```csharp
using System;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class CameraFollow : MonoBehaviour
    {
        public const float Size = 5.5f;
        public const float Depth = -10f;

        private Func<Vector3?>? _target;

        public Camera Camera { get; private set; } = null!;

        public static CameraFollow Create(Transform parent)
        {
            var go = new GameObject("Camera");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 0f, Depth);
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Size;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.07f, 0.08f);
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = Vector3.up;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            go.AddComponent<AudioListener>();
            var follow = go.AddComponent<CameraFollow>();
            follow.Camera = camera;
            return follow;
        }

        public void Follow(Func<Vector3?> target) => _target = target;

        public void Snap() => LateUpdate();

        private void LateUpdate()
        {
            var target = _target?.Invoke();
            if (target.HasValue)
            {
                transform.position = new Vector3(target.Value.x, target.Value.y, Depth);
            }
        }
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: `[PlayMode] total 12, passed 12, failed 0`.

- [ ] **Step 7: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Views unity/com.mortar.unity/Runtime/Views.meta unity/com.mortar.unity/Tests/Runtime
git commit -m "add level view, marker props and camera"
```

---

### Task 6: Actor and ground item views

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Views/ActorPose.cs`, `ActorView.cs`, `ActorViews.cs`, `GroundItemViews.cs`
- Test: `unity/com.mortar.unity/Tests/Editor/ActorPoseTests.cs`, `Tests/Runtime/ActorViewsTests.cs`

**Interfaces:**
- Consumes: `CellSpace`, `IsoMath` (Task 2); `SpriteCatalog`, `GeneratedSprites` (Task 3); `MortarGame`, `TestContent`, `Journeys` (Task 4); `SpriteObjects`, `LevelView` constants (Task 5).
- Produces:
  - `ActorPose.Progress(int elapsed, int duration, float alpha) : float`; `ActorPose.Body(ActionKind, float progress, Vector3 from, Vector3 to, Vector3 target) : Vector3`; `ActorPose.FacesLeft(Direction) : bool`; `ActorPose.ArrowDegrees(Direction) : float`; constants `LungeDistance = 0.25f`, `BobHeight = 0.06f`.
  - `ActorView : MonoBehaviour` with `int ActorId`, `bool IsCorpse`, `Color Tint`, `static ActorView Create(Transform, Actor, Sprite, CellSpace)`, `void Sync(Actor, LevelState, CellSpace)`, `void Render(float alpha, float now)`, `void Flash(float now)`.
  - `ActorViews : MonoBehaviour` with `int Count`, `static ActorViews Create(Transform parent, MortarGame? game)`, `ActorView? Find(int actorId)`, `void Sync(World, CellSpace, SpriteCatalog)`, `void Clear()`, `void Flash(int actorId)`, `void RenderNow()`, `static string SpriteSetOf(World, Actor)`.
  - `GroundItemViews : MonoBehaviour` with `const string GoldIcon = "gold"`, `int Count`, `static GroundItemViews Create(Transform parent)`, `void Sync(LevelState, CellSpace, SpriteCatalog)`, `void Clear()`.

- [ ] **Step 1: Write the failing tests**

`Tests/Editor/ActorPoseTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class ActorPoseTests
    {
        private static readonly Vector3 From = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 To = new Vector3(1f, 0f, 0f);

        [Test]
        public void ProgressAddsAlphaAndClamps()
        {
            Assert.AreEqual(0.5f, ActorPose.Progress(3, 7, 0.5f), 1e-4f);
            Assert.AreEqual(1f, ActorPose.Progress(9, 7, 0.5f));
            Assert.AreEqual(1f, ActorPose.Progress(0, 0, 0.3f));
        }

        [Test]
        public void MovingLerpsWithABobAtTheMidpoint()
        {
            var body = ActorPose.Body(ActionKind.Moving, 0.5f, From, To, To);

            Assert.AreEqual(0.5f, body.x, 1e-4f);
            Assert.AreEqual(ActorPose.BobHeight, body.y, 1e-4f);
        }

        [Test]
        public void MovingEndsExactlyOnTheDestination()
        {
            var body = ActorPose.Body(ActionKind.Moving, 1f, From, To, To);

            Assert.AreEqual(To.x, body.x, 1e-4f);
            Assert.AreEqual(To.y, body.y, 1e-4f);
        }

        [Test]
        public void AttackingLungesTowardTheTargetAndReturns()
        {
            var target = new Vector3(1f, 1f, 0f);

            var mid = ActorPose.Body(ActionKind.Attacking, 0.5f, To, To, target);
            var end = ActorPose.Body(ActionKind.Attacking, 1f, To, To, target);

            Assert.AreEqual(To.y + ActorPose.LungeDistance, mid.y, 1e-4f);
            Assert.AreEqual(To.y, end.y, 1e-4f);
        }

        [Test]
        public void IdleStaysOnItsCell()
        {
            Assert.AreEqual(To, ActorPose.Body(ActionKind.Idle, 0.4f, From, To, To));
        }

        [TestCase(Direction.West, true)]
        [TestCase(Direction.SouthWest, true)]
        [TestCase(Direction.South, true)]
        [TestCase(Direction.East, false)]
        [TestCase(Direction.NorthEast, false)]
        [TestCase(Direction.NorthWest, false)]
        public void FacesLeftFollowsTheScreenDirection(Direction facing, bool expected)
        {
            Assert.AreEqual(expected, ActorPose.FacesLeft(facing));
        }

        [Test]
        public void ArrowForEastPointsDownRight()
        {
            Assert.AreEqual(Mathf.Atan2(-0.25f, 0.5f) * Mathf.Rad2Deg, ActorPose.ArrowDegrees(Direction.East), 1e-3f);
        }
    }
}
```

`Tests/Runtime/ActorViewsTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class ActorViewsTests
    {
        private GameObject _root = null!;
        private SpriteCatalog _catalog = null!;
        private MortarGame _game = null!;
        private CellSpace _space = null!;
        private ActorViews _actors = null!;
        private GroundItemViews _items = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("actor-views-test");
            _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
            _game = _root.AddComponent<MortarGame>();
            _space = new CellSpace(CellSpace.CreateGrid(_root.transform));
            _actors = ActorViews.Create(_root.transform, _game);
            _items = GroundItemViews.Create(_root.transform);
            _game.Begin(TestContent.NewSession());
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_catalog);
        }

        private World World => _game.Session!.World;

        [Test]
        public void TownHasOneViewForThePlayer()
        {
            _actors.Sync(World, _space, _catalog);

            Assert.AreEqual(1, _actors.Count);
            Assert.IsNotNull(_actors.Find(Actor.PlayerId));
        }

        [Test]
        public void DeeperLevelHasAViewPerActor()
        {
            Journeys.TakeStairsDown(_game);
            _actors.Clear();

            _actors.Sync(World, _space, _catalog);

            Assert.AreEqual(World.Level.Actors.Count, _actors.Count);
            Assert.Greater(_actors.Count, 1);
        }

        [Test]
        public void PlayerViewSettlesOnThePlayerCellAfterMoving()
        {
            var start = World.Player.Cell;
            _game.Enqueue(new MoveCommand(new Cell(start.X + 1, start.Y)));
            for (var i = 0; i < 30; i++)
            {
                _game.RunTick();
                _actors.Sync(World, _space, _catalog);
            }

            _actors.Find(Actor.PlayerId)!.Render(0f, Time.time);

            var expected = _space.Center(World.Player.Cell);
            Assert.AreEqual(expected, _actors.Find(Actor.PlayerId)!.transform.position);
        }

        [Test]
        public void DroppedItemGetsAGroundView()
        {
            var potion = World.Belongings.Belt.Get(0)!;
            _game.Enqueue(new DropItemCommand(potion.Id));
            _game.RunTick();

            _items.Sync(World.Level, _space, _catalog);

            Assert.AreEqual(1, World.Level.GroundItems.Count);
            Assert.AreEqual(1, _items.Count);
        }
    }
}
```

`PlayerViewSettlesOnThePlayerCellAfterMoving` moves east; if the cell east of the town entrance is not walkable, pick the first walkable neighbour of the start cell with `World.Level.IsFree` instead (search `Directions.All` in order).

- [ ] **Step 2: Run tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: compile errors naming `ActorPose`, `ActorViews`, `GroundItemViews`.

- [ ] **Step 3: Implement the pose math**

`Runtime/Views/ActorPose.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Simulation;
using UnityEngine;

namespace Mortar.Unity
{
    public static class ActorPose
    {
        public const float LungeDistance = 0.25f;
        public const float BobHeight = 0.06f;

        public static float Progress(int elapsed, int duration, float alpha) =>
            duration <= 0 ? 1f : Mathf.Clamp01((elapsed + alpha) / duration);

        public static Vector3 Body(ActionKind action, float progress, Vector3 from, Vector3 to, Vector3 target)
        {
            var arc = Mathf.Sin(Mathf.PI * progress);
            switch (action)
            {
                case ActionKind.Moving:
                    return Vector3.Lerp(from, to, progress) + Vector3.up * (BobHeight * arc);
                case ActionKind.Attacking:
                    var toward = target - to;
                    return toward.sqrMagnitude < 1e-6f ? to : to + toward.normalized * (LungeDistance * arc);
                default:
                    return to;
            }
        }

        public static bool FacesLeft(Direction facing) => IsoMath.ScreenDirection(facing).x < -0.01f;

        public static float ArrowDegrees(Direction facing)
        {
            var direction = IsoMath.ScreenDirection(facing);
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
    }
}
```

`Mathf.Sin(Mathf.PI * 1f)` is about `-8.7e-8`, inside the test tolerances.

- [ ] **Step 4: Implement the actor view**

`Runtime/Views/ActorView.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Simulation;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class ActorView : MonoBehaviour
    {
        public const float FlashSeconds = 0.15f;
        public const float FallSeconds = 0.3f;

        private static readonly Color HitColor = new Color(1f, 0.35f, 0.35f);
        private static readonly Color CorpseColor = new Color(0.45f, 0.45f, 0.45f);
        private static readonly Color ArrowColor = new Color(1f, 0.85f, 0.3f, 0.8f);

        private SpriteRenderer _body = null!;
        private SpriteRenderer _arrow = null!;
        private Vector3 _from;
        private Vector3 _to;
        private Vector3 _target;
        private Cell _cell;
        private ActionKind _action;
        private int _elapsed;
        private int _duration;
        private Direction _facing;
        private float _flashUntil = -1f;
        private float _diedAt = -1f;

        public int ActorId { get; private set; }

        public bool IsCorpse { get; private set; }

        public Color Tint => _body.color;

        public static ActorView Create(Transform parent, Actor actor, Sprite sprite, CellSpace space)
        {
            var body = SpriteObjects.Create(parent, $"Actor {actor.Id}", sprite, LevelView.WorldOrder);
            var view = body.gameObject.AddComponent<ActorView>();
            view._body = body;
            view.ActorId = actor.Id;
            view._arrow = SpriteObjects.Create(body.transform, "Facing", GeneratedSprites.Arrow, LevelView.FloorOrder + 2);
            view._arrow.color = ArrowColor;
            view._cell = actor.Cell;
            view._from = view._to = view._target = space.Center(actor.Cell);
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
            transform.position = ActorPose.Body(IsCorpse ? ActionKind.Idle : _action, progress, _from, _to, _target);
            _body.flipX = ActorPose.FacesLeft(_facing);
            _arrow.enabled = !IsCorpse;
            _arrow.transform.localRotation = Quaternion.Euler(0f, 0f, ActorPose.ArrowDegrees(_facing));
            if (IsCorpse)
            {
                var fall = Mathf.Clamp01((now - _diedAt) / FallSeconds);
                transform.rotation = Quaternion.Euler(0f, 0f, 90f * fall * (_body.flipX ? -1f : 1f));
                _body.color = CorpseColor;
                _body.sortingOrder = LevelView.FloorOrder + 3;
                return;
            }
            transform.rotation = Quaternion.identity;
            _body.color = now < _flashUntil ? HitColor : Color.white;
        }

        public void Flash(float now) => _flashUntil = now + FlashSeconds;
    }
}
```

- [ ] **Step 5: Implement the pools**

`Runtime/Views/ActorViews.cs`:

```csharp
using System.Collections.Generic;
using Mortar.Game.Simulation;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class ActorViews : MonoBehaviour
    {
        private readonly Dictionary<int, ActorView> _views = new Dictionary<int, ActorView>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _stale = new List<int>();
        private MortarGame? _game;

        public int Count => _views.Count;

        public static ActorViews Create(Transform parent, MortarGame? game)
        {
            var go = new GameObject("Actors");
            go.transform.SetParent(parent, false);
            var views = go.AddComponent<ActorViews>();
            views._game = game;
            return views;
        }

        public ActorView? Find(int actorId) => _views.TryGetValue(actorId, out var view) ? view : null;

        public void Sync(World world, CellSpace space, SpriteCatalog catalog)
        {
            var level = world.Level;
            _seen.Clear();
            foreach (var actor in level.Actors)
            {
                if (!_views.TryGetValue(actor.Id, out var view))
                {
                    view = ActorView.Create(transform, actor, catalog.Actor(SpriteSetOf(world, actor)), space);
                    _views.Add(actor.Id, view);
                }
                view.Sync(actor, level, space);
                _seen.Add(actor.Id);
            }
            _stale.Clear();
            foreach (var id in _views.Keys)
            {
                if (!_seen.Contains(id))
                {
                    _stale.Add(id);
                }
            }
            foreach (var id in _stale)
            {
                Destroy(_views[id].gameObject);
                _views.Remove(id);
            }
        }

        public void Clear()
        {
            foreach (var view in _views.Values)
            {
                Destroy(view.gameObject);
            }
            _views.Clear();
        }

        public void Flash(int actorId)
        {
            var view = Find(actorId);
            if (view != null)
            {
                view.Flash(Time.time);
            }
        }

        public void RenderNow() => LateUpdate();

        public static string SpriteSetOf(World world, Actor actor)
        {
            if (actor.Kind == ActorKind.Player)
            {
                return world.PlayerClass.SpriteSet;
            }
            return world.Content.TryGetMonster(actor.DefinitionId, out var monster) ? monster.SpriteSet : actor.DefinitionId;
        }

        private void LateUpdate()
        {
            var alpha = _game != null ? _game.Alpha : 0f;
            var now = Time.time;
            foreach (var view in _views.Values)
            {
                view.Render(alpha, now);
            }
        }
    }
}
```

`Runtime/Views/GroundItemViews.cs`:

```csharp
using System.Collections.Generic;
using Mortar.Game.Simulation;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class GroundItemViews : MonoBehaviour
    {
        public const string GoldIcon = "gold";
        public const float IconScale = 0.5f;

        private readonly Dictionary<int, SpriteRenderer> _views = new Dictionary<int, SpriteRenderer>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _stale = new List<int>();

        public int Count => _views.Count;

        public static GroundItemViews Create(Transform parent)
        {
            var go = new GameObject("Ground Items");
            go.transform.SetParent(parent, false);
            return go.AddComponent<GroundItemViews>();
        }

        public void Sync(LevelState level, CellSpace space, SpriteCatalog catalog)
        {
            _seen.Clear();
            foreach (var ground in level.GroundItems)
            {
                _seen.Add(ground.Id);
                if (_views.ContainsKey(ground.Id))
                {
                    continue;
                }
                var icon = ground.Item != null ? ground.Item.Definition.IconId : GoldIcon;
                var view = SpriteObjects.Create(transform, $"Ground {ground.Id}", catalog.Icon(icon), LevelView.FloorOrder + 1);
                view.transform.position = space.Center(ground.Cell);
                view.transform.localScale = Vector3.one * IconScale;
                _views.Add(ground.Id, view);
            }
            _stale.Clear();
            foreach (var id in _views.Keys)
            {
                if (!_seen.Contains(id))
                {
                    _stale.Add(id);
                }
            }
            foreach (var id in _stale)
            {
                Destroy(_views[id].gameObject);
                _views.Remove(id);
            }
        }

        public void Clear()
        {
            foreach (var view in _views.Values)
            {
                Destroy(view.gameObject);
            }
            _views.Clear();
        }
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: `[EditMode] total 43, passed 43, failed 0`.

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: `[PlayMode] total 16, passed 16, failed 0`.

- [ ] **Step 7: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Views unity/com.mortar.unity/Tests/Editor/ActorPoseTests.cs unity/com.mortar.unity/Tests/Editor/ActorPoseTests.cs.meta unity/com.mortar.unity/Tests/Runtime
git commit -m "add actor and ground item views"
```

---

### Task 7: Click-to-act input

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Input/ClickAction.cs`, `ClickResolver.cs`, `WorldCellQuery.cs`, `StairsWalk.cs`, `PlayerControls.cs`
- Test: `unity/com.mortar.unity/Tests/Editor/ClickResolverTests.cs`, `Tests/Runtime/PlayerControlsTests.cs`

**Interfaces:**
- Consumes: `CellSpace` (Task 2); `MortarGame`, `GameSession`, test helpers (Task 4); `LevelView` (Task 5).
- Produces:
  - `enum ClickKind { Move, Attack, PickUp, Stairs }`; `readonly struct ClickAction(ClickKind kind, Cell cell, int targetId)` with `Kind`, `Cell`, `TargetId`.
  - `interface ICellQuery { int LivingMonsterAt(Cell); int GroundItemAt(Cell); bool IsStairs(Cell); bool IsWalkable(Cell); }`.
  - `ClickResolver.Resolve(ICellQuery, Cell) : ClickAction?`; `ClickResolver.ToCommand(ClickAction) : Command`.
  - `WorldCellQuery(World) : ICellQuery`.
  - `StairsWalk.IsNextToStairs(World) : bool`; `StairsWalk.IsSettled(Actor) : bool`.
  - `PlayerControls : MonoBehaviour` with `const int HoldRepeatFrames = 6`, events `InventoryToggled`, `CharacterToggled`, `MenuRequested` (all `Action`), properties `Func<Vector2, bool> IsOverUi`, `Func<bool> InterceptWorldClick`, `bool HasPendingStairs`, methods `static PlayerControls Create(Transform parent, MortarGame game, Func<CellSpace?> space, Camera? camera)`, `ClickKind? Click(Cell)`, `Cell? CellUnderPointer()`.

- [ ] **Step 1: Write the failing tests**

`Tests/Editor/ClickResolverTests.cs`:

```csharp
using System.Collections.Generic;
using Mortar.Game;
using Mortar.Game.Commands;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class ClickResolverTests
    {
        private static readonly Cell Spot = new Cell(3, 4);

        [Test]
        public void LivingMonsterWinsOverEverythingElse()
        {
            var query = new FakeQuery { Monster = 9, Item = 5, Stairs = true, Walkable = true };

            var action = ClickResolver.Resolve(query, Spot)!.Value;

            Assert.AreEqual(ClickKind.Attack, action.Kind);
            Assert.AreEqual(9, action.TargetId);
        }

        [Test]
        public void GroundItemWinsOverStairsAndFloor()
        {
            var query = new FakeQuery { Item = 5, Stairs = true, Walkable = true };

            Assert.AreEqual(ClickKind.PickUp, ClickResolver.Resolve(query, Spot)!.Value.Kind);
        }

        [Test]
        public void StairsWinOverFloor()
        {
            var query = new FakeQuery { Stairs = true, Walkable = true };

            Assert.AreEqual(ClickKind.Stairs, ClickResolver.Resolve(query, Spot)!.Value.Kind);
        }

        [Test]
        public void WalkableFloorMoves()
        {
            var action = ClickResolver.Resolve(new FakeQuery { Walkable = true }, Spot)!.Value;

            Assert.AreEqual(ClickKind.Move, action.Kind);
            Assert.AreEqual(Spot, action.Cell);
        }

        [Test]
        public void WallsResolveToNothing()
        {
            Assert.IsNull(ClickResolver.Resolve(new FakeQuery(), Spot));
        }

        [Test]
        public void ActionsBecomeTheMatchingCommands()
        {
            Assert.IsInstanceOf<AttackCommand>(ClickResolver.ToCommand(new ClickAction(ClickKind.Attack, Spot, 9)));
            Assert.IsInstanceOf<PickUpCommand>(ClickResolver.ToCommand(new ClickAction(ClickKind.PickUp, Spot, 5)));
            var move = (MoveCommand)ClickResolver.ToCommand(new ClickAction(ClickKind.Move, Spot, 0));
            Assert.AreEqual(Spot, move.Destination);
        }

        private sealed class FakeQuery : ICellQuery
        {
            public int Monster { get; set; }

            public int Item { get; set; }

            public bool Stairs { get; set; }

            public bool Walkable { get; set; }

            public int LivingMonsterAt(Cell cell) => Monster;

            public int GroundItemAt(Cell cell) => Item;

            public bool IsStairs(Cell cell) => Stairs;

            public bool IsWalkable(Cell cell) => Walkable;
        }
    }
}
```

`Tests/Runtime/PlayerControlsTests.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Game;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class PlayerControlsTests
    {
        private GameObject _root = null!;
        private MortarGame _game = null!;
        private PlayerControls _input = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("player-input-test");
            _game = _root.AddComponent<MortarGame>();
            var space = new CellSpace(CellSpace.CreateGrid(_root.transform));
            _input = PlayerControls.Create(_root.transform, _game, () => space, null);
            _game.Begin(TestContent.NewSession());
        }

        [TearDown]
        public void TearDown() => Object.Destroy(_root);

        private World World => _game.Session!.World;

        [Test]
        public void ClickingFloorWalksThePlayerThere()
        {
            var target = FindFloorAway(World, 3);

            Assert.AreEqual(ClickKind.Move, _input.Click(target));
            RunUntil(() => World.Player.Cell == target && StairsWalk.IsSettled(World.Player));

            Assert.AreEqual(target, World.Player.Cell);
        }

        [Test]
        public void ClickingStairsWalksThereAndDescends()
        {
            var exit = Journeys.MarkerCell(World, MarkerKinds.Exit);

            Assert.AreEqual(ClickKind.Stairs, _input.Click(exit));
            Assert.IsTrue(_input.HasPendingStairs);
            RunUntil(() => World.Depth == 1);

            Assert.AreEqual(1, World.Depth);
            Assert.IsFalse(_input.HasPendingStairs);
        }

        [Test]
        public void ClickingAWallDoesNothing()
        {
            var wall = FindWall(World);

            Assert.IsNull(_input.Click(wall));
            _game.RunTick();

            Assert.AreEqual(IntentKind.None, World.Player.Intent);
        }

        private void RunUntil(System.Func<bool> done)
        {
            for (var i = 0; i < Journeys.MaxTicks && !done(); i++)
            {
                _game.RunTick();
            }
        }

        private static Cell FindFloorAway(World world, int distance)
        {
            var start = world.Player.Cell;
            foreach (var direction in Directions.All)
            {
                var cell = new Cell(start.X + direction.DeltaX() * distance, start.Y + direction.DeltaY() * distance);
                if (world.Level.IsFree(cell) && !new WorldCellQuery(world).IsStairs(cell))
                {
                    return cell;
                }
            }
            Assert.Fail("no free floor near the start");
            return start;
        }

        private static Cell FindWall(World world)
        {
            var cells = world.Level.Layout.Cells;
            for (var y = 0; y < cells.Height; y++)
            {
                for (var x = 0; x < cells.Width; x++)
                {
                    if (!cells.IsWalkable(x, y))
                    {
                        return new Cell(x, y);
                    }
                }
            }
            Assert.Fail("no wall cell in the town");
            return default;
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: compile errors naming `ClickResolver`, `ICellQuery`, `PlayerControls`.

- [ ] **Step 3: Implement the click model**

`Runtime/Input/ClickAction.cs`:

```csharp
using Mortar.Game;

namespace Mortar.Unity
{
    public enum ClickKind
    {
        Move,
        Attack,
        PickUp,
        Stairs,
    }

    public readonly struct ClickAction
    {
        public ClickAction(ClickKind kind, Cell cell, int targetId)
        {
            Kind = kind;
            Cell = cell;
            TargetId = targetId;
        }

        public ClickKind Kind { get; }

        public Cell Cell { get; }

        public int TargetId { get; }
    }

    public interface ICellQuery
    {
        int LivingMonsterAt(Cell cell);

        int GroundItemAt(Cell cell);

        bool IsStairs(Cell cell);

        bool IsWalkable(Cell cell);
    }
}
```

`Runtime/Input/ClickResolver.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Commands;

namespace Mortar.Unity
{
    public static class ClickResolver
    {
        public static ClickAction? Resolve(ICellQuery query, Cell cell)
        {
            var monster = query.LivingMonsterAt(cell);
            if (monster != 0)
            {
                return new ClickAction(ClickKind.Attack, cell, monster);
            }
            var item = query.GroundItemAt(cell);
            if (item != 0)
            {
                return new ClickAction(ClickKind.PickUp, cell, item);
            }
            if (query.IsStairs(cell))
            {
                return new ClickAction(ClickKind.Stairs, cell, 0);
            }
            return query.IsWalkable(cell) ? new ClickAction(ClickKind.Move, cell, 0) : (ClickAction?)null;
        }

        public static Command ToCommand(ClickAction action)
        {
            switch (action.Kind)
            {
                case ClickKind.Attack:
                    return new AttackCommand(action.TargetId);
                case ClickKind.PickUp:
                    return new PickUpCommand(action.TargetId);
                default:
                    return new MoveCommand(action.Cell);
            }
        }
    }
}
```

`Runtime/Input/WorldCellQuery.cs`:

```csharp
using Mortar.Core.Model;
using Mortar.Game;
using Mortar.Game.Simulation;

namespace Mortar.Unity
{
    public sealed class WorldCellQuery : ICellQuery
    {
        private readonly World _world;

        public WorldCellQuery(World world)
        {
            _world = world;
        }

        public int LivingMonsterAt(Cell cell)
        {
            var level = _world.Level;
            var actor = level.Find(level.OccupantAt(cell));
            return actor != null && actor.Kind == ActorKind.Monster && actor.IsAlive ? actor.Id : 0;
        }

        public int GroundItemAt(Cell cell)
        {
            foreach (var ground in _world.Level.GroundItems)
            {
                if (ground.Cell == cell)
                {
                    return ground.Id;
                }
            }
            return 0;
        }

        public bool IsStairs(Cell cell)
        {
            foreach (var marker in _world.Level.Layout.Markers)
            {
                if (IsStairsKind(marker.Kind) && marker.X == cell.X && marker.Y == cell.Y)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsWalkable(Cell cell) => _world.Level.IsWalkable(cell);

        public static bool IsStairsKind(string kind) => kind == MarkerKinds.Entrance || kind == MarkerKinds.Exit;
    }
}
```

`Runtime/Input/StairsWalk.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Simulation;

namespace Mortar.Unity
{
    public static class StairsWalk
    {
        public static bool IsNextToStairs(World world)
        {
            var cell = world.Player.Cell;
            foreach (var marker in world.Level.Layout.Markers)
            {
                if (WorldCellQuery.IsStairsKind(marker.Kind) && cell.DistanceTo(new Cell(marker.X, marker.Y)) <= 1)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsSettled(Actor player) => player.Action == ActionKind.Idle && player.Intent == IntentKind.None;
    }
}
```

- [ ] **Step 4: Implement the input component**

`Runtime/Input/PlayerControls.cs`:

```csharp
using System;
using System.Collections.Generic;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Events;
using Mortar.Game.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mortar.Unity
{
    public sealed class PlayerControls : MonoBehaviour
    {
        public const int HoldRepeatFrames = 6;

        private readonly InputAction[] _belt = new InputAction[Belt.Slots];
        private MortarGame _game = null!;
        private Func<CellSpace?> _space = () => null;
        private Camera? _camera;
        private InputAction _click = null!;
        private InputAction _point = null!;
        private InputAction _inventory = null!;
        private InputAction _character = null!;
        private InputAction _menu = null!;
        private bool _holdingMove;
        private int _framesSinceMove;

        public event Action? InventoryToggled;

        public event Action? CharacterToggled;

        public event Action? MenuRequested;

        public Func<Vector2, bool> IsOverUi { get; set; } = position => false;

        public Func<bool> InterceptWorldClick { get; set; } = () => false;

        public bool HasPendingStairs { get; private set; }

        public static PlayerControls Create(Transform parent, MortarGame game, Func<CellSpace?> space, Camera? camera)
        {
            var go = new GameObject("Input");
            go.transform.SetParent(parent, false);
            var input = go.AddComponent<PlayerControls>();
            input._game = game;
            input._space = space;
            input._camera = camera;
            game.Ticked += input.OnTicked;
            return input;
        }

        public ClickKind? Click(Cell cell)
        {
            var session = _game.Session;
            HasPendingStairs = false;
            if (session == null)
            {
                return null;
            }
            var action = ClickResolver.Resolve(new WorldCellQuery(session.World), cell);
            if (action == null)
            {
                return null;
            }
            if (action.Value.Kind == ClickKind.Stairs)
            {
                HasPendingStairs = true;
                _game.Enqueue(new MoveCommand(cell));
            }
            else
            {
                _game.Enqueue(ClickResolver.ToCommand(action.Value));
            }
            return action.Value.Kind;
        }

        public Cell? CellUnderPointer() => CellUnder(_point.ReadValue<Vector2>());

        private void Awake()
        {
            _click = new InputAction("click", InputActionType.Button, "<Mouse>/leftButton");
            _point = new InputAction("point", InputActionType.Value, "<Pointer>/position", expectedControlType: "Vector2");
            _inventory = new InputAction("inventory", InputActionType.Button, "<Keyboard>/i");
            _character = new InputAction("character", InputActionType.Button, "<Keyboard>/c");
            _menu = new InputAction("menu", InputActionType.Button, "<Keyboard>/escape");
            for (var i = 0; i < _belt.Length; i++)
            {
                _belt[i] = new InputAction($"belt{i + 1}", InputActionType.Button, $"<Keyboard>/{i + 1}");
            }
        }

        private IEnumerable<InputAction> Actions()
        {
            yield return _click;
            yield return _point;
            yield return _inventory;
            yield return _character;
            yield return _menu;
            foreach (var action in _belt)
            {
                yield return action;
            }
        }

        private void OnEnable()
        {
            foreach (var action in Actions())
            {
                action.Enable();
            }
        }

        private void OnDisable()
        {
            foreach (var action in Actions())
            {
                action.Disable();
            }
        }

        private void OnDestroy()
        {
            if (_game != null)
            {
                _game.Ticked -= OnTicked;
            }
            foreach (var action in Actions())
            {
                action.Dispose();
            }
        }

        private void Update()
        {
            if (_menu.WasPressedThisFrame())
            {
                MenuRequested?.Invoke();
            }
            if (_game.Session == null || _game.Paused)
            {
                return;
            }
            if (_inventory.WasPressedThisFrame())
            {
                InventoryToggled?.Invoke();
            }
            if (_character.WasPressedThisFrame())
            {
                CharacterToggled?.Invoke();
            }
            for (var i = 0; i < _belt.Length; i++)
            {
                if (_belt[i].WasPressedThisFrame())
                {
                    _game.Enqueue(new UseBeltCommand(i));
                }
            }
            HandlePointer(_point.ReadValue<Vector2>());
        }

        private void HandlePointer(Vector2 screen)
        {
            if (_click.WasPressedThisFrame())
            {
                _holdingMove = false;
                _framesSinceMove = 0;
                if (IsOverUi(screen) || InterceptWorldClick())
                {
                    return;
                }
                var cell = CellUnder(screen);
                _holdingMove = cell.HasValue && Click(cell.Value) == ClickKind.Move;
                return;
            }
            if (!_holdingMove || !_click.IsPressed())
            {
                _holdingMove = false;
                return;
            }
            if (++_framesSinceMove < HoldRepeatFrames)
            {
                return;
            }
            _framesSinceMove = 0;
            var held = CellUnder(screen);
            if (held.HasValue && _game.Session != null && _game.Session.World.Level.IsWalkable(held.Value))
            {
                _game.Enqueue(new MoveCommand(held.Value));
            }
        }

        private void OnTicked(IReadOnlyList<GameEvent> events)
        {
            var session = _game.Session;
            if (!HasPendingStairs || session == null || !StairsWalk.IsSettled(session.World.Player))
            {
                return;
            }
            HasPendingStairs = false;
            if (StairsWalk.IsNextToStairs(session.World))
            {
                _game.Enqueue(new UseStairsCommand());
            }
        }

        private Cell? CellUnder(Vector2 screen)
        {
            var space = _space();
            if (space == null || _camera == null)
            {
                return null;
            }
            var world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            return space.CellAt(world);
        }
    }
}
```

The stairs flow: the click enqueues a move onto the stairs cell; after each tick, once the player is idle with no intent, the input queues `UseStairsCommand` if stairs are within reach. The level change then happens on the following tick.

- [ ] **Step 5: Run tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: `[EditMode] total 49, passed 49, failed 0`.

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: `[PlayMode] total 19, passed 19, failed 0`.

- [ ] **Step 6: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Input unity/com.mortar.unity/Runtime/Input.meta unity/com.mortar.unity/Tests/Editor/ClickResolverTests.cs unity/com.mortar.unity/Tests/Editor/ClickResolverTests.cs.meta unity/com.mortar.unity/Tests/Runtime
git commit -m "add click-to-act player input"
```

---

### Task 8: Event messages and combat feedback

Pure translation from simulation state and events to player-facing text, kept separate from UI so it is fully edit-mode tested.

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Feedback/EventText.cs`, `CombatFeedback.cs`, `MessageLog.cs`, `ExperienceBar.cs`, `HoverText.cs`
- Test: `unity/com.mortar.unity/Tests/Editor/EventTextTests.cs`, `CombatFeedbackTests.cs`, `MessageLogTests.cs`, `ExperienceBarTests.cs`, `HoverTextTests.cs`

**Interfaces:**
- Consumes: `TestContent` (Task 4), `WorldCellQuery` (Task 7).
- Produces:
  - `EventText.Describe(GameEvent) : string?`; `EventText.Sentence(string) : string`.
  - `readonly struct Popup(int actorId, string text, bool hurtsPlayer, bool isHit)` with those properties; `CombatFeedback.For(GameEvent) : Popup?`.
  - `MessageLog` with `const int Capacity = 4`, `const float LifetimeSeconds = 4f`, `void Add(string text, float now)`, `IReadOnlyList<string> Visible(float now)`, `void Clear()`.
  - `ExperienceBar.Fraction(PlayerProgress, GameRules) : float`; `ExperienceBar.NextThreshold(PlayerProgress, GameRules) : long?`.
  - `HoverText.Describe(World, Cell) : string?`.

- [ ] **Step 1: Write the failing tests**

`Tests/Editor/EventTextTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class EventTextTests
    {
        [Test]
        public void RejectionsReadAsSentences() =>
            Assert.AreEqual("There is nothing above", EventText.Describe(new CommandRejected(1, new UseStairsCommand(), "there is nothing above")));

        [Test]
        public void FailedPickupsGiveTheReason() =>
            Assert.AreEqual("Cannot pick that up: no room", EventText.Describe(new PickupFailed(1, 4, "no room")));

        [Test]
        public void PickupsNameTheItem() =>
            Assert.AreEqual("Picked up Iron Knife", EventText.Describe(new ItemPickedUp(1, 4, "Iron Knife")));

        [Test]
        public void GoldPickupsGiveTheAmount() =>
            Assert.AreEqual("Picked up 25 gold", EventText.Describe(new GoldPickedUp(1, 25, 100)));

        [Test]
        public void PotionsSayWhatTheyRestored() =>
            Assert.AreEqual("Restored 30 life", EventText.Describe(new PotionDrunk(1, 3, "life", 30)));

        [Test]
        public void LevelUpsGiveTheNewLevel() =>
            Assert.AreEqual("Reached level 2", EventText.Describe(new LeveledUp(1, 2)));

        [Test]
        public void StatIncreasesNameTheStat() =>
            Assert.AreEqual("Strength is now 25", EventText.Describe(new StatIncreased(1, StatKind.Strength, 25)));

        [Test]
        public void LevelChangesNameTheDestination()
        {
            Assert.AreEqual("Entered depth 1", EventText.Describe(new LevelChanged(1, 0, 1, new Cell(2, 2))));
            Assert.AreEqual("Entered the town", EventText.Describe(new LevelChanged(1, 1, 0, new Cell(2, 2))));
        }

        [Test]
        public void OtherEventsAreSilent() =>
            Assert.IsNull(EventText.Describe(new MonsterAwoke(1, 5)));
    }
}
```

`Tests/Editor/CombatFeedbackTests.cs`:

```csharp
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class CombatFeedbackTests
    {
        [Test]
        public void HitsOnMonstersShowDamage()
        {
            var popup = CombatFeedback.For(new AttackResolved(1, Actor.PlayerId, 7, AttackOutcome.Hit, 12))!.Value;

            Assert.AreEqual(7, popup.ActorId);
            Assert.AreEqual("12", popup.Text);
            Assert.IsFalse(popup.HurtsPlayer);
            Assert.IsTrue(popup.IsHit);
        }

        [Test]
        public void HitsOnThePlayerAreMarked()
        {
            var popup = CombatFeedback.For(new AttackResolved(1, 7, Actor.PlayerId, AttackOutcome.Hit, 3))!.Value;

            Assert.IsTrue(popup.HurtsPlayer);
        }

        [Test]
        public void MissesAndBlocksAreNamed()
        {
            Assert.AreEqual("miss", CombatFeedback.For(new AttackResolved(1, 1, 7, AttackOutcome.Miss, 0))!.Value.Text);
            Assert.AreEqual("block", CombatFeedback.For(new AttackResolved(1, 1, 7, AttackOutcome.Blocked, 0))!.Value.Text);
            Assert.IsFalse(CombatFeedback.For(new AttackResolved(1, 1, 7, AttackOutcome.Miss, 0))!.Value.IsHit);
        }

        [Test]
        public void OtherEventsHaveNoPopup() =>
            Assert.IsNull(CombatFeedback.For(new MonsterAwoke(1, 7)));
    }
}
```

`Tests/Editor/MessageLogTests.cs`:

```csharp
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class MessageLogTests
    {
        [Test]
        public void KeepsOnlyTheNewestMessages()
        {
            var log = new MessageLog();
            for (var i = 0; i < MessageLog.Capacity + 2; i++)
            {
                log.Add($"m{i}", 0f);
            }

            var visible = log.Visible(0f);

            Assert.AreEqual(MessageLog.Capacity, visible.Count);
            Assert.AreEqual("m2", visible[0]);
            Assert.AreEqual($"m{MessageLog.Capacity + 1}", visible[visible.Count - 1]);
        }

        [Test]
        public void MessagesExpire()
        {
            var log = new MessageLog();
            log.Add("old", 0f);
            log.Add("new", 3f);

            CollectionAssert.AreEqual(new[] { "new" }, log.Visible(MessageLog.LifetimeSeconds + 0.5f));
        }

        [Test]
        public void ClearRemovesEverything()
        {
            var log = new MessageLog();
            log.Add("a", 0f);

            log.Clear();

            Assert.IsEmpty(log.Visible(0f));
        }
    }
}
```

`Tests/Editor/ExperienceBarTests.cs`:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Simulation;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class ExperienceBarTests
    {
        private static GameRules Rules => TestContent.Content.Content.Rules;

        private static PlayerProgress Progress(int level, long experience) =>
            new PlayerProgress("warrior", level, experience, new Stats(10, 10, 10, 10), 0);

        [Test]
        public void FirstLevelFillsTowardTheFirstThreshold()
        {
            var threshold = Rules.ExperienceThresholds[0];

            Assert.AreEqual(0.5f, ExperienceBar.Fraction(Progress(1, threshold / 2), Rules), 1e-4f);
            Assert.AreEqual(threshold, ExperienceBar.NextThreshold(Progress(1, 0), Rules));
        }

        [Test]
        public void LaterLevelsFillBetweenThresholds()
        {
            var low = Rules.ExperienceThresholds[0];
            var high = Rules.ExperienceThresholds[1];

            Assert.AreEqual(0.5f, ExperienceBar.Fraction(Progress(2, (low + high) / 2), Rules), 1e-4f);
        }

        [Test]
        public void MaxLevelIsFull()
        {
            Assert.AreEqual(1f, ExperienceBar.Fraction(Progress(Rules.MaxLevel, 0), Rules));
            Assert.IsNull(ExperienceBar.NextThreshold(Progress(Rules.MaxLevel, 0), Rules));
        }
    }
}
```

`Tests/Editor/HoverTextTests.cs`:

```csharp
using Mortar.Game.Commands;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class HoverTextTests
    {
        [Test]
        public void ThePlayerAndEmptyFloorHaveNoHoverText()
        {
            var world = TestContent.NewSession().World;

            Assert.IsNull(HoverText.Describe(world, world.Player.Cell));
        }

        [Test]
        public void GroundItemsShowTheirName()
        {
            var session = TestContent.NewSession();
            var potion = session.World.Belongings.Belt.Get(0)!;
            session.Enqueue(new DropItemCommand(potion.Id));
            session.Tick();
            var ground = session.World.Level.GroundItems[0];

            Assert.AreEqual(potion.Name, HoverText.Describe(session.World, ground.Cell));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: compile errors naming `EventText`, `CombatFeedback`, `MessageLog`, `ExperienceBar`, `HoverText`.

- [ ] **Step 3: Implement the feedback helpers**

`Runtime/Feedback/EventText.cs`:

```csharp
using Mortar.Game.Events;

namespace Mortar.Unity
{
    public static class EventText
    {
        public static string? Describe(GameEvent gameEvent)
        {
            switch (gameEvent)
            {
                case CommandRejected rejected:
                    return Sentence(rejected.Reason);
                case PickupFailed failed:
                    return $"Cannot pick that up: {failed.Reason}";
                case ItemPickedUp picked:
                    return $"Picked up {picked.Name}";
                case GoldPickedUp gold:
                    return $"Picked up {gold.Amount} gold";
                case PotionDrunk potion:
                    return $"Restored {potion.Amount} {potion.Restores}";
                case LeveledUp leveled:
                    return $"Reached level {leveled.Level}";
                case StatIncreased stat:
                    return $"{Sentence(stat.Stat.ToString().ToLowerInvariant())} is now {stat.Value}";
                case LevelChanged changed:
                    return changed.ToDepth == 0 ? "Entered the town" : $"Entered depth {changed.ToDepth}";
                default:
                    return null;
            }
        }

        public static string Sentence(string text) =>
            text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
```

`Runtime/Feedback/CombatFeedback.cs`:

```csharp
using System.Globalization;
using Mortar.Game.Events;
using Mortar.Game.Simulation;

namespace Mortar.Unity
{
    public readonly struct Popup
    {
        public Popup(int actorId, string text, bool hurtsPlayer, bool isHit)
        {
            ActorId = actorId;
            Text = text;
            HurtsPlayer = hurtsPlayer;
            IsHit = isHit;
        }

        public int ActorId { get; }

        public string Text { get; }

        public bool HurtsPlayer { get; }

        public bool IsHit { get; }
    }

    public static class CombatFeedback
    {
        public static Popup? For(GameEvent gameEvent)
        {
            if (!(gameEvent is AttackResolved attack))
            {
                return null;
            }
            var hurtsPlayer = attack.TargetId == Actor.PlayerId;
            switch (attack.Outcome)
            {
                case AttackOutcome.Hit:
                    return new Popup(attack.TargetId, attack.Damage.ToString(CultureInfo.InvariantCulture), hurtsPlayer, true);
                case AttackOutcome.Blocked:
                    return new Popup(attack.TargetId, "block", hurtsPlayer, false);
                default:
                    return new Popup(attack.TargetId, "miss", hurtsPlayer, false);
            }
        }
    }
}
```

`Runtime/Feedback/MessageLog.cs`:

```csharp
using System.Collections.Generic;

namespace Mortar.Unity
{
    public sealed class MessageLog
    {
        public const int Capacity = 4;
        public const float LifetimeSeconds = 4f;

        private readonly List<KeyValuePair<string, float>> _entries = new List<KeyValuePair<string, float>>();

        public void Add(string text, float now)
        {
            _entries.Add(new KeyValuePair<string, float>(text, now));
            if (_entries.Count > Capacity)
            {
                _entries.RemoveAt(0);
            }
        }

        public IReadOnlyList<string> Visible(float now)
        {
            var visible = new List<string>(_entries.Count);
            foreach (var entry in _entries)
            {
                if (now - entry.Value < LifetimeSeconds)
                {
                    visible.Add(entry.Key);
                }
            }
            return visible;
        }

        public void Clear() => _entries.Clear();
    }
}
```

`Runtime/Feedback/ExperienceBar.cs`:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Simulation;
using UnityEngine;

namespace Mortar.Unity
{
    public static class ExperienceBar
    {
        public static long? NextThreshold(PlayerProgress progress, GameRules rules) =>
            progress.Level >= rules.MaxLevel ? (long?)null : rules.ExperienceThresholds[progress.Level - 1];

        public static float Fraction(PlayerProgress progress, GameRules rules)
        {
            var next = NextThreshold(progress, rules);
            if (!next.HasValue)
            {
                return 1f;
            }
            var floor = progress.Level >= 2 ? rules.ExperienceThresholds[progress.Level - 2] : 0L;
            var span = next.Value - floor;
            return span <= 0 ? 1f : Mathf.Clamp01((float)(progress.Experience - floor) / span);
        }
    }
}
```

`Runtime/Feedback/HoverText.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Simulation;

namespace Mortar.Unity
{
    public static class HoverText
    {
        public static string? Describe(World world, Cell cell)
        {
            var query = new WorldCellQuery(world);
            var monsterId = query.LivingMonsterAt(cell);
            if (monsterId != 0)
            {
                var monster = world.Level.Find(monsterId)!;
                var name = world.Content.TryGetMonster(monster.DefinitionId, out var definition) ? definition.Name : monster.DefinitionId;
                return $"{name} ({monster.Life}/{monster.Profile.MaxLife})";
            }
            var groundId = query.GroundItemAt(cell);
            return groundId == 0 ? null : world.Level.FindGroundItem(groundId)?.Describe();
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: `[EditMode] total 70, passed 70, failed 0`.

- [ ] **Step 5: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Feedback unity/com.mortar.unity/Runtime/Feedback.meta unity/com.mortar.unity/Tests/Editor
git commit -m "add event messages and combat feedback"
```

---

### Task 9: UI root and HUD

**Files:**
- Create: `unity/com.mortar.unity/Runtime/UI/Ui.cs`, `UiRoot.cs`, `Orb.cs`, `BeltBar.cs`, `FloatingTexts.cs`, `HudView.cs`
- Test: `unity/com.mortar.unity/Tests/Runtime/HudTests.cs`

**Interfaces:**
- Consumes: `SpriteCatalog` (Task 3); `TestContent` (Task 4); `ExperienceBar` (Task 8).
- Produces:
  - `Ui` static styling helpers: colors `PanelColor`, `BorderColor`, `TextColor`, `MagicColor`, `HeldColor`, `SlotColor`; `Box(string name)`, `Text(string text, int size = 14)`, `MenuButton(string text, Action onClick)`, `Icon(Sprite? sprite, float size)`, `Border(VisualElement, float, Color)`, `Padding(VisualElement, float)`, `Radius(VisualElement, float)`, `Fill(VisualElement)`, `Place(VisualElement, float? left, float? top, float? right, float? bottom)`, `Show(VisualElement, bool)`, `IsShown(VisualElement)`.
  - `UiRoot : MonoBehaviour` with `VisualElement Root`, `Overlay`, `Hud`, `Screens`, `Modal`; `static UiRoot Create(Transform parent, PanelSettings settings)`; `bool IsPointerOverUi(Vector2 screenPosition)`; `static PanelSettings CreatePanelSettings(ThemeStyleSheet? theme)`.
  - `Orb(string name, Color color)` with `VisualElement Element`, `string Text`, `void Set(int current, int max)`.
  - `BeltBar(SpriteCatalog)` with `VisualElement Element`, `event Action<int> SlotClicked`, `int FilledSlots`, `void Refresh(Belt)`.
  - `FloatingTexts(VisualElement layer)` with `const float Lifetime = 0.8f`, `int Count`, `void Spawn(string text, Vector3 world, Color color, float now)`, `void Update(Camera? camera, float now)`, `void Clear()`.
  - `HudView(UiRoot, SpriteCatalog)` with `Orb Life`, `Orb Mana`, `BeltBar Belt`, `IReadOnlyList<string> MessageTexts`, `string HoverTextShown`, `void Refresh(World, IReadOnlyList<string> messages, string? hover)`, `void Show(bool)`.

- [ ] **Step 1: Write the failing tests**

`Tests/Runtime/HudTests.cs`:

```csharp
using System.Collections;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mortar.Unity.Tests
{
    public sealed class HudTests
    {
        private GameObject _root = null!;
        private SpriteCatalog _catalog = null!;
        private UiRoot _ui = null!;
        private HudView _hud = null!;
        private World _world = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("hud-test");
            _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
            _ui = UiRoot.Create(_root.transform, UiRoot.CreatePanelSettings(null));
            _hud = new HudView(_ui, _catalog);
            _world = TestContent.NewSession().World;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_catalog);
        }

        [Test]
        public void OrbsShowLifeAndMana()
        {
            _hud.Refresh(_world, new string[0], null);

            var player = _world.Player;
            Assert.AreEqual($"{player.Life}/{player.Profile.MaxLife}", _hud.Life.Text);
            Assert.AreEqual($"{player.Mana}/{player.Profile.MaxMana}", _hud.Mana.Text);
        }

        [Test]
        public void BeltShowsTheStartingPotions()
        {
            _hud.Refresh(_world, new string[0], null);

            Assert.AreEqual(2, _hud.Belt.FilledSlots);
        }

        [Test]
        public void MessagesAndHoverTextAreShown()
        {
            _hud.Refresh(_world, new[] { "Entered depth 1" }, "Healing Draught");

            CollectionAssert.AreEqual(new[] { "Entered depth 1" }, _hud.MessageTexts);
            Assert.AreEqual("Healing Draught", _hud.HoverTextShown);
        }

        [Test]
        public void FloatingTextsExpire()
        {
            var texts = new FloatingTexts(_ui.Overlay);
            texts.Spawn("12", Vector3.zero, Color.white, 0f);

            texts.Update(null, FloatingTexts.Lifetime + 0.1f);

            Assert.AreEqual(0, texts.Count);
        }

        [UnityTest]
        public IEnumerator EmptyScreenCenterIsNotUi()
        {
            _hud.Refresh(_world, new string[0], null);
            yield return null;

            Assert.IsFalse(_ui.IsPointerOverUi(new Vector2(Screen.width / 2f, Screen.height / 2f)));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: compile errors naming `UiRoot`, `HudView`, `FloatingTexts`.

- [ ] **Step 3: Implement the style helpers and root**

`Runtime/UI/Ui.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public static class Ui
    {
        public static readonly Color PanelColor = new Color(0.08f, 0.07f, 0.06f, 0.94f);
        public static readonly Color BorderColor = new Color(0.45f, 0.36f, 0.22f);
        public static readonly Color TextColor = new Color(0.9f, 0.86f, 0.76f);
        public static readonly Color MagicColor = new Color(0.45f, 0.6f, 1f);
        public static readonly Color HeldColor = new Color(0.95f, 0.8f, 0.3f);
        public static readonly Color SlotColor = new Color(0.14f, 0.12f, 0.1f, 0.95f);

        public static VisualElement Box(string name)
        {
            var box = new VisualElement { name = name };
            box.style.backgroundColor = PanelColor;
            Border(box, 2f, BorderColor);
            Padding(box, 10f);
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
            return button;
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

`Runtime/UI/UiRoot.cs`:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class UiRoot : MonoBehaviour
    {
        private UIDocument _document = null!;

        public VisualElement Root => _document.rootVisualElement;

        public VisualElement Overlay { get; private set; } = null!;

        public VisualElement Hud { get; private set; } = null!;

        public VisualElement Screens { get; private set; } = null!;

        public VisualElement Modal { get; private set; } = null!;

        public static UiRoot Create(Transform parent, PanelSettings settings)
        {
            var go = new GameObject("UI");
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = settings;
            var ui = go.AddComponent<UiRoot>();
            ui._document = document;
            go.SetActive(true);
            ui.BuildLayers();
            return ui;
        }

        public static PanelSettings CreatePanelSettings(ThemeStyleSheet? theme)
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.themeStyleSheet = theme;
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1280, 720);
            settings.match = 0.5f;
            return settings;
        }

        public bool IsPointerOverUi(Vector2 screenPosition)
        {
            var panel = Root.panel;
            if (panel == null)
            {
                return false;
            }
            var point = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            return panel.Pick(point) != null;
        }

        private void BuildLayers()
        {
            Root.pickingMode = PickingMode.Ignore;
            Overlay = Layer("overlay");
            Hud = Layer("hud");
            Screens = Layer("screens");
            Modal = Layer("modal");
        }

        private VisualElement Layer(string name)
        {
            var layer = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            Ui.Fill(layer);
            Root.Add(layer);
            return layer;
        }
    }
}
```

- [ ] **Step 4: Implement orbs, belt, and floating texts**

`Runtime/UI/Orb.cs`:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class Orb
    {
        public const float Size = 96f;

        private readonly VisualElement _fill;
        private readonly Label _label;

        public Orb(string name, Color color)
        {
            Element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            Element.style.width = Size;
            Element.style.height = Size;
            Element.style.overflow = Overflow.Hidden;
            Element.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.9f);
            Ui.Radius(Element, Size / 2f);
            Ui.Border(Element, 3f, Ui.BorderColor);
            _fill = new VisualElement { pickingMode = PickingMode.Ignore };
            Ui.Place(_fill, 0f, null, 0f, 0f);
            _fill.style.backgroundColor = color;
            Element.Add(_fill);
            _label = Ui.Text("", 13);
            Ui.Place(_label, 0f, Size / 2f - 10f, 0f, null);
            _label.style.unityTextAlign = TextAnchor.MiddleCenter;
            Element.Add(_label);
        }

        public VisualElement Element { get; }

        public string Text => _label.text;

        public void Set(int current, int max)
        {
            var fraction = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            _fill.style.height = Length.Percent(fraction * 100f);
            _label.text = $"{Mathf.Max(0, current)}/{max}";
        }
    }
}
```

`Runtime/UI/BeltBar.cs`:

```csharp
using System;
using Mortar.Game.Items;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class BeltBar
    {
        public const float SlotSize = 44f;

        private readonly SpriteCatalog _catalog;
        private readonly Image[] _icons = new Image[Belt.Slots];

        public BeltBar(SpriteCatalog catalog)
        {
            _catalog = catalog;
            Element = new VisualElement { name = "belt", pickingMode = PickingMode.Ignore };
            Element.style.flexDirection = FlexDirection.Row;
            for (var i = 0; i < Belt.Slots; i++)
            {
                Element.Add(CreateSlot(i));
            }
        }

        public event Action<int>? SlotClicked;

        public VisualElement Element { get; }

        public int FilledSlots
        {
            get
            {
                var filled = 0;
                foreach (var icon in _icons)
                {
                    if (icon.sprite != null) filled++;
                }
                return filled;
            }
        }

        public void Refresh(Belt belt)
        {
            for (var i = 0; i < Belt.Slots; i++)
            {
                var item = belt.Get(i);
                _icons[i].sprite = item != null ? _catalog.Icon(item.Definition.IconId) : null;
            }
        }

        private VisualElement CreateSlot(int index)
        {
            var slot = new VisualElement { name = $"belt-{index + 1}" };
            slot.style.width = SlotSize;
            slot.style.height = SlotSize;
            slot.style.marginLeft = 2f;
            slot.style.marginRight = 2f;
            slot.style.backgroundColor = Ui.SlotColor;
            slot.style.alignItems = Align.Center;
            slot.style.justifyContent = Justify.Center;
            Ui.Border(slot, 1f, Ui.BorderColor);
            _icons[index] = Ui.Icon(null, SlotSize - 8f);
            slot.Add(_icons[index]);
            var key = Ui.Text((index + 1).ToString(), 10);
            Ui.Place(key, 2f, 0f, null, null);
            slot.Add(key);
            slot.RegisterCallback<PointerDownEvent>(evt => SlotClicked?.Invoke(index));
            return slot;
        }
    }
}
```

`Runtime/UI/FloatingTexts.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class FloatingTexts
    {
        public const float Lifetime = 0.8f;
        public const float StartHeight = 0.9f;
        public const float Rise = 0.6f;

        private readonly VisualElement _layer;
        private readonly List<Entry> _entries = new List<Entry>();

        public FloatingTexts(VisualElement layer)
        {
            _layer = layer;
        }

        public int Count => _entries.Count;

        public void Spawn(string text, Vector3 world, Color color, float now)
        {
            var label = Ui.Text(text, 16);
            label.style.color = color;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.position = Position.Absolute;
            _layer.Add(label);
            _entries.Add(new Entry(label, world, now));
        }

        public void Update(Camera? camera, float now)
        {
            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                var age = now - entry.Start;
                if (age >= Lifetime)
                {
                    entry.Label.RemoveFromHierarchy();
                    _entries.RemoveAt(i);
                    continue;
                }
                if (camera == null || _layer.panel == null)
                {
                    continue;
                }
                var height = StartHeight + Rise * age / Lifetime;
                var point = RuntimePanelUtils.CameraTransformWorldToPanel(_layer.panel, entry.World + Vector3.up * height, camera);
                entry.Label.style.left = point.x - 12f;
                entry.Label.style.top = point.y;
                entry.Label.style.opacity = 1f - age / Lifetime;
            }
        }

        public void Clear()
        {
            foreach (var entry in _entries)
            {
                entry.Label.RemoveFromHierarchy();
            }
            _entries.Clear();
        }

        private sealed class Entry
        {
            public Entry(Label label, Vector3 world, float start)
            {
                Label = label;
                World = world;
                Start = start;
            }

            public Label Label { get; }

            public Vector3 World { get; }

            public float Start { get; }
        }
    }
}
```

- [ ] **Step 5: Implement the HUD**

`Runtime/UI/HudView.cs`:

```csharp
using System.Collections.Generic;
using Mortar.Game.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class HudView
    {
        public const float ExperienceWidth = 360f;

        private readonly VisualElement _bar;
        private readonly VisualElement _experienceFill;
        private readonly VisualElement _messages;
        private readonly Label _hover;
        private readonly List<string> _shownMessages = new List<string>();

        public HudView(UiRoot ui, SpriteCatalog catalog)
        {
            Life = new Orb("life", new Color(0.65f, 0.08f, 0.08f));
            Mana = new Orb("mana", new Color(0.12f, 0.2f, 0.7f));
            Belt = new BeltBar(catalog);
            _experienceFill = new VisualElement { pickingMode = PickingMode.Ignore };
            _bar = BuildBottomBar(BuildCenter());
            ui.Hud.Add(_bar);
            _messages = new VisualElement { name = "messages", pickingMode = PickingMode.Ignore };
            Ui.Place(_messages, 0f, 12f, 0f, null);
            _messages.style.alignItems = Align.Center;
            ui.Hud.Add(_messages);
            _hover = Ui.Text("", 15);
            Ui.Place(_hover, 0f, 110f, 0f, null);
            _hover.style.unityTextAlign = TextAnchor.MiddleCenter;
            ui.Hud.Add(_hover);
        }

        public Orb Life { get; }

        public Orb Mana { get; }

        public BeltBar Belt { get; }

        public IReadOnlyList<string> MessageTexts => _shownMessages;

        public string HoverTextShown => _hover.text;

        public void Refresh(World world, IReadOnlyList<string> messages, string? hover)
        {
            var player = world.Player;
            Life.Set(player.Life, player.Profile.MaxLife);
            Mana.Set(player.Mana, player.Profile.MaxMana);
            Belt.Refresh(world.Belongings.Belt);
            _experienceFill.style.width = Length.Percent(ExperienceBar.Fraction(world.Progress, world.Content.Rules) * 100f);
            ShowMessages(messages);
            _hover.text = hover ?? "";
        }

        public void Show(bool visible)
        {
            Ui.Show(_bar, visible);
            Ui.Show(_messages, visible);
            Ui.Show(_hover, visible);
        }

        private void ShowMessages(IReadOnlyList<string> messages)
        {
            if (SameMessages(messages))
            {
                return;
            }
            _shownMessages.Clear();
            _shownMessages.AddRange(messages);
            _messages.Clear();
            foreach (var message in messages)
            {
                _messages.Add(Ui.Text(message, 15));
            }
        }

        private bool SameMessages(IReadOnlyList<string> messages)
        {
            if (messages.Count != _shownMessages.Count)
            {
                return false;
            }
            for (var i = 0; i < messages.Count; i++)
            {
                if (messages[i] != _shownMessages[i]) return false;
            }
            return true;
        }

        private VisualElement BuildCenter()
        {
            var center = new VisualElement { pickingMode = PickingMode.Ignore };
            center.style.alignItems = Align.Center;
            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.style.width = ExperienceWidth;
            track.style.height = 6f;
            track.style.marginBottom = 6f;
            track.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            _experienceFill.style.height = Length.Percent(100f);
            _experienceFill.style.backgroundColor = new Color(0.8f, 0.7f, 0.3f);
            track.Add(_experienceFill);
            center.Add(track);
            center.Add(Belt.Element);
            return center;
        }

        private VisualElement BuildBottomBar(VisualElement center)
        {
            var bar = new VisualElement { name = "hud-bar", pickingMode = PickingMode.Ignore };
            Ui.Place(bar, 16f, null, 16f, 8f);
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.justifyContent = Justify.SpaceBetween;
            bar.style.alignItems = Align.FlexEnd;
            bar.Add(Life.Element);
            bar.Add(center);
            bar.Add(Mana.Element);
            return bar;
        }
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: `[PlayMode] total 24, passed 24, failed 0`.

- [ ] **Step 7: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/UI unity/com.mortar.unity/Runtime/UI.meta unity/com.mortar.unity/Tests/Runtime
git commit -m "add ui root and hud"
```

---

### Task 10: Inventory, equipment, and tooltips

Interaction model (Design Interpretation 2): left-click a bag item to hold it, left-click a bag cell, equipment slot, or belt slot to place it, left-click the world to drop it; right-click is the quick action (equip, unequip, or belt a potion). Only bag items can be held, which keeps every placement a single existing command.

**Files:**
- Create: `unity/com.mortar.unity/Runtime/Feedback/ItemText.cs`, `Runtime/Feedback/InventoryCursor.cs`
- Create: `unity/com.mortar.unity/Runtime/UI/ItemTooltip.cs`, `EquipmentPanel.cs`, `InventoryScreen.cs`
- Test: `unity/com.mortar.unity/Tests/Editor/ItemTextTests.cs`, `Tests/Editor/InventoryCursorTests.cs`, `Tests/Runtime/InventoryScreenTests.cs`

**Interfaces:**
- Consumes: `Ui`, `UiRoot` (Task 9); `MortarGame`, `TestContent` (Task 4); `SpriteCatalog` (Task 3).
- Produces:
  - `ItemText.Lines(Item) : IReadOnlyList<string>`; `ItemText.BonusName(string bonus) : string`; `ItemText.NameColor(Item) : Color`.
  - `InventoryCursor` with `int HeldItemId`, `bool IsHolding`, `void Release()`, `void Validate(Inventory)`, `Command? ClickBag(Cell, Inventory)`, `Command ClickBelt(int slot)`, `Command? ClickEquipment(EquipmentSlot, Equipment)`, `Command? Drop()`, `static Command? QuickUse(Item, Belongings)`.
  - `ItemTooltip` with `VisualElement Element`, `void Show(Item?)`, `IReadOnlyList<string> Lines`.
  - `EquipmentPanel(SpriteCatalog)` with `VisualElement Element`, `event Action<EquipmentSlot, int> SlotClicked` (slot, mouse button), `event Action<EquipmentSlot?> SlotHovered`, `int FilledCount`, `void Refresh(Equipment)`.
  - `InventoryScreen(UiRoot, MortarGame, SpriteCatalog)` with `const float CellSize = 36f`, `InventoryCursor Cursor`, `EquipmentPanel Equipment`, `bool IsOpen`, `int ItemElementCount`, `string GoldText`, `void Toggle()`, `void Open()`, `void Close()`, `bool DropHeld()`, `void Refresh(Belongings)`.

- [ ] **Step 1: Write the failing edit-mode tests**

`Tests/Editor/ItemTextTests.cs`:

```csharp
using Mortar.Game.Items;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class ItemTextTests
    {
        private static Belongings StartingGear() => TestContent.NewSession().World.Belongings;

        [Test]
        public void WeaponsShowTheirDamage()
        {
            var knife = StartingGear().Equipment.Get(EquipmentSlot.Weapon)!;

            CollectionAssert.AreEqual(new[] { "Iron Knife", "Damage: 2-5" }, ItemText.Lines(knife));
        }

        [Test]
        public void ArmorShowsItsRolledValue()
        {
            var vest = StartingGear().Equipment.Get(EquipmentSlot.Body)!;

            Assert.AreEqual($"Armor: {vest.ArmorValue}", ItemText.Lines(vest)[1]);
            Assert.That(vest.ArmorValue, Is.InRange(3, 6));
        }

        [Test]
        public void PotionsShowWhatTheyRestore()
        {
            var potion = StartingGear().Belt.Get(0)!;

            CollectionAssert.AreEqual(new[] { "Healing Draught", "Restores 30 life" }, ItemText.Lines(potion));
        }

        [Test]
        public void BonusNamesAreReadable()
        {
            Assert.AreEqual("armor class", ItemText.BonusName("armorClass"));
            Assert.AreEqual("to hit", ItemText.BonusName("toHit"));
            Assert.AreEqual("strength", ItemText.BonusName("strength"));
        }
    }
}
```

`Tests/Editor/InventoryCursorTests.cs`:

```csharp
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Items;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class InventoryCursorTests
    {
        private GameSession _session = null!;
        private Belongings _gear = null!;
        private Item _knife = null!;
        private InventoryCursor _cursor = null!;

        [SetUp]
        public void SetUp()
        {
            _session = TestContent.NewSession();
            _gear = _session.World.Belongings;
            _knife = _gear.Equipment.Get(EquipmentSlot.Weapon)!;
            _cursor = new InventoryCursor();
            Run(new UnequipCommand(EquipmentSlot.Weapon));
        }

        private void Run(Command? command)
        {
            Assert.IsNotNull(command);
            _session.Enqueue(command!);
            _session.Tick();
        }

        private Cell KnifeCell => _gear.Inventory.Find(_knife.Id)!.Cell;

        [Test]
        public void ClickingABagItemHoldsIt()
        {
            Assert.IsNull(_cursor.ClickBag(KnifeCell, _gear.Inventory));

            Assert.AreEqual(_knife.Id, _cursor.HeldItemId);
        }

        [Test]
        public void ClickingAnotherCellMovesTheHeldItem()
        {
            _cursor.ClickBag(KnifeCell, _gear.Inventory);

            Run(_cursor.ClickBag(new Cell(5, 0), _gear.Inventory));

            Assert.AreEqual(new Cell(5, 0), KnifeCell);
            Assert.IsFalse(_cursor.IsHolding);
        }

        [Test]
        public void ClickingAnEquipmentSlotWhileHoldingEquips()
        {
            _cursor.ClickBag(KnifeCell, _gear.Inventory);

            Run(_cursor.ClickEquipment(EquipmentSlot.Weapon, _gear.Equipment));

            Assert.AreEqual(_knife.Id, _gear.Equipment.Get(EquipmentSlot.Weapon)!.Id);
        }

        [Test]
        public void ClickingFilledEquipmentUnequipsAndEmptyDoesNothing()
        {
            Assert.IsInstanceOf<UnequipCommand>(_cursor.ClickEquipment(EquipmentSlot.Body, _gear.Equipment));
            Assert.IsNull(_cursor.ClickEquipment(EquipmentSlot.Head, _gear.Equipment));
        }

        [Test]
        public void DroppingReleasesTheHeldItem()
        {
            Assert.IsNull(_cursor.Drop());
            _cursor.ClickBag(KnifeCell, _gear.Inventory);

            Run(_cursor.Drop());

            Assert.IsFalse(_cursor.IsHolding);
            Assert.IsNull(_gear.Inventory.Find(_knife.Id));
        }

        [Test]
        public void BeltClicksUseTheSlotWhenNothingIsHeld()
        {
            var command = (UseBeltCommand)_cursor.ClickBelt(3);

            Assert.AreEqual(3, command.Slot);
        }

        [Test]
        public void QuickUseEquipsBagItemsAndUnequipsWornOnes()
        {
            Assert.IsInstanceOf<EquipCommand>(InventoryCursor.QuickUse(_knife, _gear));

            var vest = _gear.Equipment.Get(EquipmentSlot.Body)!;
            Assert.IsInstanceOf<UnequipCommand>(InventoryCursor.QuickUse(vest, _gear));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: compile errors naming `ItemText` and `InventoryCursor`.

- [ ] **Step 3: Implement item text and the cursor**

`Runtime/Feedback/ItemText.cs`:

```csharp
using System.Collections.Generic;
using Mortar.Game.Content;
using Mortar.Game.Items;
using UnityEngine;

namespace Mortar.Unity
{
    public static class ItemText
    {
        public static IReadOnlyList<string> Lines(Item item)
        {
            var definition = item.Definition;
            var lines = new List<string> { item.Name };
            if (definition.Damage.HasValue)
            {
                lines.Add($"Damage: {definition.Damage.Value.Min}-{definition.Damage.Value.Max}");
            }
            if (definition.Armor.HasValue)
            {
                lines.Add($"Armor: {item.ArmorValue}");
            }
            if (definition.BlockChance > 0)
            {
                lines.Add($"Block: {definition.BlockChance}%");
            }
            if (definition.Potion != null)
            {
                lines.Add($"Restores {definition.Potion.Amount} {definition.Potion.Restores}");
            }
            AddAffix(lines, item.Prefix);
            AddAffix(lines, item.Suffix);
            foreach (var kind in Stats.Kinds)
            {
                var required = definition.Requirements.Get(kind);
                if (required > 0)
                {
                    lines.Add($"Requires {kind} {required}");
                }
            }
            return lines;
        }

        public static string BonusName(string bonus)
        {
            switch (bonus)
            {
                case BonusKinds.ArmorClass:
                    return "armor class";
                case BonusKinds.ToHit:
                    return "to hit";
                default:
                    return bonus;
            }
        }

        public static Color NameColor(Item item) => item.Quality == ItemQuality.Magic ? Ui.MagicColor : Ui.TextColor;

        private static void AddAffix(List<string> lines, RolledAffix? affix)
        {
            if (affix != null)
            {
                var sign = affix.Value >= 0 ? "+" : "";
                lines.Add($"{sign}{affix.Value} {BonusName(affix.Definition.Bonus)}");
            }
        }
    }
}
```

`Runtime/Feedback/InventoryCursor.cs`:

```csharp
using System;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Items;

namespace Mortar.Unity
{
    public sealed class InventoryCursor
    {
        public int HeldItemId { get; private set; }

        public bool IsHolding => HeldItemId != 0;

        public void Release() => HeldItemId = 0;

        public void Validate(Inventory inventory)
        {
            if (IsHolding && inventory.Find(HeldItemId) == null)
            {
                Release();
            }
        }

        public Command? ClickBag(Cell cell, Inventory inventory)
        {
            if (IsHolding)
            {
                return Take(id => new MoveItemCommand(id, cell));
            }
            HeldItemId = inventory.ItemAt(cell);
            return null;
        }

        public Command ClickBelt(int slot) =>
            IsHolding ? Take(id => new PutInBeltCommand(id, slot)) : new UseBeltCommand(slot);

        public Command? ClickEquipment(EquipmentSlot slot, Equipment equipment)
        {
            if (IsHolding)
            {
                return Take(id => new EquipCommand(id));
            }
            return equipment.Get(slot) != null ? new UnequipCommand(slot) : null;
        }

        public Command? Drop() => IsHolding ? Take(id => new DropItemCommand(id)) : null;

        public static Command? QuickUse(Item item, Belongings belongings)
        {
            var worn = belongings.Equipment.SlotOf(item.Id);
            if (worn.HasValue)
            {
                return new UnequipCommand(worn.Value);
            }
            if (item.Definition.IsPotion)
            {
                var slot = belongings.Belt.FirstEmpty();
                return slot >= 0 ? new PutInBeltCommand(item.Id, slot) : null;
            }
            return item.Definition.IsEquippable ? new EquipCommand(item.Id) : null;
        }

        private Command Take(Func<int, Command> create)
        {
            var command = create(HeldItemId);
            Release();
            return command;
        }
    }
}
```

- [ ] **Step 4: Run edit-mode tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: `[EditMode] total 81, passed 81, failed 0`.

- [ ] **Step 5: Write the failing play-mode test**

`Tests/Runtime/InventoryScreenTests.cs`:

```csharp
using Mortar.Game.Commands;
using Mortar.Game.Items;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class InventoryScreenTests
    {
        private GameObject _root = null!;
        private SpriteCatalog _catalog = null!;
        private MortarGame _game = null!;
        private InventoryScreen _screen = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("inventory-test");
            _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
            _game = _root.AddComponent<MortarGame>();
            _game.Begin(TestContent.NewSession());
            _game.Paused = true;
            var ui = UiRoot.Create(_root.transform, UiRoot.CreatePanelSettings(null));
            _screen = new InventoryScreen(ui, _game, _catalog);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_catalog);
        }

        private Belongings Gear => _game.Session!.World.Belongings;

        [Test]
        public void OpeningShowsEquippedItemsAndGold()
        {
            _screen.Open();
            _screen.Refresh(Gear);

            Assert.IsTrue(_screen.IsOpen);
            Assert.AreEqual(2, _screen.Equipment.FilledCount);
            Assert.AreEqual(0, _screen.ItemElementCount);
            Assert.AreEqual("Gold: 0", _screen.GoldText);
        }

        [Test]
        public void UnequippedItemsAppearInTheBag()
        {
            _game.Session!.Enqueue(new UnequipCommand(EquipmentSlot.Weapon));
            _game.RunTick();
            _screen.Open();

            _screen.Refresh(Gear);

            Assert.AreEqual(1, _screen.ItemElementCount);
            Assert.AreEqual(1, _screen.Equipment.FilledCount);
        }

        [Test]
        public void ClosingReleasesTheHeldItem()
        {
            _game.Session!.Enqueue(new UnequipCommand(EquipmentSlot.Weapon));
            _game.RunTick();
            _screen.Open();
            _screen.Cursor.ClickBag(Gear.Inventory.Items[0].Cell, Gear.Inventory);

            _screen.Toggle();

            Assert.IsFalse(_screen.IsOpen);
            Assert.IsFalse(_screen.Cursor.IsHolding);
        }
    }
}
```

The game is paused so `Update` cannot tick on its own; commands go straight to the session because `MortarGame.Enqueue` ignores input while paused.

- [ ] **Step 6: Run tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: compile error naming `InventoryScreen`.

- [ ] **Step 7: Implement the tooltip and equipment panel**

`Runtime/UI/ItemTooltip.cs`:

```csharp
using System.Collections.Generic;
using Mortar.Game.Items;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class ItemTooltip
    {
        private readonly List<string> _lines = new List<string>();

        public ItemTooltip()
        {
            Element = new VisualElement { name = "tooltip", pickingMode = PickingMode.Ignore };
            Element.style.minHeight = 72f;
            Element.style.marginTop = 8f;
        }

        public VisualElement Element { get; }

        public IReadOnlyList<string> Lines => _lines;

        public void Show(Item? item)
        {
            Element.Clear();
            _lines.Clear();
            if (item == null)
            {
                return;
            }
            _lines.AddRange(ItemText.Lines(item));
            for (var i = 0; i < _lines.Count; i++)
            {
                var label = Ui.Text(_lines[i], i == 0 ? 15 : 13);
                if (i == 0)
                {
                    label.style.color = ItemText.NameColor(item);
                }
                Element.Add(label);
            }
        }
    }
}
```

`Runtime/UI/EquipmentPanel.cs`:

```csharp
using System;
using System.Collections.Generic;
using Mortar.Game.Items;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class EquipmentPanel
    {
        public const float SlotSize = 64f;

        private readonly SpriteCatalog _catalog;
        private readonly Dictionary<EquipmentSlot, Image> _icons = new Dictionary<EquipmentSlot, Image>();

        public EquipmentPanel(SpriteCatalog catalog)
        {
            _catalog = catalog;
            Element = new VisualElement { name = "equipment" };
            Element.style.flexDirection = FlexDirection.Row;
            Element.style.flexWrap = Wrap.Wrap;
            Element.style.justifyContent = Justify.Center;
            foreach (var slot in Equipment.Slots)
            {
                Element.Add(CreateSlot(slot));
            }
        }

        public event Action<EquipmentSlot, int>? SlotClicked;

        public event Action<EquipmentSlot?>? SlotHovered;

        public VisualElement Element { get; }

        public int FilledCount { get; private set; }

        public void Refresh(Equipment equipment)
        {
            FilledCount = 0;
            foreach (var slot in Equipment.Slots)
            {
                var item = equipment.Get(slot);
                _icons[slot].sprite = item != null ? _catalog.Icon(item.Definition.IconId) : null;
                if (item != null) FilledCount++;
            }
        }

        private VisualElement CreateSlot(EquipmentSlot slot)
        {
            var element = new VisualElement { name = $"equip-{slot}" };
            element.style.width = SlotSize;
            element.style.height = SlotSize;
            element.style.marginLeft = 3f;
            element.style.marginRight = 3f;
            element.style.marginTop = 3f;
            element.style.marginBottom = 3f;
            element.style.backgroundColor = Ui.SlotColor;
            element.style.alignItems = Align.Center;
            element.style.justifyContent = Justify.Center;
            Ui.Border(element, 1f, Ui.BorderColor);
            _icons[slot] = Ui.Icon(null, SlotSize - 12f);
            element.Add(_icons[slot]);
            var label = Ui.Text(slot.ToString(), 9);
            Ui.Place(label, 3f, 1f, null, null);
            element.Add(label);
            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                SlotClicked?.Invoke(slot, evt.button);
                evt.StopPropagation();
            });
            element.RegisterCallback<PointerEnterEvent>(evt => SlotHovered?.Invoke(slot));
            element.RegisterCallback<PointerLeaveEvent>(evt => SlotHovered?.Invoke(null));
            return element;
        }
    }
}
```

- [ ] **Step 8: Implement the inventory screen**

`Runtime/UI/InventoryScreen.cs`:

```csharp
using System.Collections.Generic;
using System.Text;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Items;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class InventoryScreen
    {
        public const float CellSize = 36f;

        private readonly MortarGame _game;
        private readonly SpriteCatalog _catalog;
        private readonly VisualElement _panel;
        private readonly VisualElement _grid;
        private readonly Label _gold;
        private readonly ItemTooltip _tooltip = new ItemTooltip();
        private readonly List<VisualElement> _itemElements = new List<VisualElement>();
        private string _shownKey = "";

        public InventoryScreen(UiRoot ui, MortarGame game, SpriteCatalog catalog)
        {
            _game = game;
            _catalog = catalog;
            Equipment = new EquipmentPanel(catalog);
            Equipment.SlotClicked += OnEquipmentClicked;
            Equipment.SlotHovered += slot => _tooltip.Show(slot.HasValue ? Gear?.Equipment.Get(slot.Value) : null);
            _gold = Ui.Text("", 14);
            _grid = BuildGrid();
            _panel = Ui.Box("inventory");
            Ui.Place(_panel, null, 16f, 16f, null);
            _panel.Add(Equipment.Element);
            _panel.Add(_gold);
            _panel.Add(_grid);
            _panel.Add(_tooltip.Element);
            Ui.Show(_panel, false);
            ui.Screens.Add(_panel);
        }

        public InventoryCursor Cursor { get; } = new InventoryCursor();

        public EquipmentPanel Equipment { get; }

        public bool IsOpen => Ui.IsShown(_panel);

        public int ItemElementCount => _itemElements.Count;

        public string GoldText => _gold.text;

        private Belongings? Gear => _game.Session?.World.Belongings;

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            _shownKey = "";
            Ui.Show(_panel, true);
        }

        public void Close()
        {
            Cursor.Release();
            _tooltip.Show(null);
            Ui.Show(_panel, false);
        }

        public bool DropHeld()
        {
            var command = Cursor.Drop();
            if (command == null)
            {
                return false;
            }
            _game.Enqueue(command);
            return true;
        }

        public void Refresh(Belongings belongings)
        {
            if (!IsOpen)
            {
                return;
            }
            Cursor.Validate(belongings.Inventory);
            var key = SnapshotKey(belongings);
            if (key == _shownKey)
            {
                return;
            }
            _shownKey = key;
            Equipment.Refresh(belongings.Equipment);
            _gold.text = $"Gold: {belongings.Gold}";
            RebuildItems(belongings.Inventory);
        }

        private string SnapshotKey(Belongings belongings)
        {
            var key = new StringBuilder();
            key.Append(belongings.Gold).Append('|').Append(Cursor.HeldItemId);
            foreach (var stored in belongings.Inventory.Items)
            {
                key.Append('|').Append(stored.Item.Id).Append('@').Append(stored.Cell.X).Append(',').Append(stored.Cell.Y);
            }
            foreach (var worn in belongings.Equipment.Worn)
            {
                key.Append("|w").Append(worn.Id);
            }
            return key.ToString();
        }

        private void RebuildItems(Inventory inventory)
        {
            foreach (var element in _itemElements)
            {
                element.RemoveFromHierarchy();
            }
            _itemElements.Clear();
            foreach (var stored in inventory.Items)
            {
                var element = ItemElement(stored);
                _grid.Add(element);
                _itemElements.Add(element);
            }
        }

        private VisualElement ItemElement(StoredItem stored)
        {
            var definition = stored.Item.Definition;
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            Ui.Place(element, stored.Cell.X * CellSize, stored.Cell.Y * CellSize, null, null);
            element.style.width = definition.Width * CellSize;
            element.style.height = definition.Height * CellSize;
            element.style.alignItems = Align.Center;
            element.style.justifyContent = Justify.Center;
            element.style.backgroundColor = new Color(0.22f, 0.19f, 0.15f, 0.95f);
            Ui.Border(element, 1f, stored.Item.Id == Cursor.HeldItemId ? Ui.HeldColor : Ui.BorderColor);
            element.Add(Ui.Icon(_catalog.Icon(definition.IconId), Mathf.Min(definition.Width, definition.Height) * CellSize - 4f));
            return element;
        }

        private VisualElement BuildGrid()
        {
            var grid = new VisualElement { name = "bag" };
            grid.style.width = Inventory.Width * CellSize;
            grid.style.height = Inventory.Height * CellSize;
            grid.style.marginTop = 8f;
            grid.style.backgroundColor = Ui.SlotColor;
            Ui.Border(grid, 1f, Ui.BorderColor);
            grid.RegisterCallback<PointerDownEvent>(OnGridClicked);
            grid.RegisterCallback<PointerMoveEvent>(evt => _tooltip.Show(ItemAt(evt.localPosition)));
            grid.RegisterCallback<PointerLeaveEvent>(evt => _tooltip.Show(null));
            return grid;
        }

        private void OnGridClicked(PointerDownEvent evt)
        {
            evt.StopPropagation();
            var gear = Gear;
            if (gear == null)
            {
                return;
            }
            if (evt.button == 1)
            {
                var item = ItemAt(evt.localPosition);
                Send(item != null ? InventoryCursor.QuickUse(item, gear) : null);
                return;
            }
            Send(Cursor.ClickBag(CellAt(evt.localPosition), gear.Inventory));
        }

        private void OnEquipmentClicked(EquipmentSlot slot, int button)
        {
            var gear = Gear;
            if (gear == null)
            {
                return;
            }
            if (button == 1)
            {
                Send(gear.Equipment.Get(slot) != null ? new UnequipCommand(slot) : null);
                return;
            }
            Send(Cursor.ClickEquipment(slot, gear.Equipment));
        }

        private void Send(Command? command)
        {
            if (command != null)
            {
                _game.Enqueue(command);
            }
        }

        private Item? ItemAt(Vector3 local)
        {
            var gear = Gear;
            if (gear == null)
            {
                return null;
            }
            var id = gear.Inventory.ItemAt(CellAt(local));
            return id == 0 ? null : gear.Inventory.Find(id)?.Item;
        }

        private static Cell CellAt(Vector3 local) =>
            new Cell(Mathf.FloorToInt(local.x / CellSize), Mathf.FloorToInt(local.y / CellSize));
    }
}
```

Item elements use `PickingMode.Ignore`, so pointer events always target the grid and `localPosition` is grid-relative.

- [ ] **Step 9: Run tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: `[PlayMode] total 27, passed 27, failed 0`.

- [ ] **Step 10: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/Feedback unity/com.mortar.unity/Runtime/UI unity/com.mortar.unity/Tests/Editor unity/com.mortar.unity/Tests/Runtime
git commit -m "add inventory, equipment and tooltips"
```

---

### Task 11: Character sheet, menus, app, and sample scene

`MortarApp` is the composition root: it builds every view, wires input and UI together, and owns the menu flow. `SessionPresenter` turns ticks into view updates. After this task the sample project is playable with placeholder sprites.

**Files:**
- Create: `unity/com.mortar.unity/Runtime/UI/CharacterSheet.cs`, `MenuScreen.cs`, `Menus.cs`, `MortarTheme.tss`
- Create: `unity/com.mortar.unity/Runtime/App/MortarApp.cs`, `Runtime/App/SessionPresenter.cs`
- Create: `unity/com.mortar.unity/Editor/SampleSceneBuilder.cs`
- Create: `unity/Mortar.Sample/Assets/link.xml`, `docs/unity.md`
- Generated by the builder and committed: `unity/Mortar.Sample/Assets/Scenes/Game.unity`, `unity/Mortar.Sample/Assets/Settings/MortarPanelSettings.asset`, `unity/Mortar.Sample/ProjectSettings/EditorBuildSettings.asset`
- Test: `unity/com.mortar.unity/Tests/Runtime/MortarAppTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 2–10.
- Produces:
  - `CharacterSheet(UiRoot, MortarGame)` with `bool IsOpen`, `bool CanSpend`, `string HeaderText`, `string StatText(StatKind)`, `void Toggle()`, `void Open()`, `void Close()`, `void Refresh(World)`.
  - `MenuScreen(string title)` with `VisualElement Element`, `string Message`, `Button AddButton(string, Action)`, `void SetMessage(string?)`.
  - `Menus(UiRoot, MortarApp)` with `bool IsMainOpen`, `bool IsPauseOpen`, `bool IsDeathOpen`, `bool AnyOpen`, `string MainMessage`, `string PauseMessage`, `void ShowMain(string? message, bool canContinue)`, `void ShowPause(string? message)`, `void ShowDeath()`, `void HideAll()`.
  - `SessionPresenter(MortarApp, SpriteCatalog)` with `MessageLog Messages`, `FloatingTexts Popups`, `event Action PlayerDied`, `void Reset()`.
  - `MortarApp : MonoBehaviour` with serialized `_catalog`, `_panelSettings`, `_classId = "warrior"`; `void Configure(SpriteCatalog, PanelSettings, string? saveFolder)`; properties `Game`, `Content`, `LoadError`, `CanContinue`, `Level`, `Actors`, `Items`, `CameraRig`, `Controls`, `Ui`, `Hud`, `Inventory`, `Character`, `Menus`, `Presenter`; methods `StartNewGame()`, `ContinueGame()`, `SaveGame()`, `QuitToMenu()`, `TogglePause()`, `Quit()`, `RefreshFrame()`.
  - `SampleSceneBuilder.Build()` (menu item `Mortar/Build Sample Scene`, also run through `run-method.ps1`).

- [ ] **Step 1: Write the failing tests**

`Tests/Runtime/MortarAppTests.cs`:

```csharp
using Mortar.Game.Content;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class MortarAppTests
    {
        private GameObject _host = null!;
        private SpriteCatalog _catalog = null!;
        private MortarApp _app = null!;
        private string _folder = "";

        [SetUp]
        public void SetUp()
        {
            _folder = TestPaths.FreshSaveFolder();
            _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
            _host = new GameObject("app-test");
            _host.SetActive(false);
            _app = _host.AddComponent<MortarApp>();
            _app.Configure(_catalog, UiRoot.CreatePanelSettings(null), _folder);
            _host.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_host);
            Object.Destroy(_catalog);
            TestPaths.Delete(_folder);
        }

        private World World => _app.Game.Session!.World;

        [Test]
        public void StartsOnTheMainMenu()
        {
            Assert.IsNull(_app.LoadError);
            Assert.IsTrue(_app.Menus.IsMainOpen);
            Assert.IsFalse(_app.CanContinue);
        }

        [Test]
        public void NewGameShowsTheTownAndHud()
        {
            _app.StartNewGame();
            _app.RefreshFrame();

            Assert.IsFalse(_app.Menus.AnyOpen);
            Assert.AreEqual(0, World.Depth);
            Assert.Greater(_app.Level.TileCount, 0);
            Assert.IsNotNull(_app.Actors.Find(Actor.PlayerId));
            Assert.AreEqual($"{World.Player.Life}/{World.Player.Profile.MaxLife}", _app.Hud.Life.Text);
        }

        [Test]
        public void SaveThenContinueRestoresTheSameWorld()
        {
            _app.StartNewGame();
            _app.TogglePause();
            _app.SaveGame();
            var hash = WorldHash.Compute(World);

            Assert.AreEqual("Game saved", _app.Menus.PauseMessage);
            _app.QuitToMenu();
            Assert.IsTrue(_app.CanContinue);
            _app.ContinueGame();

            Assert.AreEqual(hash, WorldHash.Compute(World));
            Assert.IsFalse(_app.Game.Paused);
        }

        [Test]
        public void DescendingRebuildsTheLevelAroundThePlayer()
        {
            _app.StartNewGame();

            Journeys.TakeStairsDown(_app.Game);
            _app.Actors.RenderNow();

            Assert.AreEqual(1, World.Depth);
            Assert.IsTrue(_app.CanContinue);
            var view = _app.Actors.Find(Actor.PlayerId)!;
            var center = _app.Level.Space.Center(World.Player.Cell);
            Assert.Less(Vector2.Distance(view.transform.position, center), 0.01f);
        }

        [Test]
        public void TogglePauseShowsAndHidesThePauseMenu()
        {
            _app.StartNewGame();

            _app.TogglePause();
            Assert.IsTrue(_app.Game.Paused);
            Assert.IsTrue(_app.Menus.IsPauseOpen);

            _app.TogglePause();
            Assert.IsFalse(_app.Game.Paused);
            Assert.IsFalse(_app.Menus.AnyOpen);
        }

        [Test]
        public void CharacterSheetShowsBaseStats()
        {
            _app.StartNewGame();
            _app.Character.Open();

            _app.Character.Refresh(World);

            Assert.AreEqual($"Strength: {World.Progress.BaseStats.Strength}", _app.Character.StatText(StatKind.Strength));
            Assert.IsFalse(_app.Character.CanSpend);
        }
    }
}
```

These are plain `[Test]` methods, so no frames pass and `MortarGame.Update` never ticks on its own; every tick comes from `Journeys`.

- [ ] **Step 2: Run tests to verify they fail**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: compile error naming `MortarApp`.

- [ ] **Step 3: Implement the character sheet**

`Runtime/UI/CharacterSheet.cs`:

```csharp
using System.Collections.Generic;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Simulation;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class CharacterSheet
    {
        private readonly MortarGame _game;
        private readonly VisualElement _panel;
        private readonly Label _header = Ui.Text("", 18);
        private readonly Label _experience = Ui.Text("");
        private readonly Label _points = Ui.Text("");
        private readonly Label _vitals = Ui.Text("");
        private readonly Label _combat = Ui.Text("");
        private readonly Dictionary<StatKind, Label> _stats = new Dictionary<StatKind, Label>();
        private readonly Dictionary<StatKind, Button> _plus = new Dictionary<StatKind, Button>();

        public CharacterSheet(UiRoot ui, MortarGame game)
        {
            _game = game;
            _panel = Ui.Box("character");
            Ui.Place(_panel, 16f, 16f, null, null);
            _panel.style.width = 300f;
            _panel.Add(_header);
            _panel.Add(_experience);
            _panel.Add(_points);
            foreach (var kind in Stats.Kinds)
            {
                _panel.Add(StatRow(kind));
            }
            _panel.Add(_vitals);
            _panel.Add(_combat);
            Ui.Show(_panel, false);
            ui.Screens.Add(_panel);
        }

        public bool IsOpen => Ui.IsShown(_panel);

        public bool CanSpend => Ui.IsShown(_plus[StatKind.Strength]);

        public string HeaderText => _header.text;

        public string StatText(StatKind kind) => _stats[kind].text;

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open() => Ui.Show(_panel, true);

        public void Close() => Ui.Show(_panel, false);

        public void Refresh(World world)
        {
            if (!IsOpen)
            {
                return;
            }
            var progress = world.Progress;
            var profile = world.Player.Profile;
            _header.text = $"{world.PlayerClass.Name}  -  Level {progress.Level}";
            var next = ExperienceBar.NextThreshold(progress, world.Content.Rules);
            _experience.text = next.HasValue ? $"Experience: {progress.Experience} / {next.Value}" : $"Experience: {progress.Experience} (max level)";
            _points.text = $"Unspent points: {progress.UnspentPoints}";
            Ui.Show(_points, progress.UnspentPoints > 0);
            foreach (var kind in Stats.Kinds)
            {
                _stats[kind].text = $"{kind}: {progress.BaseStats.Get(kind)}";
                Ui.Show(_plus[kind], progress.UnspentPoints > 0);
            }
            _vitals.text = $"Life: {world.Player.Life}/{profile.MaxLife}\nMana: {world.Player.Mana}/{profile.MaxMana}";
            _combat.text = $"Damage: {profile.Damage.Min}-{profile.Damage.Max}\nArmor class: {profile.ArmorClass}\nTo hit: {profile.ToHit}\nBlock: {profile.BlockChance}%";
        }

        private VisualElement StatRow(StatKind kind)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.minHeight = 44f;
            _stats[kind] = Ui.Text("", 15);
            row.Add(_stats[kind]);
            var plus = new Button(() => _game.Enqueue(new SpendStatPointCommand(kind))) { text = "+" };
            plus.style.minWidth = 44f;
            plus.style.minHeight = 44f;
            Ui.Show(plus, false);
            _plus[kind] = plus;
            row.Add(plus);
            return row;
        }
    }
}
```

- [ ] **Step 4: Implement the menus**

`Runtime/UI/MenuScreen.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class MenuScreen
    {
        private readonly VisualElement _box;
        private readonly Label _message;

        public MenuScreen(string title)
        {
            Element = new VisualElement { name = $"menu-{title.ToLowerInvariant()}" };
            Ui.Fill(Element);
            Element.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
            Element.style.alignItems = Align.Center;
            Element.style.justifyContent = Justify.Center;
            _box = Ui.Box("menu-box");
            _box.style.alignItems = Align.Center;
            Ui.Padding(_box, 24f);
            _box.Add(Ui.Text(title, 28));
            _message = Ui.Text("", 14);
            _message.style.marginTop = 8f;
            _message.style.marginBottom = 8f;
            _message.style.whiteSpace = WhiteSpace.Normal;
            _message.style.maxWidth = 420f;
            _box.Add(_message);
            Element.Add(_box);
            SetMessage(null);
        }

        public VisualElement Element { get; }

        public string Message => _message.text;

        public Button AddButton(string text, Action onClick)
        {
            var button = Ui.MenuButton(text, onClick);
            _box.Add(button);
            return button;
        }

        public void SetMessage(string? message)
        {
            _message.text = message ?? "";
            Ui.Show(_message, message != null);
        }
    }
}
```

`Runtime/UI/Menus.cs`:

```csharp
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class Menus
    {
        private readonly MenuScreen _main = new MenuScreen("Mortar");
        private readonly MenuScreen _pause = new MenuScreen("Paused");
        private readonly MenuScreen _death = new MenuScreen("You have died");
        private readonly Button _continue;

        public Menus(UiRoot ui, MortarApp app)
        {
            _main.AddButton("New Game", app.StartNewGame);
            _continue = _main.AddButton("Continue", app.ContinueGame);
            _main.AddButton("Quit", app.Quit);
            _pause.AddButton("Resume", app.TogglePause);
            _pause.AddButton("Save", app.SaveGame);
            _pause.AddButton("Load", app.ContinueGame);
            _pause.AddButton("Quit to Menu", app.QuitToMenu);
            _death.AddButton("Load Last Save", app.ContinueGame);
            _death.AddButton("New Game", app.StartNewGame);
            ui.Modal.Add(_main.Element);
            ui.Modal.Add(_pause.Element);
            ui.Modal.Add(_death.Element);
            HideAll();
        }

        public bool IsMainOpen => Ui.IsShown(_main.Element);

        public bool IsPauseOpen => Ui.IsShown(_pause.Element);

        public bool IsDeathOpen => Ui.IsShown(_death.Element);

        public bool AnyOpen => IsMainOpen || IsPauseOpen || IsDeathOpen;

        public string MainMessage => _main.Message;

        public string PauseMessage => _pause.Message;

        public void ShowMain(string? message, bool canContinue)
        {
            HideAll();
            _continue.SetEnabled(canContinue);
            _main.SetMessage(message);
            Ui.Show(_main.Element, true);
        }

        public void ShowPause(string? message)
        {
            HideAll();
            _pause.SetMessage(message);
            Ui.Show(_pause.Element, true);
        }

        public void ShowDeath()
        {
            HideAll();
            Ui.Show(_death.Element, true);
        }

        public void HideAll()
        {
            Ui.Show(_main.Element, false);
            Ui.Show(_pause.Element, false);
            Ui.Show(_death.Element, false);
        }
    }
}
```

`Runtime/UI/MortarTheme.tss`:

```css
@import url("unity-theme://default");
```

- [ ] **Step 5: Implement the session presenter**

`Runtime/App/SessionPresenter.cs`:

```csharp
using System;
using System.Collections.Generic;
using Mortar.Game.Events;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class SessionPresenter
    {
        private static readonly Color PlayerHurtColor = new Color(1f, 0.3f, 0.25f);
        private static readonly Color MissColor = new Color(0.7f, 0.7f, 0.7f);

        private readonly MortarApp _app;
        private readonly SpriteCatalog _catalog;
        private bool _deathShown;

        public SessionPresenter(MortarApp app, SpriteCatalog catalog)
        {
            _app = app;
            _catalog = catalog;
            Popups = new FloatingTexts(app.Ui.Overlay);
            app.Game.SessionStarted += OnSessionStarted;
            app.Game.Ticked += OnTicked;
            app.Game.Notice += text => Messages.Add(text, Time.time);
        }

        public event Action? PlayerDied;

        public MessageLog Messages { get; } = new MessageLog();

        public FloatingTexts Popups { get; }

        public void Reset()
        {
            _deathShown = false;
            Messages.Clear();
            Popups.Clear();
            _app.Level.Clear();
            _app.Actors.Clear();
            _app.Items.Clear();
        }

        private void OnSessionStarted(GameSession session)
        {
            Reset();
            ShowLevel(session);
        }

        private void OnTicked(IReadOnlyList<GameEvent> events)
        {
            var session = _app.Game.Session;
            if (session == null)
            {
                return;
            }
            foreach (var gameEvent in events)
            {
                if (gameEvent is LevelChanged)
                {
                    ShowLevel(session);
                }
                Route(gameEvent);
            }
            SyncViews(session);
            if (session.World.IsGameOver && !_deathShown)
            {
                _deathShown = true;
                PlayerDied?.Invoke();
            }
        }

        private void ShowLevel(GameSession session)
        {
            var world = session.World;
            Popups.Clear();
            _app.Actors.Clear();
            _app.Items.Clear();
            _app.Level.Show(world.Level, LevelView.TilesetFor(session.Content, world.Level), world.Depth > world.Content.ShallowestLevel);
            SyncViews(session);
            _app.Actors.RenderNow();
            _app.CameraRig.Snap();
        }

        private void SyncViews(GameSession session)
        {
            _app.Actors.Sync(session.World, _app.Level.Space, _catalog);
            _app.Items.Sync(session.World.Level, _app.Level.Space, _catalog);
        }

        private void Route(GameEvent gameEvent)
        {
            var text = EventText.Describe(gameEvent);
            if (text != null)
            {
                Messages.Add(text, Time.time);
            }
            var popup = CombatFeedback.For(gameEvent);
            if (!popup.HasValue)
            {
                return;
            }
            var view = _app.Actors.Find(popup.Value.ActorId);
            if (view == null)
            {
                return;
            }
            Popups.Spawn(popup.Value.Text, view.transform.position, ColorFor(popup.Value), Time.time);
            if (popup.Value.IsHit)
            {
                _app.Actors.Flash(popup.Value.ActorId);
            }
        }

        private static Color ColorFor(Popup popup)
        {
            if (!popup.IsHit) return MissColor;
            return popup.HurtsPlayer ? PlayerHurtColor : Color.white;
        }
    }
}
```

- [ ] **Step 6: Implement the app**

`Runtime/App/MortarApp.cs`:

```csharp
using System;
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class MortarApp : MonoBehaviour
    {
        [SerializeField] private SpriteCatalog? _catalog;
        [SerializeField] private PanelSettings? _panelSettings;
        [SerializeField] private string _classId = "warrior";
        private string? _saveFolder;

        public MortarGame Game { get; private set; } = null!;

        public GameContent? Content { get; private set; }

        public string? LoadError { get; private set; }

        public bool CanContinue => Content != null && Game.Slot != null && Game.Slot.Exists;

        public LevelView Level { get; private set; } = null!;

        public ActorViews Actors { get; private set; } = null!;

        public GroundItemViews Items { get; private set; } = null!;

        public CameraFollow CameraRig { get; private set; } = null!;

        public PlayerControls Controls { get; private set; } = null!;

        public UiRoot Ui { get; private set; } = null!;

        public HudView Hud { get; private set; } = null!;

        public InventoryScreen Inventory { get; private set; } = null!;

        public CharacterSheet Character { get; private set; } = null!;

        public Menus Menus { get; private set; } = null!;

        public SessionPresenter Presenter { get; private set; } = null!;

        public void Configure(SpriteCatalog catalog, PanelSettings panelSettings, string? saveFolder)
        {
            _catalog = catalog;
            _panelSettings = panelSettings;
            _saveFolder = saveFolder;
        }

        public void StartNewGame()
        {
            if (Content == null)
            {
                Menus.ShowMain(LoadError, false);
                return;
            }
            var session = GameSession.NewGame(Content, _classId, unchecked((uint)Environment.TickCount), out var error);
            BeginOrReport(session, error);
        }

        public void ContinueGame()
        {
            if (Content == null || Game.Slot == null || !CanContinue)
            {
                Menus.ShowMain(LoadError ?? "No saved game", CanContinue);
                return;
            }
            var session = GameSession.Load(Content, Game.Slot, out var error);
            BeginOrReport(session, error);
        }

        public void SaveGame() => Menus.ShowPause(Game.Save() ?? "Game saved");

        public void QuitToMenu()
        {
            Game.End();
            CloseScreens();
            Presenter.Reset();
            Hud.Show(false);
            Menus.ShowMain(LoadError, CanContinue);
        }

        public void TogglePause()
        {
            if (Game.Session == null || Game.Session.World.IsGameOver)
            {
                return;
            }
            Game.Paused = !Game.Paused;
            if (Game.Paused)
            {
                Menus.ShowPause(null);
            }
            else
            {
                Menus.HideAll();
            }
        }

        public void Quit() => Application.Quit();

        public void RefreshFrame()
        {
            var session = Game.Session;
            if (session == null)
            {
                return;
            }
            var world = session.World;
            var hovered = Controls.CellUnderPointer();
            var hover = hovered.HasValue ? HoverText.Describe(world, hovered.Value) : null;
            Hud.Refresh(world, Presenter.Messages.Visible(Time.time), hover);
            Inventory.Refresh(world.Belongings);
            Character.Refresh(world);
            Presenter.Popups.Update(CameraRig.Camera, Time.time);
        }

        private void Awake()
        {
            var catalog = _catalog != null ? _catalog : ScriptableObject.CreateInstance<SpriteCatalog>();
            var settings = _panelSettings != null ? _panelSettings : UiRoot.CreatePanelSettings(null);
            Game = gameObject.AddComponent<MortarGame>();
            Game.Slot = _saveFolder != null ? new SaveSlot(_saveFolder) : SaveSlot.InPersistentData();
            CameraRig = CameraFollow.Create(transform);
            Level = LevelView.Create(transform, catalog);
            Actors = ActorViews.Create(transform, Game);
            Items = GroundItemViews.Create(transform);
            Ui = UiRoot.Create(transform, settings);
            Hud = new HudView(Ui, catalog);
            Inventory = new InventoryScreen(Ui, Game, catalog);
            Character = new CharacterSheet(Ui, Game);
            Controls = PlayerControls.Create(transform, Game, () => Level.Space, CameraRig.Camera);
            Presenter = new SessionPresenter(this, catalog);
            Menus = new Menus(Ui, this);
            Wire();
            Hud.Show(false);
            Content = ContentLoading.Load(ContentPaths.Manifest, out var error);
            LoadError = error;
            Menus.ShowMain(LoadError, CanContinue);
        }

        private void Wire()
        {
            Controls.InventoryToggled += Inventory.Toggle;
            Controls.CharacterToggled += Character.Toggle;
            Controls.MenuRequested += OnMenuRequested;
            Controls.IsOverUi = Ui.IsPointerOverUi;
            Controls.InterceptWorldClick = Inventory.DropHeld;
            Hud.Belt.SlotClicked += slot => Game.Enqueue(Inventory.Cursor.ClickBelt(slot));
            Presenter.PlayerDied += OnPlayerDied;
            CameraRig.Follow(PlayerPosition);
        }

        private Vector3? PlayerPosition()
        {
            if (Game.Session == null)
            {
                return null;
            }
            var view = Actors.Find(Actor.PlayerId);
            return view != null ? view.transform.position : (Vector3?)null;
        }

        private void BeginOrReport(GameSession? session, string? error)
        {
            if (session == null)
            {
                Menus.ShowMain(error, CanContinue);
                return;
            }
            Menus.HideAll();
            CloseScreens();
            Hud.Show(true);
            Game.Begin(session);
        }

        private void OnMenuRequested()
        {
            if (Inventory.IsOpen)
            {
                Inventory.Close();
            }
            else if (Character.IsOpen)
            {
                Character.Close();
            }
            else
            {
                TogglePause();
            }
        }

        private void OnPlayerDied()
        {
            Game.Paused = true;
            CloseScreens();
            Menus.ShowDeath();
        }

        private void CloseScreens()
        {
            Inventory.Close();
            Character.Close();
        }

        private void Update() => RefreshFrame();
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: `[PlayMode] total 33, passed 33, failed 0`.

Run: `pwsh -File tools/unity/test.ps1 -Platform EditMode`
Expected: `[EditMode] total 81, passed 81, failed 0`.

- [ ] **Step 8: Add the sample scene builder and link file**

`Editor/SampleSceneBuilder.cs`:

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity.EditorTools
{
    public static class SampleSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Game.unity";
        public const string PanelSettingsPath = "Assets/Settings/MortarPanelSettings.asset";
        public const string ThemePath = "Packages/com.mortar.unity/Runtime/UI/MortarTheme.tss";

        [MenuItem("Mortar/Build Sample Scene")]
        public static void Build()
        {
            var catalog = SpriteCatalogBuilder.Rebuild();
            var settings = PanelSettingsAsset();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var app = new GameObject("Mortar").AddComponent<MortarApp>();
            var serialized = new SerializedObject(app);
            serialized.FindProperty("_catalog").objectReferenceValue = catalog;
            serialized.FindProperty("_panelSettings").objectReferenceValue = settings;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SpriteCatalogBuilder.EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[Mortar] Sample scene written to {ScenePath}");
        }

        private static PanelSettings PanelSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (settings == null)
            {
                SpriteCatalogBuilder.EnsureFolder("Assets/Settings");
                settings = UiRoot.CreatePanelSettings(null);
                AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            }
            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            EditorUtility.SetDirty(settings);
            return settings;
        }
    }
}
```

`MortarApp.Awake` does not run in edit mode, so adding the component in the builder only serializes its fields.

`unity/Mortar.Sample/Assets/link.xml`:

```xml
<linker>
  <assembly fullname="Mortar.Core" preserve="all" />
  <assembly fullname="Mortar.Game" preserve="all" />
  <assembly fullname="Mortar.Json" preserve="all" />
  <assembly fullname="System.Text.Json" preserve="all" />
</linker>
```

Run: `pwsh -File tools/unity/run-method.ps1 -Method Mortar.Unity.EditorTools.SampleSceneBuilder.Build`
Expected: exit code 0 and the log line `[Mortar] Sample scene written to Assets/Scenes/Game.unity`; `Assets/Scenes/Game.unity`, `Assets/Settings/MortarPanelSettings.asset`, and `Assets/SpriteCatalog` changes exist on disk.

- [ ] **Step 9: Manual smoke check**

Open `unity/Mortar.Sample` in the editor, open `Assets/Scenes/Game.unity`, and press Play. Confirm: the main menu appears; New Game shows the town with placeholder diamonds and the warrior placeholder; clicking floor walks; `I` and `C` open the inventory and character sheet; Escape pauses; walking onto the cathedral entrance changes level and shows "Game saved". Close the editor before running batch tests again.

- [ ] **Step 10: Start `docs/unity.md`**

```markdown
# Mortar for Unity

The Unity layer lives in two folders:

- `unity/com.mortar.unity`: the UPM package (runtime, editor tools, tests).
- `unity/Mortar.Sample`: a sample project that references the package from disk and contains the playable scene.

## Setup

1. Install the editor version in `unity/Mortar.Sample/ProjectSettings/ProjectVersion.txt` (Unity 6 LTS). The scripts find it through the Unity CLI; set `MORTAR_UNITY` to an editor executable to override.
2. Build and copy the simulation into the package and the content into the sample:

   ```powershell
   pwsh -File tools/unity/sync.ps1
   ```

   This publishes `Mortar.Json` for netstandard2.1 into `com.mortar.unity/Plugins/` and copies `content/` into `Mortar.Sample/Assets/StreamingAssets/content/`. Both folders are gitignored; run the script again after changing simulation code or content.
3. Open `unity/Mortar.Sample` in the editor and open `Assets/Scenes/Game.unity`.

## Tests

Close the editor first; batch runs need exclusive access to the project.

```powershell
pwsh -File tools/unity/test.ps1 -Platform EditMode
pwsh -File tools/unity/test.ps1 -Platform PlayMode
```

Add `-Filter <TestName>` to run a subset. Results are written to `out/unity-tests/`.

## Editor tools

- `Mortar/Rebuild Sprite Catalog` rescans `Assets/Art` into `Assets/Art/SpriteCatalog.asset`.
- `Mortar/Build Sample Scene` regenerates the sample scene, panel settings, and build settings.

Both can run headless:

```powershell
pwsh -File tools/unity/run-method.ps1 -Method Mortar.Unity.EditorTools.SampleSceneBuilder.Build
```

## Controls

| Input | Action |
| --- | --- |
| Left click | Walk, attack, pick up, or use stairs |
| Hold left click | Keep walking toward the pointer |
| 1-8 | Drink from belt slot |
| I | Inventory |
| C | Character sheet |
| Esc | Close a screen, or pause |

In the inventory, left-click an item to hold it and left-click a bag cell, equipment slot, or belt slot to place it. Left-click the world to drop it. Right-click equips, unequips, or belts a potion.
```

- [ ] **Step 11: Commit**

```powershell
git add unity/com.mortar.unity/Runtime/App unity/com.mortar.unity/Runtime/App.meta unity/com.mortar.unity/Runtime/UI unity/com.mortar.unity/Editor unity/com.mortar.unity/Tests/Runtime unity/Mortar.Sample/Assets/link.xml unity/Mortar.Sample/Assets/link.xml.meta unity/Mortar.Sample/Assets/Scenes unity/Mortar.Sample/Assets/Scenes.meta unity/Mortar.Sample/Assets/Settings unity/Mortar.Sample/Assets/Settings.meta unity/Mortar.Sample/Assets/Art unity/Mortar.Sample/Assets/Art.meta unity/Mortar.Sample/ProjectSettings/EditorBuildSettings.asset docs/unity.md
git commit -m "add app, menus, character sheet and sample scene"
```

Before staging, run `git status --short` and confirm nothing under `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Plugins/`, or `StreamingAssets/` is listed.

---

### Task 12: Art tool imaging core

Pure Pillow operations that turn a generated still into a game sprite. No network, fully unit tested.

**Files:**
- Create: `tools/art/requirements.txt`, `tools/art/pytest.ini`, `tools/art/mortar_art/__init__.py`, `tools/art/mortar_art/imaging.py`
- Test: `tools/art/tests/test_imaging.py`

**Interfaces:**
- Produces (`mortar_art.imaging`):
  - `make_seamless(image, blend=48) -> Image` (RGB, same size).
  - `project_diamond(texture, width=64, height=32) -> Image` (RGBA 2:1 diamond).
  - `raised_block(texture, width=64, height=64) -> Image` (RGBA: top diamond plus shaded left and right faces).
  - `estimate_background(image, patch=8) -> tuple[int, ...]` (mean RGB of the four corners).
  - `chroma_key(image, key, tolerance=80.0, softness=0.5) -> Image` (RGBA).
  - `crop_to_content(image) -> Image`; `fit(image, width, height, anchor="center"|"bottom", margin=1) -> Image`; `add_outline(image, color=(20, 14, 10, 255)) -> Image`; `contact_sheet(entries: list[tuple[str, Image]], cell=128, columns=6) -> Image`.

The two affine maps below are inverse maps (output pixel to texture pixel), as `Image.transform` expects. For a square texture of side `W` drawn as a `W`×`W/2` diamond: `u = x + 2y - W/2`, `v = -x + 2y + W/2`. The raised block's left face is `u = 2x`, `v = -x + 2y - W/2`; the right face is `u = 2x - W`, `v = x + 2y - 3W/2`.

- [ ] **Step 1: Create the environment files**

`tools/art/requirements.txt`:

```text
Pillow>=12.0
pytest>=8.4
```

`tools/art/pytest.ini`:

```ini
[pytest]
testpaths = tests
pythonpath = .
```

`tools/art/mortar_art/__init__.py`:

```python
"""Placeholder art generation for the Mortar Unity sample."""
```

Run:

```powershell
py -3 -m venv tools/art/.venv
tools/art/.venv/Scripts/python -m pip install -r tools/art/requirements.txt
```

Expected: Pillow and pytest install without errors. `tools/art/.venv/` is gitignored (Task 1).

- [ ] **Step 2: Write the failing tests**

`tools/art/tests/test_imaging.py`:

```python
from PIL import Image

from mortar_art import imaging

RED = (200, 0, 0, 255)
GREEN = (0, 200, 0, 255)
BLUE = (0, 0, 200, 255)
YELLOW = (200, 200, 0, 255)


def quadrants(size: int = 64) -> Image.Image:
    image = Image.new("RGBA", (size, size))
    half = size // 2
    image.paste(RED, (0, 0, half, half))
    image.paste(GREEN, (half, 0, size, half))
    image.paste(BLUE, (0, half, half, size))
    image.paste(YELLOW, (half, half, size, size))
    return image


def gradient(size: int = 256) -> Image.Image:
    image = Image.new("RGB", (size, size))
    image.putdata([(x, x, x) for _ in range(size) for x in range(size)])
    return image


def test_seamless_edges_wrap():
    result = imaging.make_seamless(gradient())

    assert abs(result.getpixel((0, 128))[0] - result.getpixel((255, 128))[0]) <= 1


def test_seamless_keeps_the_center():
    result = imaging.make_seamless(gradient())

    assert result.getpixel((128, 128)) == (128, 128, 128)


def test_diamond_places_texture_quadrants():
    diamond = imaging.project_diamond(quadrants())

    assert diamond.size == (64, 32)
    assert diamond.getpixel((32, 8))[:3] == RED[:3]
    assert diamond.getpixel((48, 16))[:3] == GREEN[:3]
    assert diamond.getpixel((16, 16))[:3] == BLUE[:3]
    assert diamond.getpixel((32, 24))[:3] == YELLOW[:3]


def test_diamond_corners_are_transparent():
    diamond = imaging.project_diamond(quadrants())

    for corner in [(0, 0), (63, 0), (0, 31), (63, 31)]:
        assert diamond.getpixel(corner)[3] == 0


def test_raised_block_shape():
    block = imaging.raised_block(Image.new("RGB", (64, 64), (120, 120, 120)))

    assert block.size == (64, 64)
    assert block.getpixel((32, 16))[3] == 255
    assert block.getpixel((8, 40))[3] == 255
    assert block.getpixel((56, 40))[3] == 255
    assert block.getpixel((4, 4))[3] == 0
    assert block.getpixel((4, 60))[3] == 0


def test_raised_block_faces_are_shaded():
    block = imaging.raised_block(Image.new("RGB", (64, 64), (120, 120, 120)))

    top = block.getpixel((32, 16))[0]
    left = block.getpixel((8, 40))[0]
    right = block.getpixel((56, 40))[0]
    assert left < right < top


def test_estimate_background_uses_corners():
    image = Image.new("RGB", (64, 64), (250, 0, 250))
    image.paste((10, 10, 10), (16, 16, 48, 48))

    assert imaging.estimate_background(image) == (250, 0, 250)


def test_chroma_key_removes_the_background():
    image = Image.new("RGB", (64, 64), (250, 0, 250))
    image.paste((90, 90, 90), (16, 16, 48, 48))

    keyed = imaging.chroma_key(image, (250, 0, 250))

    assert keyed.getpixel((2, 2))[3] == 0
    assert keyed.getpixel((32, 32))[3] == 255


def test_crop_to_content():
    image = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    image.paste(RED, (10, 20, 30, 50))

    assert imaging.crop_to_content(image).size == (20, 30)


def test_fit_bottom_anchors_with_margin():
    fitted = imaging.fit(Image.new("RGBA", (20, 40), RED), 64, 96, anchor="bottom", margin=4)

    assert fitted.size == (64, 96)
    assert fitted.getpixel((32, 91))[3] == 255
    assert fitted.getpixel((32, 93))[3] == 0
    assert fitted.getpixel((32, 3))[3] == 0


def test_outline_grows_the_silhouette():
    image = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    image.paste(RED, (6, 6, 10, 10))

    outlined = imaging.add_outline(image)

    assert outlined.getpixel((5, 7))[3] == 255
    assert outlined.getpixel((7, 7))[:3] == RED[:3]
    assert outlined.getpixel((2, 2))[3] == 0


def test_contact_sheet_lays_out_a_grid():
    entries = [(f"item-{index}", quadrants()) for index in range(7)]

    sheet = imaging.contact_sheet(entries, cell=64, columns=3)

    assert sheet.size == (3 * 64, 3 * (64 + imaging.LABEL_HEIGHT))
```

- [ ] **Step 3: Run tests to verify they fail**

Run (from `tools/art`): `.venv/Scripts/python -m pytest`
Expected: `ImportError` / `AttributeError` for `mortar_art.imaging`.

- [ ] **Step 4: Implement the imaging module**

`tools/art/mortar_art/imaging.py`:

```python
"""Image operations that turn generated stills into isometric sprites."""

from __future__ import annotations

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter

TRANSPARENT = (0, 0, 0, 0)
LEFT_FACE_BRIGHTNESS = 0.55
RIGHT_FACE_BRIGHTNESS = 0.75
LABEL_HEIGHT = 16


def make_seamless(image: Image.Image, blend: int = 48) -> Image.Image:
    rgb = image.convert("RGB")
    width, height = rgb.size
    shifted = ImageChops.offset(rgb, width // 2, height // 2)
    return Image.composite(rgb, shifted, _edge_mask(width, height, blend))


def project_diamond(texture: Image.Image, width: int = 64, height: int = 32) -> Image.Image:
    if width != height * 2:
        raise ValueError("diamonds must be twice as wide as they are tall")
    square = _square(texture, width)
    half = width / 2
    diamond = _affine(square, (width, height), (1, 2, -half, -1, 2, half))
    mask = Image.new("L", (width, height), 0)
    ImageDraw.Draw(mask).polygon(
        [(half, 0), (width - 1, height / 2), (half, height - 1), (0, height / 2)], fill=255
    )
    diamond.putalpha(ImageChops.multiply(diamond.getchannel("A"), mask))
    return diamond


def raised_block(texture: Image.Image, width: int = 64, height: int = 64) -> Image.Image:
    if width != height:
        raise ValueError("raised blocks must be square")
    square = _square(texture, width)
    left = _affine(square, (width, height), (2, 0, 0, -1, 2, -width / 2))
    right = _affine(square, (width, height), (2, 0, -width, 1, 2, -1.5 * width))
    block = Image.new("RGBA", (width, height), TRANSPARENT)
    block.alpha_composite(_shade(left, LEFT_FACE_BRIGHTNESS))
    block.alpha_composite(_shade(right, RIGHT_FACE_BRIGHTNESS))
    block.alpha_composite(project_diamond(square, width, width // 2))
    return block


def estimate_background(image: Image.Image, patch: int = 8) -> tuple[int, ...]:
    rgb = image.convert("RGB")
    width, height = rgb.size
    boxes = [
        (0, 0, patch, patch),
        (width - patch, 0, width, patch),
        (0, height - patch, patch, height),
        (width - patch, height - patch, width, height),
    ]
    pixels = [pixel for box in boxes for pixel in rgb.crop(box).getdata()]
    return tuple(sum(pixel[channel] for pixel in pixels) // len(pixels) for channel in range(3))


def chroma_key(
    image: Image.Image, key: tuple[int, ...], tolerance: float = 80.0, softness: float = 0.5
) -> Image.Image:
    rgba = image.convert("RGBA")
    solid = tolerance * (1 + softness)
    alphas = []
    for red, green, blue, alpha in rgba.getdata():
        distance = ((red - key[0]) ** 2 + (green - key[1]) ** 2 + (blue - key[2]) ** 2) ** 0.5
        if distance <= tolerance:
            alphas.append(0)
        elif distance >= solid:
            alphas.append(alpha)
        else:
            alphas.append(int(alpha * (distance - tolerance) / (solid - tolerance)))
    mask = Image.new("L", rgba.size)
    mask.putdata(alphas)
    rgba.putalpha(mask)
    return rgba


def crop_to_content(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    box = rgba.getchannel("A").getbbox()
    return rgba.crop(box) if box else rgba


def fit(
    image: Image.Image, width: int, height: int, anchor: str = "center", margin: int = 1
) -> Image.Image:
    if anchor not in ("center", "bottom"):
        raise ValueError(f"unknown anchor '{anchor}'")
    rgba = image.convert("RGBA")
    scale = min((width - 2 * margin) / rgba.width, (height - 2 * margin) / rgba.height)
    size = (max(1, round(rgba.width * scale)), max(1, round(rgba.height * scale)))
    resized = rgba.resize(size, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (width, height), TRANSPARENT)
    x = (width - size[0]) // 2
    y = height - margin - size[1] if anchor == "bottom" else (height - size[1]) // 2
    canvas.alpha_composite(resized, (x, y))
    return canvas


def add_outline(image: Image.Image, color: tuple[int, int, int, int] = (20, 14, 10, 255)) -> Image.Image:
    rgba = image.convert("RGBA")
    solid = rgba.getchannel("A").point(lambda alpha: 255 if alpha > 32 else 0)
    outline = Image.new("RGBA", rgba.size, color)
    outline.putalpha(solid.filter(ImageFilter.MaxFilter(3)))
    outline.alpha_composite(rgba)
    return outline


def contact_sheet(
    entries: list[tuple[str, Image.Image]], cell: int = 128, columns: int = 6
) -> Image.Image:
    rows = max(1, -(-len(entries) // columns))
    sheet = Image.new("RGB", (columns * cell, rows * (cell + LABEL_HEIGHT)), (40, 40, 40))
    draw = ImageDraw.Draw(sheet)
    for index, (label, image) in enumerate(entries):
        x = (index % columns) * cell
        y = (index // columns) * (cell + LABEL_HEIGHT)
        thumbnail = fit(image, cell, cell, margin=4)
        sheet.paste(thumbnail, (x, y), thumbnail)
        draw.text((x + 4, y + cell + 2), label[-24:], fill=(220, 220, 220))
    return sheet


def _square(texture: Image.Image, size: int) -> Image.Image:
    return texture.convert("RGBA").resize((size, size), Image.Resampling.LANCZOS)


def _affine(image: Image.Image, size: tuple[int, int], data: tuple[float, ...]) -> Image.Image:
    return image.transform(
        size, Image.Transform.AFFINE, data, resample=Image.Resampling.BILINEAR, fillcolor=TRANSPARENT
    )


def _shade(face: Image.Image, brightness: float) -> Image.Image:
    shaded = ImageEnhance.Brightness(face.convert("RGB")).enhance(brightness)
    shaded.putalpha(face.getchannel("A"))
    return shaded


def _edge_mask(width: int, height: int, blend: int) -> Image.Image:
    mask = Image.new("L", (width, height))
    mask.putdata(
        [
            min(255, 255 * min(x, y, width - 1 - x, height - 1 - y) // blend)
            for y in range(height)
            for x in range(width)
        ]
    )
    return mask
```

`_shade` darkens RGB only; brightening an RGBA image directly would also scale its alpha.

- [ ] **Step 5: Run tests to verify they pass**

Run (from `tools/art`): `.venv/Scripts/python -m pytest`
Expected: `12 passed`.

If a quadrant or face assertion fails by a single pixel because of bilinear sampling at a boundary, move the sampled point one pixel further inside the region; do not change the affine coefficients, which are derived above.

- [ ] **Step 6: Commit**

```powershell
git add tools/art/requirements.txt tools/art/pytest.ini tools/art/mortar_art/__init__.py tools/art/mortar_art/imaging.py tools/art/tests/test_imaging.py
git commit -m "add art tool imaging core"
```

---

### Task 13: Art generation with Workers AI

Credentials, the Workers AI client, the job list, sprite processing, and the CLI, followed by a controller-only generation and approval pass.

**Files:**
- Create: `tools/art/mortar_art/config.py`, `workers_ai.py`, `jobs.py`, `process.py`, `tools/art/generate.py`, `tools/art/prompts.json`, `.env.example`
- Create: `unity/com.mortar.unity/Tests/Runtime/ArtPreviewCapture.cs`
- Test: `tools/art/tests/test_config.py`, `test_workers_ai.py`, `test_jobs.py`, `test_process.py`
- Modify: `docs/unity.md` (art tool section)

**Interfaces:**
- Consumes: `mortar_art.imaging` (Task 12); `MortarApp`, `Journeys`, `TestPaths` (Tasks 4 and 11); `SpriteCatalogBuilder` (Task 3).
- Produces:
  - `config.REPO_ROOT`, `config.REQUIRED_KEYS`, `ConfigError`, `Credentials(account_id, api_token)` (both fields hidden from `repr`), `parse_env_file(text) -> dict[str, str]`, `load_credentials(environ, env_file) -> Credentials`.
  - `workers_ai.API_BASE`, `MAX_STEPS = 8`, `ArtError`, `build_request(credentials, model, prompt, seed, steps) -> Request`, `generate_image(credentials, model, prompt, seed, steps=8, timeout=120.0, opener=urlopen) -> bytes`.
  - `jobs.ART_ROOT`, `RAW_ROOT`, `PROMPTS_PATH`, `KINDS = ("tiles", "actors", "icons")`, `REROLL_STEP = 1000`, `JobError`, frozen `Job(kind, entry, key, prompt, seed, raised=False)` with `output` and `raw` paths, `load_spec(path)`, `save_spec(spec, path)`, `build_jobs(spec, kind="all")`, `select(jobs, only)`, `reroll(spec, only) -> int`.
  - `process.tile(raw, raised)`, `process.actor(raw)`, `process.icon(raw)`, `process.for_job(job, raw)`.
  - `generate.py --kind all|tiles|actors|icons [--only <entry-or-key>] [--reroll] [--force] [--reprocess] [--sheet]`.

Job keys equal sprite catalog keys (`tiles/<theme>/<ground>-<variant>`, `actors/<id>`, `icons/<id>`), and outputs are written to `unity/Mortar.Sample/Assets/Art/<key>.png`, so `SpriteCatalogBuilder` picks them up with no mapping.

- [ ] **Step 1: Write the failing tests**

`tools/art/tests/test_config.py`:

```python
import pytest

from mortar_art.config import ConfigError, Credentials, load_credentials, parse_env_file


def test_parse_env_file_handles_comments_and_quotes():
    text = '# comment\n\nCF_API_TOKEN="abc"\nCF_ACCOUNT_ID = xyz \nOTHER=1\n'

    assert parse_env_file(text) == {"CF_API_TOKEN": "abc", "CF_ACCOUNT_ID": "xyz", "OTHER": "1"}


def test_environment_overrides_the_file(tmp_path):
    env_file = tmp_path / ".env"
    env_file.write_text("CF_API_TOKEN=file-token\nCF_ACCOUNT_ID=file-account\n", encoding="utf-8")

    credentials = load_credentials({"CF_API_TOKEN": "env-token"}, env_file)

    assert credentials.api_token == "env-token"
    assert credentials.account_id == "file-account"


def test_missing_keys_are_named_without_values(tmp_path):
    env_file = tmp_path / ".env"
    env_file.write_text("CF_API_TOKEN=abc123\n", encoding="utf-8")

    with pytest.raises(ConfigError) as raised:
        load_credentials({}, env_file)

    assert "CF_ACCOUNT_ID" in str(raised.value)
    assert "abc123" not in str(raised.value)


def test_repr_hides_values():
    credentials = Credentials(account_id="acct-123", api_token="secret-token")

    assert "acct-123" not in repr(credentials)
    assert "secret-token" not in repr(credentials)
```

`tools/art/tests/test_workers_ai.py`:

```python
import base64
import io
import json
import urllib.error

import pytest

from mortar_art.config import Credentials
from mortar_art.workers_ai import ArtError, build_request, generate_image

CREDENTIALS = Credentials(account_id="acct-123", api_token="secret-token")
MODEL = "@cf/black-forest-labs/flux-1-schnell"


def opener_returning(payload, captured=None):
    def opener(request, timeout):
        if captured is not None:
            captured.append(request)
        return io.BytesIO(json.dumps(payload).encode("utf-8"))

    return opener


def test_request_targets_the_model_with_a_bearer_token():
    request = build_request(CREDENTIALS, MODEL, "a stone floor", 7, 8)

    assert request.full_url.endswith("/accounts/acct-123/ai/run/" + MODEL)
    assert request.get_header("Authorization") == "Bearer secret-token"
    assert json.loads(request.data) == {"prompt": "a stone floor", "steps": 8, "seed": 7}


def test_decodes_the_returned_image():
    encoded = base64.b64encode(b"jpeg-bytes").decode("ascii")
    captured = []

    data = generate_image(CREDENTIALS, MODEL, "p", 1, opener=opener_returning({"success": True, "result": {"image": encoded}}, captured))

    assert data == b"jpeg-bytes"
    assert len(captured) == 1


def test_http_errors_hide_credentials():
    def failing(request, timeout):
        raise urllib.error.HTTPError(request.full_url, 401, "Unauthorized", None, None)

    with pytest.raises(ArtError) as raised:
        generate_image(CREDENTIALS, MODEL, "p", 1, opener=failing)

    message = str(raised.value)
    assert "401" in message
    assert "secret-token" not in message
    assert "acct-123" not in message
    assert raised.value.__cause__ is None


def test_missing_image_is_an_error():
    with pytest.raises(ArtError):
        generate_image(CREDENTIALS, MODEL, "p", 1, opener=opener_returning({"success": False, "errors": [{"message": "bad prompt"}]}))
```

`tools/art/tests/test_jobs.py`:

```python
import json

import pytest

from mortar_art import jobs
from mortar_art.config import REPO_ROOT

CONTENT = REPO_ROOT / "content"


def spec():
    return {
        "model": "m",
        "steps": 8,
        "style": {"tile": "tile style", "actor": "actor style", "icon": "icon style"},
        "tiles": [{"theme": "town", "ground": "grass", "variants": 2, "seed": 100, "prompt": "grass"}],
        "actors": [{"id": "warrior", "seed": 200, "prompt": "a warrior"}],
        "icons": [{"id": "gold", "seed": 300, "prompt": "coins"}],
    }


def test_tile_entries_expand_into_variants():
    tiles = jobs.build_jobs(spec(), "tiles")

    assert [job.key for job in tiles] == ["tiles/town/grass-0", "tiles/town/grass-1"]
    assert [job.seed for job in tiles] == [100, 101]
    assert tiles[0].prompt == "grass, tile style"
    assert tiles[0].output == jobs.ART_ROOT / "tiles" / "town" / "grass-0.png"


def test_actor_and_icon_keys_match_the_sprite_catalog():
    keys = [job.key for job in jobs.build_jobs(spec())]

    assert keys == ["tiles/town/grass-0", "tiles/town/grass-1", "actors/warrior", "icons/gold"]


def test_unknown_kinds_are_rejected():
    with pytest.raises(jobs.JobError):
        jobs.build_jobs(spec(), "sounds")


def test_duplicate_keys_are_rejected():
    duplicated = spec()
    duplicated["actors"].append({"id": "warrior", "seed": 1, "prompt": "again"})

    with pytest.raises(jobs.JobError):
        jobs.build_jobs(duplicated)


def test_select_matches_an_entry_or_a_key():
    all_jobs = jobs.build_jobs(spec())

    assert len(jobs.select(all_jobs, "tiles/town/grass")) == 2
    assert len(jobs.select(all_jobs, "tiles/town/grass-1")) == 1
    assert jobs.select(all_jobs, None) == all_jobs


def test_reroll_bumps_the_entry_seed():
    data = spec()

    assert jobs.reroll(data, "tiles/town/grass-1") == 1
    assert data["tiles"][0]["seed"] == 100 + jobs.REROLL_STEP
    with pytest.raises(jobs.JobError):
        jobs.reroll(data, "actors/nobody")


def load(path):
    return json.loads(path.read_text(encoding="utf-8"))


def test_prompts_cover_all_content():
    expected = set()
    for tileset in (CONTENT / "tilesets").glob("*.json"):
        data = load(tileset)
        weights = data.get("variantWeights", {})
        for ground in data["groundTypes"]:
            if ground["id"] != "void":
                for variant in range(len(weights.get(ground["id"], [1]))):
                    expected.add(f"tiles/{tileset.stem}/{ground['id']}-{variant}")
    for folder in ("classes", "monsters"):
        expected |= {f"actors/{load(path)['spriteSet']}" for path in (CONTENT / folder).glob("*.json")}
    expected |= {f"actors/{name}" for name in ("stairs-down", "stairs-up", "smith", "healer", "sage")}
    expected |= {f"icons/{load(path)['icon']}" for path in (CONTENT / "items").glob("*.json")}
    expected.add("icons/gold")

    assert {job.key for job in jobs.build_jobs(jobs.load_spec())} == expected


def test_raised_tiles_are_the_sight_blocking_grounds():
    blocking = set()
    for tileset in (CONTENT / "tilesets").glob("*.json"):
        blocking |= {f"tiles/{tileset.stem}/{ground['id']}" for ground in load(tileset)["groundTypes"] if ground["blocksSight"]}

    tiles = jobs.build_jobs(jobs.load_spec(), "tiles")

    assert {job.entry for job in tiles if job.raised} == blocking
```

`tools/art/tests/test_process.py`:

```python
from PIL import Image

from mortar_art import process


def figure_on_magenta():
    image = Image.new("RGB", (512, 512), (250, 0, 250))
    image.paste((90, 80, 70), (200, 100, 312, 450))
    return image


def test_tiles_have_flat_and_raised_sizes():
    texture = Image.new("RGB", (512, 512), (100, 90, 80))

    assert process.tile(texture, raised=False).size == (64, 32)
    assert process.tile(texture, raised=True).size == (64, 64)


def test_actors_are_cut_out_and_stand_on_the_pivot():
    sprite = process.actor(figure_on_magenta())

    assert sprite.size == (64, 96)
    assert sprite.getpixel((0, 0))[3] == 0
    assert sprite.getpixel((32, 90))[3] == 255
    assert all(sprite.getpixel((x, 94))[3] == 0 for x in range(64))


def test_icons_are_square_and_centered():
    sprite = process.icon(figure_on_magenta())

    assert sprite.size == (64, 64)
    assert sprite.getpixel((32, 32))[3] == 255
    assert sprite.getpixel((0, 0))[3] == 0
```

- [ ] **Step 2: Run tests to verify they fail**

Run (from `tools/art`): `.venv/Scripts/python -m pytest`
Expected: import errors for `mortar_art.config`, `workers_ai`, `jobs`, `process`.

- [ ] **Step 3: Implement config and the Workers AI client**

`tools/art/mortar_art/config.py`:

```python
"""Credentials for Cloudflare Workers AI, read without ever echoing their values."""

from __future__ import annotations

from collections.abc import Mapping
from dataclasses import dataclass, field
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
REQUIRED_KEYS = ("CF_API_TOKEN", "CF_ACCOUNT_ID")


class ConfigError(Exception):
    pass


@dataclass(frozen=True)
class Credentials:
    account_id: str = field(repr=False)
    api_token: str = field(repr=False)


def parse_env_file(text: str) -> dict[str, str]:
    values: dict[str, str] = {}
    for line in text.splitlines():
        stripped = line.strip()
        if not stripped or stripped.startswith("#") or "=" not in stripped:
            continue
        key, value = stripped.split("=", 1)
        values[key.strip()] = value.strip().strip('"').strip("'")
    return values


def load_credentials(environ: Mapping[str, str], env_file: Path | None) -> Credentials:
    values: dict[str, str] = {}
    if env_file is not None and env_file.is_file():
        values.update(parse_env_file(env_file.read_text(encoding="utf-8")))
    values.update({key: environ[key] for key in REQUIRED_KEYS if environ.get(key)})
    missing = [key for key in REQUIRED_KEYS if not values.get(key)]
    if missing:
        raise ConfigError(f"Missing {', '.join(missing)}; set them in the environment or in .env")
    return Credentials(account_id=values["CF_ACCOUNT_ID"], api_token=values["CF_API_TOKEN"])
```

`tools/art/mortar_art/workers_ai.py`:

```python
"""Minimal Cloudflare Workers AI client for text-to-image models."""

from __future__ import annotations

import base64
import binascii
import json
import urllib.error
import urllib.request
from collections.abc import Callable
from typing import Any

from .config import Credentials

API_BASE = "https://api.cloudflare.com/client/v4/accounts"
MAX_STEPS = 8
MAX_ERROR_LENGTH = 200


class ArtError(Exception):
    pass


def build_request(credentials: Credentials, model: str, prompt: str, seed: int, steps: int) -> urllib.request.Request:
    if not 1 <= steps <= MAX_STEPS:
        raise ArtError(f"steps must be between 1 and {MAX_STEPS}")
    body = json.dumps({"prompt": prompt, "steps": steps, "seed": seed}).encode("utf-8")
    return urllib.request.Request(
        f"{API_BASE}/{credentials.account_id}/ai/run/{model}",
        data=body,
        method="POST",
        headers={"Authorization": f"Bearer {credentials.api_token}", "Content-Type": "application/json"},
    )


def generate_image(
    credentials: Credentials,
    model: str,
    prompt: str,
    seed: int,
    steps: int = MAX_STEPS,
    timeout: float = 120.0,
    opener: Callable[..., Any] = urllib.request.urlopen,
) -> bytes:
    request = build_request(credentials, model, prompt, seed, steps)
    try:
        with opener(request, timeout=timeout) as response:
            payload = json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as error:
        raise ArtError(f"Workers AI returned HTTP {error.code}") from None
    except (urllib.error.URLError, OSError) as error:
        raise ArtError(f"Workers AI request failed ({type(error).__name__})") from None
    except json.JSONDecodeError:
        raise ArtError("Workers AI returned invalid JSON") from None
    return _decode_image(payload)


def _decode_image(payload: Any) -> bytes:
    result = payload.get("result") if isinstance(payload, dict) else None
    image = result.get("image") if isinstance(result, dict) else None
    if not isinstance(image, str):
        raise ArtError("Workers AI returned no image" + _first_error(payload))
    try:
        return base64.b64decode(image, validate=True)
    except binascii.Error:
        raise ArtError("Workers AI returned an invalid image") from None


def _first_error(payload: Any) -> str:
    errors = payload.get("errors") if isinstance(payload, dict) else None
    if isinstance(errors, list) and errors and isinstance(errors[0], dict):
        return f": {str(errors[0].get('message', ''))[:MAX_ERROR_LENGTH]}"
    return ""
```

Errors are raised `from None` so tracebacks never chain an exception that holds the request URL (which contains the account id).

- [ ] **Step 4: Implement jobs and processing**

`tools/art/mortar_art/jobs.py`:

```python
"""Generation jobs built from prompts.json; keys match the Unity sprite catalog."""

from __future__ import annotations

import json
from collections.abc import Callable
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from .config import REPO_ROOT

ART_ROOT = REPO_ROOT / "unity" / "Mortar.Sample" / "Assets" / "Art"
RAW_ROOT = REPO_ROOT / "tools" / "art" / "out" / "raw"
PROMPTS_PATH = REPO_ROOT / "tools" / "art" / "prompts.json"
KINDS = ("tiles", "actors", "icons")
REROLL_STEP = 1000

Spec = dict[str, Any]


class JobError(Exception):
    pass


@dataclass(frozen=True)
class Job:
    kind: str
    entry: str
    key: str
    prompt: str
    seed: int
    raised: bool = False

    @property
    def output(self) -> Path:
        return ART_ROOT / f"{self.key}.png"

    @property
    def raw(self) -> Path:
        return RAW_ROOT / f"{self.key}.jpg"


def load_spec(path: Path = PROMPTS_PATH) -> Spec:
    return json.loads(path.read_text(encoding="utf-8"))


def save_spec(spec: Spec, path: Path = PROMPTS_PATH) -> None:
    path.write_text(json.dumps(spec, indent=2) + "\n", encoding="utf-8")


def build_jobs(spec: Spec, kind: str = "all") -> list[Job]:
    kinds = KINDS if kind == "all" else (kind,)
    if any(name not in KINDS for name in kinds):
        raise JobError(f"unknown kind '{kind}'")
    built = [job for name in kinds for job in _BUILDERS[name](spec)]
    keys = [job.key for job in built]
    duplicates = sorted({key for key in keys if keys.count(key) > 1})
    if duplicates:
        raise JobError(f"duplicate keys: {', '.join(duplicates)}")
    return built


def select(all_jobs: list[Job], only: str | None) -> list[Job]:
    return all_jobs if only is None else [job for job in all_jobs if only in (job.entry, job.key)]


def reroll(spec: Spec, only: str) -> int:
    matches = {job.entry for job in select(build_jobs(spec), only)}
    if not matches:
        raise JobError(f"nothing matches '{only}'")
    bumped = 0
    for section in KINDS:
        for entry in spec.get(section, []):
            if _entry_name(section, entry) in matches:
                entry["seed"] = int(entry["seed"]) + REROLL_STEP
                bumped += 1
    return bumped


def _tile_jobs(spec: Spec) -> list[Job]:
    style = _style(spec, "tile")
    built = []
    for entry in spec.get("tiles", []):
        name = _entry_name("tiles", entry)
        for variant in range(int(_field(entry, "variants"))):
            built.append(
                Job("tiles", name, f"{name}-{variant}", f"{_field(entry, 'prompt')}, {style}",
                    int(_field(entry, "seed")) + variant, bool(entry.get("raised", False)))
            )
    return built


def _named_jobs(spec: Spec, section: str, style_key: str) -> list[Job]:
    style = _style(spec, style_key)
    return [
        Job(section, _entry_name(section, entry), _entry_name(section, entry),
            f"{_field(entry, 'prompt')}, {style}", int(_field(entry, "seed")))
        for entry in spec.get(section, [])
    ]


def _entry_name(section: str, entry: dict[str, Any]) -> str:
    if section == "tiles":
        return f"tiles/{_field(entry, 'theme')}/{_field(entry, 'ground')}"
    return f"{section}/{_field(entry, 'id')}"


def _field(entry: dict[str, Any], name: str) -> Any:
    if name not in entry:
        raise JobError(f"prompt entry {entry!r} is missing '{name}'")
    return entry[name]


def _style(spec: Spec, key: str) -> str:
    styles = spec.get("style", {})
    if key not in styles:
        raise JobError(f"prompts.json has no '{key}' style")
    return styles[key]


_BUILDERS: dict[str, Callable[[Spec], list[Job]]] = {
    "tiles": _tile_jobs,
    "actors": lambda spec: _named_jobs(spec, "actors", "actor"),
    "icons": lambda spec: _named_jobs(spec, "icons", "icon"),
}
```

`tools/art/mortar_art/process.py`:

```python
"""Turn raw generated stills into sprites sized for the Unity importer."""

from __future__ import annotations

from PIL import Image

from . import imaging
from .jobs import Job

WORK_SIZE = 256
ACTOR_WIDTH = 64
ACTOR_HEIGHT = 96
ACTOR_FOOT_MARGIN = 4
ICON_SIZE = 64
ICON_MARGIN = 2


def tile(raw: Image.Image, raised: bool) -> Image.Image:
    texture = imaging.make_seamless(raw.convert("RGB").resize((WORK_SIZE, WORK_SIZE), Image.Resampling.LANCZOS))
    return imaging.raised_block(texture, 64, 64) if raised else imaging.project_diamond(texture, 64, 32)


def actor(raw: Image.Image) -> Image.Image:
    fitted = imaging.fit(_cutout(raw), ACTOR_WIDTH, ACTOR_HEIGHT, anchor="bottom", margin=ACTOR_FOOT_MARGIN)
    return imaging.add_outline(fitted)


def icon(raw: Image.Image) -> Image.Image:
    return imaging.add_outline(imaging.fit(_cutout(raw), ICON_SIZE, ICON_SIZE, margin=ICON_MARGIN))


def for_job(job: Job, raw: Image.Image) -> Image.Image:
    if job.kind == "tiles":
        return tile(raw, job.raised)
    return actor(raw) if job.kind == "actors" else icon(raw)


def _cutout(raw: Image.Image) -> Image.Image:
    small = raw.convert("RGB").resize((WORK_SIZE, WORK_SIZE), Image.Resampling.LANCZOS)
    keyed = imaging.chroma_key(small, imaging.estimate_background(small))
    return imaging.crop_to_content(keyed)
```

`ACTOR_FOOT_MARGIN` matches the actor pivot `4/96` set by `ArtImport` (Task 3).

- [ ] **Step 5: Write the prompts and the CLI**

`tools/art/prompts.json`:

```json
{
  "model": "@cf/black-forest-labs/flux-1-schnell",
  "steps": 8,
  "style": {
    "tile": "seamless tileable texture viewed straight from above, flat even lighting, no perspective, no shadows, muted dark fantasy colors, hand-painted game texture",
    "actor": "single full-body figure standing and facing the viewer, seen slightly from above, centered with empty space around it, plain flat solid magenta background, muted dark fantasy colors, painted game sprite, no text",
    "icon": "single object centered with empty space around it, plain flat solid magenta background, muted dark fantasy colors, painted inventory icon, no text"
  },
  "tiles": [
    { "theme": "cathedral", "ground": "floor", "variants": 3, "seed": 1101, "prompt": "worn grey stone flagstones of an old chapel floor" },
    { "theme": "cathedral", "ground": "wall", "variants": 3, "seed": 1201, "raised": true, "prompt": "rough dark grey cut stone blocks" },
    { "theme": "town", "ground": "floor", "variants": 2, "seed": 2101, "prompt": "packed dirt and gravel" },
    { "theme": "town", "ground": "wall", "variants": 1, "seed": 2201, "raised": true, "prompt": "weathered timber planks and plaster" },
    { "theme": "town", "ground": "grass", "variants": 3, "seed": 2301, "prompt": "short green and brown meadow grass" },
    { "theme": "town", "ground": "road", "variants": 1, "seed": 2401, "prompt": "worn cobblestone road" },
    { "theme": "town", "ground": "water", "variants": 1, "seed": 2501, "prompt": "dark still pond water" },
    { "theme": "town", "ground": "tree", "variants": 1, "seed": 2601, "raised": true, "prompt": "dense dark green leaves" }
  ],
  "actors": [
    { "id": "warrior", "seed": 3101, "prompt": "human warrior in dented plate and chain armor holding a short blade and a round shield" },
    { "id": "zombie", "seed": 3201, "prompt": "shambling rotting corpse in torn burial rags" },
    { "id": "skeleton", "seed": 3301, "prompt": "skeleton soldier holding a rusty sword" },
    { "id": "imp", "seed": 3401, "prompt": "small horned red-brown cave imp with long claws" },
    { "id": "imp-chieftain", "seed": 3501, "prompt": "large horned cave imp chieftain with a bone club and a feathered headdress" },
    { "id": "carrion-bat", "seed": 3601, "prompt": "large leathery carrion bat with spread wings" },
    { "id": "gravewarden", "seed": 3701, "prompt": "towering undead knight in black armor with a great axe and glowing eyes" },
    { "id": "smith", "seed": 3801, "prompt": "burly village blacksmith with a leather apron and a hammer" },
    { "id": "healer", "seed": 3901, "prompt": "elderly village healer in a green robe holding a bundle of herbs" },
    { "id": "sage", "seed": 4001, "prompt": "old bearded sage in a blue robe with a staff and a book" },
    { "id": "stairs-down", "seed": 4101, "prompt": "small square stone stairwell descending into darkness, seen from above at an angle" },
    { "id": "stairs-up", "seed": 4201, "prompt": "short flight of stone stairs rising upward, seen from above at an angle" }
  ],
  "icons": [
    { "id": "iron-knife", "seed": 5101, "prompt": "plain iron knife" },
    { "id": "hatchet", "seed": 5102, "prompt": "small wood-handled hatchet" },
    { "id": "cudgel", "seed": 5103, "prompt": "knotted wooden cudgel" },
    { "id": "broad-blade", "seed": 5104, "prompt": "broad steel sword" },
    { "id": "padded-vest", "seed": 5105, "prompt": "quilted padded cloth vest" },
    { "id": "studded-jerkin", "seed": 5106, "prompt": "leather jerkin with metal studs" },
    { "id": "ring-hauberk", "seed": 5107, "prompt": "chainmail shirt" },
    { "id": "leather-hood", "seed": 5108, "prompt": "brown leather hood" },
    { "id": "iron-pot-helm", "seed": 5109, "prompt": "round iron pot helmet" },
    { "id": "plank-shield", "seed": 5110, "prompt": "round shield made of wooden planks" },
    { "id": "banded-buckler", "seed": 5111, "prompt": "small round buckler with iron bands" },
    { "id": "copper-band", "seed": 5112, "prompt": "plain copper ring" },
    { "id": "bone-charm", "seed": 5113, "prompt": "carved bone charm on a cord" },
    { "id": "healing-draught", "seed": 5114, "prompt": "small glass flask of red liquid" },
    { "id": "mana-draught", "seed": 5115, "prompt": "small glass flask of blue liquid" },
    { "id": "gold", "seed": 5116, "prompt": "small pile of gold coins" }
  ]
}
```

`tools/art/generate.py`:

```python
"""Generate placeholder art for the Mortar Unity sample with Cloudflare Workers AI."""

from __future__ import annotations

import argparse
import io
import os
import sys
from pathlib import Path

from PIL import Image

from mortar_art import imaging, jobs, process
from mortar_art.config import REPO_ROOT, ConfigError, load_credentials
from mortar_art.workers_ai import ArtError, generate_image

SHEETS_ROOT = REPO_ROOT / "tools" / "art" / "out" / "sheets"


def parse_args(argv: list[str] | None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--kind", choices=("all",) + jobs.KINDS, default="all")
    parser.add_argument("--only", help="an entry or key, e.g. actors/warrior or tiles/town/grass")
    parser.add_argument("--reroll", action="store_true", help="bump the seed of --only and regenerate it")
    parser.add_argument("--force", action="store_true", help="regenerate sprites that already exist")
    parser.add_argument("--reprocess", action="store_true", help="rebuild sprites from saved raw images without calling the API")
    parser.add_argument("--sheet", action="store_true", help="write contact sheets to tools/art/out/sheets")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        spec = jobs.load_spec()
        if args.reroll:
            if not args.only:
                raise jobs.JobError("--reroll needs --only")
            jobs.reroll(spec, args.only)
            jobs.save_spec(spec)
        selected = jobs.select(jobs.build_jobs(spec, args.kind), args.only)
        if not selected:
            raise jobs.JobError("no jobs match")
        if args.reprocess:
            reprocess(selected)
        else:
            generate(selected, spec, force=args.force or args.reroll)
        if args.sheet:
            write_sheets(selected)
    except (jobs.JobError, ConfigError, ArtError) as error:
        print(f"[ERROR] {error}", file=sys.stderr)
        return 1
    return 0


def generate(selected: list[jobs.Job], spec: jobs.Spec, force: bool) -> None:
    pending = [job for job in selected if force or not job.output.exists()]
    if not pending:
        print("[ART] Everything selected is already generated")
        return
    credentials = load_credentials(os.environ, REPO_ROOT / ".env")
    for index, job in enumerate(pending, 1):
        print(f"[ART] {index}/{len(pending)} {job.key} (seed {job.seed})")
        data = generate_image(credentials, spec["model"], job.prompt, job.seed, int(spec.get("steps", 8)))
        job.raw.parent.mkdir(parents=True, exist_ok=True)
        job.raw.write_bytes(data)
        save_sprite(job, Image.open(io.BytesIO(data)))


def reprocess(selected: list[jobs.Job]) -> None:
    for job in selected:
        if not job.raw.exists():
            raise jobs.JobError(f"no raw image for {job.key}; generate it first")
        save_sprite(job, load(job.raw))
        print(f"[ART] Reprocessed {job.key}")


def save_sprite(job: jobs.Job, raw: Image.Image) -> None:
    job.output.parent.mkdir(parents=True, exist_ok=True)
    process.for_job(job, raw).save(job.output)


def write_sheets(selected: list[jobs.Job]) -> None:
    SHEETS_ROOT.mkdir(parents=True, exist_ok=True)
    for kind in jobs.KINDS:
        entries = [(job.key, load(job.output)) for job in selected if job.kind == kind and job.output.exists()]
        if entries:
            path = SHEETS_ROOT / f"{kind}.png"
            imaging.contact_sheet(entries).save(path)
            print(f"[ART] Wrote {path.relative_to(REPO_ROOT)}")


def load(path: Path) -> Image.Image:
    with Image.open(path) as image:
        return image.copy()


if __name__ == "__main__":
    sys.exit(main())
```

`.env.example` (repo root):

```text
CF_API_TOKEN=your-cloudflare-api-token
CF_ACCOUNT_ID=your-cloudflare-account-id
```

- [ ] **Step 6: Run tests to verify they pass**

Run (from `tools/art`): `.venv/Scripts/python -m pytest`
Expected: `31 passed`.

If `test_prompts_cover_all_content` fails, fix `prompts.json` (never the test): add or remove entries until the job keys match the content.

- [ ] **Step 7: Add the art preview capture**

`Tests/Runtime/ArtPreviewCapture.cs`:

```csharp
#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mortar.Unity.Tests
{
    public sealed class ArtPreviewCapture
    {
        public const int Width = 1280;
        public const int Height = 720;

        [UnityTest]
        [Explicit("Writes art preview images to out/; run with -Filter ArtPreviewCapture")]
        public IEnumerator CaptureTownAndCathedral()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SpriteCatalog>("Assets/Art/SpriteCatalog.asset");
            Assert.IsNotNull(catalog, "Rebuild the sprite catalog first");
            var folder = TestPaths.FreshSaveFolder();
            var host = new GameObject("art-preview");
            host.SetActive(false);
            var app = host.AddComponent<MortarApp>();
            app.Configure(catalog, UiRoot.CreatePanelSettings(null), folder);
            host.SetActive(true);

            app.StartNewGame();
            yield return null;
            Capture(app.CameraRig.Camera, "town");
            Journeys.TakeStairsDown(app.Game);
            yield return null;
            Capture(app.CameraRig.Camera, "cathedral");

            Object.Destroy(host);
            TestPaths.Delete(folder);
        }

        private static void Capture(Camera camera, string name)
        {
            var target = new RenderTexture(Width, Height, 24);
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();
            camera.targetTexture = null;
            RenderTexture.active = null;
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "out", $"art-preview-{name}.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.Destroy(image);
            target.Release();
            Object.Destroy(target);
            Debug.Log($"[Mortar] Wrote {path}");
        }
    }
}
#endif
```

The camera render shows the world only; the UI Toolkit panel draws to the screen, not the camera.

Run: `pwsh -File tools/unity/test.ps1 -Platform PlayMode`
Expected: 33 passed, 0 failed; the explicit capture is skipped.

- [ ] **Step 8: Add the art tool section to `docs/unity.md`**

Append:

```markdown
## Placeholder art

`tools/art` generates the placeholder sprites with Cloudflare Workers AI (FLUX-1 schnell) and Pillow.

1. Copy `.env.example` to `.env` at the repo root and fill in `CF_API_TOKEN` and `CF_ACCOUNT_ID`. `.env` is gitignored; environment variables with the same names take precedence.
2. Create the environment:

   ```powershell
   py -3 -m venv tools/art/.venv
   tools/art/.venv/Scripts/python -m pip install -r tools/art/requirements.txt
   ```

3. Generate (from `tools/art`):

   ```powershell
   .venv/Scripts/python generate.py --kind all --sheet
   ```

   Existing sprites are skipped unless you pass `--force`. `--only actors/warrior` limits the run to one entry; `--reroll` with `--only` bumps that entry's seed in `prompts.json` and regenerates every variant. `--reprocess` rebuilds sprites from the raw images in `tools/art/out/raw` without calling the API. Contact sheets go to `tools/art/out/sheets`.
4. In Unity, run `Mortar/Rebuild Sprite Catalog` (or `run-method.ps1 -Method Mortar.Unity.EditorTools.SpriteCatalogBuilder.RebuildFromCommandLine`).

Sprites are stills: one front-facing image per actor, reused for all eight directions with a horizontal flip and a facing arrow. Tiles are seamless textures projected onto 64×32 diamonds, or 64×64 blocks for walls and trees.

Run the art tool tests from `tools/art` with `.venv/Scripts/python -m pytest`.
```

- [ ] **Step 9: Commit the tool**

```powershell
git add tools/art/mortar_art/config.py tools/art/mortar_art/workers_ai.py tools/art/mortar_art/jobs.py tools/art/mortar_art/process.py tools/art/generate.py tools/art/prompts.json tools/art/tests/test_config.py tools/art/tests/test_workers_ai.py tools/art/tests/test_jobs.py tools/art/tests/test_process.py .env.example unity/com.mortar.unity/Tests/Runtime/ArtPreviewCapture.cs unity/com.mortar.unity/Tests/Runtime/ArtPreviewCapture.cs.meta docs/unity.md
git commit -m "add workers ai art generation tool"
```

- [ ] **Step 10 (controller only): Generate, review, and commit the art**

The implementer stops after Step 9. The controller runs these steps and must never print credential values.

1. Create `mortar/.env` from the approved source, printing key names only:

   ```powershell
   Get-Content C:\bws-development\centre-county-live\.env | Where-Object { $_ -match '^(CF_API_TOKEN|CF_ACCOUNT_ID)=' } | Set-Content .env -Encoding utf8
   git check-ignore -q .env; if ($LASTEXITCODE -ne 0) { throw '.env is not ignored' }
   Get-Content .env | ForEach-Object { ($_ -split '=', 2)[0] }
   ```

   Expected: the last command prints exactly `CF_API_TOKEN` and `CF_ACCOUNT_ID`.
2. Generate everything with sheets (from `tools/art`): `.venv/Scripts/python generate.py --kind all --sheet`. Expected: 43 `[ART]` progress lines, then three sheet paths.
3. Rebuild the catalog: `pwsh -File tools/unity/run-method.ps1 -Method Mortar.Unity.EditorTools.SpriteCatalogBuilder.RebuildFromCommandLine`.
4. Capture previews: `pwsh -File tools/unity/test.ps1 -Platform PlayMode -Filter ArtPreviewCapture`. Expected: `out/art-preview-town.png` and `out/art-preview-cathedral.png`.
5. Show the user `tools/art/out/sheets/tiles.png`, `actors.png`, `icons.png`, and both previews inline. Reroll whatever the user rejects with `--only <entry> --reroll --sheet`, then repeat steps 3–5.
6. After approval, run `git status --short` and confirm `.env`, `tools/art/out/`, and `tools/art/.venv/` are absent, then:

   ```powershell
   git add tools/art/prompts.json unity/Mortar.Sample/Assets/Art unity/Mortar.Sample/Assets/Art.meta
   git commit -m "add generated placeholder art"
   ```

---

### Task 14: Documentation and final checks

**Files:**
- Modify: `README.md`, `docs/superpowers/specs/2026-10-14-unity-layer-design.md`

- [ ] **Step 1: Update the README**

In `README.md`, replace the first `## Status` paragraph:

```markdown
Version 0.1 generates **indoor** levels by stitching hand-made rooms along a landmark graph. Outdoor generation is planned (see `docs/design.md`).
```

Append to the end of the second `## Status` paragraph:

```markdown
The Unity package (`unity/com.mortar.unity`) and its sample project make that first milestone playable with generated placeholder art (see `docs/unity.md`).
```

Add these rows to the repository layout table, after the `content/` row:

```markdown
| `unity/com.mortar.unity` | The Unity package: views, input, UI, and editor tools over the simulation. |
| `unity/Mortar.Sample` | A Unity 6 sample project with the playable scene and placeholder art. |
| `tools/unity` | Scripts that sync the simulation into Unity and run Unity tests headless. |
| `tools/art` | Placeholder art generator (Workers AI and Pillow). |
```

- [ ] **Step 2: Correct the spec**

In `docs/superpowers/specs/2026-10-14-unity-layer-design.md`:

- Replace ``- `1`–`4` → `UseBeltCommand`; `I` inventory; `C` character sheet; `Esc` menu.`` with ``- `1`–`8` → `UseBeltCommand`; `I` inventory; `C` character sheet; `Esc` menu.``
- Replace `UI Toolkit (UXML/USS in the package):` with `UI Toolkit, built in C# with one theme file:`.
- Replace `4-slot belt with hotkey labels` with `8-slot belt with hotkey labels`.
- Replace the inventory bullet's interaction sentences ("Click to equip or unequip, drag to move within the grid (`MoveItemCommand`), drag to the belt (`PutInBeltCommand`), drag out of the window to drop (`DropItemCommand`).") with: "Left-click an item to hold it, then left-click a bag cell (`MoveItemCommand`), an equipment slot (`EquipCommand`), a belt slot (`PutInBeltCommand`), or the world (`DropItemCommand`). Right-click equips, unequips, or belts in one step."

- [ ] **Step 3: Run every check**

```powershell
dotnet test Mortar.slnx
pwsh -File tools/unity/sync.ps1
pwsh -File tools/unity/test.ps1 -Platform EditMode
pwsh -File tools/unity/test.ps1 -Platform PlayMode
Push-Location tools/art; .venv/Scripts/python -m pytest; Pop-Location
```

Expected: `dotnet test` passes with the same count as before this plan; EditMode 81 passed; PlayMode 33 passed (capture skipped); pytest 31 passed.

- [ ] **Step 4: Manual play check (controller with the user)**

Open the sample scene and play: new game in town, walk to the cathedral, descend to depth 4, fight, pick up and equip loot, drink a potion with a hotkey, spend stat points after a level-up, save and continue from the pause menu, and let the warrior die to confirm the death screen's Load Last Save and New Game both work. Note any issues for follow-up rather than fixing them in this step.

- [ ] **Step 5: Commit**

```powershell
git add README.md docs/superpowers/specs/2026-10-14-unity-layer-design.md
git commit -m "document the unity layer"
```
