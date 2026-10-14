using Mortar.Game.Commands;

namespace Mortar.Game.Events;

public sealed class CommandRejected : GameEvent
{
    public CommandRejected(long tick, Command command, string reason)
        : base(tick)
    {
        Command = command;
        Reason = reason;
    }

    public Command Command { get; }

    public string Reason { get; }

    protected override string Describe() => $"rejected '{Command.Describe()}': {Reason}";
}
