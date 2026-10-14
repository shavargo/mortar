using Mortar.Game.Commands;
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class CommandSystem
{
    public static void Apply(World world, IReadOnlyList<Command> commands, EventSink events)
    {
        foreach (var command in commands)
        {
            var reason = command is null ? CommandRejected.MissingCommand : Execute(world, command, events);
            if (reason is not null) events.Reject(command, reason);
        }
    }

    private static string? Execute(World world, Command command, EventSink events)
    {
        switch (command)
        {
            case MoveCommand move:
                return Move(world, move);
            case AttackCommand attack:
                return Attack(world, attack);
            case SpendStatPointCommand spend:
                return PlayerGrowth.SpendPoint(world, spend.Stat, events);
            case UseStairsCommand _:
                return Stairs.Use(world, events);
            case PickUpCommand pickUp:
                return PickupSystem.Start(world, pickUp);
            case EquipCommand equip:
                return EquipActions.Equip(world, equip.ItemId, events);
            case UnequipCommand unequip:
                return EquipActions.Unequip(world, unequip.Slot, events);
            case DropItemCommand drop:
                return InventoryActions.Drop(world, drop.ItemId, events);
            case MoveItemCommand moveItem:
                return InventoryActions.Move(world, moveItem.ItemId, moveItem.To, events);
            case PutInBeltCommand putInBelt:
                return InventoryActions.PutInBelt(world, putInBelt.ItemId, putInBelt.Slot, events);
            case UseBeltCommand useBelt:
                return InventoryActions.UseBelt(world, useBelt.Slot, events);
            default:
                return "unsupported command";
        }
    }

    private static string? Attack(World world, AttackCommand attack)
    {
        var target = world.Level.Find(attack.TargetId);
        if (target is null || target.Kind != ActorKind.Monster)
        {
            return "no such monster on this level";
        }
        if (!target.IsAlive)
        {
            return "target is already dead";
        }
        world.Player.SetAttackIntent(target.Id);
        return null;
    }

    private static string? Move(World world, MoveCommand move)
    {
        if (!world.Level.IsWalkable(move.Destination))
        {
            return "destination is not walkable";
        }
        if (!Movement.IsReachable(world.Level, world.Player.Cell, move.Destination))
        {
            return "destination is unreachable";
        }
        world.Player.SetMoveIntent(move.Destination);
        return null;
    }
}
