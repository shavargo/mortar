using Mortar.Game.Content;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class LevelingTests
{
    [Fact]
    public void KillExperience_ScalesWithLevelDifference()
    {
        var rules = TestGameContent.Rules(experiencePercentPerLevel: 12, minimumExperiencePercent: 25);
        Assert.Equal(50, Leveling.KillExperience(50, 1, 1, rules));
        Assert.Equal(62, Leveling.KillExperience(50, 3, 1, rules));
        Assert.Equal(38, Leveling.KillExperience(50, 3, 5, rules));
        Assert.Equal(12, Leveling.KillExperience(50, 1, 15, rules));
    }

    [Fact]
    public void KillExperience_CanDropToZeroWithNoMinimum()
    {
        var rules = TestGameContent.Rules(experiencePercentPerLevel: 10, minimumExperiencePercent: 0);
        Assert.Equal(0, Leveling.KillExperience(50, 1, 15, rules));
    }

    [Fact]
    public void AddExperience_CanGainSeveralLevels()
    {
        var warrior = TestGameContent.Warrior();
        var progress = PlayerProgress.Start(warrior);
        var gained = Leveling.AddExperience(progress, 350, warrior, TestGameContent.Rules());
        Assert.Equal(2, gained);
        Assert.Equal(3, progress.Level);
        Assert.Equal(10, progress.UnspentPoints);
        Assert.Equal(350, progress.Experience);
    }

    [Fact]
    public void AddExperience_StopsAtMaxLevel()
    {
        var warrior = TestGameContent.Warrior();
        var progress = PlayerProgress.Start(warrior);
        Assert.Equal(3, Leveling.AddExperience(progress, 10_000, warrior, TestGameContent.Rules()));
        Assert.Equal(4, progress.Level);
    }

    [Fact]
    public void SpendPoint_NeedsUnspentPoints()
    {
        var warrior = TestGameContent.Warrior();
        Assert.Equal("no unspent stat points", Leveling.SpendPoint(PlayerProgress.Start(warrior), StatKind.Strength, warrior));
    }

    [Fact]
    public void SpendPoint_RaisesTheStat()
    {
        var warrior = TestGameContent.Warrior();
        var progress = new PlayerProgress("warrior", 2, 0, warrior.BaseStats, 1);
        Assert.Null(Leveling.SpendPoint(progress, StatKind.Strength, warrior));
        Assert.Equal(21, progress.BaseStats.Strength);
        Assert.Equal(0, progress.UnspentPoints);
    }

    [Fact]
    public void SpendPoint_RespectsTheCap()
    {
        var warrior = TestGameContent.Warrior();
        var progress = new PlayerProgress("warrior", 2, 0, new Stats(100, 20, 20, 10), 1);
        Assert.Equal("strength is at its cap", Leveling.SpendPoint(progress, StatKind.Strength, warrior));
    }
}
