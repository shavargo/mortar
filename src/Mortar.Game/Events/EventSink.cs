using Mortar.Game.Commands;

namespace Mortar.Game.Events;

internal sealed class EventSink
{
    private readonly List<GameEvent> _events = new List<GameEvent>();

    public EventSink(long tick)
    {
        Tick = tick;
    }

    public long Tick { get; }

    public IReadOnlyList<GameEvent> Events => _events;

    public void Add(GameEvent gameEvent) => _events.Add(gameEvent);

    public void Reject(Command command, string reason) => _events.Add(new CommandRejected(Tick, command, reason));
}
