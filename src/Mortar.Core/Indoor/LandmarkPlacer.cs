using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Indoor;

internal static class LandmarkPlacer
{
    public static string? PlaceAll(CompiledArea area, SlotGrid grid, Pcg32 rng)
    {
        foreach (var node in area.Definition.Nodes)
        {
            if (!Place(node, area, grid, rng))
            {
                return "landmark placement failed";
            }
        }
        return null;
    }

    public static int PickSlot(double from, double to, int count, Pcg32 rng)
    {
        var low = Math.Clamp((int)Math.Floor(from * count), 0, count - 1);
        var high = Math.Clamp((int)Math.Ceiling(to * count) - 1, low, count - 1);
        return rng.NextInt(low, high + 1);
    }

    private static bool Place(GraphNode node, CompiledArea area, SlotGrid grid, Pcg32 rng)
    {
        var slotX = PickSlot(node.Region.X0, node.Region.X1, grid.Columns, rng);
        var slotY = PickSlot(node.Region.Y0, node.Region.Y1, grid.Rows, rng);
        var room = RoomPicker.Pick(area.RoomsTagged(node.Kind), rng);
        var origins = new List<(int X, int Y)>();
        for (var y = slotY - room.SlotsHigh + 1; y <= slotY; y++)
        {
            for (var x = slotX - room.SlotsWide + 1; x <= slotX; x++)
            {
                if (grid.CanPlace(room, x, y)) origins.Add((x, y));
            }
        }
        if (origins.Count == 0)
        {
            return false;
        }
        var origin = rng.Pick(origins);
        grid.Place(new Placement(room, origin.X, origin.Y, node.Id));
        return true;
    }
}
