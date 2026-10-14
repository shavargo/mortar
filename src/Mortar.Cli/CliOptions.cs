namespace Mortar.Cli;

internal sealed class CliOptions
{
    private readonly Dictionary<string, string> _values;

    private CliOptions(Dictionary<string, string> values)
    {
        _values = values;
    }

    public static CliOptions? Parse(IReadOnlyList<string> args, int start, TextWriter error)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = start; i < args.Count; i += 2)
        {
            var name = args[i];
            if (!name.StartsWith("--", StringComparison.Ordinal) || i + 1 >= args.Count)
            {
                error.WriteLine($"error: expected '--option value' but got '{name}'");
                return null;
            }
            values[name[2..]] = args[i + 1];
        }
        return new CliOptions(values);
    }

    public string? Get(string name) => _values.TryGetValue(name, out var value) ? value : null;

    public string? Require(string name, TextWriter error)
    {
        var value = Get(name);
        if (value is null)
        {
            error.WriteLine($"error: missing --{name}");
        }
        return value;
    }
}
