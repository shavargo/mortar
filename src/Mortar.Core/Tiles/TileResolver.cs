using Mortar.Core.Layouts;
using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Tiles;

internal static class TileResolver
{
    public static ResolvedTiles Resolve(CellGrid grid, Tileset tileset, Pcg32 rng)
    {
        var palette = new List<string>();
        var paletteIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        var tiles = new int[grid.Width * grid.Height];
        var variants = new int[tiles.Length];
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var tileType = TileTypeAt(grid, tileset, x, y);
                if (!paletteIndex.TryGetValue(tileType, out var index))
                {
                    index = palette.Count;
                    palette.Add(tileType);
                    paletteIndex[tileType] = index;
                }
                var weights = tileset.WeightsFor(tileType);
                tiles[y * grid.Width + x] = index;
                variants[y * grid.Width + x] = weights.Count == 1 ? 0 : rng.PickWeighted(weights);
            }
        }
        return new ResolvedTiles(palette, tiles, variants);
    }

    public static string TileTypeAt(CellGrid grid, Tileset tileset, int x, int y)
    {
        var corners = new[]
        {
            Corner(grid, tileset, x, y),
            Corner(grid, tileset, x + 1, y),
            Corner(grid, tileset, x, y + 1),
            Corner(grid, tileset, x + 1, y + 1),
        };
        return Name(tileset, corners);
    }

    public static string Name(Tileset tileset, int[] corners)
    {
        var distinct = corners.Distinct().OrderBy(index => index).ToArray();
        if (distinct.Length == 1)
        {
            return tileset.Grounds[distinct[0]].Id;
        }
        if (distinct.Length == 2)
        {
            var mask = new string(corners.Select(c => c == distinct[1] ? '1' : '0').ToArray());
            return $"{tileset.Grounds[distinct[0]].Id}-{tileset.Grounds[distinct[1]].Id}:{mask}";
        }
        return string.Join("|", corners.Select(c => tileset.Grounds[c].Id));
    }

    private static int Corner(CellGrid grid, Tileset tileset, int x, int y) =>
        grid.InBounds(x, y) ? grid[x, y] : tileset.VoidIndex;
}
