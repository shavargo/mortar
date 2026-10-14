using Mortar.Core.Model;

namespace Mortar.Core.Indoor;

internal static class Sides
{
    public static readonly IReadOnlyList<Side> All = new[] { Side.North, Side.East, Side.South, Side.West };

    public static (int Dx, int Dy) Offset(Side side) => side switch
    {
        Side.North => (0, -1),
        Side.East => (1, 0),
        Side.South => (0, 1),
        _ => (-1, 0),
    };

    public static Side Opposite(Side side) => side switch
    {
        Side.North => Side.South,
        Side.East => Side.West,
        Side.South => Side.North,
        _ => Side.East,
    };

    public static Side Toward(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => Side.North,
        (1, 0) => Side.East,
        (0, 1) => Side.South,
        (-1, 0) => Side.West,
        _ => throw new ArgumentException("slots are not adjacent"),
    };
}
