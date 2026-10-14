namespace Mortar.Game.Pathing;

internal sealed class MinHeap
{
    private readonly List<Entry> _items = new List<Entry>();
    private int _sequence;

    public int Count => _items.Count;

    public void Push(Cell cell, int cost, int estimate)
    {
        _items.Add(new Entry(cell, cost + estimate, estimate, _sequence++));
        var index = _items.Count - 1;
        while (index > 0)
        {
            var parent = (index - 1) / 2;
            if (!Less(_items[index], _items[parent])) break;
            Swap(index, parent);
            index = parent;
        }
    }

    public Cell Pop()
    {
        var top = _items[0].Cell;
        var last = _items.Count - 1;
        _items[0] = _items[last];
        _items.RemoveAt(last);
        var index = 0;
        while (true)
        {
            var left = index * 2 + 1;
            if (left >= _items.Count) break;
            var right = left + 1;
            var child = right < _items.Count && Less(_items[right], _items[left]) ? right : left;
            if (!Less(_items[child], _items[index])) break;
            Swap(index, child);
            index = child;
        }
        return top;
    }

    private static bool Less(Entry a, Entry b)
    {
        if (a.Total != b.Total) return a.Total < b.Total;
        if (a.Estimate != b.Estimate) return a.Estimate < b.Estimate;
        return a.Sequence < b.Sequence;
    }

    private void Swap(int a, int b)
    {
        var held = _items[a];
        _items[a] = _items[b];
        _items[b] = held;
    }

    private readonly struct Entry
    {
        public Entry(Cell cell, int total, int estimate, int sequence)
        {
            Cell = cell;
            Total = total;
            Estimate = estimate;
            Sequence = sequence;
        }

        public Cell Cell { get; }

        public int Total { get; }

        public int Estimate { get; }

        public int Sequence { get; }
    }
}
