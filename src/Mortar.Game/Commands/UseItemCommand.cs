namespace Mortar.Game.Commands;

public sealed class UseItemCommand : Command
{
    public UseItemCommand(int itemId, int targetActorId = 0, Cell? targetCell = null)
    {
        ItemId = itemId;
        TargetActorId = targetActorId;
        TargetCell = targetCell;
    }

    public int ItemId { get; }

    public int TargetActorId { get; }

    public Cell? TargetCell { get; }

    public override string Describe() => $"use item #{ItemId}";
}
