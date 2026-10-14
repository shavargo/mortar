using Mortar.Core.Generation;
using Mortar.Core.Layouts;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Generation;

public class WalkabilityTests
{
    private static readonly CompiledArea Crypt = SampleArea.LoadCrypt();

    [Fact]
    public void SuccessfulLayouts_ReachEveryMarkerAndWalkableCell()
    {
        for (uint seed = 0; seed < 50; seed++)
        {
            var result = Generator.Generate(Crypt, seed);
            if (!result.Succeeded) continue;
            var layout = result.Layout!;
            var entrance = layout.Markers.First(m => m.Kind == MarkerKinds.Entrance);
            var reached = Reach(layout.Cells, entrance.X, entrance.Y);
            foreach (var marker in layout.Markers)
            {
                Assert.True(reached[marker.Y * layout.Width + marker.X], $"seed {seed}: {marker.Kind} unreachable");
            }
            Assert.Equal(layout.Cells.Walkable, reached);
        }
    }

    [Fact]
    public void SampleArea_SucceedsOnAtLeast80PercentOfSeeds()
    {
        var successes = Enumerable.Range(0, 100).Count(seed => Generator.Generate(Crypt, (uint)seed).Succeeded);
        Assert.True(successes >= 80, $"only {successes}/100 seeds succeeded");
    }

    private static bool[] Reach(LayoutCells cells, int startX, int startY)
    {
        var reached = new bool[cells.Width * cells.Height];
        var queue = new Queue<(int X, int Y)>();
        reached[startY * cells.Width + startX] = true;
        queue.Enqueue((startX, startY));
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (nx, ny) in new[] { (x, y - 1), (x + 1, y), (x, y + 1), (x - 1, y) })
            {
                if (nx < 0 || ny < 0 || nx >= cells.Width || ny >= cells.Height) continue;
                if (reached[ny * cells.Width + nx] || !cells.IsWalkable(nx, ny)) continue;
                reached[ny * cells.Width + nx] = true;
                queue.Enqueue((nx, ny));
            }
        }
        return reached;
    }
}
