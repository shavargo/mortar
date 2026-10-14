using Mortar.Game.Items;

namespace Mortar.Game.Commands;

public sealed class PickUpCommand : Command
{
    public PickUpCommand(int groundItemId)
    {
        GroundItemId = groundItemId;
    }

    public int GroundItemId { get; }

    public override string Describe() => $"pick up item {GroundItemId}";
}

public sealed class EquipCommand : Command
{
    public EquipCommand(int itemId)
    {
        ItemId = itemId;
    }

    public int ItemId { get; }

    public override string Describe() => $"equip item {ItemId}";
}

public sealed class UnequipCommand : Command
{
    public UnequipCommand(EquipmentSlot slot)
    {
        Slot = slot;
    }

    public EquipmentSlot Slot { get; }

    public override string Describe() => $"unequip {Slot}";
}

public sealed class DropItemCommand : Command
{
    public DropItemCommand(int itemId)
    {
        ItemId = itemId;
    }

    public int ItemId { get; }

    public override string Describe() => $"drop item {ItemId}";
}

public sealed class MoveItemCommand : Command
{
    public MoveItemCommand(int itemId, Cell to)
    {
        ItemId = itemId;
        To = to;
    }

    public int ItemId { get; }

    public Cell To { get; }

    public override string Describe() => $"move item {ItemId} to {To}";
}

public sealed class PutInBeltCommand : Command
{
    public PutInBeltCommand(int itemId, int slot)
    {
        ItemId = itemId;
        Slot = slot;
    }

    public int ItemId { get; }

    public int Slot { get; }

    public override string Describe() => $"put item {ItemId} in belt slot {Slot}";
}

public sealed class UseBeltCommand : Command
{
    public UseBeltCommand(int slot)
    {
        Slot = slot;
    }

    public int Slot { get; }

    public override string Describe() => $"use belt slot {Slot}";
}
