using System.Diagnostics.CodeAnalysis;
using Mortar.Core.Authoring;

namespace Mortar.Game.Content;

public sealed class ContentSet
{
    private readonly Dictionary<string, ClassDefinition> _classes;
    private readonly Dictionary<string, MonsterDefinition> _monsters;
    private readonly Dictionary<string, ItemBaseDefinition> _itemBases;
    private readonly Dictionary<string, AffixDefinition> _affixes;
    private readonly Dictionary<string, LootTableDefinition> _lootTables;

    private ContentSet(
        GameRules rules,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        IReadOnlyList<LevelDefinition> levels,
        ItemCatalog items)
    {
        Rules = rules;
        Classes = classes.ToArray();
        Monsters = monsters.ToArray();
        Levels = levels;
        Items = items;
        _classes = classes.ToDictionary(c => c.Id, StringComparer.Ordinal);
        _monsters = monsters.ToDictionary(m => m.Id, StringComparer.Ordinal);
        _itemBases = items.Bases.ToDictionary(b => b.Id, StringComparer.Ordinal);
        _affixes = items.Affixes.ToDictionary(a => a.Id, StringComparer.Ordinal);
        _lootTables = items.LootTables.ToDictionary(t => t.Id, StringComparer.Ordinal);
    }

    public GameRules Rules { get; }

    public IReadOnlyList<ClassDefinition> Classes { get; }

    public IReadOnlyList<MonsterDefinition> Monsters { get; }

    public IReadOnlyList<LevelDefinition> Levels { get; }

    public ItemCatalog Items { get; }

    public IReadOnlyList<ItemBaseDefinition> ItemBases => Items.Bases;

    public IReadOnlyList<AffixDefinition> Affixes => Items.Affixes;

    public int DeepestLevel => Levels.Count;

    public static AuthoringResult<ContentSet> Create(
        GameRules rules,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        IReadOnlyList<LevelDefinition> levels,
        ItemCatalog items)
    {
        var errors = ContentValidator.Validate(rules, classes, monsters, levels, items);
        if (errors.Count > 0)
        {
            return AuthoringResult<ContentSet>.Failure(errors);
        }
        var ordered = levels.OrderBy(level => level.Depth).ToList();
        return AuthoringResult<ContentSet>.Success(new ContentSet(rules, classes, monsters, ordered, items));
    }

    public bool TryGetClass(string id, [NotNullWhen(true)] out ClassDefinition? definition) =>
        _classes.TryGetValue(id, out definition);

    public MonsterDefinition Monster(string id) => _monsters[id];

    public bool TryGetMonster(string id, [NotNullWhen(true)] out MonsterDefinition? definition) =>
        _monsters.TryGetValue(id, out definition);

    public LevelDefinition Level(int depth) => Levels[depth - 1];

    public ItemBaseDefinition ItemBase(string id) => _itemBases[id];

    public bool TryGetItemBase(string id, [NotNullWhen(true)] out ItemBaseDefinition? definition) =>
        _itemBases.TryGetValue(id, out definition);

    public bool TryGetAffix(string id, [NotNullWhen(true)] out AffixDefinition? definition) =>
        _affixes.TryGetValue(id, out definition);

    public LootTableDefinition LootTable(string id) => _lootTables[id];
}
