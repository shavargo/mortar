using Mortar.Core.Authoring;
using Mortar.Core.Model;

namespace Mortar.Json;

internal static class AreaMapper
{
    public static AuthoringResult<AreaDefinition> ToArea(string source, AreaDto dto)
    {
        var errors = new List<AuthoringError>();
        void Error(string message) => errors.Add(new AuthoringError(source, message));
        var mode = MapMode(dto.Mode, Error);
        if (string.IsNullOrWhiteSpace(dto.Tileset)) Error("area has no tileset");
        if (dto.Rooms is null || dto.Rooms.Count == 0) Error("area lists no rooms");
        if (dto.TargetSize is null) Error("area has no targetSize");
        if (dto.SlotSize is null) Error("area has no slotSize");
        if (dto.Graph?.Nodes is null) Error("area has no graph nodes");
        var nodes = MapAll(dto.Graph?.Nodes, "graph node", MapNode, Error);
        var edges = MapAll(dto.Graph?.Edges, "graph edge", MapEdge, Error);
        if (errors.Count > 0)
        {
            return AuthoringResult<AreaDefinition>.Failure(errors);
        }
        var size = dto.TargetSize!;
        return AuthoringResult<AreaDefinition>.Success(new AreaDefinition(
            source,
            dto.Id ?? "",
            mode,
            new TargetSize(size.Width, size.Height, size.Tolerance),
            dto.SlotSize!.Value,
            dto.MaxSideBranches,
            dto.MaxAttempts ?? AreaDefinition.DefaultMaxAttempts,
            dto.RoomTags ?? new List<string>(),
            nodes,
            edges));
    }

    private static AreaMode MapMode(string? mode, Action<string> error)
    {
        switch (mode)
        {
            case "indoor": return AreaMode.Indoor;
            case "outdoor": return AreaMode.Outdoor;
            default:
                error($"mode must be 'indoor' or 'outdoor', got '{mode}'");
                return AreaMode.Indoor;
        }
    }

    private static List<TOut> MapAll<TIn, TOut>(
        List<TIn?>? items,
        string label,
        Func<TIn, Action<string>, TOut> map,
        Action<string> error)
        where TIn : class
    {
        var mapped = new List<TOut>();
        if (items is null) return mapped;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item is null) error($"{label} {i} is null");
            else mapped.Add(map(item, error));
        }
        return mapped;
    }

    private static GraphNode MapNode(NodeDto dto, Action<string> error)
    {
        var id = dto.Id ?? "";
        var kind = dto.Kind ?? id;
        var x = dto.Region?.X;
        var y = dto.Region?.Y;
        if (x is not { Count: 2 } || y is not { Count: 2 })
        {
            error($"node '{id}' region needs x and y as [from, to] pairs");
            return new GraphNode(id, kind, new FractionRect(0, 1, 0, 1));
        }
        return new GraphNode(id, kind, new FractionRect(x[0], x[1], y[0], y[1]));
    }

    private static GraphEdge MapEdge(EdgeDto dto, Action<string> error)
    {
        EdgeKind? kind = dto.Kind switch
        {
            "path" => EdgeKind.Path,
            "branch" => EdgeKind.Branch,
            _ => null,
        };
        if (kind is null)
        {
            error($"edge {dto.From}->{dto.To} kind must be 'path' or 'branch', got '{dto.Kind}'");
        }
        return new GraphEdge(dto.From ?? "", dto.To ?? "", kind ?? EdgeKind.Path);
    }
}
