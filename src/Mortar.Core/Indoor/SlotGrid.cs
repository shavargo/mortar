using Mortar.Core.Model;

namespace Mortar.Core.Indoor;

internal sealed class SlotGrid
{
    private readonly int[] _blockOf;
    private readonly bool[] _used;
    private readonly bool[] _openEast;
    private readonly bool[] _openSouth;
    private readonly List<Placement> _placements = new List<Placement>();

    public SlotGrid(int columns, int rows, int stride)
    {
        Columns = columns;
        Rows = rows;
        Stride = stride;
        _blockOf = new int[columns * rows];
        Array.Fill(_blockOf, -1);
        _used = new bool[columns * rows];
        _openEast = new bool[columns * rows];
        _openSouth = new bool[columns * rows];
    }

    public static SlotGrid ForArea(AreaDefinition area)
    {
        var stride = area.SlotSize - 1;
        var columns = Math.Max(1, (area.TargetSize.Width - 1) / stride);
        var rows = Math.Max(1, (area.TargetSize.Height - 1) / stride);
        return new SlotGrid(columns, rows, stride);
    }

    public int Columns { get; }

    public int Rows { get; }

    public int Stride { get; }

    public int CellWidth => Columns * Stride + 1;

    public int CellHeight => Rows * Stride + 1;

    public IReadOnlyList<Placement> Placements => _placements;

    public int Index(int x, int y) => y * Columns + x;

    public (int X, int Y) Coords(int index) => (index % Columns, index / Columns);

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Columns && y < Rows;

    public int BlockAt(int x, int y) => _blockOf[Index(x, y)];

    public bool IsUsed(int x, int y) => _used[Index(x, y)];

    public void MarkUsed(int x, int y) => _used[Index(x, y)] = true;

    public int BlockOfNode(string nodeId) => _placements.FindIndex(p => p.NodeId == nodeId);

    public bool CanPlace(Room room, int originX, int originY)
    {
        for (var y = originY; y < originY + room.SlotsHigh; y++)
        {
            for (var x = originX; x < originX + room.SlotsWide; x++)
            {
                if (!InBounds(x, y) || BlockAt(x, y) != -1) return false;
            }
        }
        return true;
    }

    public void Place(Placement placement)
    {
        var block = _placements.Count;
        _placements.Add(placement);
        for (var y = placement.SlotY; y < placement.SlotY + placement.Room.SlotsHigh; y++)
        {
            for (var x = placement.SlotX; x < placement.SlotX + placement.Room.SlotsWide; x++)
            {
                _blockOf[Index(x, y)] = block;
                _used[Index(x, y)] = true;
            }
        }
    }

    public void Open(int x, int y, Side side)
    {
        switch (side)
        {
            case Side.East: _openEast[Index(x, y)] = true; break;
            case Side.West: _openEast[Index(x - 1, y)] = true; break;
            case Side.South: _openSouth[Index(x, y)] = true; break;
            default: _openSouth[Index(x, y - 1)] = true; break;
        }
    }

    public bool IsOpen(int x, int y, Side side)
    {
        var (dx, dy) = Sides.Offset(side);
        if (!InBounds(x, y) || !InBounds(x + dx, y + dy)) return false;
        return side switch
        {
            Side.East => _openEast[Index(x, y)],
            Side.West => _openEast[Index(x - 1, y)],
            Side.South => _openSouth[Index(x, y)],
            _ => _openSouth[Index(x, y - 1)],
        };
    }

    public void ConnectPath(IReadOnlyList<int> slots)
    {
        for (var i = 0; i < slots.Count; i++)
        {
            var (x, y) = Coords(slots[i]);
            MarkUsed(x, y);
            if (i == 0) continue;
            var (px, py) = Coords(slots[i - 1]);
            var block = BlockAt(x, y);
            if (block != -1 && block == BlockAt(px, py)) continue;
            Open(px, py, Sides.Toward(x - px, y - py));
        }
    }
}
