using Mortar.Game.Content;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class UniqueLootTests
{
    [Fact]
    public void Roll_SameSeed_ProducesSameUnique()
    {
        var content = ContentWithUniques(TestGameContent.Unique("relic", "Relic Blade", "knife"));
        var (first, firstWorld) = Roll(content, seed: 42, uniqueChance: 100);
        var (second, _) = Roll(content, seed: 42, uniqueChance: 100);
        Assert.Equal("relic", first.UniqueId);
        Assert.Equal(first.UniqueId, second.UniqueId);
        Assert.Equal("Relic Blade", first.UniqueName);
        Assert.False(first.Identified);
        Assert.Equal(first.Definition.MaxDurability, first.Durability);
        Assert.True(firstWorld.WasUniqueDropped("relic"));
    }

    [Fact]
    public void Roll_SkipsUniqueAlreadyMarkedDropped()
    {
        var early = TestGameContent.Unique("early", "Early Edge", "knife", minLevel: 1);
        var later = TestGameContent.Unique("later", "Later Edge", "knife", minLevel: 1);
        var content = ContentWithUniques(early, later);
        var world = TestWorlds.Create(content, seed: 3);
        world.MarkUniqueDropped("early");
        var item = ItemFactory.Roll(world, itemLevel: 1, magicPercent: 0, uniqueChance: 100);
        Assert.NotNull(item);
        Assert.Equal("later", item!.UniqueId);
        Assert.True(world.WasUniqueDropped("later"));
    }

    [Fact]
    public void Roll_WhenAllUniquesDropped_FallsThroughToNormal()
    {
        var content = ContentWithUniques(TestGameContent.Unique("relic", "Relic Blade", "knife"));
        var world = TestWorlds.Create(content, seed: 9);
        world.MarkUniqueDropped("relic");
        var item = ItemFactory.Roll(world, itemLevel: 1, magicPercent: 0, uniqueChance: 100);
        Assert.NotNull(item);
        Assert.Null(item!.UniqueId);
        Assert.Equal(ItemQuality.Normal, item.Quality);
    }

    [Fact]
    public void Roll_RespectsMinLevel()
    {
        var content = ContentWithUniques(
            TestGameContent.Unique("high", "High Edge", "knife", minLevel: 5));
        var world = TestWorlds.Create(content, seed: 2);
        var item = ItemFactory.Roll(world, itemLevel: 1, magicPercent: 0, uniqueChance: 100);
        Assert.NotNull(item);
        Assert.Null(item!.UniqueId);
        Assert.False(world.WasUniqueDropped("high"));
    }

    [Fact]
    public void VendorStock_NeverRollsUniques()
    {
        var knife = TestGameContent.Weapon();
        var keen = TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.Damage, 3, ItemSlots.Weapon);
        var content = TestGameContent.Valid(
            levels: TestGameContent.WithTown(),
            items: TestGameContent.Items(
                new[] { knife },
                new[] { keen },
                uniques: new[] { TestGameContent.Unique("relic", "Relic Blade", "knife") }));
        var world = TestWorlds.CreateWithTown(content);
        Assert.All(world.Smith.Stock, item => Assert.Null(item.UniqueId));
        Assert.False(world.WasUniqueDropped("relic"));
    }

    [Fact]
    public void WorldHash_IncludesDroppedUniqueFlags()
    {
        var content = ContentWithUniques(TestGameContent.Unique("relic", "Relic Blade", "knife"));
        var world = TestWorlds.Create(content, seed: 1);
        var before = WorldHash.Compute(world);
        world.MarkUniqueDropped("relic");
        Assert.NotEqual(before, WorldHash.Compute(world));
    }

    private static ContentSet ContentWithUniques(params UniqueDefinition[] uniques)
    {
        var knife = TestGameContent.Weapon();
        var keen = TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.Damage, 3, ItemSlots.Weapon);
        return TestGameContent.Valid(
            items: TestGameContent.Items(
                new[] { knife },
                new[] { keen },
                uniques: uniques));
    }

    private static (Item Item, World World) Roll(ContentSet content, uint seed, int uniqueChance)
    {
        var world = TestWorlds.Create(content, seed);
        var item = ItemFactory.Roll(world, itemLevel: 1, magicPercent: 0, uniqueChance);
        Assert.NotNull(item);
        return (item!, world);
    }
}
