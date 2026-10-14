using Mortar.Game.Commands;

namespace Mortar.Game.Events;

public sealed class CommandRejected : GameEvent
{
    public const string MissingCommand = "missing command";

    public CommandRejected(long tick, Command? command, string reason)
        : base(tick)
    {
        Command = command;
        Reason = reason;
    }

    /// <summary>The rejected command; null when the caller passed a null entry.</summary>
    public Command? Command { get; }

    public string Reason { get; }

    protected override string Describe() => $"rejected '{Command?.Describe() ?? "(none)"}': {Reason}";
}
