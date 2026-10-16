using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

public static class WorldHash
{
    public static string Compute(World world)
    {
        var hasher = new StateHasher();
        hasher.Add(world.Seed).Add(world.TickCount).Add((long)world.Depth).Add((long)world.NextActorId).Add((long)world.NextItemId);
        hasher.Add((long)world.NextProjectileId);
        hasher.Add(world.Rng.State).Add(world.Rng.Increment);
        AddProgress(hasher, world.Progress);
        AddBelongings(hasher, world.Belongings);
        foreach (var depth in world.VisitedDepths)
        {
            var level = world.GetLevel(depth);
            hasher.Add((long)level.Depth).Add(level.Seed).Add(level.Layout.Fingerprint);
            foreach (var actor in level.Actors) AddActor(hasher, actor);
            hasher.Add((long)level.GroundItems.Count);
            foreach (var ground in level.GroundItems) AddGround(hasher, ground);
            hasher.Add((long)level.Projectiles.Count);
            foreach (var projectile in level.Projectiles) AddProjectile(hasher, projectile);
        }
        return hasher.Value.ToString("x16");
    }

    private static void AddProjectile(StateHasher hasher, Projectile projectile)
    {
        hasher.Add((long)projectile.Id).Add((long)projectile.OwnerId).Add(projectile.Kind);
        hasher.Add((long)projectile.Cell.X).Add((long)projectile.Cell.Y);
        hasher.Add((long)projectile.Aim.X).Add((long)projectile.Aim.Y);
        hasher.Add((long)projectile.RemainingRange);
        hasher.Add((long)projectile.DamageMin).Add((long)projectile.DamageMax);
        hasher.Add(projectile.HitsMonsters).Add(projectile.HitsPlayer);
    }

    private static void AddProgress(StateHasher hasher, PlayerProgress progress)
    {
        var stats = progress.BaseStats;
        hasher.Add((long)progress.Level).Add(progress.Experience).Add((long)progress.UnspentPoints);
        hasher.Add((long)stats.Strength).Add((long)stats.Dexterity).Add((long)stats.Vitality).Add((long)stats.Magic);
        hasher.Add((long)progress.Spells.Count);
        foreach (var spell in progress.Spells)
        {
            hasher.Add(spell.SpellId).Add((long)spell.Level);
        }
    }

    private static void AddActor(StateHasher hasher, Actor actor)
    {
        hasher.Add((long)actor.Id).Add((long)actor.Kind).Add(actor.DefinitionId);
        hasher.Add((long)actor.Cell.X).Add((long)actor.Cell.Y).Add((long)actor.Facing);
        hasher.Add((long)actor.Life).Add((long)actor.Mana).Add(actor.IsDead).Add(actor.IsAwake);
        hasher.Add((long)actor.LeaderId).Add((long)actor.LastAttackerId);
        hasher.Add((long)actor.Action).Add((long)actor.ActionElapsed).Add((long)actor.ActionDuration).Add((long)actor.ActionTargetId);
        hasher.Add(actor.ActionSpellId).Add((long)actor.ActionAim.X).Add((long)actor.ActionAim.Y);
        hasher.Add((long)actor.Intent).Add((long)actor.IntentCell.X).Add((long)actor.IntentCell.Y).Add((long)actor.IntentTargetId);
        hasher.Add(actor.IntentSpellId);
        hasher.Add((long)actor.Profile.Level).Add((long)actor.Profile.MaxLife).Add((long)actor.Profile.MaxMana);
    }

    private static void AddBelongings(StateHasher hasher, Belongings belongings)
    {
        hasher.Add(belongings.Gold).Add((long)belongings.Inventory.Items.Count);
        foreach (var stored in belongings.Inventory.Items)
        {
            hasher.Add((long)stored.Cell.X).Add((long)stored.Cell.Y);
            AddItem(hasher, stored.Item);
        }
        for (var slot = 0; slot < Belt.Slots; slot++) AddOptional(hasher, belongings.Belt.Get(slot));
        foreach (var slot in Equipment.Slots) AddOptional(hasher, belongings.Equipment.Get(slot));
    }

    private static void AddGround(StateHasher hasher, GroundItem ground)
    {
        hasher.Add((long)ground.Id).Add((long)ground.Cell.X).Add((long)ground.Cell.Y).Add((long)ground.Gold);
        AddOptional(hasher, ground.Item);
    }

    private static void AddOptional(StateHasher hasher, Item? item)
    {
        hasher.Add(item is not null);
        if (item is not null) AddItem(hasher, item);
    }

    private static void AddItem(StateHasher hasher, Item item)
    {
        hasher.Add((long)item.Id).Add(item.Definition.Id).Add((long)item.ItemLevel).Add((long)item.ArmorValue);
        hasher.Add((long)item.StackCount);
        AddAffix(hasher, item.Prefix);
        AddAffix(hasher, item.Suffix);
    }

    private static void AddAffix(StateHasher hasher, RolledAffix? affix) =>
        hasher.Add(affix?.Definition.Id ?? "").Add((long)(affix?.Value ?? 0));
}
