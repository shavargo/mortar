using Mortar.Core.Layouts;
using Mortar.Core.Rng;

namespace Mortar.Cli.Preview;

internal static class MapRenderer
{
    private static readonly Dictionary<string, Rgb> GroundColors = new()
    {
        ["void"] = new Rgb(18, 18, 18),
        ["floor"] = new Rgb(196, 186, 166),
        ["wall"] = new Rgb(72, 66, 60),
        ["water"] = new Rgb(58, 108, 196),
        ["cliff_high"] = new Rgb(122, 100, 78),
    };

    private static readonly Dictionary<string, Rgb> MarkerColors = new()
    {
        ["entrance"] = new Rgb(46, 204, 64),
        ["exit"] = new Rgb(0, 200, 220),
        ["boss"] = new Rgb(230, 40, 40),
        ["waypoint"] = new Rgb(255, 214, 0),
        ["spawn"] = new Rgb(255, 133, 27),
        ["chest"] = new Rgb(177, 13, 201),
    };

    private static readonly Rgb OtherMarker = new Rgb(255, 255, 255);

    public static RgbImage Render(Layout layout, int scale)
    {
        var image = new RgbImage(layout.Width * scale, layout.Height * scale);
        for (var y = 0; y < layout.Height; y++)
        {
            for (var x = 0; x < layout.Width; x++)
            {
                image.FillRect(x * scale, y * scale, scale, scale, GroundColor(layout.Cells.GroundAt(x, y)));
            }
        }
        var inset = scale / 4;
        var size = scale - 2 * inset;
        foreach (var marker in layout.Markers)
        {
            var color = MarkerColors.TryGetValue(marker.Kind, out var known) ? known : OtherMarker;
            image.FillRect(marker.X * scale + inset, marker.Y * scale + inset, size, size, color);
        }
        return image;
    }

    internal static Rgb GroundColor(string groundId)
    {
        if (GroundColors.TryGetValue(groundId, out var color))
        {
            return color;
        }
        var hash = Hashing.Fnv1a64(groundId);
        return new Rgb((byte)hash, (byte)(hash >> 8), (byte)(hash >> 16));
    }
}
