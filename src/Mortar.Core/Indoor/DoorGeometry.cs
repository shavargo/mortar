using Mortar.Core.Model;

namespace Mortar.Core.Indoor;

internal static class DoorGeometry
{
    public static IEnumerable<(int X, int Y)> WorldDoors(Placement placement, int stride) =>
        placement.Room.Doors.Select(d => (placement.SlotX * stride + d.X, placement.SlotY * stride + d.Y));

    public static bool DoorsMeetOn(Placement placement, Side side, int segment, Placement neighbor, int stride)
    {
        var neighborDoors = new HashSet<(int X, int Y)>(WorldDoors(neighbor, stride));
        return placement.Room.Doors
            .Where(door => door.Side == side && door.Segment == segment)
            .Any(door => neighborDoors.Contains((placement.SlotX * stride + door.X, placement.SlotY * stride + door.Y)));
    }
}
