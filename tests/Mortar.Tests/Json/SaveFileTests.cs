using System.Text.RegularExpressions;
using Mortar.Game.Content;
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using Mortar.Json.Saves;
using Mortar.Tests.Gameplay.Support;
using Mortar.Tests.Support;

namespace Mortar.Tests.Json;

public class SaveFileTests
{
    private static readonly ContentSet TestContent = TestGameContent.Valid(
        items: TestGameContent.Items(new[] { TestGameContent.Weapon(), TestGameContent.Armor(), TestGameContent.Potion() }),
        classes: new[] { TestGameContent.Warrior("knife", "vest", "tonic") });

    [Fact]
    public void ScriptedGame_SavedAndLoaded_ContinuesIdentically()
    {
        var content = GameContentLoader.Load(ContentFixture.Manifest).Value!;
        var world = World.Create(content.Content, content.CreateLevelGenerator(), "warrior", 7).World!;
        var firstHalf = ScriptedPlayer.Play(world, 1500);
        var loaded = SaveFile.FromJson(SaveFile.ToJson(world), content.Content, content.CreateLevelGenerator());
        Assert.True(loaded.Succeeded, loaded.Error);
        Assert.Equal(WorldHash.Compute(world), WorldHash.Compute(loaded.World!));
        Assert.Equal(ScriptedPlayer.Play(world, 1500), ScriptedPlayer.Play(loaded.World!, 1500));
        Assert.Equal(WorldHash.Compute(world), WorldHash.Compute(loaded.World!));
        Assert.Contains(firstHalf, line => line.Contains("picked up"));
    }

    [Fact]
    public void WriteAndRead_RoundTripsThroughAFile()
    {
        using var temp = new TempDir();
        var path = Path.Combine(temp.Root, "slot1.json");
        var world = TestWorlds.Create(TestContent);
        TestWorlds.Run(world, 10);
        Assert.Null(SaveFile.Write(path, world));
        TestWorlds.Run(world, 10);
        Assert.Null(SaveFile.Write(path, world));
        var loaded = SaveFile.Read(path, TestContent, Generator());
        Assert.True(loaded.Succeeded, loaded.Error);
        Assert.Equal(WorldHash.Compute(world), WorldHash.Compute(loaded.World!));
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Read_MissingFile_IsAnError()
    {
        using var temp = new TempDir();
        var result = SaveFile.Read(Path.Combine(temp.Root, "none.json"), TestContent, Generator());
        Assert.Contains("file not found", result.Error);
    }

    [Fact]
    public void Read_HugeFile_IsAnError()
    {
        using var temp = new TempDir();
        var path = Path.Combine(temp.Root, "huge.json");
        File.WriteAllText(path, new string(' ', (int)SaveFile.MaxBytes + 1));
        Assert.Contains("larger than", SaveFile.Read(path, TestContent, Generator()).Error);
    }

    [Theory]
    [InlineData("not json", "save is not valid JSON")]
    [InlineData("[]", "save must be a JSON object")]
    [InlineData("{}", "save has no version number")]
    [InlineData("{\"version\": \"one\"}", "save has no version number")]
    [InlineData("{\"version\": 9}", "unsupported save version 9")]
    public void FromJson_RejectsBrokenFiles(string json, string error)
    {
        Assert.Contains(error, SaveFile.FromJson(json, TestContent, Generator()).Error);
    }

    [Theory]
    [InlineData("\"version\": 3,", "\"version\": 3, \"cheat\": 1,", "save is not valid")]
    [InlineData("\"levels\": [", "\"levels\": [ null,", "save is invalid: level is missing")]
    public void FromJson_RejectsEditedSaves(string find, string replace, string error)
    {
        var json = SaveFile.ToJson(TestWorlds.Create(TestContent));
        Assert.Contains(find, json);
        Assert.Contains(error, SaveFile.FromJson(json.Replace(find, replace), TestContent, Generator()).Error);
    }

    [Fact]
    public void FromJson_NumberTooLargeForItsField_IsATypeError()
    {
        var json = SaveFile.ToJson(TestWorlds.Create(TestContent));
        var huge = Regex.Replace(json, "\"nextItemId\": \\d+", "\"nextItemId\": 99999999999");
        Assert.NotEqual(json, huge);
        var error = SaveFile.FromJson(huge, TestContent, Generator()).Error;
        Assert.StartsWith("save is not valid: ", error);
        Assert.Contains("System.Int32", error);
        Assert.Contains("nextItemId", error);
        Assert.DoesNotContain("could not be mapped", error);
    }

    [Fact]
    public void FromJson_CapitalizedVersion_LoadsLikeTheLoader()
    {
        var world = TestWorlds.Create(TestContent);
        var json = SaveFile.ToJson(world).Replace("\"version\": 3", "\"Version\": 3");
        var loaded = SaveFile.FromJson(json, TestContent, Generator());
        Assert.True(loaded.Succeeded, loaded.Error);
        Assert.Equal(WorldHash.Compute(world), WorldHash.Compute(loaded.World!));
    }

    [Fact]
    public void FromJson_Version1_IsAcceptedAndMigrated()
    {
        var world = TestWorlds.Create(TestContent);
        var json = SaveFile.ToJson(world).Replace("\"version\": 3", "\"version\": 1");
        var loaded = SaveFile.FromJson(json, TestContent, Generator());
        Assert.True(loaded.Succeeded, loaded.Error);
        Assert.Equal(1, loaded.World!.NextProjectileId);
        Assert.Null(loaded.World.Portal);
        Assert.Empty(loaded.World.Progress.Spells);
        Assert.Null(loaded.World.ActiveVendor);
        Assert.Empty(loaded.World.Smith.Stock);
    }

    [Fact]
    public void FromJson_Version2_IsAcceptedAndMigrated()
    {
        var world = TestWorlds.Create(TestContent);
        var json = SaveFile.ToJson(world).Replace("\"version\": 3", "\"version\": 2");
        var loaded = SaveFile.FromJson(json, TestContent, Generator());
        Assert.True(loaded.Succeeded, loaded.Error);
        Assert.Null(loaded.World!.ActiveVendor);
        Assert.Empty(loaded.World.Smith.Stock);
        Assert.Equal(
            loaded.World.TickCount + loaded.World.Content.Rules.VendorStockRegenTicks,
            loaded.World.NextStockRegenTick);
    }

    [Fact]
    public void FromJson_Null_IsAnEmptySave()
    {
        Assert.Equal("save is empty", SaveFile.FromJson(null!, TestContent, Generator()).Error);
    }

    [Fact]
    public void FromJson_WrongTypeOrFingerprint_IsAnError()
    {
        var json = SaveFile.ToJson(TestWorlds.Create(TestContent));
        var badTick = Regex.Replace(json, "\"tick\": \\d+", "\"tick\": \"soon\"");
        Assert.Contains("save is not valid", SaveFile.FromJson(badTick, TestContent, Generator()).Error);
        var badLevel = Regex.Replace(json, "\"fingerprint\": \"[^\"]*\"", "\"fingerprint\": \"0000\"");
        Assert.Contains("does not match", SaveFile.FromJson(badLevel, TestContent, Generator()).Error);
    }

    private static FixedLevelGenerator Generator() => new FixedLevelGenerator(TestWorlds.Room, TestWorlds.Room);
}
