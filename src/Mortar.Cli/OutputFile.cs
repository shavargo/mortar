namespace Mortar.Cli;

internal static class OutputFile
{
    public static bool TryWrite(string path, byte[] contents, TextWriter error)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllBytes(fullPath, contents);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"error: cannot write {path}: {ex.Message}");
            return false;
        }
    }
}
