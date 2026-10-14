using Mortar.Core.Model;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Pathing;
using Mortar.Game.Simulation;
using Mortar.Json.Content;

namespace Mortar.Tests.Gameplay.Support;

internal sealed class ScriptedRun
{
    public ScriptedRun(IReadOnlyList<string> log, string hash)
    {
        Log = log;
        Hash = hash;
    }

    public IReadOnlyList<string> Log { get; }

    public string Hash { get; }
}

/// <summary>A simple bot: fight awake monsters nearby, spend points on strength, otherwise head for the exit and descend.</summary>
internal static class ScriptedPlayer
{
    private const int EngageDistance = 6;

    public static ScriptedRun Run(GameContent content, uint seed, int ticks)
    {
        var created = World.Create(content.Content, content.CreateLevelGenerator(), "warrior", seed);
        var world = created.World ?? throw new InvalidOperationException(created.Error);
        var log = new List<string>();
        for (var i = 0; i < ticks && !world.IsGameOver; i++)
        {
            log.AddRange(world.Tick(Decide(world)).Select(e => e.ToString()));
        }
        return new ScriptedRun(log, WorldHash.Compute(world));
    }

    private static IReadOnlyList<Command> Decide(World world)
    {
        var player = world.Player;
        var target = NearestAwakeMonster(world);
        if (player.Intent == IntentKind.Attack)
        {
            return target is not null && ShouldSwitchTo(world, target) ? new Command[] { new AttackCommand(target.Id) } : Array.Empty<Command>();
        }
        if (target is not null) return new Command[] { new AttackCommand(target.Id) };
        if (player.Intent != IntentKind.None) return Array.Empty<Command>();
        if (world.Progress.UnspentPoints > 0 && world.Progress.BaseStats.Strength < world.PlayerClass.StatCaps.Strength)
        {
            return new Command[] { new SpendStatPointCommand(StatKind.Strength) };
        }
        var exit = world.Level.FindMarker(MarkerKinds.Exit);
        if (exit is null) return Array.Empty<Command>();
        return player.Cell.DistanceTo(exit.Value) <= 1
            ? new Command[] { new UseStairsCommand() }
            : new Command[] { new MoveCommand(exit.Value) };
    }

    /// <summary>Drop a target that is out of reach (for example boxed in by its pack) when another monster can be struck now.</summary>
    private static bool ShouldSwitchTo(World world, Actor target)
    {
        var player = world.Player;
        var current = world.Level.Find(player.IntentTargetId);
        return current is not null
            && !Reach.CanStrike(player.Cell, current.Cell, world.Level.IsWalkable)
            && Reach.CanStrike(player.Cell, target.Cell, world.Level.IsWalkable);
    }

    private static Actor? NearestAwakeMonster(World world)
    {
        Actor? best = null;
        foreach (var actor in world.Level.Actors)
        {
            if (actor.Kind != ActorKind.Monster || !actor.IsAlive || !actor.IsAwake) continue;
            var distance = actor.Cell.DistanceTo(world.Player.Cell);
            if (distance > EngageDistance) continue;
            if (best is null || distance < best.Cell.DistanceTo(world.Player.Cell)) best = actor;
        }
        return best;
    }
}
