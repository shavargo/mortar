using Mortar.Core.Authoring;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Authoring;

public class AreaCompilerTests
{
    private static AuthoringResult<CompiledArea> Compile(AreaDefinition area, List<RoomDefinition>? rooms = null) =>
        AreaCompiler.Compile(area, TestContent.CreateTileset(), rooms ?? TestContent.CryptRooms());

    private static void AssertError(AuthoringResult<CompiledArea> result, string expected)
    {
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains(expected));
    }

    [Fact]
    public void Compile_CryptArea_SortsRoomsById()
    {
        var area = TestContent.CompileCrypt();
        Assert.Equal(
            new[] { "alcove", "boss", "entrance", "exit", "hall", "waypoint" },
            area.Rooms.Select(r => r.Id));
        Assert.Equal("boss", Assert.Single(area.RoomsTagged("boss")).Id);
    }

    [Fact]
    public void Compile_OutdoorMode_IsNotSupportedYet() =>
        AssertError(Compile(TestContent.CryptDefinition(mode: AreaMode.Outdoor)), "outdoor areas are not supported yet");

    [Fact]
    public void Compile_EdgeToUnknownNode_IsAnError()
    {
        var edges = TestContent.CryptEdges().Append(new GraphEdge("entrance", "ghost", EdgeKind.Path)).ToArray();
        AssertError(Compile(TestContent.CryptDefinition(edges: edges)), "unknown node 'ghost'");
    }

    [Fact]
    public void Compile_NoEntranceNode_IsAnError()
    {
        var nodes = new[] { new GraphNode("boss", "boss", new FractionRect(0, 1, 0, 1)) };
        var area = TestContent.CryptDefinition(nodes: nodes, edges: Array.Empty<GraphEdge>());
        AssertError(Compile(area), "exactly one entrance node, found 0");
    }

    [Fact]
    public void Compile_InvalidRegion_IsAnError()
    {
        var nodes = TestContent.CryptNodes();
        nodes[1] = new GraphNode("waypoint", "waypoint", new FractionRect(0.5, 0.2, 0, 1));
        AssertError(Compile(TestContent.CryptDefinition(nodes: nodes)), "node 'waypoint' region must lie within 0..1");
    }

    [Fact]
    public void Compile_NodeWithoutPathToEntrance_IsAnError()
    {
        var edges = new[] { new GraphEdge("entrance", "waypoint", EdgeKind.Path), new GraphEdge("entrance", "boss", EdgeKind.Branch) };
        AssertError(Compile(TestContent.CryptDefinition(edges: edges)), "node 'boss' is not connected to the entrance by path edges");
    }

    [Fact]
    public void Compile_TagNoRoomProvides_IsAnError()
    {
        var rooms = TestContent.CryptRooms().Where(r => r.Id != "boss").ToList();
        AssertError(Compile(TestContent.CryptDefinition(), rooms), "no room provides tag 'boss' needed by node 'boss'");
    }

    [Fact]
    public void Compile_RoomTagsFilterRemovesFillers_IsAnError()
    {
        var area = TestContent.CryptDefinition(roomTags: new[] { "entrance", "exit", "waypoint", "boss" });
        AssertError(Compile(area), "no single-slot room is tagged 'filler' or 'corridor'");
    }

    [Fact]
    public void Compile_DuplicateRoomIds_IsAnError()
    {
        var rooms = TestContent.CryptRooms();
        rooms.Add(TestContent.RoomDef("hall", new[] { "filler" }, TestContent.Box(9, 9, new[] { 4 })));
        AssertError(Compile(TestContent.CryptDefinition(), rooms), "more than one room has id 'hall'");
    }

    [Fact]
    public void Compile_RoomErrors_NameTheRoomFile()
    {
        var rooms = TestContent.CryptRooms();
        rooms.Add(TestContent.RoomDef("broken", new[] { "filler" }, TestContent.Box(8, 8, new[] { 4 })));
        var result = Compile(TestContent.CryptDefinition(), rooms);
        Assert.Contains(result.Errors, e => e.Source == "broken.json");
    }
}
