using Mortar.Game.Content;
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class PlayerGrowth
{
    public static void RewardKill(World world, Actor monster, EventSink events)
    {
        var definition = world.Content.Monster(monster.DefinitionId);
        var amount = Leveling.KillExperience(definition.Experience, definition.Level, world.Progress.Level, world.Content.Rules);
        if (amount == 0) return;
        var gained = Leveling.AddExperience(world.Progress, amount, world.PlayerClass, world.Content.Rules);
        events.Add(new ExperienceGained(events.Tick, amount, world.Progress.Experience));
        if (gained == 0) return;
        world.RefreshPlayer();
        world.Player.RestoreVitals();
        events.Add(new LeveledUp(events.Tick, world.Progress.Level));
    }

    public static string? SpendPoint(World world, StatKind stat, EventSink events)
    {
        if (!Stats.Kinds.Contains(stat)) return "unknown stat";
        var reason = Leveling.SpendPoint(world.Progress, stat, world.PlayerClass);
        if (reason is not null) return reason;
        world.RefreshPlayer();
        events.Add(new StatIncreased(events.Tick, stat, world.Progress.BaseStats.Get(stat)));
        return null;
    }
}
