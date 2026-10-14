using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class CombatTests
{
    private static World Arena(GameRules? rules = null) =>
        TestWorlds.Create(TestGameContent.Valid(rules: rules ?? TestGameContent.Rules(minHit: 100, maxHit: 100)));

    [Fact]
    public void Attack_SwingsAndLandsOnTheHitFrame()
    {
        var world = Arena();
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        var events = TestWorlds.Run(world, 4, new AttackCommand(zombie.Id));
        var started = events.OfType<AttackStarted>().First(e => e.AttackerId == Actor.PlayerId);
        Assert.Equal(1, started.Tick);
        Assert.Equal(6, started.DurationTicks);
        var hit = events.OfType<AttackResolved>().Single(e => e.AttackerId == Actor.PlayerId);
        Assert.Equal(4, hit.Tick);
        Assert.Equal(AttackOutcome.Hit, hit.Outcome);
        Assert.InRange(hit.Damage, 4, 6);
        Assert.Equal(10 - hit.Damage, zombie.Life);
    }

    [Fact]
    public void AttackingUntilDeath_RewardsExperience()
    {
        var world = Arena();
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        var events = TestWorlds.Run(world, 40, new AttackCommand(zombie.Id));
        var died = events.OfType<ActorDied>().Single();
        Assert.Equal(zombie.Id, died.ActorId);
        Assert.Equal(Actor.PlayerId, died.KillerId);
        Assert.True(zombie.IsDead);
        Assert.True(world.Level.IsFree(new Cell(2, 1)));
        Assert.Equal(50, events.OfType<ExperienceGained>().Single().Amount);
        Assert.Equal(IntentKind.None, world.Player.Intent);
    }

    [Fact]
    public void ZeroHitChance_AlwaysMisses()
    {
        var world = Arena(TestGameContent.Rules(minHit: 0, maxHit: 0));
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        var events = TestWorlds.Run(world, 4, new AttackCommand(zombie.Id));
        var miss = events.OfType<AttackResolved>().Single(e => e.AttackerId == Actor.PlayerId);
        Assert.Equal(AttackOutcome.Miss, miss.Outcome);
        Assert.Equal(0, miss.Damage);
        Assert.Equal(10, zombie.Life);
    }

    [Fact]
    public void StrongHit_StaggersTheTarget()
    {
        var world = Arena();
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        var events = TestWorlds.Run(world, 4, new AttackCommand(zombie.Id));
        var staggered = events.OfType<ActorStaggered>().Single();
        Assert.Equal(zombie.Id, staggered.ActorId);
        Assert.Equal(3, staggered.DurationTicks);
        Assert.Equal(ActionKind.Recovering, zombie.Action);
    }

    [Fact]
    public void StaggeredAttacker_DoesNotLandItsSwing()
    {
        var world = Arena();
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        zombie.SetAttackIntent(Actor.PlayerId);
        var events = TestWorlds.Run(world, 4, new AttackCommand(zombie.Id));
        Assert.Equal(2, events.OfType<AttackStarted>().Count());
        var resolved = Assert.Single(events.OfType<AttackResolved>());
        Assert.Equal(Actor.PlayerId, resolved.AttackerId);
        Assert.Equal(world.Player.Profile.MaxLife, world.Player.Life);
    }

    [Fact]
    public void AttackingAMissingOrDeadTarget_IsRejected()
    {
        var world = Arena();
        var missing = world.Tick(new Command[] { new AttackCommand(99) }).OfType<CommandRejected>().Single();
        Assert.Equal("no such monster on this level", missing.Reason);
        var self = world.Tick(new Command[] { new AttackCommand(Actor.PlayerId) }).OfType<CommandRejected>().Single();
        Assert.Equal("no such monster on this level", self.Reason);
        var zombie = TestWorlds.Spawn(world, "zombie", 5, 3);
        zombie.Life = 0;
        world.Tick(Array.Empty<Command>());
        var dead = world.Tick(new Command[] { new AttackCommand(zombie.Id) }).OfType<CommandRejected>().Single();
        Assert.Equal("target is already dead", dead.Reason);
    }

    [Fact]
    public void DeadPlayer_CannotAct()
    {
        var world = Arena();
        world.Player.Life = 0;
        var died = world.Tick(Array.Empty<Command>()).OfType<ActorDied>().Single();
        Assert.Equal(Actor.PlayerId, died.ActorId);
        Assert.True(world.IsGameOver);
        var rejected = world.Tick(new Command[] { new MoveCommand(new Cell(3, 3)) }).OfType<CommandRejected>().Single();
        Assert.Equal("the player is dead", rejected.Reason);
    }

    [Fact]
    public void DistantTarget_IsApproachedThenAttacked()
    {
        var world = Arena();
        var zombie = TestWorlds.Spawn(world, "zombie", 6, 1);
        var events = TestWorlds.Run(world, 30, new AttackCommand(zombie.Id));
        Assert.Contains(events, e => e is ActorMoved moved && moved.ActorId == Actor.PlayerId);
        var swing = events.OfType<AttackStarted>().First(e => e.AttackerId == Actor.PlayerId);
        Assert.Equal(zombie.Id, swing.TargetId);
    }

    [Theory]
    [InlineData(70, 4, 1, 1, 66)]
    [InlineData(70, 4, 5, 1, 74)]
    [InlineData(10, 40, 1, 1, 5)]
    [InlineData(200, 0, 1, 1, 95)]
    public void HitChance_IsClampedToTheRuleLimits(int toHit, int armor, int attackerLevel, int defenderLevel, int expected)
    {
        var timings = TestGameContent.Timings();
        var attacker = new CombatProfile(attackerLevel, 10, 0, 0, toHit, new IntRange(1, 2), 0, timings);
        var defender = new CombatProfile(defenderLevel, 10, 0, armor, 0, new IntRange(1, 2), 0, timings);
        Assert.Equal(expected, HitChance.Compute(attacker, defender, TestGameContent.Rules()));
    }
}
