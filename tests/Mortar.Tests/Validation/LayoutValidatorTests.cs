using Mortar.Core.Layouts;
using Mortar.Core.Model;
using Mortar.Core.Validation;
using Mortar.Tests.Support;

namespace Mortar.Tests.Validation;

public class LayoutValidatorTests
{
    private static readonly Tileset Tiles = TestContent.CreateTileset();
    private static readonly Marker Entrance = new Marker(MarkerKinds.Entrance, "entrance", 1, 1);
    private static readonly Marker Chest = new Marker(MarkerKinds.Chest, null, 3, 1);

    [Fact]
    public void Validate_EverythingReachable_Succeeds()
    {
        var grid = TestContent.Cells(Tiles, "#####", "#...#", "#####");
        Assert.Null(LayoutValidator.Validate(grid, Tiles, new[] { Entrance, Chest }, new TargetSize(3, 1, 0)));
    }

    [Fact]
    public void Validate_MarkerBehindWall_IsUnreachable()
    {
        var grid = TestContent.Cells(Tiles, "#####", "#.#.#", "#####");
        Assert.Equal("chest unreachable", LayoutValidator.Validate(grid, Tiles, new[] { Entrance, Chest }, new TargetSize(3, 1, 0)));
    }

    [Fact]
    public void Validate_UnreachedPocket_BecomesVoid()
    {
        var grid = TestContent.Cells(Tiles, "######", "#..#.#", "######");
        Assert.Null(LayoutValidator.Validate(grid, Tiles, new[] { Entrance }, new TargetSize(2, 1, 0)));
        Assert.Equal(Tiles.VoidIndex, grid[4, 1]);
        Assert.Equal(Tiles.FloorIndex, grid[2, 1]);
    }

    [Fact]
    public void Validate_BelowTolerance_IsTooSmall()
    {
        var grid = TestContent.Cells(Tiles, "#####", "#...#", "#####");
        Assert.Equal("too small", LayoutValidator.Validate(grid, Tiles, new[] { Entrance }, new TargetSize(10, 10, 10)));
    }

    [Fact]
    public void Validate_AboveTolerance_IsTooLarge()
    {
        var grid = TestContent.Cells(Tiles, "#####", "#...#", "#####");
        Assert.Equal("too large", LayoutValidator.Validate(grid, Tiles, new[] { Entrance }, new TargetSize(1, 1, 0)));
    }

    [Fact]
    public void Validate_NoEntranceMarker_Fails()
    {
        var grid = TestContent.Cells(Tiles, "#####", "#...#", "#####");
        Assert.Equal("entrance missing", LayoutValidator.Validate(grid, Tiles, new[] { Chest }, new TargetSize(3, 1, 0)));
    }
}
