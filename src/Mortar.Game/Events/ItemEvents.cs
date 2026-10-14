namespace Mortar.Game.Events;

public sealed class ItemDropped : GameEvent
{
    public ItemDropped(long tick, int groundItemId, Cell cell, string description)
        : base(tick)
    {
        GroundItemId = groundItemId;
        Cell = cell;
        Description = description;
    }

    public int GroundItemId { get; }

    public Cell Cell { get; }

    public string Description { get; }

    protected override string Describe() => $"item {GroundItemId} '{Description}' dropped at {Cell}";
}
