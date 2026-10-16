namespace Mortar.Game.Saves;

public sealed class ItemSnapshot
{
    public int Id { get; set; }

    public string? Base { get; set; }

    public int ItemLevel { get; set; }

    public int Armor { get; set; }

    public int StackCount { get; set; } = 1;

    public int Charges { get; set; }

    public AffixRollSnapshot? Prefix { get; set; }

    public AffixRollSnapshot? Suffix { get; set; }
}

public sealed class AffixRollSnapshot
{
    public string? Id { get; set; }

    public int Value { get; set; }
}

public sealed class StoredItemSnapshot
{
    public int X { get; set; }

    public int Y { get; set; }

    public ItemSnapshot? Item { get; set; }
}

public sealed class BeltItemSnapshot
{
    public int Slot { get; set; }

    public ItemSnapshot? Item { get; set; }
}

public sealed class EquippedItemSnapshot
{
    public string? Slot { get; set; }

    public ItemSnapshot? Item { get; set; }
}

public sealed class GroundItemSnapshot
{
    public int Id { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public int Gold { get; set; }

    public ItemSnapshot? Item { get; set; }
}
