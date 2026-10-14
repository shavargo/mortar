using System.Diagnostics.CodeAnalysis;
using Mortar.Core.Authoring;

namespace Mortar.Game.Content;

public sealed class ContentSet
{
    private readonly Dictionary<string, ClassDefinition> _classes;
    private readonly Dictionary<string, MonsterDefinition> _monsters;

    private ContentSet(
        GameRules rules,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        IReadOnlyList<LevelDefinition> levels)
    {
        Rules = rules;
        Classes = classes;
        Monsters = monsters;
        Levels = levels;
        _classes = classes.ToDictionary(c => c.Id, StringComparer.Ordinal);
        _monsters = monsters.ToDictionary(m => m.Id, StringComparer.Ordinal);
    }

    public GameRules Rules { get; }

    public IReadOnlyList<ClassDefinition> Classes { get; }

    public IReadOnlyList<MonsterDefinition> Monsters { get; }

    public IReadOnlyList<LevelDefinition> Levels { get; }

    public int DeepestLevel => Levels.Count;

    public static AuthoringResult<ContentSet> Create(
        GameRules rules,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        IReadOnlyList<LevelDefinition> levels)
    {
        var errors = ContentValidator.Validate(rules, classes, monsters, levels);
        if (errors.Count > 0)
        {
            return AuthoringResult<ContentSet>.Failure(errors);
        }
        var ordered = levels.OrderBy(level => level.Depth).ToList();
        return AuthoringResult<ContentSet>.Success(new ContentSet(rules, classes, monsters, ordered));
    }

    public bool TryGetClass(string id, [NotNullWhen(true)] out ClassDefinition? definition) =>
        _classes.TryGetValue(id, out definition);

    public MonsterDefinition Monster(string id) => _monsters[id];

    public LevelDefinition Level(int depth) => Levels[depth - 1];
}
