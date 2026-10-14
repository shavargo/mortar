namespace Mortar.Game.Simulation;

public static class WorldHash
{
    public static string Compute(World world)
    {
        var hasher = new StateHasher();
        hasher.Add(world.Seed).Add(world.TickCount).Add((long)world.Depth);
        hasher.Add(world.Rng.State).Add(world.Rng.Increment);
        AddProgress(hasher, world.Progress);
        foreach (var depth in world.VisitedDepths)
        {
            var level = world.GetLevel(depth);
            hasher.Add((long)level.Depth).Add(level.Seed).Add(level.Layout.Fingerprint);
            foreach (var actor in level.Actors) AddActor(hasher, actor);
        }
        return hasher.Value.ToString("x16");
    }

    private static void AddProgress(StateHasher hasher, PlayerProgress progress)
    {
        var stats = progress.BaseStats;
        hasher.Add((long)progress.Level).Add(progress.Experience).Add((long)progress.UnspentPoints);
        hasher.Add((long)stats.Strength).Add((long)stats.Dexterity).Add((long)stats.Vitality).Add((long)stats.Magic);
    }

    private static void AddActor(StateHasher hasher, Actor actor)
    {
        hasher.Add((long)actor.Id).Add((long)actor.Kind).Add(actor.DefinitionId);
        hasher.Add((long)actor.Cell.X).Add((long)actor.Cell.Y).Add((long)actor.Facing);
        hasher.Add((long)actor.Life).Add((long)actor.Mana).Add(actor.IsDead).Add(actor.IsAwake);
        hasher.Add((long)actor.LeaderId).Add((long)actor.LastAttackerId);
        hasher.Add((long)actor.Action).Add((long)actor.ActionElapsed).Add((long)actor.ActionDuration).Add((long)actor.ActionTargetId);
        hasher.Add((long)actor.Intent).Add((long)actor.IntentCell.X).Add((long)actor.IntentCell.Y).Add((long)actor.IntentTargetId);
        hasher.Add((long)actor.Profile.Level).Add((long)actor.Profile.MaxLife).Add((long)actor.Profile.MaxMana);
    }
}
