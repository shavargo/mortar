using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public sealed class PlayerProgress
{
    public PlayerProgress(string classId, int level, long experience, Stats baseStats, int unspentPoints)
    {
        ClassId = classId;
        Level = level;
        Experience = experience;
        BaseStats = baseStats;
        UnspentPoints = unspentPoints;
    }

    public string ClassId { get; }

    public int Level { get; internal set; }

    public long Experience { get; internal set; }

    public Stats BaseStats { get; internal set; }

    public int UnspentPoints { get; internal set; }

    public static PlayerProgress Start(ClassDefinition definition) =>
        new PlayerProgress(definition.Id, 1, 0, definition.BaseStats, 0);
}
