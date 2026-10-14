using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class StairsTests
{
    private static readonly Command[] Stairs = { new UseStairsCommand() };

    [Fact]
    public void UsingTheExit_DescendsToTheNextLevelEntrance()
    {
        var world = TestWorlds.Create();
        var firstLevel = world.Level;
        firstLevel.Move(world.Player, new Cell(6, 4));
        var changed = world.Tick(Stairs).OfType<LevelChanged>().Single();
        Assert.Equal(1, changed.FromDepth);
        Assert.Equal(2, changed.ToDepth);
        Assert.Equal(new Cell(1, 1), changed.Cell);
        Assert.Equal(2, world.Depth);
        Assert.Equal(new Cell(1, 1), world.Player.Cell);
        Assert.Equal(new[] { 1, 2 }, world.VisitedDepths);
        Assert.Null(firstLevel.Find(Actor.PlayerId));
        Assert.True(firstLevel.IsFree(new Cell(6, 4)));
    }

    [Fact]
    public void UsingTheEntrance_ReturnsToTheSameLevelAtItsExit()
    {
        var world = TestWorlds.Create();
        var firstLevel = world.Level;
        firstLevel.Move(world.Player, new Cell(6, 4));
        world.Tick(Stairs);
        var changed = world.Tick(Stairs).OfType<LevelChanged>().Single();
        Assert.Equal(1, changed.ToDepth);
        Assert.Same(firstLevel, world.Level);
        Assert.Equal(new Cell(7, 4), world.Player.Cell);
    }

    [Fact]
    public void AwayFromStairs_IsRejected()
    {
        var world = TestWorlds.Create();
        world.Level.Move(world.Player, new Cell(4, 2));
        Assert.Equal("no stairs within reach", world.Tick(Stairs).OfType<CommandRejected>().Single().Reason);
    }

    [Fact]
    public void LevelOneEntrance_IsRejected()
    {
        var world = TestWorlds.Create();
        Assert.Equal("there is no level above", world.Tick(Stairs).OfType<CommandRejected>().Single().Reason);
    }

    [Fact]
    public void DeepestExit_IsRejected()
    {
        var world = TestWorlds.Create();
        world.Level.Move(world.Player, new Cell(6, 4));
        world.Tick(Stairs);
        world.Level.Move(world.Player, new Cell(6, 4));
        Assert.Equal("there is no deeper level", world.Tick(Stairs).OfType<CommandRejected>().Single().Reason);
    }
}
