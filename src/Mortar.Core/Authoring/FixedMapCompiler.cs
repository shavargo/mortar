using Mortar.Core.Layouts;
using Mortar.Core.Model;
using Mortar.Core.Validation;

namespace Mortar.Core.Authoring;

internal static class FixedMapCompiler
{
    public static AuthoringResult<FixedMap> Compile(RoomDefinition map, Tileset tileset)
    {
        var errors = new List<AuthoringError>();
        if (!CheckShape(map, errors))
        {
            return AuthoringResult<FixedMap>.Failure(errors);
        }
        var grid = new CellGrid(map.Grid[0].Length, map.Grid.Count, tileset.VoidIndex);
        var markers = new List<Marker>();
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                ParseCell(map, tileset, grid, markers, x, y, errors);
            }
        }
        if (errors.Count == 0)
        {
            CheckReachability(map, tileset, grid, markers, errors);
        }
        return errors.Count > 0
            ? AuthoringResult<FixedMap>.Failure(errors)
            : AuthoringResult<FixedMap>.Success(new FixedMap(grid, markers));
    }

    private static bool CheckShape(RoomDefinition map, List<AuthoringError> errors)
    {
        var rows = map.Grid;
        if (rows.Count == 0 || rows[0].Length == 0)
        {
            errors.Add(new AuthoringError(map.Source, "map has an empty grid"));
            return false;
        }
        for (var y = 1; y < rows.Count; y++)
        {
            if (rows[y].Length != rows[0].Length)
            {
                errors.Add(new AuthoringError(map.Source, $"map row {y} has length {rows[y].Length}, expected {rows[0].Length}"));
                return false;
            }
        }
        if (rows[0].Length > Layout.MaxDimension || rows.Count > Layout.MaxDimension)
        {
            errors.Add(new AuthoringError(
                map.Source,
                $"map is {rows[0].Length}x{rows.Count}; width and height must be at most {Layout.MaxDimension}"));
            return false;
        }
        return true;
    }

    private static void ParseCell(
        RoomDefinition map, Tileset tileset, CellGrid grid, List<Marker> markers, int x, int y, List<AuthoringError> errors)
    {
        var symbol = map.Grid[y][x];
        if (symbol == MarkerChars.Door)
        {
            errors.Add(new AuthoringError(map.Source, $"map has a door at ({x}, {y}); fixed maps have no doors"));
            return;
        }
        if (tileset.TryGetMarkerKind(symbol, out var kind))
        {
            grid[x, y] = tileset.FloorIndex;
            markers.Add(new Marker(kind, null, x, y));
            return;
        }
        var ground = tileset.IndexOfSymbol(symbol);
        if (ground < 0) errors.Add(new AuthoringError(map.Source, $"map uses unknown char '{symbol}' at ({x}, {y})"));
        else grid[x, y] = ground;
    }

    private static void CheckReachability(
        RoomDefinition map, Tileset tileset, CellGrid grid, List<Marker> markers, List<AuthoringError> errors)
    {
        var entrances = markers.Where(m => m.Kind == MarkerKinds.Entrance).ToList();
        if (entrances.Count != 1)
        {
            errors.Add(new AuthoringError(map.Source, $"map must have exactly one entrance (E), found {entrances.Count}"));
            return;
        }
        var reached = LayoutValidator.FloodFill(grid, tileset, entrances[0].X, entrances[0].Y);
        foreach (var marker in markers.Where(m => !reached[m.Y * grid.Width + m.X]))
        {
            errors.Add(new AuthoringError(
                map.Source,
                $"map marker '{marker.Kind}' at ({marker.X}, {marker.Y}) cannot be reached from the entrance"));
        }
        LayoutValidator.VoidUnreached(grid, tileset, reached);
    }
}
