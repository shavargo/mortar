using Mortar.Core.Model;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Saves;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class VendorStockTests
{
    [Fact]
    public void Create_FillsVendorStocksAndSchedulesRegen()
    {
        var world = CreateStocked();
        Assert.Equal(2, world.Smith.Stock.Count);
        Assert.Equal(3, world.Healer.Stock.Count);
        Assert.Equal(2, world.Sage.Stock.Count);
        Assert.Equal(world.Content.Rules.VendorStockRegenTicks, world.NextStockRegenTick);
        Assert.All(AllStock(world), item => Assert.True(item.Identified));
        Assert.All(world.Smith.Stock, item => Assert.Contains(item.Definition.Slot, SmithSlots));
        Assert.All(world.Healer.Stock, item => Assert.Equal(ItemSlots.Potion, item.Definition.Slot));
        Assert.All(world.Sage.Stock, item => Assert.Contains(item.Definition.Slot, SageSlots));
    }

    [Fact]
    public void Create_WithoutMatchingBases_LeavesStockEmpty()
    {
        var world = TestWorlds.CreateWithTown();
        Assert.Empty(world.Smith.Stock);
        Assert.Empty(world.Healer.Stock);
        Assert.Empty(world.Sage.Stock);
        Assert.Equal(world.Content.Rules.VendorStockRegenTicks, world.NextStockRegenTick);
    }

    [Fact]
    public void Regen_OnDeadline_RefillsAllVendorsAndEmitsEvent()
    {
        var world = CreateStocked(regenTicks: 3);
        var before = AllStock(world).Select(item => item.Id).ToList();
        Assert.Equal(3, world.NextStockRegenTick);

        var early = world.Tick(Array.Empty<Command>());
        Assert.Empty(early.OfType<VendorStockRefreshed>());
        Assert.Equal(before, AllStock(world).Select(item => item.Id));

        world.Tick(Array.Empty<Command>());
        var events = world.Tick(Array.Empty<Command>());
        Assert.Equal(3, world.TickCount);
        Assert.Single(events.OfType<VendorStockRefreshed>());
        Assert.Equal(6, world.NextStockRegenTick);
        Assert.Equal(2, world.Smith.Stock.Count);
        Assert.Equal(3, world.Healer.Stock.Count);
        Assert.Equal(2, world.Sage.Stock.Count);
        Assert.DoesNotContain(AllStock(world).Select(item => item.Id), id => before.Contains(id));
    }

    [Fact]
    public void Regen_DiscardsUnsoldStock()
    {
        var world = CreateStocked(regenTicks: 1);
        var staleId = world.Smith.Stock[0].Id;
        world.Tick(Array.Empty<Command>());
        Assert.DoesNotContain(world.Smith.Stock, item => item.Id == staleId);
    }

    [Fact]
    public void ShopItemLevel_UsesDeepestVisitedDungeonDepth()
    {
        var world = CreateStocked(regenTicks: 1, smithSlots: 1, healerSlots: 0, sageSlots: 0);
        Assert.All(world.Smith.Stock, item => Assert.Equal(1, item.ItemLevel));

        Assert.Null(LevelEntry.Enter(world, 2, MarkerKinds.Entrance));
        world.Tick(Array.Empty<Command>());
        Assert.All(world.Smith.Stock, item => Assert.Equal(2, item.ItemLevel));
    }

    [Fact]
    public void MagicShopGoods_AreIdentified()
    {
        var world = CreateStocked(regenTicks: 144000, smithSlots: 8, healerSlots: 0, sageSlots: 0, smithMagicPercent: 100);
        Assert.Contains(world.Smith.Stock, item => item.Quality == ItemQuality.Magic);
        Assert.All(world.Smith.Stock, item => Assert.True(item.Identified));
    }

    [Fact]
    public void Save_RoundTripsStockAndRegenTick()
    {
        var world = CreateStocked();
        world.Level.Move(world.Player, new Cell(4, 1));
        world.Tick(new Command[] { new UseStairsCommand() });
        var snapshot = WorldSnapshot.Capture(world);
        Assert.Equal(world.NextStockRegenTick, snapshot.NextStockRegenTick);
        Assert.Equal(world.Smith.Stock.Count, snapshot.SmithStock!.Count);
        var restored = World.Restore(snapshot, world.Content, FixedLevelGenerator.WithTown(TestWorlds.Town, TestWorlds.Room, TestWorlds.Room));
        Assert.True(restored.Succeeded, restored.Error);
        Assert.Equal(WorldHash.Compute(world), WorldHash.Compute(restored.World!));
        Assert.Equal(world.Smith.Stock.Select(i => i.Id), restored.World!.Smith.Stock.Select(i => i.Id));
    }

    private static readonly string[] SmithSlots =
    {
        ItemSlots.Weapon, ItemSlots.Body, ItemSlots.Head, ItemSlots.Shield, ItemSlots.Ring, ItemSlots.Amulet,
    };

    private static readonly string[] SageSlots = { ItemSlots.Scroll, ItemSlots.Book };

    private static IEnumerable<Item> AllStock(World world) =>
        world.Smith.Stock.Concat(world.Healer.Stock).Concat(world.Sage.Stock);

    private static World CreateStocked(
        int regenTicks = 144000,
        int smithSlots = 2,
        int healerSlots = 3,
        int sageSlots = 2,
        int smithMagicPercent = 40)
    {
        var result = ContentSet.Create(
            TestGameContent.Rules(
                vendorStockRegenTicks: regenTicks,
                vendorSmithSlots: smithSlots,
                vendorHealerSlots: healerSlots,
                vendorSageSlots: sageSlots,
                vendorSmithMagicPercent: smithMagicPercent),
            new[] { TestGameContent.Warrior() },
            new[] { TestGameContent.Monster("zombie") },
            TestGameContent.WithTown(),
            TestGameContent.Items(
                new[]
                {
                    TestGameContent.Weapon(dropWeight: 1),
                    TestGameContent.Armor(dropWeight: 1),
                    TestGameContent.Ring(dropWeight: 1),
                    TestGameContent.Potion(dropWeight: 1),
                    Scroll(dropWeight: 1),
                    Book(dropWeight: 1),
                },
                new[]
                {
                    TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon),
                    TestGameContent.Affix("sturdy", AffixPositions.Suffix, BonusKinds.ArmorClass, 2, ItemSlots.Body),
                }),
            new SpellCatalog(new[]
            {
                new SpellDefinition("healing", "Healing", SpellShapes.Self, 10, 6, 3, 0, null, 20, 0, 0, 5),
                new SpellDefinition(
                    "fireball", "Fireball", SpellShapes.Area, 18, 6, 3, 5, new IntRange(8, 8), 0, 2, 3, 0),
            }));
        var content = result.Value ?? throw new InvalidOperationException(string.Join("; ", result.Errors));
        return TestWorlds.CreateWithTown(content);
    }

    private static ItemBaseDefinition Scroll(int dropWeight = 0) => new ItemBaseDefinition(
        "scroll-heal", "Scroll", "scroll", ItemSlots.Scroll, 1, 1, 1, dropWeight, default,
        null, null, null, 0, null, spellId: "healing", value: 60);

    private static ItemBaseDefinition Book(int dropWeight = 0) => new ItemBaseDefinition(
        "book-fireball", "Book", "book", ItemSlots.Book, 1, 1, 1, dropWeight, default,
        null, null, null, 0, null, spellId: "fireball", value: 200);
}
