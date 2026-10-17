namespace Mortar.Game.Events;

public sealed class VendorSessionEnded : GameEvent
{
    public VendorSessionEnded(long tick, string vendorKind)
        : base(tick)
    {
        VendorKind = vendorKind;
    }

    public string VendorKind { get; }

    protected override string Describe() => $"vendor session ended ({VendorKind})";
}

public sealed class VendorStockRefreshed : GameEvent
{
    public VendorStockRefreshed(long tick)
        : base(tick)
    {
    }

    protected override string Describe() => "vendor stock refreshed";
}

public sealed class ItemBought : GameEvent
{
    public ItemBought(long tick, string vendorKind, int itemId, string name, int price, long goldRemaining)
        : base(tick)
    {
        VendorKind = vendorKind;
        ItemId = itemId;
        Name = name;
        Price = price;
        GoldRemaining = goldRemaining;
    }

    public string VendorKind { get; }

    public int ItemId { get; }

    public string Name { get; }

    public int Price { get; }

    public long GoldRemaining { get; }

    protected override string Describe() =>
        $"bought item {ItemId} '{Name}' from {VendorKind} for {Price} gold (remaining {GoldRemaining})";
}

public sealed class ItemSold : GameEvent
{
    public ItemSold(long tick, string vendorKind, int itemId, string name, int price, long goldTotal)
        : base(tick)
    {
        VendorKind = vendorKind;
        ItemId = itemId;
        Name = name;
        Price = price;
        GoldTotal = goldTotal;
    }

    public string VendorKind { get; }

    public int ItemId { get; }

    public string Name { get; }

    public int Price { get; }

    public long GoldTotal { get; }

    protected override string Describe() =>
        $"sold item {ItemId} '{Name}' to {VendorKind} for {Price} gold (total {GoldTotal})";
}

public sealed class ItemRepaired : GameEvent
{
    public ItemRepaired(long tick, string vendorKind, int itemId, string name, int price, long goldRemaining)
        : base(tick)
    {
        VendorKind = vendorKind;
        ItemId = itemId;
        Name = name;
        Price = price;
        GoldRemaining = goldRemaining;
    }

    public string VendorKind { get; }

    public int ItemId { get; }

    public string Name { get; }

    public int Price { get; }

    public long GoldRemaining { get; }

    protected override string Describe() =>
        $"repaired item {ItemId} '{Name}' at {VendorKind} for {Price} gold (remaining {GoldRemaining})";
}

public sealed class VendorHealed : GameEvent
{
    public VendorHealed(long tick, string vendorKind, int price, int life, long goldRemaining)
        : base(tick)
    {
        VendorKind = vendorKind;
        Price = price;
        Life = life;
        GoldRemaining = goldRemaining;
    }

    public string VendorKind { get; }

    public int Price { get; }

    public int Life { get; }

    public long GoldRemaining { get; }

    protected override string Describe() =>
        $"healed at {VendorKind} for {Price} gold (life {Life}, remaining {GoldRemaining})";
}
