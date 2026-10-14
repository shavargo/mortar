using System.Text;
using System.Text.Json;
using Mortar.Core.Layouts;

namespace Mortar.Json;

public static class LayoutWriter
{
    public static string ToJson(Layout layout)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            WriteHeader(writer, layout);
            WriteCells(writer, layout.Cells);
            WriteMarkers(writer, layout.Markers);
            writer.WriteString("fingerprint", layout.Fingerprint);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteHeader(Utf8JsonWriter writer, Layout layout)
    {
        writer.WriteNumber("formatVersion", layout.FormatVersion);
        writer.WriteString("mortarVersion", layout.MortarVersion);
        writer.WriteString("areaId", layout.AreaId);
        writer.WriteNumber("seed", layout.Seed);
        writer.WriteNumber("attempt", layout.Attempt);
        writer.WriteNumber("width", layout.Width);
        writer.WriteNumber("height", layout.Height);
    }

    private static void WriteCells(Utf8JsonWriter writer, LayoutCells cells)
    {
        WriteStrings(writer, "groundTypes", cells.GroundTypes);
        WriteStrings(writer, "tileTypes", cells.TileTypes);
        writer.WriteStartObject("cells");
        WriteInts(writer, "ground", cells.Ground);
        WriteInts(writer, "tile", cells.Tiles);
        WriteInts(writer, "variant", cells.Variants);
        writer.WriteEndObject();
        WriteInts(writer, "walkable", cells.Walkable.Select(walkable => walkable ? 1 : 0));
    }

    private static void WriteMarkers(Utf8JsonWriter writer, IReadOnlyList<Marker> markers)
    {
        writer.WriteStartArray("markers");
        foreach (var marker in markers)
        {
            writer.WriteStartObject();
            writer.WriteString("kind", marker.Kind);
            if (marker.NodeId is not null) writer.WriteString("node", marker.NodeId);
            writer.WriteNumber("x", marker.X);
            writer.WriteNumber("y", marker.Y);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteStrings(Utf8JsonWriter writer, string name, IEnumerable<string> values)
    {
        writer.WriteStartArray(name);
        foreach (var value in values) writer.WriteStringValue(value);
        writer.WriteEndArray();
    }

    private static void WriteInts(Utf8JsonWriter writer, string name, IEnumerable<int> values)
    {
        writer.WriteStartArray(name);
        foreach (var value in values) writer.WriteNumberValue(value);
        writer.WriteEndArray();
    }
}
