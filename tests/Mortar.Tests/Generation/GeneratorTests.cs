using Mortar.Core.Generation;
using Mortar.Core.Model;
using Mortar.Tests.Support;

namespace Mortar.Tests.Generation;

public class GeneratorTests
{
    private static readonly CompiledArea Crypt = SampleArea.LoadCrypt();

    [Fact]
    public void Generate_Sample_Succeeds()
    {
        var result = Generator.Generate(Crypt, 1);
        Assert.True(result.Succeeded, result.FailureSummary);
        var layout = result.Layout!;
        Assert.Equal(("crypt", 1u, 57, 57), (layout.AreaId, layout.Seed, layout.Width, layout.Height));
        Assert.Equal(result.Attempts, layout.Attempt + 1);
    }

    [Fact]
    public void Generate_SameSeed_SameFingerprint() =>
        Assert.Equal(Generator.Generate(Crypt, 42).Layout?.Fingerprint, Generator.Generate(Crypt, 42).Layout?.Fingerprint);

    [Fact]
    public void Generate_DifferentSeeds_DifferentFingerprints() =>
        Assert.NotEqual(Generator.Generate(Crypt, 1).Layout?.Fingerprint, Generator.Generate(Crypt, 2).Layout?.Fingerprint);

    [Theory]
    [InlineData(0u)]
    [InlineData(uint.MaxValue)]
    public void Generate_SeedEdgeValues_AreDeterministic(uint seed)
    {
        var first = Generator.Generate(Crypt, seed);
        var second = Generator.Generate(Crypt, seed);
        Assert.Equal(first.Succeeded, second.Succeeded);
        Assert.Equal(first.Layout?.Fingerprint, second.Layout?.Fingerprint);
    }

    [Fact]
    public void RoomOrder_DoesNotAffectOutput()
    {
        var rooms = TestContent.CryptRooms();
        var forward = Generator.Generate(TestContent.CompileCrypt(rooms: rooms), 9);
        var reversed = Generator.Generate(TestContent.CompileCrypt(rooms: Enumerable.Reverse(rooms).ToList()), 9);
        Assert.NotNull(forward.Layout);
        Assert.Equal(forward.Layout!.Fingerprint, reversed.Layout?.Fingerprint);
    }

    [Fact]
    public void Generate_ImpossibleArea_ReportsCountedFailures()
    {
        var area = TestContent.CompileCrypt(TestContent.CryptDefinition(tolerance: 0, maxAttempts: 5));
        var result = Generator.Generate(area, 1);
        Assert.False(result.Succeeded);
        Assert.Equal(5, result.Attempts);
        Assert.Equal(5, result.Failures.Sum(f => f.Count));
        Assert.Equal("too small", result.Failures[0].Reason);
        Assert.StartsWith("too small: ", result.FailureSummary);
    }
}
