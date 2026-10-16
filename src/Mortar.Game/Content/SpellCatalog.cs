namespace Mortar.Game.Content;

public sealed class SpellCatalog
{
    public SpellCatalog(IReadOnlyList<SpellDefinition> spells)
    {
        Spells = spells.ToArray();
    }

    public static SpellCatalog Empty { get; } = new SpellCatalog(Array.Empty<SpellDefinition>());

    public IReadOnlyList<SpellDefinition> Spells { get; }
}
