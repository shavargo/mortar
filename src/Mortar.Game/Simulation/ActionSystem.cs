using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;
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
            if (actor.IsAlive) Progress(world, actor, due);
        }
        foreach (var actor in level.Actors)
        {
            if (actor.IsAlive && actor.Action == ActionKind.Idle) StartFromIntent(world, actor, events);
        }
        return due;
    }

    private static void Progress(World world, Actor actor, List<Actor> due)
    {
        if (actor.Action == ActionKind.Idle) return;
        actor.ActionElapsed++;
        if (IsDue(world, actor)) due.Add(actor);
        if (actor.ActionElapsed >= actor.ActionDuration) actor.ClearAction();
    }

    private static bool IsDue(World world, Actor actor)
    {
        if (actor.Action == ActionKind.Casting)
        {
            return world.Content.TryGetSpell(actor.ActionSpellId, out var spell)
                && actor.ActionElapsed == spell!.HitFrame;
        }
        return IsStrikeAction(actor.Action) && actor.ActionElapsed == actor.Profile.Timings.HitFrame;
    }

    private static bool IsStrikeAction(ActionKind action) =>
        action == ActionKind.Attacking || action == ActionKind.Shooting;

    private static void StartFromIntent(World world, Actor actor, EventSink events)
    {
        switch (actor.Intent)
        {
            case IntentKind.MoveTo:
                Movement.ContinueMoveTo(world.Level, actor, events);
                break;
            case IntentKind.Attack:
                ContinueAttack(world, actor, events);
                break;
            case IntentKind.Cast:
                ContinueCast(world, actor, events);
                break;
            case IntentKind.PickUp:
                Movement.Approach(world.Level, actor, actor.IntentCell, events);
                break;
        }
    }

    private static void ContinueCast(World world, Actor actor, EventSink events)
    {
        if (!world.Content.TryGetSpell(actor.IntentSpellId, out var spell))
        {
            actor.ClearIntent();
            return;
        }
        var fromStaff = actor.IntentFromStaff;
        if (!fromStaff && actor.Mana < spell!.ManaCost)
        {
            actor.ClearIntent();
            return;
        }
        if (fromStaff)
        {
            var staff = CombatSystem.EquippedWeapon(world);
            if (staff is null || staff.Charges < 1 || staff.Definition.SpellId != spell!.Id)
            {
                actor.ClearIntent();
                return;
            }
        }
        SpellSystem.StartCast(
            world, actor, spell!, actor.IntentTargetId, actor.IntentCell, events, fromStaff, actor.IntentItemId);
        actor.ClearIntent();
    }

    private static void ContinueAttack(World world, Actor actor, EventSink events)
    {
        var level = world.Level;
        var target = level.Find(actor.IntentTargetId);
        if (target is null || !target.IsAlive)
        {
            actor.ClearIntent();
            return;
        }
        if (actor.Kind == ActorKind.Player && CombatSystem.IsRangedWeapon(CombatSystem.EquippedWeapon(world)))
        {
            ContinueRanged(world, actor, target, events);
            return;
        }
        if (actor.Kind == ActorKind.Monster && IsRangedKite(world, actor))
        {
            ContinueMonsterRanged(world, actor, target, events);
            return;
        }
        if (Reach.CanStrike(actor.Cell, target.Cell, level.IsWalkable))
        {
            CombatSystem.StartAttack(actor, target, events);
            return;
        }
        var path = Pathfinder.FindPathNextTo(actor.Cell, target.Cell, level.IsWalkable, level.IsFree);
        if (path is { Count: > 0 }) Movement.Step(level, actor, path[0], events);
    }

    private static bool IsRangedKite(World world, Actor actor) =>
        world.Content.Monster(actor.DefinitionId).Ai == AiKinds.RangedKite;

    private static void ContinueMonsterRanged(World world, Actor actor, Actor target, EventSink events)
    {
        var range = world.Content.Monster(actor.DefinitionId).PreferredRange;
        if (actor.Cell.DistanceTo(target.Cell) <= range)
        {
            CombatSystem.StartShoot(actor, target, events);
            return;
        }
        var path = Pathfinder.FindPathNextTo(actor.Cell, target.Cell, world.Level.IsWalkable, world.Level.IsFree);
        if (path is { Count: > 0 }) Movement.Step(world.Level, actor, path[0], events);
    }

    private static void ContinueRanged(World world, Actor actor, Actor target, EventSink events)
    {
        var weapon = CombatSystem.EquippedWeapon(world)!;
        var ammoId = weapon.Definition.AmmoId!;
        if (!Ammo.Has(world.Belongings, ammoId))
        {
            actor.ClearIntent();
            return;
        }
        var range = weapon.Definition.Range;
        if (actor.Cell.DistanceTo(target.Cell) <= range)
        {
            CombatSystem.StartShoot(actor, target, events);
            return;
        }
        var path = Pathfinder.FindPathNextTo(actor.Cell, target.Cell, world.Level.IsWalkable, world.Level.IsFree);
        if (path is { Count: > 0 }) Movement.Step(world.Level, actor, path[0], events);
    }
}
