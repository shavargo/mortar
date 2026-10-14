namespace Mortar.Cli;

internal static class OutputFile
{
    public static bool TryWrite(string path, byte[] contents, TextWriter error)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllBytes(fullPath, contents);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            error.WriteLine($"error: '{path}' is an invalid path");
            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"error: cannot write {path}: {ex.Message}");
            return false;
        }
    }
}
