using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Indoor;

internal sealed class SlotPathfinder
{
    private const int MaxRandomWeight = 4;
    private readonly SlotGrid _grid;
    private readonly int[] _weights;

    public SlotPathfinder(SlotGrid grid, Pcg32 rng)
    {
        _grid = grid;
        _weights = new int[grid.Columns * grid.Rows];
        for (var i = 0; i < _weights.Length; i++)
        {
            _weights[i] = rng.NextInt(MaxRandomWeight);
        }
    }

    public IReadOnlyList<int>? FindPath(int fromBlock, int toBlock)
    {
        var search = new SlotSearch(_weights.Length);
        foreach (var slot in SlotsOf(fromBlock))
        {
            search.Push(slot, 0, -1);
        }
        while (search.TryPop(out var slot))
        {
            var (x, y) = _grid.Coords(slot);
            if (_grid.BlockAt(x, y) == toBlock)
            {
                return search.PathTo(slot);
            }
            foreach (var side in Sides.All)
            {
                Relax(search, slot, side, fromBlock, toBlock);
            }
        }
        return null;
    }

    private void Relax(SlotSearch search, int slot, Side side, int fromBlock, int toBlock)
    {
        var (x, y) = _grid.Coords(slot);
        var (dx, dy) = Sides.Offset(side);
        var nx = x + dx;
        var ny = y + dy;
        if (!_grid.InBounds(nx, ny) || !CanCross(x, y, nx, ny, side, fromBlock, toBlock))
        {
            return;
        }
        var next = _grid.Index(nx, ny);
        var step = _grid.IsUsed(nx, ny) ? 1 : 1 + _weights[next];
        search.Push(next, search.CostOf(slot) + step, slot);
    }

    private bool CanCross(int x, int y, int nx, int ny, Side side, int fromBlock, int toBlock)
    {
        var from = _grid.BlockAt(x, y);
        var to = _grid.BlockAt(nx, ny);
        if (from == to) return true;
        if (to != -1 && to != fromBlock && to != toBlock) return false;
        if (from != -1 && !HasDoorToward(from, x, y, side)) return false;
        return to == -1 || HasDoorToward(to, nx, ny, Sides.Opposite(side));
    }

    private bool HasDoorToward(int block, int x, int y, Side side)
    {
        var placement = _grid.Placements[block];
        var segment = side == Side.North || side == Side.South ? x - placement.SlotX : y - placement.SlotY;
        return placement.Room.HasDoorInSegment(side, segment);
    }

    private IEnumerable<int> SlotsOf(int block)
    {
        var placement = _grid.Placements[block];
        for (var y = placement.SlotY; y < placement.SlotY + placement.Room.SlotsHigh; y++)
        {
            for (var x = placement.SlotX; x < placement.SlotX + placement.Room.SlotsWide; x++)
            {
                yield return _grid.Index(x, y);
            }
        }
    }
}
