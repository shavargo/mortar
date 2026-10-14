using Mortar.Game.Events;
using Mortar.Game.Pathing;

namespace Mortar.Game.Simulation;

internal static class Movement
{
    public static void ContinueMoveTo(LevelState level, Actor actor, EventSink events)
    {
        var goal = actor.IntentCell;
        var occupied = IsOccupiedByOther(level, actor, goal);
        if (actor.Cell == goal || (occupied && actor.Cell.IsAdjacentTo(goal)))
        {
            actor.ClearIntent();
            return;
        }
        var path = occupied
            ? Pathfinder.FindPathNextTo(actor.Cell, goal, level.IsWalkable, level.IsFree)
            : Pathfinder.FindPath(actor.Cell, goal, level.IsWalkable, level.IsFree);
        if (path is null)
        {
            if (!IsReachable(level, actor.Cell, goal)) actor.ClearIntent();
            return;
        }
        Step(level, actor, path[0], events);
        if (actor.Cell == goal) actor.ClearIntent();
    }

    public static bool IsReachable(LevelState level, Cell from, Cell to) =>
        from == to || Pathfinder.FindPath(from, to, level.IsWalkable, level.IsWalkable) is not null;

    public static void Step(LevelState level, Actor actor, Cell next, EventSink events)
    {
        var from = actor.Cell;
        var duration = actor.Profile.Timings.MoveTicks;
        actor.Facing = Directions.Toward(from, next);
        level.Move(actor, next);
        actor.StartAction(ActionKind.Moving, duration);
        events.Add(new ActorMoved(events.Tick, actor.Id, from, next, duration));
    }

    private static bool IsOccupiedByOther(LevelState level, Actor actor, Cell cell)
    {
        var occupant = level.OccupantAt(cell);
        return occupant != 0 && occupant != actor.Id;
    }
}
