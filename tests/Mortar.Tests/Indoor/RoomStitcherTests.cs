using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class RoomStitcherTests
{
    private static readonly Tileset Tiles = TestContent.CreateTileset();
    private static readonly Room Cross = TestContent.CompileRoom("cross", new[] { "filler" }, TestContent.Box(9, 9, new[] { 4 }));

    private static SlotGrid TwoCrosses()
    {
        var grid = new SlotGrid(2, 1, 8);
        grid.Place(new Placement(Cross, 0, 0, null));
        grid.Place(new Placement(Cross, 1, 0, null));
        return grid;
    }

    [Fact]
    public void Stitch_OpensSharedDoorWhenConnected()
    {
        var grid = TwoCrosses();
        grid.Open(0, 0, Side.East);
        var cells = RoomStitcher.Stitch(grid, Tiles);
        Assert.Equal((17, 9), (cells.Width, cells.Height));
        Assert.Equal(Tiles.FloorIndex, cells[8, 4]);
        Assert.Equal(Tiles.WallIndex, cells[0, 4]);
        Assert.Equal(Tiles.WallIndex, cells[4, 0]);
        Assert.Equal(Tiles.FloorIndex, cells[4, 4]);
    }

    [Fact]
    public void Stitch_KeepsSharedDoorClosedWithoutOpening()
    {
        var cells = RoomStitcher.Stitch(TwoCrosses(), Tiles);
        Assert.Equal(Tiles.WallIndex, cells[8, 4]);
    }

    [Fact]
    public void Stitch_LeavesUnusedSlotsVoid()
    {
        var grid = new SlotGrid(2, 1, 8);
        grid.Place(new Placement(Cross, 0, 0, null));
        Assert.Equal(Tiles.VoidIndex, RoomStitcher.Stitch(grid, Tiles)[12, 4]);
    }
}
