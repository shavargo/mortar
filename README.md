# Mortar

Mortar generates replayable, seed-based levels for action RPGs from hand-made pieces. You draw rooms as text, describe each area's overall structure, and Mortar turns a seed into a finished layout. The same seed always produces the same level on Windows, Linux, and macOS.

Mortar is free and open source under the MIT license.

## Status

Version 0.1 generates **indoor** levels by stitching hand-made rooms along a landmark graph. Outdoor generation and the Unity package are planned (see `docs/design.md` and `docs/superpowers/specs/2026-10-14-arpg-framework-design.md`).

## Quick start

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build
dotnet run --project src/Mortar.Cli -- generate --area samples/areas/crypt.json --seed 1 --out crypt-1.json
dotnet run --project src/Mortar.Cli -- preview --layout crypt-1.json --out crypt-1.png
```

`generate` exits with `0` on success, `1` if every attempt failed (it prints counted reasons such as `too small: 6`), and `2` for bad input such as an invalid room file.

## Using the library

```csharp
using Mortar.Core.Generation;
using Mortar.Json;

var area = MortarLoader.LoadArea("samples/areas/crypt.json");
if (!area.Succeeded)
{
    foreach (var error in area.Errors) Console.Error.WriteLine(error);
    return;
}

var result = Generator.Generate(area.Value!, seed: 1);
if (result.Succeeded)
{
    File.WriteAllText("crypt-1.json", LayoutWriter.ToJson(result.Layout!));
}
```

`Mortar.Core` and `Mortar.Json` target .NET Standard 2.1, so they also run inside Unity.

## Repository layout

| Path | Purpose |
| --- | --- |
| `src/Mortar.Core` | The generator. No engine or file-format dependencies. |
| `src/Mortar.Json` | Reads tileset, room, and area files; writes and reads layout files. |
| `src/Mortar.Cli` | The `mortar` command-line tool. |
| `samples/` | A sample tileset, rooms, and the `crypt` indoor area. |
| `tests/` | Unit, determinism snapshot, walkability, and success-rate tests. |
| `docs/` | Design and the authoring guide. |

See `docs/authoring.md` to write your own tilesets, rooms, and areas.

## License

MIT. See `LICENSE`.
