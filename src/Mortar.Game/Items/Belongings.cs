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
        if (item.Definition.IsPotion)
        {
            var slot = Belt.FirstEmpty();
            if (slot >= 0) return Belt.TryPut(item, slot);
        }
        return Inventory.TryAdd(item);
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
