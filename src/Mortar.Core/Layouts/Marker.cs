namespace Mortar.Core.Layouts;

public sealed class Marker
{
    public Marker(string kind, string? nodeId, int x, int y)
    {
        Kind = kind;
        NodeId = nodeId;
        X = x;
        Y = y;
    }

    public string Kind { get; }

    public string? NodeId { get; }

    public int X { get; }

    public int Y { get; }
}
