namespace Mortar.Game.Simulation;

public sealed class PortalPair
{
    public PortalPair(int dungeonDepth, Cell dungeonCell, Cell townCell)
    {
        DungeonDepth = dungeonDepth;
        DungeonCell = dungeonCell;
        TownCell = townCell;
    }

    public int DungeonDepth { get; }

    public Cell DungeonCell { get; }

    public Cell TownCell { get; }
}
