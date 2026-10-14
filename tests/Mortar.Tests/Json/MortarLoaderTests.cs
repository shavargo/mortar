using Mortar.Json;
using Mortar.Tests.Support;

namespace Mortar.Tests.Json;

public class MortarLoaderTests
{
    [Fact]
    public void LoadArea_Sample_Succeeds()
    {
        var area = SampleArea.LoadCrypt();
        Assert.Equal("crypt", area.Definition.Id);
        Assert.Equal(9, area.Rooms.Count);
        Assert.Equal(new[] { "void", "floor", "wall" }, area.Tileset.Grounds.Select(g => g.Id));
        Assert.Equal(new[] { 6, 1, 1 }, area.Tileset.WeightsFor("floor"));
    }

    [Fact]
    public void LoadArea_InvalidJson_NamesTheFile()
    {
        using var temp = new TempDir();
        var path = temp.Write("area.json", "{ not json");
        var error = Assert.Single(MortarLoader.LoadArea(path).Errors);
        Assert.Equal(path, error.Source);
        Assert.StartsWith("invalid JSON", error.Message);
    }

    [Fact]
    public void LoadArea_UnknownProperty_IsAnError()
    {
        using var temp = new TempDir();
        var json = SampleArea.AreaJson(SampleArea.Tileset, SampleArea.Rooms, extra: "\n  \"maxSideBranch\": 2,");
        var path = temp.Write("area.json", json);
        Assert.Contains(MortarLoader.LoadArea(path).Errors, e => e.Message.Contains("maxSideBranch"));
    }

    [Fact]
    public void LoadArea_MissingRoomPath_IsAnError()
    {
        using var temp = new TempDir();
        var path = temp.Write("area.json", SampleArea.AreaJson(SampleArea.Tileset, "missing"));
        Assert.Contains(MortarLoader.LoadArea(path).Errors, e => e.Message == "room path 'missing' does not exist");
    }

    [Fact]
    public void LoadArea_UnknownMode_IsAnError()
    {
        using var temp = new TempDir();
        var path = temp.Write("area.json", SampleArea.AreaJson(SampleArea.Tileset, SampleArea.Rooms, mode: "underwater"));
        Assert.Contains(MortarLoader.LoadArea(path).Errors, e => e.Message.Contains("mode must be 'indoor' or 'outdoor'"));
    }

    [Fact]
    public void LoadArea_BadRoom_NamesTheRoomFile()
    {
        using var temp = new TempDir();
        var room = temp.Write("rooms/bad.json", """{ "id": "bad", "tags": ["filler"], "grid": ["####", "#..#"] }""");
        var area = temp.Write("area.json", SampleArea.AreaJson(SampleArea.Tileset, "rooms"));
        Assert.Contains(MortarLoader.LoadArea(area).Errors, e => e.Source == room && e.Message.Contains("width and height"));
    }
}
