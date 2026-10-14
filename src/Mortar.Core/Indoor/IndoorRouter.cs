using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Indoor;

internal static class IndoorRouter
{
    public static string? Route(CompiledArea area, SlotGrid grid, Pcg32 rng)
    {
        var finder = new SlotPathfinder(grid, rng);
        foreach (var edge in area.Definition.Edges.Where(e => e.Kind == EdgeKind.Path))
        {
            var path = finder.FindPath(grid.BlockOfNode(edge.From), grid.BlockOfNode(edge.To));
            if (path is null)
            {
                return $"no path: {edge.From}->{edge.To}";
            }
            grid.ConnectPath(path);
        }
        foreach (var edge in area.Definition.Edges.Where(e => e.Kind == EdgeKind.Branch))
        {
            if (rng.NextInt(2) == 0) continue;
            var path = finder.FindPath(grid.BlockOfNode(edge.From), grid.BlockOfNode(edge.To));
            if (path is not null) grid.ConnectPath(path);
        }
        AddSideBranches(area.Definition.MaxSideBranches, grid, rng);
        return null;
    }

    private static void AddSideBranches(int maxSideBranches, SlotGrid grid, Pcg32 rng)
    {
        var count = rng.NextInt(maxSideBranches + 1);
        for (var i = 0; i < count; i++)
        {
            var options = SideBranchOptions(grid);
            if (options.Count == 0) return;
            var (x, y, side) = rng.Pick(options);
            var (dx, dy) = Sides.Offset(side);
            grid.Open(x, y, side);
            grid.MarkUsed(x + dx, y + dy);
        }
    }

    private static List<(int X, int Y, Side Side)> SideBranchOptions(SlotGrid grid)
    {
        var options = new List<(int X, int Y, Side Side)>();
        for (var y = 0; y < grid.Rows; y++)
        {
            for (var x = 0; x < grid.Columns; x++)
            {
                if (!grid.IsUsed(x, y) || grid.BlockAt(x, y) != -1) continue;
                foreach (var side in Sides.All)
                {
                    var (dx, dy) = Sides.Offset(side);
                    if (grid.InBounds(x + dx, y + dy) && !grid.IsUsed(x + dx, y + dy))
                    {
                        options.Add((x, y, side));
                    }
                }
            }
        }
        return options;
    }
}
