namespace Mortar.Json;

internal sealed class TilesetDto
{
    public List<GroundTypeDto?>? GroundTypes { get; set; }

    public Dictionary<string, List<int>?>? VariantWeights { get; set; }

    public List<MarkerDto?>? Markers { get; set; }
}

internal sealed class GroundTypeDto
{
    public string? Id { get; set; }

    public string? Char { get; set; }

    public bool Walkable { get; set; }

    public bool BlocksSight { get; set; }
}

internal sealed class MarkerDto
{
    public string? Char { get; set; }

    public string? Kind { get; set; }
}

internal sealed class RoomDto
{
    public string? Id { get; set; }

    public List<string>? Tags { get; set; }

    public int? Weight { get; set; }

    public List<string?>? Grid { get; set; }
}

internal sealed class AreaDto
{
    public string? Id { get; set; }

    public string? Mode { get; set; }

    public string? Tileset { get; set; }

    public TargetSizeDto? TargetSize { get; set; }

    public int? SlotSize { get; set; }

    public List<string?>? Rooms { get; set; }

    public List<string>? RoomTags { get; set; }

    public int MaxSideBranches { get; set; }

    public int? MaxAttempts { get; set; }

    public GraphDto? Graph { get; set; }
}

internal sealed class TargetSizeDto
{
    public int Width { get; set; }

    public int Height { get; set; }

    public int Tolerance { get; set; }
}

internal sealed class GraphDto
{
    public List<NodeDto?>? Nodes { get; set; }

    public List<EdgeDto?>? Edges { get; set; }
}

internal sealed class NodeDto
{
    public string? Id { get; set; }

    public string? Kind { get; set; }

    public RegionDto? Region { get; set; }
}

internal sealed class RegionDto
{
    public List<double>? X { get; set; }

    public List<double>? Y { get; set; }
}

internal sealed class EdgeDto
{
    public string? From { get; set; }

    public string? To { get; set; }

    public string? Kind { get; set; }
}
