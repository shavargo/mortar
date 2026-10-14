using Mortar.Game.Commands;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Pathing;

namespace Mortar.Game.Simulation;

internal static class PickupSystem
{
    public const string InventoryFull = "inventory is full";

    public static string? Start(World world, PickUpCommand command)
    {
        var ground = world.Level.FindGroundItem(command.GroundItemId);
        if (ground is null) return "no such item on this level";
        if (!Movement.IsReachable(world.Level, world.Player.Cell, ground.Cell)) return "item is unreachable";
        world.Player.SetPickUpIntent(ground.Id, ground.Cell);
        return null;
    }

    public static void Process(World world, EventSink events)
    {
        var player = world.Player;
        if (!player.IsAlive || player.Intent != IntentKind.PickUp || player.Action != ActionKind.Idle) return;
        var level = world.Level;
        var ground = level.FindGroundItem(player.IntentTargetId);
        if (ground is null)
        {
            player.ClearIntent();
            return;
        }
        if (player.Cell != ground.Cell && !Reach.CanStrike(player.Cell, ground.Cell, level.IsWalkable)) return;
        player.ClearIntent();
        Take(world, ground, events);
    }

    private static void Take(World world, GroundItem ground, EventSink events)
    {
        if (ground.Item is null)
        {
            world.Belongings.Gold += ground.Gold;
            world.Level.RemoveGroundItem(ground);
            events.Add(new GoldPickedUp(events.Tick, ground.Gold, world.Belongings.Gold));
            return;
        }
        if (!world.Belongings.Stow(ground.Item))
        {
            events.Add(new PickupFailed(events.Tick, ground.Id, InventoryFull));
            return;
        }
        world.Level.RemoveGroundItem(ground);
        events.Add(new ItemPickedUp(events.Tick, ground.Item.Id, ground.Item.Name));
    }
}
