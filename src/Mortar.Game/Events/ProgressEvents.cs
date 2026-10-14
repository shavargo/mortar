using Mortar.Game.Content;

namespace Mortar.Game.Events;

public sealed class ExperienceGained : GameEvent
{
    public ExperienceGained(long tick, long amount, long total)
        : base(tick)
    {
        Amount = amount;
        Total = total;
    }

    public long Amount { get; }

    public long Total { get; }

    protected override string Describe() => $"gained {Amount} xp (total {Total})";
}

public sealed class LeveledUp : GameEvent
{
    public LeveledUp(long tick, int level)
        : base(tick)
    {
        Level = level;
    }

    public int Level { get; }

    protected override string Describe() => $"reached level {Level}";
}

public sealed class StatIncreased : GameEvent
{
    public StatIncreased(long tick, StatKind stat, int value)
        : base(tick)
    {
        Stat = stat;
        Value = value;
    }

    public StatKind Stat { get; }

    public int Value { get; }

    protected override string Describe() => $"{Stat.ToString().ToLowerInvariant()} is now {Value}";
}
