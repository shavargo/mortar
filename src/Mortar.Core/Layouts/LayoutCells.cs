namespace Mortar.Core.Layouts;

public sealed class LayoutCells
{
    private readonly int[] _ground;
    private readonly int[] _tiles;
    private readonly int[] _variants;
    private readonly bool[] _walkable;

    public LayoutCells(
        int width,
        int height,
        IReadOnlyList<string> groundTypes,
        int[] ground,
        IReadOnlyList<string> tileTypes,
        int[] tiles,
        int[] variants,
        bool[] walkable)
    {
        Width = width;
        Height = height;
        GroundTypes = groundTypes;
        TileTypes = tileTypes;
        _ground = ground;
        _tiles = tiles;
        _variants = variants;
        _walkable = walkable;
    }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<string> GroundTypes { get; }

    public IReadOnlyList<string> TileTypes { get; }

    public IReadOnlyList<int> Ground => _ground;

    public IReadOnlyList<int> Tiles => _tiles;

    public IReadOnlyList<int> Variants => _variants;

    public IReadOnlyList<bool> Walkable => _walkable;

    public string GroundAt(int x, int y) => GroundTypes[_ground[Index(x, y)]];

    public string TileAt(int x, int y) => TileTypes[_tiles[Index(x, y)]];

    public int VariantAt(int x, int y) => _variants[Index(x, y)];

    public bool IsWalkable(int x, int y) => _walkable[Index(x, y)];

    private int Index(int x, int y) => y * Width + x;
}
