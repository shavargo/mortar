using System.Text.Json;
using Mortar.Core.Authoring;
using Mortar.Core.Layouts;

namespace Mortar.Json;

public static class LayoutReader
{
    public static AuthoringResult<Layout> FromFile(string path)
    {
        var source = SafePath.Resolve(null, path);
        if (source is null)
        {
            return AuthoringResult<Layout>.Failure(path, "invalid path");
        }
        try
        {
            return FromJson(File.ReadAllText(source), source);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return AuthoringResult<Layout>.Failure(source, $"cannot read file: {ex.Message}");
        }
    }

    public static AuthoringResult<Layout> FromJson(string json, string source = "layout")
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return Map(document.RootElement, source);
        }
        catch (JsonException ex)
        {
            return AuthoringResult<Layout>.Failure(source, $"invalid JSON: {ex.Message}");
        }
        catch (Exception ex) when (ex is KeyNotFoundException || ex is InvalidOperationException || ex is FormatException)
        {
            return AuthoringResult<Layout>.Failure(source, $"malformed layout: {ex.Message}");
        }
    }

    private static AuthoringResult<Layout> Map(JsonElement root, string source)
    {
        var formatVersion = root.GetProperty("formatVersion").GetInt32();
        if (formatVersion != Layout.CurrentFormatVersion)
        {
            return AuthoringResult<Layout>.Failure(
                source,
                $"unsupported formatVersion {formatVersion}; this Mortar reads {Layout.CurrentFormatVersion}");
        }
        var walkable = Ints(root.GetProperty("walkable"));
        var cells = ReadCells(root, walkable);
        var markers = ReadMarkers(root);
        var problem = CheckCells(cells, walkable) ?? CheckMarkers(cells, markers);
        if (problem is not null)
        {
            return AuthoringResult<Layout>.Failure(source, $"malformed layout: {problem}");
        }
        var layout = new Layout(
            root.GetProperty("areaId").GetString() ?? "",
            root.GetProperty("seed").GetUInt32(),
            root.GetProperty("attempt").GetInt32(),
            cells,
            markers,
            root.GetProperty("mortarVersion").GetString() ?? "",
            formatVersion);
        var stored = root.GetProperty("fingerprint").GetString();
        return stored == layout.Fingerprint
            ? AuthoringResult<Layout>.Success(layout)
            : AuthoringResult<Layout>.Failure(source, $"fingerprint mismatch: file has {stored}, contents hash to {layout.Fingerprint}");
    }

    private static LayoutCells ReadCells(JsonElement root, int[] walkable)
    {
        var cells = root.GetProperty("cells");
        return new LayoutCells(
            root.GetProperty("width").GetInt32(),
            root.GetProperty("height").GetInt32(),
            Strings(root.GetProperty("groundTypes")),
            Ints(cells.GetProperty("ground")),
            Strings(root.GetProperty("tileTypes")),
            Ints(cells.GetProperty("tile")),
            Ints(cells.GetProperty("variant")),
            walkable.Select(value => value != 0).ToArray());
    }

    private static string? CheckCells(LayoutCells cells, int[] walkable)
    {
        if (!InRange(cells.Width, 1, Layout.MaxDimension + 1) || !InRange(cells.Height, 1, Layout.MaxDimension + 1))
            return $"width and height must be between 1 and {Layout.MaxDimension}";
        var count = cells.Width * cells.Height;
        if (cells.Ground.Count != count || cells.Tiles.Count != count || cells.Variants.Count != count || walkable.Length != count)
            return $"cell arrays must each hold width x height = {count} entries";
        if (cells.Ground.Any(g => !InRange(g, 0, cells.GroundTypes.Count))) return "ground index out of range";
        if (cells.Tiles.Any(t => !InRange(t, 0, cells.TileTypes.Count))) return "tile index out of range";
        if (cells.Variants.Any(v => v < 0)) return "variant must not be negative";
        if (walkable.Any(w => w != 0 && w != 1)) return "walkable entries must be 0 or 1";
        return null;
    }

    private static string? CheckMarkers(LayoutCells cells, IReadOnlyList<Marker> markers)
    {
        for (var i = 0; i < markers.Count; i++)
        {
            if (!InRange(markers[i].X, 0, cells.Width) || !InRange(markers[i].Y, 0, cells.Height))
                return $"marker {i} lies outside the grid";
        }
        return null;
    }

    private static bool InRange(int value, int minInclusive, int maxExclusive) =>
        value >= minInclusive && value < maxExclusive;

    private static List<Marker> ReadMarkers(JsonElement root) =>
        root.GetProperty("markers").EnumerateArray()
            .Select(m => new Marker(
                m.GetProperty("kind").GetString() ?? "",
                m.TryGetProperty("node", out var node) ? node.GetString() : null,
                m.GetProperty("x").GetInt32(),
                m.GetProperty("y").GetInt32()))
            .ToList();

    private static string[] Strings(JsonElement array) =>
        array.EnumerateArray().Select(element => element.GetString() ?? "").ToArray();

    private static int[] Ints(JsonElement array) =>
        array.EnumerateArray().Select(element => element.GetInt32()).ToArray();
}
