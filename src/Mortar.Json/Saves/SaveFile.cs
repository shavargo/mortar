using System.Text.Json;
using System.Text.Json.Serialization;
using Mortar.Game.Content;
using Mortar.Game.Saves;
using Mortar.Game.Simulation;

namespace Mortar.Json.Saves;

public static class SaveFile
{
    public const long MaxBytes = 16L * 1024 * 1024;

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static string ToJson(World world) => JsonSerializer.Serialize(WorldSnapshot.Capture(world), Options);

    public static WorldResult FromJson(string json, ContentSet content, ILevelGenerator generator)
    {
        if (string.IsNullOrWhiteSpace(json)) return WorldResult.Failure("save is empty");
        var versionError = CheckVersion(json);
        if (versionError is not null) return WorldResult.Failure(versionError);
        WorldSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<WorldSnapshot>(json, Options);
        }
        catch (JsonException ex)
        {
            return WorldResult.Failure($"save is not valid: {ex.Message}");
        }
        return snapshot is null ? WorldResult.Failure("save is empty") : World.Restore(snapshot, content, generator);
    }

    public static string? Write(string path, World world)
    {
        var target = SafePath.Resolve(null, path);
        if (target is null) return $"{path}: invalid path";
        var temporary = target + ".tmp";
        try
        {
            File.WriteAllText(temporary, ToJson(world));
            if (File.Exists(target)) File.Replace(temporary, target, null);
            else File.Move(temporary, target);
            return null;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return $"{target}: cannot write save: {ex.Message}";
        }
    }

    public static WorldResult Read(string path, ContentSet content, ILevelGenerator generator)
    {
        var source = SafePath.Resolve(null, path);
        if (source is null) return WorldResult.Failure($"{path}: invalid path");
        try
        {
            var info = new FileInfo(source);
            if (!info.Exists) return WorldResult.Failure($"{source}: file not found");
            if (info.Length > MaxBytes) return WorldResult.Failure($"{source}: save is larger than {MaxBytes} bytes");
            var result = FromJson(File.ReadAllText(source), content, generator);
            return result.Succeeded ? result : WorldResult.Failure($"{source}: {result.Error}");
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return WorldResult.Failure($"{source}: cannot read save: {ex.Message}");
        }
    }

    private static string? CheckVersion(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return "save must be a JSON object";
            var version = FindVersion(root);
            if (version is null
                || version.Value.ValueKind != JsonValueKind.Number
                || !version.Value.TryGetInt32(out var number))
            {
                return "save has no version number";
            }
            return number == WorldSnapshot.CurrentVersion || number == 1 || number == 2
                ? null
                : $"unsupported save version {number}; this build reads version {WorldSnapshot.CurrentVersion}";
        }
        catch (JsonException ex)
        {
            return $"save is not valid JSON: {ex.Message}";
        }
    }

    private static JsonElement? FindVersion(JsonElement root)
    {
        JsonElement? found = null;
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, "version", StringComparison.OrdinalIgnoreCase)) found = property.Value;
        }
        return found;
    }
}
