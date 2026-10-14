namespace Mortar.Game.Items;

public sealed class Belt
{
    public const int Slots = 8;

    private readonly Item?[] _slots = new Item?[Slots];

    public Item? Get(int slot) => slot >= 0 && slot < Slots ? _slots[slot] : null;

    public int IndexOf(int itemId) => Array.FindIndex(_slots, item => item is not null && item.Id == itemId);

    public int FirstEmpty() => Array.FindIndex(_slots, item => item is null);

    internal bool TryPut(Item item, int slot)
    {
        if (slot < 0 || slot >= Slots || _slots[slot] is not null || !item.Definition.IsPotion) return false;
        _slots[slot] = item;
        return true;
    }

    internal Item? Take(int slot)
    {
        var item = Get(slot);
        if (item is not null) _slots[slot] = null;
        return item;
    }
}
