namespace Mortar.Game.Commands;

public sealed class IdentifyServiceCommand : Command
{
    public IdentifyServiceCommand(int itemId)
    {
        ItemId = itemId;
    }

    public int ItemId { get; }

    public override string Describe() => $"identify service item {ItemId}";
}
