using Mortar.Core.Authoring;

namespace Mortar.Game.Content;

internal static class ContentValidator
{
    public static List<AuthoringError> Validate(
        GameRules rules,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        IReadOnlyList<LevelDefinition> levels)
    {
        var errors = new List<AuthoringError>();
        CheckRules(rules, errors);
        if (classes.Count == 0)
        {
            errors.Add("classes", "at least one class is required");
        }
        CheckUniqueIds(classes.Select(c => c.Id), "class", errors);
        CheckUniqueIds(monsters.Select(m => m.Id), "monster", errors);
        var byId = new Dictionary<string, MonsterDefinition>(StringComparer.Ordinal);
        foreach (var monster in monsters)
        {
            if (!byId.ContainsKey(monster.Id)) byId.Add(monster.Id, monster);
        }
        MonsterDefinition? Find(string id) => byId.TryGetValue(id, out var found) ? found : null;
        foreach (var definition in classes) DefinitionChecks.CheckClass(definition, errors);
        foreach (var monster in monsters) DefinitionChecks.CheckMonster(monster, Find, errors);
        CheckLevels(levels, Find, errors);
        return errors;
    }

    private static void CheckRules(GameRules rules, List<AuthoringError> errors)
    {
        const string source = "rules";
        if (rules.MinHitChance < 0 || rules.MinHitChance > rules.MaxHitChance || rules.MaxHitChance > 100)
        {
            errors.Add(source, "hit chance limits must satisfy 0 <= minHitChance <= maxHitChance <= 100");
        }
        if (rules.PlayerBaseToHit < 0 || rules.ToHitPerDexterity < 0 || rules.ToHitPerLevel < 0 || rules.ToHitPerLevelDifference < 0)
        {
            errors.Add(source, "to-hit values must not be negative");
        }
        if (rules.DexterityPerArmor < 1 || rules.StrengthPerDamage < 1)
        {
            errors.Add(source, "dexterityPerArmor and strengthPerDamage must be at least 1");
        }
        if (rules.HitRecoveryPercent < 1 || rules.HitRecoveryPercent > 100)
        {
            errors.Add(source, "hitRecoveryPercent must be between 1 and 100");
        }
        for (var i = 0; i < rules.ExperienceThresholds.Count; i++)
        {
            var previous = i == 0 ? 0 : rules.ExperienceThresholds[i - 1];
            if (rules.ExperienceThresholds[i] <= previous)
            {
                errors.Add(source, "experienceThresholds must be positive and strictly increasing");
                break;
            }
        }
    }

    private static void CheckUniqueIds(IEnumerable<string> ids, string kind, List<AuthoringError> errors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id)) errors.Add(kind, "id must not be empty");
            else if (!seen.Add(id)) errors.Add($"{kind} '{id}'", "duplicate id");
        }
    }

    private static void CheckLevels(
        IReadOnlyList<LevelDefinition> levels,
        Func<string, MonsterDefinition?> find,
        List<AuthoringError> errors)
    {
        if (levels.Count == 0)
        {
            errors.Add("levels", "at least one level is required");
            return;
        }
        var depths = levels.Select(level => level.Depth).OrderBy(depth => depth).ToList();
        for (var i = 0; i < depths.Count; i++)
        {
            if (depths[i] != i + 1)
            {
                errors.Add("levels", "depths must be 1, 2, 3, ... with no gaps or repeats");
                break;
            }
        }
        foreach (var level in levels) CheckLevel(level, find, errors);
    }

    private static void CheckLevel(LevelDefinition level, Func<string, MonsterDefinition?> find, List<AuthoringError> errors)
    {
        var source = $"level {level.Depth}";
        if (string.IsNullOrWhiteSpace(level.AreaId)) errors.Add(source, "area is required");
        if (level.Spawns.Count == 0) errors.Add(source, "at least one spawn entry is required");
        long total = 0;
        foreach (var spawn in level.Spawns)
        {
            if (spawn.Weight < 1) errors.Add(source, $"spawn '{spawn.MonsterId}' weight must be at least 1");
            if (find(spawn.MonsterId) is null) errors.Add(source, $"spawn references unknown monster '{spawn.MonsterId}'");
            total += Math.Max(0, spawn.Weight);
        }
        if (total > int.MaxValue) errors.Add(source, "spawn weights add up to more than 2147483647");
        if (level.BossId is not null && find(level.BossId) is null)
        {
            errors.Add(source, $"boss references unknown monster '{level.BossId}'");
        }
    }
}
