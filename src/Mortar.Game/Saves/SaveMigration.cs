namespace Mortar.Game.Saves;

internal static class SaveMigration
{
    public static bool TryMigrate(WorldSnapshot snapshot, out string? error)
    {
        error = null;
        if (snapshot.Version == WorldSnapshot.CurrentVersion) return true;
        if (snapshot.Version != 1)
        {
            error = $"unsupported version {snapshot.Version}; this build reads version {WorldSnapshot.CurrentVersion}";
            return false;
        }
        MigrateV1ToV2(snapshot);
        return true;
    }

    private static void MigrateV1ToV2(WorldSnapshot snapshot)
    {
        snapshot.Version = 2;
        snapshot.NextProjectileId = 1;
        snapshot.Portal = null;
        if (snapshot.Player is { } player)
        {
            player.Spells = new List<KnownSpellSnapshot?>();
        }
        if (snapshot.Levels is not null)
        {
            foreach (var level in snapshot.Levels)
            {
                if (level is not null) level.Projectiles = new List<ProjectileSnapshot?>();
            }
        }
        MarkItemsIdentified(snapshot);
    }

    private static void MarkItemsIdentified(WorldSnapshot snapshot)
    {
        if (snapshot.Player is { } player)
        {
            foreach (var entry in player.Inventory ?? Enumerable.Empty<StoredItemSnapshot?>())
            {
                if (entry?.Item is not null) entry.Item.Identified = true;
            }
            foreach (var entry in player.Belt ?? Enumerable.Empty<BeltItemSnapshot?>())
            {
                if (entry?.Item is not null) entry.Item.Identified = true;
            }
            foreach (var entry in player.Equipment ?? Enumerable.Empty<EquippedItemSnapshot?>())
            {
                if (entry?.Item is not null) entry.Item.Identified = true;
            }
        }
        if (snapshot.Levels is null) return;
        foreach (var level in snapshot.Levels)
        {
            if (level?.GroundItems is null) continue;
            foreach (var ground in level.GroundItems)
            {
                if (ground?.Item is not null) ground.Item.Identified = true;
            }
        }
    }
}
