using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public sealed class PlayerProgress
{
    private readonly List<KnownSpell> _spells = new List<KnownSpell>();

    public PlayerProgress(string classId, int level, long experience, Stats baseStats, int unspentPoints)
    {
        ClassId = classId;
        Level = level;
        Experience = experience;
        BaseStats = baseStats;
        UnspentPoints = unspentPoints;
    }

    public string ClassId { get; }

    public int Level { get; internal set; }

    public long Experience { get; internal set; }

    public Stats BaseStats { get; internal set; }

    public int UnspentPoints { get; internal set; }

    public IReadOnlyList<KnownSpell> Spells => _spells;

    public static PlayerProgress Start(ClassDefinition definition) =>
        new PlayerProgress(definition.Id, 1, 0, definition.BaseStats, 0);

    public int SpellLevel(string spellId)
    {
        var known = Find(spellId);
        return known?.Level ?? 0;
    }

    public bool Knows(string spellId) => Find(spellId) is not null;

    internal bool Learn(string spellId, int maxLevel)
    {
        var known = Find(spellId);
        if (known is null)
        {
            var index = _spells.FindIndex(spell => string.CompareOrdinal(spell.SpellId, spellId) > 0);
            _spells.Insert(index < 0 ? _spells.Count : index, new KnownSpell(spellId, 1));
            return true;
        }
        if (known.Level >= maxLevel) return false;
        known.Level++;
        return true;
    }

    internal void SetSpell(string spellId, int level)
    {
        var known = Find(spellId);
        if (known is null)
        {
            var index = _spells.FindIndex(spell => string.CompareOrdinal(spell.SpellId, spellId) > 0);
            _spells.Insert(index < 0 ? _spells.Count : index, new KnownSpell(spellId, level));
            return;
        }
        known.Level = level;
    }

    private KnownSpell? Find(string spellId) => _spells.Find(spell => spell.SpellId == spellId);
}
