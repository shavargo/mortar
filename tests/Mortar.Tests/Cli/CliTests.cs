using System.Buffers.Binary;
using Mortar.Cli;
using Mortar.Core.Layouts;
using Mortar.Json;
using Mortar.Tests.Support;

namespace Mortar.Tests.Cli;

public class CliTests
{
    private static (int Code, string Output, string Error) Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var code = CliApp.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }

    [Fact]
    public void Generate_WritesReadableLayout()
    {
        using var temp = new TempDir();
        var outPath = Path.Combine(temp.Root, "layout.json");
        var (code, output, _) = Run("generate", "--area", TestPaths.CryptArea, "--seed", "1", "--out", outPath);
        Assert.Equal(ExitCodes.Success, code);
        Assert.Contains("fingerprint", output);
        Assert.True(LayoutReader.FromFile(outPath).Succeeded);
    }

    [Fact]
    public void Generate_CreatesMissingOutputFolder()
    {
        using var temp = new TempDir();
        var outPath = Path.Combine(temp.Root, "nested", "deeper", "layout.json");
        Assert.Equal(ExitCodes.Success, Run("generate", "--area", TestPaths.CryptArea, "--seed", "1", "--out", outPath).Code);
        Assert.True(File.Exists(outPath));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("4294967296")]
    public void Generate_RejectsBadSeeds(string seed)
    {
        var (code, _, error) = Run("generate", "--area", TestPaths.CryptArea, "--seed", seed, "--out", "unused.json");
        Assert.Equal(ExitCodes.InvalidInput, code);
        Assert.Contains("--seed must be a whole number", error);
    }

    [Fact]
    public void Generate_RequiresSeed()
    {
        var (code, _, error) = Run("generate", "--area", TestPaths.CryptArea, "--out", "unused.json");
        Assert.Equal(ExitCodes.InvalidInput, code);
        Assert.Contains("missing --seed", error);
    }

    [Theory]
    [InlineData("--area", "")]
    [InlineData("--out", "")]
    public void Generate_EmptyPath_ExitsTwo(string option, string value)
    {
        using var temp = new TempDir();
        var args = new List<string> { "generate", "--area", TestPaths.CryptArea, "--seed", "1", "--out", Path.Combine(temp.Root, "out.json") };
        args[args.IndexOf(option) + 1] = value;
        var (code, _, error) = Run(args.ToArray());
        Assert.Equal(ExitCodes.InvalidInput, code);
        Assert.Contains("invalid path", error);
    }

    [Fact]
    public void Preview_TooLargeImage_ExitsTwo()
    {
        using var temp = new TempDir();
        var layoutPath = temp.Write("wide.json", LayoutWriter.ToJson(WideLayout(4096, 64)));
        var (code, _, error) = Run("preview", "--layout", layoutPath, "--out", Path.Combine(temp.Root, "map.png"), "--scale", "64");
        Assert.Equal(ExitCodes.InvalidInput, code);
        Assert.Contains("image too large", error);
    }

    private static Layout WideLayout(int width, int height)
    {
        var count = width * height;
        var cells = new LayoutCells(width, height, new[] { "void" }, new int[count], new[] { "void" }, new int[count], new int[count], new bool[count]);
        return new Layout("wide", 0, 0, cells, Array.Empty<Marker>(), "0.1.0", Layout.CurrentFormatVersion);
    }

    [Fact]
    public void Generate_RequiresOut()
    {
        var (code, _, error) = Run("generate", "--area", TestPaths.CryptArea, "--seed", "1");
        Assert.Equal(ExitCodes.InvalidInput, code);
        Assert.Contains("missing --out", error);
    }

    [Fact]
    public void Generate_ReportsAuthoringErrors()
    {
        using var temp = new TempDir();
        var missing = Path.Combine(temp.Root, "missing.json");
        var (code, _, error) = Run("generate", "--area", missing, "--seed", "1", "--out", Path.Combine(temp.Root, "out.json"));
        Assert.Equal(ExitCodes.InvalidInput, code);
        Assert.Contains("missing.json", error);
    }

    [Fact]
    public void Generate_ImpossibleArea_ExitsOneWithoutOutput()
    {
        using var temp = new TempDir();
        var area = temp.Write("area.json", SampleArea.AreaJson(SampleArea.Tileset, SampleArea.Rooms, tolerance: 0));
        var outPath = Path.Combine(temp.Root, "layout.json");
        var (code, _, error) = Run("generate", "--area", area, "--seed", "1", "--out", outPath);
        Assert.Equal(ExitCodes.GenerationFailed, code);
        Assert.Contains("generation failed after 5 attempts: too small: ", error);
        Assert.False(File.Exists(outPath));
    }

    [Fact]
    public void Preview_WritesPngAtScale()
    {
        using var temp = new TempDir();
        var layoutPath = Path.Combine(temp.Root, "layout.json");
        var pngPath = Path.Combine(temp.Root, "map.png");
        Run("generate", "--area", TestPaths.CryptArea, "--seed", "1", "--out", layoutPath);
        Assert.Equal(ExitCodes.Success, Run("preview", "--layout", layoutPath, "--out", pngPath).Code);
        var png = File.ReadAllBytes(pngPath);
        Assert.Equal(57 * 8, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)));
    }

    [Fact]
    public void Preview_RejectsBadScale()
    {
        var (code, _, error) = Run("preview", "--layout", "layout.json", "--out", "map.png", "--scale", "0");
        Assert.Equal(ExitCodes.InvalidInput, code);
        Assert.Contains("--scale must be a whole number", error);
    }

    [Fact]
    public void UnknownCommand_ExitsTwo()
    {
        var (code, _, error) = Run("stats");
        Assert.Equal(ExitCodes.InvalidInput, code);
        Assert.Contains("unknown command 'stats'", error);
    }

    [Fact]
    public void NoArguments_PrintsUsage()
    {
        var (code, _, error) = Run();
        Assert.Equal(ExitCodes.InvalidInput, code);
        Assert.Contains("Usage:", error);
    }
}
