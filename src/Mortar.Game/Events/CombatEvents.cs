namespace Mortar.Game.Events;

public enum AttackOutcome
{
    Miss,
    Blocked,
    Hit,
}

public sealed class AttackStarted : GameEvent
{
    public AttackStarted(long tick, int attackerId, int targetId, int durationTicks)
        : base(tick)
    {
        AttackerId = attackerId;
        TargetId = targetId;
        DurationTicks = durationTicks;
    }

    public int AttackerId { get; }

    public int TargetId { get; }

    public int DurationTicks { get; }

    protected override string Describe() => $"#{AttackerId} swings at #{TargetId} for {DurationTicks}";
}

public sealed class AttackResolved : GameEvent
{
    public AttackResolved(long tick, int attackerId, int targetId, AttackOutcome outcome, int damage)
        : base(tick)
    {
        AttackerId = attackerId;
        TargetId = targetId;
        Outcome = outcome;
        Damage = damage;
    }

    public int AttackerId { get; }

    public int TargetId { get; }

    public AttackOutcome Outcome { get; }

    public int Damage { get; }

    protected override string Describe() => Outcome switch
    {
        AttackOutcome.Hit => $"#{AttackerId} hit #{TargetId} for {Damage}",
        AttackOutcome.Blocked => $"#{TargetId} blocked #{AttackerId}",
        _ => $"#{AttackerId} missed #{TargetId}",
    };
}

public sealed class ActorStaggered : GameEvent
{
    public ActorStaggered(long tick, int actorId, int durationTicks)
        : base(tick)
    {
        ActorId = actorId;
        DurationTicks = durationTicks;
    }

    public int ActorId { get; }

    public int DurationTicks { get; }

    protected override string Describe() => $"#{ActorId} staggered for {DurationTicks}";
}

public sealed class ActorDied : GameEvent
{
    public ActorDied(long tick, int actorId, int killerId)
        : base(tick)
    {
        ActorId = actorId;
        KillerId = killerId;
    }

    public int ActorId { get; }

    /// <summary>The last actor to hit the one that died, or 0 if none.</summary>
    public int KillerId { get; }

    protected override string Describe() =>
        KillerId == 0 ? $"#{ActorId} died" : $"#{ActorId} died (killed by #{KillerId})";
}
