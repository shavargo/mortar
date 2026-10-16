using Mortar.Core.Model;
using Mortar.Game;
using Mortar.Game.Commands;
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
        Assert.Equal(0, content.ShallowestLevel);
        Assert.Equal(4, content.DeepestLevel);
        Assert.Equal("town", content.Level(0).AreaId);
        Assert.Equal("gravewarden", content.Level(4).BossId);
        Assert.Equal(
            new[] { "cathedral-1", "cathedral-2", "cathedral-3", "cathedral-4", "town" },
            result.Value.Areas.Keys.OrderBy(id => id, StringComparer.Ordinal));
    }

    [Fact]
    public void RepoContent_StartsInAQuietTownAboveAPopulatedCathedral()
    {
        var content = GameContentLoader.Load(ContentFixture.Manifest).Value!;
        var created = World.Create(content.Content, content.CreateLevelGenerator(), "warrior", 1);
        Assert.True(created.Succeeded, created.Error);
        var world = created.World!;
        Assert.Equal(0, world.Depth);
        Assert.DoesNotContain(world.Level.Actors, actor => actor.Kind == ActorKind.Monster);
        world.Level.Move(world.Player, world.Level.FindMarker(MarkerKinds.Exit)!.Value);
        world.Tick(new Command[] { new UseStairsCommand() });
        Assert.Equal(1, world.Depth);
        Assert.Contains(world.Level.Actors, actor => actor.Kind == ActorKind.Monster);
    }

    [Fact]
    public void TownSpawns_AreAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var levels = Path.Combine(temp.Root, "levels.json");
        File.WriteAllText(levels, File.ReadAllText(levels).Replace("\"area\": \"areas/town.json\"", "\"area\": \"areas/town.json\", \"spawns\": []"));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == levels && e.Message.StartsWith("invalid JSON") && e.Message.Contains("spawns"));
    }

    [Fact]
    public void TownWithIndoorArea_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var levels = Path.Combine(temp.Root, "levels.json");
        File.WriteAllText(levels, File.ReadAllText(levels).Replace("\"area\": \"areas/town.json\"", "\"area\": \"areas/cathedral-1.json\""));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Source == levels && e.Message == "the town area must use mode 'fixed'");
    }

    [Fact]
    public void TownMapWithoutExit_IsAnError()
    {
        using var temp = new TempDir();
        var manifest = ContentFixture.Copy(temp);
        var map = Path.Combine(temp.Root, "maps", "town.json");
        File.WriteAllText(map, File.ReadAllText(map).Replace('X', '.'));
        var result = GameContentLoader.Load(manifest);
        Assert.Contains(result.Errors, e => e.Message == "the town map must have exactly one exit (X), found 0");
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
        Assert.Equal(17, content.ItemBases.Count);
        Assert.Equal(13, content.Affixes.Count);
        Assert.Equal(4, content.Items.LootTables.Count);
        Assert.Equal("common", content.Monster("zombie").LootTableId);
        Assert.True(content.TryGetClass("warrior", out var warrior));
        Assert.Equal(new[] { "iron-knife", "padded-vest", "healing-draught", "healing-draught" }, warrior!.StartingItems);
        Assert.True(content.TryGetClass("rogue", out var rogue));
        Assert.Equal(new[] { "short-bow", "arrows", "padded-vest", "healing-draught", "healing-draught" }, rogue!.StartingItems);
        Assert.True(content.TryGetItemBase("short-bow", out var bow));
        Assert.True(bow!.Ranged);
        Assert.Equal("arrows", bow.AmmoId);
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
