using Mortar.Game;

namespace Mortar.Tests.Gameplay.Support;

internal sealed class GridText
{
    private readonly string[] _rows;

    public GridText(params string[] rows)
    {
        _rows = rows;
    }

    public bool Walkable(Cell cell) =>
        cell.Y >= 0 && cell.Y < _rows.Length && cell.X >= 0 && cell.X < _rows[cell.Y].Length && _rows[cell.Y][cell.X] != '#';

    public Cell Find(char symbol)
    {
        for (var y = 0; y < _rows.Length; y++)
        {
            var x = _rows[y].IndexOf(symbol);
            if (x >= 0) return new Cell(x, y);
        }
        throw new InvalidOperationException($"'{symbol}' not found");
    }
}
