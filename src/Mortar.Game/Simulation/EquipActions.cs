using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class EquipActions
{
    public static string? Equip(World world, int itemId, EventSink events)
    {
        var belongings = world.Belongings;
        var stored = belongings.Inventory.Find(itemId);
        if (stored is null)
        {
            return belongings.Equipment.SlotOf(itemId) is null ? "no such item in the inventory" : "item is already equipped";
        }
        var item = stored.Item;
        if (!item.Definition.IsEquippable) return "item cannot be equipped";
        var missing = MissingRequirement(world.Progress.BaseStats, item.Definition.Requirements);
        if (missing is not null) return missing;
        var slot = belongings.Equipment.SlotFor(item.Definition);
        var replaced = belongings.Equipment.Get(slot);
        belongings.Inventory.Remove(itemId);
        if (replaced is not null && !belongings.Inventory.TryAdd(replaced))
        {
            belongings.Inventory.TryPlace(item, stored.Cell);
            return "no room in the inventory for the item being replaced";
        }
        belongings.Equipment.Set(slot, item);
        world.RefreshPlayer();
        if (replaced is not null) events.Add(new ItemUnequipped(events.Tick, replaced.Id, slot));
        events.Add(new ItemEquipped(events.Tick, item.Id, slot));
        return null;
    }

    public static string? Unequip(World world, EquipmentSlot slot, EventSink events)
    {
        if (!Equipment.Slots.Contains(slot)) return "unknown equipment slot";
        var belongings = world.Belongings;
        var item = belongings.Equipment.Get(slot);
        if (item is null) return "nothing is equipped there";
        if (!belongings.Inventory.TryAdd(item)) return PickupSystem.InventoryFull;
        belongings.Equipment.Set(slot, null);
        world.RefreshPlayer();
        events.Add(new ItemUnequipped(events.Tick, item.Id, slot));
        return null;
    }

    private static string? MissingRequirement(Stats stats, Stats requirements)
    {
        foreach (var kind in Stats.Kinds)
        {
            var needed = requirements.Get(kind);
            if (stats.Get(kind) < needed) return $"requires {needed} {kind.ToString().ToLowerInvariant()}";
        }
        return null;
    }
}
