using Mortar.Game;
using Mortar.Game.Content;
using Mortar.Game.Items;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class ItemContainerTests
{
    private static readonly ItemBaseDefinition Vest = TestGameContent.Armor();
    private static readonly ItemBaseDefinition Knife = TestGameContent.Weapon();
    private static readonly ItemBaseDefinition Tonic = TestGameContent.Potion();

    [Fact]
    public void Inventory_FillsEachColumnTopToBottomFirst()
    {
        var inventory = new Inventory();
        Assert.True(inventory.TryAdd(TestItems.Create(Vest, 1)));
        Assert.True(inventory.TryAdd(TestItems.Create(Vest, 2)));
        Assert.True(inventory.TryAdd(TestItems.Create(Vest, 3)));
        Assert.Equal(new Cell(0, 0), inventory.Find(1)!.Cell);
        Assert.Equal(new Cell(0, 2), inventory.Find(2)!.Cell);
        Assert.Equal(new Cell(2, 0), inventory.Find(3)!.Cell);
        Assert.Equal(3, inventory.ItemAt(new Cell(2, 1)));
    }

    [Fact]
    public void Inventory_RejectsOverlapsAndCellsOutsideTheGrid()
    {
        var inventory = new Inventory();
        Assert.True(inventory.TryPlace(TestItems.Create(Vest, 1), new Cell(0, 0)));
        Assert.False(inventory.TryPlace(TestItems.Create(Vest, 2), new Cell(1, 1)));
        Assert.False(inventory.TryPlace(TestItems.Create(Vest, 3), new Cell(9, 0)));
        Assert.False(inventory.TryPlace(TestItems.Create(Knife, 4), new Cell(5, 3)));
        Assert.True(inventory.Fits(Vest, new Cell(1, 1), ignoredItemId: 1));
    }

    [Fact]
    public void Inventory_RemoveFreesTheCells()
    {
        var inventory = new Inventory();
        inventory.TryPlace(TestItems.Create(Vest, 1), new Cell(0, 0));
        Assert.True(inventory.Remove(1));
        Assert.Equal(0, inventory.ItemAt(new Cell(1, 1)));
        Assert.Empty(inventory.Items);
        Assert.False(inventory.Remove(1));
    }

    [Fact]
    public void Inventory_ListsItemsInIdOrder()
    {
        var inventory = new Inventory();
        inventory.TryAdd(TestItems.Create(Knife, 5));
        inventory.TryAdd(TestItems.Create(Knife, 2));
        Assert.Equal(new[] { 2, 5 }, inventory.Items.Select(stored => stored.Item.Id));
    }

    [Fact]
    public void Inventory_WhenFull_HasNoSpace()
    {
        var inventory = new Inventory();
        for (var id = 1; id <= 10; id++) Assert.True(inventory.TryAdd(TestItems.Create(Vest, id)));
        Assert.Null(inventory.FindSpace(Vest));
        Assert.False(inventory.TryAdd(TestItems.Create(Vest, 11)));
    }

    [Fact]
    public void Belt_HoldsOnlyPotions()
    {
        var belt = new Belt();
        Assert.False(belt.TryPut(TestItems.Create(Knife, 1), 0));
        Assert.True(belt.TryPut(TestItems.Create(Tonic, 2), 0));
        Assert.False(belt.TryPut(TestItems.Create(Tonic, 3), 0));
        Assert.False(belt.TryPut(TestItems.Create(Tonic, 4), Belt.Slots));
        Assert.Equal(1, belt.FirstEmpty());
        Assert.Equal(0, belt.IndexOf(2));
        Assert.Equal(2, belt.Take(0)!.Id);
        Assert.Null(belt.Get(0));
    }

    [Fact]
    public void Equipment_RingsFillLeftThenRightThenReplaceLeft()
    {
        var equipment = new Equipment();
        var ring = TestGameContent.Ring();
        Assert.Equal(EquipmentSlot.LeftRing, equipment.SlotFor(ring));
        equipment.Set(EquipmentSlot.LeftRing, TestItems.Create(ring, 1));
        Assert.Equal(EquipmentSlot.RightRing, equipment.SlotFor(ring));
        equipment.Set(EquipmentSlot.RightRing, TestItems.Create(ring, 2));
        Assert.Equal(EquipmentSlot.LeftRing, equipment.SlotFor(ring));
        Assert.Equal(EquipmentSlot.RightRing, equipment.SlotOf(2));
        Assert.True(Equipment.Accepts(EquipmentSlot.RightRing, ring));
        Assert.False(Equipment.Accepts(EquipmentSlot.Weapon, ring));
    }

    [Fact]
    public void Item_NameJoinsAffixesAroundTheBase()
    {
        var keen = TestGameContent.Affix("Keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon);
        var brawn = TestGameContent.Affix("of Brawn", AffixPositions.Suffix, BonusKinds.Strength, 2, ItemSlots.Weapon);
        var magic = TestItems.Create(Knife, prefix: TestItems.Roll(keen, 3), suffix: TestItems.Roll(brawn, 2));
        Assert.Equal("Keen knife of Brawn", magic.Name);
        Assert.Equal(ItemQuality.Magic, magic.Quality);
        Assert.Equal(ItemQuality.Normal, TestItems.Create(Knife).Quality);
    }

    [Fact]
    public void Belongings_StowPutsPotionsInTheBeltFirst()
    {
        var belongings = new Belongings();
        for (var id = 1; id <= Belt.Slots; id++) Assert.True(belongings.Stow(TestItems.Create(Tonic, id)));
        Assert.Equal(-1, belongings.Belt.FirstEmpty());
        Assert.True(belongings.Stow(TestItems.Create(Tonic, 9)));
        Assert.NotNull(belongings.Inventory.Find(9));
    }

    [Fact]
    public void Belongings_TakeFindsItemsWhereverTheyAre()
    {
        var belongings = new Belongings();
        belongings.Equipment.Set(EquipmentSlot.Weapon, TestItems.Create(Knife, 1));
        belongings.Stow(TestItems.Create(Tonic, 2));
        belongings.Stow(TestItems.Create(Vest, 3));
        Assert.Equal(1, belongings.Find(1)!.Id);
        Assert.Equal(1, belongings.Take(1, out var slot)!.Id);
        Assert.Equal(EquipmentSlot.Weapon, slot);
        Assert.Equal(2, belongings.Take(2, out slot)!.Id);
        Assert.Null(slot);
        Assert.Equal(3, belongings.Take(3, out _)!.Id);
        Assert.Null(belongings.Take(3, out _));
    }
}
