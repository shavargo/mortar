using System.Globalization;
using System.Text;
using Mortar.Core.Generation;
using Mortar.Json;

namespace Mortar.Cli;

internal static class GenerateCommand
{
    public static int Run(CliOptions options, TextWriter output, TextWriter error)
    {
        var areaPath = options.Require("area", error);
        var seedText = options.Require("seed", error);
        var outPath = options.Require("out", error);
        if (areaPath is null || seedText is null || outPath is null) return ExitCodes.InvalidInput;
        if (!uint.TryParse(seedText, NumberStyles.None, CultureInfo.InvariantCulture, out var seed))
        {
            error.WriteLine($"error: --seed must be a whole number from 0 to {uint.MaxValue}");
            return ExitCodes.InvalidInput;
        }
        var area = MortarLoader.LoadArea(areaPath);
        if (!area.Succeeded)
        {
            foreach (var problem in area.Errors) error.WriteLine($"error: {problem}");
            return ExitCodes.InvalidInput;
        }
        var result = Generator.Generate(area.Value!, seed);
        if (!result.Succeeded)
        {
            error.WriteLine($"generation failed after {result.Attempts} attempts: {result.FailureSummary}");
            return ExitCodes.GenerationFailed;
        }
        var layout = result.Layout!;
        if (!OutputFile.TryWrite(outPath, Encoding.UTF8.GetBytes(LayoutWriter.ToJson(layout)), error))
        {
            return ExitCodes.InvalidInput;
        }
        output.WriteLine($"wrote {outPath} (attempt {layout.Attempt}, fingerprint {layout.Fingerprint})");
        return ExitCodes.Success;
    }
}
