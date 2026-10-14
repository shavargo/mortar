using Mortar.Core.Model;
using Mortar.Game.Commands;
using Mortar.Game.Content;
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
        if (player.Intent == IntentKind.Attack) return Array.Empty<Command>();
        var target = NearestAwakeMonster(world);
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
