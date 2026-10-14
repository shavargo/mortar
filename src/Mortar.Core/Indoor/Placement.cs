using Mortar.Core.Model;

namespace Mortar.Core.Indoor;

internal sealed class Placement
{
    public Placement(Room room, int slotX, int slotY, string? nodeId)
    {
        Room = room;
        SlotX = slotX;
        SlotY = slotY;
        NodeId = nodeId;
    }

    public Room Room { get; }

    public int SlotX { get; }

    public int SlotY { get; }

    public string? NodeId { get; }

    public bool Contains(int x, int y) =>
        x >= SlotX && x < SlotX + Room.SlotsWide && y >= SlotY && y < SlotY + Room.SlotsHigh;
}
