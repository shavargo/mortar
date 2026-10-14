namespace Mortar.Game.Pathing;

public static class Reach
{
    /// <summary>True when <paramref name="to"/> is a neighbour of <paramref name="from"/> and, for diagonals, neither corner is a wall.</summary>
    public static bool CanStrike(Cell from, Cell to, Func<Cell, bool> walkable) =>
        from.IsAdjacentTo(to) && CornersClear(from, to, walkable);

    /// <summary>A diagonal move or strike needs both orthogonal neighbours walkable; straight ones always pass.</summary>
    public static bool CornersClear(Cell from, Cell to, Func<Cell, bool> walkable) =>
        from.X == to.X || from.Y == to.Y || (walkable(new Cell(to.X, from.Y)) && walkable(new Cell(from.X, to.Y)));
}
