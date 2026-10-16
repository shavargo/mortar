namespace Mortar.Game.Items;

public sealed class Belongings
{
    public Inventory Inventory { get; } = new Inventory();

    public Belt Belt { get; } = new Belt();

    public Equipment Equipment { get; } = new Equipment();

    public long Gold { get; internal set; }

    public Item? Find(int itemId)
    {
        var worn = Equipment.SlotOf(itemId);
        return Inventory.Find(itemId)?.Item ?? Belt.Get(Belt.IndexOf(itemId)) ?? (worn is { } slot ? Equipment.Get(slot) : null);
    }

    internal bool Stow(Item item)
    {
        if (item.Definition.IsStackable && MergeStacks(item))
        {
            return true;
        }
        if (item.Definition.IsPotion || item.Definition.IsAmmo)
        {
            var slot = Belt.FirstEmpty();
            if (slot >= 0) return Belt.TryPut(item, slot);
        }
        return Inventory.TryAdd(item);
    }

    private bool MergeStacks(Item incoming)
    {
        for (var slot = 0; slot < Belt.Slots && incoming.StackCount > 0; slot++)
        {
            MergeInto(Belt.Get(slot), incoming);
        }
        foreach (var stored in Inventory.Items)
        {
            if (incoming.StackCount <= 0) break;
            MergeInto(stored.Item, incoming);
        }
        return incoming.StackCount <= 0;
    }

    private static void MergeInto(Item? existing, Item incoming)
    {
        if (existing is null || existing.Definition.Id != incoming.Definition.Id) return;
        var room = existing.Definition.StackMax - existing.StackCount;
        if (room <= 0) return;
        var moved = Math.Min(room, incoming.StackCount);
        existing.StackCount += moved;
        incoming.StackCount -= moved;
    }

    internal Item? Take(int itemId, out EquipmentSlot? fromSlot)
    {
        fromSlot = Equipment.SlotOf(itemId);
        if (fromSlot is { } slot)
        {
            var worn = Equipment.Get(slot);
            Equipment.Set(slot, null);
            return worn;
        }
        var stored = Inventory.Find(itemId);
        if (stored is not null)
        {
            Inventory.Remove(itemId);
            return stored.Item;
        }
        var beltSlot = Belt.IndexOf(itemId);
        return beltSlot >= 0 ? Belt.Take(beltSlot) : null;
    }
}
