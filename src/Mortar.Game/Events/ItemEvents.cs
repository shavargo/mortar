using Mortar.Game.Items;

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

public sealed class ItemEquipped : GameEvent
{
    public ItemEquipped(long tick, int itemId, EquipmentSlot slot)
        : base(tick)
    {
        ItemId = itemId;
        Slot = slot;
    }

    public int ItemId { get; }

    public EquipmentSlot Slot { get; }

    protected override string Describe() => $"equipped item {ItemId} as {Slot}";
}

public sealed class ItemUnequipped : GameEvent
{
    public ItemUnequipped(long tick, int itemId, EquipmentSlot slot)
        : base(tick)
    {
        ItemId = itemId;
        Slot = slot;
    }

    public int ItemId { get; }

    public EquipmentSlot Slot { get; }

    protected override string Describe() => $"unequipped item {ItemId} from {Slot}";
}

public sealed class ItemMoved : GameEvent
{
    public ItemMoved(long tick, int itemId, Cell cell)
        : base(tick)
    {
        ItemId = itemId;
        Cell = cell;
    }

    public int ItemId { get; }

    public Cell Cell { get; }

    protected override string Describe() => $"moved item {ItemId} to {Cell}";
}

public sealed class ItemBelted : GameEvent
{
    public ItemBelted(long tick, int itemId, int slot)
        : base(tick)
    {
        ItemId = itemId;
        Slot = slot;
    }

    public int ItemId { get; }

    public int Slot { get; }

    protected override string Describe() => $"put item {ItemId} in belt slot {Slot}";
}

public sealed class PotionDrunk : GameEvent
{
    public PotionDrunk(long tick, int itemId, string restores, int amount)
        : base(tick)
    {
        ItemId = itemId;
        Restores = restores;
        Amount = amount;
    }

    public int ItemId { get; }

    public string Restores { get; }

    public int Amount { get; }

    protected override string Describe() => $"drank item {ItemId}, restoring {Amount} {Restores}";
}
