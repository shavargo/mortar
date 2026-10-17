using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class RangedKiteTests
{
    private static ContentSet ArcherContent()
    {
        var archer = TestGameContent.Monster(
            "archer", ai: AiKinds.RangedKite, life: 20, sight: 8, experience: 10, preferredRange: 4);
        var result = ContentSet.Create(
            TestGameContent.Rules(minHit: 100, maxHit: 100),
            new[] { TestGameContent.Warrior() },
            new[] { archer },
            new[] { TestGameContent.Level(1, "archer"), TestGameContent.Level(2, "archer") },
            TestGameContent.Items());
        return result.Value ?? throw new InvalidOperationException(string.Join("; ", result.Errors));
    }

    [Fact]
    public void RangedKite_ShootsFromPreferredRange()
    {
        var world = TestWorlds.Create(ArcherContent());
        var archer = TestWorlds.Spawn(world, "archer", 5, 1);
        archer.IsAwake = true;
        var events = TestWorlds.Run(world, 20);
        Assert.Contains(events.OfType<AttackStarted>(), e => e.AttackerId == archer.Id);
        Assert.Contains(events.OfType<ProjectileSpawned>(), e => e.OwnerId == archer.Id);
    }

    [Fact]
    public void RangedKite_StepsAwayWhenAdjacent()
    {
        var world = TestWorlds.Create(ArcherContent());
        var archer = TestWorlds.Spawn(world, "archer", 2, 1);
        archer.IsAwake = true;
        var start = archer.Cell;
        TestWorlds.Run(world, 3);
        Assert.True(archer.Cell.DistanceTo(world.Player.Cell) >= start.DistanceTo(world.Player.Cell));
        Assert.NotEqual(start, archer.Cell);
    }
}
