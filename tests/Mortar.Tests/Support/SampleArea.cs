using Mortar.Core.Model;
using Mortar.Json;

namespace Mortar.Tests.Support;

internal static class SampleArea
{
    public static string Tileset => JsonPath(Path.Combine(TestPaths.Samples, "tileset.json"));

    public static string Rooms => JsonPath(Path.Combine(TestPaths.Samples, "rooms"));

    public static CompiledArea LoadCrypt()
    {
        var result = MortarLoader.LoadArea(TestPaths.CryptArea);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        return result.Value!;
    }

    public static string AreaJson(string tileset, string rooms, string mode = "indoor", int tolerance = 15, string extra = "") => $$"""
        {
          "id": "temp",
          "mode": "{{mode}}",
          "tileset": "{{tileset}}",
          "targetSize": { "width": 60, "height": 60, "tolerance": {{tolerance}} },
          "slotSize": 9,
          "rooms": ["{{rooms}}"],
          "maxSideBranches": 4,
          "maxAttempts": 5,{{extra}}
          "graph": {
            "nodes": [
              { "id": "entrance", "region": { "x": [0.0, 0.1], "y": [0.3, 0.7] } },
              { "id": "waypoint", "region": { "x": [0.3, 0.7], "y": [0.0, 0.1] } },
              { "id": "boss", "region": { "x": [0.9, 1.0], "y": [0.3, 0.7] } },
              { "id": "exit", "region": { "x": [0.3, 0.7], "y": [0.9, 1.0] } }
            ],
            "edges": [
              { "from": "entrance", "to": "waypoint", "kind": "path" },
              { "from": "waypoint", "to": "boss", "kind": "path" },
              { "from": "boss", "to": "exit", "kind": "path" }
            ]
          }
        }
        """;

    private static string JsonPath(string path) => path.Replace('\\', '/');
}
