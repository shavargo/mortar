using Mortar.Core.Indoor;
using Mortar.Core.Rng;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class LandmarkPlacerTests
{
    [Theory]
    [InlineData(0.0, 0.1, 7, 0)]
    [InlineData(0.9, 1.0, 7, 6)]
    [InlineData(0.0, 1.0, 1, 0)]
    public void PickSlot_StaysInsideRegion(double from, double to, int count, int expected)
    {
        var rng = new Pcg32(1, 1);
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(expected, LandmarkPlacer.PickSlot(from, to, count, rng));
        }
    }

    [Fact]
    public void PlaceAll_PutsEachLandmarkInItsRegion()
    {
        var area = TestContent.CompileCrypt();
        var grid = SlotGrid.ForArea(area.Definition);
        Assert.Null(LandmarkPlacer.PlaceAll(area, grid, Pcg32.ForAttempt("crypt", 1, 0)));
        Assert.Equal(4, grid.Placements.Count);
        Assert.Equal(0, grid.Placements[grid.BlockOfNode("entrance")].SlotX);
        Assert.Equal(0, grid.Placements[grid.BlockOfNode("waypoint")].SlotY);
        Assert.Equal(5, grid.Placements[grid.BlockOfNode("boss")].SlotX);
        Assert.Equal(6, grid.Placements[grid.BlockOfNode("exit")].SlotY);
    }
}
