using System.Text.Json;
using Mortar.Core.Authoring;
using Mortar.Core.Layouts;

namespace Mortar.Json;

public static class LayoutReader
{
    public static AuthoringResult<Layout> FromFile(string path)
    {
        var source = Path.GetFullPath(path);
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
        var cells = ReadCells(root);
        var problem = CheckCells(cells);
        if (problem is not null)
        {
            return AuthoringResult<Layout>.Failure(source, $"malformed layout: {problem}");
        }
        var layout = new Layout(
            root.GetProperty("areaId").GetString() ?? "",
            root.GetProperty("seed").GetUInt32(),
            root.GetProperty("attempt").GetInt32(),
            cells,
            ReadMarkers(root),
            root.GetProperty("mortarVersion").GetString() ?? "",
            formatVersion);
        var stored = root.GetProperty("fingerprint").GetString();
        return stored == layout.Fingerprint
            ? AuthoringResult<Layout>.Success(layout)
            : AuthoringResult<Layout>.Failure(source, $"fingerprint mismatch: file has {stored}, contents hash to {layout.Fingerprint}");
    }

    private static LayoutCells ReadCells(JsonElement root)
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
            Ints(root.GetProperty("walkable")).Select(value => value != 0).ToArray());
    }

    private static string? CheckCells(LayoutCells cells)
    {
        if (cells.Width <= 0 || cells.Height <= 0) return "width and height must be positive";
        var count = cells.Width * cells.Height;
        if (cells.Ground.Count != count || cells.Tiles.Count != count || cells.Variants.Count != count || cells.Walkable.Count != count)
            return $"cell arrays must each hold width x height = {count} entries";
        if (cells.Ground.Any(g => g < 0 || g >= cells.GroundTypes.Count)) return "ground index out of range";
        if (cells.Tiles.Any(t => t < 0 || t >= cells.TileTypes.Count)) return "tile index out of range";
        return null;
    }

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
