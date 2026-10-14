using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class IndoorRouterTests
{
    [Fact]
    public void Route_OpensEveryLandmarkToTheLevel()
    {
        var area = TestContent.CompileCrypt();
        var grid = SlotGrid.ForArea(area.Definition);
        var rng = Pcg32.ForAttempt("crypt", 1, 0);
        Assert.Null(LandmarkPlacer.PlaceAll(area, grid, rng));
        Assert.Null(IndoorRouter.Route(area, grid, rng));
        Assert.All(grid.Placements, placement => Assert.True(HasOpening(grid, placement), placement.NodeId));
    }

    private static bool HasOpening(SlotGrid grid, Placement placement)
    {
        for (var y = placement.SlotY; y < placement.SlotY + placement.Room.SlotsHigh; y++)
        {
            for (var x = placement.SlotX; x < placement.SlotX + placement.Room.SlotsWide; x++)
            {
                foreach (var side in Sides.All)
                {
                    var (dx, dy) = Sides.Offset(side);
                    if (!placement.Contains(x + dx, y + dy) && grid.IsOpen(x, y, side))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}
