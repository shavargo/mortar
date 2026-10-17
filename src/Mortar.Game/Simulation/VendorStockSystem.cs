using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class VendorStockSystem
{
    public static void Initialize(World world)
    {
        RefillAll(world);
        world.NextStockRegenTick = world.TickCount + world.Content.Rules.VendorStockRegenTicks;
    }

    public static void Update(World world, EventSink events)
    {
        if (world.TickCount < world.NextStockRegenTick) return;
        RefillAll(world);
        world.NextStockRegenTick += world.Content.Rules.VendorStockRegenTicks;
        events.Add(new VendorStockRefreshed(events.Tick));
    }

    private static void RefillAll(World world)
    {
        var level = ShopItemLevel(world);
        var rules = world.Content.Rules;
        Fill(world, world.Smith, level, rules.VendorSmithSlots, rules.VendorSmithMagicPercent);
        Fill(world, world.Healer, level, rules.VendorHealerSlots, magicPercent: 0);
        Fill(world, world.Sage, level, rules.VendorSageSlots, magicPercent: 0);
    }

    private static void Fill(World world, VendorState vendor, int itemLevel, int slots, int magicPercent)
    {
        vendor.MutableStock.Clear();
        var pool = PoolFor(world, vendor.Kind, itemLevel);
        if (pool.Count == 0 || slots < 1) return;
        for (var i = 0; i < slots; i++)
        {
            var item = ItemFactory.RollFrom(world, itemLevel, magicPercent, pool);
            if (item is null) break;
            item.Identified = true;
            vendor.MutableStock.Add(item);
        }
    }

    private static List<ItemBaseDefinition> PoolFor(World world, string vendorKind, int itemLevel)
    {
        var pool = new List<ItemBaseDefinition>();
        foreach (var definition in world.Content.ItemBases)
        {
            if (definition.DropWeight <= 0 || definition.ItemLevel > itemLevel) continue;
            if (!VendorKinds.Accepts(vendorKind, definition.Slot)) continue;
            pool.Add(definition);
        }
        return pool;
    }

    internal static int ShopItemLevel(World world)
    {
        var deepest = 0;
        foreach (var depth in world.VisitedDepths)
        {
            if (depth > deepest) deepest = depth;
        }
        return deepest < 1 ? 1 : deepest;
    }
}
