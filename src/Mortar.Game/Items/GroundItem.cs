namespace Mortar.Game.Items;

public sealed class GroundItem
{
    internal GroundItem(int id, Cell cell, Item? item, int gold)
    {
        Id = id;
        Cell = cell;
        Item = item;
        Gold = gold;
    }

    public int Id { get; }

    public Cell Cell { get; }

    public Item? Item { get; }

    public int Gold { get; }

    public bool IsGold => Item is null;

    public string Describe() => Item?.Name ?? $"{Gold} gold";
}
