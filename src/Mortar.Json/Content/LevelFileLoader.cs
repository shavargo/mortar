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
        if (dto.Town is not null)
        {
            var town = LoadTown(path, dto.Town, areas, areaIdsByFile, errors);
            if (town is not null) levels.Add(town);
        }
        for (var i = 0; i < dto.Levels.Count; i++)
        {
            var level = dto.Levels[i];
            if (level is null)
            {
                errors.Add(new AuthoringError(path, $"levels entry {i} is null"));
                continue;
            }
            var areaId = LoadArea(path, $"levels entry {i}", level.Area, areas, areaIdsByFile, errors);
            if (areaId is not null) levels.Add(ContentMapper.ToLevel(path, i, level, areaId, errors));
        }
        return levels;
    }

    private static LevelDefinition? LoadTown(
        string source,
        TownDto town,
        Dictionary<string, CompiledArea> areas,
        Dictionary<string, string> areaIdsByFile,
        List<AuthoringError> errors)
    {
        var id = LoadArea(source, "town", town.Area, areas, areaIdsByFile, errors);
        if (id is null) return null;
        var map = areas[id].Map;
        if (map is null)
        {
            errors.Add(new AuthoringError(source, "the town area must use mode 'fixed'"));
            return null;
        }
        var exits = map.Markers.Count(marker => marker.Kind == MarkerKinds.Exit);
        if (exits != 1)
        {
            errors.Add(new AuthoringError(source, $"the town map must have exactly one exit (X), found {exits}"));
            return null;
        }
        return new LevelDefinition(0, id, Array.Empty<SpawnEntry>(), null);
    }

    private static string? LoadArea(
        string source,
        string label,
        string? entry,
        Dictionary<string, CompiledArea> areas,
        Dictionary<string, string> areaIdsByFile,
        List<AuthoringError> errors)
    {
        var file = SafePath.Resolve(Path.GetDirectoryName(source)!, entry);
        if (file is null)
        {
            errors.Add(new AuthoringError(source, $"{label} has an empty or invalid area path"));
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
