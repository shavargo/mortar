namespace Mortar.Core.Indoor;

internal sealed class SlotSearch
{
    private readonly int[] _cost;
    private readonly int[] _previous;
    private readonly SortedSet<(int Cost, int Slot)> _open = new SortedSet<(int Cost, int Slot)>();

    public SlotSearch(int slotCount)
    {
        _cost = new int[slotCount];
        Array.Fill(_cost, int.MaxValue);
        _previous = new int[slotCount];
        Array.Fill(_previous, -1);
    }

    public int CostOf(int slot) => _cost[slot];

    public void Push(int slot, int cost, int previous)
    {
        if (cost >= _cost[slot]) return;
        if (_cost[slot] != int.MaxValue) _open.Remove((_cost[slot], slot));
        _cost[slot] = cost;
        _previous[slot] = previous;
        _open.Add((cost, slot));
    }

    public bool TryPop(out int slot)
    {
        if (_open.Count == 0)
        {
            slot = -1;
            return false;
        }
        var next = _open.Min;
        _open.Remove(next);
        slot = next.Slot;
        return true;
    }

    public List<int> PathTo(int slot)
    {
        var path = new List<int>();
        for (var current = slot; current != -1; current = _previous[current])
        {
            path.Add(current);
        }
        path.Reverse();
        return path;
    }
}
