namespace Mortar.Game.Commands;

public sealed class SellCommand : Command
{
    public SellCommand(int itemId)
    {
        ItemId = itemId;
    }

    public int ItemId { get; }

    public override string Describe() => $"sell item {ItemId}";
}
