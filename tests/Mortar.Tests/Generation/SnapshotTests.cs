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
        var path = Path.Combine(TestPaths.Root, "tests", "Mortar.Tests", "Snapshots", $"crypt-seed-{seed}.txt");
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual);
            Assert.Fail($"Snapshot created at {path}; review it, then run the tests again.");
        }
        Assert.Equal(File.ReadAllText(path).Replace("\r\n", "\n"), actual);
    }
}
