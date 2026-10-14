using Mortar.Core.Authoring;
using Mortar.Core.Model;

namespace Mortar.Json;

internal static class FixedAreaLoader
{
    public const string Mode = "fixed";

    public static AuthoringResult<CompiledArea> Load(string source, AreaDto dto)
    {
        var errors = CheckFields(source, dto);
        if (errors.Count > 0)
        {
            return AuthoringResult<CompiledArea>.Failure(errors);
        }
        var directory = Path.GetDirectoryName(source)!;
        var tileset = MortarLoader.LoadAreaTileset(source, directory, dto.Tileset);
        errors.AddRange(tileset.Errors);
        var map = LoadMap(source, directory, dto.Map, errors);
        if (errors.Count > 0)
        {
            return AuthoringResult<CompiledArea>.Failure(errors);
        }
        return AreaCompiler.CompileFixed(AreaDefinition.Fixed(source, dto.Id ?? ""), tileset.Value!, map!);
    }

    private static List<AuthoringError> CheckFields(string source, AreaDto dto)
    {
        var errors = new List<AuthoringError>();
        void NotAllowed(bool present, string field)
        {
            if (present) errors.Add(new AuthoringError(source, $"'{field}' is not allowed in a fixed area"));
        }
        NotAllowed(dto.Graph is not null, "graph");
        NotAllowed(dto.SlotSize is not null, "slotSize");
        NotAllowed(dto.Rooms is not null, "rooms");
        NotAllowed(dto.RoomTags is not null, "roomTags");
        NotAllowed(dto.MaxSideBranches is not null, "maxSideBranches");
        NotAllowed(dto.TargetSize is not null, "targetSize");
        NotAllowed(dto.MaxAttempts is not null, "maxAttempts");
        if (string.IsNullOrWhiteSpace(dto.Tileset)) errors.Add(new AuthoringError(source, "area has no tileset"));
        if (string.IsNullOrWhiteSpace(dto.Map)) errors.Add(new AuthoringError(source, "fixed area has no map"));
        return errors;
    }

    private static RoomDefinition? LoadMap(string source, string directory, string? entry, List<AuthoringError> errors)
    {
        var path = SafePath.Resolve(directory, entry);
        if (path is null)
        {
            errors.Add(new AuthoringError(source, $"map '{entry}' is an invalid path"));
            return null;
        }
        if (!File.Exists(path))
        {
            errors.Add(new AuthoringError(source, $"map '{entry}' does not exist"));
            return null;
        }
        var dto = JsonFile.Read<RoomDto>(path, out var error);
        if (dto is null)
        {
            errors.Add(error!);
            return null;
        }
        return DefinitionMapper.ToRoom(path, dto);
    }
}
