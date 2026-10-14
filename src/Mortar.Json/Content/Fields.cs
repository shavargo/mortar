using Mortar.Core.Authoring;
using Mortar.Game.Content;

namespace Mortar.Json.Content;

internal sealed class Fields
{
    private readonly string _source;
    private readonly string _prefix;
    private readonly List<AuthoringError> _errors;

    public Fields(string source, List<AuthoringError> errors, string context = "")
    {
        _source = source;
        _errors = errors;
        _prefix = context.Length == 0 ? "" : context + " ";
    }

    public bool Present(object? value, string name)
    {
        if (value is null) Missing(name);
        return value is not null;
    }

    public int Int(int? value, string name)
    {
        Present(value, name);
        return value ?? 0;
    }

    public string Text(string? value, string name)
    {
        if (!string.IsNullOrWhiteSpace(value)) return value!;
        Missing(name);
        return "";
    }

    public IntRange ReadRange(RangeDto? dto, string name) =>
        Present(dto, name) ? new IntRange(Int(dto!.Min, name + ".min"), Int(dto.Max, name + ".max")) : default;

    public Stats ReadStats(StatsDto? dto, string name) =>
        Present(dto, name)
            ? new Stats(
                Int(dto!.Strength, name + ".strength"),
                Int(dto.Dexterity, name + ".dexterity"),
                Int(dto.Vitality, name + ".vitality"),
                Int(dto.Magic, name + ".magic"))
            : default;

    public ActionTimings ReadTimings(TimingsDto? dto) =>
        Present(dto, "timings")
            ? new ActionTimings(
                Int(dto!.MoveTicks, "timings.moveTicks"),
                Int(dto.AttackTicks, "timings.attackTicks"),
                Int(dto.HitFrame, "timings.hitFrame"),
                Int(dto.RecoveryTicks, "timings.recoveryTicks"))
            : new ActionTimings(0, 0, 0, 0);

    private void Missing(string name) => _errors.Add(new AuthoringError(_source, $"{_prefix}'{name}' is required"));
}
