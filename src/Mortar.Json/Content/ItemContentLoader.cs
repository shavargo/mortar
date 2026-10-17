using Mortar.Core.Authoring;
using Mortar.Game.Content;

namespace Mortar.Json.Content;

internal static class ItemContentLoader
{
    public static List<AffixDefinition> LoadAffixes(string manifest, string directory, string? entry, List<AuthoringError> errors) =>
        LoadList<AffixListDto, AffixDto, AffixDefinition>(
            manifest, directory, entry, "affixes", "affixes", file => file.Affixes, ItemMapper.ToAffix, errors);

    public static List<LootTableDefinition> LoadLootTables(string manifest, string directory, string? entry, List<AuthoringError> errors) =>
        LoadList<LootListDto, LootTableDto, LootTableDefinition>(
            manifest, directory, entry, "loot", "tables", file => file.Tables, ItemMapper.ToLootTable, errors);

    public static List<UniqueDefinition> LoadUniques(string manifest, string directory, string? entry, List<AuthoringError> errors) =>
        LoadList<UniqueListDto, UniqueDto, UniqueDefinition>(
            manifest, directory, entry, "uniques", "uniques", file => file.Uniques, ItemMapper.ToUnique, errors);

    private static List<TDefinition> LoadList<TFile, TEntry, TDefinition>(
        string manifest,
        string directory,
        string? entry,
        string field,
        string listName,
        Func<TFile, List<TEntry?>?> entries,
        Func<string, int, TEntry, List<AuthoringError>, TDefinition> map,
        List<AuthoringError> errors)
        where TFile : class
        where TEntry : class
    {
        var definitions = new List<TDefinition>();
        var path = SafePath.Resolve(directory, entry);
        if (path is null)
        {
            errors.Add(new AuthoringError(manifest, $"'{field}' is empty or an invalid path"));
            return definitions;
        }
        var file = JsonFile.Read<TFile>(path, out var error);
        if (file is null)
        {
            errors.Add(error!);
            return definitions;
        }
        var list = entries(file);
        if (list is null)
        {
            errors.Add(new AuthoringError(path, $"'{listName}' is required"));
            return definitions;
        }
        for (var i = 0; i < list.Count; i++)
        {
            var dto = list[i];
            if (dto is null) errors.Add(new AuthoringError(path, $"{listName} entry {i} is null"));
            else definitions.Add(map(path, i, dto, errors));
        }
        return definitions;
    }
}
