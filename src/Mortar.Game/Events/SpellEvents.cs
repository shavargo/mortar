namespace Mortar.Game.Events;

public sealed class SpellCastStarted : GameEvent
{
    public SpellCastStarted(long tick, int casterId, string spellId, int durationTicks)
        : base(tick)
    {
        CasterId = casterId;
        SpellId = spellId;
        DurationTicks = durationTicks;
    }

    public int CasterId { get; }

    public string SpellId { get; }

    public int DurationTicks { get; }

    protected override string Describe() => $"#{CasterId} casts {SpellId} for {DurationTicks}";
}

public sealed class SpellHealed : GameEvent
{
    public SpellHealed(long tick, int actorId, string spellId, int amount, int life)
        : base(tick)
    {
        ActorId = actorId;
        SpellId = spellId;
        Amount = amount;
        Life = life;
    }

    public int ActorId { get; }

    public string SpellId { get; }

    public int Amount { get; }

    public int Life { get; }

    protected override string Describe() => $"#{ActorId} healed {Amount} with {SpellId} (life {Life})";
}
