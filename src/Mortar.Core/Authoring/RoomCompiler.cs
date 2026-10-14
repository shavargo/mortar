using Mortar.Core.Model;

namespace Mortar.Core.Authoring;

public static class RoomCompiler
{
    public static AuthoringResult<Room> Compile(RoomDefinition definition, Tileset tileset, int slotSize)
    {
        var errors = new List<AuthoringError>();
        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            errors.Add(new AuthoringError(definition.Source, "room has no id"));
        }
        if (definition.Weight < 1 || definition.Weight > Room.MaxWeight)
        {
            errors.Add(new AuthoringError(definition.Source, $"room '{definition.Id}' weight must be between 1 and {Room.MaxWeight}"));
        }
        if (!CheckShape(definition, slotSize, errors))
        {
            return AuthoringResult<Room>.Failure(errors);
        }

        var room = new RoomGridParser(definition, tileset, slotSize - 1, errors).Parse();
        return errors.Count > 0 ? AuthoringResult<Room>.Failure(errors) : AuthoringResult<Room>.Success(room);
    }

    private static bool CheckShape(RoomDefinition definition, int slotSize, List<AuthoringError> errors)
    {
        var grid = definition.Grid;
        if (grid.Count == 0 || grid[0].Length == 0)
        {
            errors.Add(new AuthoringError(definition.Source, $"room '{definition.Id}' has an empty grid"));
            return false;
        }
        var width = grid[0].Length;
        for (var y = 1; y < grid.Count; y++)
        {
            if (grid[y].Length != width)
            {
                errors.Add(new AuthoringError(
                    definition.Source,
                    $"room '{definition.Id}' row {y} has length {grid[y].Length}, expected {width}"));
                return false;
            }
        }
        var stride = slotSize - 1;
        if (FitsSlots(width, stride) && FitsSlots(grid.Count, stride))
        {
            return true;
        }
        errors.Add(new AuthoringError(
            definition.Source,
            $"room '{definition.Id}' is {width}x{grid.Count}; width and height must be (slotSize - 1) x n + 1 " +
            $"(for slotSize {slotSize}: {stride + 1}, {2 * stride + 1}, {3 * stride + 1}...)"));
        return false;
    }

    private static bool FitsSlots(int length, int stride) => length > 1 && (length - 1) % stride == 0;
}
