using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class PlayerStatsTests
{
    [Fact]
    public void StartingWarrior_DerivesFromBaseStats()
    {
        var warrior = TestGameContent.Warrior();
        var profile = PlayerStats.Compute(warrior, PlayerProgress.Start(warrior), TestGameContent.Rules());
        Assert.Equal(1, profile.Level);
        Assert.Equal(50, profile.MaxLife);
        Assert.Equal(10, profile.MaxMana);
        Assert.Equal(4, profile.ArmorClass);
        Assert.Equal(71, profile.ToHit);
        Assert.Equal(4, profile.Damage.Min);
        Assert.Equal(6, profile.Damage.Max);
        Assert.Equal(0, profile.BlockChance);
    }

    [Fact]
    public void HigherLevel_AddsLifeManaAndToHit()
    {
        var warrior = TestGameContent.Warrior();
        var progress = new PlayerProgress("warrior", 3, 0, warrior.BaseStats, 0);
        var profile = PlayerStats.Compute(warrior, progress, TestGameContent.Rules());
        Assert.Equal(54, profile.MaxLife);
        Assert.Equal(12, profile.MaxMana);
        Assert.Equal(73, profile.ToHit);
    }
}
