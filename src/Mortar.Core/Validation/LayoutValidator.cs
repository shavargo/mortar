using Mortar.Core.Layouts;
using Mortar.Core.Model;

namespace Mortar.Core.Validation;

internal static class LayoutValidator
{
    public static string? Validate(CellGrid grid, Tileset tileset, IReadOnlyList<Marker> markers, TargetSize target)
    {
        var entrance = markers.FirstOrDefault(m => m.Kind == MarkerKinds.Entrance);
        if (entrance is null)
        {
            return "entrance missing";
        }
        var reached = FloodFill(grid, tileset, entrance.X, entrance.Y);
        var unreachable = markers.FirstOrDefault(m => !reached[m.Y * grid.Width + m.X]);
        if (unreachable is not null)
        {
            return $"{unreachable.Kind} unreachable";
        }
        VoidUnreached(grid, tileset, reached);
        return CheckSize(grid, tileset, target);
    }

    internal static bool[] FloodFill(CellGrid grid, Tileset tileset, int startX, int startY)
    {
        var reached = new bool[grid.Width * grid.Height];
        if (!tileset.IsWalkable(grid[startX, startY]))
        {
            return reached;
        }
        var queue = new Queue<(int X, int Y)>();
        reached[startY * grid.Width + startX] = true;
        queue.Enqueue((startX, startY));
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (nx, ny) in new[] { (x, y - 1), (x + 1, y), (x, y + 1), (x - 1, y) })
            {
                if (!grid.InBounds(nx, ny) || reached[ny * grid.Width + nx] || !tileset.IsWalkable(grid[nx, ny]))
                {
                    continue;
                }
                reached[ny * grid.Width + nx] = true;
                queue.Enqueue((nx, ny));
            }
        }
        return reached;
    }

    internal static void VoidUnreached(CellGrid grid, Tileset tileset, bool[] reached)
    {
        for (var i = 0; i < grid.Ground.Length; i++)
        {
            if (!reached[i] && tileset.IsWalkable(grid.Ground[i]))
            {
                grid.Ground[i] = tileset.VoidIndex;
            }
        }
    }

    private static string? CheckSize(CellGrid grid, Tileset tileset, TargetSize target)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                if (!tileset.IsWalkable(grid[x, y])) continue;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }
        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        if (maxX < 0 || width < target.MinWidth || height < target.MinHeight) return "too small";
        if (width > target.MaxWidth || height > target.MaxHeight) return "too large";
        return null;
    }
}
