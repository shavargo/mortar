using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class PortalTests
{
    private static World MageInDungeon()
    {
        var portal = new SpellDefinition(
            "town-portal", "Town Portal", SpellShapes.Self, 20, 6, 3, 0, null, 0, 0, 0, 0);
        var result = ContentSet.Create(
            TestGameContent.Rules(),
            new[]
            {
                new ClassDefinition(
                    "mage", "Mage", "mage",
                    new Stats(10, 10, 10, 40),
                    new Stats(100, 100, 100, 100),
                    new ClassGrowth(10, 1, 1, 40, 2, 2, 5),
                    new IntRange(1, 2),
                    TestGameContent.Timings(),
                    Array.Empty<string>(),
                    new[] { "town-portal" }),
            },
            new[] { TestGameContent.Monster("zombie", sight: 1) },
            TestGameContent.WithTown(),
            TestGameContent.Items(),
            new SpellCatalog(new[] { portal }));
        var world = TestWorlds.CreateWithTown(result.Value, classId: "mage");
        world.Level.Move(world.Player, new Cell(4, 1));
        world.Tick(new Command[] { new UseStairsCommand() });
        Assert.Equal(1, world.Depth);
        return world;
    }

    [Fact]
    public void TownPortal_OpensPairAndWarpsBothWays()
    {
        var world = MageInDungeon();
        var dungeonCell = world.Player.Cell;
        var events = TestWorlds.Run(world, 4, new CastCommand("town-portal"));
        Assert.Contains(events.OfType<PortalOpened>(), e => e.DungeonDepth == 1);
        Assert.NotNull(world.Portal);
        Assert.Equal(dungeonCell, world.Portal!.DungeonCell);

        var toTown = world.Tick(new Command[] { new UsePortalCommand() });
        Assert.Equal(0, world.Depth);
        Assert.Contains(toTown.OfType<PortalUsed>(), e => e.ToDepth == 0);
        Assert.Equal(world.Portal.TownCell, world.Player.Cell);

        var back = world.Tick(new Command[] { new UsePortalCommand() });
        Assert.Equal(1, world.Depth);
        Assert.Equal(dungeonCell, world.Player.Cell);
        Assert.Contains(back.OfType<PortalUsed>(), e => e.ToDepth == 1);
    }

    [Fact]
    public void TownPortal_RejectedInTown()
    {
        var world = MageInDungeon();
        TestWorlds.Run(world, 4, new CastCommand("town-portal"));
        world.Tick(new Command[] { new UsePortalCommand() });
        Assert.Equal(0, world.Depth);
        world.Player.Mana = 50;
        var events = world.Tick(new Command[] { new CastCommand("town-portal") });
        Assert.Equal("cannot open a portal in town", Assert.Single(events.OfType<CommandRejected>()).Reason);
    }
}
