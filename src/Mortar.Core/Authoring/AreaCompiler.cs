using Mortar.Core.Model;

namespace Mortar.Core.Authoring;

public static class AreaCompiler
{
    public static AuthoringResult<CompiledArea> Compile(
        AreaDefinition area,
        Tileset tileset,
        IReadOnlyList<RoomDefinition> roomDefinitions)
    {
        var errors = AreaValidator.Validate(area);
        if (errors.Count > 0)
        {
            return AuthoringResult<CompiledArea>.Failure(errors);
        }
        var rooms = CompileRooms(area, tileset, roomDefinitions, errors);
        if (errors.Count == 0)
        {
            CheckTagCoverage(area, rooms, errors);
        }
        return errors.Count > 0
            ? AuthoringResult<CompiledArea>.Failure(errors)
            : AuthoringResult<CompiledArea>.Success(new CompiledArea(area, tileset, rooms));
    }

    private static List<Room> CompileRooms(
        AreaDefinition area,
        Tileset tileset,
        IReadOnlyList<RoomDefinition> roomDefinitions,
        List<AuthoringError> errors)
    {
        var rooms = new List<Room>();
        foreach (var definition in roomDefinitions)
        {
            if (area.RoomTags.Count > 0 && !definition.Tags.Any(tag => area.RoomTags.Contains(tag)))
            {
                continue;
            }
            var result = RoomCompiler.Compile(definition, tileset, area.SlotSize);
            if (result.Succeeded) rooms.Add(result.Value!);
            else errors.AddRange(result.Errors);
        }
        var duplicates = rooms
            .GroupBy(r => r.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(id => id, StringComparer.Ordinal);
        foreach (var id in duplicates)
        {
            errors.Add(new AuthoringError(area.Source, $"more than one room has id '{id}'"));
        }
        return rooms.OrderBy(r => r.Id, StringComparer.Ordinal).ToList();
    }

    private static void CheckTagCoverage(AreaDefinition area, List<Room> rooms, List<AuthoringError> errors)
    {
        foreach (var node in area.Nodes)
        {
            if (!rooms.Any(r => r.HasTag(node.Kind)))
            {
                errors.Add(new AuthoringError(area.Source, $"no room provides tag '{node.Kind}' needed by node '{node.Id}'"));
            }
        }
        if (!rooms.Any(r => r.IsSingleSlot && (r.HasTag(StandardTags.Filler) || r.HasTag(StandardTags.Corridor))))
        {
            errors.Add(new AuthoringError(area.Source, "no single-slot room is tagged 'filler' or 'corridor'"));
        }
    }
}
