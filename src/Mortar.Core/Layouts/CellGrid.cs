namespace Mortar.Core.Layouts;

internal sealed class CellGrid
{
    public CellGrid(int width, int height, int fill)
    {
        Width = width;
        Height = height;
        Ground = new int[width * height];
        Array.Fill(Ground, fill);
    }

    public int Width { get; }

    public int Height { get; }

    public int[] Ground { get; }

    public int this[int x, int y]
    {
        get => Ground[y * Width + x];
        set => Ground[y * Width + x] = value;
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
}
