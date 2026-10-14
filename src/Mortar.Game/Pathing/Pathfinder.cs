namespace Mortar.Game.Pathing;

public static class Pathfinder
{
    public const int MaxExpanded = 4096;

    public static IReadOnlyList<Cell>? FindPath(Cell start, Cell goal, Func<Cell, bool> walkable, Func<Cell, bool> free) =>
        Search(start, cell => cell == goal, cell => cell.DistanceTo(goal), walkable, free);

    public static IReadOnlyList<Cell>? FindPathNextTo(Cell start, Cell target, Func<Cell, bool> walkable, Func<Cell, bool> free) =>
        Search(start, cell => cell.IsAdjacentTo(target), cell => Math.Max(0, cell.DistanceTo(target) - 1), walkable, free);

    private static IReadOnlyList<Cell>? Search(
        Cell start,
        Func<Cell, bool> isGoal,
        Func<Cell, int> estimate,
        Func<Cell, bool> walkable,
        Func<Cell, bool> free)
    {
        var open = new MinHeap();
        var cost = new Dictionary<Cell, int> { [start] = 0 };
        var cameFrom = new Dictionary<Cell, Cell>();
        var closed = new HashSet<Cell>();
        open.Push(start, 0, estimate(start));
        while (open.Count > 0)
        {
            var current = open.Pop();
            if (!closed.Add(current)) continue;
            if (isGoal(current)) return Rebuild(cameFrom, start, current);
            if (closed.Count > MaxExpanded) return null;
            foreach (var direction in Directions.All)
            {
                var next = current.Step(direction);
                if (closed.Contains(next) || !CanStep(current, direction, walkable, free)) continue;
                var nextCost = cost[current] + 1;
                if (cost.TryGetValue(next, out var known) && known <= nextCost) continue;
                cost[next] = nextCost;
                cameFrom[next] = current;
                open.Push(next, nextCost, estimate(next));
            }
        }
        return null;
    }

    private static bool CanStep(Cell from, Direction direction, Func<Cell, bool> walkable, Func<Cell, bool> free)
    {
        var to = from.Step(direction);
        if (!free(to)) return false;
        if (!direction.IsDiagonal()) return true;
        return walkable(new Cell(to.X, from.Y)) && walkable(new Cell(from.X, to.Y));
    }

    private static IReadOnlyList<Cell> Rebuild(Dictionary<Cell, Cell> cameFrom, Cell start, Cell end)
    {
        var path = new List<Cell>();
        for (var cell = end; cell != start; cell = cameFrom[cell])
        {
            path.Add(cell);
        }
        path.Reverse();
        return path;
    }
}
