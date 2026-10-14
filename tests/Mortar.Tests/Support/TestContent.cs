using Mortar.Core.Authoring;
using Mortar.Core.Model;

namespace Mortar.Tests.Support;

internal static partial class TestContent
{
    public static Tileset CreateTileset(
        IReadOnlyDictionary<string, IReadOnlyList<int>>? weights = null,
        IReadOnlyList<MarkerType>? markers = null)
    {
        var grounds = new[]
        {
            new GroundType("void", ' ', false, false),
            new GroundType("floor", '.', true, false),
            new GroundType("wall", '#', false, true),
            new GroundType("water", '~', false, false),
        };
        var result = Tileset.Create("test-tileset.json", grounds, weights ?? new Dictionary<string, IReadOnlyList<int>>(), markers);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        return result.Value!;
    }

    public static string[] Box(int width, int height, int[] doorOffsets, params (int X, int Y, char C)[] cells)
    {
        var rows = new char[height][];
        for (var y = 0; y < height; y++)
        {
            rows[y] = new char[width];
            for (var x = 0; x < width; x++)
            {
                var edge = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                rows[y][x] = edge ? '#' : '.';
            }
        }
        foreach (var offset in doorOffsets)
        {
            if (offset < width)
            {
                rows[0][offset] = 'D';
                rows[height - 1][offset] = 'D';
            }
            if (offset < height)
            {
                rows[offset][0] = 'D';
                rows[offset][width - 1] = 'D';
            }
        }
        foreach (var cell in cells)
        {
            rows[cell.Y][cell.X] = cell.C;
        }
        return rows.Select(row => new string(row)).ToArray();
    }

    public static RoomDefinition RoomDef(string id, string[] tags, string[] grid, int weight = 1) =>
        new RoomDefinition($"{id}.json", id, tags, grid, weight);

    public static Room CompileRoom(string id, string[] tags, string[] grid, int weight = 1)
    {
        var result = RoomCompiler.Compile(RoomDef(id, tags, grid, weight), CreateTileset(), 9);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        return result.Value!;
    }
}
