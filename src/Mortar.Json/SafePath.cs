namespace Mortar.Json;

internal static class SafePath
{
    public static string? Resolve(string? baseDirectory, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }
        try
        {
            return Path.GetFullPath(baseDirectory is null ? path : Path.Combine(baseDirectory, path));
        }
        catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
        {
            return null;
        }
    }
}
