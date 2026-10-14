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
        ActionSystem.Advance(world, events);
        return events.Events;
    }
}
