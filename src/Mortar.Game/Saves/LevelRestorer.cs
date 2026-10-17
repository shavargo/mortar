using Mortar.Game.Content;
using Mortar.Game.Items;
using Mortar.Game.Simulation;

namespace Mortar.Game.Saves;

internal static class LevelRestorer
{
    public static void Restore(World world, LevelSnapshot snapshot, RestoredIds ids, bool fromV1)
    {
        var depth = snapshot.Depth;
        Check.That(depth >= world.Content.ShallowestLevel && depth <= world.Content.DeepestLevel, $"level depth {depth} is not in the content");
        Check.That(world.FindLevel(depth) is null, $"level {depth} appears twice");
        var result = world.Generator.Generate(world.Content.Level(depth), snapshot.Seed);
        Check.That(result.Succeeded, $"level {depth} could not be regenerated: {result.Error}");
        var layout = result.Layout!;
        Check.That(
            layout.Fingerprint == snapshot.Fingerprint,
            $"level {depth} does not match: the save has fingerprint '{snapshot.Fingerprint}', the regenerated level has '{layout.Fingerprint}'");
        var level = new LevelState(depth, snapshot.Seed, layout);
        world.AddLevel(level);
        foreach (var monster in Check.Present(snapshot.Monsters, $"level {depth} monsters"))
        {
            ActorRestorer.RestoreMonster(world, level, Check.Present(monster, $"level {depth} monster"), ids);
        }
        foreach (var ground in Check.Present(snapshot.GroundItems, $"level {depth} groundItems"))
        {
            level.AddGroundItem(RestoreGround(world, level, Check.Present(ground, $"level {depth} ground item"), ids, fromV1));
        }
        foreach (var projectile in snapshot.Projectiles ?? Enumerable.Empty<ProjectileSnapshot?>())
        {
            level.AddProjectile(RestoreProjectile(level, Check.Present(projectile, $"level {depth} projectile"), ids));
        }
    }

    private static GroundItem RestoreGround(World world, LevelState level, GroundItemSnapshot snapshot, RestoredIds ids, bool fromV1)
    {
        var cell = new Cell(snapshot.X, snapshot.Y);
        var where = $"level {level.Depth} ground item {snapshot.Id}";
        Check.That(level.IsWalkable(cell), $"{where} lies on a wall or outside the level");
        if (snapshot.Item is null)
        {
            ids.ClaimItem(snapshot.Id);
            return new GroundItem(snapshot.Id, cell, null, Check.Range(snapshot.Gold, 1, int.MaxValue, $"{where} gold"));
        }
        Check.That(snapshot.Gold == 0, $"{where} has both gold and an item");
        var item = ItemRestorer.Restore(world.Content, snapshot.Item, ids, where, fromV1);
        Check.That(item.Id == snapshot.Id, $"{where} must have the same id as its item");
        return new GroundItem(item.Id, cell, item, 0);
    }

    private static Projectile RestoreProjectile(LevelState level, ProjectileSnapshot snapshot, RestoredIds ids)
    {
        var where = $"level {level.Depth} projectile {snapshot.Id}";
        ids.ClaimProjectile(snapshot.Id);
        var kind = Check.Text(snapshot.Kind, $"{where} kind");
        Check.That(level.InBounds(new Cell(snapshot.X, snapshot.Y)), $"{where} is out of bounds");
        Check.Range(snapshot.RemainingRange, 0, DefinitionChecks.MaxValue, $"{where} remainingRange");
        Check.Range(snapshot.Radius, 0, DefinitionChecks.MaxValue, $"{where} radius");
        return new Projectile(
            snapshot.Id,
            snapshot.OwnerId,
            kind,
            new Cell(snapshot.X, snapshot.Y),
            new Cell(snapshot.AimX, snapshot.AimY),
            snapshot.RemainingRange,
            snapshot.DamageMin,
            snapshot.DamageMax,
            snapshot.HitsMonsters,
            snapshot.HitsPlayer,
            snapshot.Radius);
    }
}
