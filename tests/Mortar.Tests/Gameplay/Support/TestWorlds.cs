using Mortar.Game;
using Mortar.Game.Content;
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

    public static World Create(ContentSet? content = null, uint seed = 1, string[][]? levels = null)
    {
        var generator = new FixedLevelGenerator(levels ?? new[] { Room, Room });
        var result = World.Create(content ?? TestGameContent.Valid(), generator, "warrior", seed);
        return result.World ?? throw new InvalidOperationException(result.Error);
    }

    public static Actor Spawn(World world, string monsterId, int x, int y) =>
        world.SpawnMonster(world.Level, world.Content.Monster(monsterId), new Cell(x, y));
}
