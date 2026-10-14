using Mortar.Core.Model;
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class Stairs
{
    public static string? Use(World world, EventSink events)
    {
        var level = world.Level;
        var cell = world.Player.Cell;
        if (IsNear(level, cell, MarkerKinds.Exit))
        {
            return Travel(world, world.Depth + 1, MarkerKinds.Entrance, events);
        }
        if (IsNear(level, cell, MarkerKinds.Entrance))
        {
            return Travel(world, world.Depth - 1, MarkerKinds.Exit, events);
        }
        return "no stairs within reach";
    }

    private static bool IsNear(LevelState level, Cell cell, string kind)
    {
        foreach (var marker in level.Layout.Markers)
        {
            if (marker.Kind == kind && cell.DistanceTo(new Cell(marker.X, marker.Y)) <= 1) return true;
        }
        return false;
    }

    private static string? Travel(World world, int depth, string arrivalMarker, EventSink events)
    {
        if (depth < 1)
        {
            return "there is no level above";
        }
        if (depth > world.Content.DeepestLevel)
        {
            return "there is no deeper level";
        }
        var from = world.Depth;
        var error = LevelEntry.Enter(world, depth, arrivalMarker);
        if (error is not null)
        {
            return error;
        }
        events.Add(new LevelChanged(events.Tick, from, depth, world.Player.Cell));
        return null;
    }
}
