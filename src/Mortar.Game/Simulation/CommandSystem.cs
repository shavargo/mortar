using Mortar.Game.Commands;
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class CommandSystem
{
    public static void Apply(World world, IReadOnlyList<Command> commands, EventSink events)
    {
        foreach (var command in commands)
        {
            var reason = Execute(world, command, events);
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
        world.Player.SetMoveIntent(move.Destination);
        return null;
    }
}
