# Mortar for Unity

The Unity layer lives in two folders:

- `unity/com.backwoods.mortar`: the UPM package (runtime, editor tools, tests).
- `unity/Mortar.Sample`: a sample project that references the package from disk and contains the playable scene.

## Setup

1. Install the editor version in `unity/Mortar.Sample/ProjectSettings/ProjectVersion.txt` (Unity 6 LTS). The scripts find it through the Unity CLI; set `MORTAR_UNITY` to an editor executable to override.
2. Build and copy the simulation into the package and the content into the sample:

   ```powershell
   pwsh -File tools/unity/sync.ps1
   ```

   This publishes `Mortar.Json` for netstandard2.1 into `com.backwoods.mortar/Plugins/` and copies `content/` into `Mortar.Sample/Assets/StreamingAssets/content/`. Both folders are gitignored; run the script again after changing simulation code or content.
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
