using Mortar.Game.Events;

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
        if (actor.Intent == IntentKind.MoveTo) Movement.ContinueMoveTo(level, actor, events);
    }
}
