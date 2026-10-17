using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Pathing;

namespace Mortar.Game.Simulation;

internal static class CombatSystem
{
    public static void StartAttack(Actor attacker, Actor target, EventSink events)
    {
        var duration = attacker.Profile.Timings.AttackTicks;
        attacker.Facing = Directions.Toward(attacker.Cell, target.Cell);
        attacker.StartAction(ActionKind.Attacking, duration, target.Id);
        events.Add(new AttackStarted(events.Tick, attacker.Id, target.Id, duration));
    }

    public static void StartShoot(Actor attacker, Actor target, EventSink events)
    {
        var duration = attacker.Profile.Timings.AttackTicks;
        attacker.Facing = Directions.Toward(attacker.Cell, target.Cell);
        attacker.StartAction(ActionKind.Shooting, duration, target.Id);
        events.Add(new AttackStarted(events.Tick, attacker.Id, target.Id, duration));
    }

    public static void Resolve(World world, IReadOnlyList<Actor> due, EventSink events)
    {
        foreach (var attacker in due)
        {
            if (!attacker.IsAlive) continue;
            if (attacker.Action == ActionKind.Shooting)
            {
                ResolveShot(world, attacker, events);
                continue;
            }
            if (attacker.Action != ActionKind.Attacking) continue;
            var target = world.Level.Find(attacker.ActionTargetId);
            if (target is null || !target.IsAlive) continue;
            if (!Reach.CanStrike(attacker.Cell, target.Cell, world.Level.IsWalkable))
            {
                events.Add(new AttackResolved(events.Tick, attacker.Id, target.Id, AttackOutcome.Miss, 0));
                continue;
            }
            Strike(world, attacker, target, events);
        }
    }

    public static Item? EquippedWeapon(World world) =>
        world.Belongings.Equipment.Get(EquipmentSlot.Weapon);

    public static bool IsRangedWeapon(Item? weapon) => weapon?.Definition.Ranged == true;

    private static void ResolveShot(World world, Actor attacker, EventSink events)
    {
        var target = world.Level.Find(attacker.ActionTargetId);
        if (target is null || !target.IsAlive) return;
        if (attacker.Kind != ActorKind.Player)
        {
            var range = world.Content.Monster(attacker.DefinitionId).PreferredRange;
            SpawnArrow(world, attacker, target, events, range > 0 ? range : 8);
            return;
        }
        var weapon = EquippedWeapon(world);
        if (!IsRangedWeapon(weapon)) return;
        var ammoId = weapon!.Definition.AmmoId!;
        if (!Ammo.TrySpend(world.Belongings, ammoId)) return;
        SpawnArrow(world, attacker, target, events, weapon.Definition.Range);
    }

    private static void SpawnArrow(World world, Actor attacker, Actor target, EventSink events, int range)
    {
        var damage = attacker.Profile.Damage;
        ProjectileSystem.Spawn(
            world,
            attacker,
            ProjectileKinds.Arrow,
            target.Cell,
            range,
            damage.Min,
            damage.Max,
            events);
    }

    private static void Strike(World world, Actor attacker, Actor target, EventSink events)
    {
        var outcome = Roll(world, attacker, target);
        var damageRange = attacker.Profile.Damage;
        var damage = outcome == AttackOutcome.Hit ? world.Rng.NextInt(damageRange.Min, damageRange.Max + 1) : 0;
        events.Add(new AttackResolved(events.Tick, attacker.Id, target.Id, outcome, damage));
        if (outcome != AttackOutcome.Hit) return;
        target.Life -= damage;
        target.LastAttackerId = attacker.Id;
        if (target.Life > 0 && damage * 100 >= target.Profile.MaxLife * world.Content.Rules.HitRecoveryPercent)
        {
            var recovery = target.Profile.Timings.RecoveryTicks;
            target.StartAction(ActionKind.Recovering, recovery);
            events.Add(new ActorStaggered(events.Tick, target.Id, recovery));
        }
    }

    private static AttackOutcome Roll(World world, Actor attacker, Actor target)
    {
        var chance = HitChance.Compute(attacker.Profile, target.Profile, world.Content.Rules);
        if (world.Rng.NextInt(100) >= chance) return AttackOutcome.Miss;
        var block = target.Profile.BlockChance;
        if (block > 0 && world.Rng.NextInt(100) < block) return AttackOutcome.Blocked;
        return AttackOutcome.Hit;
    }
}
