using Mortar.Core.Authoring;
using Mortar.Core.Model;

namespace Mortar.Tests.Support;

internal static partial class TestContent
{
    public static GraphNode[] CryptNodes() => new[]
    {
        new GraphNode("entrance", "entrance", new FractionRect(0.0, 0.1, 0.3, 0.7)),
        new GraphNode("waypoint", "waypoint", new FractionRect(0.3, 0.7, 0.0, 0.1)),
        new GraphNode("boss", "boss", new FractionRect(0.9, 1.0, 0.3, 0.7)),
        new GraphNode("exit", "exit", new FractionRect(0.3, 0.7, 0.9, 1.0)),
    };

    public static GraphEdge[] CryptEdges() => new[]
    {
        new GraphEdge("entrance", "waypoint", EdgeKind.Path),
        new GraphEdge("waypoint", "boss", EdgeKind.Path),
        new GraphEdge("boss", "exit", EdgeKind.Path),
        new GraphEdge("entrance", "exit", EdgeKind.Branch),
    };

    public static AreaDefinition CryptDefinition(
        AreaMode mode = AreaMode.Indoor,
        GraphNode[]? nodes = null,
        GraphEdge[]? edges = null,
        int size = 60,
        int tolerance = 15,
        int maxAttempts = 20,
        string[]? roomTags = null,
        int maxSideBranches = 4) =>
        new AreaDefinition(
            "crypt.json",
            "crypt",
            mode,
            new TargetSize(size, size, tolerance),
            9,
            maxSideBranches,
            maxAttempts,
            roomTags ?? Array.Empty<string>(),
            nodes ?? CryptNodes(),
            edges ?? CryptEdges());

    public static List<RoomDefinition> CryptRooms() => new List<RoomDefinition>
    {
        RoomDef("entrance", new[] { "entrance" }, Box(9, 9, new[] { 4 }, (4, 4, 'E'))),
        RoomDef("exit", new[] { "exit" }, Box(9, 9, new[] { 4 }, (4, 4, 'X'))),
        RoomDef("waypoint", new[] { "waypoint" }, Box(9, 9, new[] { 4 }, (4, 4, 'W'))),
        RoomDef("boss", new[] { "boss" }, Box(17, 17, new[] { 4, 12 }, (8, 8, 'B'))),
        RoomDef("hall", new[] { "filler" }, Box(9, 9, new[] { 4 }, (4, 4, 'S'))),
        RoomDef("alcove", new[] { "dead_end" }, Box(9, 9, new[] { 4 }, (4, 4, 'C'))),
    };

    public static CompiledArea CompileCrypt(AreaDefinition? definition = null, List<RoomDefinition>? rooms = null)
    {
        var result = AreaCompiler.Compile(definition ?? CryptDefinition(), CreateTileset(), rooms ?? CryptRooms());
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        return result.Value!;
    }
}
