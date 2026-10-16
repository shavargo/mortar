using Mortar.Core.Authoring;
using Mortar.Core.Model;
using Mortar.Game.Content;

namespace Mortar.Json.Content;

public static class GameContentLoader
{
    public static AuthoringResult<GameContent> Load(string manifestPath)
    {
        var source = SafePath.Resolve(null, manifestPath);
        if (source is null)
        {
            return AuthoringResult<GameContent>.Failure(manifestPath, "invalid path");
        }
        var manifest = JsonFile.Read<ManifestDto>(source, out var readError);
        if (manifest is null)
        {
            return AuthoringResult<GameContent>.Failure(new[] { readError! });
        }
        var directory = Path.GetDirectoryName(source)!;
        var errors = new List<AuthoringError>();
        var rules = LoadRules(source, directory, manifest.Rules, errors);
        var classes = LoadFolder<ClassDto, ClassDefinition>(source, directory, manifest.Classes, "classes", ContentMapper.ToClass, errors);
        var monsters = LoadFolder<MonsterDto, MonsterDefinition>(source, directory, manifest.Monsters, "monsters", ContentMapper.ToMonster, errors);
        var items = LoadFolder<ItemBaseDto, ItemBaseDefinition>(source, directory, manifest.Items, "items", ItemMapper.ToItemBase, errors);
        var spells = LoadFolder<SpellDto, SpellDefinition>(source, directory, manifest.Spells, "spells", SpellMapper.ToSpell, errors);
        var affixes = ItemContentLoader.LoadAffixes(source, directory, manifest.Affixes, errors);
        var loot = ItemContentLoader.LoadLootTables(source, directory, manifest.Loot, errors);
        var areas = new Dictionary<string, CompiledArea>(StringComparer.Ordinal);
        var levels = LevelFileLoader.Load(source, directory, manifest.Levels, areas, errors);
        if (errors.Count > 0 || rules is null)
        {
            return AuthoringResult<GameContent>.Failure(errors);
        }
        var content = ContentSet.Create(
            rules, classes, monsters, levels, new ItemCatalog(items, affixes, loot), new SpellCatalog(spells));
        return content.Succeeded
            ? AuthoringResult<GameContent>.Success(new GameContent(content.Value!, areas))
            : AuthoringResult<GameContent>.Failure(content.Errors);
    }

    private static GameRules? LoadRules(string manifest, string directory, string? entry, List<AuthoringError> errors)
    {
        var path = SafePath.Resolve(directory, entry);
        if (path is null)
        {
            errors.Add(new AuthoringError(manifest, "'rules' is empty or an invalid path"));
            return null;
        }
        var dto = JsonFile.Read<RulesDto>(path, out var error);
        if (dto is null)
        {
            errors.Add(error!);
            return null;
        }
        return ContentMapper.ToRules(path, dto, errors);
    }

    private static List<TDefinition> LoadFolder<TDto, TDefinition>(
        string manifest,
        string directory,
        string? entry,
        string field,
        Func<string, TDto, List<AuthoringError>, TDefinition> map,
        List<AuthoringError> errors)
        where TDto : class
    {
        var definitions = new List<TDefinition>();
        var path = SafePath.Resolve(directory, entry);
        if (path is null || !Directory.Exists(path))
        {
            errors.Add(new AuthoringError(manifest, $"'{field}' must name an existing folder"));
            return definitions;
        }
        var files = Directory.GetFiles(path)
            .Where(file => string.Equals(Path.GetExtension(file), ".json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => Path.GetFileName(file), StringComparer.Ordinal);
        foreach (var file in files)
        {
            var dto = JsonFile.Read<TDto>(file, out var error);
            if (dto is null) errors.Add(error!);
            else definitions.Add(map(file, dto, errors));
        }
        return definitions;
    }
}
