namespace Mortar.Core.Model;

public sealed class FractionRect
{
    public FractionRect(double x0, double x1, double y0, double y1)
    {
        X0 = x0;
        X1 = x1;
        Y0 = y0;
        Y1 = y1;
    }

    public double X0 { get; }

    public double X1 { get; }

    public double Y0 { get; }

    public double Y1 { get; }
}

public sealed class GraphNode
{
    public GraphNode(string id, string kind, FractionRect region)
    {
        Id = id;
        Kind = kind;
        Region = region;
    }

    public string Id { get; }

    public string Kind { get; }

    public FractionRect Region { get; }
}

public enum EdgeKind
{
    Path,
    Branch,
}

public sealed class GraphEdge
{
    public GraphEdge(string from, string to, EdgeKind kind)
    {
        From = from;
        To = to;
        Kind = kind;
    }

    public string From { get; }

    public string To { get; }

    public EdgeKind Kind { get; }
}
