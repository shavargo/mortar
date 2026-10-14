using Mortar.Core.Model;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Items;
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

/// <summary>A simple bot: drink when hurt, fight awake monsters nearby, spend points on strength, wear found gear in empty slots, pick up nearby loot, otherwise head for the exit and descend.</summary>
internal static class ScriptedPlayer
{
    private const int EngageDistance = 6;
    private const int LootDistance = 8;
    private const int DrinkBelowPercent = 40;

    public static ScriptedRun Run(GameContent content, uint seed, int ticks)
    {
        var created = World.Create(content.Content, content.CreateLevelGenerator(), "warrior", seed);
        var world = created.World ?? throw new InvalidOperationException(created.Error);
        var log = Play(world, ticks);
        return new ScriptedRun(log, WorldHash.Compute(world));
    }

    public static List<string> Play(World world, int ticks)
    {
        var log = new List<string>();
        for (var i = 0; i < ticks && !world.IsGameOver; i++)
        {
            log.AddRange(world.Tick(Decide(world)).Select(e => e.ToString()));
        }
        return log;
    }

    private static IReadOnlyList<Command> Decide(World world)
    {
        var player = world.Player;
        var drink = DrinkIfHurt(world);
        if (drink is not null) return new[] { drink };
        var target = NearestAwakeMonster(world);
        if (player.Intent == IntentKind.Attack)
        {
            return target is not null && ShouldSwitchTo(world, target) ? new Command[] { new AttackCommand(target.Id) } : Array.Empty<Command>();
        }
        if (target is not null) return new Command[] { new AttackCommand(target.Id) };
        if (player.Intent != IntentKind.None) return Array.Empty<Command>();
        var chore = SpendPoint(world) ?? EquipIntoEmptySlot(world) ?? PickUpNearbyLoot(world);
        if (chore is not null) return new[] { chore };
        var exit = world.Level.FindMarker(MarkerKinds.Exit);
        if (exit is null) return Array.Empty<Command>();
        return player.Cell.DistanceTo(exit.Value) <= 1
            ? new Command[] { new UseStairsCommand() }
            : new Command[] { new MoveCommand(exit.Value) };
    }

    private static Command? DrinkIfHurt(World world)
    {
        var player = world.Player;
        if (player.Life * 100 >= player.Profile.MaxLife * DrinkBelowPercent) return null;
        for (var slot = 0; slot < Belt.Slots; slot++)
        {
            if (world.Belongings.Belt.Get(slot)?.Definition.Potion?.Restores == PotionKinds.Life) return new UseBeltCommand(slot);
        }
        return null;
    }

    private static Command? SpendPoint(World world) =>
        world.Progress.UnspentPoints > 0 && world.Progress.BaseStats.Strength < world.PlayerClass.StatCaps.Strength
            ? new SpendStatPointCommand(StatKind.Strength)
            : null;

    private static Command? EquipIntoEmptySlot(World world)
    {
        var belongings = world.Belongings;
        foreach (var stored in belongings.Inventory.Items)
        {
            var definition = stored.Item.Definition;
            if (!definition.IsEquippable || !MeetsRequirements(world, definition)) continue;
            if (belongings.Equipment.Get(belongings.Equipment.SlotFor(definition)) is null) return new EquipCommand(stored.Item.Id);
        }
        return null;
    }

    private static bool MeetsRequirements(World world, ItemBaseDefinition definition) =>
        Stats.Kinds.All(kind => world.Progress.BaseStats.Get(kind) >= definition.Requirements.Get(kind));

    private static Command? PickUpNearbyLoot(World world)
    {
        var player = world.Player;
        GroundItem? best = null;
        foreach (var ground in world.Level.GroundItems)
        {
            var distance = ground.Cell.DistanceTo(player.Cell);
            if (distance > LootDistance || !HasRoomFor(world.Belongings, ground)) continue;
            if (best is not null && distance >= best.Cell.DistanceTo(player.Cell)) continue;
            if (Movement.IsReachable(world.Level, player.Cell, ground.Cell)) best = ground;
        }
        return best is null ? null : new PickUpCommand(best.Id);
    }

    private static bool HasRoomFor(Belongings belongings, GroundItem ground)
    {
        if (ground.Item is null) return true;
        var definition = ground.Item.Definition;
        return (definition.IsPotion && belongings.Belt.FirstEmpty() >= 0) || belongings.Inventory.FindSpace(definition) is not null;
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
