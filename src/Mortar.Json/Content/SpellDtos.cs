namespace Mortar.Json.Content;

internal sealed class SpellDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Shape { get; set; }

    public int? ManaCost { get; set; }

    public int? CastTicks { get; set; }

    public int? HitFrame { get; set; }

    public int? Range { get; set; }

    public RangeDto? Damage { get; set; }

    public int? HealAmount { get; set; }

    public int? Radius { get; set; }

    public int? DamagePerLevel { get; set; }

    public int? HealPerLevel { get; set; }
}
