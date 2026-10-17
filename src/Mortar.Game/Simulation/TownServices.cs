using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class TownServices
{
    public static string? Buy(World world, int stockIndex, EventSink events)
    {
        var sessionError = VendorSession.RequireActive(world);
        if (sessionError is not null) return sessionError;
        var vendor = Vendor(world, world.ActiveVendor!);
        if (stockIndex < 0 || stockIndex >= vendor.MutableStock.Count) return "no such stock item";
        var item = vendor.MutableStock[stockIndex];
        var price = Pricing.BuyPrice(item, world.Content.Rules);
        if (world.Belongings.Gold < price) return "not enough gold";
        vendor.MutableStock.RemoveAt(stockIndex);
        if (!world.Belongings.Stow(item))
        {
            vendor.MutableStock.Insert(stockIndex, item);
            return PickupSystem.InventoryFull;
        }
        world.Belongings.Gold -= price;
        events.Add(new ItemBought(events.Tick, vendor.Kind, item.Id, item.Name, price, world.Belongings.Gold));
        return null;
    }

    public static string? Sell(World world, int itemId, EventSink events)
    {
        var sessionError = VendorSession.RequireActive(world);
        if (sessionError is not null) return sessionError;
        var vendorKind = world.ActiveVendor!;
        var held = world.Belongings.Find(itemId);
        if (held is null) return "no such item";
        if (!VendorKinds.Accepts(vendorKind, held.Definition.Slot)) return "vendor will not buy that";
        var price = Pricing.SellPrice(held, world.Content.Rules);
        var item = world.Belongings.Take(itemId, out var fromSlot);
        if (item is null) return "no such item";
        if (fromSlot is not null) world.RefreshPlayer();
        world.Belongings.Gold += price;
        events.Add(new ItemSold(events.Tick, vendorKind, item.Id, item.Name, price, world.Belongings.Gold));
        return null;
    }

    public static string? Repair(World world, int itemId, EventSink events)
    {
        var vendorError = RequireVendor(world, VendorKinds.Smith);
        if (vendorError is not null) return vendorError;
        var item = world.Belongings.Find(itemId);
        if (item is null) return "no such item";
        if (item.MaxDurability <= 0) return "item cannot be repaired";
        if (item.Durability >= item.MaxDurability) return "item does not need repair";
        var price = Pricing.RepairCost(item, world.Content.Rules);
        if (world.Belongings.Gold < price) return "not enough gold";
        world.Belongings.Gold -= price;
        item.Durability = item.MaxDurability;
        world.RefreshPlayer();
        events.Add(new ItemRepaired(events.Tick, VendorKinds.Smith, item.Id, item.Name, price, world.Belongings.Gold));
        return null;
    }

    public static string? Identify(World world, int itemId, EventSink events)
    {
        var vendorError = RequireVendor(world, VendorKinds.Sage);
        if (vendorError is not null) return vendorError;
        var item = world.Belongings.Find(itemId);
        if (item is null) return "no such item";
        if (item.Identified) return "item is already identified";
        var price = world.Content.Rules.VendorIdentifyFee;
        if (world.Belongings.Gold < price) return "not enough gold";
        var error = SpellSystem.IdentifyItem(world, itemId, events);
        if (error is not null) return error;
        world.Belongings.Gold -= price;
        return null;
    }

    public static string? Heal(World world, EventSink events)
    {
        var vendorError = RequireVendor(world, VendorKinds.Healer);
        if (vendorError is not null) return vendorError;
        var player = world.Player;
        if (player.Life >= player.Profile.MaxLife) return "already at full life";
        var price = world.Content.Rules.VendorHealFee;
        if (world.Belongings.Gold < price) return "not enough gold";
        world.Belongings.Gold -= price;
        player.Life = player.Profile.MaxLife;
        events.Add(new VendorHealed(events.Tick, VendorKinds.Healer, price, player.Life, world.Belongings.Gold));
        return null;
    }

    private static string? RequireVendor(World world, string kind)
    {
        var sessionError = VendorSession.RequireActive(world);
        if (sessionError is not null) return sessionError;
        if (world.ActiveVendor != kind) return "wrong vendor";
        return null;
    }

    private static VendorState Vendor(World world, string kind) =>
        kind switch
        {
            VendorKinds.Smith => world.Smith,
            VendorKinds.Healer => world.Healer,
            VendorKinds.Sage => world.Sage,
            _ => throw new InvalidOperationException($"unknown vendor '{kind}'"),
        };
}
