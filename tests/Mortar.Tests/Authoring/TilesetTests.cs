using Mortar.Core.Authoring;
using Mortar.Core.Model;

namespace Mortar.Tests.Authoring;

public class TilesetTests
{
    private static readonly GroundType Void = new GroundType("void", ' ', false, false);
    private static readonly GroundType Floor = new GroundType("floor", '.', true, false);
    private static readonly GroundType Wall = new GroundType("wall", '#', false, true);

    private static AuthoringResult<Tileset> Create(params GroundType[] grounds) =>
        Tileset.Create("tileset.json", grounds, new Dictionary<string, IReadOnlyList<int>>());

    private static void AssertError(AuthoringResult<Tileset> result, string expected)
    {
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains(expected) && e.Source == "tileset.json");
    }

    [Fact]
    public void Create_ValidTileset_IndexesGrounds()
    {
        var tileset = Create(Void, Floor, Wall).Value!;
        Assert.Equal(0, tileset.VoidIndex);
        Assert.Equal(1, tileset.FloorIndex);
        Assert.Equal(2, tileset.WallIndex);
        Assert.Equal(tileset.FloorIndex, tileset.IndexOfSymbol('.'));
        Assert.Equal(-1, tileset.IndexOf("lava"));
        Assert.Equal(new[] { 1 }, tileset.WeightsFor("floor"));
    }

    [Fact]
    public void Create_MissingFloor_IsAnError() =>
        AssertError(Create(Void, Wall), "missing required ground type 'floor'");

    [Fact]
    public void Create_DuplicateChar_IsAnError() =>
        AssertError(Create(Void, Floor, Wall, new GroundType("sand", '.', true, false)), "duplicate ground char '.'");

    [Fact]
    public void Create_ReservedChar_IsAnError() =>
        AssertError(Create(Void, Floor, Wall, new GroundType("door", 'D', true, false)), "uses reserved char 'D'");

    [Fact]
    public void Create_WalkableWall_IsAnError() =>
        AssertError(Create(Void, Floor, new GroundType("wall", '#', true, true)), "'wall' must not be walkable");

    [Fact]
    public void Create_AllZeroWeights_IsAnError()
    {
        var weights = new Dictionary<string, IReadOnlyList<int>> { ["floor"] = new[] { 0, 0 } };
        AssertError(Tileset.Create("tileset.json", new[] { Void, Floor, Wall }, weights), "variant weights for 'floor'");
    }

    [Fact]
    public void Create_WeightsOverflowingInt_IsAnError()
    {
        var weights = new Dictionary<string, IReadOnlyList<int>> { ["floor"] = new[] { int.MaxValue, 1 } };
        AssertError(Tileset.Create("tileset.json", new[] { Void, Floor, Wall }, weights), "variant weights for 'floor'");
    }
}
