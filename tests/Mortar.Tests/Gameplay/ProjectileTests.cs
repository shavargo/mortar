using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class ProjectileTests
{
    [Fact]
    public void Projectile_StepsOneCellPerTickTowardAim()
    {
        var world = TestWorlds.Create();
        var sink = new EventSink(0);
        var shot = ProjectileSystem.Spawn(
            world, world.Player, ProjectileKinds.Arrow, new Cell(5, 1), range: 10, damageMin: 1, damageMax: 1, sink);
        Assert.Equal(new Cell(1, 1), shot.Cell);
        var events = TestWorlds.Run(world, 3);
        var moves = events.OfType<ProjectileMoved>().Where(e => e.ProjectileId == shot.Id).ToList();
        Assert.Equal(3, moves.Count);
        Assert.Equal(new Cell(2, 1), moves[0].To);
        Assert.Equal(new Cell(3, 1), moves[1].To);
        Assert.Equal(new Cell(4, 1), moves[2].To);
        Assert.Equal(new Cell(4, 1), shot.Cell);
        Assert.Contains(shot, world.Level.Projectiles);
    }

    [Fact]
    public void Projectile_HitsMonsterAndAppliesDamage()
    {
        var world = TestWorlds.Create(TestGameContent.Valid(monsters: new[] { TestGameContent.Monster("zombie", sight: 1) }));
        var zombie = TestWorlds.Spawn(world, "zombie", 4, 1);
        var life = zombie.Life;
        var sink = new EventSink(0);
        var shot = ProjectileSystem.Spawn(
            world, world.Player, ProjectileKinds.Firebolt, zombie.Cell, range: 10, damageMin: 4, damageMax: 4, sink);
        var events = TestWorlds.Run(world, 3);
        var hit = Assert.Single(events.OfType<ProjectileHit>());
        Assert.Equal(shot.Id, hit.ProjectileId);
        Assert.Equal(zombie.Id, hit.TargetId);
        Assert.Equal(4, hit.Damage);
        Assert.Equal(life - 4, zombie.Life);
        Assert.DoesNotContain(shot, world.Level.Projectiles);
    }

    [Fact]
    public void Projectile_StopsAtWallWithoutEnteringIt()
    {
        var world = TestWorlds.Create();
        var sink = new EventSink(0);
        var shot = ProjectileSystem.Spawn(
            world, world.Player, ProjectileKinds.Arrow, new Cell(0, 1), range: 10, damageMin: 1, damageMax: 1, sink);
        var events = TestWorlds.Run(world, 1);
        var stopped = Assert.Single(events.OfType<ProjectileStopped>());
        Assert.Equal(ProjectileSystem.StoppedByWall, stopped.Reason);
        Assert.Equal(new Cell(1, 1), stopped.Cell);
        Assert.Empty(events.OfType<ProjectileMoved>());
        Assert.DoesNotContain(shot, world.Level.Projectiles);
    }

    [Fact]
    public void Projectile_ExpiresWhenRangeRunsOut()
    {
        var world = TestWorlds.Create();
        var sink = new EventSink(0);
        var shot = ProjectileSystem.Spawn(
            world, world.Player, ProjectileKinds.Arrow, new Cell(7, 1), range: 2, damageMin: 1, damageMax: 1, sink);
        var events = TestWorlds.Run(world, 3);
        Assert.Equal(2, events.OfType<ProjectileMoved>().Count(e => e.ProjectileId == shot.Id));
        var stopped = Assert.Single(events.OfType<ProjectileStopped>());
        Assert.Equal(ProjectileSystem.StoppedByRange, stopped.Reason);
        Assert.Equal(new Cell(3, 1), stopped.Cell);
        Assert.DoesNotContain(shot, world.Level.Projectiles);
    }

    [Fact]
    public void Projectile_DoesNotHitOwnerOnSpawnCell()
    {
        var world = TestWorlds.Create();
        var sink = new EventSink(0);
        ProjectileSystem.Spawn(
            world, world.Player, ProjectileKinds.Arrow, new Cell(5, 1), range: 5, damageMin: 50, damageMax: 50, sink);
        var events = TestWorlds.Run(world, 1);
        Assert.Empty(events.OfType<ProjectileHit>());
        Assert.Equal(world.Player.Profile.MaxLife, world.Player.Life);
    }

    [Fact]
    public void PlayerProjectile_IgnoresPassingThroughWithoutMonsterHit()
    {
        var world = TestWorlds.Create();
        var sink = new EventSink(0);
        ProjectileSystem.Spawn(
            world, world.Player, ProjectileKinds.Arrow, new Cell(4, 1), range: 5, damageMin: 1, damageMax: 1, sink);
        var events = TestWorlds.Run(world, 3);
        Assert.Empty(events.OfType<ProjectileHit>());
        Assert.Equal(world.Player.Profile.MaxLife, world.Player.Life);
    }

    [Fact]
    public void MonsterProjectile_HitsPlayerOnly()
    {
        var world = TestWorlds.Create();
        var archer = TestWorlds.Spawn(world, "zombie", 4, 1);
        var other = TestWorlds.Spawn(world, "zombie", 3, 1);
        var otherLife = other.Life;
        var sink = new EventSink(0);
        var shot = ProjectileSystem.Spawn(
            world, archer, ProjectileKinds.Arrow, world.Player.Cell, range: 10, damageMin: 3, damageMax: 3, sink);
        var events = TestWorlds.Run(world, 3);
        var hit = Assert.Single(events.OfType<ProjectileHit>());
        Assert.Equal(shot.Id, hit.ProjectileId);
        Assert.Equal(Actor.PlayerId, hit.TargetId);
        Assert.Equal(3, hit.Damage);
        Assert.Equal(otherLife, other.Life);
        Assert.Equal(world.Player.Profile.MaxLife - 3, world.Player.Life);
    }

    [Fact]
    public void Projectiles_AdvanceInAscendingIdOrder()
    {
        var world = TestWorlds.Create();
        var east = TestWorlds.Spawn(world, "zombie", 2, 1);
        var south = TestWorlds.Spawn(world, "zombie", 1, 2);
        var sink = new EventSink(0);
        var first = ProjectileSystem.Spawn(
            world, world.Player, ProjectileKinds.Arrow, east.Cell, range: 5, damageMin: 100, damageMax: 100, sink);
        var second = ProjectileSystem.Spawn(
            world, world.Player, ProjectileKinds.Arrow, south.Cell, range: 5, damageMin: 100, damageMax: 100, sink);
        Assert.True(first.Id < second.Id);
        var events = TestWorlds.Run(world, 1);
        var hits = events.OfType<ProjectileHit>().ToList();
        Assert.Equal(2, hits.Count);
        Assert.Equal(first.Id, hits[0].ProjectileId);
        Assert.Equal(east.Id, hits[0].TargetId);
        Assert.Equal(second.Id, hits[1].ProjectileId);
        Assert.Equal(south.Id, hits[1].TargetId);
    }

    [Fact]
    public void LeavingLevel_ClearsInFlightProjectiles()
    {
        var world = TestWorlds.Create();
        var sink = new EventSink(0);
        ProjectileSystem.Spawn(
            world, world.Player, ProjectileKinds.Arrow, new Cell(5, 1), range: 10, damageMin: 1, damageMax: 1, sink);
        Assert.NotEmpty(world.Level.Projectiles);
        world.Level.Move(world.Player, new Cell(6, 4));
        var events = world.Tick(new Command[] { new UseStairsCommand() });
        Assert.Contains(events.OfType<LevelChanged>(), e => e.ToDepth == 2);
        Assert.Empty(world.GetLevel(1).Projectiles);
        Assert.Empty(world.Level.Projectiles);
    }
}

