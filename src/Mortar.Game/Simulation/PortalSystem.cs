using Mortar.Core.Model;
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class PortalSystem
{
    public static string? Open(World world, EventSink events)
    {
        if (world.Content.ShallowestLevel > 0)
        {
            return "there is no town";
        }
        if (world.Depth <= 0)
        {
            return "cannot open a portal in town";
        }
        var town = LevelEntry.Ensure(world, 0, out var townError);
        if (town is null) return townError;
        var townCell = TownAnchor(town);
        if (townCell is null) return "town has no portal anchor";
        world.Portal = new PortalPair(world.Depth, world.Player.Cell, townCell.Value);
        events.Add(new PortalOpened(events.Tick, world.Depth, world.Player.Cell, townCell.Value));
        return null;
    }

    public static string? Use(World world, EventSink events)
    {
        var portal = world.Portal;
        if (portal is null) return "no portal is open";
        if (world.Depth == portal.DungeonDepth && world.Player.Cell.DistanceTo(portal.DungeonCell) <= 1)
        {
            return Warp(world, 0, portal.TownCell, events);
        }
        if (world.Depth == 0 && world.Player.Cell.DistanceTo(portal.TownCell) <= 1)
        {
            return Warp(world, portal.DungeonDepth, portal.DungeonCell, events);
        }
        return "no portal within reach";
    }

    private static string? Warp(World world, int depth, Cell arrival, EventSink events)
    {
        var level = LevelEntry.Ensure(world, depth, out var error);
        if (level is null) return error;
        var from = world.Depth;
        if (from != depth)
        {
            var current = world.Level;
            current.Remove(world.Player);
            current.ClearProjectiles();
            world.Player.ClearAction();
            world.Player.ClearIntent();
            var cell = PlaceAt(level, arrival);
            if (cell is null) return "portal arrival is blocked";
            level.Add(world.Player, cell.Value);
            world.Depth = depth;
        }
        else if (world.Player.Cell != arrival)
        {
            var cell = PlaceAt(level, arrival);
            if (cell is not null && cell.Value != world.Player.Cell && level.IsFree(cell.Value))
            {
                level.Move(world.Player, cell.Value);
            }
        }
        events.Add(new PortalUsed(events.Tick, from, world.Depth, world.Player.Cell));
        events.Add(new LevelChanged(events.Tick, from, world.Depth, world.Player.Cell));
        return null;
    }

    private static Cell? PlaceAt(LevelState level, Cell arrival) =>
        level.IsFree(arrival) ? arrival : level.NearestFree(arrival, LevelEntry.ArrivalSearchRadius);

    private static Cell? TownAnchor(LevelState town)
    {
        foreach (var kind in new[] { MarkerKinds.Waypoint, MarkerKinds.Entrance, MarkerKinds.Exit })
        {
            var marker = town.FindMarker(kind);
            if (marker is not null)
            {
                return town.NearestFree(marker.Value, LevelEntry.ArrivalSearchRadius) ?? marker;
            }
        }
        return null;
    }
}
