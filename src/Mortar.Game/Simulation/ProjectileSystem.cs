using Mortar.Game.Events;
using Mortar.Game.Pathing;

namespace Mortar.Game.Simulation;

internal static class ProjectileSystem
{
    public const string StoppedByWall = "wall";
    public const string StoppedByRange = "range";

    public static Projectile Spawn(
        World world,
        Actor owner,
        string kind,
        Cell aim,
        int range,
        int damageMin,
        int damageMax,
        EventSink events)
    {
        var hitsMonsters = owner.Kind == ActorKind.Player;
        var hitsPlayer = owner.Kind == ActorKind.Monster;
        var projectile = new Projectile(
            world.NextProjectileId++,
            owner.Id,
            kind,
            owner.Cell,
            aim,
            range,
            damageMin,
            damageMax,
            hitsMonsters,
            hitsPlayer);
        world.Level.AddProjectile(projectile);
        events.Add(new ProjectileSpawned(events.Tick, projectile.Id, owner.Id, kind, owner.Cell));
        return projectile;
    }

    public static void Advance(World world, EventSink events)
    {
        var level = world.Level;
        var finished = new List<Projectile>();
        foreach (var projectile in level.Projectiles)
        {
            if (Step(world, projectile, events))
            {
                finished.Add(projectile);
            }
        }
        foreach (var projectile in finished)
        {
            level.RemoveProjectile(projectile);
        }
    }

    private static bool Step(World world, Projectile projectile, EventSink events)
    {
        if (projectile.RemainingRange <= 0 || projectile.Cell == projectile.Aim)
        {
            events.Add(new ProjectileStopped(events.Tick, projectile.Id, projectile.Cell, StoppedByRange));
            return true;
        }
        var next = projectile.Cell.Step(Directions.Toward(projectile.Cell, projectile.Aim));
        if (!world.Level.IsWalkable(next) || !Reach.CornersClear(projectile.Cell, next, world.Level.IsWalkable))
        {
            events.Add(new ProjectileStopped(events.Tick, projectile.Id, projectile.Cell, StoppedByWall));
            return true;
        }
        var from = projectile.Cell;
        projectile.Cell = next;
        projectile.RemainingRange--;
        events.Add(new ProjectileMoved(events.Tick, projectile.Id, from, next));
        var occupantId = world.Level.OccupantAt(next);
        if (occupantId != 0 && occupantId != projectile.OwnerId)
        {
            var target = world.Level.Find(occupantId);
            if (target is not null && target.IsAlive && CanHit(projectile, target))
            {
                Hit(world, projectile, target, events);
                return true;
            }
        }
        if (projectile.RemainingRange <= 0 || projectile.Cell == projectile.Aim)
        {
            events.Add(new ProjectileStopped(events.Tick, projectile.Id, projectile.Cell, StoppedByRange));
            return true;
        }
        return false;
    }

    private static bool CanHit(Projectile projectile, Actor target) =>
        (projectile.HitsMonsters && target.Kind == ActorKind.Monster)
        || (projectile.HitsPlayer && target.Kind == ActorKind.Player);

    private static void Hit(World world, Projectile projectile, Actor target, EventSink events)
    {
        var damage = world.Rng.NextInt(projectile.DamageMin, projectile.DamageMax + 1);
        events.Add(new ProjectileHit(events.Tick, projectile.Id, projectile.OwnerId, target.Id, damage));
        target.Life -= damage;
        target.LastAttackerId = projectile.OwnerId;
        if (target.Life > 0 && damage * 100 >= target.Profile.MaxLife * world.Content.Rules.HitRecoveryPercent)
        {
            var recovery = target.Profile.Timings.RecoveryTicks;
            target.StartAction(ActionKind.Recovering, recovery);
            events.Add(new ActorStaggered(events.Tick, target.Id, recovery));
        }
    }
}
