using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public static class Leveling
{
    public static long KillExperience(int baseExperience, int monsterLevel, int playerLevel, GameRules rules)
    {
        var percent = Math.Max(
            rules.MinimumExperiencePercent,
            100L + (long)rules.ExperiencePercentPerLevel * (monsterLevel - playerLevel));
        return (long)baseExperience * percent / 100;
    }

    public static int AddExperience(PlayerProgress progress, long amount, ClassDefinition definition, GameRules rules)
    {
        progress.Experience += amount;
        var gained = 0;
        while (progress.Level < rules.MaxLevel && progress.Experience >= rules.ExperienceThresholds[progress.Level - 1])
        {
            progress.Level++;
            progress.UnspentPoints += definition.Growth.StatPointsPerLevel;
            gained++;
        }
        return gained;
    }

    public static string? SpendPoint(PlayerProgress progress, StatKind stat, ClassDefinition definition)
    {
        if (progress.UnspentPoints <= 0)
        {
            return "no unspent stat points";
        }
        var current = progress.BaseStats.Get(stat);
        if (current >= definition.StatCaps.Get(stat))
        {
            return $"{stat.ToString().ToLowerInvariant()} is at its cap";
        }
        progress.BaseStats = progress.BaseStats.With(stat, current + 1);
        progress.UnspentPoints--;
        return null;
    }
}
