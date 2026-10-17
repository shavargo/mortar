namespace Mortar.Game.Commands;

public sealed class RepairCommand : Command
{
    public RepairCommand(int itemId)
    {
        ItemId = itemId;
    }

    public int ItemId { get; }

    public override string Describe() => $"repair item {ItemId}";
}
