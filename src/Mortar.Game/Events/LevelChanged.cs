namespace Mortar.Game.Events;

public sealed class LevelChanged : GameEvent
{
    public LevelChanged(long tick, int fromDepth, int toDepth, Cell cell)
        : base(tick)
    {
        FromDepth = fromDepth;
        ToDepth = toDepth;
        Cell = cell;
    }

    public int FromDepth { get; }

    public int ToDepth { get; }

    public Cell Cell { get; }

    protected override string Describe() => $"level {FromDepth} -> {ToDepth} at {Cell}";
}
