using Mortar.Game.Content;

namespace Mortar.Game.Commands;

public sealed class SpendStatPointCommand : Command
{
    public SpendStatPointCommand(StatKind stat)
    {
        Stat = stat;
    }

    public StatKind Stat { get; }

    public override string Describe() => $"spend a point on {Stat.ToString().ToLowerInvariant()}";
}
