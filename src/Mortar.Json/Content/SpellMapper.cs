using Mortar.Core.Authoring;
using Mortar.Game.Content;

namespace Mortar.Json.Content;

internal static class SpellMapper
{
    public static SpellDefinition ToSpell(string source, SpellDto dto, List<AuthoringError> errors)
    {
        var fields = new Fields(source, errors);
        return new SpellDefinition(
            fields.Text(dto.Id, "id"),
            fields.Text(dto.Name, "name"),
            fields.Text(dto.Shape, "shape"),
            fields.Int(dto.ManaCost, "manaCost"),
            fields.Int(dto.CastTicks, "castTicks"),
            fields.Int(dto.HitFrame, "hitFrame"),
            dto.Range ?? 0,
            dto.Damage is null ? null : (IntRange?)fields.ReadRange(dto.Damage, "damage"),
            dto.HealAmount ?? 0,
            dto.Radius ?? 0,
            dto.DamagePerLevel ?? 0,
            dto.HealPerLevel ?? 0);
    }
}
