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

    [Theory]
    [InlineData("\"rooms\": [", "\"rooms\": [null, ", "rooms entry 0 is empty")]
    [InlineData("\"nodes\": [", "\"nodes\": [null, ", "graph node 0 is null")]
    [InlineData("\"edges\": [", "\"edges\": [null, ", "graph edge 0 is null")]
    [InlineData("\"tileset\": \"", "\"tileset\": \"bad\\u0000", "invalid path")]
    public void LoadArea_NullOrInvalidEntries_AreErrors(string find, string replace, string expected)
    {
        using var temp = new TempDir();
        var json = SampleArea.AreaJson(SampleArea.Tileset, SampleArea.Rooms).Replace(find, replace);
        var path = temp.Write("area.json", json);
        Assert.Contains(MortarLoader.LoadArea(path).Errors, e => e.Message.Contains(expected));
    }

    [Fact]
    public void LoadArea_EmptyPath_IsAnError() =>
        Assert.Contains(MortarLoader.LoadArea("").Errors, e => e.Message.Contains("invalid path"));

    [Fact]
    public void LoadTileset_NullGroundType_IsAnError()
    {
        using var temp = new TempDir();
        var path = temp.Write("tileset.json", """{ "groundTypes": [null] }""");
        Assert.Contains(MortarLoader.LoadTileset(path).Errors, e => e.Message.Contains("ground type 0 is null"));
    }

    [Fact]
    public void LoadArea_RoomFolder_MatchesJsonExtensionIgnoringCase()
    {
        using var temp = new TempDir();
        CopySampleRooms(temp);
        File.Move(Path.Combine(temp.Root, "rooms", "boss.json"), Path.Combine(temp.Root, "rooms", "Boss.JSON"));
        var area = temp.Write("area.json", SampleArea.AreaJson(SampleArea.Tileset, "rooms"));
        Assert.True(MortarLoader.LoadArea(area).Succeeded);
    }

    [Fact]
    public void LoadArea_ReadsRoomWeights()
    {
        using var temp = new TempDir();
        CopySampleRooms(temp);
        WriteHall(temp, "\"weight\": 5, \"tags\"");
        var area = temp.Write("area.json", SampleArea.AreaJson(SampleArea.Tileset, "rooms"));
        var loaded = MortarLoader.LoadArea(area);
        Assert.True(loaded.Succeeded, string.Join("; ", loaded.Errors));
        Assert.Equal(5, loaded.Value!.Rooms.Single(r => r.Id == "filler_hall").Weight);
    }

    [Fact]
    public void LoadArea_RoomWeightOutOfRange_NamesTheRoomFile()
    {
        using var temp = new TempDir();
        CopySampleRooms(temp);
        var hall = WriteHall(temp, "\"weight\": 0, \"tags\"");
        var area = temp.Write("area.json", SampleArea.AreaJson(SampleArea.Tileset, "rooms"));
        Assert.Contains(MortarLoader.LoadArea(area).Errors, e => e.Source == hall && e.Message.Contains("weight must be between 1 and 1000"));
    }

    private static void CopySampleRooms(TempDir temp)
    {
        foreach (var file in Directory.GetFiles(Path.Combine(TestPaths.Samples, "rooms")))
        {
            temp.Write(Path.Combine("rooms", Path.GetFileName(file)), File.ReadAllText(file));
        }
    }

    private static string WriteHall(TempDir temp, string tagsReplacement)
    {
        var text = File.ReadAllText(Path.Combine(TestPaths.Samples, "rooms", "filler_hall.json"));
        return temp.Write(Path.Combine("rooms", "filler_hall.json"), text.Replace("\"tags\"", tagsReplacement));
    }
}
