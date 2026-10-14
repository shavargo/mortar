using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class RoomFillerTests
{
    private static readonly Room Cross = TestContent.CompileRoom("cross", new[] { "filler" }, TestContent.Box(9, 9, new[] { 4 }));
    private static readonly Room NorthOnly = TestContent.CompileRoom("north", new[] { "filler" }, TestContent.Box(9, 9, Array.Empty<int>(), (4, 0, 'D')));
    private static readonly Room OffCenter = TestContent.CompileRoom("off", new[] { "filler" }, TestContent.Box(9, 9, Array.Empty<int>(), (0, 2, 'D'), (8, 2, 'D')));

    [Fact]
    public void Fits_CrossRoomCoversAnyOpenings() =>
        Assert.True(RoomFiller.Fits(new SlotGrid(3, 3, 8), Cross, 1, 1, Sides.All));

    [Fact]
    public void Fits_RejectsRoomWithoutDoorOnOpenSide() =>
        Assert.False(RoomFiller.Fits(new SlotGrid(3, 3, 8), NorthOnly, 1, 1, new[] { Side.East }));

    [Fact]
    public void Fits_RequiresDoorsToLineUpWithPlacedNeighbors()
    {
        var grid = new SlotGrid(2, 1, 8);
        grid.Place(new Placement(OffCenter, 1, 0, null));
        Assert.False(RoomFiller.Fits(grid, Cross, 0, 0, new[] { Side.East }));
        Assert.True(RoomFiller.Fits(grid, OffCenter, 0, 0, new[] { Side.East }));
    }

    [Fact]
    public void FillAll_FillsEveryUsedSlot()
    {
        var area = TestContent.CompileCrypt();
        var grid = SlotGrid.ForArea(area.Definition);
        var rng = Pcg32.ForAttempt("crypt", 1, 0);
        Assert.Null(LandmarkPlacer.PlaceAll(area, grid, rng));
        Assert.Null(IndoorRouter.Route(area, grid, rng));
        Assert.Null(RoomFiller.FillAll(area, grid, rng));
        for (var y = 0; y < grid.Rows; y++)
        {
            for (var x = 0; x < grid.Columns; x++)
            {
                Assert.True(!grid.IsUsed(x, y) || grid.BlockAt(x, y) != -1, $"slot ({x}, {y}) is used but empty");
            }
        }
    }

    [Fact]
    public void FillAll_UsesDeadEndsForSingleOpenings()
    {
        var area = TestContent.CompileCrypt();
        var grid = new SlotGrid(2, 1, 8);
        grid.Place(new Placement(area.RoomsTagged("entrance")[0], 0, 0, "entrance"));
        grid.MarkUsed(1, 0);
        grid.Open(0, 0, Side.East);
        Assert.Null(RoomFiller.FillAll(area, grid, new Pcg32(1, 1)));
        Assert.Equal("alcove", grid.Placements[1].Room.Id);
    }
}
