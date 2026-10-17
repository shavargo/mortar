using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class VendorServiceTests
{
    [Fact]
    public void Pricing_RepairIsValueTimesMissingOverMaxTimesPercent()
    {
        var rules = TestGameContent.Rules(vendorRepairPercent: 50);
        var item = TestItems.Create(TestGameContent.Weapon(value: 50, maxDurability: 30));
        item.Durability = 15;
        Assert.Equal(12, Pricing.RepairCost(item, rules));
        item.Durability = 29;
        Assert.Equal(1, Pricing.RepairCost(item, rules));
        item.Durability = 30;
        Assert.Equal(0, Pricing.RepairCost(item, rules));
    }

    [Fact]
    public void Repair_PaysFeeAndRestoresDurability()
    {
        var world = ArmedAt(VendorKinds.Smith, new Cell(2, 1));
        var item = TestWorlds.Give(world, TestGameContent.Weapon(value: 50, maxDurability: 30));
        item.Durability = 15;
        world.Belongings.Gold = 20;

        var events = world.Tick(new Command[] { new RepairCommand(item.Id) });

        Assert.Empty(events.OfType<CommandRejected>());
        Assert.Equal(30, item.Durability);
        Assert.Equal(8, world.Belongings.Gold);
        var repaired = Assert.Single(events.OfType<ItemRepaired>());
        Assert.Equal(VendorKinds.Smith, repaired.VendorKind);
        Assert.Equal(item.Id, repaired.ItemId);
        Assert.Equal(12, repaired.Price);
    }

    [Fact]
    public void Repair_FullDurability_RejectsWithoutCharge()
    {
        var world = ArmedAt(VendorKinds.Smith, new Cell(2, 1));
        var item = TestWorlds.Give(world, TestGameContent.Weapon(value: 50, maxDurability: 30));
        world.Belongings.Gold = 20;

        var events = world.Tick(new Command[] { new RepairCommand(item.Id) });

        Assert.Equal("item does not need repair", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.Equal(20, world.Belongings.Gold);
        Assert.Equal(30, item.Durability);
    }

    [Fact]
    public void Repair_NonRepairable_Rejects()
    {
        var world = ArmedAt(VendorKinds.Smith, new Cell(2, 1));
        var potion = TestWorlds.Give(world, TestGameContent.Potion(value: 40));
        world.Belongings.Gold = 20;

        var events = world.Tick(new Command[] { new RepairCommand(potion.Id) });

        Assert.Equal("item cannot be repaired", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.Equal(20, world.Belongings.Gold);
    }

    [Fact]
    public void Repair_WrongVendor_Rejects()
    {
        var world = ArmedAt(VendorKinds.Healer, new Cell(3, 1));
        var item = TestWorlds.Give(world, TestGameContent.Weapon(value: 50, maxDurability: 30));
        item.Durability = 10;
        world.Belongings.Gold = 100;

        var events = world.Tick(new Command[] { new RepairCommand(item.Id) });

        Assert.Equal("wrong vendor", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.Equal(10, item.Durability);
        Assert.Equal(100, world.Belongings.Gold);
    }

    [Fact]
    public void Repair_CannotAfford_RejectsWithoutMutating()
    {
        var world = ArmedAt(VendorKinds.Smith, new Cell(2, 1));
        var item = TestWorlds.Give(world, TestGameContent.Weapon(value: 50, maxDurability: 30));
        item.Durability = 15;
        world.Belongings.Gold = 11;

        var events = world.Tick(new Command[] { new RepairCommand(item.Id) });

        Assert.Equal("not enough gold", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.Equal(15, item.Durability);
        Assert.Equal(11, world.Belongings.Gold);
    }

    [Fact]
    public void IdentifyService_PaysFeeAndIdentifies()
    {
        var world = ArmedAt(VendorKinds.Sage, new Cell(4, 1));
        var affix = TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon);
        var item = new Item(
            world.NextItemId++,
            TestGameContent.Weapon(value: 50),
            1,
            0,
            TestItems.Roll(affix, 3),
            null,
            identified: false);
        Assert.True(world.Belongings.Stow(item));
        world.Belongings.Gold = 60;

        var events = world.Tick(new Command[] { new IdentifyServiceCommand(item.Id) });

        Assert.Empty(events.OfType<CommandRejected>());
        Assert.True(item.Identified);
        Assert.Equal(10, world.Belongings.Gold);
        Assert.Single(events.OfType<ItemIdentified>());
    }

    [Fact]
    public void IdentifyService_AlreadyIdentified_RejectsWithoutCharge()
    {
        var world = ArmedAt(VendorKinds.Sage, new Cell(4, 1));
        var item = TestWorlds.Give(world, TestGameContent.Weapon());
        world.Belongings.Gold = 60;

        var events = world.Tick(new Command[] { new IdentifyServiceCommand(item.Id) });

        Assert.Equal("item is already identified", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.True(item.Identified);
        Assert.Equal(60, world.Belongings.Gold);
    }

    [Fact]
    public void IdentifyService_CannotAfford_RejectsWithoutMutating()
    {
        var world = ArmedAt(VendorKinds.Sage, new Cell(4, 1));
        var affix = TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon);
        var item = new Item(
            world.NextItemId++,
            TestGameContent.Weapon(value: 50),
            1,
            0,
            TestItems.Roll(affix, 3),
            null,
            identified: false);
        Assert.True(world.Belongings.Stow(item));
        world.Belongings.Gold = 49;

        var events = world.Tick(new Command[] { new IdentifyServiceCommand(item.Id) });

        Assert.Equal("not enough gold", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.False(item.Identified);
        Assert.Equal(49, world.Belongings.Gold);
    }

    [Fact]
    public void IdentifyService_WrongVendor_Rejects()
    {
        var world = ArmedAt(VendorKinds.Smith, new Cell(2, 1));
        var affix = TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon);
        var item = new Item(
            world.NextItemId++,
            TestGameContent.Weapon(),
            1,
            0,
            TestItems.Roll(affix, 3),
            null,
            identified: false);
        Assert.True(world.Belongings.Stow(item));
        world.Belongings.Gold = 60;

        var events = world.Tick(new Command[] { new IdentifyServiceCommand(item.Id) });

        Assert.Equal("wrong vendor", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.False(item.Identified);
        Assert.Equal(60, world.Belongings.Gold);
    }

    [Fact]
    public void HealService_PaysFeeAndRestoresLifeOnly()
    {
        var content = TestGameContent.Valid(
            levels: TestGameContent.WithTown(),
            rules: TestGameContent.Rules(vendorHealFee: 25));
        var world = ArmedAt(VendorKinds.Healer, new Cell(3, 1), content);
        world.Player.Life = 40;
        world.Player.Mana = 5;
        world.Belongings.Gold = 30;

        var events = world.Tick(new Command[] { new HealServiceCommand() });

        Assert.Empty(events.OfType<CommandRejected>());
        Assert.Equal(world.Player.Profile.MaxLife, world.Player.Life);
        Assert.Equal(5, world.Player.Mana);
        Assert.Equal(5, world.Belongings.Gold);
        var healed = Assert.Single(events.OfType<VendorHealed>());
        Assert.Equal(25, healed.Price);
        Assert.Equal(world.Player.Profile.MaxLife, healed.Life);
    }

    [Fact]
    public void HealService_FullLife_RejectsWithoutCharge()
    {
        var content = TestGameContent.Valid(
            levels: TestGameContent.WithTown(),
            rules: TestGameContent.Rules(vendorHealFee: 25));
        var world = ArmedAt(VendorKinds.Healer, new Cell(3, 1), content);
        world.Belongings.Gold = 30;

        var events = world.Tick(new Command[] { new HealServiceCommand() });

        Assert.Equal("already at full life", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.Equal(30, world.Belongings.Gold);
    }

    [Fact]
    public void HealService_ZeroFee_StillHeals()
    {
        var world = ArmedAt(VendorKinds.Healer, new Cell(3, 1));
        world.Player.Life = 1;
        world.Belongings.Gold = 0;

        var events = world.Tick(new Command[] { new HealServiceCommand() });

        Assert.Empty(events.OfType<CommandRejected>());
        Assert.Equal(world.Player.Profile.MaxLife, world.Player.Life);
        Assert.Equal(0, world.Belongings.Gold);
    }

    [Fact]
    public void HealService_WrongVendor_Rejects()
    {
        var world = ArmedAt(VendorKinds.Smith, new Cell(2, 1));
        world.Player.Life = 1;
        world.Belongings.Gold = 100;

        var events = world.Tick(new Command[] { new HealServiceCommand() });

        Assert.Equal("wrong vendor", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.Equal(1, world.Player.Life);
    }

    private static World ArmedAt(string vendorKind, Cell cell, ContentSet? content = null)
    {
        var world = TestWorlds.CreateWithTown(content);
        world.Level.Move(world.Player, cell);
        world.Tick(new Command[] { new TalkCommand(vendorKind) });
        Assert.Equal(vendorKind, world.ActiveVendor);
        return world;
    }
}
