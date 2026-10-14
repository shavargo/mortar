namespace Mortar.Core.Model;

public sealed class RoomMarker
{
    public RoomMarker(string kind, int x, int y)
    {
        Kind = kind;
        X = x;
        Y = y;
    }

    public string Kind { get; }

    public int X { get; }

    public int Y { get; }
}
