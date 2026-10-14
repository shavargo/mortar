namespace Mortar.Game.Events;

public sealed class ActorMoved : GameEvent
{
    public ActorMoved(long tick, int actorId, Cell from, Cell to, int durationTicks)
        : base(tick)
    {
        ActorId = actorId;
        From = from;
        To = to;
        DurationTicks = durationTicks;
    }

    public int ActorId { get; }

    public Cell From { get; }

    public Cell To { get; }

    public int DurationTicks { get; }

    protected override string Describe() => $"#{ActorId} moved {From}->{To} in {DurationTicks}";
}
