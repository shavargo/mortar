namespace Mortar.Game;

public static class Directions
{
    private static readonly int[] Dx = { 0, -1, -1, -1, 0, 1, 1, 1 };
    private static readonly int[] Dy = { 1, 1, 0, -1, -1, -1, 0, 1 };

    public static IReadOnlyList<Direction> All { get; } = new[]
    {
        Direction.South, Direction.SouthWest, Direction.West, Direction.NorthWest,
        Direction.North, Direction.NorthEast, Direction.East, Direction.SouthEast,
    };

    public static int DeltaX(this Direction direction) => Dx[(int)direction];

    public static int DeltaY(this Direction direction) => Dy[(int)direction];

    public static bool IsDiagonal(this Direction direction) => direction.DeltaX() != 0 && direction.DeltaY() != 0;

    public static Cell Step(this Cell cell, Direction direction) =>
        new Cell(cell.X + direction.DeltaX(), cell.Y + direction.DeltaY());

    public static Direction Toward(Cell from, Cell to)
    {
        var dx = Math.Sign(to.X - from.X);
        var dy = Math.Sign(to.Y - from.Y);
        for (var i = 0; i < Dx.Length; i++)
        {
            if (Dx[i] == dx && Dy[i] == dy)
            {
                return (Direction)i;
            }
        }
        return Direction.South;
    }
}
