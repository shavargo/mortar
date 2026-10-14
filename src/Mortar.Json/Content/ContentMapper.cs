using Mortar.Core.Authoring;
using Mortar.Game.Content;

namespace Mortar.Json.Content;

internal static class ContentMapper
{
    public static GameRules ToRules(string source, RulesDto dto, List<AuthoringError> errors)
    {
        var fields = new Fields(source, errors);
        fields.Present(dto.ExperienceThresholds, "experienceThresholds");
        return new GameRules(
            fields.Int(dto.MinHitChance, "minHitChance"),
            fields.Int(dto.MaxHitChance, "maxHitChance"),
            fields.Int(dto.PlayerBaseToHit, "playerBaseToHit"),
            fields.Int(dto.ToHitPerDexterity, "toHitPerDexterity"),
            fields.Int(dto.ToHitPerLevel, "toHitPerLevel"),
            fields.Int(dto.ToHitPerLevelDifference, "toHitPerLevelDifference"),
            fields.Int(dto.DexterityPerArmor, "dexterityPerArmor"),
            fields.Int(dto.StrengthPerDamage, "strengthPerDamage"),
            fields.Int(dto.HitRecoveryPercent, "hitRecoveryPercent"),
            fields.Int(dto.ExperiencePercentPerLevel, "experiencePercentPerLevel"),
            fields.Int(dto.MinimumExperiencePercent, "minimumExperiencePercent"),
            dto.ExperienceThresholds ?? new List<long>());
    }

    public static ClassDefinition ToClass(string source, ClassDto dto, List<AuthoringError> errors)
    {
        var fields = new Fields(source, errors);
        return new ClassDefinition(
            fields.Text(dto.Id, "id"),
            fields.Text(dto.Name, "name"),
            fields.Text(dto.SpriteSet, "spriteSet"),
            fields.ReadStats(dto.BaseStats, "baseStats"),
            fields.ReadStats(dto.StatCaps, "statCaps"),
            ToGrowth(fields, dto.Growth),
            fields.ReadRange(dto.UnarmedDamage, "unarmedDamage"),
            fields.ReadTimings(dto.Timings),
            ToStartingItems(fields, dto.StartingItems));
    }

    public static MonsterDefinition ToMonster(string source, MonsterDto dto, List<AuthoringError> errors)
    {
        var fields = new Fields(source, errors);
        return new MonsterDefinition(
            fields.Text(dto.Id, "id"),
            fields.Text(dto.Name, "name"),
            fields.Text(dto.SpriteSet, "spriteSet"),
            fields.Int(dto.Level, "level"),
            fields.ReadRange(dto.Life, "life"),
            fields.Int(dto.ArmorClass, "armorClass"),
            fields.Int(dto.ToHit, "toHit"),
            fields.ReadRange(dto.Damage, "damage"),
            dto.BlockChance ?? 0,
            fields.ReadTimings(dto.Timings),
            fields.Int(dto.SightRadius, "sightRadius"),
            fields.Int(dto.Experience, "experience"),
            fields.Text(dto.Ai, "ai"),
            dto.Minions is null ? null : new MinionSpec(
                fields.Text(dto.Minions.Monster, "minions.monster"),
                fields.ReadRange(dto.Minions.Count, "minions.count")),
            string.IsNullOrWhiteSpace(dto.Loot) ? null : dto.Loot);
    }

    public static LevelDefinition ToLevel(string source, int index, LevelDto dto, string areaId, List<AuthoringError> errors)
    {
        var fields = new Fields(source, errors, $"levels entry {index}");
        var spawns = new List<SpawnEntry>();
        if (fields.Present(dto.Spawns, "spawns"))
        {
            for (var i = 0; i < dto.Spawns!.Count; i++)
            {
                var spawn = dto.Spawns[i];
                if (!fields.Present(spawn, $"spawns[{i}]")) continue;
                spawns.Add(new SpawnEntry(
                    fields.Text(spawn!.Monster, $"spawns[{i}].monster"),
                    fields.Int(spawn.Weight, $"spawns[{i}].weight")));
            }
        }
        var boss = string.IsNullOrWhiteSpace(dto.Boss) ? null : dto.Boss;
        return new LevelDefinition(fields.Int(dto.Depth, "depth"), areaId, spawns, boss);
    }

    private static ClassGrowth ToGrowth(Fields fields, GrowthDto? dto)
    {
        if (!fields.Present(dto, "growth")) return new ClassGrowth(0, 0, 0, 0, 0, 0, 0);
        return new ClassGrowth(
            fields.Int(dto!.LifeBase, "growth.lifeBase"),
            fields.Int(dto.LifePerVitality, "growth.lifePerVitality"),
            fields.Int(dto.LifePerLevel, "growth.lifePerLevel"),
            fields.Int(dto.ManaBase, "growth.manaBase"),
            fields.Int(dto.ManaPerMagic, "growth.manaPerMagic"),
            fields.Int(dto.ManaPerLevel, "growth.manaPerLevel"),
            fields.Int(dto.StatPointsPerLevel, "growth.statPointsPerLevel"));
    }

    private static IReadOnlyList<string> ToStartingItems(Fields fields, List<string?>? ids) =>
        ids is null ? Array.Empty<string>() : ids.Select((id, i) => fields.Text(id, $"startingItems[{i}]")).ToList();
}
