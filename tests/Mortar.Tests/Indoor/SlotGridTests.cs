using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class SlotGridTests
{
    private static readonly Room Cross = TestContent.CompileRoom("cross", new[] { "filler" }, TestContent.Box(9, 9, new[] { 4 }));
    private static readonly Room Boss = TestContent.CompileRoom("boss", new[] { "boss" }, TestContent.Box(17, 17, new[] { 4, 12 }, (8, 8, 'B')));

    [Fact]
    public void ForArea_DerivesColumnsFromTargetSize()
    {
        var grid = SlotGrid.ForArea(TestContent.CryptDefinition());
        Assert.Equal((7, 7, 8), (grid.Columns, grid.Rows, grid.Stride));
        Assert.Equal(57, grid.CellWidth);
    }

    [Fact]
    public void Open_IsVisibleFromBothSides()
    {
        var grid = new SlotGrid(3, 3, 8);
        grid.Open(1, 1, Side.East);
        Assert.True(grid.IsOpen(2, 1, Side.West));
        Assert.True(grid.IsOpen(1, 1, Side.East));
        Assert.False(grid.IsOpen(1, 1, Side.South));
    }

    [Fact]
    public void IsOpen_IsFalseAtGridEdge() => Assert.False(new SlotGrid(3, 3, 8).IsOpen(0, 0, Side.West));

    [Fact]
    public void CanPlace_RejectsOverlapAndOutOfBounds()
    {
        var grid = new SlotGrid(3, 3, 8);
        Assert.False(grid.CanPlace(Boss, 2, 0));
        grid.Place(new Placement(Boss, 0, 0, "boss"));
        Assert.False(grid.CanPlace(Boss, 1, 1));
        Assert.True(grid.CanPlace(Cross, 2, 2));
        Assert.Equal(0, grid.BlockAt(1, 1));
        Assert.Equal(0, grid.BlockOfNode("boss"));
    }

    [Fact]
    public void ConnectPath_OpensBetweenSlotsButNotInsideBlocks()
    {
        var grid = new SlotGrid(4, 2, 8);
        grid.Place(new Placement(Boss, 0, 0, "boss"));
        grid.ConnectPath(new[] { grid.Index(0, 0), grid.Index(1, 0), grid.Index(2, 0), grid.Index(3, 0) });
        Assert.False(grid.IsOpen(0, 0, Side.East));
        Assert.True(grid.IsOpen(1, 0, Side.East));
        Assert.True(grid.IsOpen(2, 0, Side.East));
        Assert.True(grid.IsUsed(3, 0));
    }
}
