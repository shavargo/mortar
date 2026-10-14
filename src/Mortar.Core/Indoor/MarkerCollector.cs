using Mortar.Core.Layouts;
using Mortar.Core.Model;

namespace Mortar.Core.Indoor;

internal static class MarkerCollector
{
    public static List<Marker> Collect(CompiledArea area, SlotGrid grid)
    {
        var markers = new List<Marker>();
        foreach (var placement in grid.Placements)
        {
            var node = placement.NodeId is null ? null : area.Node(placement.NodeId);
            AddRoomMarkers(placement, node, grid.Stride, markers);
        }
        return markers;
    }

    private static void AddRoomMarkers(Placement placement, GraphNode? node, int stride, List<Marker> markers)
    {
        var originX = placement.SlotX * stride;
        var originY = placement.SlotY * stride;
        var landmarkFound = false;
        foreach (var marker in placement.Room.Markers)
        {
            var isLandmark = node is not null && !landmarkFound && marker.Kind == node.Kind;
            landmarkFound |= isLandmark;
            markers.Add(new Marker(marker.Kind, isLandmark ? node!.Id : null, originX + marker.X, originY + marker.Y));
        }
        if (node is not null && !landmarkFound)
        {
            var room = placement.Room;
            markers.Add(new Marker(node.Kind, node.Id, originX + room.Width / 2, originY + room.Height / 2));
        }
    }
}
