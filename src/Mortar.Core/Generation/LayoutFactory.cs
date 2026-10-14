using Mortar.Core.Layouts;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Core.Tiles;

namespace Mortar.Core.Generation;

internal static class LayoutFactory
{
    public static Layout Create(
        CompiledArea area,
        uint seed,
        int attempt,
        CellGrid grid,
        IReadOnlyList<Marker> markers,
        Pcg32 rng)
    {
        var tileset = area.Tileset;
        var tiles = TileResolver.Resolve(grid, tileset, rng);
        var cells = new LayoutCells(
            grid.Width,
            grid.Height,
            tileset.Grounds.Select(g => g.Id).ToArray(),
            (int[])grid.Ground.Clone(),
            tiles.TileTypes,
            tiles.Tiles,
            tiles.Variants,
            grid.Ground.Select(tileset.IsWalkable).ToArray());
        return new Layout(area.Definition.Id, seed, attempt, cells, markers, MortarInfo.Version, Layout.CurrentFormatVersion);
    }
}
