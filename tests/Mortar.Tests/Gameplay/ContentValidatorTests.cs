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
            TestGameContent.Timings(attack: 6, hit: 6), 5, 10, AiKinds.MeleeChase, null);
        AssertError(TestGameContent.Create(monsters: new[] { bad }), "monster 'zombie'", "hitFrame must be at least 1 and less than attackTicks");
    }

    [Fact]
    public void ExperienceThresholds_MustIncrease()
    {
        var rules = TestGameContent.Rules(thresholds: new long[] { 100, 100 });
        AssertError(TestGameContent.Create(rules: rules), "rules", "strictly increasing");
    }

    [Fact]
    public void StatCapBelowBase_IsRejected()
    {
        var warrior = TestGameContent.Warrior();
        var capped = new ClassDefinition(
            warrior.Id, warrior.Name, warrior.SpriteSet, warrior.BaseStats, new Stats(10, 100, 100, 100),
            warrior.Growth, warrior.UnarmedDamage, warrior.Timings);
        AssertError(TestGameContent.Create(classes: new[] { capped }), "class 'warrior'", "strength cap must be at least the base value");
    }

    private static void AssertError(AuthoringResult<ContentSet> result, string source, string fragment)
    {
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Source == source && e.Message.Contains(fragment));
    }
}
