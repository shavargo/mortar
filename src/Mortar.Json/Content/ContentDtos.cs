namespace Mortar.Json.Content;

internal sealed class ManifestDto
{
    public string? Rules { get; set; }

    public string? Classes { get; set; }

    public string? Monsters { get; set; }

    public string? Levels { get; set; }

    public string? Items { get; set; }

    public string? Spells { get; set; }

    public string? Affixes { get; set; }

    public string? Loot { get; set; }
}

internal sealed class RulesDto
{
    public int? MinHitChance { get; set; }

    public int? MaxHitChance { get; set; }

    public int? PlayerBaseToHit { get; set; }

    public int? ToHitPerDexterity { get; set; }

    public int? ToHitPerLevel { get; set; }

    public int? ToHitPerLevelDifference { get; set; }

    public int? DexterityPerArmor { get; set; }

    public int? StrengthPerDamage { get; set; }

    public int? HitRecoveryPercent { get; set; }

    public int? ExperiencePercentPerLevel { get; set; }

    public int? MinimumExperiencePercent { get; set; }

    public List<long>? ExperienceThresholds { get; set; }

    public int? MaxSpellLevel { get; set; }
}

internal sealed class StatsDto
{
    public int? Strength { get; set; }

    public int? Dexterity { get; set; }

    public int? Vitality { get; set; }

    public int? Magic { get; set; }
}

internal sealed class RangeDto
{
    public int? Min { get; set; }

    public int? Max { get; set; }
}

internal sealed class TimingsDto
{
    public int? MoveTicks { get; set; }

    public int? AttackTicks { get; set; }

    public int? HitFrame { get; set; }

    public int? RecoveryTicks { get; set; }
}

internal sealed class GrowthDto
{
    public int? LifeBase { get; set; }

    public int? LifePerVitality { get; set; }

    public int? LifePerLevel { get; set; }

    public int? ManaBase { get; set; }

    public int? ManaPerMagic { get; set; }

    public int? ManaPerLevel { get; set; }

    public int? StatPointsPerLevel { get; set; }
}

internal sealed class ClassDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? SpriteSet { get; set; }

    public StatsDto? BaseStats { get; set; }

    public StatsDto? StatCaps { get; set; }

    public GrowthDto? Growth { get; set; }

    public RangeDto? UnarmedDamage { get; set; }

    public TimingsDto? Timings { get; set; }

    public List<string?>? StartingItems { get; set; }

    public List<string?>? StartingSpells { get; set; }
}

internal sealed class MinionsDto
{
    public string? Monster { get; set; }

    public RangeDto? Count { get; set; }
}

internal sealed class MonsterDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? SpriteSet { get; set; }

    public int? Level { get; set; }

    public RangeDto? Life { get; set; }

    public int? ArmorClass { get; set; }

    public int? ToHit { get; set; }

    public RangeDto? Damage { get; set; }

    public int? BlockChance { get; set; }

    public TimingsDto? Timings { get; set; }

    public int? SightRadius { get; set; }

    public int? Experience { get; set; }

    public string? Ai { get; set; }

    public MinionsDto? Minions { get; set; }

    public string? Loot { get; set; }
}

internal sealed class LevelsDto
{
    public List<LevelDto?>? Levels { get; set; }

    public TownDto? Town { get; set; }
}

internal sealed class TownDto
{
    public string? Area { get; set; }
}

internal sealed class LevelDto
{
    public int? Depth { get; set; }

    public string? Area { get; set; }

    public List<SpawnDto?>? Spawns { get; set; }

    public string? Boss { get; set; }
}

internal sealed class SpawnDto
{
    public string? Monster { get; set; }

    public int? Weight { get; set; }
}
