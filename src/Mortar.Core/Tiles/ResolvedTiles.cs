namespace Mortar.Core.Tiles;

internal sealed class ResolvedTiles
{
    public ResolvedTiles(IReadOnlyList<string> tileTypes, int[] tiles, int[] variants)
    {
        TileTypes = tileTypes;
        Tiles = tiles;
        Variants = variants;
    }

    public IReadOnlyList<string> TileTypes { get; }

    public int[] Tiles { get; }

    public int[] Variants { get; }
}
