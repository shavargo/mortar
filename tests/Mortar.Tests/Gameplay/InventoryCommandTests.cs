using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class InventoryCommandTests
{
    private static readonly ItemBaseDefinition Knife = TestGameContent.Weapon();
    private static readonly ItemBaseDefinition Sword = TestGameContent.Weapon("sword", strength: 30);
    private static readonly ItemBaseDefinition Maul = new ItemBaseDefinition(
        "maul", "maul", "maul", ItemSlots.Weapon, 2, 3, 1, 1, default, new IntRange(5, 9), null, null, 0, null);
    private static readonly ItemBaseDefinition Vest = TestGameContent.Armor();
    private static readonly ItemBaseDefinition Ring = TestGameContent.Ring();
    private static readonly ItemBaseDefinition Tonic = TestGameContent.Potion();

    [Fact]
    public void Equip_MovesTheItemAndRaisesStats()
    {
        var world = Create();
        var knife = TestWorlds.Give(world, Knife);
        var events = Tick(world, new EquipCommand(knife.Id));
        Assert.Equal(EquipmentSlot.Weapon, Assert.Single(events.OfType<ItemEquipped>()).Slot);
        Assert.Same(knife, world.Belongings.Equipment.Get(EquipmentSlot.Weapon));
        Assert.Empty(world.Belongings.Inventory.Items);
        Assert.Equal(5, world.Player.Profile.Damage.Min);
    }

    [Fact]
    public void Equip_SwapsTheOldItemIntoTheInventory()
    {
        var world = Create();
        var first = TestWorlds.Give(world, Knife);
        var second = TestWorlds.Give(world, Knife);
        Tick(world, new EquipCommand(first.Id));
        var events = Tick(world, new EquipCommand(second.Id));
        Assert.Equal(first.Id, Assert.Single(events.OfType<ItemUnequipped>()).ItemId);
        Assert.Same(second, world.Belongings.Equipment.Get(EquipmentSlot.Weapon));
        Assert.NotNull(world.Belongings.Inventory.Find(first.Id));
    }

    [Fact]
    public void Equip_WithUnmetRequirement_IsRejected()
    {
        var world = Create();
        var sword = TestWorlds.Give(world, Sword);
        Assert.Equal("requires 30 strength", Rejection(Tick(world, new EquipCommand(sword.Id))));
    }

    [Fact]
    public void Equip_WithNoRoomForTheSwappedItem_ChangesNothing()
    {
        var world = Create();
        var maul = TestWorlds.Give(world, Maul);
        Tick(world, new EquipCommand(maul.Id));
        var knife = TestWorlds.Give(world, Knife);
        for (var i = 0; i < 38; i++) TestWorlds.Give(world, Ring);
        var before = world.Player.Profile;
        var events = Tick(world, new EquipCommand(knife.Id));
        Assert.Equal("no room in the inventory for the item being replaced", Rejection(events));
        Assert.Same(maul, world.Belongings.Equipment.Get(EquipmentSlot.Weapon));
        Assert.Equal(new Cell(0, 0), world.Belongings.Inventory.Find(knife.Id)!.Cell);
        Assert.Same(before, world.Player.Profile);
    }

    [Fact]
    public void Unequip_RestoresTheExactPreviousStats()
    {
        var world = Create();
        var before = world.Player.Profile;
        var vest = TestWorlds.Give(world, Vest);
        Tick(world, new EquipCommand(vest.Id));
        Assert.Equal(before.ArmorClass + 5, world.Player.Profile.ArmorClass);
        var events = Tick(world, new UnequipCommand(EquipmentSlot.Body));
        Assert.Single(events.OfType<ItemUnequipped>());
        var after = world.Player.Profile;
        Assert.Equal(
            (before.MaxLife, before.MaxMana, before.ArmorClass, before.ToHit, before.Damage.Min, before.Damage.Max, before.BlockChance),
            (after.MaxLife, after.MaxMana, after.ArmorClass, after.ToHit, after.Damage.Min, after.Damage.Max, after.BlockChance));
        Assert.NotNull(world.Belongings.Inventory.Find(vest.Id));
    }

    [Fact]
    public void Unequip_BadRequests_AreRejected()
    {
        var world = Create();
        Assert.Equal("nothing is equipped there", Rejection(Tick(world, new UnequipCommand(EquipmentSlot.Head))));
        Assert.Equal("unknown equipment slot", Rejection(Tick(world, new UnequipCommand((EquipmentSlot)42))));
        var vest = TestWorlds.Give(world, Vest);
        Tick(world, new EquipCommand(vest.Id));
        for (var i = 0; i < 10; i++) TestWorlds.Give(world, Vest);
        Assert.Equal(PickupSystem.InventoryFull, Rejection(Tick(world, new UnequipCommand(EquipmentSlot.Body))));
        Assert.Same(vest, world.Belongings.Equipment.Get(EquipmentSlot.Body));
    }

    [Fact]
    public void Drop_PutsTheItemUnderThePlayer()
    {
        var world = Create();
        var knife = TestWorlds.Give(world, Knife);
        var events = Tick(world, new DropItemCommand(knife.Id));
        Assert.Single(events.OfType<ItemDropped>());
        var ground = Assert.Single(world.Level.GroundItems);
        Assert.Equal(knife.Id, ground.Id);
        Assert.Equal(world.Player.Cell, ground.Cell);
        Assert.Empty(world.Belongings.Inventory.Items);
    }

    [Fact]
    public void DroppingWornGear_RefreshesStats()
    {
        var world = Create();
        var armor = world.Player.Profile.ArmorClass;
        var vest = TestWorlds.Give(world, Vest);
        Tick(world, new EquipCommand(vest.Id));
        var events = Tick(world, new DropItemCommand(vest.Id));
        Assert.Single(events.OfType<ItemUnequipped>());
        Assert.Equal(armor, world.Player.Profile.ArmorClass);
    }

    [Fact]
    public void Move_PlacesTheItemAtTheCell()
    {
        var world = Create();
        var knife = TestWorlds.Give(world, Knife);
        Assert.Single(Tick(world, new MoveItemCommand(knife.Id, new Cell(5, 1))).OfType<ItemMoved>());
        Assert.Equal(new Cell(5, 1), world.Belongings.Inventory.Find(knife.Id)!.Cell);
        Assert.Equal("item does not fit there", Rejection(Tick(world, new MoveItemCommand(knife.Id, new Cell(9, 3)))));
        var tonic = TestWorlds.Give(world, Tonic);
        Tick(world, new MoveItemCommand(tonic.Id, new Cell(3, 3)));
        Assert.Null(world.Belongings.Belt.Get(0));
        Assert.Equal(new Cell(3, 3), world.Belongings.Inventory.Find(tonic.Id)!.Cell);
    }

    [Fact]
    public void UseBelt_RestoresLifeUpToTheMaximum()
    {
        var world = Create();
        TestWorlds.Give(world, Tonic);
        TestWorlds.Give(world, Tonic);
        world.Player.Life = 10;
        Assert.Equal(20, Assert.Single(Tick(world, new UseBeltCommand(0)).OfType<PotionDrunk>()).Amount);
        Assert.Equal(30, world.Player.Life);
        Assert.Null(world.Belongings.Belt.Get(0));
        world.Player.Life = world.Player.Profile.MaxLife - 5;
        Assert.Equal(5, Assert.Single(Tick(world, new UseBeltCommand(1)).OfType<PotionDrunk>()).Amount);
        Assert.Equal(world.Player.Profile.MaxLife, world.Player.Life);
    }

    [Fact]
    public void UseBelt_BadSlots_AreRejected()
    {
        var world = Create();
        Assert.Equal("that belt slot is empty", Rejection(Tick(world, new UseBeltCommand(0))));
        Assert.Equal("belt slot must be between 0 and 7", Rejection(Tick(world, new UseBeltCommand(8))));
    }

    [Fact]
    public void PutInBelt_MovesPotionsOnly()
    {
        var world = Create();
        var potions = Enumerable.Range(0, 9).Select(_ => TestWorlds.Give(world, Tonic)).ToList();
        var knife = TestWorlds.Give(world, Knife);
        Assert.Equal("only potions go in the belt", Rejection(Tick(world, new PutInBeltCommand(knife.Id, 0))));
        Assert.Equal("that belt slot is taken", Rejection(Tick(world, new PutInBeltCommand(potions[8].Id, 0))));
        Tick(world, new UseBeltCommand(3));
        Assert.Single(Tick(world, new PutInBeltCommand(potions[8].Id, 3)).OfType<ItemBelted>());
        Assert.Same(potions[8], world.Belongings.Belt.Get(3));
        Assert.Null(world.Belongings.Inventory.Find(potions[8].Id));
        Tick(world, new UseBeltCommand(5));
        Tick(world, new PutInBeltCommand(potions[0].Id, 5));
        Assert.Null(world.Belongings.Belt.Get(0));
        Assert.Same(potions[0], world.Belongings.Belt.Get(5));
    }

    private static World Create() => TestWorlds.Create(
        TestGameContent.Valid(items: TestGameContent.Items(new[] { Knife, Sword, Maul, Vest, Ring, Tonic })));

    private static IReadOnlyList<GameEvent> Tick(World world, Command command) => world.Tick(new[] { command });

    private static string Rejection(IReadOnlyList<GameEvent> events) => Assert.Single(events.OfType<CommandRejected>()).Reason;
}
