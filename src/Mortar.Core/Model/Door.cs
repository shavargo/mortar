namespace Mortar.Core.Model;

public sealed class Door
{
    public Door(int x, int y, Side side, int segment)
    {
        X = x;
        Y = y;
        Side = side;
        Segment = segment;
    }

    public int X { get; }

    public int Y { get; }

    public Side Side { get; }

    public int Segment { get; }
}
