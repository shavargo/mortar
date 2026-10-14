using Mortar.Game.Content;

namespace Mortar.Game.Items;

public sealed class StoredItem
{
    internal StoredItem(Item item, Cell cell)
    {
        Item = item;
        Cell = cell;
    }

    public Item Item { get; }

    public Cell Cell { get; }
}

public sealed class Inventory
{
    public const int Width = 10;
    public const int Height = 4;

    private readonly int[] _cells = new int[Width * Height];
    private readonly List<StoredItem> _items = new List<StoredItem>();

    public IReadOnlyList<StoredItem> Items => _items;

    public int ItemAt(Cell cell) => InGrid(cell) ? _cells[Index(cell)] : 0;

    public StoredItem? Find(int itemId) => _items.Find(stored => stored.Item.Id == itemId);

    public bool Fits(ItemBaseDefinition definition, Cell topLeft, int ignoredItemId = 0)
    {
        for (var dy = 0; dy < definition.Height; dy++)
        {
            for (var dx = 0; dx < definition.Width; dx++)
            {
                var cell = new Cell(topLeft.X + dx, topLeft.Y + dy);
                if (!InGrid(cell)) return false;
                var occupant = _cells[Index(cell)];
                if (occupant != 0 && occupant != ignoredItemId) return false;
            }
        }
        return true;
    }

    public Cell? FindSpace(ItemBaseDefinition definition)
    {
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                var cell = new Cell(x, y);
                if (Fits(definition, cell)) return cell;
            }
        }
        return null;
    }

    internal bool TryPlace(Item item, Cell topLeft)
    {
        if (Find(item.Id) is not null || !Fits(item.Definition, topLeft)) return false;
        Mark(item.Definition, topLeft, item.Id);
        var index = _items.FindIndex(stored => stored.Item.Id > item.Id);
        _items.Insert(index < 0 ? _items.Count : index, new StoredItem(item, topLeft));
        return true;
    }

    internal bool TryAdd(Item item)
    {
        var space = FindSpace(item.Definition);
        return space is not null && TryPlace(item, space.Value);
    }

    internal bool Remove(int itemId)
    {
        var stored = Find(itemId);
        if (stored is null) return false;
        Mark(stored.Item.Definition, stored.Cell, 0);
        _items.Remove(stored);
        return true;
    }

    private void Mark(ItemBaseDefinition definition, Cell topLeft, int value)
    {
        for (var dy = 0; dy < definition.Height; dy++)
        {
            for (var dx = 0; dx < definition.Width; dx++)
            {
                _cells[Index(new Cell(topLeft.X + dx, topLeft.Y + dy))] = value;
            }
        }
    }

    private static bool InGrid(Cell cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

    private static int Index(Cell cell) => cell.Y * Width + cell.X;
}
