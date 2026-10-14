namespace Mortar.Game.Events;

public abstract class GameEvent
{
    protected GameEvent(long tick)
    {
        Tick = tick;
    }

    public long Tick { get; }

    public override string ToString() => $"{Tick} {Describe()}";

    protected abstract string Describe();
}
