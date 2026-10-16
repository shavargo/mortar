namespace Mortar.Game.Items;

internal static class Ammo
{
    public static bool Has(Belongings belongings, string ammoId) => Find(belongings, ammoId) is not null;

    public static bool TrySpend(Belongings belongings, string ammoId)
    {
        var found = Find(belongings, ammoId);
        if (found is null) return false;
        found.Item.StackCount--;
        if (found.Item.StackCount > 0) return true;
        if (found.BeltSlot >= 0)
        {
            belongings.Belt.Take(found.BeltSlot);
            return true;
        }
        belongings.Inventory.Remove(found.Item.Id);
        return true;
    }

    private static AmmoStack? Find(Belongings belongings, string ammoId)
    {
        for (var slot = 0; slot < Belt.Slots; slot++)
        {
            var item = belongings.Belt.Get(slot);
            if (item is not null && item.Definition.Id == ammoId && item.StackCount > 0)
            {
                return new AmmoStack(item, slot);
            }
        }
        foreach (var stored in belongings.Inventory.Items)
        {
            if (stored.Item.Definition.Id == ammoId && stored.Item.StackCount > 0)
            {
                return new AmmoStack(stored.Item, -1);
            }
        }
        return null;
    }

    private sealed class AmmoStack
    {
        public AmmoStack(Item item, int beltSlot)
        {
            Item = item;
            BeltSlot = beltSlot;
        }

        public Item Item { get; }

        public int BeltSlot { get; }
    }
}
