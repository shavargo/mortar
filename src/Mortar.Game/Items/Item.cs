using Mortar.Game.Content;

namespace Mortar.Game.Items;

public enum ItemQuality
{
    Normal,
    Magic,
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
        int stackCount = 1)
    {
        Id = id;
        Definition = definition;
        ItemLevel = itemLevel;
        ArmorValue = armorValue;
        Prefix = prefix;
        Suffix = suffix;
        StackCount = stackCount;
    }

    public int Id { get; }

    public ItemBaseDefinition Definition { get; }

    public int ItemLevel { get; }

    public int ArmorValue { get; }

    public RolledAffix? Prefix { get; }

    public RolledAffix? Suffix { get; }

    public int StackCount { get; internal set; }

    public ItemQuality Quality => Prefix is null && Suffix is null ? ItemQuality.Normal : ItemQuality.Magic;

    public string Name
    {
        get
        {
            var parts = new List<string>(3);
            if (Prefix is not null) parts.Add(Prefix.Definition.Name);
            parts.Add(Definition.Name);
            if (Suffix is not null) parts.Add(Suffix.Definition.Name);
            return string.Join(" ", parts);
        }
    }
}
