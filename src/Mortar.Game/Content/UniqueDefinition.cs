namespace Mortar.Game.Content;

public sealed class UniquePower
{
    public UniquePower(string affixId, int value)
    {
        AffixId = affixId;
        Value = value;
    }

    public string AffixId { get; }

    public int Value { get; }
}

public sealed class UniqueDefinition
{
    public UniqueDefinition(
        string id,
        string name,
        string baseItemId,
        int minLevel,
        IReadOnlyList<UniquePower> powers,
        int value = 0)
    {
        Id = id;
        Name = name;
        BaseItemId = baseItemId;
        MinLevel = minLevel;
        Powers = powers.ToArray();
        Value = value;
    }

    public string Id { get; }

    public string Name { get; }

    public string BaseItemId { get; }

    public int MinLevel { get; }

    public IReadOnlyList<UniquePower> Powers { get; }

    public int Value { get; }
}
