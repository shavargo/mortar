using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Indoor;

internal static class RoomFiller
{
    public static string? FillAll(CompiledArea area, SlotGrid grid, Pcg32 rng)
    {
        var fillers = area.Rooms
            .Where(r => r.IsSingleSlot && (r.HasTag(StandardTags.Filler) || r.HasTag(StandardTags.Corridor)))
            .ToList();
        var deadEnds = area.Rooms.Where(r => r.IsSingleSlot && r.HasTag(StandardTags.DeadEnd)).ToList();
        for (var y = 0; y < grid.Rows; y++)
        {
            for (var x = 0; x < grid.Columns; x++)
            {
                if (!grid.IsUsed(x, y) || grid.BlockAt(x, y) != -1) continue;
                var room = Choose(grid, x, y, fillers, deadEnds, rng);
                if (room is null) return "no room fits";
                grid.Place(new Placement(room, x, y, null));
            }
        }
        return null;
    }

    public static bool Fits(SlotGrid grid, Room room, int x, int y, IReadOnlyList<Side> open)
    {
        var candidate = new Placement(room, x, y, null);
        foreach (var side in open)
        {
            if (!room.HasDoorInSegment(side, 0)) return false;
            var (dx, dy) = Sides.Offset(side);
            var neighbor = grid.BlockAt(x + dx, y + dy);
            if (neighbor != -1 && !DoorGeometry.DoorsMeet(candidate, grid.Placements[neighbor], grid.Stride)) return false;
        }
        return true;
    }

    private static Room? Choose(SlotGrid grid, int x, int y, List<Room> fillers, List<Room> deadEnds, Pcg32 rng)
    {
        var open = Sides.All.Where(side => grid.IsOpen(x, y, side)).ToList();
        if (open.Count == 1)
        {
            var endings = deadEnds.Where(r => Fits(grid, r, x, y, open)).ToList();
            if (endings.Count > 0) return rng.Pick(endings);
        }
        var candidates = fillers.Where(r => Fits(grid, r, x, y, open)).ToList();
        return candidates.Count == 0 ? null : rng.Pick(candidates);
    }
}
