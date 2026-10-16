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
    private readonly Dictionary<string, SpellDefinition> _spells;

    private ContentSet(
        GameRules rules,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        IReadOnlyList<LevelDefinition> levels,
        ItemCatalog items,
        SpellCatalog spells)
    {
        Rules = rules;
        Classes = classes.ToArray();
        Monsters = monsters.ToArray();
        Levels = levels;
        Items = items;
        Spells = spells;
        _classes = classes.ToDictionary(c => c.Id, StringComparer.Ordinal);
        _monsters = monsters.ToDictionary(m => m.Id, StringComparer.Ordinal);
        _itemBases = items.Bases.ToDictionary(b => b.Id, StringComparer.Ordinal);
        _affixes = items.Affixes.ToDictionary(a => a.Id, StringComparer.Ordinal);
        _lootTables = items.LootTables.ToDictionary(t => t.Id, StringComparer.Ordinal);
        _spells = spells.Spells.ToDictionary(s => s.Id, StringComparer.Ordinal);
    }

    public GameRules Rules { get; }

    public IReadOnlyList<ClassDefinition> Classes { get; }

    public IReadOnlyList<MonsterDefinition> Monsters { get; }

    public IReadOnlyList<LevelDefinition> Levels { get; }

    public ItemCatalog Items { get; }

    public SpellCatalog Spells { get; }

    public IReadOnlyList<ItemBaseDefinition> ItemBases => Items.Bases;

    public IReadOnlyList<AffixDefinition> Affixes => Items.Affixes;

    /// <summary>0 when the content has a town, otherwise 1.</summary>
    public int ShallowestLevel => Levels[0].Depth;

    public int DeepestLevel => Levels[Levels.Count - 1].Depth;

    public static AuthoringResult<ContentSet> Create(
        GameRules rules,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        IReadOnlyList<LevelDefinition> levels,
        ItemCatalog items,
        SpellCatalog? spells = null)
    {
        var spellCatalog = spells ?? SpellCatalog.Empty;
        var errors = ContentValidator.Validate(rules, classes, monsters, levels, items, spellCatalog);
        if (errors.Count > 0)
        {
            return AuthoringResult<ContentSet>.Failure(errors);
        }
        var ordered = levels.OrderBy(level => level.Depth).ToList();
        return AuthoringResult<ContentSet>.Success(new ContentSet(rules, classes, monsters, ordered, items, spellCatalog));
    }

    public bool TryGetClass(string id, [NotNullWhen(true)] out ClassDefinition? definition) =>
        _classes.TryGetValue(id, out definition);

    public MonsterDefinition Monster(string id) => _monsters[id];

    public bool TryGetMonster(string id, [NotNullWhen(true)] out MonsterDefinition? definition) =>
        _monsters.TryGetValue(id, out definition);

    public LevelDefinition Level(int depth) => Levels[depth - ShallowestLevel];

    public ItemBaseDefinition ItemBase(string id) => _itemBases[id];

    public bool TryGetItemBase(string id, [NotNullWhen(true)] out ItemBaseDefinition? definition) =>
        _itemBases.TryGetValue(id, out definition);

    public bool TryGetAffix(string id, [NotNullWhen(true)] out AffixDefinition? definition) =>
        _affixes.TryGetValue(id, out definition);

    public LootTableDefinition LootTable(string id) => _lootTables[id];

    public SpellDefinition Spell(string id) => _spells[id];

    public bool TryGetSpell(string id, [NotNullWhen(true)] out SpellDefinition? definition) =>
        _spells.TryGetValue(id, out definition);
}
