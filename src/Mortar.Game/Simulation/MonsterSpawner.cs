using Mortar.Core.Model;
using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

internal static class MonsterSpawner
{
    public const int EntranceSafeRadius = 5;
    public const int PlacementRadius = 3;

    public static void Populate(World world, LevelState level, LevelDefinition definition)
    {
        var weights = definition.Spawns.Select(spawn => spawn.Weight).ToList();
        var entrance = level.FindMarker(MarkerKinds.Entrance);
        foreach (var marker in level.Layout.Markers)
        {
            if (marker.Kind != MarkerKinds.Spawn || weights.Count == 0) continue;
            var origin = new Cell(marker.X, marker.Y);
            if (entrance is not null && origin.DistanceTo(entrance.Value) <= EntranceSafeRadius) continue;
            var spawn = definition.Spawns[world.Rng.PickWeighted(weights)];
            SpawnGroup(world, level, world.Content.Monster(spawn.MonsterId), origin);
        }
        var boss = level.FindMarker(MarkerKinds.Boss);
        if (definition.BossId is not null && boss is not null)
        {
            SpawnGroup(world, level, world.Content.Monster(definition.BossId), boss.Value);
        }
    }

    private static void SpawnGroup(World world, LevelState level, MonsterDefinition monster, Cell origin)
    {
        var cell = level.NearestFree(origin, PlacementRadius);
        if (cell is null) return;
        var leader = world.SpawnMonster(level, monster, cell.Value);
        if (monster.Minions is null) return;
        var minion = world.Content.Monster(monster.Minions.MonsterId);
        var count = world.Rng.NextInt(monster.Minions.Count.Min, monster.Minions.Count.Max + 1);
        for (var i = 0; i < count; i++)
        {
            var spot = level.NearestFree(cell.Value, PlacementRadius);
            if (spot is null) return;
            world.SpawnMonster(level, minion, spot.Value).LeaderId = leader.Id;
        }
    }
}
