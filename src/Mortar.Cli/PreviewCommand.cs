using System.Globalization;
using Mortar.Cli.Preview;
using Mortar.Json;

namespace Mortar.Cli;

internal static class PreviewCommand
{
    private const int DefaultScale = 8;
    private const int MaxScale = 64;

    public static int Run(CliOptions options, TextWriter output, TextWriter error)
    {
        var layoutPath = options.Require("layout", error);
        var outPath = options.Require("out", error);
        if (layoutPath is null || outPath is null) return ExitCodes.InvalidInput;
        var scale = ParseScale(options.Get("scale"), error);
        if (scale is null) return ExitCodes.InvalidInput;
        var layout = LayoutReader.FromFile(layoutPath);
        if (!layout.Succeeded)
        {
            foreach (var problem in layout.Errors) error.WriteLine($"error: {problem}");
            return ExitCodes.InvalidInput;
        }
        var image = MapRenderer.Render(layout.Value!, scale.Value);
        if (!OutputFile.TryWrite(outPath, PngEncoder.EncodeRgb(image.Width, image.Height, image.Pixels), error))
        {
            return ExitCodes.InvalidInput;
        }
        output.WriteLine($"wrote {outPath} ({image.Width}x{image.Height} px)");
        return ExitCodes.Success;
    }

    private static int? ParseScale(string? text, TextWriter error)
    {
        if (text is null) return DefaultScale;
        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var scale) && scale >= 1 && scale <= MaxScale)
        {
            return scale;
        }
        error.WriteLine($"error: --scale must be a whole number from 1 to {MaxScale}");
        return null;
    }
}
