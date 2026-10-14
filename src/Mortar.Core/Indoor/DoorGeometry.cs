namespace Mortar.Core.Indoor;

internal static class DoorGeometry
{
    public static IEnumerable<(int X, int Y)> WorldDoors(Placement placement, int stride) =>
        placement.Room.Doors.Select(d => (placement.SlotX * stride + d.X, placement.SlotY * stride + d.Y));

    public static bool DoorsMeet(Placement a, Placement b, int stride)
    {
        var doorsOfB = new HashSet<(int X, int Y)>(WorldDoors(b, stride));
        return WorldDoors(a, stride).Any(doorsOfB.Contains);
    }
}
