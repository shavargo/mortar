using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Indoor;

internal static class RoomFiller
{
    public static string? FillAll(CompiledArea area, SlotGrid grid, Pcg32 rng)
    {
        var fillers = area.Rooms.Where(r => r.HasTag(StandardTags.Filler) || r.HasTag(StandardTags.Corridor)).ToList();
        var deadEnds = area.Rooms.Where(r => r.HasTag(StandardTags.DeadEnd)).ToList();
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

    public static bool Fits(SlotGrid grid, Room room, int x, int y) =>
        CoversEmptyUsedSlots(grid, room, x, y)
        && OuterOpenings(grid, room, x, y).All(opening => HasMatchingDoor(grid, new Placement(room, x, y, null), opening));

    private static Room? Choose(SlotGrid grid, int x, int y, List<Room> fillers, List<Room> deadEnds, Pcg32 rng)
    {
        var endings = deadEnds.Where(r => Fits(grid, r, x, y) && OuterOpenings(grid, r, x, y).Count == 1).ToList();
        if (endings.Count > 0) return RoomPicker.Pick(endings, rng);
        var candidates = fillers.Where(r => Fits(grid, r, x, y)).ToList();
        return candidates.Count == 0 ? null : RoomPicker.Pick(candidates, rng);
    }

    private static bool CoversEmptyUsedSlots(SlotGrid grid, Room room, int originX, int originY)
    {
        for (var y = originY; y < originY + room.SlotsHigh; y++)
        {
            for (var x = originX; x < originX + room.SlotsWide; x++)
            {
                if (!grid.InBounds(x, y) || !grid.IsUsed(x, y) || grid.BlockAt(x, y) != -1) return false;
            }
        }
        return true;
    }

    private static List<(int X, int Y, Side Side)> OuterOpenings(SlotGrid grid, Room room, int originX, int originY)
    {
        var footprint = new Placement(room, originX, originY, null);
        var openings = new List<(int X, int Y, Side Side)>();
        for (var y = originY; y < originY + room.SlotsHigh; y++)
        {
            for (var x = originX; x < originX + room.SlotsWide; x++)
            {
                foreach (var side in Sides.All)
                {
                    var (dx, dy) = Sides.Offset(side);
                    if (!footprint.Contains(x + dx, y + dy) && grid.IsOpen(x, y, side)) openings.Add((x, y, side));
                }
            }
        }
        return openings;
    }

    private static bool HasMatchingDoor(SlotGrid grid, Placement candidate, (int X, int Y, Side Side) opening)
    {
        var segment = opening.Side == Side.North || opening.Side == Side.South
            ? opening.X - candidate.SlotX
            : opening.Y - candidate.SlotY;
        if (!candidate.Room.HasDoorInSegment(opening.Side, segment)) return false;
        var (dx, dy) = Sides.Offset(opening.Side);
        var neighbor = grid.BlockAt(opening.X + dx, opening.Y + dy);
        return neighbor == -1
            || DoorGeometry.DoorsMeetOn(candidate, opening.Side, segment, grid.Placements[neighbor], grid.Stride);
    }
}
