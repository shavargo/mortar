using Mortar.Core.Model;
using Mortar.Json;
using Mortar.Tests.Support;

namespace Mortar.Tests.Json;

public class FixedAreaLoaderTests
{
    private const string Map = """{ "grid": ["#####", "#E.X#", "#####"] }""";

    private static string AreaJson(string extra = "", string map = "\"map\": \"map.json\",") => $$"""
        {
          "id": "town",
          "mode": "fixed",{{map}}{{extra}}
          "tileset": "{{SampleArea.Tileset}}"
        }
        """;

    private static string WriteArea(TempDir temp, string json)
    {
        temp.Write("map.json", Map);
        return temp.Write("area.json", json);
    }

    [Fact]
    public void LoadArea_FixedArea_Succeeds()
    {
        using var temp = new TempDir();
        var result = MortarLoader.LoadArea(WriteArea(temp, AreaJson()));
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        Assert.Equal(AreaMode.Fixed, result.Value!.Definition.Mode);
        Assert.Equal("town", result.Value.Definition.Id);
        Assert.Equal(5, result.Value.Map!.Width);
    }

    [Theory]
    [InlineData("\n  \"graph\": { \"nodes\": [] },", "graph")]
    [InlineData("\n  \"slotSize\": 9,", "slotSize")]
    [InlineData("\n  \"rooms\": [],", "rooms")]
    [InlineData("\n  \"roomTags\": [],", "roomTags")]
    [InlineData("\n  \"maxSideBranches\": 0,", "maxSideBranches")]
    [InlineData("\n  \"targetSize\": { \"width\": 5, \"height\": 3, \"tolerance\": 0 },", "targetSize")]
    [InlineData("\n  \"maxAttempts\": 1,", "maxAttempts")]
    public void LoadArea_FixedAreaWithIndoorField_IsAnError(string extra, string field)
    {
        using var temp = new TempDir();
        var path = WriteArea(temp, AreaJson(extra));
        Assert.Contains(MortarLoader.LoadArea(path).Errors, e => e.Source == path && e.Message == $"'{field}' is not allowed in a fixed area");
    }

    [Fact]
    public void LoadArea_FixedAreaWithoutMap_IsAnError()
    {
        using var temp = new TempDir();
        var path = WriteArea(temp, AreaJson(map: ""));
        Assert.Contains(MortarLoader.LoadArea(path).Errors, e => e.Source == path && e.Message == "fixed area has no map");
    }

    [Fact]
    public void LoadArea_MissingMapFile_IsAnError()
    {
        using var temp = new TempDir();
        var path = temp.Write("area.json", AreaJson());
        Assert.Contains(MortarLoader.LoadArea(path).Errors, e => e.Source == path && e.Message == "map 'map.json' does not exist");
    }

    [Fact]
    public void LoadArea_BadMap_NamesTheMapFile()
    {
        using var temp = new TempDir();
        var map = temp.Write("map.json", """{ "grid": ["#E?#"] }""");
        var area = temp.Write("area.json", AreaJson());
        Assert.Contains(MortarLoader.LoadArea(area).Errors, e => e.Source == map && e.Message.Contains("unknown char '?'"));
    }

    [Fact]
    public void LoadArea_IndoorAreaWithMap_IsAnError()
    {
        using var temp = new TempDir();
        var json = SampleArea.AreaJson(SampleArea.Tileset, SampleArea.Rooms, extra: "\n  \"map\": \"map.json\",");
        var path = temp.Write("area.json", json);
        Assert.Contains(MortarLoader.LoadArea(path).Errors, e => e.Message == "'map' is only allowed in fixed areas");
    }
}
