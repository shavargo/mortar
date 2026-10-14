namespace Mortar.Game.Commands;

public sealed class MoveCommand : Command
{
    public MoveCommand(Cell destination)
    {
        Destination = destination;
    }

    public Cell Destination { get; }

    public override string Describe() => $"move to {Destination}";
}
