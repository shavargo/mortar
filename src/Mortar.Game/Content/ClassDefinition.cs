namespace Mortar.Game.Content;

public sealed class ClassDefinition
{
    public ClassDefinition(
        string id,
        string name,
        string spriteSet,
        Stats baseStats,
        Stats statCaps,
        ClassGrowth growth,
        IntRange unarmedDamage,
        ActionTimings timings,
        IReadOnlyList<string> startingItems,
        IReadOnlyList<string>? startingSpells = null,
        string classSkill = "")
    {
        Id = id;
        Name = name;
        SpriteSet = spriteSet;
        BaseStats = baseStats;
        StatCaps = statCaps;
        Growth = growth;
        UnarmedDamage = unarmedDamage;
        Timings = timings;
        StartingItems = startingItems.ToArray();
        StartingSpells = (startingSpells ?? Array.Empty<string>()).ToArray();
        ClassSkill = classSkill ?? "";
    }

    public string Id { get; }

    public string Name { get; }

    public string SpriteSet { get; }

    public Stats BaseStats { get; }

    public Stats StatCaps { get; }

    public ClassGrowth Growth { get; }

    public IntRange UnarmedDamage { get; }

    public ActionTimings Timings { get; }

    public IReadOnlyList<string> StartingItems { get; }

    public IReadOnlyList<string> StartingSpells { get; }

    public string ClassSkill { get; }
}
