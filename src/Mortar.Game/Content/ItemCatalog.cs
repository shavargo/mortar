namespace Mortar.Game.Content;

public sealed class ItemCatalog
{
    public ItemCatalog(
        IReadOnlyList<ItemBaseDefinition> bases,
        IReadOnlyList<AffixDefinition> affixes,
        IReadOnlyList<LootTableDefinition> lootTables)
    {
        Bases = bases.ToArray();
        Affixes = affixes.ToArray();
        LootTables = lootTables.ToArray();
    }

    public static ItemCatalog Empty { get; } = new ItemCatalog(
        Array.Empty<ItemBaseDefinition>(), Array.Empty<AffixDefinition>(), Array.Empty<LootTableDefinition>());

    public IReadOnlyList<ItemBaseDefinition> Bases { get; }

    public IReadOnlyList<AffixDefinition> Affixes { get; }

    public IReadOnlyList<LootTableDefinition> LootTables { get; }
}
