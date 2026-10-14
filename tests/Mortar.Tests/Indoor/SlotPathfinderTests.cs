using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class SlotPathfinderTests
{
    private static readonly Room Cross = TestContent.CompileRoom("cross", new[] { "filler" }, TestContent.Box(9, 9, new[] { 4 }));
    private static readonly Room NorthOnly = TestContent.CompileRoom("north", new[] { "filler" }, TestContent.Box(9, 9, Array.Empty<int>(), (4, 0, 'D')));

    [Fact]
    public void FindPath_ConnectsBlocksWithAdjacentSteps()
    {
        var grid = new SlotGrid(5, 1, 8);
        grid.Place(new Placement(Cross, 0, 0, "a"));
        grid.Place(new Placement(Cross, 4, 0, "b"));
        var path = new SlotPathfinder(grid, new Pcg32(1, 1)).FindPath(0, 1);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, path);
    }

    [Fact]
    public void FindPath_DoesNotCrossOtherLandmarks()
    {
        var grid = new SlotGrid(3, 1, 8);
        grid.Place(new Placement(Cross, 0, 0, "a"));
        grid.Place(new Placement(Cross, 1, 0, "middle"));
        grid.Place(new Placement(Cross, 2, 0, "b"));
        Assert.Null(new SlotPathfinder(grid, new Pcg32(1, 1)).FindPath(0, 2));
    }

    [Fact]
    public void FindPath_LeavesBlocksOnlyThroughDoors()
    {
        var grid = new SlotGrid(2, 2, 8);
        grid.Place(new Placement(NorthOnly, 0, 1, "a"));
        grid.Place(new Placement(Cross, 1, 1, "b"));
        var path = new SlotPathfinder(grid, new Pcg32(1, 1)).FindPath(0, 1);
        Assert.Equal(new[] { grid.Index(0, 1), grid.Index(0, 0), grid.Index(1, 0), grid.Index(1, 1) }, path);
    }
}
