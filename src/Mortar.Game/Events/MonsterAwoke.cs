namespace Mortar.Game.Events;

public sealed class MonsterAwoke : GameEvent
{
    public MonsterAwoke(long tick, int actorId)
        : base(tick)
    {
        ActorId = actorId;
    }

    public int ActorId { get; }

    protected override string Describe() => $"#{ActorId} woke up";
}
