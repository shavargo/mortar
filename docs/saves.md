# Saves

`Mortar.Json.Saves.SaveFile` writes and reads whole worlds as JSON.

- `SaveFile.Write(path, world)` returns null on success or an error message. It writes `<path>.tmp` first and then swaps it into place, so an interrupted write never damages the previous save.
- `SaveFile.Read(path, content, generator)` returns a `WorldResult`. Files over 16 MiB, broken JSON, unknown properties, wrong value types, and saves that don't fit the content (unknown ids, impossible stats or positions, levels whose layout no longer matches) are all reported as errors, never thrown.

A save stores the seed, the tick, the random generator state, the player (class, level, experience, stats, gold, inventory, belt, equipment, and current action), and every visited level: its seed, a fingerprint of its layout, its monsters (alive or dead), and the items on the ground. Levels are rebuilt from their seeds when loading; if the rebuilt layout's fingerprint differs (for example because an area file changed), the save is rejected.

Loading a save and continuing produces exactly the same game as never having saved.

`version` is 1. A save with any other version is rejected with a message naming both versions.

The simulation never writes files on its own. The engine layer saves when it chooses to, for example after each `LevelChanged` event.
