using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class MarkerCollectorTests
{
    [Fact]
    public void Collect_TagsLandmarkMarkersWithNodeId()
    {
        var area = TestContent.CompileCrypt();
        var grid = SlotGrid.ForArea(area.Definition);
        grid.Place(new Placement(area.RoomsTagged("entrance")[0], 0, 3, "entrance"));
        grid.Place(new Placement(area.RoomsTagged("filler")[0], 1, 3, null));
        var markers = MarkerCollector.Collect(area, grid);
        Assert.Equal(2, markers.Count);
        Assert.Equal(("entrance", (string?)"entrance", 4, 28), (markers[0].Kind, markers[0].NodeId, markers[0].X, markers[0].Y));
        Assert.Equal(("spawn", (string?)null, 12, 28), (markers[1].Kind, markers[1].NodeId, markers[1].X, markers[1].Y));
    }

    [Fact]
    public void Collect_UsesRoomCenterForCustomLandmarks()
    {
        var nodes = TestContent.CryptNodes().Append(new GraphNode("shrine", "shrine", new FractionRect(0.4, 0.6, 0.4, 0.6))).ToArray();
        var edges = TestContent.CryptEdges().Append(new GraphEdge("entrance", "shrine", EdgeKind.Path)).ToArray();
        var rooms = TestContent.CryptRooms();
        rooms.Add(TestContent.RoomDef("shrine_room", new[] { "shrine" }, TestContent.Box(9, 9, new[] { 4 })));
        var area = TestContent.CompileCrypt(TestContent.CryptDefinition(nodes: nodes, edges: edges), rooms);
        var grid = SlotGrid.ForArea(area.Definition);
        grid.Place(new Placement(area.RoomsTagged("shrine")[0], 2, 2, "shrine"));
        var marker = Assert.Single(MarkerCollector.Collect(area, grid));
        Assert.Equal(("shrine", (string?)"shrine", 20, 20), (marker.Kind, marker.NodeId, marker.X, marker.Y));
    }
}
