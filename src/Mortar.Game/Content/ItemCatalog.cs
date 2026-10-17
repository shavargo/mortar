namespace Mortar.Game.Content;

public sealed class ItemCatalog
{
    public ItemCatalog(
        IReadOnlyList<ItemBaseDefinition> bases,
        IReadOnlyList<AffixDefinition> affixes,
        IReadOnlyList<LootTableDefinition> lootTables,
        IReadOnlyList<UniqueDefinition>? uniques = null)
    {
        Bases = bases.ToArray();
        Affixes = affixes.ToArray();
        LootTables = lootTables.ToArray();
        Uniques = (uniques ?? Array.Empty<UniqueDefinition>()).ToArray();
    }

    public static ItemCatalog Empty { get; } = new ItemCatalog(
        Array.Empty<ItemBaseDefinition>(),
        Array.Empty<AffixDefinition>(),
        Array.Empty<LootTableDefinition>(),
        Array.Empty<UniqueDefinition>());

    public IReadOnlyList<ItemBaseDefinition> Bases { get; }

    public IReadOnlyList<AffixDefinition> Affixes { get; }

    public IReadOnlyList<LootTableDefinition> LootTables { get; }

    public IReadOnlyList<UniqueDefinition> Uniques { get; }
}
