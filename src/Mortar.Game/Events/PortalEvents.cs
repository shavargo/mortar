namespace Mortar.Game.Events;

public sealed class PortalOpened : GameEvent
{
    public PortalOpened(long tick, int dungeonDepth, Cell dungeonCell, Cell townCell)
        : base(tick)
    {
        DungeonDepth = dungeonDepth;
        DungeonCell = dungeonCell;
        TownCell = townCell;
    }

    public int DungeonDepth { get; }

    public Cell DungeonCell { get; }

    public Cell TownCell { get; }

    protected override string Describe() =>
        $"portal opened dungeon {DungeonDepth}{DungeonCell} <-> town {TownCell}";
}

public sealed class PortalUsed : GameEvent
{
    public PortalUsed(long tick, int fromDepth, int toDepth, Cell cell)
        : base(tick)
    {
        FromDepth = fromDepth;
        ToDepth = toDepth;
        Cell = cell;
    }

    public int FromDepth { get; }

    public int ToDepth { get; }

    public Cell Cell { get; }

    protected override string Describe() => $"used portal {FromDepth} -> {ToDepth} at {Cell}";
}
