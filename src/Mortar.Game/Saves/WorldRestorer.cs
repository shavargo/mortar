using Mortar.Game.Content;
using Mortar.Game.Simulation;

namespace Mortar.Game.Saves;

internal static class WorldRestorer
{
    public static WorldResult Restore(WorldSnapshot? snapshot, ContentSet content, ILevelGenerator generator)
    {
        if (snapshot is null) return WorldResult.Failure("save is empty");
        try
        {
            return WorldResult.Success(Build(snapshot, content, generator));
        }
        catch (SaveException ex)
        {
            return WorldResult.Failure($"save is invalid: {ex.Message}");
        }
    }

    private static World Build(WorldSnapshot snapshot, ContentSet content, ILevelGenerator generator)
    {
        Check.That(
            snapshot.Version == WorldSnapshot.CurrentVersion,
            $"unsupported version {snapshot.Version}; this build reads version {WorldSnapshot.CurrentVersion}");
        Check.That(snapshot.Tick >= 0, "tick must not be negative");
        Check.That(snapshot.Tick <= SaveLimits.MaxTotal, "tick is too large");
        Check.That(snapshot.NextActorId <= SaveLimits.MaxId, "nextActorId is too large");
        Check.That(snapshot.NextItemId <= SaveLimits.MaxId, "nextItemId is too large");
        Check.That((snapshot.RngIncrement & 1UL) == 1UL, "rngIncrement must be odd");
        var player = Check.Present(snapshot.Player, "player");
        var world = PlayerRestorer.CreateWorld(snapshot, player, content, generator);
        var ids = new RestoredIds();
        foreach (var level in Check.Present(snapshot.Levels, "levels"))
        {
            LevelRestorer.Restore(world, Check.Present(level, "level"), ids);
        }
        PlayerRestorer.Place(world, player, snapshot.Depth, ids);
        Check.That(snapshot.NextActorId > ids.MaxActorId, $"nextActorId must be greater than every actor id ({ids.MaxActorId})");
        Check.That(snapshot.NextItemId > ids.MaxItemId, $"nextItemId must be greater than every item id ({ids.MaxItemId})");
        world.TickCount = snapshot.Tick;
        world.NextActorId = snapshot.NextActorId;
        world.NextItemId = snapshot.NextItemId;
        return world;
    }
}
