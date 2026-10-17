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

    private static VendorState Vendor(World world, string kind) =>
        kind switch
        {
            VendorKinds.Smith => world.Smith,
            VendorKinds.Healer => world.Healer,
            VendorKinds.Sage => world.Sage,
            _ => throw new InvalidOperationException($"unknown vendor '{kind}'"),
        };
}
