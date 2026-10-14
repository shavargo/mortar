namespace Mortar.Game.Content;

public sealed class MinionSpec
{
    public MinionSpec(string monsterId, IntRange count)
    {
        MonsterId = monsterId;
        Count = count;
    }

    public string MonsterId { get; }

    public IntRange Count { get; }
}

public sealed class MonsterDefinition
{
    public MonsterDefinition(
        string id,
        string name,
        string spriteSet,
        int level,
        IntRange life,
        int armorClass,
        int toHit,
        IntRange damage,
        int blockChance,
        ActionTimings timings,
        int sightRadius,
        int experience,
        string ai,
        MinionSpec? minions)
    {
        Id = id;
        Name = name;
        SpriteSet = spriteSet;
        Level = level;
        Life = life;
        ArmorClass = armorClass;
        ToHit = toHit;
        Damage = damage;
        BlockChance = blockChance;
        Timings = timings;
        SightRadius = sightRadius;
        Experience = experience;
        Ai = ai;
        Minions = minions;
    }

    public string Id { get; }

    public string Name { get; }

    public string SpriteSet { get; }

    public int Level { get; }

    public IntRange Life { get; }

    public int ArmorClass { get; }

    public int ToHit { get; }

    public IntRange Damage { get; }

    public int BlockChance { get; }

    public ActionTimings Timings { get; }

    public int SightRadius { get; }

    public int Experience { get; }

    public string Ai { get; }

    public MinionSpec? Minions { get; }
}
