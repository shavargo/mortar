using Mortar.Game.Events;
using Mortar.Game.Pathing;

namespace Mortar.Game.Simulation;

internal static class Movement
{
    public static void ContinueMoveTo(LevelState level, Actor actor, EventSink events)
    {
        if (actor.Cell == actor.IntentCell)
        {
            actor.ClearIntent();
            return;
        }
        var path = Pathfinder.FindPath(actor.Cell, actor.IntentCell, level.IsWalkable, level.IsFree);
        if (path is null)
        {
            actor.ClearIntent();
            return;
        }
        Step(level, actor, path[0], events);
        if (actor.Cell == actor.IntentCell) actor.ClearIntent();
    }

    public static void Step(LevelState level, Actor actor, Cell next, EventSink events)
    {
        var from = actor.Cell;
        var duration = actor.Profile.Timings.MoveTicks;
        actor.Facing = Directions.Toward(from, next);
        level.Move(actor, next);
        actor.StartAction(ActionKind.Moving, duration);
        events.Add(new ActorMoved(events.Tick, actor.Id, from, next, duration));
    }
}
