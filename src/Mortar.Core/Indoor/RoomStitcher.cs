using Mortar.Core.Layouts;
using Mortar.Core.Model;

namespace Mortar.Core.Indoor;

internal static class RoomStitcher
{
    public static CellGrid Stitch(SlotGrid grid, Tileset tileset)
    {
        var connected = ConnectedDoors(grid);
        var canvas = new StitchCanvas(grid.CellWidth, grid.CellHeight, tileset);
        foreach (var placement in grid.Placements)
        {
            Draw(placement, grid.Stride, connected, canvas, tileset);
        }
        return canvas.Cells;
    }

    private static void Draw(
        Placement placement,
        int stride,
        HashSet<(int X, int Y)> connected,
        StitchCanvas canvas,
        Tileset tileset)
    {
        var room = placement.Room;
        for (var ly = 0; ly < room.Height; ly++)
        {
            for (var lx = 0; lx < room.Width; lx++)
            {
                var x = placement.SlotX * stride + lx;
                var y = placement.SlotY * stride + ly;
                if (!room.HasDoorAt(lx, ly)) canvas.Merge(x, y, room.GroundAt(lx, ly));
                else if (connected.Contains((x, y))) canvas.ForceFloor(x, y);
                else canvas.Merge(x, y, tileset.WallIndex);
            }
        }
    }

    private static HashSet<(int X, int Y)> ConnectedDoors(SlotGrid grid)
    {
        var seen = new HashSet<(int X, int Y)>();
        var connected = new HashSet<(int X, int Y)>();
        foreach (var placement in grid.Placements)
        {
            foreach (var door in DoorGeometry.WorldDoors(placement, grid.Stride))
            {
                if (!seen.Add(door) && IsOpenBoundary(grid, door.X, door.Y))
                {
                    connected.Add(door);
                }
            }
        }
        return connected;
    }

    private static bool IsOpenBoundary(SlotGrid grid, int x, int y)
    {
        var stride = grid.Stride;
        if (x % stride == 0 && y % stride != 0)
        {
            var column = x / stride;
            return column > 0 && column < grid.Columns && grid.IsOpen(column - 1, y / stride, Side.East);
        }
        if (y % stride == 0 && x % stride != 0)
        {
            var row = y / stride;
            return row > 0 && row < grid.Rows && grid.IsOpen(x / stride, row - 1, Side.South);
        }
        return false;
    }
}
