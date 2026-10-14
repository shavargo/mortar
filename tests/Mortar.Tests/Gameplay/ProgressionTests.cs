using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class ProgressionTests
{
    [Fact]
    public void KillThatCrossesAThreshold_LevelsUpAndRestoresLife()
    {
        var rules = TestGameContent.Rules(minHit: 100, maxHit: 100, thresholds: new long[] { 40, 300, 600 });
        var world = TestWorlds.Create(TestGameContent.Valid(rules: rules));
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        world.Player.Life = 30;
        var events = TestWorlds.Run(world, 40, new AttackCommand(zombie.Id));
        Assert.Equal(2, events.OfType<LeveledUp>().Single().Level);
        Assert.Equal(5, world.Progress.UnspentPoints);
        Assert.Equal(52, world.Player.Profile.MaxLife);
        Assert.Equal(52, world.Player.Life);
    }

    [Fact]
    public void SpendStatPoint_WithoutPoints_IsRejected()
    {
        var world = TestWorlds.Create();
        var rejected = world.Tick(new Command[] { new SpendStatPointCommand(StatKind.Strength) }).OfType<CommandRejected>().Single();
        Assert.Equal("no unspent stat points", rejected.Reason);
    }

    [Fact]
    public void SpendStatPoint_RaisesVitalityAndLife()
    {
        var world = TestWorlds.Create();
        world.Progress.UnspentPoints = 1;
        var life = world.Player.Life;
        var maxLife = world.Player.Profile.MaxLife;
        var events = world.Tick(new Command[] { new SpendStatPointCommand(StatKind.Vitality) });
        var increased = events.OfType<StatIncreased>().Single();
        Assert.Equal(StatKind.Vitality, increased.Stat);
        Assert.Equal(21, increased.Value);
        Assert.Equal(maxLife + 2, world.Player.Profile.MaxLife);
        Assert.Equal(life + 2, world.Player.Life);
    }
}
