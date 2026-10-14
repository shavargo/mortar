using Mortar.Core.Authoring;

namespace Mortar.Game.Content;

internal static class DefinitionChecks
{
    public const int MaxMinions = 12;

    public static void Add(this List<AuthoringError> errors, string source, string message) =>
        errors.Add(new AuthoringError(source, message));

    public static void CheckClass(ClassDefinition definition, List<AuthoringError> errors)
    {
        var source = $"class '{definition.Id}'";
        if (string.IsNullOrWhiteSpace(definition.Name)) errors.Add(source, "name is required");
        foreach (var kind in Stats.Kinds)
        {
            var name = kind.ToString().ToLowerInvariant();
            if (definition.BaseStats.Get(kind) < 0) errors.Add(source, $"base {name} must not be negative");
            if (definition.StatCaps.Get(kind) < definition.BaseStats.Get(kind))
            {
                errors.Add(source, $"{name} cap must be at least the base value");
            }
        }
        var growth = definition.Growth;
        if (growth.LifeBase < 1) errors.Add(source, "lifeBase must be at least 1");
        if (growth.LifePerVitality < 0 || growth.LifePerLevel < 0 || growth.ManaBase < 0
            || growth.ManaPerMagic < 0 || growth.ManaPerLevel < 0 || growth.StatPointsPerLevel < 0)
        {
            errors.Add(source, "growth values must not be negative");
        }
        CheckRange(source, "unarmedDamage", definition.UnarmedDamage, 0, errors);
        CheckTimings(source, definition.Timings, errors);
    }

    public static void CheckMonster(MonsterDefinition monster, Func<string, MonsterDefinition?> find, List<AuthoringError> errors)
    {
        var source = $"monster '{monster.Id}'";
        if (string.IsNullOrWhiteSpace(monster.Name)) errors.Add(source, "name is required");
        if (monster.Level < 1) errors.Add(source, "level must be at least 1");
        CheckRange(source, "life", monster.Life, 1, errors);
        CheckRange(source, "damage", monster.Damage, 0, errors);
        if (monster.ArmorClass < 0 || monster.ToHit < 0 || monster.Experience < 0)
        {
            errors.Add(source, "armorClass, toHit, and experience must not be negative");
        }
        if (monster.BlockChance < 0 || monster.BlockChance > 100) errors.Add(source, "blockChance must be between 0 and 100");
        if (monster.SightRadius < 1) errors.Add(source, "sightRadius must be at least 1");
        CheckTimings(source, monster.Timings, errors);
        CheckAi(source, monster, find, errors);
    }

    private static void CheckAi(string source, MonsterDefinition monster, Func<string, MonsterDefinition?> find, List<AuthoringError> errors)
    {
        if (!AiKinds.IsKnown(monster.Ai))
        {
            errors.Add(source, $"unknown ai '{monster.Ai}'");
            return;
        }
        if (monster.Ai != AiKinds.PackLeader)
        {
            if (monster.Minions is not null) errors.Add(source, "only pack-leader monsters can have minions");
            return;
        }
        if (monster.Minions is null)
        {
            errors.Add(source, "pack-leader monsters need minions");
            return;
        }
        CheckRange(source, "minions count", monster.Minions.Count, 0, errors);
        if (monster.Minions.Count.Max > MaxMinions) errors.Add(source, $"minions count max must be at most {MaxMinions}");
        var minion = find(monster.Minions.MonsterId);
        if (minion is null) errors.Add(source, $"minions reference unknown monster '{monster.Minions.MonsterId}'");
        else if (minion.Ai == AiKinds.PackLeader) errors.Add(source, "minions cannot be pack leaders");
    }

    private static void CheckRange(string source, string field, IntRange range, int minimum, List<AuthoringError> errors)
    {
        if (range.Min < minimum || range.Max < range.Min)
        {
            errors.Add(source, $"{field} must satisfy {minimum} <= min <= max");
        }
    }

    private static void CheckTimings(string source, ActionTimings timings, List<AuthoringError> errors)
    {
        if (timings.MoveTicks < 1 || timings.AttackTicks < 1 || timings.RecoveryTicks < 1)
        {
            errors.Add(source, "moveTicks, attackTicks, and recoveryTicks must be at least 1");
        }
        if (timings.HitFrame < 1 || timings.HitFrame >= timings.AttackTicks)
        {
            errors.Add(source, "hitFrame must be at least 1 and less than attackTicks");
        }
    }
}
