namespace Mortar.Game.Events;

public sealed class StaffRecharged : GameEvent
{
    public StaffRecharged(long tick, int itemId, int charges, int manaSpent)
        : base(tick)
    {
        ItemId = itemId;
        Charges = charges;
        ManaSpent = manaSpent;
    }

    public int ItemId { get; }

    public int Charges { get; }

    public int ManaSpent { get; }

    protected override string Describe() => $"recharged staff #{ItemId} to {Charges} (spent {ManaSpent} mana)";
}

public sealed class ItemFieldRepaired : GameEvent
{
    public ItemFieldRepaired(long tick, int itemId, int durability, int maxDurability)
        : base(tick)
    {
        ItemId = itemId;
        Durability = durability;
        MaxDurability = maxDurability;
    }

    public int ItemId { get; }

    public int Durability { get; }

    public int MaxDurability { get; }

    protected override string Describe() =>
        $"field-repaired item #{ItemId} to {Durability}/{MaxDurability}";
}

public sealed class ItemDestroyed : GameEvent
{
    public ItemDestroyed(long tick, int itemId, string name)
        : base(tick)
    {
        ItemId = itemId;
        Name = name;
    }

    public int ItemId { get; }

    public string Name { get; }

    protected override string Describe() => $"destroyed item #{ItemId} '{Name}'";
}
