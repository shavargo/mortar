using Mortar.Game;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class WorldCreationTests
{
    [Fact]
    public void Create_PlacesThePlayerAtTheEntrance()
    {
        var world = TestWorlds.Create();
        Assert.Equal(1, world.Depth);
        Assert.Equal(0, world.TickCount);
        Assert.Equal(new Cell(1, 1), world.Player.Cell);
        Assert.Equal(world.Player.Profile.MaxLife, world.Player.Life);
        Assert.Same(world.Player, world.Level.Find(Actor.PlayerId));
        Assert.Equal(new[] { 1 }, world.VisitedDepths);
    }

    [Fact]
    public void Create_UnknownClass_Fails()
    {
        var result = World.Create(TestGameContent.Valid(), new FixedLevelGenerator(TestWorlds.Room), "bard", 1);
        Assert.False(result.Succeeded);
        Assert.Equal("unknown class 'bard'", result.Error);
    }

    [Fact]
    public void Create_GeneratorFailure_Fails()
    {
        var result = World.Create(TestGameContent.Valid(), new FixedLevelGenerator(), "warrior", 1);
        Assert.Equal("level 1 could not be generated: no test layout", result.Error);
    }

    [Fact]
    public void Create_MissingEntrance_Fails()
    {
        var generator = new FixedLevelGenerator(new[] { "###", "#.#", "###" });
        var result = World.Create(TestGameContent.Valid(), generator, "warrior", 1);
        Assert.Equal("level 1 has no entrance marker", result.Error);
    }

    [Fact]
    public void LevelSeeds_DifferByDepthAndWorldSeed()
    {
        Assert.NotEqual(LevelSeeds.For(1, 1), LevelSeeds.For(1, 2));
        Assert.NotEqual(LevelSeeds.For(1, 1), LevelSeeds.For(2, 1));
        Assert.Equal(LevelSeeds.For(5, 3), LevelSeeds.For(5, 3));
    }
}
