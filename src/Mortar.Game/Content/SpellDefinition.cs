namespace Mortar.Game.Content;

public sealed class SpellDefinition
{
    public SpellDefinition(
        string id,
        string name,
        string shape,
        int manaCost,
        int castTicks,
        int hitFrame,
        int range,
        IntRange? damage,
        int healAmount,
        int radius,
        int damagePerLevel,
        int healPerLevel)
    {
        Id = id;
        Name = name;
        Shape = shape;
        ManaCost = manaCost;
        CastTicks = castTicks;
        HitFrame = hitFrame;
        Range = range;
        Damage = damage;
        HealAmount = healAmount;
        Radius = radius;
        DamagePerLevel = damagePerLevel;
        HealPerLevel = healPerLevel;
    }

    public string Id { get; }

    public string Name { get; }

    public string Shape { get; }

    public int ManaCost { get; }

    public int CastTicks { get; }

    public int HitFrame { get; }

    public int Range { get; }

    public IntRange? Damage { get; }

    public int HealAmount { get; }

    public int Radius { get; }

    public int DamagePerLevel { get; }

    public int HealPerLevel { get; }
}
