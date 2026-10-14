using Mortar.Core.Layouts;

namespace Mortar.Core.Model;

public sealed class FixedMap
{
    internal FixedMap(CellGrid grid, IReadOnlyList<Marker> markers)
    {
        Grid = grid;
        Markers = markers;
    }

    public int Width => Grid.Width;

    public int Height => Grid.Height;

    public IReadOnlyList<Marker> Markers { get; }

    internal CellGrid Grid { get; }
}
