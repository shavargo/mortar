using Mortar.Core.Authoring;

namespace Mortar.Game.Content;

internal static class SpellChecks
{
    private const int MaxValue = DefinitionChecks.MaxValue;

    public static void Validate(SpellCatalog spells, List<AuthoringError> errors)
    {
        ContentValidator.CheckUniqueIds(spells.Spells.Select(s => s.Id), "spell", errors);
        foreach (var spell in spells.Spells)
        {
            CheckSpell(spell, errors);
        }
    }

    private static void CheckSpell(SpellDefinition spell, List<AuthoringError> errors)
    {
        var source = $"spell '{spell.Id}'";
        if (string.IsNullOrWhiteSpace(spell.Name)) errors.Add(source, "name is required");
        if (!SpellShapes.IsKnown(spell.Shape)) errors.Add(source, $"unknown shape '{spell.Shape}'");
        if (spell.ManaCost < 0 || spell.ManaCost > MaxValue) errors.Add(source, $"manaCost must be between 0 and {MaxValue}");
        if (spell.CastTicks < 1 || spell.CastTicks > MaxValue) errors.Add(source, $"castTicks must be between 1 and {MaxValue}");
        if (spell.HitFrame < 1 || spell.HitFrame >= spell.CastTicks)
        {
            errors.Add(source, "hitFrame must satisfy 1 <= hitFrame < castTicks");
        }
        if (spell.Range < 0 || spell.Range > MaxValue) errors.Add(source, $"range must be between 0 and {MaxValue}");
        if (spell.HealAmount < 0 || spell.HealAmount > MaxValue) errors.Add(source, $"healAmount must be between 0 and {MaxValue}");
        if (spell.Radius < 0 || spell.Radius > MaxValue) errors.Add(source, $"radius must be between 0 and {MaxValue}");
        if (spell.DamagePerLevel < 0 || spell.DamagePerLevel > MaxValue)
        {
            errors.Add(source, $"damagePerLevel must be between 0 and {MaxValue}");
        }
        if (spell.HealPerLevel < 0 || spell.HealPerLevel > MaxValue)
        {
            errors.Add(source, $"healPerLevel must be between 0 and {MaxValue}");
        }
        if (spell.Damage is { } damage) DefinitionChecks.CheckRange(source, "damage", damage, 0, errors);
        if (spell.Shape == SpellShapes.Projectile && spell.Range < 1)
        {
            errors.Add(source, "projectile spells need range of at least 1");
        }
        if (spell.Shape == SpellShapes.Area && spell.Radius < 1)
        {
            errors.Add(source, "area spells need radius of at least 1");
        }
        if (spell.Shape == SpellShapes.Self && spell.Damage is not null)
        {
            errors.Add(source, "self spells do not carry damage");
        }
    }
}
