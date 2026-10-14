using Mortar.Game;
using Mortar.Game.Pathing;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class PathfinderTests
{
    private static bool Always(Cell cell) => true;

    [Fact]
    public void StraightLine_TakesTheDirectRoute()
    {
        var grid = new GridText("S....G");
        var path = Pathfinder.FindPath(grid.Find('S'), grid.Find('G'), grid.Walkable, grid.Walkable);
        Assert.NotNull(path);
        Assert.Equal(5, path!.Count);
        Assert.Equal(grid.Find('G'), path[^1]);
    }

    [Fact]
    public void Diagonal_CountsAsOneStep()
    {
        var grid = new GridText("S....", ".....", ".....", ".....", "....G");
        var path = Pathfinder.FindPath(grid.Find('S'), grid.Find('G'), grid.Walkable, grid.Walkable);
        Assert.Equal(4, path!.Count);
    }

    [Fact]
    public void WallCorners_AreNotCut()
    {
        var grid = new GridText("S#", ".G");
        var path = Pathfinder.FindPath(grid.Find('S'), grid.Find('G'), grid.Walkable, grid.Walkable);
        Assert.Equal(new[] { new Cell(0, 1), new Cell(1, 1) }, path);
    }

    [Fact]
    public void Unreachable_ReturnsNull()
    {
        var grid = new GridText("S#G");
        Assert.Null(Pathfinder.FindPath(grid.Find('S'), grid.Find('G'), grid.Walkable, grid.Walkable));
    }

    [Fact]
    public void OccupiedCells_AreAvoided()
    {
        var grid = new GridText("...", "S.G", "...");
        var blocked = new Cell(1, 1);
        var path = Pathfinder.FindPath(grid.Find('S'), grid.Find('G'), grid.Walkable, cell => grid.Walkable(cell) && cell != blocked);
        Assert.Equal(2, path!.Count);
        Assert.DoesNotContain(blocked, path);
    }

    [Fact]
    public void NextTo_StopsBesideTheTarget()
    {
        var grid = new GridText("S....T");
        var path = Pathfinder.FindPathNextTo(grid.Find('S'), grid.Find('T'), grid.Walkable, grid.Walkable);
        Assert.Equal(4, path!.Count);
        Assert.Equal(new Cell(4, 0), path[^1]);
    }

    [Fact]
    public void NextTo_AlreadyAdjacent_ReturnsEmpty()
    {
        var path = Pathfinder.FindPathNextTo(new Cell(0, 0), new Cell(1, 1), Always, Always);
        Assert.Empty(path!);
    }

    [Fact]
    public void NextTo_DoesNotStopDiagonallyPastAWallCorner()
    {
        var grid = new GridText("S.", "#T");
        var path = Pathfinder.FindPathNextTo(grid.Find('S'), grid.Find('T'), grid.Walkable, grid.Walkable);
        Assert.Equal(new[] { new Cell(1, 0) }, path);
    }

    [Theory]
    [InlineData(1, 0, true)]
    [InlineData(1, 1, false)]
    [InlineData(2, 0, false)]
    public void CanStrike_NeedsReachWithClearCorners(int toX, int toY, bool expected)
    {
        var grid = new GridText("S..", "#..");
        Assert.Equal(expected, Reach.CanStrike(grid.Find('S'), new Cell(toX, toY), grid.Walkable));
    }

    [Fact]
    public void SameInputs_GiveTheSamePath()
    {
        var first = Pathfinder.FindPath(new Cell(0, 0), new Cell(7, 3), Always, Always);
        var second = Pathfinder.FindPath(new Cell(0, 0), new Cell(7, 3), Always, Always);
        Assert.Equal(first, second);
    }

    [Fact]
    public void EndlessOpenArea_GivesUpAfterTheLimit()
    {
        static bool Quadrant(Cell cell) => cell.X >= 0 && cell.Y >= 0;
        Assert.Null(Pathfinder.FindPath(new Cell(0, 0), new Cell(-5, -5), Quadrant, Quadrant));
    }
}
