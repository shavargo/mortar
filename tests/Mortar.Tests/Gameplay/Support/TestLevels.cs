using Mortar.Core.Layouts;
using Mortar.Core.Model;
using Mortar.Game.Content;
using Mortar.Game.Simulation;

namespace Mortar.Tests.Gameplay.Support;

/// <summary>'#' is wall; every other character is floor. Marker characters (E, X, S, B, W, C) also place markers.</summary>
internal static class TestLevels
{
    public static Layout FromAscii(params string[] rows)
    {
        var height = rows.Length;
        var width = rows[0].Length;
        var ground = new int[width * height];
        var walkable = new bool[width * height];
        var markers = new List<Marker>();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var symbol = rows[y][x];
                var index = y * width + x;
                ground[index] = symbol == '#' ? 1 : 0;
                walkable[index] = symbol != '#';
                if (MarkerChars.TryGetKind(symbol, out var kind)) markers.Add(new Marker(kind, null, x, y));
            }
        }
        var cells = new LayoutCells(
            width, height, new[] { "floor", "wall" }, ground, new[] { "floor" },
            new int[width * height], new int[width * height], walkable);
        return new Layout("test", 0, 0, cells, markers, "test", Layout.CurrentFormatVersion);
    }
}

internal sealed class FixedLayoutGenerator : ILevelGenerator
{
    private readonly Layout _layout;

    public FixedLayoutGenerator(Layout layout) => _layout = layout;

    public LevelBuildResult Generate(LevelDefinition level, uint seed) => LevelBuildResult.Success(_layout);
}

internal sealed class FixedLevelGenerator : ILevelGenerator
{
    private readonly string[]? _town;
    private readonly string[][] _levels;

    public FixedLevelGenerator(params string[][] levels)
        : this(null, levels)
    {
    }

    private FixedLevelGenerator(string[]? town, string[][] levels)
    {
        _town = town;
        _levels = levels;
    }

    /// <summary>Depth 0 is built from <paramref name="town"/>; depth n from the nth entry of <paramref name="levels"/>.</summary>
    public static FixedLevelGenerator WithTown(string[] town, params string[][] levels) => new FixedLevelGenerator(town, levels);

    public LevelBuildResult Generate(LevelDefinition level, uint seed)
    {
        var rows = level.Depth == 0 ? _town : level.Depth <= _levels.Length ? _levels[level.Depth - 1] : null;
        return rows is null ? LevelBuildResult.Failure("no test layout") : LevelBuildResult.Success(TestLevels.FromAscii(rows));
    }
}
