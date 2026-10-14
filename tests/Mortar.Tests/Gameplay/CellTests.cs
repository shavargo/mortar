using Mortar.Game;

namespace Mortar.Tests.Gameplay;

public class CellTests
{
    [Fact]
    public void DistanceTo_UsesChebyshevDistance()
    {
        Assert.Equal(3, new Cell(1, 1).DistanceTo(new Cell(4, 3)));
        Assert.True(new Cell(1, 1).IsAdjacentTo(new Cell(2, 2)));
        Assert.False(new Cell(1, 1).IsAdjacentTo(new Cell(1, 1)));
    }

    [Theory]
    [InlineData(5, 5, 5, 9, Direction.South)]
    [InlineData(5, 5, 1, 9, Direction.SouthWest)]
    [InlineData(5, 5, 1, 5, Direction.West)]
    [InlineData(5, 5, 9, 1, Direction.NorthEast)]
    [InlineData(5, 5, 5, 5, Direction.South)]
    public void Toward_UsesTheSignOfEachAxis(int fromX, int fromY, int toX, int toY, Direction expected)
    {
        Assert.Equal(expected, Directions.Toward(new Cell(fromX, fromY), new Cell(toX, toY)));
    }

    [Fact]
    public void Step_MovesOneCellInEachDirection()
    {
        var origin = new Cell(3, 3);
        foreach (var direction in Directions.All)
        {
            var next = origin.Step(direction);
            Assert.True(origin.IsAdjacentTo(next));
            Assert.Equal(direction, Directions.Toward(origin, next));
        }
    }
}
