using Mortar.Core.Model;

namespace Mortar.Core.Authoring;

internal sealed class RoomGridParser
{
    private readonly RoomDefinition _definition;
    private readonly Tileset _tileset;
    private readonly int _stride;
    private readonly List<AuthoringError> _errors;
    private readonly int _width;
    private readonly int _height;
    private readonly int[] _ground;
    private readonly List<Door> _doors = new List<Door>();
    private readonly List<RoomMarker> _markers = new List<RoomMarker>();

    public RoomGridParser(RoomDefinition definition, Tileset tileset, int stride, List<AuthoringError> errors)
    {
        _definition = definition;
        _tileset = tileset;
        _stride = stride;
        _errors = errors;
        _width = definition.Grid[0].Length;
        _height = definition.Grid.Count;
        _ground = new int[_width * _height];
    }

    public Room Parse()
    {
        for (var y = 0; y < _height; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                ParseCell(x, y);
            }
        }
        CheckDoorsAndTags();
        return new Room(_definition.Id, _definition.Tags, _width, _height, _stride, _ground, _doors, _markers);
    }

    private void ParseCell(int x, int y)
    {
        var symbol = _definition.Grid[y][x];
        var index = y * _width + x;
        if (symbol == MarkerChars.Door)
        {
            _ground[index] = _tileset.WallIndex;
            AddDoor(x, y);
            return;
        }
        if (MarkerChars.TryGetKind(symbol, out var kind))
        {
            _ground[index] = _tileset.FloorIndex;
            AddMarker(kind, symbol, x, y);
            return;
        }
        var ground = _tileset.IndexOfSymbol(symbol);
        if (ground < 0)
        {
            Error($"uses unknown ground char '{symbol}' at ({x}, {y})");
            ground = _tileset.VoidIndex;
        }
        _ground[index] = ground;
    }

    private void AddDoor(int x, int y)
    {
        if (!TryGetEdge(x, y, out var side, out var along))
        {
            Error($"has a door at ({x}, {y}) that is not on its outer edge");
            return;
        }
        if (along % _stride == 0)
        {
            Error($"has a door at ({x}, {y}) on a slot corner; doors must sit between slot corners");
            return;
        }
        _doors.Add(new Door(x, y, side, along / _stride));
    }

    private void AddMarker(string kind, char symbol, int x, int y)
    {
        if (x == 0 || y == 0 || x == _width - 1 || y == _height - 1)
        {
            Error($"has marker '{symbol}' on its edge at ({x}, {y})");
            return;
        }
        _markers.Add(new RoomMarker(kind, x, y));
    }

    private bool TryGetEdge(int x, int y, out Side side, out int along)
    {
        (side, along) = (Side.North, x);
        if (y == 0) return true;
        (side, along) = (Side.South, x);
        if (y == _height - 1) return true;
        (side, along) = (Side.West, y);
        if (x == 0) return true;
        (side, along) = (Side.East, y);
        return x == _width - 1;
    }

    private void CheckDoorsAndTags()
    {
        if (_doors.Count == 0)
        {
            Error("has no doors");
        }
        foreach (var landmark in MarkerKinds.Landmarks)
        {
            if (_definition.Tags.Contains(landmark) && !_markers.Any(m => m.Kind == landmark))
            {
                Error($"is tagged '{landmark}' but has no {landmark} marker");
            }
        }
    }

    private void Error(string message) =>
        _errors.Add(new AuthoringError(_definition.Source, $"room '{_definition.Id}' {message}"));
}
