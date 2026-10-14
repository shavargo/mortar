using Mortar.Game.Content;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

public static class PlayerStats
{
    public static CombatProfile Compute(ClassDefinition definition, PlayerProgress progress, GameRules rules, GearBonus gear)
    {
        var stats = progress.BaseStats.Plus(gear.Stats);
        var growth = definition.Growth;
        var levelsGained = progress.Level - 1;
        var maxLife = growth.LifeBase + stats.Vitality * growth.LifePerVitality + levelsGained * growth.LifePerLevel + gear.Life;
        var maxMana = growth.ManaBase + stats.Magic * growth.ManaPerMagic + levelsGained * growth.ManaPerLevel + gear.Mana;
        var armorClass = stats.Dexterity / rules.DexterityPerArmor + gear.ArmorClass;
        var toHit = rules.PlayerBaseToHit + stats.Dexterity * rules.ToHitPerDexterity + progress.Level * rules.ToHitPerLevel + gear.ToHit;
        var damage = (gear.WeaponDamage ?? definition.UnarmedDamage).Shift(stats.Strength / rules.StrengthPerDamage + gear.Damage);
        return new CombatProfile(
            progress.Level, maxLife, maxMana, armorClass, toHit, damage, gear.BlockChance, Timings(definition.Timings, gear.Swing));
    }

    private static ActionTimings Timings(ActionTimings classTimings, WeaponSwing? swing) =>
        swing is null
            ? classTimings
            : new ActionTimings(classTimings.MoveTicks, swing.AttackTicks, swing.HitFrame, classTimings.RecoveryTicks);
}
