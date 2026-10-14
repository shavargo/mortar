using Mortar.Core.Authoring;
using Mortar.Core.Model;
using Mortar.Game.Content;

namespace Mortar.Json.Content;

internal static class LevelFileLoader
{
    public static List<LevelDefinition> Load(
        string manifest,
        string directory,
        string? entry,
        Dictionary<string, CompiledArea> areas,
        List<AuthoringError> errors)
    {
        var levels = new List<LevelDefinition>();
        var path = SafePath.Resolve(directory, entry);
        if (path is null)
        {
            errors.Add(new AuthoringError(manifest, "'levels' is empty or an invalid path"));
            return levels;
        }
        var dto = JsonFile.Read<LevelsDto>(path, out var error);
        if (dto is null)
        {
            errors.Add(error!);
            return levels;
        }
        if (dto.Levels is null)
        {
            errors.Add(new AuthoringError(path, "'levels' is required"));
            return levels;
        }
        var areaIdsByFile = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < dto.Levels.Count; i++)
        {
            var level = dto.Levels[i];
            if (level is null)
            {
                errors.Add(new AuthoringError(path, $"levels entry {i} is null"));
                continue;
            }
            var areaId = LoadArea(path, i, level.Area, areas, areaIdsByFile, errors);
            if (areaId is not null) levels.Add(ContentMapper.ToLevel(path, i, level, areaId, errors));
        }
        return levels;
    }

    private static string? LoadArea(
        string source,
        int index,
        string? entry,
        Dictionary<string, CompiledArea> areas,
        Dictionary<string, string> areaIdsByFile,
        List<AuthoringError> errors)
    {
        var file = SafePath.Resolve(Path.GetDirectoryName(source)!, entry);
        if (file is null)
        {
            errors.Add(new AuthoringError(source, $"levels entry {index} has an empty or invalid area path"));
            return null;
        }
        if (areaIdsByFile.TryGetValue(file, out var known)) return known;
        var result = MortarLoader.LoadArea(file);
        if (!result.Succeeded)
        {
            errors.AddRange(result.Errors);
            return null;
        }
        var id = result.Value!.Definition.Id;
        if (areas.ContainsKey(id))
        {
            errors.Add(new AuthoringError(file, $"area id '{id}' is already used by another area file"));
            return null;
        }
        areas.Add(id, result.Value);
        areaIdsByFile.Add(file, id);
        return id;
    }
}
