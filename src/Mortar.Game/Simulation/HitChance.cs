using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public static class HitChance
{
    public static int Compute(CombatProfile attacker, CombatProfile defender, GameRules rules)
    {
        var chance = attacker.ToHit - defender.ArmorClass + (attacker.Level - defender.Level) * rules.ToHitPerLevelDifference;
        return Math.Max(rules.MinHitChance, Math.Min(rules.MaxHitChance, chance));
    }
}
