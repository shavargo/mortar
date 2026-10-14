using Mortar.Game.Commands;
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class TickRunner
{
    public static IReadOnlyList<GameEvent> Run(World world, IReadOnlyList<Command> commands)
    {
        world.TickCount++;
        var events = new EventSink(world.TickCount);
        if (world.IsGameOver)
        {
            foreach (var command in commands) events.Reject(command, "the player is dead");
            return events.Events;
        }
        CommandSystem.Apply(world, commands, events);
        var due = ActionSystem.Advance(world, events);
        CombatSystem.Resolve(world, due, events);
        DeathSystem.Process(world, events);
        return events.Events;
    }
}
