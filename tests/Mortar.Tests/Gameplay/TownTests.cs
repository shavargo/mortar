using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Events;
using Mortar.Game.Saves;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class TownTests
{
    private static readonly Command[] Stairs = { new UseStairsCommand() };

    [Fact]
    public void NewGame_StartsInTownAtItsEntrance()
    {
        var world = TestWorlds.CreateWithTown();
        Assert.Equal(0, world.Depth);
        Assert.Equal(new Cell(1, 1), world.Player.Cell);
        Assert.Equal(Actor.PlayerId, Assert.Single(world.Level.Actors).Id);
    }

    [Fact]
    public void TownExit_LeadsToLevelOne()
    {
        var world = TestWorlds.CreateWithTown();
        var town = world.Level;
        town.Move(world.Player, new Cell(4, 1));
        var changed = world.Tick(Stairs).OfType<LevelChanged>().Single();
        Assert.Equal((0, 1), (changed.FromDepth, changed.ToDepth));
        Assert.Equal(new Cell(1, 1), world.Player.Cell);
        Assert.Null(town.Find(Actor.PlayerId));
    }

    [Fact]
    public void LevelOneEntrance_LeadsBackToTheTownExit()
    {
        var world = TestWorlds.CreateWithTown();
        var town = world.Level;
        town.Move(world.Player, new Cell(4, 1));
        world.Tick(Stairs);
        world.Tick(Stairs);
        Assert.Equal(0, world.Depth);
        Assert.Same(town, world.Level);
        Assert.Equal(new Cell(5, 1), world.Player.Cell);
    }

    [Fact]
    public void TownEntrance_IsRejected()
    {
        var world = TestWorlds.CreateWithTown();
        Assert.Equal("there is nothing above", world.Tick(Stairs).OfType<CommandRejected>().Single().Reason);
    }

    [Fact]
    public void Save_RoundTripsInTown() => AssertRoundTrip(TestWorlds.CreateWithTown());

    [Fact]
    public void Save_RoundTripsAfterLeavingTown()
    {
        var world = TestWorlds.CreateWithTown();
        world.Level.Move(world.Player, new Cell(4, 1));
        world.Tick(Stairs);
        AssertRoundTrip(world);
    }

    private static void AssertRoundTrip(World world)
    {
        var generator = FixedLevelGenerator.WithTown(TestWorlds.Town, TestWorlds.Room, TestWorlds.Room);
        var restored = World.Restore(WorldSnapshot.Capture(world), world.Content, generator);
        Assert.True(restored.Succeeded, restored.Error);
        Assert.Equal(WorldHash.Compute(world), WorldHash.Compute(restored.World!));
    }
}
