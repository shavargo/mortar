using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Core.Tiles;
using Mortar.Tests.Support;

namespace Mortar.Tests.Tiles;

public class TileResolverTests
{
    private static readonly Tileset Tiles = TestContent.CreateTileset();
    private static readonly int Floor = Tiles.FloorIndex;
    private static readonly int Wall = Tiles.WallIndex;
    private static readonly int Water = Tiles.IndexOf("water");

    [Fact]
    public void Name_UniformCorners_UsesGroundId() =>
        Assert.Equal("floor", TileResolver.Name(Tiles, new[] { Floor, Floor, Floor, Floor }));

    [Fact]
    public void Name_WaterOnNorthCorners_IsNorthShoreline() =>
        Assert.Equal("floor-water:1100", TileResolver.Name(Tiles, new[] { Water, Water, Floor, Floor }));

    [Fact]
    public void Name_ThreeGrounds_ListsAllCorners() =>
        Assert.Equal("floor|water|wall|wall", TileResolver.Name(Tiles, new[] { Floor, Water, Wall, Wall }));

    [Fact]
    public void TileTypeAt_TreatsOutsideCellsAsVoid()
    {
        var grid = TestContent.Cells(Tiles, ".");
        Assert.Equal("void-floor:1000", TileResolver.TileTypeAt(grid, Tiles, 0, 0));
    }

    [Fact]
    public void Resolve_BuildsPaletteInFirstAppearanceOrder()
    {
        var resolved = TileResolver.Resolve(TestContent.Cells(Tiles, ".."), Tiles, new Pcg32(1, 1));
        Assert.Equal(new[] { "void-floor:1100", "void-floor:1000" }, resolved.TileTypes);
        Assert.Equal(new[] { 0, 1 }, resolved.Tiles);
        Assert.Equal(new[] { 0, 0 }, resolved.Variants);
    }

    [Fact]
    public void Resolve_PicksVariantsFromWeights()
    {
        var weights = new Dictionary<string, IReadOnlyList<int>> { ["floor"] = new[] { 0, 1 } };
        var tileset = TestContent.CreateTileset(weights);
        var resolved = TileResolver.Resolve(TestContent.Cells(tileset, "...", "...", "..."), tileset, new Pcg32(1, 1));
        Assert.Equal("floor", resolved.TileTypes[resolved.Tiles[0]]);
        Assert.Equal(1, resolved.Variants[0]);
    }
}
