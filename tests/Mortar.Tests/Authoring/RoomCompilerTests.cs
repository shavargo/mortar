using Mortar.Core.Authoring;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Authoring;

public class RoomCompilerTests
{
    private static readonly Tileset Tiles = TestContent.CreateTileset();

    private static AuthoringResult<Room> Compile(string[] grid, params string[] tags) =>
        RoomCompiler.Compile(TestContent.RoomDef("room_a", tags, grid), Tiles, 9);

    private static void AssertError(AuthoringResult<Room> result, string expected)
    {
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains(expected) && e.Source == "room_a.json");
    }

    [Fact]
    public void Compile_CrossRoom_FindsFourDoorsOnSegmentZero()
    {
        var room = Compile(TestContent.Box(9, 9, new[] { 4 })).Value!;
        Assert.Equal(new[] { Side.North, Side.West, Side.East, Side.South }, room.Doors.Select(d => d.Side));
        Assert.All(room.Doors, d => Assert.Equal(0, d.Segment));
        Assert.True(room.IsSingleSlot);
        Assert.Equal(Tiles.WallIndex, room.GroundAt(4, 0));
        Assert.Equal(Tiles.FloorIndex, room.GroundAt(4, 4));
    }

    [Fact]
    public void Compile_TwoSlotRoom_AssignsDoorSegments()
    {
        var room = Compile(TestContent.Box(17, 9, new[] { 4, 12 })).Value!;
        Assert.Equal(2, room.SlotsWide);
        Assert.Equal(1, room.SlotsHigh);
        Assert.True(room.HasDoorInSegment(Side.North, 0));
        Assert.True(room.HasDoorInSegment(Side.North, 1));
        Assert.True(room.HasDoorInSegment(Side.East, 0));
    }

    [Fact]
    public void Compile_RecordsMarkersOnFloor()
    {
        var room = Compile(TestContent.Box(9, 9, new[] { 4 }, (4, 4, 'E')), "entrance").Value!;
        var marker = Assert.Single(room.Markers);
        Assert.Equal((MarkerKinds.Entrance, 4, 4), (marker.Kind, marker.X, marker.Y));
        Assert.Equal(Tiles.FloorIndex, room.GroundAt(4, 4));
    }

    [Fact]
    public void Compile_SizeOffSlotGrid_IsAnError() =>
        AssertError(Compile(TestContent.Box(8, 8, new[] { 4 })), "(slotSize - 1) x n + 1");

    [Fact]
    public void Compile_UnevenRows_IsAnError() =>
        AssertError(Compile(new[] { "####D####", "#..#" }), "row 1 has length 4");

    [Fact]
    public void Compile_UnknownChar_IsAnError() =>
        AssertError(Compile(TestContent.Box(9, 9, new[] { 4 }, (2, 2, '?'))), "unknown ground char '?' at (2, 2)");

    [Fact]
    public void Compile_InteriorDoor_IsAnError() =>
        AssertError(Compile(TestContent.Box(9, 9, new[] { 4 }, (3, 3, 'D'))), "not on its outer edge");

    [Fact]
    public void Compile_DoorOnSlotCorner_IsAnError() =>
        AssertError(Compile(TestContent.Box(17, 9, new[] { 4 }, (8, 0, 'D'))), "on a slot corner");

    [Fact]
    public void Compile_NoDoors_IsAnError() =>
        AssertError(Compile(TestContent.Box(9, 9, Array.Empty<int>())), "has no doors");

    [Fact]
    public void Compile_EntranceTagWithoutMarker_IsAnError() =>
        AssertError(Compile(TestContent.Box(9, 9, new[] { 4 }), "entrance"), "tagged 'entrance' but has no entrance marker");

    [Fact]
    public void Compile_MarkerOnEdge_IsAnError() =>
        AssertError(Compile(TestContent.Box(9, 9, new[] { 4 }, (0, 2, 'S'))), "on its edge");

    [Fact]
    public void Compile_ReportsEveryProblem()
    {
        var result = Compile(TestContent.Box(9, 9, new[] { 4 }, (2, 2, '?'), (3, 3, 'D')));
        Assert.Equal(2, result.Errors.Count);
    }
}
