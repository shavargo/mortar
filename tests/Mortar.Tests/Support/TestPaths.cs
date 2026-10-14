namespace Mortar.Tests.Support;

internal static class TestPaths
{
    public static string Root { get; } = FindRoot();

    public static string Samples => Path.Combine(Root, "samples");

    public static string CryptArea => Path.Combine(Samples, "areas", "crypt.json");

    public static string Content => Path.Combine(Root, "content");

    public static string ContentArea(string name) => Path.Combine(Content, "areas", $"{name}.json");

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Mortar.slnx")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException("Mortar.slnx not found above the test output folder");
    }
}
