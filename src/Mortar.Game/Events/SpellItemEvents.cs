namespace Mortar.Game.Events;

public sealed class SpellLearned : GameEvent
{
    public SpellLearned(long tick, string spellId, int level)
        : base(tick)
    {
        SpellId = spellId;
        Level = level;
    }

    public string SpellId { get; }

    public int Level { get; }

    protected override string Describe() => Level <= 1 ? $"learned {SpellId}" : $"{SpellId} is now level {Level}";
}

public sealed class ItemConsumed : GameEvent
{
    public ItemConsumed(long tick, int itemId, string definitionId)
        : base(tick)
    {
        ItemId = itemId;
        DefinitionId = definitionId;
    }

    public int ItemId { get; }

    public string DefinitionId { get; }

    protected override string Describe() => $"used {DefinitionId} #{ItemId}";
}
