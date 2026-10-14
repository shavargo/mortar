namespace Mortar.Tests.Support;

internal static class Snapshots
{
    /// <summary>A missing snapshot is written and the test fails, so every new snapshot gets reviewed once.</summary>
    public static void AssertMatches(string fileName, string actual)
    {
        var path = Path.Combine(TestPaths.Root, "tests", "Mortar.Tests", "Snapshots", fileName);
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual);
            Assert.Fail($"Snapshot created at {path}; review it, then run the tests again.");
        }
        Assert.Equal(File.ReadAllText(path).Replace("\r\n", "\n"), actual);
    }
}
