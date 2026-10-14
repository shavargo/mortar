using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class AiTests
{
    private static readonly string[] Hall =
    {
        "####################",
        "#E.................#",
        "####################",
    };

    private static World HallWorld(params MonsterDefinition[] monsters)
    {
        var spawnable = monsters[0].Id;
        var levels = new[] { TestGameContent.Level(1, spawnable), TestGameContent.Level(2, spawnable) };
        return TestWorlds.Create(TestGameContent.Valid(monsters, levels), levels: new[] { Hall, Hall });
    }

    [Fact]
    public void MonsterOutOfSight_StaysAsleep()
    {
        var world = HallWorld(TestGameContent.Monster("zombie", sight: 3));
        var zombie = TestWorlds.Spawn(world, "zombie", 15, 1);
        var events = TestWorlds.Run(world, 20);
        Assert.False(zombie.IsAwake);
        Assert.DoesNotContain(events, e => e is ActorMoved moved && moved.ActorId == zombie.Id);
    }

    [Fact]
    public void MonsterInSight_WakesChasesAndAttacks()
    {
        var world = HallWorld(TestGameContent.Monster("zombie", sight: 10));
        var zombie = TestWorlds.Spawn(world, "zombie", 8, 1);
        var events = TestWorlds.Run(world, 60);
        var awoke = events.OfType<MonsterAwoke>().First();
        Assert.Equal(zombie.Id, awoke.ActorId);
        Assert.Equal(1, awoke.Tick);
        Assert.Contains(events, e => e is ActorMoved moved && moved.ActorId == zombie.Id);
        Assert.Contains(events, e => e is AttackStarted swing && swing.AttackerId == zombie.Id && swing.TargetId == Actor.PlayerId);
    }

    [Fact]
    public void BeingHit_WakesAMonster()
    {
        var world = HallWorld(TestGameContent.Monster("zombie", sight: 1));
        var zombie = TestWorlds.Spawn(world, "zombie", 15, 1);
        zombie.LastAttackerId = Actor.PlayerId;
        var awoke = Assert.Single(world.Tick(Array.Empty<Command>()).OfType<MonsterAwoke>());
        Assert.Equal(zombie.Id, awoke.ActorId);
        Assert.True(zombie.IsAwake);
    }

    [Fact]
    public void Pack_WakesTogether()
    {
        var world = HallWorld(
            TestGameContent.Monster("imp", sight: 1),
            TestGameContent.Monster("chief", AiKinds.PackLeader, new MinionSpec("imp", new IntRange(2, 2)), sight: 1));
        var chief = TestWorlds.Spawn(world, "chief", 15, 1);
        var first = TestWorlds.Spawn(world, "imp", 16, 1);
        var second = TestWorlds.Spawn(world, "imp", 17, 1);
        first.LeaderId = chief.Id;
        second.LeaderId = chief.Id;
        second.LastAttackerId = Actor.PlayerId;
        var awoke = world.Tick(Array.Empty<Command>()).OfType<MonsterAwoke>().Select(e => e.ActorId);
        Assert.Equal(new[] { chief.Id, first.Id, second.Id }, awoke.OrderBy(id => id));
    }
}
