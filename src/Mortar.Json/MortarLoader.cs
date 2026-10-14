using Mortar.Core.Authoring;
using Mortar.Core.Model;

namespace Mortar.Json;

public static class MortarLoader
{
    public static AuthoringResult<CompiledArea> LoadArea(string areaPath)
    {
        var source = Path.GetFullPath(areaPath);
        var dto = JsonFile.Read<AreaDto>(source, out var readError);
        if (dto is null)
        {
            return AuthoringResult<CompiledArea>.Failure(new[] { readError! });
        }
        var area = AreaMapper.ToArea(source, dto);
        if (!area.Succeeded)
        {
            return AuthoringResult<CompiledArea>.Failure(area.Errors);
        }
        var directory = Path.GetDirectoryName(source)!;
        var tileset = LoadTileset(Path.Combine(directory, dto.Tileset!));
        var errors = new List<AuthoringError>(tileset.Errors);
        var rooms = LoadRooms(source, directory, dto.Rooms!, errors);
        if (errors.Count > 0)
        {
            return AuthoringResult<CompiledArea>.Failure(errors);
        }
        return AreaCompiler.Compile(area.Value!, tileset.Value!, rooms);
    }

    public static AuthoringResult<Tileset> LoadTileset(string path)
    {
        var source = Path.GetFullPath(path);
        var dto = JsonFile.Read<TilesetDto>(source, out var error);
        return dto is null
            ? AuthoringResult<Tileset>.Failure(new[] { error! })
            : DefinitionMapper.ToTileset(source, dto);
    }

    private static List<RoomDefinition> LoadRooms(
        string areaSource,
        string directory,
        IReadOnlyList<string> entries,
        List<AuthoringError> errors)
    {
        var rooms = new List<RoomDefinition>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            foreach (var file in ExpandRoomEntry(areaSource, directory, entry, errors))
            {
                if (!seen.Add(file)) continue;
                var dto = JsonFile.Read<RoomDto>(file, out var error);
                if (dto is null) errors.Add(error!);
                else rooms.Add(DefinitionMapper.ToRoom(file, dto));
            }
        }
        return rooms;
    }

    private static IEnumerable<string> ExpandRoomEntry(
        string areaSource,
        string directory,
        string entry,
        List<AuthoringError> errors)
    {
        var path = Path.GetFullPath(Path.Combine(directory, entry));
        if (Directory.Exists(path))
        {
            return Directory.GetFiles(path, "*.json").OrderBy(file => Path.GetFileName(file), StringComparer.Ordinal);
        }
        if (File.Exists(path))
        {
            return new[] { path };
        }
        errors.Add(new AuthoringError(areaSource, $"room path '{entry}' does not exist"));
        return Array.Empty<string>();
    }
}
