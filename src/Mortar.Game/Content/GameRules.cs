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
        int experiencePercentPerLevel,
        int minimumExperiencePercent,
        IReadOnlyList<long> experienceThresholds,
        int maxSpellLevel = 15,
        int vendorStockRegenTicks = 144000,
        int vendorSellPercent = 25,
        int vendorMagicBuyMult = 3,
        int vendorRepairPercent = 50,
        int vendorIdentifyFee = 50,
        int vendorHealFee = 0,
        int vendorSmithSlots = 8,
        int vendorHealerSlots = 6,
        int vendorSageSlots = 6,
        int vendorSmithMagicPercent = 40)
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
        ExperiencePercentPerLevel = experiencePercentPerLevel;
        MinimumExperiencePercent = minimumExperiencePercent;
        ExperienceThresholds = experienceThresholds.ToArray();
        MaxSpellLevel = maxSpellLevel;
        VendorStockRegenTicks = vendorStockRegenTicks;
        VendorSellPercent = vendorSellPercent;
        VendorMagicBuyMult = vendorMagicBuyMult;
        VendorRepairPercent = vendorRepairPercent;
        VendorIdentifyFee = vendorIdentifyFee;
        VendorHealFee = vendorHealFee;
        VendorSmithSlots = vendorSmithSlots;
        VendorHealerSlots = vendorHealerSlots;
        VendorSageSlots = vendorSageSlots;
        VendorSmithMagicPercent = vendorSmithMagicPercent;
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

    /// <summary>Percent of base kill experience added per level the monster is above the player (removed when below).</summary>
    public int ExperiencePercentPerLevel { get; }

    /// <summary>Kill experience never drops below this percent of the monster's base experience.</summary>
    public int MinimumExperiencePercent { get; }

    /// <summary>Total experience needed to reach level 2, 3, ...; index 0 is level 2.</summary>
    public IReadOnlyList<long> ExperienceThresholds { get; }

    public int MaxSpellLevel { get; }

    public int VendorStockRegenTicks { get; }

    public int VendorSellPercent { get; }

    public int VendorMagicBuyMult { get; }

    public int VendorRepairPercent { get; }

    public int VendorIdentifyFee { get; }

    public int VendorHealFee { get; }

    public int VendorSmithSlots { get; }

    public int VendorHealerSlots { get; }

    public int VendorSageSlots { get; }

    public int VendorSmithMagicPercent { get; }

    public int MaxLevel => ExperienceThresholds.Count + 1;
}
