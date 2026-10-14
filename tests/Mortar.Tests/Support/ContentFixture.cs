namespace Mortar.Tests.Support;

internal static class ContentFixture
{
    public static string Manifest => Path.Combine(TestPaths.Root, "content", "game.json");

    /// <summary>Copies the repo's content folder into <paramref name="temp"/> and returns the copied manifest path.</summary>
    public static string Copy(TempDir temp)
    {
        var source = Path.Combine(TestPaths.Root, "content");
        foreach (var file in Directory.GetFiles(source, "*.json", SearchOption.AllDirectories))
        {
            temp.Write(Path.GetRelativePath(source, file), File.ReadAllText(file));
        }
        return Path.Combine(temp.Root, "game.json");
    }
}
