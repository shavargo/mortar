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
        ActionTimings timings)
    {
        Id = id;
        Name = name;
        SpriteSet = spriteSet;
        BaseStats = baseStats;
        StatCaps = statCaps;
        Growth = growth;
        UnarmedDamage = unarmedDamage;
        Timings = timings;
    }

    public string Id { get; }

    public string Name { get; }

    public string SpriteSet { get; }

    public Stats BaseStats { get; }

    public Stats StatCaps { get; }

    public ClassGrowth Growth { get; }

    public IntRange UnarmedDamage { get; }

    public ActionTimings Timings { get; }
}
