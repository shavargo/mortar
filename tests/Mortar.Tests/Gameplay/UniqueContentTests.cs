using Mortar.Core.Authoring;
using Mortar.Game.Content;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class UniqueContentTests
{
    private static readonly AffixDefinition Keen =
        TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon);

    [Fact]
    public void ValidUnique_IsAvailableById()
    {
        var unique = TestGameContent.Unique("ember-edge", "Ember Edge", "knife", 2, new[] { TestGameContent.Power("keen", 3) }, 120);
        var items = TestGameContent.Items(new[] { TestGameContent.Weapon() }, new[] { Keen }, uniques: new[] { unique });
        var content = TestGameContent.Valid(items: items);
        Assert.True(content.TryGetUnique("ember-edge", out var found));
        Assert.Equal("Ember Edge", found!.Name);
        Assert.Equal("knife", found.BaseItemId);
        Assert.Equal(2, found.MinLevel);
        Assert.Equal(120, found.Value);
        Assert.Single(found.Powers);
        Assert.Equal("keen", found.Powers[0].AffixId);
        Assert.Equal(3, found.Powers[0].Value);
    }

    [Fact]
    public void MultipleUniquesOnSameBase_AreAllowed()
    {
        var first = TestGameContent.Unique("one", "One", "knife", 1, new[] { TestGameContent.Power("keen", 3) });
        var second = TestGameContent.Unique("two", "Two", "knife", 2, new[] { TestGameContent.Power("keen", 3) });
        var items = TestGameContent.Items(new[] { TestGameContent.Weapon() }, new[] { Keen }, uniques: new[] { first, second });
        var content = TestGameContent.Valid(items: items);
        Assert.Equal(2, content.Uniques.Count);
        Assert.Equal("knife", content.Unique("one").BaseItemId);
        Assert.Equal("knife", content.Unique("two").BaseItemId);
    }

    [Fact]
    public void UnknownBaseItem_IsRejected()
    {
        var unique = TestGameContent.Unique(baseItemId: "sword");
        AssertError(Create(unique), "unique 'relic'", "base references unknown item 'sword'");
    }

    [Fact]
    public void UnknownAffix_IsRejected()
    {
        var unique = TestGameContent.Unique(powers: new[] { TestGameContent.Power("missing", 1) });
        AssertError(Create(unique), "unique 'relic'", "power references unknown affix 'missing'");
    }

    [Fact]
    public void PowerValueOutsideAffixRange_IsRejected()
    {
        var unique = TestGameContent.Unique(powers: new[] { TestGameContent.Power("keen", 4) });
        AssertError(Create(unique), "unique 'relic'", "power value must be between 3 and 3");
    }

    [Fact]
    public void DuplicateUniqueIds_AreRejected()
    {
        var unique = TestGameContent.Unique();
        var items = TestGameContent.Items(
            new[] { TestGameContent.Weapon() }, new[] { Keen }, uniques: new[] { unique, unique });
        AssertError(TestGameContent.Create(items: items), "unique 'relic'", "duplicate id");
    }

    [Fact]
    public void UniqueChanceOutOfRange_IsRejected()
    {
        var loot = TestGameContent.Loot(uniqueChance: 101);
        var items = TestGameContent.Items(new[] { TestGameContent.Weapon() }, loot: new[] { loot });
        AssertError(TestGameContent.Create(items: items), "loot table 'loot'", "uniqueChance must be between 0 and 100");
    }

    [Fact]
    public void LootTable_StoresUniqueChance()
    {
        var loot = TestGameContent.Loot(uniqueChance: 15);
        var items = TestGameContent.Items(new[] { TestGameContent.Weapon() }, loot: new[] { loot });
        var content = TestGameContent.Valid(items: items);
        Assert.Equal(15, content.LootTable("loot").UniqueChance);
    }

    [Fact]
    public void LootTable_DefaultsUniqueChanceToZero()
    {
        var loot = new LootTableDefinition("loot", 1, 0, 0, 1, new IntRange(5, 5), 0);
        var items = TestGameContent.Items(new[] { TestGameContent.Weapon() }, loot: new[] { loot });
        var content = TestGameContent.Valid(items: items);
        Assert.Equal(0, content.LootTable("loot").UniqueChance);
    }

    [Fact]
    public void Content_KeepsItsOwnCopyOfUniquePowers()
    {
        var powers = new List<UniquePower> { TestGameContent.Power("keen", 3) };
        var unique = new UniqueDefinition("relic", "Relic", "knife", 1, powers);
        var content = TestGameContent.Valid(items: TestGameContent.Items(
            new[] { TestGameContent.Weapon() }, new[] { Keen }, uniques: new[] { unique }));
        powers.Clear();
        Assert.Single(content.Unique("relic").Powers);
    }

    private static AuthoringResult<ContentSet> Create(UniqueDefinition unique) =>
        TestGameContent.Create(items: TestGameContent.Items(
            new[] { TestGameContent.Weapon() }, new[] { Keen }, uniques: new[] { unique }));

    private static void AssertError(AuthoringResult<ContentSet> result, string source, string fragment)
    {
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Source == source && e.Message.Contains(fragment));
    }
}
