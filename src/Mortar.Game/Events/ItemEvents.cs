namespace Mortar.Game.Events;

public sealed class ItemDropped : GameEvent
{
    public ItemDropped(long tick, int groundItemId, Cell cell, string description)
        : base(tick)
    {
        GroundItemId = groundItemId;
        Cell = cell;
        Description = description;
    }

    public int GroundItemId { get; }

    public Cell Cell { get; }

    public string Description { get; }

    protected override string Describe() => $"item {GroundItemId} '{Description}' dropped at {Cell}";
}

public sealed class ItemPickedUp : GameEvent
{
    public ItemPickedUp(long tick, int itemId, string name)
        : base(tick)
    {
        ItemId = itemId;
        Name = name;
    }

    public int ItemId { get; }

    public string Name { get; }

    protected override string Describe() => $"picked up item {ItemId} '{Name}'";
}

public sealed class GoldPickedUp : GameEvent
{
    public GoldPickedUp(long tick, int amount, long total)
        : base(tick)
    {
        Amount = amount;
        Total = total;
    }

    public int Amount { get; }

    public long Total { get; }

    protected override string Describe() => $"picked up {Amount} gold (total {Total})";
}

public sealed class PickupFailed : GameEvent
{
    public PickupFailed(long tick, int groundItemId, string reason)
        : base(tick)
    {
        GroundItemId = groundItemId;
        Reason = reason;
    }

    public int GroundItemId { get; }

    public string Reason { get; }

    protected override string Describe() => $"could not pick up item {GroundItemId}: {Reason}";
}
