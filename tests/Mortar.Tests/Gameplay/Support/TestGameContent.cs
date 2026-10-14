using Mortar.Core.Authoring;
using Mortar.Game.Content;

namespace Mortar.Tests.Gameplay.Support;

internal static class TestGameContent
{
    public static GameRules Rules(
        int minHit = 5,
        int maxHit = 95,
        int recoveryPercent = 20,
        long[]? thresholds = null,
        int experiencePercentPerLevel = 10,
        int minimumExperiencePercent = 0) =>
        new GameRules(
            minHit, maxHit, 50, 1, 1, 2, 5, 10, recoveryPercent,
            experiencePercentPerLevel, minimumExperiencePercent, thresholds ?? new long[] { 100, 300, 600 });

    public static ActionTimings Timings(int move = 4, int attack = 6, int hit = 3, int recovery = 3) =>
        new ActionTimings(move, attack, hit, recovery);

    public static ClassDefinition Warrior() => new ClassDefinition(
        "warrior",
        "Warrior",
        "warrior",
        new Stats(20, 20, 20, 10),
        new Stats(100, 100, 100, 100),
        new ClassGrowth(10, 2, 2, 0, 1, 1, 5),
        new IntRange(2, 4),
        Timings());

    public static MonsterDefinition Monster(
        string id,
        string ai = AiKinds.MeleeChase,
        MinionSpec? minions = null,
        int life = 10,
        int sight = 6,
        int experience = 50) =>
        new MonsterDefinition(id, id, id, 1, new IntRange(life, life), 0, 50, new IntRange(1, 2), 0, Timings(), sight, experience, ai, minions);

    public static LevelDefinition Level(int depth, string monsterId = "zombie", string? bossId = null) =>
        new LevelDefinition(depth, "test", new[] { new SpawnEntry(monsterId, 1) }, bossId);

    public static AuthoringResult<ContentSet> Create(
        IReadOnlyList<MonsterDefinition>? monsters = null,
        IReadOnlyList<LevelDefinition>? levels = null,
        GameRules? rules = null,
        IReadOnlyList<ClassDefinition>? classes = null) =>
        ContentSet.Create(
            rules ?? Rules(),
            classes ?? new[] { Warrior() },
            monsters ?? new[] { Monster("zombie") },
            levels ?? new[] { Level(1), Level(2) });

    public static ContentSet Valid(
        IReadOnlyList<MonsterDefinition>? monsters = null,
        IReadOnlyList<LevelDefinition>? levels = null,
        GameRules? rules = null)
    {
        var result = Create(monsters, levels, rules);
        return result.Value ?? throw new InvalidOperationException(string.Join("; ", result.Errors));
    }
}
