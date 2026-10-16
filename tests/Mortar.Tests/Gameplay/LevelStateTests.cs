using Mortar.Core.Layouts;
using Mortar.Core.Model;
using Mortar.Game;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class LevelStateTests
{
    private static LevelState Level() =>
        new LevelState(1, 0, TestLevels.FromAscii("#####", "#...#", "#...#", "#####"));

    private static Actor Monster(int id) =>
        new Actor(id, ActorKind.Monster, "zombie", CombatProfile.ForMonster(TestGameContent.Monster("zombie"), 10));

    [Fact]
    public void IsWalkable_IsFalseForWallsAndOutsideTheMap()
    {
        var level = Level();
        Assert.False(level.IsWalkable(new Cell(-1, 0)));
        Assert.False(level.IsWalkable(new Cell(5, 1)));
        Assert.False(level.IsWalkable(new Cell(0, 0)));
        Assert.True(level.IsWalkable(new Cell(1, 1)));
    }

    [Fact]
    public void Add_KeepsActorsInIdOrderAndMarksOccupancy()
    {
        var level = Level();
        level.Add(Monster(3), new Cell(1, 1));
        level.Add(Monster(2), new Cell(2, 1));
        Assert.Equal(new[] { 2, 3 }, level.Actors.Select(a => a.Id));
        Assert.Equal(3, level.OccupantAt(new Cell(1, 1)));
        Assert.False(level.IsFree(new Cell(1, 1)));
        Assert.Equal(new Cell(2, 1), level.Find(2)!.Cell);
    }

    [Fact]
    public void Move_UpdatesOccupancy()
    {
        var level = Level();
        var monster = Monster(2);
        level.Add(monster, new Cell(1, 1));
        level.Move(monster, new Cell(2, 2));
        Assert.True(level.IsFree(new Cell(1, 1)));
        Assert.Equal(2, level.OccupantAt(new Cell(2, 2)));
        Assert.Equal(new Cell(2, 2), monster.Cell);
    }

    [Fact]
    public void NearestFree_PrefersTheOriginThenTheClosestRing()
    {
        var level = Level();
        Assert.Equal(new Cell(1, 1), level.NearestFree(new Cell(1, 1), 2));
        level.Add(Monster(2), new Cell(1, 1));
        Assert.Equal(new Cell(2, 1), level.NearestFree(new Cell(1, 1), 2));
    }

    [Fact]
    public void TownNpcMarkersAreNotFree()
    {
        var baseLayout = TestLevels.FromAscii("#####", "#E.X#", "#####");
        var markers = baseLayout.Markers.Append(new Marker(MarkerKinds.Smith, null, 2, 1)).ToList();
        var layout = new Layout(
            baseLayout.AreaId, baseLayout.Seed, baseLayout.Attempt, baseLayout.Cells, markers,
            baseLayout.MortarVersion, baseLayout.FormatVersion);
        var level = new LevelState(0, 0, layout);

        Assert.True(level.IsWalkable(new Cell(2, 1)));
        Assert.True(level.HasTownNpc(new Cell(2, 1)));
        Assert.False(level.IsFree(new Cell(2, 1)));
    }
}
