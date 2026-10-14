using Mortar.Game;
using Mortar.Game.Content;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class SpawnerTests
{
    private static readonly string[] SpawnRoom =
    {
        "###########",
        "#E.S.....S#",
        "#.........#",
        "#........S#",
        "###########",
    };

    private static readonly string[] OneSpawn =
    {
        "###########",
        "#E.......S#",
        "#.........#",
        "###########",
    };

    private static readonly string[] BossRoom =
    {
        "###########",
        "#E........#",
        "#........B#",
        "###########",
    };

    [Fact]
    public void Populate_SpawnsAtMarkersAwayFromTheEntrance()
    {
        var world = TestWorlds.Create(levels: new[] { SpawnRoom, TestWorlds.Room });
        var monsters = world.Level.Actors.Where(a => a.Kind == ActorKind.Monster).ToList();
        Assert.Equal(new[] { new Cell(9, 1), new Cell(9, 3) }, monsters.Select(m => m.Cell));
        Assert.Equal(new[] { 2, 3 }, monsters.Select(m => m.Id));
    }

    [Fact]
    public void PackLeader_SpawnsWithMinions()
    {
        var monsters = new[]
        {
            TestGameContent.Monster("imp"),
            TestGameContent.Monster("chief", AiKinds.PackLeader, new MinionSpec("imp", new IntRange(2, 2))),
        };
        var levels = new[] { TestGameContent.Level(1, "chief"), TestGameContent.Level(2, "imp") };
        var world = TestWorlds.Create(TestGameContent.Valid(monsters, levels), levels: new[] { OneSpawn, TestWorlds.Room });
        var chief = world.Level.Actors.Single(a => a.DefinitionId == "chief");
        var imps = world.Level.Actors.Where(a => a.DefinitionId == "imp").ToList();
        Assert.Equal(2, imps.Count);
        Assert.All(imps, imp => Assert.Equal(chief.Id, imp.LeaderId));
        Assert.All(imps, imp => Assert.True(imp.Cell.DistanceTo(chief.Cell) <= MonsterSpawner.PlacementRadius));
    }

    [Fact]
    public void Boss_SpawnsAtTheBossMarker()
    {
        var monsters = new[] { TestGameContent.Monster("zombie"), TestGameContent.Monster("warden") };
        var levels = new[] { TestGameContent.Level(1, bossId: "warden"), TestGameContent.Level(2) };
        var world = TestWorlds.Create(TestGameContent.Valid(monsters, levels), levels: new[] { BossRoom, TestWorlds.Room });
        Assert.Equal(new Cell(9, 2), world.Level.Actors.Single(a => a.DefinitionId == "warden").Cell);
    }

    [Fact]
    public void SameSeed_SpawnsTheSameMonsters()
    {
        Assert.Equal(Describe(MixedWorld(5)), Describe(MixedWorld(5)));
    }

    private static World MixedWorld(uint seed)
    {
        var monsters = new[] { TestGameContent.Monster("zombie"), TestGameContent.Monster("imp", life: 4) };
        var mixed = new LevelDefinition(1, "test", new[] { new SpawnEntry("zombie", 1), new SpawnEntry("imp", 1) }, null);
        var content = TestGameContent.Valid(monsters, new[] { mixed, TestGameContent.Level(2) });
        return TestWorlds.Create(content, seed, new[] { SpawnRoom, TestWorlds.Room });
    }

    private static IEnumerable<string> Describe(World world) =>
        world.Level.Actors.Select(a => $"{a.Id} {a.DefinitionId} {a.Cell} {a.Life}");
}
