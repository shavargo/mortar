namespace Mortar.Game.Content;

public static class AffixPositions
{
    public const string Prefix = "prefix";
    public const string Suffix = "suffix";

    public static bool IsKnown(string position) => position == Prefix || position == Suffix;
}

public sealed class AffixDefinition
{
    public AffixDefinition(
        string id,
        string name,
        string position,
        IReadOnlyList<string> slots,
        IntRange itemLevel,
        int weight,
        string bonus,
        IntRange value)
    {
        Id = id;
        Name = name;
        Position = position;
        Slots = slots.ToArray();
        ItemLevel = itemLevel;
        Weight = weight;
        Bonus = bonus;
        Value = value;
    }

    public string Id { get; }

    public string Name { get; }

    public string Position { get; }

    public IReadOnlyList<string> Slots { get; }

    public IntRange ItemLevel { get; }

    public int Weight { get; }

    public string Bonus { get; }

    public IntRange Value { get; }

    public bool Allows(string slot, int itemLevel) =>
        Slots.Contains(slot) && itemLevel >= ItemLevel.Min && itemLevel <= ItemLevel.Max;
}
