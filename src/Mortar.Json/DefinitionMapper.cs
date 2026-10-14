using Mortar.Core.Authoring;
using Mortar.Core.Model;

namespace Mortar.Json;

internal static class DefinitionMapper
{
    public static AuthoringResult<Tileset> ToTileset(string source, TilesetDto dto)
    {
        var errors = new List<AuthoringError>();
        var grounds = new List<GroundType>();
        var groundTypes = dto.GroundTypes ?? new List<GroundTypeDto?>();
        for (var i = 0; i < groundTypes.Count; i++)
        {
            var ground = groundTypes[i];
            if (ground is null)
            {
                errors.Add(new AuthoringError(source, $"ground type {i} is null"));
                continue;
            }
            if (ground.Char is not { Length: 1 })
            {
                errors.Add(new AuthoringError(source, $"ground '{ground.Id}' must have a single-character char"));
                continue;
            }
            grounds.Add(new GroundType(ground.Id ?? "", ground.Char[0], ground.Walkable, ground.BlocksSight));
        }
        if (errors.Count > 0)
        {
            return AuthoringResult<Tileset>.Failure(errors);
        }
        var weights = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal);
        foreach (var pair in dto.VariantWeights ?? new Dictionary<string, List<int>?>())
        {
            weights[pair.Key] = pair.Value ?? new List<int>();
        }
        return Tileset.Create(source, grounds, weights);
    }

    public static RoomDefinition ToRoom(string source, RoomDto dto) => new RoomDefinition(
        source,
        dto.Id ?? "",
        dto.Tags ?? new List<string>(),
        (dto.Grid ?? new List<string?>()).Select(row => row ?? "").ToList(),
        dto.Weight ?? 1);
}
