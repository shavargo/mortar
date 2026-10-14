namespace Mortar.Core.Model;

public enum AreaMode
{
    Indoor,
    Outdoor,
    Fixed,
}

public sealed class TargetSize
{
    public TargetSize(int width, int height, int tolerancePercent)
    {
        Width = width;
        Height = height;
        TolerancePercent = tolerancePercent;
    }

    public int Width { get; }

    public int Height { get; }

    public int TolerancePercent { get; }

    public int MinWidth => Width * (100 - TolerancePercent) / 100;

    public int MaxWidth => (Width * (100 + TolerancePercent) + 99) / 100;

    public int MinHeight => Height * (100 - TolerancePercent) / 100;

    public int MaxHeight => (Height * (100 + TolerancePercent) + 99) / 100;
}

public sealed class AreaDefinition
{
    public const int DefaultMaxAttempts = 20;

    /// <summary>A fixed area is one stamped map, so the indoor settings are unused placeholders.</summary>
    public static AreaDefinition Fixed(string source, string id) => new AreaDefinition(
        source,
        id,
        AreaMode.Fixed,
        new TargetSize(0, 0, 0),
        0,
        0,
        1,
        Array.Empty<string>(),
        Array.Empty<GraphNode>(),
        Array.Empty<GraphEdge>());

    public AreaDefinition(
        string source,
        string id,
        AreaMode mode,
        TargetSize targetSize,
        int slotSize,
        int maxSideBranches,
        int maxAttempts,
        IReadOnlyList<string> roomTags,
        IReadOnlyList<GraphNode> nodes,
        IReadOnlyList<GraphEdge> edges)
    {
        Source = source;
        Id = id;
        Mode = mode;
        TargetSize = targetSize;
        SlotSize = slotSize;
        MaxSideBranches = maxSideBranches;
        MaxAttempts = maxAttempts;
        RoomTags = roomTags;
        Nodes = nodes;
        Edges = edges;
    }

    public string Source { get; }

    public string Id { get; }

    public AreaMode Mode { get; }

    public TargetSize TargetSize { get; }

    public int SlotSize { get; }

    public int MaxSideBranches { get; }

    public int MaxAttempts { get; }

    public IReadOnlyList<string> RoomTags { get; }

    public IReadOnlyList<GraphNode> Nodes { get; }

    public IReadOnlyList<GraphEdge> Edges { get; }
}
