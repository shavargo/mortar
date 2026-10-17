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
        var fromV1 = snapshot.Version == 1;
        if (!SaveMigration.TryMigrate(snapshot, out var migrateError))
        {
            throw new SaveException(migrateError!);
        }
        Check.That(
            snapshot.Version == WorldSnapshot.CurrentVersion,
            $"unsupported version {snapshot.Version}; this build reads version {WorldSnapshot.CurrentVersion}");
        Check.That(snapshot.Tick >= 0, "tick must not be negative");
        Check.That(snapshot.Tick <= SaveLimits.MaxTotal, "tick is too large");
        Check.That(snapshot.NextActorId <= SaveLimits.MaxId, "nextActorId is too large");
        Check.That(snapshot.NextItemId <= SaveLimits.MaxId, "nextItemId is too large");
        Check.That(snapshot.NextProjectileId <= SaveLimits.MaxId, "nextProjectileId is too large");
        Check.That(snapshot.NextProjectileId >= 1, "nextProjectileId must be at least 1");
        Check.That((snapshot.RngIncrement & 1UL) == 1UL, "rngIncrement must be odd");
        var player = Check.Present(snapshot.Player, "player");
        var world = PlayerRestorer.CreateWorld(snapshot, player, content, generator);
        var ids = new RestoredIds();
        foreach (var level in Check.Present(snapshot.Levels, "levels"))
        {
            LevelRestorer.Restore(world, Check.Present(level, "level"), ids, fromV1);
        }
        PlayerRestorer.Place(world, player, snapshot.Depth, ids, fromV1);
        Check.That(snapshot.NextActorId > ids.MaxActorId, $"nextActorId must be greater than every actor id ({ids.MaxActorId})");
        Check.That(snapshot.NextItemId > ids.MaxItemId, $"nextItemId must be greater than every item id ({ids.MaxItemId})");
        Check.That(snapshot.NextProjectileId > ids.MaxProjectileId, $"nextProjectileId must be greater than every projectile id ({ids.MaxProjectileId})");
        world.TickCount = snapshot.Tick;
        world.NextActorId = snapshot.NextActorId;
        world.NextItemId = snapshot.NextItemId;
        world.NextProjectileId = snapshot.NextProjectileId;
        if (snapshot.Portal is { } portal)
        {
            world.Portal = new PortalPair(
                portal.DungeonDepth,
                new Cell(portal.DungeonX, portal.DungeonY),
                new Cell(portal.TownX, portal.TownY));
        }
        return world;
    }
}
