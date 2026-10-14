using Mortar.Core.Model;

namespace Mortar.Core.Authoring;

internal static class AreaValidator
{
    public static List<AuthoringError> Validate(AreaDefinition area)
    {
        var errors = new List<AuthoringError>();
        CheckSettings(area, errors);
        CheckNodes(area, errors);
        CheckEdges(area, errors);
        if (errors.Count == 0)
        {
            CheckConnectivity(area, errors);
        }
        return errors;
    }

    private static void CheckSettings(AreaDefinition area, List<AuthoringError> errors)
    {
        void Error(string message) => errors.Add(new AuthoringError(area.Source, message));
        var size = area.TargetSize;
        if (string.IsNullOrWhiteSpace(area.Id)) Error("area has no id");
        if (area.Mode == AreaMode.Outdoor) Error("outdoor areas are not supported yet; use mode 'indoor'");
        if (area.SlotSize < 3) Error("slotSize must be at least 3");
        if (size.Width < area.SlotSize || size.Height < area.SlotSize) Error("targetSize width and height must be at least slotSize");
        if (size.TolerancePercent < 0 || size.TolerancePercent > 100) Error("targetSize tolerance must be between 0 and 100");
        if (area.MaxAttempts < 1) Error("maxAttempts must be at least 1");
        if (area.MaxSideBranches < 0) Error("maxSideBranches must not be negative");
    }

    private static void CheckNodes(AreaDefinition area, List<AuthoringError> errors)
    {
        void Error(string message) => errors.Add(new AuthoringError(area.Source, message));
        if (area.Nodes.Count == 0) Error("graph has no nodes");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in area.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id)) Error("a graph node has no id");
            else if (!ids.Add(node.Id)) Error($"duplicate node id '{node.Id}'");
            if (!IsValidRegion(node.Region)) Error($"node '{node.Id}' region must lie within 0..1 with from <= to");
        }
        var entrances = area.Nodes.Count(n => n.Kind == MarkerKinds.Entrance);
        if (entrances != 1) Error($"graph must have exactly one entrance node, found {entrances}");
    }

    private static bool IsValidRegion(FractionRect region) =>
        InUnit(region.X0) && InUnit(region.X1) && InUnit(region.Y0) && InUnit(region.Y1)
        && region.X0 <= region.X1 && region.Y0 <= region.Y1;

    private static bool InUnit(double value) => value >= 0 && value <= 1;

    private static void CheckEdges(AreaDefinition area, List<AuthoringError> errors)
    {
        var ids = area.Nodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var edge in area.Edges)
        {
            var label = $"edge {edge.From}->{edge.To}";
            if (!ids.Contains(edge.From)) errors.Add(new AuthoringError(area.Source, $"{label} references unknown node '{edge.From}'"));
            if (!ids.Contains(edge.To)) errors.Add(new AuthoringError(area.Source, $"{label} references unknown node '{edge.To}'"));
            if (edge.From == edge.To) errors.Add(new AuthoringError(area.Source, $"{label} connects a node to itself"));
        }
    }

    private static void CheckConnectivity(AreaDefinition area, List<AuthoringError> errors)
    {
        var entrance = area.Nodes.First(n => n.Kind == MarkerKinds.Entrance).Id;
        var reached = new HashSet<string>(StringComparer.Ordinal) { entrance };
        var queue = new Queue<string>();
        queue.Enqueue(entrance);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var edge in area.Edges.Where(e => e.Kind == EdgeKind.Path))
            {
                var next = edge.From == current ? edge.To : edge.To == current ? edge.From : null;
                if (next is not null && reached.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }
        foreach (var node in area.Nodes.Where(n => !reached.Contains(n.Id)))
        {
            errors.Add(new AuthoringError(area.Source, $"node '{node.Id}' is not connected to the entrance by path edges"));
        }
    }
}
