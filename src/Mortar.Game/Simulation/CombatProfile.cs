using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public sealed class CombatProfile
{
    public CombatProfile(
        int level,
        int maxLife,
        int maxMana,
        int armorClass,
        int toHit,
        IntRange damage,
        int blockChance,
        ActionTimings timings)
    {
        Level = level;
        MaxLife = maxLife;
        MaxMana = maxMana;
        ArmorClass = armorClass;
        ToHit = toHit;
        Damage = damage;
        BlockChance = blockChance;
        Timings = timings;
    }

    public int Level { get; }

    public int MaxLife { get; }

    public int MaxMana { get; }

    public int ArmorClass { get; }

    public int ToHit { get; }

    public IntRange Damage { get; }

    public int BlockChance { get; }

    public ActionTimings Timings { get; }

    public static CombatProfile ForMonster(MonsterDefinition monster, int maxLife) =>
        new CombatProfile(monster.Level, maxLife, 0, monster.ArmorClass, monster.ToHit, monster.Damage, monster.BlockChance, monster.Timings);
}
