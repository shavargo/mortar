using Mortar.Core.Layouts;
using Mortar.Core.Model;

namespace Mortar.Tests.Support;

internal static partial class TestContent
{
    public static CellGrid Cells(Tileset tileset, params string[] rows)
    {
        var grid = new CellGrid(rows[0].Length, rows.Length, tileset.VoidIndex);
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                grid[x, y] = tileset.IndexOfSymbol(rows[y][x]);
            }
        }
        return grid;
    }
}
