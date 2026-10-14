using Mortar.Core.Generation;
using Mortar.Tests.Support;

namespace Mortar.Tests.Generation;

public class SnapshotTests
{
    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(3u)]
    public void SampleCrypt_MatchesSnapshot(uint seed)
    {
        var area = SampleArea.LoadCrypt();
        var result = Generator.Generate(area, seed);
        Assert.True(result.Succeeded, result.FailureSummary);
        var actual = AsciiMap.Render(result.Layout!, area.Tileset);
        Snapshots.AssertMatches($"crypt-seed-{seed}.txt", actual);
    }
}
