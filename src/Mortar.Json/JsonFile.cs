using System.Text.Json;
using System.Text.Json.Serialization;
using Mortar.Core.Authoring;

namespace Mortar.Json;

internal static class JsonFile
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static T? Read<T>(string path, out AuthoringError? error)
        where T : class
    {
        error = null;
        try
        {
            var value = JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
            if (value is null) error = new AuthoringError(path, "file contains null");
            return value;
        }
        catch (JsonException ex)
        {
            error = new AuthoringError(path, $"invalid JSON: {ex.Message}");
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            error = new AuthoringError(path, $"cannot read file: {ex.Message}");
        }
        return null;
    }
}
