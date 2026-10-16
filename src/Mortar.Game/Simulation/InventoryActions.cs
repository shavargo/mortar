using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class InventoryActions
{
    public static string? Drop(World world, int itemId, EventSink events)
    {
        var item = world.Belongings.Take(itemId, out var fromSlot);
        if (item is null) return "no such item";
        if (fromSlot is { } slot)
        {
            world.RefreshPlayer();
            events.Add(new ItemUnequipped(events.Tick, item.Id, slot));
        }
        LootSystem.Place(world, world.Player.Cell, item, 0, events);
        return null;
    }

    public static string? Move(World world, int itemId, Cell to, EventSink events)
    {
        var belongings = world.Belongings;
        var stored = belongings.Inventory.Find(itemId);
        var beltSlot = belongings.Belt.IndexOf(itemId);
        var item = stored?.Item ?? belongings.Belt.Get(beltSlot);
        if (item is null) return "no such item in the inventory or belt";
        if (!belongings.Inventory.Fits(item.Definition, to, itemId)) return "item does not fit there";
        if (stored is not null) belongings.Inventory.Remove(itemId);
        else belongings.Belt.Take(beltSlot);
        belongings.Inventory.TryPlace(item, to);
        events.Add(new ItemMoved(events.Tick, item.Id, to));
        return null;
    }

    public static string? PutInBelt(World world, int itemId, int slot, EventSink events)
    {
        if (slot < 0 || slot >= Belt.Slots) return BadSlot;
        var belongings = world.Belongings;
        var current = belongings.Belt.IndexOf(itemId);
        var item = belongings.Inventory.Find(itemId)?.Item ?? belongings.Belt.Get(current);
        if (item is null) return "no such item in the inventory or belt";
        if (!item.Definition.IsPotion && !item.Definition.IsAmmo) return "only potions and ammo go in the belt";
        if (belongings.Belt.Get(slot) is not null) return "that belt slot is taken";
        if (current >= 0) belongings.Belt.Take(current);
        else belongings.Inventory.Remove(itemId);
        belongings.Belt.TryPut(item, slot);
        events.Add(new ItemBelted(events.Tick, item.Id, slot));
        return null;
    }

    public static string? UseBelt(World world, int slot, EventSink events)
    {
        if (slot < 0 || slot >= Belt.Slots) return BadSlot;
        var item = world.Belongings.Belt.Take(slot);
        if (item is null) return "that belt slot is empty";
        var potion = item.Definition.Potion!;
        var player = world.Player;
        int restored;
        if (potion.Restores == PotionKinds.Life)
        {
            restored = Restorable(player.Life, player.Profile.MaxLife, potion.Amount);
            player.Life += restored;
        }
        else
        {
            restored = Restorable(player.Mana, player.Profile.MaxMana, potion.Amount);
            player.Mana += restored;
        }
        events.Add(new PotionDrunk(events.Tick, item.Id, potion.Restores, restored));
        return null;
    }

    private static string BadSlot => $"belt slot must be between 0 and {Belt.Slots - 1}";

    private static int Restorable(int current, int maximum, int amount) => Math.Max(0, Math.Min(amount, maximum - current));
}
