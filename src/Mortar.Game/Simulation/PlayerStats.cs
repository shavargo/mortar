using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public static class PlayerStats
{
    public static CombatProfile Compute(ClassDefinition definition, PlayerProgress progress, GameRules rules)
    {
        var stats = progress.BaseStats;
        var growth = definition.Growth;
        var levelsGained = progress.Level - 1;
        var maxLife = growth.LifeBase + stats.Vitality * growth.LifePerVitality + levelsGained * growth.LifePerLevel;
        var maxMana = growth.ManaBase + stats.Magic * growth.ManaPerMagic + levelsGained * growth.ManaPerLevel;
        var armorClass = stats.Dexterity / rules.DexterityPerArmor;
        var toHit = rules.PlayerBaseToHit + stats.Dexterity * rules.ToHitPerDexterity + progress.Level * rules.ToHitPerLevel;
        var damage = definition.UnarmedDamage.Shift(stats.Strength / rules.StrengthPerDamage);
        return new CombatProfile(progress.Level, maxLife, maxMana, armorClass, toHit, damage, 0, definition.Timings);
    }
}
