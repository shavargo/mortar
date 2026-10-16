using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Simulation;

namespace Mortar.Tests.Gameplay.Support;

internal static class TestWorlds
{
    public static readonly string[] Room =
    {
        "#########",
        "#E......#",
        "#.......#",
        "#.......#",
        "#......X#",
        "#########",
    };

    public static readonly string[] Town =
    {
        "#######",
        "#E.S.X#",
        "#.....#",
        "#######",
    };

    public static World Create(ContentSet? content = null, uint seed = 1, string[][]? levels = null, string classId = "warrior")
    {
        var generator = new FixedLevelGenerator(levels ?? new[] { Room, Room });
        var result = World.Create(content ?? TestGameContent.Valid(), generator, classId, seed);
        return result.World ?? throw new InvalidOperationException(result.Error);
    }

    public static World CreateWithTown(ContentSet? content = null, string classId = "warrior")
    {
        var generator = FixedLevelGenerator.WithTown(Town, Room, Room);
        var result = World.Create(content ?? TestGameContent.Valid(levels: TestGameContent.WithTown()), generator, classId, 1);
        return result.World ?? throw new InvalidOperationException(result.Error);
    }

    public static Actor Spawn(World world, string monsterId, int x, int y) =>
        world.SpawnMonster(world.Level, world.Content.Monster(monsterId), new Cell(x, y));

    public static List<GameEvent> Run(World world, int ticks, params Command[] first)
    {
        var events = new List<GameEvent>(world.Tick(first));
        for (var i = 1; i < ticks; i++)
        {
            events.AddRange(world.Tick(Array.Empty<Command>()));
        }
        return events;
    }

    public static GroundItem DropGold(World world, int x, int y, int amount)
    {
        var ground = new GroundItem(world.NextItemId++, new Cell(x, y), null, amount);
        world.Level.AddGroundItem(ground);
        return ground;
    }

    public static GroundItem DropItem(World world, int x, int y, ItemBaseDefinition definition)
    {
        var item = ItemFactory.CreatePlain(world, definition);
        var ground = new GroundItem(item.Id, new Cell(x, y), item, 0);
        world.Level.AddGroundItem(ground);
        return ground;
    }

    public static Item Give(World world, ItemBaseDefinition definition)
    {
        var item = ItemFactory.CreatePlain(world, definition);
        return world.Belongings.Stow(item) ? item : throw new InvalidOperationException($"no room for {definition.Id}");
    }
}
