using Mortar.Core.Layouts;
using Mortar.Core.Model;

namespace Mortar.Core.Indoor;

internal sealed class StitchCanvas
{
    private readonly Tileset _tileset;
    private readonly bool[] _written;
    private readonly bool[] _forced;

    public StitchCanvas(int width, int height, Tileset tileset)
    {
        _tileset = tileset;
        Cells = new CellGrid(width, height, tileset.VoidIndex);
        _written = new bool[width * height];
        _forced = new bool[width * height];
    }

    public CellGrid Cells { get; }

    public void ForceFloor(int x, int y)
    {
        var index = y * Cells.Width + x;
        Cells.Ground[index] = _tileset.FloorIndex;
        _written[index] = true;
        _forced[index] = true;
    }

    public void Merge(int x, int y, int ground)
    {
        var index = y * Cells.Width + x;
        if (_forced[index]) return;
        if (!_written[index])
        {
            Cells.Ground[index] = ground;
            _written[index] = true;
            return;
        }
        if (_tileset.IsWalkable(Cells.Ground[index]) && !_tileset.IsWalkable(ground))
        {
            Cells.Ground[index] = ground;
        }
    }
}
