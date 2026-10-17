# Saves

`Mortar.Json.Saves.SaveFile` writes and reads whole worlds as JSON.

- `SaveFile.Write(path, world)` returns null on success or an error message. It writes `<path>.tmp` first and then swaps it into place, so an interrupted write never damages the previous save.
- `SaveFile.Read(path, content, generator)` returns a `WorldResult`. Files over 16 MiB, broken JSON, unknown properties, wrong value types, and saves that don't fit the content (unknown ids, impossible stats or positions, levels whose layout no longer matches) are all reported as errors, never thrown.

A save stores the seed, the tick, the random generator state, the open town portal (if any), the active vendor session (if any), vendor stock and the next stock-regen tick, the player (class, level, experience, stats, gold, inventory, belt, equipment, known spells, and current action), and every visited level: its seed, a fingerprint of its layout, its monsters (alive or dead), items on the ground, and in-flight projectiles. Item records include stack count, staff charges, whether the item is identified, and current durability (max comes from the item base on restore). Levels are rebuilt from their seeds when loading; if the rebuilt layout's fingerprint differs (for example because an area file changed), the save is rejected.

Loading a save and continuing produces exactly the same game as never having saved.

Restore also rejects values that play cannot produce: a tick, gold, or experience above `long.MaxValue / 2`, `nextActorId`, `nextItemId`, or `nextProjectileId` above `int.MaxValue / 2`, experience outside the range for the saved level (at least the current level's threshold and, below the maximum level, under the next one), and enum names that are not an exact, case-sensitive member name.

`version` is 3 (the property name is matched case-insensitively, like every other property). Older saves still load:

- Version 1 → 2 → 3: no spells, no portal, no projectiles, all items marked identified, staff charges filled to each base's `maxCharges`, then the version 2→3 steps below.
- Version 2 → 3: gear durability set to each base's max, vendor stocks cleared and the next regen tick scheduled, no active vendor session.

Any other version is rejected with a message naming both versions. Null or blank input is reported as "save is empty".

The simulation never writes files on its own. The engine layer saves when it chooses to, for example after each `LevelChanged` event.
