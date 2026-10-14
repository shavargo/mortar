namespace Mortar.Game.Content;

public enum StatKind
{
    Strength,
    Dexterity,
    Vitality,
    Magic,
}

public readonly struct Stats
{
    public Stats(int strength, int dexterity, int vitality, int magic)
    {
        Strength = strength;
        Dexterity = dexterity;
        Vitality = vitality;
        Magic = magic;
    }

    public static IReadOnlyList<StatKind> Kinds { get; } =
        new[] { StatKind.Strength, StatKind.Dexterity, StatKind.Vitality, StatKind.Magic };

    public int Strength { get; }

    public int Dexterity { get; }

    public int Vitality { get; }

    public int Magic { get; }

    public int Get(StatKind kind) => kind switch
    {
        StatKind.Strength => Strength,
        StatKind.Dexterity => Dexterity,
        StatKind.Vitality => Vitality,
        _ => Magic,
    };

    public Stats With(StatKind kind, int value) => kind switch
    {
        StatKind.Strength => new Stats(value, Dexterity, Vitality, Magic),
        StatKind.Dexterity => new Stats(Strength, value, Vitality, Magic),
        StatKind.Vitality => new Stats(Strength, Dexterity, value, Magic),
        _ => new Stats(Strength, Dexterity, Vitality, value),
    };
}
