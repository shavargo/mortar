using Mortar.Game.Events;
using Mortar.Game.Pathing;

namespace Mortar.Game.Simulation;

internal static class ActionSystem
{
    public static List<Actor> Advance(World world, EventSink events)
    {
        var level = world.Level;
        var due = new List<Actor>();
        foreach (var actor in level.Actors)
        {
            if (actor.IsAlive) Progress(actor, due);
        }
        foreach (var actor in level.Actors)
        {
            if (actor.IsAlive && actor.Action == ActionKind.Idle) StartFromIntent(level, actor, events);
        }
        return due;
    }

    private static void Progress(Actor actor, List<Actor> due)
    {
        if (actor.Action == ActionKind.Idle) return;
        actor.ActionElapsed++;
        if (actor.Action == ActionKind.Attacking && actor.ActionElapsed == actor.Profile.Timings.HitFrame)
        {
            due.Add(actor);
        }
        if (actor.ActionElapsed >= actor.ActionDuration) actor.ClearAction();
    }

    private static void StartFromIntent(LevelState level, Actor actor, EventSink events)
    {
        switch (actor.Intent)
        {
            case IntentKind.MoveTo:
                Movement.ContinueMoveTo(level, actor, events);
                break;
            case IntentKind.Attack:
                ContinueAttack(level, actor, events);
                break;
        }
    }

    private static void ContinueAttack(LevelState level, Actor actor, EventSink events)
    {
        var target = level.Find(actor.IntentTargetId);
        if (target is null || !target.IsAlive)
        {
            actor.ClearIntent();
            return;
        }
        if (actor.Cell.IsAdjacentTo(target.Cell))
        {
            CombatSystem.StartAttack(actor, target, events);
            return;
        }
        var path = Pathfinder.FindPathNextTo(actor.Cell, target.Cell, level.IsWalkable, level.IsFree);
        if (path is { Count: > 0 }) Movement.Step(level, actor, path[0], events);
    }
}
