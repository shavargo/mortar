using System.Text;
using Mortar.Core.Layouts;
using Mortar.Core.Model;

namespace Mortar.Tests.Support;

internal static class AsciiMap
{
    public static string Render(Layout layout, Tileset tileset)
    {
        var rows = new char[layout.Height][];
        for (var y = 0; y < layout.Height; y++)
        {
            rows[y] = new char[layout.Width];
            for (var x = 0; x < layout.Width; x++)
            {
                var symbol = tileset.Grounds[tileset.IndexOf(layout.Cells.GroundAt(x, y))].Symbol;
                rows[y][x] = symbol == ' ' ? '_' : symbol;
            }
        }
        foreach (var marker in layout.Markers)
        {
            rows[marker.Y][marker.X] = tileset.Markers.FirstOrDefault(type => type.Kind == marker.Kind)?.Symbol ?? '?';
        }
        var text = new StringBuilder();
        text.Append("fingerprint: ").Append(layout.Fingerprint).Append('\n');
        foreach (var row in rows)
        {
            text.Append(row).Append('\n');
        }
        return text.ToString();
    }
}
