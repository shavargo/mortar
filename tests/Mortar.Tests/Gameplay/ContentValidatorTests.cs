using Mortar.Core.Authoring;
using Mortar.Game.Content;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class ContentValidatorTests
{
    [Fact]
    public void ValidContent_Succeeds()
    {
        var result = TestGameContent.Create();
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        Assert.Equal(2, result.Value!.DeepestLevel);
        Assert.True(result.Value.TryGetClass("warrior", out _));
    }

    [Fact]
    public void Levels_AreSortedByDepth()
    {
        var content = TestGameContent.Valid(levels: new[] { TestGameContent.Level(2), TestGameContent.Level(1) });
        Assert.Equal(new[] { 1, 2 }, content.Levels.Select(l => l.Depth));
    }

    [Fact]
    public void DuplicateMonsterIds_AreRejected()
    {
        var monsters = new[] { TestGameContent.Monster("zombie"), TestGameContent.Monster("zombie") };
        AssertError(TestGameContent.Create(monsters: monsters), "monster 'zombie'", "duplicate id");
    }

    [Fact]
    public void DepthGap_IsRejected()
    {
        var levels = new[] { TestGameContent.Level(1), TestGameContent.Level(3) };
        AssertError(TestGameContent.Create(levels: levels), "levels", "depths must be 1, 2, 3");
    }

    [Fact]
    public void Town_IsDepthZero()
    {
        var content = TestGameContent.Valid(levels: TestGameContent.WithTown());
        Assert.Equal(0, content.ShallowestLevel);
        Assert.Equal(2, content.DeepestLevel);
        Assert.Equal("town", content.Level(0).AreaId);
        Assert.Equal(2, content.Level(2).Depth);
    }

    [Fact]
    public void WithoutTown_ShallowestLevelIsOne() => Assert.Equal(1, TestGameContent.Valid().ShallowestLevel);

    [Fact]
    public void TownOnly_IsRejected() =>
        AssertError(TestGameContent.Create(levels: new[] { TestGameContent.Town() }), "levels", "at least one dungeon level is required");

    [Fact]
    public void TwoTowns_AreRejected()
    {
        var levels = new[] { TestGameContent.Town(), TestGameContent.Town(), TestGameContent.Level(1) };
        AssertError(TestGameContent.Create(levels: levels), "levels", "there must be at most one town");
    }

    [Fact]
    public void TownWithSpawns_IsRejected()
    {
        var town = new LevelDefinition(0, "town", new[] { new SpawnEntry("zombie", 1) }, null);
        AssertError(TestGameContent.Create(levels: new[] { town, TestGameContent.Level(1) }), "town", "the town must not have spawns");
    }

    [Fact]
    public void TownWithBoss_IsRejected()
    {
        var town = new LevelDefinition(0, "town", Array.Empty<SpawnEntry>(), "zombie");
        AssertError(TestGameContent.Create(levels: new[] { town, TestGameContent.Level(1) }), "town", "the town must not have a boss");
    }

    [Fact]
    public void TownWithoutArea_IsRejected() =>
        AssertError(TestGameContent.Create(levels: new[] { TestGameContent.Town(""), TestGameContent.Level(1) }), "town", "area is required");

    [Fact]
    public void UnknownSpawnMonster_IsRejected()
    {
        var levels = new[] { TestGameContent.Level(1, monsterId: "ghost") };
        AssertError(TestGameContent.Create(levels: levels), "level 1", "unknown monster 'ghost'");
    }

    [Fact]
    public void UnknownBoss_IsRejected()
    {
        var levels = new[] { TestGameContent.Level(1, bossId: "lich") };
        AssertError(TestGameContent.Create(levels: levels), "level 1", "boss references unknown monster 'lich'");
    }

    [Fact]
    public void PackLeaderWithoutMinions_IsRejected()
    {
        var monsters = new[] { TestGameContent.Monster("zombie"), TestGameContent.Monster("chief", AiKinds.PackLeader) };
        AssertError(TestGameContent.Create(monsters: monsters), "monster 'chief'", "need minions");
    }

    [Fact]
    public void MinionsThatArePackLeaders_AreRejected()
    {
        var spec = new MinionSpec("chief", new IntRange(1, 2));
        var monsters = new[] { TestGameContent.Monster("zombie"), TestGameContent.Monster("chief", AiKinds.PackLeader, spec) };
        AssertError(TestGameContent.Create(monsters: monsters), "monster 'chief'", "minions cannot be pack leaders");
    }

    [Fact]
    public void MinionsOnNonLeader_AreRejected()
    {
        var spec = new MinionSpec("zombie", new IntRange(1, 2));
        var monsters = new[] { TestGameContent.Monster("zombie"), TestGameContent.Monster("imp", AiKinds.MeleeChase, spec) };
        AssertError(TestGameContent.Create(monsters: monsters), "monster 'imp'", "only pack-leader monsters can have minions");
    }

    [Fact]
    public void HitFrameAtEndOfSwing_IsRejected()
    {
        var bad = new MonsterDefinition(
            "zombie", "Zombie", "zombie", 1, new IntRange(5, 5), 0, 50, new IntRange(1, 2), 0,
            TestGameContent.Timings(attack: 6, hit: 6), 5, 10, AiKinds.MeleeChase, null, null);
        AssertError(TestGameContent.Create(monsters: new[] { bad }), "monster 'zombie'", "hitFrame must be at least 1 and less than attackTicks");
    }

    [Fact]
    public void ExperienceThresholds_MustIncrease()
    {
        var rules = TestGameContent.Rules(thresholds: new long[] { 100, 100 });
        AssertError(TestGameContent.Create(rules: rules), "rules", "strictly increasing");
    }

    [Fact]
    public void VendorRules_HaveTownDefaults()
    {
        var rules = TestGameContent.Rules();
        Assert.Equal(144000, rules.VendorStockRegenTicks);
        Assert.Equal(25, rules.VendorSellPercent);
        Assert.Equal(3, rules.VendorMagicBuyMult);
        Assert.Equal(50, rules.VendorRepairPercent);
        Assert.Equal(50, rules.VendorIdentifyFee);
        Assert.Equal(0, rules.VendorHealFee);
        Assert.Equal(8, rules.VendorSmithSlots);
        Assert.Equal(6, rules.VendorHealerSlots);
        Assert.Equal(6, rules.VendorSageSlots);
        Assert.Equal(40, rules.VendorSmithMagicPercent);
    }

    [Theory]
    [InlineData(-1, 25, 3, 50, 50, 0, 8, 6, 6, 40, "vendorStockRegenTicks")]
    [InlineData(144000, 101, 3, 50, 50, 0, 8, 6, 6, 40, "vendorSellPercent")]
    [InlineData(144000, 25, 0, 50, 50, 0, 8, 6, 6, 40, "vendorMagicBuyMult")]
    [InlineData(144000, 25, 3, -1, 50, 0, 8, 6, 6, 40, "vendorRepairPercent")]
    [InlineData(144000, 25, 3, 50, -1, 0, 8, 6, 6, 40, "vendorIdentifyFee")]
    [InlineData(144000, 25, 3, 50, 50, -1, 8, 6, 6, 40, "vendorHealFee")]
    [InlineData(144000, 25, 3, 50, 50, 0, -1, 6, 6, 40, "vendorSmithSlots")]
    [InlineData(144000, 25, 3, 50, 50, 0, 8, -1, 6, 40, "vendorHealerSlots")]
    [InlineData(144000, 25, 3, 50, 50, 0, 8, 6, -1, 40, "vendorSageSlots")]
    [InlineData(144000, 25, 3, 50, 50, 0, 8, 6, 6, 101, "vendorSmithMagicPercent")]
    public void VendorRules_RejectOutOfRange(
        int regen, int sell, int magicBuy, int repair, int identify, int heal,
        int smithSlots, int healerSlots, int sageSlots, int smithMagic, string fragment)
    {
        var rules = TestGameContent.Rules(
            vendorStockRegenTicks: regen,
            vendorSellPercent: sell,
            vendorMagicBuyMult: magicBuy,
            vendorRepairPercent: repair,
            vendorIdentifyFee: identify,
            vendorHealFee: heal,
            vendorSmithSlots: smithSlots,
            vendorHealerSlots: healerSlots,
            vendorSageSlots: sageSlots,
            vendorSmithMagicPercent: smithMagic);
        AssertError(TestGameContent.Create(rules: rules), "rules", fragment);
    }

    [Theory]
    [InlineData(101, 25)]
    [InlineData(-1, 25)]
    [InlineData(12, 101)]
    [InlineData(12, -1)]
    public void ExperiencePercents_MustBeBetweenZeroAndOneHundred(int perLevel, int minimum)
    {
        var rules = TestGameContent.Rules(experiencePercentPerLevel: perLevel, minimumExperiencePercent: minimum);
        AssertError(TestGameContent.Create(rules: rules), "rules", "must be between 0 and 100");
    }

    [Fact]
    public void HugeMonsterNumbers_AreRejected()
    {
        var huge = new MonsterDefinition(
            "zombie", "Zombie", "zombie", 1, new IntRange(5, 1_000_001), 0, 1_000_001, new IntRange(1, int.MaxValue), 0,
            TestGameContent.Timings(), 5, 10, AiKinds.MeleeChase, null, null);
        var result = TestGameContent.Create(monsters: new[] { huge });
        AssertError(result, "monster 'zombie'", "life max must be at most 1000000");
        AssertError(result, "monster 'zombie'", "damage max must be at most 1000000");
        AssertError(result, "monster 'zombie'", "armorClass, toHit, and experience must be at most 1000000");
    }

    [Fact]
    public void HugeClassNumbers_AreRejected()
    {
        var warrior = TestGameContent.Warrior();
        var huge = new ClassDefinition(
            warrior.Id, warrior.Name, warrior.SpriteSet, warrior.BaseStats, new Stats(100, 100, 1_000_001, 100),
            new ClassGrowth(10, 2, 2, 0, 1_000_001, 1, 5), new IntRange(2, 1_000_001), warrior.Timings, Array.Empty<string>());
        var result = TestGameContent.Create(classes: new[] { huge });
        AssertError(result, "class 'warrior'", "vitality cap must be at most 1000000");
        AssertError(result, "class 'warrior'", "growth values must be at most 1000000");
        AssertError(result, "class 'warrior'", "unarmedDamage max must be at most 1000000");
    }

    [Fact]
    public void StatCapBelowBase_IsRejected()
    {
        var warrior = TestGameContent.Warrior();
        var capped = new ClassDefinition(
            warrior.Id, warrior.Name, warrior.SpriteSet, warrior.BaseStats, new Stats(10, 100, 100, 100),
            warrior.Growth, warrior.UnarmedDamage, warrior.Timings, Array.Empty<string>());
        AssertError(TestGameContent.Create(classes: new[] { capped }), "class 'warrior'", "strength cap must be at least the base value");
    }

    private static void AssertError(AuthoringResult<ContentSet> result, string source, string fragment)
    {
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Source == source && e.Message.Contains(fragment));
    }
}
