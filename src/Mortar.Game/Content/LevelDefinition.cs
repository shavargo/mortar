namespace Mortar.Game.Content;

public sealed class SpawnEntry
{
    public SpawnEntry(string monsterId, int weight)
    {
        MonsterId = monsterId;
        Weight = weight;
    }

    public string MonsterId { get; }

    public int Weight { get; }
}

public sealed class LevelDefinition
{
    public LevelDefinition(int depth, string areaId, IReadOnlyList<SpawnEntry> spawns, string? bossId)
    {
        Depth = depth;
        AreaId = areaId;
        Spawns = spawns;
        BossId = bossId;
    }

    public int Depth { get; }

    public string AreaId { get; }

    public IReadOnlyList<SpawnEntry> Spawns { get; }

    public string? BossId { get; }
}
