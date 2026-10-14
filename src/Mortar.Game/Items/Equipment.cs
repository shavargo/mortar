using Mortar.Game.Content;

namespace Mortar.Game.Items;

public enum EquipmentSlot
{
    Head,
    Body,
    Weapon,
    Shield,
    LeftRing,
    RightRing,
    Amulet,
}

public sealed class Equipment
{
    private readonly Item?[] _items = new Item?[Slots.Count];

    public static IReadOnlyList<EquipmentSlot> Slots { get; } = new[]
    {
        EquipmentSlot.Head, EquipmentSlot.Body, EquipmentSlot.Weapon, EquipmentSlot.Shield,
        EquipmentSlot.LeftRing, EquipmentSlot.RightRing, EquipmentSlot.Amulet,
    };

    public IEnumerable<Item> Worn => Slots.Select(Get).OfType<Item>();

    public Item? Get(EquipmentSlot slot) => _items[(int)slot];

    public EquipmentSlot? SlotOf(int itemId)
    {
        foreach (var slot in Slots)
        {
            if (_items[(int)slot]?.Id == itemId) return slot;
        }
        return null;
    }

    public EquipmentSlot SlotFor(ItemBaseDefinition definition) => definition.Slot switch
    {
        ItemSlots.Head => EquipmentSlot.Head,
        ItemSlots.Body => EquipmentSlot.Body,
        ItemSlots.Weapon => EquipmentSlot.Weapon,
        ItemSlots.Shield => EquipmentSlot.Shield,
        ItemSlots.Amulet => EquipmentSlot.Amulet,
        ItemSlots.Ring => RingSlot(),
        _ => throw new ArgumentException($"'{definition.Id}' is not equippable", nameof(definition)),
    };

    public static bool Accepts(EquipmentSlot slot, ItemBaseDefinition definition) => definition.Slot == SlotName(slot);

    internal void Set(EquipmentSlot slot, Item? item) => _items[(int)slot] = item;

    private EquipmentSlot RingSlot()
    {
        if (Get(EquipmentSlot.LeftRing) is null) return EquipmentSlot.LeftRing;
        return Get(EquipmentSlot.RightRing) is null ? EquipmentSlot.RightRing : EquipmentSlot.LeftRing;
    }

    private static string SlotName(EquipmentSlot slot) => slot switch
    {
        EquipmentSlot.Head => ItemSlots.Head,
        EquipmentSlot.Body => ItemSlots.Body,
        EquipmentSlot.Weapon => ItemSlots.Weapon,
        EquipmentSlot.Shield => ItemSlots.Shield,
        EquipmentSlot.Amulet => ItemSlots.Amulet,
        _ => ItemSlots.Ring,
    };
}
