using Mortar.Game.Content;

namespace Mortar.Game.Items;

public enum ItemQuality
{
    Normal,
    Magic,
    Unique,
}

public sealed class RolledAffix
{
    internal RolledAffix(AffixDefinition definition, int value)
    {
        Definition = definition;
        Value = value;
    }

    public AffixDefinition Definition { get; }

    public int Value { get; }
}

public sealed class Item
{
    internal Item(
        int id,
        ItemBaseDefinition definition,
        int itemLevel,
        int armorValue,
        RolledAffix? prefix,
        RolledAffix? suffix,
        int stackCount = 1,
        int charges = 0,
        bool identified = true,
        string? uniqueId = null,
        string? uniqueName = null,
        IReadOnlyList<RolledAffix>? uniquePowers = null,
        int uniqueValue = 0)
    {
        Id = id;
        Definition = definition;
        ItemLevel = itemLevel;
        ArmorValue = armorValue;
        Prefix = prefix;
        Suffix = suffix;
        StackCount = stackCount;
        Charges = charges;
        Identified = identified;
        UniqueId = uniqueId;
        UniqueName = uniqueName;
        UniquePowers = uniquePowers ?? Array.Empty<RolledAffix>();
        UniqueValue = uniqueValue;
        MaxDurability = definition.MaxDurability;
        Durability = definition.MaxDurability;
    }

    public int Id { get; }

    public ItemBaseDefinition Definition { get; }

    public int ItemLevel { get; }

    public int ArmorValue { get; }

    public RolledAffix? Prefix { get; }

    public RolledAffix? Suffix { get; }

    public string? UniqueId { get; }

    public string? UniqueName { get; }

    public IReadOnlyList<RolledAffix> UniquePowers { get; }

    public int UniqueValue { get; }

    public int StackCount { get; internal set; }

    public int Charges { get; internal set; }

    public bool Identified { get; internal set; }

    public int Durability { get; internal set; }

    public int MaxDurability { get; internal set; }

    public ItemQuality Quality =>
        UniqueId is not null
            ? ItemQuality.Unique
            : Prefix is null && Suffix is null
                ? ItemQuality.Normal
                : ItemQuality.Magic;

    public string Name
    {
        get
        {
            if (UniqueId is not null)
            {
                return Identified && UniqueName is not null ? UniqueName : Definition.Name;
            }
            if (!Identified && Quality == ItemQuality.Magic)
            {
                return $"Magic {Definition.Slot}";
            }
            var parts = new List<string>(3);
            if (Prefix is not null) parts.Add(Prefix.Definition.Name);
            parts.Add(Definition.Name);
            if (Suffix is not null) parts.Add(Suffix.Definition.Name);
            return string.Join(" ", parts);
        }
    }
}
