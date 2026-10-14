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
    private static readonly Room Nave = TestContent.CompileRoom("nave", new[] { "filler" }, TestContent.Box(17, 9, new[] { 4, 12 }));
    private static readonly Room HalfNave = TestContent.CompileRoom("half", new[] { "filler" }, TestContent.Box(17, 9, new[] { 4 }));
    private static readonly Room SealedNave = TestContent.CompileRoom("sealed", new[] { "filler" }, TestContent.Box(17, 9, Array.Empty<int>(), (4, 0, 'D')));

    private static SlotGrid UsedGrid(int columns, int rows)
    {
        var grid = new SlotGrid(columns, rows, 8);
        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                grid.MarkUsed(x, y);
            }
        }
        return grid;
    }

    [Fact]
    public void Fits_CrossRoomCoversAnyOpenings()
    {
        var grid = UsedGrid(3, 3);
        foreach (var side in Sides.All) grid.Open(1, 1, side);
        Assert.True(RoomFiller.Fits(grid, Cross, 1, 1));
    }

    [Fact]
    public void Fits_RejectsRoomWithoutDoorOnOpenSide()
    {
        var grid = UsedGrid(3, 3);
        grid.Open(1, 1, Side.East);
        Assert.False(RoomFiller.Fits(grid, NorthOnly, 1, 1));
    }

    [Fact]
    public void Fits_RequiresDoorsToLineUpWithPlacedNeighbors()
    {
        var grid = UsedGrid(2, 1);
        grid.Place(new Placement(OffCenter, 1, 0, null));
        grid.Open(0, 0, Side.East);
        Assert.False(RoomFiller.Fits(grid, Cross, 0, 0));
        Assert.True(RoomFiller.Fits(grid, OffCenter, 0, 0));
    }

    [Fact]
    public void Fits_MultiSlotRoomNeedsEveryCoveredSlotUsedAndEmpty()
    {
        var grid = new SlotGrid(3, 1, 8);
        grid.MarkUsed(0, 0);
        Assert.False(RoomFiller.Fits(grid, Nave, 0, 0));
        grid.MarkUsed(1, 0);
        Assert.True(RoomFiller.Fits(grid, Nave, 0, 0));
        grid.Place(new Placement(Cross, 1, 0, null));
        Assert.False(RoomFiller.Fits(grid, Nave, 0, 0));
        Assert.False(RoomFiller.Fits(grid, Nave, 2, 0));
    }

    [Fact]
    public void Fits_MultiSlotRoomNeedsADoorOnEachOuterOpening()
    {
        var grid = UsedGrid(2, 2);
        grid.Open(1, 0, Side.South);
        Assert.False(RoomFiller.Fits(grid, HalfNave, 0, 0));
        Assert.True(RoomFiller.Fits(grid, Nave, 0, 0));
    }

    [Fact]
    public void Fits_IgnoresOpeningsInsideTheRoom()
    {
        var grid = UsedGrid(2, 1);
        grid.Open(0, 0, Side.East);
        Assert.True(RoomFiller.Fits(grid, SealedNave, 0, 0));
    }

    [Fact]
    public void Fits_MultiSlotDoorsMustMeetPlacedNeighbors()
    {
        var misaligned = UsedGrid(3, 1);
        misaligned.Place(new Placement(OffCenter, 2, 0, null));
        misaligned.Open(1, 0, Side.East);
        Assert.False(RoomFiller.Fits(misaligned, Nave, 0, 0));

        var aligned = UsedGrid(3, 1);
        aligned.Place(new Placement(Cross, 2, 0, null));
        aligned.Open(1, 0, Side.East);
        Assert.True(RoomFiller.Fits(aligned, Nave, 0, 0));
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

    [Fact]
    public void FillAll_PlacesAMultiSlotFillerAcrossBothSlots()
    {
        var rooms = TestContent.CryptRooms().Where(r => r.Id != "hall" && r.Id != "alcove").ToList();
        rooms.Add(TestContent.RoomDef("stub", new[] { "filler" }, TestContent.Box(9, 9, Array.Empty<int>(), (4, 0, 'D'))));
        rooms.Add(TestContent.RoomDef("nave", new[] { "filler" }, TestContent.Box(17, 9, new[] { 4, 12 })));
        var area = TestContent.CompileCrypt(rooms: rooms);
        var grid = UsedGrid(2, 1);
        grid.Open(0, 0, Side.East);
        Assert.Null(RoomFiller.FillAll(area, grid, new Pcg32(1, 1)));
        var placement = Assert.Single(grid.Placements);
        Assert.Equal(("nave", 0, 0), (placement.Room.Id, placement.SlotX, placement.SlotY));
        Assert.Equal(0, grid.BlockAt(1, 0));
    }

    [Fact]
    public void FillAll_MultiSlotDeadEndCountsItsOuterOpenings()
    {
        var rooms = TestContent.CryptRooms().Where(r => r.Id != "alcove").ToList();
        rooms.Add(TestContent.RoomDef("long-end", new[] { "dead_end" }, TestContent.Box(17, 9, new[] { 4, 12 })));
        var area = TestContent.CompileCrypt(rooms: rooms);
        var grid = UsedGrid(3, 1);
        grid.Place(new Placement(area.RoomsTagged("entrance")[0], 0, 0, "entrance"));
        grid.Open(0, 0, Side.East);
        Assert.Null(RoomFiller.FillAll(area, grid, new Pcg32(1, 1)));
        Assert.Equal("long-end", grid.Placements[1].Room.Id);
    }
}
