using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class VendorTradeTests
{
    [Fact]
    public void Pricing_BuyIsValueTimesMagicMultWhenMagic()
    {
        var rules = TestGameContent.Rules(vendorMagicBuyMult: 3, vendorSellPercent: 25);
        var plain = TestItems.Create(TestGameContent.Weapon(value: 50));
        var magic = TestItems.Create(
            TestGameContent.Weapon(value: 50),
            prefix: TestItems.Roll(TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon), 3));
        Assert.Equal(50, Pricing.BuyPrice(plain, rules));
        Assert.Equal(150, Pricing.BuyPrice(magic, rules));
        Assert.Equal(12, Pricing.SellPrice(plain, rules));
        Assert.Equal(37, Pricing.SellPrice(magic, rules));
    }

    [Fact]
    public void Buy_RemovesStockStowsAndSpendsGold()
    {
        var world = ArmedAtSmith();
        var stock = StockWeapon(world, value: 50);
        world.Belongings.Gold = 100;

        var events = world.Tick(new Command[] { new BuyCommand(0) });

        Assert.Empty(events.OfType<CommandRejected>());
        Assert.Empty(world.Smith.Stock);
        Assert.NotNull(world.Belongings.Find(stock.Id));
        Assert.Equal(50, world.Belongings.Gold);
        var bought = Assert.Single(events.OfType<ItemBought>());
        Assert.Equal(VendorKinds.Smith, bought.VendorKind);
        Assert.Equal(stock.Id, bought.ItemId);
        Assert.Equal(50, bought.Price);
    }

    [Fact]
    public void Buy_MagicItem_ChargesMagicBuyMult()
    {
        var world = ArmedAtSmith();
        var stock = StockMagicWeapon(world, value: 50);
        world.Belongings.Gold = 200;

        world.Tick(new Command[] { new BuyCommand(0) });

        Assert.Empty(world.Smith.Stock);
        Assert.Equal(50, world.Belongings.Gold);
    }

    [Fact]
    public void Buy_CannotAfford_RejectsWithoutMutating()
    {
        var world = ArmedAtSmith();
        var stock = StockWeapon(world, value: 50);
        world.Belongings.Gold = 49;

        var events = world.Tick(new Command[] { new BuyCommand(0) });

        Assert.Equal("not enough gold", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.Same(stock, Assert.Single(world.Smith.Stock));
        Assert.Equal(49, world.Belongings.Gold);
        Assert.Null(world.Belongings.Find(stock.Id));
        Assert.Empty(events.OfType<ItemBought>());
    }

    [Fact]
    public void Buy_BagFull_RejectsAndKeepsStockAndGold()
    {
        var world = ArmedAtSmith();
        var stock = StockWeapon(world, value: 10);
        world.Belongings.Gold = 100;
        for (var id = 100; id < 110; id++)
        {
            Assert.True(world.Belongings.Inventory.TryAdd(TestItems.Create(TestGameContent.Armor(id: $"vest-{id}"), id)));
        }

        var events = world.Tick(new Command[] { new BuyCommand(0) });

        Assert.Equal(PickupSystem.InventoryFull, Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.Same(stock, Assert.Single(world.Smith.Stock));
        Assert.Equal(100, world.Belongings.Gold);
        Assert.Null(world.Belongings.Find(stock.Id));
    }

    [Fact]
    public void Buy_WithoutSession_IsRejected()
    {
        var world = TestWorlds.CreateWithTown();
        world.Level.Move(world.Player, new Cell(2, 1));
        StockWeapon(world, value: 10);
        world.Belongings.Gold = 100;

        var events = world.Tick(new Command[] { new BuyCommand(0) });

        Assert.Equal("no active vendor", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.Single(world.Smith.Stock);
        Assert.Equal(100, world.Belongings.Gold);
    }

    [Fact]
    public void Buy_InvalidStockIndex_IsRejected()
    {
        var world = ArmedAtSmith();
        world.Belongings.Gold = 100;

        var events = world.Tick(new Command[] { new BuyCommand(0) });

        Assert.Equal("no such stock item", Assert.Single(events.OfType<CommandRejected>()).Reason);
    }

    [Fact]
    public void Sell_TakesItemAndGrantsGold()
    {
        var world = ArmedAtSmith();
        var item = TestWorlds.Give(world, TestGameContent.Weapon(value: 50));
        world.Belongings.Gold = 10;

        var events = world.Tick(new Command[] { new SellCommand(item.Id) });

        Assert.Empty(events.OfType<CommandRejected>());
        Assert.Null(world.Belongings.Find(item.Id));
        Assert.Equal(22, world.Belongings.Gold);
        Assert.Empty(world.Smith.Stock);
        var sold = Assert.Single(events.OfType<ItemSold>());
        Assert.Equal(VendorKinds.Smith, sold.VendorKind);
        Assert.Equal(item.Id, sold.ItemId);
        Assert.Equal(12, sold.Price);
    }

    [Fact]
    public void Sell_WrongVendor_RejectsWithoutTakingItem()
    {
        var world = ArmedAtSmith();
        var potion = TestWorlds.Give(world, TestGameContent.Potion(value: 40));
        world.Belongings.Gold = 0;

        var events = world.Tick(new Command[] { new SellCommand(potion.Id) });

        Assert.Equal("vendor will not buy that", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.NotNull(world.Belongings.Find(potion.Id));
        Assert.Equal(0, world.Belongings.Gold);
        Assert.Empty(events.OfType<ItemSold>());
    }

    [Fact]
    public void Sell_HealerAcceptsPotions()
    {
        var world = ArmedAt(VendorKinds.Healer, new Cell(3, 1));
        var potion = TestWorlds.Give(world, TestGameContent.Potion(value: 40));
        world.Belongings.Gold = 0;

        var events = world.Tick(new Command[] { new SellCommand(potion.Id) });

        Assert.Empty(events.OfType<CommandRejected>());
        Assert.Null(world.Belongings.Find(potion.Id));
        Assert.Equal(10, world.Belongings.Gold);
    }

    [Fact]
    public void Sell_MissingItem_IsRejected()
    {
        var world = ArmedAtSmith();
        var events = world.Tick(new Command[] { new SellCommand(999) });
        Assert.Equal("no such item", Assert.Single(events.OfType<CommandRejected>()).Reason);
    }

    private static World ArmedAtSmith() => ArmedAt(VendorKinds.Smith, new Cell(2, 1));

    private static World ArmedAt(string vendorKind, Cell cell)
    {
        var world = TestWorlds.CreateWithTown();
        world.Level.Move(world.Player, cell);
        world.Tick(new Command[] { new TalkCommand(vendorKind) });
        Assert.Equal(vendorKind, world.ActiveVendor);
        return world;
    }

    private static Item StockWeapon(World world, int value)
    {
        var item = ItemFactory.CreatePlain(world, TestGameContent.Weapon(value: value));
        world.Smith.MutableStock.Add(item);
        return item;
    }

    private static Item StockMagicWeapon(World world, int value)
    {
        var affix = TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon);
        var item = new Item(
            world.NextItemId++,
            TestGameContent.Weapon(value: value),
            1,
            0,
            TestItems.Roll(affix, 3),
            null);
        item.Identified = true;
        world.Smith.MutableStock.Add(item);
        return item;
    }
}
