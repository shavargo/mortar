using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using Mortar.Tests.Support;

namespace Mortar.Tests.Json;

public class GameContentLoaderTests
{
    [Fact]
    public void RepoContent_Loads()
    {
        var result = GameContentLoader.Load(ContentFixture.Manifest);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        var content = result.Value!.Content;
        Assert.True(content.TryGetClass("warrior", out _));
        Assert.Equal(6, content.Monsters.Count);
        Assert.Equal(4, content.DeepestLevel);
        Assert.Equal("gravewarden", content.Level(4).BossId);
        Assert.True(result.Value.Areas.ContainsKey("crypt"));
    }

    [Fact]
    public void RepoContent_CreatesAPlayableWorld()
    {
        var content = GameContentLoader.Load(ContentFixture.Manifest).Value!;
        var world = World.Create(content.Content, content.CreateLevelGenerator(), "warrior", 1);
        Assert.True(world.Succeeded, world.Error);
        Assert.Contains(world.World!.Level.Actors, actor => actor.Kind == ActorKind.Monster);
    }

    [Fact]
    public void MissingField_NamesTheFile()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var zombie = Path.Combine(temp.Root, "monsters", "zombie.json");
        File.WriteAllText(zombie, File.ReadAllText(zombie).Replace("\"level\": 1,", ""));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == zombie && e.Message == "'level' is required");
    }

    [Fact]
    public void MissingExperienceRule_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var rules = Path.Combine(temp.Root, "rules.json");
        File.WriteAllText(rules, File.ReadAllText(rules).Replace("\"minimumExperiencePercent\": 25,", ""));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == rules && e.Message == "'minimumExperiencePercent' is required");
    }

    [Fact]
    public void UnknownProperty_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var zombie = Path.Combine(temp.Root, "monsters", "zombie.json");
        File.WriteAllText(zombie, File.ReadAllText(zombie).Replace("\"level\": 1,", "\"level\": 1, \"speed\": 3,"));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == zombie && e.Message.StartsWith("invalid JSON") && e.Message.Contains("speed"));
    }

    [Fact]
    public void UnknownSpawnMonster_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var levels = Path.Combine(temp.Root, "levels.json");
        File.WriteAllText(levels, File.ReadAllText(levels).Replace("\"monster\": \"zombie\"", "\"monster\": \"ghoul\""));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == "level 1" && e.Message == "spawn references unknown monster 'ghoul'");
    }

    [Fact]
    public void MissingMonstersFolder_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        Directory.Delete(Path.Combine(temp.Root, "monsters"), recursive: true);
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == manifest && e.Message == "'monsters' must name an existing folder");
    }

    [Fact]
    public void RepoContent_HasItemsAffixesAndLoot()
    {
        var content = GameContentLoader.Load(ContentFixture.Manifest).Value!.Content;
        Assert.Equal(15, content.ItemBases.Count);
        Assert.Equal(13, content.Affixes.Count);
        Assert.Equal(4, content.Items.LootTables.Count);
        Assert.Equal("common", content.Monster("zombie").LootTableId);
        Assert.True(content.TryGetClass("warrior", out var warrior));
        Assert.Equal(new[] { "iron-knife", "padded-vest", "healing-draught", "healing-draught" }, warrior!.StartingItems);
    }

    [Fact]
    public void RepoContent_StartsTheWarriorEquipped()
    {
        var content = GameContentLoader.Load(ContentFixture.Manifest).Value!;
        var world = World.Create(content.Content, content.CreateLevelGenerator(), "warrior", 1).World!;
        Assert.Equal("iron-knife", world.Belongings.Equipment.Get(EquipmentSlot.Weapon)!.Definition.Id);
        Assert.Equal("padded-vest", world.Belongings.Equipment.Get(EquipmentSlot.Body)!.Definition.Id);
        Assert.Equal("healing-draught", world.Belongings.Belt.Get(1)!.Definition.Id);
    }

    [Fact]
    public void MissingItemField_NamesTheFile()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var vest = Path.Combine(temp.Root, "items", "padded-vest.json");
        File.WriteAllText(vest, File.ReadAllText(vest).Replace("\"itemLevel\": 1,", ""));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == vest && e.Message == "'itemLevel' is required");
    }

    [Fact]
    public void NullAffixEntry_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var affixes = Path.Combine(temp.Root, "affixes.json");
        File.WriteAllText(affixes, File.ReadAllText(affixes).Replace("\"affixes\": [", "\"affixes\": [null,"));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == affixes && e.Message == "affixes entry 0 is null");
    }

    [Fact]
    public void UnknownLootTable_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var zombie = Path.Combine(temp.Root, "monsters", "zombie.json");
        File.WriteAllText(zombie, File.ReadAllText(zombie).Replace("\"loot\": \"common\"", "\"loot\": \"epic\""));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == "monster 'zombie'" && e.Message == "loot references unknown loot table 'epic'");
    }

    [Fact]
    public void MissingItemsFolder_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        Directory.Delete(Path.Combine(temp.Root, "items"), recursive: true);
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == manifest && e.Message == "'items' must name an existing folder");
    }
}
