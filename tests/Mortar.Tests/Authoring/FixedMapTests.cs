using Mortar.Core.Authoring;
using Mortar.Core.Generation;
using Mortar.Core.Layouts;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Authoring;

public class FixedMapTests
{
    private static readonly string[] Square =
    {
        "#######",
        "#E...X#",
        "#.~~..#",
        "#######",
    };

    private static AuthoringResult<CompiledArea> Compile(params string[] grid) =>
        CompileWith(TestContent.CreateTileset(), grid);

    private static AuthoringResult<CompiledArea> CompileWith(Tileset tileset, params string[] grid) =>
        AreaCompiler.CompileFixed(
            AreaDefinition.Fixed("town.json", "town"),
            tileset,
            new RoomDefinition("map.json", "", Array.Empty<string>(), grid));

    private static void AssertError(AuthoringResult<CompiledArea> result, string expected)
    {
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Source == "map.json" && e.Message.Contains(expected));
    }

    [Fact]
    public void Compile_RecordsSizeAndMarkersInRowOrder()
    {
        var area = Compile(Square).Value!;
        var map = area.Map!;
        Assert.Equal(AreaMode.Fixed, area.Definition.Mode);
        Assert.Equal((7, 4), (map.Width, map.Height));
        Assert.Equal(
            new[] { (MarkerKinds.Entrance, 1, 1), (MarkerKinds.Exit, 5, 1) },
            map.Markers.Select(m => (m.Kind, m.X, m.Y)));
    }

    [Fact]
    public void Generate_StampsTheMapAsDrawn()
    {
        var area = Compile(Square).Value!;
        var result = Generator.Generate(area, 5);
        Assert.True(result.Succeeded);
        var layout = result.Layout!;
        Assert.Equal(0, layout.Attempt);
        Assert.Equal(1, result.Attempts);
        Assert.Equal("water", layout.Cells.GroundAt(2, 2));
        Assert.Equal("floor", layout.Cells.GroundAt(1, 1));
        Assert.Equal("wall", layout.Cells.GroundAt(0, 0));
        Assert.Equal(area.Map!.Markers, layout.Markers);
    }

    [Fact]
    public void Generate_SameSeedGivesTheSameFingerprint()
    {
        var area = Compile(Square).Value!;
        Assert.Equal(Generator.Generate(area, 9).Layout!.Fingerprint, Generator.Generate(area, 9).Layout!.Fingerprint);
    }

    [Fact]
    public void Compile_VoidsUnreachablePockets()
    {
        var area = Compile("#####", "#E#.#", "#####").Value!;
        Assert.Equal("void", Generator.Generate(area, 1).Layout!.Cells.GroundAt(3, 1));
    }

    [Fact]
    public void Compile_CustomMarker_IsRecorded()
    {
        var tileset = TestContent.CreateTileset(markers: new[] { new MarkerType('H', "healer") });
        var area = CompileWith(tileset, "E.H").Value!;
        Assert.Contains(area.Map!.Markers, m => m.Kind == "healer" && m.X == 2 && m.Y == 0);
    }

    [Fact]
    public void Compile_EmptyGrid_IsAnError() => AssertError(Compile(), "map has an empty grid");

    [Fact]
    public void Compile_RaggedRows_IsAnError() => AssertError(Compile("###", "#E"), "map row 1 has length 2, expected 3");

    [Fact]
    public void Compile_UnknownChar_IsAnError() => AssertError(Compile("#E?#"), "unknown char '?' at (2, 0)");

    [Fact]
    public void Compile_Door_IsAnError() => AssertError(Compile("#ED#"), "door at (2, 0)");

    [Fact]
    public void Compile_NoEntrance_IsAnError() => AssertError(Compile("#..#"), "exactly one entrance (E), found 0");

    [Fact]
    public void Compile_TwoEntrances_IsAnError() => AssertError(Compile("#EE#"), "exactly one entrance (E), found 2");

    [Fact]
    public void Compile_UnreachableMarker_IsAnError() =>
        AssertError(Compile("#E#X#"), "marker 'exit' at (3, 0) cannot be reached from the entrance");

    [Fact]
    public void Compile_TooWide_IsAnError() =>
        AssertError(Compile("E" + new string('.', Layout.MaxDimension)), "width and height must be at most 4096");

    [Fact]
    public void CompileFixed_IndoorDefinition_IsAnError()
    {
        var result = AreaCompiler.CompileFixed(
            TestContent.CryptDefinition(), TestContent.CreateTileset(), new RoomDefinition("map.json", "", Array.Empty<string>(), Square));
        Assert.Contains(result.Errors, e => e.Message == "only fixed areas can be compiled from a map");
    }

    [Fact]
    public void Compile_FixedDefinitionWithRooms_IsAnError()
    {
        var result = AreaCompiler.Compile(AreaDefinition.Fixed("town.json", "town"), TestContent.CreateTileset(), TestContent.CryptRooms());
        Assert.Contains(result.Errors, e => e.Message.Contains("fixed areas are built from a map"));
    }
}
