namespace Mortar.Game.Simulation;

public sealed class KnownSpell
{
    internal KnownSpell(string spellId, int level)
    {
        SpellId = spellId;
        Level = level;
    }

    public string SpellId { get; }

    public int Level { get; internal set; }
}
