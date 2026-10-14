namespace Mortar.Game.Content;

public sealed class GameRules
{
    public GameRules(
        int minHitChance,
        int maxHitChance,
        int playerBaseToHit,
        int toHitPerDexterity,
        int toHitPerLevel,
        int toHitPerLevelDifference,
        int dexterityPerArmor,
        int strengthPerDamage,
        int hitRecoveryPercent,
        IReadOnlyList<long> experienceThresholds)
    {
        MinHitChance = minHitChance;
        MaxHitChance = maxHitChance;
        PlayerBaseToHit = playerBaseToHit;
        ToHitPerDexterity = toHitPerDexterity;
        ToHitPerLevel = toHitPerLevel;
        ToHitPerLevelDifference = toHitPerLevelDifference;
        DexterityPerArmor = dexterityPerArmor;
        StrengthPerDamage = strengthPerDamage;
        HitRecoveryPercent = hitRecoveryPercent;
        ExperienceThresholds = experienceThresholds;
    }

    public int MinHitChance { get; }

    public int MaxHitChance { get; }

    public int PlayerBaseToHit { get; }

    public int ToHitPerDexterity { get; }

    public int ToHitPerLevel { get; }

    public int ToHitPerLevelDifference { get; }

    public int DexterityPerArmor { get; }

    public int StrengthPerDamage { get; }

    public int HitRecoveryPercent { get; }

    /// <summary>Total experience needed to reach level 2, 3, ...; index 0 is level 2.</summary>
    public IReadOnlyList<long> ExperienceThresholds { get; }

    public int MaxLevel => ExperienceThresholds.Count + 1;
}
